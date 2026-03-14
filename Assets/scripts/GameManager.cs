using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static bool CanControl = false;

    public FighterStats playerStats;
    public FighterStats enemyStats;

    public FighterController enemyController;

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

        if (resultText != null)
            resultText.text = "";

        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.text = "Mini Fighting Game\nPress 1 : 1P vs CPU\nPress 2 : 1P vs 2P";
        }
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
        }

        if (enemyStats != null && enemyHPText != null)
        {
            enemyHPText.text = "Enemy HP: " + enemyStats.currentHP;
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

        yield return new WaitForSeconds(0.8f);

        if (titleText != null)
        {
            titleText.text = "Ready";
        }

        yield return new WaitForSeconds(1.0f);

        if (titleText != null)
        {
            titleText.text = "Fight!";
        }

        yield return new WaitForSeconds(1.0f);

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
        }
        else if (enemyStats != null && enemyStats.IsDead)
        {
            isGameOver = true;
            CanControl = false;

            if (resultText != null)
                resultText.text = "Player Wins!\nPress R to Restart";

            Debug.Log("Player Wins!");
        }
    }
}