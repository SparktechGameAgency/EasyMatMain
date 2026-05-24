using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class PlayerXPManager : MonoBehaviour
{
    public static PlayerXPManager Instance;

    public enum GameType { Chix, Ball, Snake, Block }

    private const string KEY_BEST_CHIX = "BestScore_Chix";
    private const string KEY_BEST_BALL = "BestScore_Ball";
    private const string KEY_BEST_SNAKE = "BestScore_Snake";
    private const string KEY_BEST_BLOCK = "BestScore_Block";

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private bool firebaseReady = false;

    private List<(int score, GameType game)> pendingScores = new List<(int, GameType)>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Debug.Log("? PlayerXPManager instance created at root.");
            StartCoroutine(InitFirebase());
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator InitFirebase()
    {
        Debug.Log("?? Initializing Firebase...");

        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result == DependencyStatus.Available)
        {
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            firebaseReady = true;
            Debug.Log("? Firebase ready in PlayerXPManager!");

            if (pendingScores.Count > 0)
            {
                Debug.Log("? Flushing " + pendingScores.Count + " pending score(s)...");
                List<(int, GameType)> toFlush = new List<(int, GameType)>(pendingScores);
                pendingScores.Clear();
                foreach (var (s, g) in toFlush)
                    StartCoroutine(ProcessScore(s, g));
            }
        }
        else
        {
            Debug.LogError("? Firebase failed: " + task.Result);
        }
    }

    // ??? Public call site ????????????????????????????????????????????????????
    // Each game passes its own GameType so bests are tracked separately.
    //
    // ChixGameManager  ? PlayerXPManager.SaveScore(score, PlayerXPManager.GameType.Chix);
    // GameManager      ? PlayerXPManager.SaveScore(score, PlayerXPManager.GameType.Ball);
    // GameHandler      ? PlayerXPManager.SaveScore(score, PlayerXPManager.GameType.Snake);
    // BlockManager     ? PlayerXPManager.SaveScore(score, PlayerXPManager.GameType.Block);
    // ?????????????????????????????????????????????????????????????????????????
    public static void SaveScore(int gameScore, GameType game)
    {
        Debug.Log($"?? SaveScore called: {gameScore} [{game}]");

        if (Instance == null)
        {
            Debug.LogWarning("?? Creating PlayerXPManager on the fly...");
            GameObject go = new GameObject("PlayerXPManager");
            go.AddComponent<PlayerXPManager>();
        }

        if (!Instance.firebaseReady)
        {
            Debug.LogWarning($"? Queuing score: {gameScore} [{game}]");
            Instance.pendingScores.Add((gameScore, game));
            return;
        }

        Instance.StartCoroutine(Instance.ProcessScore(gameScore, game));
    }

    // ??? Core logic ??????????????????????????????????????????????????????????
    IEnumerator ProcessScore(int newScore, GameType game)
    {
        if (newScore <= 0)
        {
            Debug.LogWarning("?? Score is 0 — skipping.");
            yield break;
        }

        if (auth.CurrentUser == null)
        {
            Debug.LogError("? Not logged in!");
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

        // 4. Read current total XP from Firestore, add only the diff
        string uid = auth.CurrentUser.UserId;
        DocumentReference userRef = db.Collection("users").Document(uid);

        var readTask = userRef.GetSnapshotAsync();
        yield return new WaitUntil(() => readTask.IsCompleted);

        if (readTask.IsFaulted)
        {
            Debug.LogError("? Read failed: " + readTask.Exception);
            yield break;
        }

        if (!readTask.Result.Exists)
        {
            Debug.LogError("? User document not found for UID: " + uid);
            yield break;
        }

        long currentXP = 0;
        if (readTask.Result.ContainsField("xp"))
            readTask.Result.TryGetValue("xp", out currentXP);

        long newXP = currentXP + diff;
        Debug.Log($"?? {currentXP} + {diff} = {newXP}");

        // 5. Write updated total XP
        var data = new Dictionary<string, object> { { "xp", newXP } };
        var writeTask = userRef.SetAsync(data, SetOptions.MergeAll);
        yield return new WaitUntil(() => writeTask.IsCompleted);

        if (writeTask.IsFaulted)
            Debug.LogError("? Save failed: " + writeTask.Exception);
        else
            Debug.Log($"? XP saved! +{diff} ? Total: {newXP}");
    }

    // ??? Helpers ?????????????????????????????????????????????????????????????
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