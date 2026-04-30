using UnityEngine;
using TMPro;

public class GameManagerBlock : MonoBehaviour
{
    public static GameManagerBlock Instance; // ✅ Fix 1: was GameManager, now GameManagerBlock
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public TowerManager towerManager;
    public BlockSpawner spawner;

    private int score = 0;
    private bool isAsteroidEvent = false;
    private float timeFreezeDuration = 3f;

    void Awake() { Instance = this; }

    public void AddScore(int amount = 1)
    {
        score += amount;
        scoreText.text = score.ToString();

        if (score % 10 == 0) TriggerAsteroidEvent();
    }

    public void GameOver(string reason)
    {
        Debug.Log("Game Over: " + reason);
        Time.timeScale = 0;
        gameOverPanel.SetActive(true);
    }

    void TriggerAsteroidEvent()
    {
        isAsteroidEvent = true;
        Debug.Log("ASTEROID EVENT! Place blocks fast!");
    }

    public void UseTimeFreeze()
    {
        if (!isAsteroidEvent) return;
        Time.timeScale = 0.2f;
        Invoke(nameof(EndTimeFreeze), timeFreezeDuration); // ✅ Fix 2: was timeFreezeeDuration (extra e)
    }

    void EndTimeFreeze()
    {
        Time.timeScale = 1f;
    }
}