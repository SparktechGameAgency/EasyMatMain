using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerXPManager : MonoBehaviour
{
    public static PlayerXPManager Instance;

    public enum GameType { Chix, Ball, Snake, Block }

    private const string KEY_BEST_CHIX = "BestScore_Chix";
    private const string KEY_BEST_BALL = "BestScore_Ball";
    private const string KEY_BEST_SNAKE = "BestScore_Snake";
    private const string KEY_BEST_BLOCK = "BestScore_Block";
    private const string XP_STAT_NAME = "XP";

    private List<(int score, GameType game)> pendingScores = new List<(int, GameType)>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Debug.Log("✅ PlayerXPManager instance created at root.");

            if (pendingScores.Count > 0 && PlayFabClientAPI.IsClientLoggedIn())
                FlushPending();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void FlushPending()
    {
        Debug.Log("✅ Flushing " + pendingScores.Count + " pending score(s)...");
        List<(int, GameType)> toFlush = new List<(int, GameType)>(pendingScores);
        pendingScores.Clear();
        foreach (var (s, g) in toFlush)
            StartCoroutine(ProcessScore(s, g));
    }

    // ─── Public call site ───────────────────────────────────────────────────
    // Each game passes its own GameType so bests are tracked separately.
    //
    // ChixGameManager  → PlayerXPManager.SaveScore(score, PlayerXPManager.GameType.Chix);
    // GameManager      → PlayerXPManager.SaveScore(score, PlayerXPManager.GameType.Ball);
    // GameHandler      → PlayerXPManager.SaveScore(score, PlayerXPManager.GameType.Snake);
    // BlockManager     → PlayerXPManager.SaveScore(score, PlayerXPManager.GameType.Block);
    // ─────────────────────────────────────────────────────────────────────────
    public static void SaveScore(int gameScore, GameType game)
    {
        Debug.Log($"💾 SaveScore called: {gameScore} [{game}]");

        if (Instance == null)
        {
            Debug.LogWarning("⚠️ Creating PlayerXPManager on the fly...");
            GameObject go = new GameObject("PlayerXPManager");
            go.AddComponent<PlayerXPManager>();
        }

        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogWarning($"⏳ Queuing score: {gameScore} [{game}]");
            Instance.pendingScores.Add((gameScore, game));
            return;
        }

        Instance.StartCoroutine(Instance.ProcessScore(gameScore, game));
    }

    // ─── Core logic ─────────────────────────────────────────────────────────
    IEnumerator ProcessScore(int newScore, GameType game)
    {
        if (newScore <= 0)
        {
            Debug.LogWarning("⚠️ Score is 0 — skipping.");
            yield break;
        }

        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogError("❌ Not logged in!");
            yield break;
        }

        // 1. Check personal best stored locally
        string prefKey = GetPrefKey(game);
        int oldBest = PlayerPrefs.GetInt(prefKey, 0);

        if (newScore <= oldBest)
        {
            Debug.Log($"[XP] {game}: {newScore} didn't beat best of {oldBest}. XP unchanged.");
            yield break;
        }

        // 2. Calculate how much XP to add (only the improvement)
        int diff = newScore - oldBest;

        // 3. Save new personal best locally
        PlayerPrefs.SetInt(prefKey, newScore);
        PlayerPrefs.Save();
        Debug.Log($"[XP] {game}: New best {newScore} (was {oldBest}). Adding +{diff} XP.");

        // 4. Read current total XP from PlayFab statistics, add only the diff
        var readTask = new StatResult();
        PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string> { XP_STAT_NAME }
        },
        result =>
        {
            readTask.Done = true;
            readTask.Success = true;
            var stat = result.Statistics.Find(s => s.StatisticName == XP_STAT_NAME);
            readTask.CurrentXP = stat != null ? stat.Value : 0;
        },
        error =>
        {
            readTask.Done = true;
            readTask.Success = false;
            Debug.LogError("❌ Read failed: " + error.GenerateErrorReport());
        });

        yield return new WaitUntil(() => readTask.Done);

        if (!readTask.Success)
            yield break;

        int newXP = readTask.CurrentXP + diff;
        Debug.Log($"🔢 {readTask.CurrentXP} + {diff} = {newXP}");

        // 5. Write updated total XP
        var writeTask = new StatResult();
        PlayFabClientAPI.UpdatePlayerStatistics(new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = XP_STAT_NAME, Value = newXP }
            }
        },
        result =>
        {
            writeTask.Done = true;
            writeTask.Success = true;
        },
        error =>
        {
            writeTask.Done = true;
            writeTask.Success = false;
            Debug.LogError("❌ Save failed: " + error.GenerateErrorReport());
        });

        yield return new WaitUntil(() => writeTask.Done);

        if (writeTask.Success)
            Debug.Log($"✅ XP saved! +{diff} → Total: {newXP}");
    }

    private class StatResult
    {
        public bool Done;
        public bool Success;
        public int CurrentXP;
    }

    // ─── Helpers ────────────────────────────────────────────────────────────
    private static string GetPrefKey(GameType game)
    {
        switch (game)
        {
            case GameType.Chix: return KEY_BEST_CHIX;
            case GameType.Ball: return KEY_BEST_BALL;
            case GameType.Snake: return KEY_BEST_SNAKE;
            case GameType.Block: return KEY_BEST_BLOCK;
            default: return "BestScore_Unknown";
        }
    }
}
