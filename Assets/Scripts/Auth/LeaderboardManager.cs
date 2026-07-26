using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [Header("── LEADERBOARD UI ──")]
    public Transform listContainer;
    public GameObject playerRowPrefab;

    private const string XP_STAT_NAME = "XP";
    private const string PUBLIC_DATA_KEY = "publicProfile";

    [Serializable]
    private class PublicProfileData
    {
        public string profilePicBase64;
    }

    private List<GameObject> spawnedRows = new List<GameObject>();

    void OnEnable()
    {
        StartCoroutine(LoadAfterDelay());
    }

    IEnumerator LoadAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        LoadLeaderboard();
    }

    public void LoadLeaderboard()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogError("❌ Not logged in — cannot load leaderboard.");
            return;
        }

        Debug.Log("✅ Logged in as: " + FirebaseAuthManager.CurrentEmail);

        foreach (var row in spawnedRows)
            Destroy(row);
        spawnedRows.Clear();

        Debug.Log("Loading leaderboard...");

        PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest
        {
            StatisticName = XP_STAT_NAME,
            StartPosition = 0,
            MaxResultsCount = 100
        },
        result =>
        {
            Debug.Log("✅ Users loaded: " + result.Leaderboard.Count);

            foreach (var entry in result.Leaderboard)
                SpawnRow(entry);
        },
        error => Debug.LogError("❌ Leaderboard failed: " + error.GenerateErrorReport()));
    }

    void SpawnRow(PlayerLeaderboardEntry entry)
    {
        if (playerRowPrefab == null || listContainer == null)
        {
            Debug.LogError("❌ playerRowPrefab or listContainer is null!");
            return;
        }

        string username = string.IsNullOrEmpty(entry.DisplayName) ? "Unknown" : entry.DisplayName;
        int xp = entry.StatValue;
        int rank = entry.Position + 1;

        GameObject row = Instantiate(playerRowPrefab, listContainer);
        if (row == null) return;
        spawnedRows.Add(row);

        TMP_Text rankText = row.transform.Find("RankBadge/RankText")?.GetComponent<TMP_Text>();
        TMP_Text nameText = row.transform.Find("UsernameText")?.GetComponent<TMP_Text>();
        TMP_Text xpText = row.transform.Find("XPText")?.GetComponent<TMP_Text>();
        Image profileImage = row.transform.Find("ProfileImage")?.GetComponent<Image>();

        if (rankText != null) rankText.text = rank.ToString();
        if (nameText != null) nameText.text = username;
        if (xpText != null) xpText.text = xp + " XP";

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

        if (profileImage != null)
            LoadProfilePicture(entry.PlayFabId, profileImage);
    }

    void LoadProfilePicture(string playFabId, Image targetImage)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            PlayFabId = playFabId,
            Keys = new List<string> { PUBLIC_DATA_KEY }
        },
        result =>
        {
            if (targetImage == null) return; // row may have been destroyed already

            if (!result.Data.TryGetValue(PUBLIC_DATA_KEY, out var record) || string.IsNullOrEmpty(record.Value))
                return;

            var publicData = JsonUtility.FromJson<PublicProfileData>(record.Value);
            if (string.IsNullOrEmpty(publicData.profilePicBase64))
                return;

            byte[] bytes = Convert.FromBase64String(publicData.profilePicBase64);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(bytes);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            targetImage.sprite = sprite;
        },
        error => Debug.LogWarning("⚠️ Failed to load picture for " + playFabId + ": " + error.GenerateErrorReport()));
    }
}
