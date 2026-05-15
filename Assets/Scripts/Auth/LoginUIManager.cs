using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

public class LoginUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject homePanel;

    private FirebaseAuth auth;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        // ? Hide both first to avoid flicker
        if (loginPanel != null) loginPanel.SetActive(false);
        if (homePanel != null) homePanel.SetActive(false);

        // ? Check Firebase is ready then check login state
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (auth.CurrentUser != null)
            {
                // Already logged in ? go straight to Home
                Debug.Log("? Already logged in: " + auth.CurrentUser.Email);
                GoToHome();
            }
            else
            {
                // Not logged in ? show Login
                Debug.Log("?? Not logged in ? showing login panel");
                GoToLogin();
            }
        });
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

    // ? Call this from your Logout button
    public void Logout()
    {
        auth.SignOut();
        GoToLogin();
    }
}