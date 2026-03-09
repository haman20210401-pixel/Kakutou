using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public FighterStats playerStats;
    public FighterStats enemyStats;

    public TextMeshProUGUI playerHPText;
    public TextMeshProUGUI enemyHPText;
    public TextMeshProUGUI resultText;

    private bool isGameOver = false;

    void Start()
    {
        if (resultText != null)
            resultText.text = "";
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

        if (isGameOver) return;

        if (playerStats != null && playerStats.IsDead)
        {
            isGameOver = true;

            if (resultText != null)
                resultText.text = "Enemy Wins!";

            Debug.Log("Enemy Wins!");
        }
        else if (enemyStats != null && enemyStats.IsDead)
        {
            isGameOver = true;

            if (resultText != null)
                resultText.text = "Player Wins!";

            Debug.Log("Player Wins!");
        }
    }
}