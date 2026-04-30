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

    // ── REPLACE YOUR OLD OnEnable WITH THIS ────────────────────────
    void OnEnable()
    {
        StartCoroutine(LoadAfterDelay());
    }

    IEnumerator LoadAfterDelay()
    {
        // Wait for Firebase Auth to be ready
        yield return new WaitForSeconds(0.5f);
        db = FirebaseFirestore.DefaultInstance;
        LoadLeaderboard();
    }

    // ── LOAD ALL USERS ─────────────────────────────────────────────
    public void LoadLeaderboard()
    {
        if (db == null) db = FirebaseFirestore.DefaultInstance;

        // ── Check logged in ────────────────────────────────────────
        var auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null)
        {
            Debug.LogError("❌ Not logged in — cannot load leaderboard.");
            return;
        }

        Debug.Log("✅ Logged in as: " + auth.CurrentUser.Email);

        // Clear old rows
        foreach (var row in spawnedRows)
            Destroy(row);
        spawnedRows.Clear();

        Debug.Log("Loading leaderboard...");

        db.Collection("users")
          //.OrderByDescending("xp")
          .GetSnapshotAsync()
          .ContinueWith(task =>
          {
              UnityMainThreadDispatcher.Instance().Enqueue(() =>
              {
                  if (task.IsFaulted)
                  {
                      Debug.LogError("Leaderboard failed: " + task.Exception);
                      return;
                  }

                  List<DocumentSnapshot> users = task.Result.Documents.ToList();
                  Debug.Log("✅ Users loaded: " + users.Count);

                  for (int i = 0; i < users.Count; i++)
                  {
                      SpawnRow(users[i], i + 1);
                  }
              });
          });
    }

    // ── SPAWN ONE ROW PER USER ─────────────────────────────────────
    void SpawnRow(DocumentSnapshot doc, int rank)
    {
        if (playerRowPrefab == null || listContainer == null) return;

        doc.TryGetValue("username", out string username);
        doc.TryGetValue("xp", out long xp);
        doc.TryGetValue("profilePicUrl", out string picUrl);

        GameObject row = Instantiate(playerRowPrefab, listContainer);
        spawnedRows.Add(row);

        TMP_Text rankText = row.transform.Find("RankBadge/RankText")?.GetComponent<TMP_Text>();
        TMP_Text nameText = row.transform.Find("UsernameText")?.GetComponent<TMP_Text>();
        TMP_Text xpText = row.transform.Find("XPText")?.GetComponent<TMP_Text>();
        Image profileImage = row.transform.Find("ProfileImage")?.GetComponent<Image>();

        if (rankText) rankText.text = rank.ToString();
        if (nameText) nameText.text = username ?? "Unknown";
        if (xpText) xpText.text = xp + " XP";

        if (rankText)
        {
            rankText.color = rank switch
            {
                1 => new Color(1f, 0.84f, 0f, 1f),
                2 => new Color(0.75f, 0.75f, 0.75f, 1f),
                3 => new Color(0.8f, 0.5f, 0.2f, 1f),
                _ => Color.white
            };
        }

        if (profileImage != null && !string.IsNullOrEmpty(picUrl))
            StartCoroutine(LoadImageFromUrl(picUrl, profileImage));
    }

    // ── LOAD IMAGE FROM URL ────────────────────────────────────────
    IEnumerator LoadImageFromUrl(string url, Image targetImage)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Failed to load image: " + request.error);
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