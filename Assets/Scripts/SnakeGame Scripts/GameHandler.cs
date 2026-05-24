//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

//public class GameHandler : MonoBehaviour
//{
//    public static GameHandler instance;

//    private static int score;

//    [SerializeField] private Snake snake;
//    [SerializeField] private Text metalAppleWarningText;
//    public Canvas mainCanvas;

//    public LevelGrid levelGrid { get; private set; }

//    private void Awake()
//    {
//        instance = this;
//    }

//    public Canvas MainCanvas => mainCanvas;

//    void Start()
//    {
//        levelGrid = new LevelGrid(16, 30);

//        snake.Setup(levelGrid);
//        levelGrid.Setup(snake);
//    }

//    private void Update()
//    {
//        if (levelGrid != null)
//            levelGrid.Update();
//    }


//    public static int GetScore()
//    {
//        return score;
//    }

//    public static void AddScore(int amount)
//    {
//        score += amount;
//    }

//    public static void SnakeDied()
//    {
//        //GameOverWindow.ShowStatic();
//    }

//    public static void ShowMetalWarning(string msg)
//    {
//        instance.metalAppleWarningText.text = msg;
//        instance.metalAppleWarningText.gameObject.SetActive(true);
//    }

//    public static void HideMetalWarning()
//    {
//        instance.metalAppleWarningText.gameObject.SetActive(false);
//    }
//}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameHandler : MonoBehaviour
{
    public static GameHandler instance;

    // ─────────────────────────────────────────────
    //  SCORE SETTINGS  (edit these in the Inspector)
    // ─────────────────────────────────────────────
    [Header("Apple Score Values")]
    [Tooltip("Red apple — always present, main score source.")]
    public int redAppleScore = 100;

    [Tooltip("Blue apple — rare, grants wall-pass for 5 s.")]
    public int blueAppleScore = 150;

    [Tooltip("Golden apple — rare, 3-second timer before it vanishes.")]
    public int goldenAppleScore = 250;

    [Tooltip("Purple apple — slows the snake for 5 s.")]
    public int purpleAppleScore = 50;

    [Tooltip("Iron apple — tied to eagle attack system.")]
    public int ironAppleScore = 75;

    // ─────────────────────────────────────────────
    //  IRON APPLE SPEED MULTIPLIER
    // ─────────────────────────────────────────────
    [Header("Iron Apple — Speed Multiplier Thresholds")]
    [Tooltip("Score where Tier 1 ends and Tier 2 begins.")]
    public int ironSpeedTier1Cap = 2000;

    [Tooltip("Score where Tier 2 ends and Tier 3 begins.")]
    public int ironSpeedTier2Cap = 5000;

    [Tooltip("Score points between each speed increase step.")]
    public int ironSpeedStepSize = 500;

    [Tooltip("Speed increase per step from 0 to Tier1Cap. 0.08 = 8%")]
    [Range(0f, 1f)] public float ironSpeedIncreaseTier1 = 0.08f;

    [Tooltip("Speed increase per step from Tier1Cap to Tier2Cap. 0.05 = 5%")]
    [Range(0f, 1f)] public float ironSpeedIncreaseTier2 = 0.05f;

    [Tooltip("Speed increase per step above Tier2Cap. 0.03 = 3%")]
    [Range(0f, 1f)] public float ironSpeedIncreaseTier3 = 0.03f;

    // ─────────────────────────────────────────────
    //  REFERENCES
    // ─────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Snake snake;
    [SerializeField] private Text metalAppleWarningText;
    public Canvas mainCanvas;

    public LevelGrid levelGrid { get; private set; }

    private static int score;
    public Canvas MainCanvas => mainCanvas;

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    private void Awake()
    {
        instance = this;
        score = 0; // resets properly every session
    }

    void Start()
    {
        levelGrid = new LevelGrid(16, 30);
        snake.Setup(levelGrid);
        levelGrid.Setup(snake);
    }

    private void Update()
    {
        if (levelGrid != null)
            levelGrid.Update();
    }

    // ─────────────────────────────────────────────
    //  SCORE API
    // ─────────────────────────────────────────────
    public static int GetScore() => score;

    public static void AddScore(int amount)
    {
        score += amount;
    }

    // ─────────────────────────────────────────────
    //  IRON APPLE SPEED MULTIPLIER
    // ─────────────────────────────────────────────
    public static float GetIronAppleSpeedMultiplier()
    {
        if (instance == null) return 1f;

        float multiplier = 1f;
        int pts = score;
        int step = instance.ironSpeedStepSize;

        // Tier 1: 0 → Tier1Cap
        int tier1Steps = Mathf.Min(pts, instance.ironSpeedTier1Cap) / step;
        multiplier *= Mathf.Pow(1f + instance.ironSpeedIncreaseTier1, tier1Steps);
        pts = Mathf.Max(0, pts - instance.ironSpeedTier1Cap);

        // Tier 2: Tier1Cap → Tier2Cap
        int tier2Range = instance.ironSpeedTier2Cap - instance.ironSpeedTier1Cap;
        int tier2Steps = Mathf.Min(pts, tier2Range) / step;
        multiplier *= Mathf.Pow(1f + instance.ironSpeedIncreaseTier2, tier2Steps);
        pts = Mathf.Max(0, pts - tier2Range);

        // Tier 3: above Tier2Cap
        int tier3Steps = pts / step;
        multiplier *= Mathf.Pow(1f + instance.ironSpeedIncreaseTier3, tier3Steps);

        return multiplier;
    }

    // ─────────────────────────────────────────────
    //  WARNING UI
    // ─────────────────────────────────────────────
    public static void ShowMetalWarning(string msg)
    {
        if (instance?.metalAppleWarningText == null) return;
        instance.metalAppleWarningText.text = msg;
        instance.metalAppleWarningText.gameObject.SetActive(true);
    }

    public static void HideMetalWarning()
    {
        instance?.metalAppleWarningText?.gameObject.SetActive(false);
    }


    // ✅ Add this new method to GameHandler
    public void TriggerGameOver()
    {
       // PlayerXPManager.SaveScore(score); // ✅ ADD THIS LINE
        PlayerXPManager.SaveScore(score, PlayerXPManager.GameType.Ball);
        Debug.Log("✅ Snake score saved: " + score);
    }
}