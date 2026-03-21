using System.Collections;
using UnityEngine;

public class FighterController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float attackCooldown = 0.5f;
    public float jumpForce = 7f;
    public HitBox hitBox;
    public Transform opponent;
    public Rigidbody rb;

    public bool isCPU = false;
    public float attackRange = 1.5f;

    // --- 新規追加部分：防御機能 ---
    public bool isDefending { get; private set; } = false;
    // ----------------------------

    // --- 新規追加部分：ダッシュ関連パラメータ ---
    public float dashSpeed = 15f;
    public float dashDuration = 0.15f;
    private bool isDashing = false;
    private float lastLeftTapTime = -999f;
    private float lastRightTapTime = -999f;
    private float doubleTapThreshold = 0.3f;
    // ------------------------------------

    // --- 新規追加部分：ガードクラッシュ状態とコマンド入力バッファ ---
    public bool isGuardCrushed { get; private set; } = false;

    private System.Collections.Generic.List<string> inputBuffer = new System.Collections.Generic.List<string>(10);
    private float inputBufferTimer = 0f;
    private float inputBufferLifetime = 0.5f; // コマンドの有効時間（0.5秒）
    // ------------------------------------

    // --- 新規追加部分：攻撃の種類に応じたパラメータ ---
    private float currentAttackScaleX = 1.2f;
    private float currentAttackScaleY = 0.9f;
    // ------------------------------------------

    // --- 新規追加部分：画面両サイドの壁（移動制限） ---
    // インスペクタで左右の壁の座標を調整できます。
    public float minBoundaryX = -9f;
    public float maxBoundaryX = 9f;
    // ------------------------------------------

    // --- 新規追加部分：AIステートとタイマー ---
    private enum CPUState { Idle, Approach, Retreat, Attack, Defend }
    private CPUState currentCpuState = CPUState.Idle;
    private float cpuActionTimer = 0f;
    // ----------------------------------------

    private float lastAttackTime = -999f;
    private FighterStats stats;
    private AudioSource audioSource;

    private Vector3 originalScale;
    private bool isAttacking = false;
    private bool isGrounded = true;

    void Start()
    {
        stats = GetComponent<FighterStats>();
        audioSource = GetComponent<AudioSource>();
        originalScale = transform.localScale;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    void Update()
    {
        if (stats != null && stats.IsDead)
            return;

        // --- 新規追加部分：ゲーム終了演出中は操作を受け付けない ---
        if (isGameOverAnimating)
            return;
        // ----------------------------------------------------

        if (!GameManager.CanControl)
            return;

        if (isCPU)
        {
            CPUControl();
        }
        else
        {
            // --- 新規追加部分：防御入力とコマンド入力受付 ---
            Defend();
            ProcessCommandInputs();
            // ----------------------------
            Move();
            Jump();
            Attack();
        }

        FaceOpponent();

        // --- 新規追加部分：画面外に出ないようにクランプ ---
        ClampPosition();
        // --------------------------------------------
    }

    void ClampPosition()
    {
        // 指定した minBoundaryX ～ maxBoundaryX の範囲に X座標 を制限します
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minBoundaryX, maxBoundaryX);
        transform.position = pos;
    }

    void Move()
    {
        // --- 新規追加部分：ガード中・ガードクラッシュ中は移動不可 ---
        if (isDefending || isGuardCrushed) return;
        // ------------------------------------

        // --- 新規追加部分：ダッシュ中は普通の移動入力を受け付けない ---
        if (isDashing) return;
        // --------------------------------------------------------

        float move = 0f;

        if (CompareTag("Player"))
        {
            if (Input.GetKey(KeyCode.A)) move = -1f;
            if (Input.GetKey(KeyCode.D)) move = 1f;

            // --- 新規追加部分：A・Dキーのダブルタップでダッシュ ---
            if (Input.GetKeyDown(KeyCode.A))
            {
                if (Time.time - lastLeftTapTime < doubleTapThreshold) StartCoroutine(DashRoutine(-1f));
                lastLeftTapTime = Time.time;
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                if (Time.time - lastRightTapTime < doubleTapThreshold) StartCoroutine(DashRoutine(1f));
                lastRightTapTime = Time.time;
            }
            // -------------------------------------------------
        }
        else if (CompareTag("Enemy") && !isCPU) // CPUではない2P操作時
        {
            if (Input.GetKey(KeyCode.LeftArrow)) move = -1f;
            if (Input.GetKey(KeyCode.RightArrow)) move = 1f;

            // --- 新規追加部分：左右矢印キーのダブルタップでダッシュ ---
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (Time.time - lastLeftTapTime < doubleTapThreshold) StartCoroutine(DashRoutine(-1f));
                lastLeftTapTime = Time.time;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (Time.time - lastRightTapTime < doubleTapThreshold) StartCoroutine(DashRoutine(1f));
                lastRightTapTime = Time.time;
            }
            // -------------------------------------------------
        }

        transform.Translate(Vector3.right * move * moveSpeed * Time.deltaTime, Space.World);
    }

    // --- 新規追加部分：ダッシュ・バックステップのコルーチン処理 ---
    IEnumerator DashRoutine(float direction)
    {
        if (isAttacking || !isGrounded) yield break;

        isDashing = true;
        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            // ダッシュ中にガードを入力すると「ダッシュキャンセル」ができる（攻防の駆け引き）
            if (isDefending) break;

            transform.Translate(Vector3.right * direction * dashSpeed * Time.deltaTime, Space.World);
            ClampPosition(); // 画面端を突き抜けないように毎回制限をかける
            yield return null;
        }

        // 短い硬直を入れて連続ダッシュを防止
        yield return new WaitForSeconds(0.1f);
        isDashing = false;
    }
    // ----------------------------------------------------------

    void Jump()
    {
        if (!isGrounded) return;
        if (rb == null) return;

        // --- 新規追加部分：ガード・ダッシュ・ガードクラッシュ中はジャンプ不可 ---
        if (isDefending || isDashing || isGuardCrushed) return;
        // ------------------------------------

        bool jumpKey = false;

        if (CompareTag("Player"))
        {
            jumpKey = Input.GetKeyDown(KeyCode.W);
        }
        else if (CompareTag("Enemy"))
        {
            jumpKey = Input.GetKeyDown(KeyCode.UpArrow);
        }

        if (jumpKey)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            isGrounded = false;
        }
    }

    void Attack()
    {
        // --- 新規追加部分：ガード・ダッシュ・ガードクラッシュ中は攻撃不可 ---
        if (isDefending || isDashing || isGuardCrushed) return;
        // ------------------------------------

        // --- 新規修正部分：複数種類のアクション入力を取得 ---
        bool weakAttack = false;
        bool strongAttack = false;
        bool specialAttack = false;
        bool throwAttack = false; // 新規：投げ技用

        if (CompareTag("Player"))
        {
            weakAttack = Input.GetKeyDown(KeyCode.Space);
            strongAttack = Input.GetKeyDown(KeyCode.F);
            specialAttack = Input.GetKeyDown(KeyCode.G);
            throwAttack = Input.GetKeyDown(KeyCode.E); // Eキーで投げ
        }
        else if (CompareTag("Enemy"))
        {
            weakAttack = Input.GetKeyDown(KeyCode.Return);
            strongAttack = Input.GetKeyDown(KeyCode.RightShift);
            specialAttack = Input.GetKeyDown(KeyCode.RightControl);
            throwAttack = Input.GetKeyDown(KeyCode.Backspace); // Backspaceキーで投げ
        }

        // --- 新規追加部分：コマンド入力（↓↘→）の判定 ---
        bool hasCommand = false;
        int downIdx = inputBuffer.LastIndexOf("Down");
        int fwdIdx = inputBuffer.LastIndexOf("Forward");

        // Downが入力された後、0.5秒以内にForwardが押されていれば「コマンド成立」とする
        if (downIdx != -1 && fwdIdx != -1 && fwdIdx > downIdx)
        {
            hasCommand = true;
        }
        // --------------------------------------------

        // 弱攻撃
        if (weakAttack) 
        {
            if (hasCommand)
            {
                // コマンド必殺技（強化版）
                Debug.Log(gameObject.name + " COMMAND SPECIAL!");
                TryAttack(40, 1.5f, 0.5f, 2.5f, 1.5f);
                inputBuffer.Clear(); // 技が出たらバッファクリア
            }
            else
            {
                TryAttack(10, 0.5f, 0.2f, 1.2f, 0.9f);
            }
        }
        // 強攻撃
        else if (strongAttack) TryAttack(20, 1.0f, 0.4f, 1.6f, 0.9f);
        // 必殺技
        else if (specialAttack) TryAttack(30, 2.0f, 0.6f, 2.0f, 1.2f);
        // 投げ技（ガード不能ダメージ25, 密着のみ, 当たれば吹き飛ばす）
        else if (throwAttack) TryThrow();
        // ----------------------------------------------------
    }

    void CPUControl()
    {
        if (opponent == null) return;

        // --- 新規修正部分：AIのステートマシン化 ---
        float distance = Mathf.Abs(opponent.position.x - transform.position.x);
        float dir = opponent.position.x > transform.position.x ? 1f : -1f;

        cpuActionTimer -= Time.deltaTime;

        // タイマーがゼロになったら次の行動をランダムに決定
        if (cpuActionTimer <= 0f)
        {
            DecideNextCpuAction(distance);
        }

        // ステートに応じた行動を実行
        ExecuteCpuState(dir);
        // ------------------------------------------
    }

    // --- 新規追加部分：AI行動決定と実行ロジック ---
    void DecideNextCpuAction(float distance)
    {
        isDefending = false; // ガードはいったん解除

        if (distance > attackRange * 1.5f)
        {
            // 確実に近づくように修正
            currentCpuState = CPUState.Approach;
            cpuActionTimer = 0.5f;
        }
        else if (distance > attackRange)
        {
            // 中距離：強攻撃か近づく
            float r = Random.value;
            if (r < 0.6f) currentCpuState = CPUState.Approach;
            else currentCpuState = CPUState.Attack;

            cpuActionTimer = 0.4f;
        }
        else
        {
            // 近距離：攻撃、ガード、離れる
            float r = Random.value;
            if (r < 0.5f) currentCpuState = CPUState.Attack;
            else if (r < 0.8f) currentCpuState = CPUState.Defend;
            else currentCpuState = CPUState.Retreat;

            cpuActionTimer = 0.4f;
        }
    }

    void ExecuteCpuState(float dirToOpponent)
    {
        // 攻撃中は何もしない
        if (isAttacking) return;

        switch (currentCpuState)
        {
            case CPUState.Idle:
                break;

            case CPUState.Approach:
                transform.Translate(Vector3.right * dirToOpponent * moveSpeed * Time.deltaTime, Space.World);
                break;

            case CPUState.Retreat:
                transform.Translate(Vector3.right * -dirToOpponent * moveSpeed * Time.deltaTime, Space.World);
                break;

            case CPUState.Defend:
                // ガードし続けるように追加
                isDefending = true;
                break;

            case CPUState.Attack:
                float r = Random.value;
                if (r < 0.5f) TryAttack(10, 0.5f, 0.2f, 1.2f, 0.9f); // 弱
                else if (r < 0.9f) TryAttack(20, 1.0f, 0.4f, 1.6f, 0.9f); // 強
                else TryAttack(30, 2.0f, 0.6f, 2.0f, 1.2f); // 必殺技

                // 攻撃中ステートをリセットするのはTryAttackが成功したときだけにする
                currentCpuState = CPUState.Idle;
                cpuActionTimer = attackCooldown; 
                break;
        }
    }
    // ------------------------------------------

    // --- 新規追加部分：防御ロジック ---
    void Defend()
    {
        if (!isGrounded || isAttacking || isDashing || isGuardCrushed) 
        {
            isDefending = false;
            return;
        }

        if (CompareTag("Player"))
        {
            isDefending = Input.GetKey(KeyCode.S);
        }
        else if (CompareTag("Enemy"))
        {
            isDefending = Input.GetKey(KeyCode.DownArrow);
        }
    }
    // ----------------------------

    // --- 新規修正部分：各種攻撃のアクション処理 ---
    void TryAttack(int damage, float cooldown, float hitDuration, float scaleX, float scaleY)
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        if (isAttacking) return; // 攻撃中は新しい攻撃を出せない

        attackCooldown = cooldown;
        lastAttackTime = Time.time;
        
        currentAttackScaleX = scaleX;
        currentAttackScaleY = scaleY;

        StartCoroutine(AttackSequence(damage, hitDuration));
    }

    IEnumerator AttackSequence(int damage, float duration)
    {
        isAttacking = true;

        // 必殺技（ダメージ30想定）のときはタメ（予備動作）を入れる
        if (damage >= 30)
        {
            // まずはちょっと縮む（溜め）
            currentAttackScaleX = 0.5f;
            currentAttackScaleY = 1.2f;
            yield return new WaitForSeconds(0.4f); // 0.4秒溜める

            // その後大きく伸びて攻撃
            currentAttackScaleX = 2.0f;
            currentAttackScaleY = 1.2f;
            if (hitBox != null) hitBox.ActivateHitBox(0.4f, damage);
            yield return new WaitForSeconds(0.4f);
        }
        else
        {
            // 通常攻撃・強攻撃
            if (hitBox != null) hitBox.ActivateHitBox(duration, damage);
            yield return new WaitForSeconds(duration);
        }

        isAttacking = false;
    }
    // ------------------------------------------

    // --- 新規追加部分：コマンド入力受付ロジック ---
    void ProcessCommandInputs()
    {
        if (isCPU) return;

        if (inputBufferTimer > 0)
        {
            inputBufferTimer -= Time.deltaTime;
            if (inputBufferTimer <= 0) inputBuffer.Clear();
        }

        // 相手の方向を向いているかを判定して、Forward（前進方向キー）を特定する
        float dir = opponent != null && opponent.position.x > transform.position.x ? 1f : -1f;

        if (CompareTag("Player"))
        {
            if (Input.GetKeyDown(KeyCode.S)) AddInputToBuffer("Down");
            
            if (dir > 0) {
                if (Input.GetKeyDown(KeyCode.D)) AddInputToBuffer("Forward");
            } else {
                if (Input.GetKeyDown(KeyCode.A)) AddInputToBuffer("Forward");
            }
        }
        else if (CompareTag("Enemy"))
        {
            if (Input.GetKeyDown(KeyCode.DownArrow)) AddInputToBuffer("Down");
            
            if (dir > 0) {
                if (Input.GetKeyDown(KeyCode.RightArrow)) AddInputToBuffer("Forward");
            } else {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) AddInputToBuffer("Forward");
            }
        }
    }

    void AddInputToBuffer(string input)
    {
        // 連続で同じ入力が詰まるのを防ぐ
        if (inputBuffer.Count == 0 || inputBuffer[inputBuffer.Count - 1] != input)
        {
            inputBuffer.Add(input);
            inputBufferTimer = inputBufferLifetime;
            
            // バッファ肥大化防止
            if (inputBuffer.Count > 10) inputBuffer.RemoveAt(0);
        }
    }
    // ------------------------------------------

    // --- 新規追加部分：投げ技（ガード不能の密着攻撃） ---
    void TryThrow()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        if (isAttacking) return;

        attackCooldown = 0.8f; // 投げの全体フレーム
        lastAttackTime = Time.time;
        
        StartCoroutine(ThrowSequence());
    }

    IEnumerator ThrowSequence()
    {
        isAttacking = true;
        
        // 少し前傾姿勢になる（投げの予備動作）
        currentAttackScaleX = 1.1f;
        currentAttackScaleY = 1.2f;
        
        yield return new WaitForSeconds(0.15f); // 発生時間
        
        // 判定（密着しているか）
        if (opponent != null)
        {
            float dist = Mathf.Abs(opponent.position.x - transform.position.x);
            if (dist < attackRange * 0.7f) // 通常攻撃より射程が短い
            {
                FighterStats enemyStats = opponent.GetComponent<FighterStats>();
                if (enemyStats != null && !enemyStats.IsDead)
                {
                    // 投げ成立！ 第3引数の true が isUnblockable（ガード不能）
                    enemyStats.TakeDamage(25, transform.position, true);
                    
                    // 相手を大きく吹き飛ばす演出
                    Rigidbody opponentRb = opponent.GetComponent<Rigidbody>();
                    if (opponentRb != null)
                    {
                        opponentRb.linearVelocity = Vector3.zero;
                        float dir = opponent.position.x > transform.position.x ? 1f : -1f;
                        opponentRb.AddForce(new Vector3(dir * 12f, 8f, 0), ForceMode.Impulse);
                    }
                }
            }
        }
        
        // 投げ終わりの隙
        currentAttackScaleX = 1.0f;
        currentAttackScaleY = 1.0f;
        yield return new WaitForSeconds(0.4f); 
        
        isAttacking = false;
    }
    // ------------------------------------------

    // --- 新規追加部分：ガードクラッシュ処理 ---
    public void ApplyGuardCrush(float duration)
    {
        StartCoroutine(GuardCrushRoutine(duration));
    }

    IEnumerator GuardCrushRoutine(float duration)
    {
        isGuardCrushed = true;
        isAttacking = false;
        isDashing = false;
        isDefending = false;

        // 体勢を大きく崩す演出（Z軸に傾ける）
        float originalZ = transform.rotation.eulerAngles.z;
        float dir = opponent != null && opponent.position.x > transform.position.x ? 1f : -1f;
        transform.rotation = Quaternion.Euler(0, 0, 25f * -dir);

        yield return new WaitForSeconds(duration);

        // 元に戻す
        transform.rotation = Quaternion.Euler(0, 0, originalZ);
        isGuardCrushed = false;
    }
    // ------------------------------------------

    void FaceOpponent()
    {
        if (opponent == null) return;

        Vector3 scale = transform.localScale;
        float absOriginalX = Mathf.Abs(originalScale.x);

        if (opponent.position.x > transform.position.x)
            scale.x = absOriginalX;
        else
            scale.x = -absOriginalX;

        if (isAttacking)
        {
            // --- 新規変更部分：現在のアクションによるスケールを適用 ---
            scale.x *= currentAttackScaleX;
            scale.y = originalScale.y * currentAttackScaleY;
            // ---------------------------------------------------
        }
        // --- 新規追加部分：ガード時の見た目（しゃがむ） ---
        else if (isDefending)
        {
            scale.y = originalScale.y * 0.7f;
        }
        // ------------------------------------------
        else
        {
            scale.y = originalScale.y;
        }

        transform.localScale = scale;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isGrounded = true;
        }
    }

    // --- 新規追加部分：勝利・敗北の演出処理 ---
    private bool isGameOverAnimating = false;

    // 敗北時の演出（90度倒れる）
    public void PlayDieAnimation()
    {
        isGameOverAnimating = true;
        isAttacking = false;
        isDefending = false;
        
        // Z軸に90度回転させて「倒れた」ように見せる
        transform.rotation = Quaternion.Euler(0, 0, 90f);

        // 倒れたら地面にちょっと沈むかもしれないので、少しY軸を調整（任意）
        Vector3 pos = transform.position;
        pos.y = 0.5f; // 環境に合わせて調整が必要かもしれません
        transform.position = pos;
    }

    // 勝利時の演出（ピョンピョン跳ねる）
    public void PlayVictoryAnimation()
    {
        isGameOverAnimating = true;
        isAttacking = false;
        isDefending = false;

        StartCoroutine(VictoryAnimationRoutine());
    }

    private IEnumerator VictoryAnimationRoutine()
    {
        // 3回ほど小さくジャンプして喜ぶ
        for (int i = 0; i < 3; i++)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0, jumpForce * 0.7f, 0); // 少し低めのジャンプ
            }
            yield return new WaitForSeconds(0.4f);
        }
    }
    // ----------------------------------------
}