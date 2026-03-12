using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static bool CanControl = false;

    public FighterStats playerStats;
    public FighterStats enemyStats;

    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI enemyHPText;
    public TextMeshProUGUI resultText;

    private bool isGameOver = false;

    void Start()
    {
        CanControl = false;
        StartCoroutine(StartFightSequence());
    }

    IEnumerator StartFightSequence()
    {
        if (resultText != null)
            resultText.text = "Ready";

        yield return new WaitForSeconds(1.0f);

        if (resultText != null)
            resultText.text = "Fight!";

        yield return new WaitForSeconds(1.0f);

        if (resultText != null)
            resultText.text = "";

        CanControl = true;
    }

    void Update()
    {
        if (playerStats != null && playerHPText != null)
        {
            playerHPText.text = "Player HP: " + playerStats.currentHP;
        }

        if (enemyStats != null && enemyHPText != null)
        {
            enemyHPText.text = "Enemy HP: " + enemyStats.currentHP;
        }

        if (!isGameOver)
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

        if (isGameOver && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}