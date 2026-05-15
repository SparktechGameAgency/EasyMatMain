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

    public static PlayerXPManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("PlayerXPManager");
        Instance = go.AddComponent<PlayerXPManager>();
        return Instance;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Debug.Log("? PlayerXPManager created.");
            StartCoroutine(InitFirebaseCoroutine());
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // ? Coroutine keeps retrying until Firebase is ready
    IEnumerator InitFirebaseCoroutine()
    {
        Debug.Log("? Waiting for Firebase...");

        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();

        // Wait until task is done
        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        if (dependencyTask.Result == DependencyStatus.Available)
        {
            db = FirebaseFirestore.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
            firebaseReady = true;
            Debug.Log("? Firebase ready!");

            // ? Flush any scores that came in before Firebase was ready
            if (pendingScores.Count > 0)
            {
                Debug.Log("?? Flushing " + pendingScores.Count + " queued score(s)...");
                foreach (int pending in pendingScores)
                {
                    AddScoreToFirestore(pending);
                }
                pendingScores.Clear();
            }
        }
        else
        {
            Debug.LogError("? Firebase failed: " + dependencyTask.Result);
        }
    }

    public static void SaveScore(int gameScore)
    {
        Debug.Log("?? SaveScore called: " + gameScore);

        if (Instance == null)
        {
            Debug.LogWarning("?? Instance null — creating.");
            GetOrCreate();
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

        // ? Queue and wait — coroutine will flush it
        if (!firebaseReady)
        {
            Debug.LogWarning("? Firebase not ready — queuing: " + gameScore);
            pendingScores.Add(gameScore);
            return;
        }

        if (auth.CurrentUser == null)
        {
            Debug.LogError("? Not logged in!");
            return;
        }

        string uid = auth.CurrentUser.UserId;
        Debug.Log("?? Saving for UID: " + uid);

        DocumentReference userRef = db.Collection("users").Document(uid);

        userRef.GetSnapshotAsync().ContinueWith(task =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("? Read failed: " + task.Exception);
                    return;
                }

                long currentXP = 0;
                if (task.Result.ContainsField("xp"))
                    task.Result.TryGetValue("xp", out currentXP);

                long newXP = currentXP + gameScore;
                Debug.Log("?? " + currentXP + " + " + gameScore + " = " + newXP);

                var data = new Dictionary<string, object>
                {
                    { "xp", newXP }
                };

                userRef.SetAsync(data, SetOptions.MergeAll).ContinueWith(updateTask =>
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        if (updateTask.IsFaulted)
                            Debug.LogError("? Save failed: " + updateTask.Exception);
                        else
                            Debug.Log("? XP saved! +" + gameScore + " ? Total: " + newXP);
                    });
                });
            });
        });
    }
}