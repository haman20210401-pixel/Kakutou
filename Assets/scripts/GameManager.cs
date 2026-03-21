using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // --- 新規追加部分：UI系の操作（Slider等）に必要 ---
using TMPro;

public class GameManager : MonoBehaviour
{
    public static bool CanControl = false;

    public FighterStats playerStats;
    public FighterStats enemyStats;

    public FighterController enemyController;

    // --- 変更部分：体力ゲージをImage（Fill）に変更して見やすくする ---
    public Image playerHPBar;
    public Image enemyHPBar;
    // ----------------------------------------

    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI enemyHPText;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI titleText;

    private bool isGameOver = false;
    private bool isModeSelected = false;

    void Start()
    {
        CanControl = false;
        isGameOver = false;
        isModeSelected = false;

        // --- 修正部分：Inspectorから割り当てられていない場合は自動取得 ---
        if (enemyController == null)
        {
            GameObject enemyObj = GameObject.FindWithTag("Enemy");
            if (enemyObj != null)
                enemyController = enemyObj.GetComponent<FighterController>();
        }
        // --------------------------------------------------------

        if (resultText != null)
            resultText.text = "";

        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = "Mini Fighting Game\nPress 1 : 1P vs CPU\nPress 2 : 1P vs 2P";
        }

        // --- 変更部分：ImageのfillAmountの初期化 ---
        if (playerStats != null && playerHPBar != null)
        {
            playerHPBar.fillAmount = 1f;
        }
        if (enemyStats != null && enemyHPBar != null)
        {
            enemyHPBar.fillAmount = 1f;
        }
        // ----------------------------------------------------
    }

    void Update()
    {
        UpdateHPUI();

        if (!isModeSelected)
        {
            SelectMode();
            return;
        }

        if (!isGameOver)
        {
            CheckGameOver();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
    }

    void UpdateHPUI()
    {
        if (playerStats != null && playerHPText != null)
        {
            playerHPText.text = "Player HP: " + playerStats.currentHP;
            
            // --- 変更部分：ImageのfillAmountをLerpで滑らかに同期（0.0〜1.0） ---
            if (playerHPBar != null)
            {
                float targetFill = (float)playerStats.currentHP / playerStats.maxHP;
                playerHPBar.fillAmount = Mathf.Lerp(playerHPBar.fillAmount, targetFill, Time.unscaledDeltaTime * 10f);
            }
            // --------------------------------------------
        }

        if (enemyStats != null && enemyHPText != null)
        {
            enemyHPText.text = "Enemy HP: " + enemyStats.currentHP;
            
            // --- 変更部分：ImageのfillAmountをLerpで滑らかに同期（0.0〜1.0） ---
            if (enemyHPBar != null)
            {
                float targetFill = (float)enemyStats.currentHP / enemyStats.maxHP;
                enemyHPBar.fillAmount = Mathf.Lerp(enemyHPBar.fillAmount, targetFill, Time.unscaledDeltaTime * 10f);
            }
            // --------------------------------------------
        }
    }

    void SelectMode()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            isModeSelected = true;

            if (enemyController != null)
                enemyController.isCPU = true;

            StartCoroutine(StartFightSequence("1P vs CPU"));
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            isModeSelected = true;

            if (enemyController != null)
                enemyController.isCPU = false;

            StartCoroutine(StartFightSequence("1P vs 2P"));
        }
    }

    IEnumerator StartFightSequence(string modeName)
    {
        if (titleText != null)
        {
            titleText.text = modeName;
        }

        yield return new WaitForSeconds(0.6f); // テンポアップ: 0.8f -> 0.6f

        if (titleText != null)
        {
            titleText.text = "Ready";
        }

        yield return new WaitForSeconds(0.6f); // テンポアップ: 1.0f -> 0.6f

        if (titleText != null)
        {
            titleText.text = "Fight!";
        }

        yield return new WaitForSeconds(0.6f); // テンポアップ: 1.0f -> 0.6f

        if (titleText != null)
        {
            titleText.text = "";
            titleText.gameObject.SetActive(false);
        }

        CanControl = true;
    }

    void CheckGameOver()
    {
        if (playerStats != null && playerStats.IsDead)
        {
            isGameOver = true;
            CanControl = false;

            if (resultText != null)
                resultText.text = "Enemy Wins!\nPress R to Restart";

            Debug.Log("Enemy Wins!");

            // --- 新規追加部分：敗北と勝利の演出呼び出し ---
            FighterController playerCtrl = playerStats.GetComponent<FighterController>();
            if (playerCtrl != null) playerCtrl.PlayDieAnimation();

            if (enemyController != null) enemyController.PlayVictoryAnimation();
            // --------------------------------------------
        }
        else if (enemyStats != null && enemyStats.IsDead)
        {
            isGameOver = true;
            CanControl = false;

            if (resultText != null)
                resultText.text = "Player Wins!\nPress R to Restart";

            Debug.Log("Player Wins!");

            // --- 新規追加部分：敗北と勝利の演出呼び出し ---
            if (enemyController != null) enemyController.PlayDieAnimation();

            FighterController playerCtrl = playerStats.GetComponent<FighterController>();
            if (playerCtrl != null) playerCtrl.PlayVictoryAnimation();
            // --------------------------------------------
        }
    }

    // --- 新規追加部分：ヒットストップ処理（時間の一時停止演出） ---
    private float defaultTimeScale = 1.0f;
    private Coroutine hitStopCoroutine;

    // slowTimeScale: 0に近いほど遅くなる
    public void TriggerHitStop(float duration, float slowTimeScale = 0.05f)
    {
        if (hitStopCoroutine != null)
            StopCoroutine(hitStopCoroutine);

        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration, slowTimeScale));
    }

    private IEnumerator HitStopRoutine(float duration, float slowTimeScale)
    {
        Time.timeScale = slowTimeScale;
        // timeScaleが0に近い状態でも現実時間(Realtime)で待機する
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = defaultTimeScale;
    }
    // ----------------------------------------------------
}