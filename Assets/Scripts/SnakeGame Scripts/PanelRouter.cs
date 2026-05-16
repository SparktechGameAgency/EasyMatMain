using System.Collections;
using UnityEngine;
using Firebase.Auth;

public class PanelRouter : MonoBehaviour
{
    [Header("All Panels — assign every panel here")]
    public GameObject HomePanel;
    public GameObject LoginPanel;
    public GameObject LeaderBoardPanel;
    public GameObject AllGamePanel;
    public GameObject RegisterPanel;
    public GameObject SettingsPanel;

    void Awake()
    {
        // ✅ Hide everything immediately
        HideAllPanels();
    }

    void Start()
    {
        string targetPanel = PlayerPrefs.GetString("OpenPanel", "");

        if (!string.IsNullOrEmpty(targetPanel))
        {
            // ✅ DON'T delete key here — let FirebaseAuthManager read it too
            Debug.Log("✅ PanelRouter opening: " + targetPanel);
            StartCoroutine(WaitForAuthThenOpen(targetPanel));
        }
    }


    IEnumerator WaitForAuthThenOpen(string panelName)
    {
        Debug.Log("⏳ Waiting for Firebase Auth...");

        float timeout = 5f;
        float elapsed = 0f;

        while (FirebaseAuth.DefaultInstance.CurrentUser == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        bool isLoggedIn = FirebaseAuth.DefaultInstance.CurrentUser != null;

        if (!isLoggedIn)
        {
            string savedUID = PlayerPrefs.GetString("LoggedInUID", "");
            if (!string.IsNullOrEmpty(savedUID))
            {
                Debug.LogWarning("⚠️ Using saved UID fallback.");
                isLoggedIn = true;
            }
        }

        if (isLoggedIn)
        {
            Debug.Log("✅ Auth confirmed → opening: " + panelName);
            OpenPanel(panelName);
        }
        else
        {
            Debug.LogWarning("⚠️ Not logged in → Login.");
            PlayerPrefs.DeleteKey("LoggedInUID");
            PlayerPrefs.DeleteKey("LoggedInEmail");
            if (LoginPanel != null) LoginPanel.SetActive(true);
        }

        // ✅ Delete AFTER everything is done
        PlayerPrefs.DeleteKey("OpenPanel");
        PlayerPrefs.Save();
    }
    void HideAllPanels()
    {
        if (HomePanel != null) HomePanel.SetActive(false);
        if (LoginPanel != null) LoginPanel.SetActive(false);
        if (LeaderBoardPanel != null) LeaderBoardPanel.SetActive(false);
        if (AllGamePanel != null) AllGamePanel.SetActive(false);
        if (RegisterPanel != null) RegisterPanel.SetActive(false);
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
    }

    void OpenPanel(string panelName)
    {
        HideAllPanels();

        switch (panelName)
        {
            case "HomePanel":
                if (HomePanel != null) HomePanel.SetActive(true);
                break;
            case "LoginPanel":
                if (LoginPanel != null) LoginPanel.SetActive(true);
                break;
            case "LeaderBoardPanel":
                if (LeaderBoardPanel != null) LeaderBoardPanel.SetActive(true);
                break;
            case "AllGamePanel":
                if (AllGamePanel != null) AllGamePanel.SetActive(true);
                break;
            case "RegisterPanel":
                if (RegisterPanel != null) RegisterPanel.SetActive(true);
                break;
            case "SettingsPanel":
                if (SettingsPanel != null) SettingsPanel.SetActive(true);
                break;
            default:
                if (HomePanel != null) HomePanel.SetActive(true);
                break;
        }

        // ✅ Force hide login panel in case anything else activated it
        if (panelName != "LoginPanel")
            if (LoginPanel != null) LoginPanel.SetActive(false);

        Debug.Log("✅ Opened: " + panelName);
    }
}