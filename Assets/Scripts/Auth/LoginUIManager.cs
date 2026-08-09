using UnityEngine;
using PlayFab;

public class LoginUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject homePanel;

    void Awake()
    {
        // ✅ Force hide both immediately in Awake (before Start)
        if (loginPanel != null) loginPanel.SetActive(false);
        if (homePanel != null) homePanel.SetActive(false);
    }

    void Start()
    {
        // ✅ If PanelRouter is handling this, do nothing
        string targetPanel = PlayerPrefs.GetString("OpenPanel", "");
        if (!string.IsNullOrEmpty(targetPanel))
        {
            Debug.Log("✅ PanelRouter is handling navigation — LoginUIManager stepping back.");
            return;
        }

        // Normal flow — check login state
        bool loggedIn = PlayFabClientAPI.IsClientLoggedIn();
        if (loggedIn)
        {
            Debug.Log("✅ Already logged in → HomePanel");
            GoToHome();
        }
        else
        {
            Debug.Log("🔒 Not logged in → LoginPanel");
            GoToLogin();
        }
    }

    public void GoToHome()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (homePanel != null) homePanel.SetActive(true);
    }

    public void GoToLogin()
    {
        if (homePanel != null) homePanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(true);
    }

    public void Logout()
    {
        // ✅ Clear saved session on real logout
        PlayerPrefs.DeleteKey("LoggedInUID");
        PlayerPrefs.DeleteKey("LoggedInEmail");
        PlayerPrefs.DeleteKey("OpenPanel");
        PlayerPrefs.Save();

        PlayFabClientAPI.ForgetAllCredentials();
        GoToLogin();
    }
}