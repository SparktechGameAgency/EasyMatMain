using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class PlayerXPManager : MonoBehaviour
{
    public static PlayerXPManager Instance;

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private bool firebaseReady = false;
    private List<int> pendingScores = new List<int>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // ? Detach from any parent to make it root
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
        Debug.Log("? Initializing Firebase...");

        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Result == DependencyStatus.Available)
        {
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            firebaseReady = true;
            Debug.Log("? Firebase ready in PlayerXPManager!");

            // ? Flush pending scores
            if (pendingScores.Count > 0)
            {
                Debug.Log("?? Flushing " + pendingScores.Count + " pending score(s)...");
                List<int> toFlush = new List<int>(pendingScores);
                pendingScores.Clear();
                foreach (int s in toFlush)
                    AddScoreToFirestore(s);
            }
        }
        else
        {
            Debug.LogError("? Firebase failed: " + task.Result);
        }
    }

    public static void SaveScore(int gameScore)
    {
        Debug.Log("?? SaveScore called: " + gameScore);

        if (Instance == null)
        {
            Debug.LogWarning("?? Creating PlayerXPManager on the fly...");
            GameObject go = new GameObject("PlayerXPManager");
            go.AddComponent<PlayerXPManager>();
        }

        Instance.AddScoreToFirestore(gameScore);
    }

    public void AddScoreToFirestore(int gameScore)
    {
        if (gameScore <= 0)
        {
            Debug.LogWarning("?? Score is 0 — skipping.");
            return;
        }

        if (!firebaseReady)
        {
            Debug.LogWarning("? Queuing score: " + gameScore);
            pendingScores.Add(gameScore);
            return;
        }

        if (auth.CurrentUser == null)
        {
            Debug.LogError("? Not logged in!");
            return;
        }

        string uid = auth.CurrentUser.UserId;
        Debug.Log("?? UID: " + uid);

        // ? Use coroutine instead of ContinueWith
        StartCoroutine(SaveScoreCoroutine(uid, gameScore));
    }

    IEnumerator SaveScoreCoroutine(string uid, int gameScore)
    {
        Debug.Log("?? Reading current XP...");

        DocumentReference userRef = db.Collection("users").Document(uid);

        // ?? Read current XP ???????????????????????????????????????
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

        long newXP = currentXP + gameScore;
        Debug.Log("?? " + currentXP + " + " + gameScore + " = " + newXP);

        // ?? Write new XP ??????????????????????????????????????????
        var data = new Dictionary<string, object>
    {
        { "xp", newXP }
    };

        var writeTask = userRef.SetAsync(data, SetOptions.MergeAll);
        yield return new WaitUntil(() => writeTask.IsCompleted);

        if (writeTask.IsFaulted)
            Debug.LogError("? Save failed: " + writeTask.Exception);
        else
            Debug.Log("? XP saved! +" + gameScore + " ? Total: " + newXP);
    }
}