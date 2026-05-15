using Firebase.Firestore;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [Header("── LEADERBOARD UI ──")]
    public Transform listContainer;
    public GameObject playerRowPrefab;

    private FirebaseFirestore db;
    private List<GameObject> spawnedRows = new List<GameObject>();

    void OnEnable()
    {
        StartCoroutine(LoadAfterDelay());
    }

    IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        db = FirebaseFirestore.DefaultInstance;
        LoadLeaderboard();
    }

    public void LoadLeaderboard()
    {
        if (db == null) db = FirebaseFirestore.DefaultInstance;

        var auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ Not logged in — cannot load leaderboard.");
            return;
        }

        Debug.Log("✅ Logged in as: " + auth.CurrentUser.Email);

        foreach (var row in spawnedRows)
            Destroy(row);
        spawnedRows.Clear();

        Debug.Log("Loading leaderboard...");

        db.Collection("users")
          .OrderByDescending("xp")
          .GetSnapshotAsync()
          .ContinueWith(task =>
          {
              UnityMainThreadDispatcher.Instance().Enqueue(() =>
              {
                  if (task.IsFaulted)
                  {
                      Debug.LogError("❌ Leaderboard failed: " + task.Exception);
                      return;
                  }

                  List<DocumentSnapshot> users = task.Result.Documents
                      .OrderByDescending(doc =>
                      {
                          doc.TryGetValue("xp", out long xp);
                          return xp;
                      })
                      .ToList();

                  Debug.Log("✅ Users loaded: " + users.Count);

                  for (int i = 0; i < users.Count; i++)
                  {
                      SpawnRow(users[i], i + 1);
                  }
              });
          });
    }

    void SpawnRow(DocumentSnapshot doc, int rank)
    {
        // ✅ Null check for prefab and container
        if (playerRowPrefab == null || listContainer == null)
        {
            Debug.LogError("❌ playerRowPrefab or listContainer is null!");
            return;
        }

        // ✅ Safe defaults — no null crash
        string username = "Unknown";
        long xp = 0;
        string picUrl = "";

        if (doc.ContainsField("username")) doc.TryGetValue("username", out username);
        if (doc.ContainsField("xp")) doc.TryGetValue("xp", out xp);
        if (doc.ContainsField("profilePicUrl")) doc.TryGetValue("profilePicUrl", out picUrl);

        // ✅ Extra null safety
        if (string.IsNullOrEmpty(username)) username = "Unknown";
        if (string.IsNullOrEmpty(picUrl)) picUrl = "";

        GameObject row = Instantiate(playerRowPrefab, listContainer);
        if (row == null) return;
        spawnedRows.Add(row);

        // ✅ Safe UI find
        TMP_Text rankText = row.transform.Find("RankBadge/RankText")?.GetComponent<TMP_Text>();
        TMP_Text nameText = row.transform.Find("UsernameText")?.GetComponent<TMP_Text>();
        TMP_Text xpText = row.transform.Find("XPText")?.GetComponent<TMP_Text>();
        Image profileImage = row.transform.Find("ProfileImage")?.GetComponent<Image>();

        // ✅ Null check before assigning text
        if (rankText != null) rankText.text = rank.ToString();
        if (nameText != null) nameText.text = username;
        if (xpText != null) xpText.text = xp + " XP";

        // ✅ Rank badge color
        if (rankText != null)
        {
            rankText.color = rank switch
            {
                1 => new Color(1f, 0.84f, 0f, 1f),   // Gold
                2 => new Color(0.75f, 0.75f, 0.75f, 1f), // Silver
                3 => new Color(0.8f, 0.5f, 0.2f, 1f),    // Bronze
                _ => Color.white
            };
        }

        // ✅ Only load image if URL is valid
        if (profileImage != null && !string.IsNullOrEmpty(picUrl))
            StartCoroutine(LoadImageFromUrl(picUrl, profileImage));
    }

    IEnumerator LoadImageFromUrl(string url, Image targetImage)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("⚠️ Failed to load image: " + request.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            if (targetImage != null)
                targetImage.sprite = sprite;
        }
    }
}