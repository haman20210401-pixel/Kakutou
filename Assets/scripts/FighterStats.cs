using System.Collections;
using UnityEngine;

public class FighterStats : MonoBehaviour
{
    public int maxHP = 100;
    public int currentHP;
    public float knockbackPower = 0.5f;

    // --- 新規追加部分：Controllerの参照 ---
    private FighterController controller;
    // ------------------------------------

    // --- 新規追加部分：ガードゲージ関連 ---
    public float maxGuardGauge = 40f;   // これが0になるとガードクラッシュ
    public float currentGuardGauge;
    // ------------------------------------

    // --- 新規追加部分：GameManagerへの参照 ---
    private GameManager gameManager;
    // ------------------------------------

    // --- 新規追加部分：ヒットエフェクト・SE用 ---
    public AudioClip hitSound;       // インスペクターで設定可能
    public AudioClip guardSound;     // インスペクターで設定可能
    public GameObject hitParticle;   // インスペクターで設定可能
    // ----------------------------------------

    private Renderer bodyRenderer;
    private Color originalColor;

    public bool IsDead
    {
        get { return currentHP <= 0; }
    }

    void Start()
    {
        currentHP = maxHP;
        currentGuardGauge = maxGuardGauge; // ガードゲージ初期化

        // --- 新規追加部分：ControllerとGameManagerの取得 ---
        controller = GetComponent<FighterController>();
        gameManager = Object.FindFirstObjectByType<GameManager>(); // Unityのバージョン問わず取得する一般的な方法
        if (gameManager == null)
        {
            gameManager = Object.FindAnyObjectByType<GameManager>();
        }
        // ------------------------------------

        bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer != null)
        {
            originalColor = bodyRenderer.material.color;
        }
    }

    // --- 新規追加部分：Updateで自動回復 ---
    void Update()
    {
        // ガード中でない場合、徐々にガードゲージが回復する
        if (currentGuardGauge < maxGuardGauge && (controller == null || !controller.isDefending))
        {
            currentGuardGauge += Time.deltaTime * 15f; // 1秒間に15回復
            if (currentGuardGauge > maxGuardGauge) currentGuardGauge = maxGuardGauge;
        }
    }
    // ------------------------------------

    // --- 修正部分：ガード不能攻撃の対応と削りダメージ・ガードクラッシュの実装 ---
    public void TakeDamage(int damage, Vector3 attackerPosition, bool isUnblockable = false)
    {
        if (IsDead) return;

        bool isGuarded = false;

        // ガードクラッシュ中でなければガード判定を行う
        bool canGuard = (controller != null && controller.isDefending && !controller.isGuardCrushed);

        if (canGuard && !isUnblockable)
        {
            isGuarded = true;
            
            // 削りダメージ（Chip Damage）：通常の20%のダメージはガードしても食らう（最低1ダメ）
            int chipDamage = Mathf.Max(1, (int)(damage * 0.2f));
            currentHP -= chipDamage;

            // ガードゲージも削られる
            currentGuardGauge -= damage;

            // ゲージが0以下になったらガードクラッシュ（ガードが割れる）
            if (currentGuardGauge <= 0)
            {
                currentGuardGauge = 0; // ペナルティで0から回復待ち
                isGuarded = false; // ガード判定消失
                
                Debug.Log(gameObject.name + " GUARD CRUSH!");
                
                if (controller != null) controller.ApplyGuardCrush(2.0f); // 2秒間大きな無防備状態に
                
                // ガードが割れた瞬間の攻撃はフルヒットする仕様にする
                currentHP -= (damage - chipDamage); // 残りのダメージも食らう
            }
            else
            {
                Debug.Log(gameObject.name + " HP: " + currentHP + " (Guarded / GuardGauge: " + currentGuardGauge + ")");
            }
        }
        else
        {
            // 防御していない or ガード不能攻撃 or ガードクラッシュ中の直撃
            currentHP -= damage;
            Debug.Log(gameObject.name + " HP: " + currentHP + (isUnblockable ? " (Unblockable Hit!)" : ""));
        }

        if (currentHP < 0)
            currentHP = 0;

        // --- 新規追加部分：ガード時のノックバック軽減とヒットストップ ---
        ApplyKnockback(attackerPosition, isGuarded);
        StartCoroutine(DamageFlash(isGuarded));

        // エフェクトの発生位置（自分と攻撃者の真ん中、少し上）
        Vector3 effectPos = (transform.position + attackerPosition) / 2f;
        effectPos.y += 1.0f;
        SpawnHitEffect(effectPos, isGuarded);

        if (gameManager != null)
        {
            if (!isGuarded)
                gameManager.TriggerHitStop(0.1f, 0.05f); // ヒット時は時間停止を強めに
            else
                gameManager.TriggerHitStop(0.05f, 0.5f); // ガード時は軽く停止
        }
        // --------------------------------------------
    }

    // --- 修正部分：引数追加と物理演算（Rigidbody）によるノックバック ---
    void ApplyKnockback(Vector3 attackerPosition, bool isGuarded = false)
    {
        float dir = transform.position.x > attackerPosition.x ? 1f : -1f;
        float actualKnockback = isGuarded ? knockbackPower * 0.5f : knockbackPower;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 物理演算で吹き飛ばす。少しだけ上方向（Y）にも力を加えることで「浮き」を表現
            Vector3 force = new Vector3(dir * actualKnockback * 5f, actualKnockback * 2f, 0f);
            
            // これまでの横方向の速度をリセットして連続ヒット時の吹飛びすぎを防止する
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.AddForce(force, ForceMode.Impulse);
        }
        else
        {
            // Rigidbodyがない場合のフォールバック
            transform.Translate(Vector3.right * dir * actualKnockback, Space.World);
        }
    }
    // --------------------------------------------

    // --- 新規追加部分：引数追加とガード時エフェクト ---
    IEnumerator DamageFlash(bool isGuarded = false)
    {
        if (bodyRenderer == null) yield break;

        bodyRenderer.material.color = isGuarded ? Color.cyan : Color.red; // ガード時は青色（シアン）
        yield return new WaitForSeconds(0.1f);
        bodyRenderer.material.color = originalColor;
    }
    // --------------------------------------------

    // --- 新規追加部分：ヒットエフェクトとSEの発生処理 ---
    void SpawnHitEffect(Vector3 pos, bool isGuarded)
    {
        // 1. 音の再生
        AudioClip clipToPlay = isGuarded ? guardSound : hitSound;
        if (clipToPlay != null)
        {
            AudioSource.PlayClipAtPoint(clipToPlay, pos);
        }

        // 2. 視覚エフェクトの再生
        if (hitParticle != null)
        {
            Instantiate(hitParticle, pos, Quaternion.identity);
        }
        else
        {
            // パーティクルが設定されていない場合でも、簡易的な「火花」をコードだけで生成する
            StartCoroutine(SimpleHitVisualRoutine(pos, isGuarded));
        }
    }

    IEnumerator SimpleHitVisualRoutine(Vector3 startPos, bool isGuarded)
    {
        GameObject effectObj = new GameObject("SimpleHitEffect");
        effectObj.transform.position = startPos;
        
        SpriteRenderer sr = effectObj.AddComponent<SpriteRenderer>();
        // 1x1の真っ白な画像テクスチャを内部で生成
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        
        // ガード時は水色、ヒット時はオレンジ＋黄色の激しい色
        sr.color = isGuarded ? new Color(0.2f, 0.8f, 1f, 0.8f) : new Color(1f, 0.8f, 0.1f, 0.9f);

        // 打撃感を出すためにランダムに回転させる
        effectObj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (effectObj == null) break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // 一瞬でバガッと膨張させる
            float scale = Mathf.Lerp(1f, 5f, t);
            effectObj.transform.localScale = new Vector3(scale, scale, 1f);
            
            // すぐに透明にフェードアウト
            Color c = sr.color;
            c.a = Mathf.Lerp(0.9f, 0f, t * 1.5f);
            sr.color = c;

            yield return null;
        }

        Destroy(tex);
        Destroy(effectObj);
    }
    // ------------------------------------------------
}