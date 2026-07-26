using Mapbox.Examples.Voxels;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FirebaseAuthManager : MonoBehaviour
{
    // ── Cross-script session state (replaces FirebaseAuth.DefaultInstance.CurrentUser) ──
    public static string CurrentPlayFabId;
    public static string CurrentEmail;
    public static string CurrentUsername;
    public static bool IsLoggedIn => PlayFabClientAPI.IsClientLoggedIn();

    private const string PUBLIC_DATA_KEY = "publicProfile";
    private const string PRIVATE_DATA_KEY = "privateProfile";
    private const string XP_STAT_NAME = "XP";

    [Serializable]
    private class PublicProfileData
    {
        public string profilePicBase64;
    }

    [Serializable]
    private class PrivateProfileData
    {
        public string phone;
    }

    private bool sdkReady = false;
    private bool isProcessing = false;

    [Header("── PANELS ──")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject homepagePanel;

    [Header("── LOGIN PANEL ──")]
    public TMP_InputField login_Email;
    public TMP_InputField login_Password;
    public TMP_Text login_Status;
    public Button login_ConfirmButton;

    [Header("── REGISTER PANEL ──")]
    public TMP_InputField reg_Username;
    public TMP_InputField reg_Email;
    public TMP_InputField reg_Phone;
    public TMP_InputField reg_Password;
    public TMP_InputField reg_ConfirmPassword;
    public TMP_Text reg_Status;
    public Button reg_ConfirmButton;
    public static bool returningFromGame = false;



    [Header("── HOMEPAGE & PROFILE ──")]
    public TMP_Text home_UsernameText;       // username text on top card
    public TMP_Text home_TopUsername;        // big name text on top card
    public TMP_Text profile_Username;        // username in Profile Info section
    public TMP_Text profile_Email;           // email in Profile Info section
    public TMP_Text profile_Phone;           // phone in Profile Info section
    public Image home_ProfilePicture;     // circle image on top card
    public Image profile_ProfilePicture;  // circle image in Profile Info section
    public Button changePictureButton;     // change picture button

    [Header("── ALL GAME PANEL ──")]
    public TMP_Text allGame_UsernameText;    // drag username text from All Game panel
    public Image allGame_ProfilePicture;  // drag profile image from All Game panel

    [Header("── VISOHUNT PANEL ──")]
    public TMP_Text visoHunt_UsernameText;   // drag username text from Visohunt panel
    public Image visoHunt_ProfilePicture; //

    [Header("── SHARED ──")]
    public GameObject loadingPanel;

    // ── SINGLETON GUARD ────────────────────────────────────────────
    // The MainMenu scene has a stray, unconfigured duplicate of this
    // component sitting on another GameObject. If two instances both
    // run Start(), they race on the static session fields/flags below
    // (e.g. returningFromGame) and can knock a logged-in user back to
    // the login screen. Only let one *configured* instance run.
    public static FirebaseAuthManager Instance;

    void Awake()
    {
        bool isConfigured = loginPanel != null || registerPanel != null || homepagePanel != null;

        if (!isConfigured)
        {
            Debug.LogWarning("⚠️ FirebaseAuthManager on '" + gameObject.name + "' has no panels assigned — disabling stray duplicate.");
            enabled = false;
            return;
        }

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("⚠️ Duplicate FirebaseAuthManager found on '" + gameObject.name + "' — disabling it.");
            enabled = false;
            return;
        }

        Instance = this;
    }

    // ── START ──────────────────────────────────────────────────────
    void Start()
    {
        bool panelRouterHandling = FirebaseAuthManager.returningFromGame;
        FirebaseAuthManager.returningFromGame = false; // reset after reading

        if (!panelRouterHandling)
        {
            ShowPanel("login");
        }
        else
        {
            if (loginPanel) loginPanel.SetActive(false);
            if (registerPanel) registerPanel.SetActive(false);
            if (homepagePanel) homepagePanel.SetActive(false);
        }

        SetLoginConfirmInteractable(false);
        SetRegisterConfirmInteractable(false);
        SetLoginStatus("Initializing...");

        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            // ✅ Session survived a scene load within this app run — same as a persisted Firebase user.
            sdkReady = true;
            SetLoginConfirmInteractable(true);
            SetRegisterConfirmInteractable(true);

            if (panelRouterHandling)
            {
                Debug.Log("✅ Returning from game — loading silently.");
                StartCoroutine(LoadUserDataSilentlyDelayed());
            }
            else
            {
                // ✅ Session is already valid (e.g. app was reopened and the
                // device-linked session survived) — go straight to homepage
                // instead of stranding a logged-in user on the login screen.
                Debug.Log("✅ Session already active — restoring homepage.");
                ShowPanel("homepage");
                StartCoroutine(LoadUserDataSilentlyDelayed());
            }
            return;
        }

        // ── Cold start: try silent device-linked auto-login (no password stored) ──
        SetLoginStatus("Checking saved session...");
        TryDeviceAutoLogin(panelRouterHandling);
    }

    // ── DEVICE AUTO-LOGIN ──────────────────────────────────────────
    void TryDeviceAutoLogin(bool panelRouterHandling)
    {
        string deviceId = SystemInfo.deviceUniqueIdentifier;

        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest
        {
            CustomId = deviceId,
            CreateAccount = false,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetUserAccountInfo = true,
                GetUserData = true
            }
        },
        result =>
        {
            sdkReady = true;
            SetLoginConfirmInteractable(true);
            SetRegisterConfirmInteractable(true);
            CacheSessionInfo(result);

            Debug.Log("✅ Auto-logged in via device ID: " + CurrentEmail);

            if (panelRouterHandling)
                LoadUserDataSilently(result);
            else
                LoadUserDataAndShowHomepage(result);
        },
        error =>
        {
            sdkReady = true;
            SetLoginConfirmInteractable(true);
            SetRegisterConfirmInteractable(true);
            Debug.Log("ℹ️ No saved device session (" + error.Error + ") — showing login.");
            SetLoginStatus("Please login or register.");
        });
    }

    // ── Link this device to the account so future cold starts auto-login ──
    void LinkDeviceToAccount()
    {
        PlayFabClientAPI.LinkCustomID(new LinkCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            ForceLink = true
        },
        result => Debug.Log("✅ Device linked for auto-login."),
        error => Debug.LogWarning("⚠️ Device link failed: " + error.GenerateErrorReport()));
    }

    void CacheSessionInfo(LoginResult result)
    {
        CurrentPlayFabId = result.PlayFabId;
        CurrentEmail = result.InfoResultPayload?.AccountInfo?.PrivateInfo?.Email ?? CurrentEmail;
        CurrentUsername = result.InfoResultPayload?.AccountInfo?.TitleInfo?.DisplayName ?? CurrentUsername;
    }

    IEnumerator LoadUserDataSilentlyDelayed()
    {
        yield return new WaitForSeconds(1f);

        PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), result =>
        {
            CurrentPlayFabId = result.AccountInfo.PlayFabId;
            CurrentEmail = result.AccountInfo.PrivateInfo?.Email ?? CurrentEmail;
            CurrentUsername = result.AccountInfo.TitleInfo?.DisplayName ?? CurrentUsername;
            LoadUserDataSilently(null);
        },
        error => Debug.LogError("❌ GetAccountInfo failed: " + error.GenerateErrorReport()));
    }

    void LoadUserDataSilently(LoginResult loginResult)
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest
        {
            Keys = new List<string> { PRIVATE_DATA_KEY, PUBLIC_DATA_KEY }
        },
        result =>
        {
            string displayUsername = CurrentUsername ?? "";
            string displayEmail = CurrentEmail ?? "";
            string displayPhone = ReadPrivateData(result.Data).phone ?? "";

            if (string.IsNullOrEmpty(displayUsername))
                displayUsername = CurrentEmail ?? "";

            ApplyUsernameAndEmailToUI(displayUsername, displayEmail, displayPhone);

            string base64 = ReadPublicData(result.Data).profilePicBase64;
            if (!string.IsNullOrEmpty(base64))
                ApplyProfilePictureFromBase64(base64);

            Debug.Log("✅ User data loaded silently for: " + displayUsername);
        },
        error => Debug.LogError("❌ GetUserData failed: " + error.GenerateErrorReport()));
    }

    // ── LOGIN CONFIRM ──────────────────────────────────────────────
    public void OnLoginConfirmButton()
    {
        if (isProcessing) { SetLoginStatus("Please wait..."); return; }
        if (!sdkReady) { SetLoginStatus("Not ready, wait..."); return; }

        string email = login_Email != null ? login_Email.text.Trim() : "";
        string password = login_Password != null ? login_Password.text : "";

        if (string.IsNullOrEmpty(email)) { SetLoginStatus("Please enter your email."); return; }
        if (!IsValidEmail(email)) { SetLoginStatus("Invalid email address."); return; }
        if (string.IsNullOrEmpty(password)) { SetLoginStatus("Please enter your password."); return; }
        if (password.Length < 6) { SetLoginStatus("Password min 6 characters."); return; }

        Login(email, password);
    }

    // ── REGISTER CONFIRM ───────────────────────────────────────────
    public void OnRegisterConfirmButton()
    {
        if (isProcessing) { SetRegisterStatus("Please wait..."); return; }
        if (!sdkReady) { SetRegisterStatus("Not ready, wait..."); return; }

        string username = reg_Username != null ? reg_Username.text.Trim() : "";
        string email = reg_Email != null ? reg_Email.text.Trim() : "";
        string phone = reg_Phone != null ? reg_Phone.text.Trim() : "";
        string password = reg_Password != null ? reg_Password.text : "";
        string confirmPassword = reg_ConfirmPassword != null ? reg_ConfirmPassword.text : "";

        if (string.IsNullOrEmpty(username)) { SetRegisterStatus("Please enter a username."); return; }
        if (username.Length < 3) { SetRegisterStatus("Username min 3 characters."); return; }
        if (username.Length > 20) { SetRegisterStatus("Username max 20 characters."); return; }
        if (!IsValidUsername(username)) { SetRegisterStatus("Username: letters, numbers, _ only."); return; }
        if (string.IsNullOrEmpty(email)) { SetRegisterStatus("Please enter your email."); return; }
        if (!IsValidEmail(email)) { SetRegisterStatus("Invalid email address."); return; }
        if (string.IsNullOrEmpty(phone)) { SetRegisterStatus("Please enter phone number."); return; }
        if (!IsValidPhone(phone)) { SetRegisterStatus("Invalid phone number."); return; }
        if (string.IsNullOrEmpty(password)) { SetRegisterStatus("Please enter a password."); return; }
        if (password.Length < 6) { SetRegisterStatus("Password min 6 characters."); return; }
        if (string.IsNullOrEmpty(confirmPassword)) { SetRegisterStatus("Please confirm your password."); return; }
        if (password != confirmPassword) { SetRegisterStatus("Passwords do not match."); return; }

        CheckUsernameAndRegister(username, email, password, phone);
    }

    // ── REGISTER (PlayFab enforces Username uniqueness atomically) ─
    void CheckUsernameAndRegister(string username, string email, string password, string phone)
    {
        Register(email, password, username, phone);
    }

    // ── REGISTER ───────────────────────────────────────────────────
    void Register(string email, string password, string username, string phone)
    {
        isProcessing = true;
        SetRegisterConfirmInteractable(false);
        ShowLoading(true);
        SetRegisterStatus("Creating account...");

        PlayFabClientAPI.RegisterPlayFabUser(new RegisterPlayFabUserRequest
        {
            Email = email,
            Password = password,
            Username = username,
            DisplayName = username // shows up as leaderboard DisplayName
        },
        result =>
        {
            CurrentPlayFabId = result.PlayFabId;
            CurrentEmail = email;
            CurrentUsername = username;

            LinkDeviceToAccount();

            var privateData = new PrivateProfileData { phone = phone };
            PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { PRIVATE_DATA_KEY, JsonUtility.ToJson(privateData) } },
                Permission = UserDataPermission.Private
            },
            updateResult =>
            {
                isProcessing = false;
                ShowLoading(false);
                SetRegisterConfirmInteractable(true);

                Debug.Log("✅ Registered: " + username);
                ClearRegisterFields();
                ShowPanel("login");
                SetLoginStatus("✅ Registered! Please login.");
            },
            error =>
            {
                isProcessing = false;
                ShowLoading(false);
                SetRegisterConfirmInteractable(true);
                Debug.LogError("Save phone failed: " + error.GenerateErrorReport());
                SetRegisterStatus("Account created but data save failed.");
            });
        },
        error =>
        {
            isProcessing = false;
            ShowLoading(false);
            SetRegisterConfirmInteractable(true);
            SetRegisterStatus(GetPlayFabError(error));
        });
    }

    // ── LOGIN ──────────────────────────────────────────────────────
    void Login(string email, string password)
    {
        isProcessing = true;
        SetLoginConfirmInteractable(false);
        ShowLoading(true);
        SetLoginStatus("Logging in...");

        PlayFabClientAPI.LoginWithEmailAddress(new LoginWithEmailAddressRequest
        {
            Email = email,
            Password = password,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetUserAccountInfo = true,
                GetUserData = true
            }
        },
        result =>
        {
            isProcessing = false;
            SetLoginConfirmInteractable(true);
            CacheSessionInfo(result);
            LinkDeviceToAccount();

            Debug.Log("✅ Logged in: " + CurrentEmail);
            LoadUserDataAndShowHomepage(result);
        },
        error =>
        {
            isProcessing = false;
            ShowLoading(false);
            SetLoginConfirmInteractable(true);
            SetLoginStatus(GetPlayFabError(error));
        });
    }

    // ── LOAD USER DATA → HOMEPAGE ──────────────────────────────────
    void LoadUserDataAndShowHomepage(LoginResult loginResult)
    {
        SetLoginStatus("Loading profile...");
        ShowLoading(true);

        string displayUsername = CurrentUsername ?? "";
        string displayEmail = CurrentEmail ?? "";
        string displayPhone = "";

        var dataDict = loginResult?.InfoResultPayload?.UserData;
        if (dataDict != null)
        {
            displayPhone = ReadPrivateData(dataDict).phone ?? "";
        }

        if (string.IsNullOrEmpty(displayUsername))
            displayUsername = displayEmail;

        ApplyUsernameAndEmailToUI(displayUsername, displayEmail, displayPhone);

        if (dataDict != null)
        {
            string base64 = ReadPublicData(dataDict).profilePicBase64;
            if (!string.IsNullOrEmpty(base64))
                ApplyProfilePictureFromBase64(base64);
        }

        ShowLoading(false);
        ClearLoginFields();
        ShowPanel("homepage");
    }

    void ApplyUsernameAndEmailToUI(string username, string email, string phone)
    {
        if (home_UsernameText != null) home_UsernameText.text = username ?? "";
        if (home_TopUsername != null) home_TopUsername.text = username ?? "";
        if (profile_Username != null) profile_Username.text = username ?? "";
        if (profile_Email != null) profile_Email.text = email ?? "";
        if (profile_Phone != null) profile_Phone.text = phone ?? "";

        if (allGame_UsernameText != null) allGame_UsernameText.text = username ?? "";
        if (visoHunt_UsernameText != null) visoHunt_UsernameText.text = username ?? "";
    }

    PrivateProfileData ReadPrivateData(Dictionary<string, UserDataRecord> data)
    {
        if (data != null && data.TryGetValue(PRIVATE_DATA_KEY, out var record) && !string.IsNullOrEmpty(record.Value))
            return JsonUtility.FromJson<PrivateProfileData>(record.Value);
        return new PrivateProfileData();
    }

    PublicProfileData ReadPublicData(Dictionary<string, UserDataRecord> data)
    {
        if (data != null && data.TryGetValue(PUBLIC_DATA_KEY, out var record) && !string.IsNullOrEmpty(record.Value))
            return JsonUtility.FromJson<PublicProfileData>(record.Value);
        return new PublicProfileData();
    }

    // ── LOGOUT ─────────────────────────────────────────────────────
    public void Logout()
    {
        PlayFabClientAPI.ForgetAllCredentials();
        CurrentPlayFabId = null;
        CurrentEmail = null;
        CurrentUsername = null;

        ClearLoginFields();
        ClearRegisterFields();

        if (home_UsernameText != null) home_UsernameText.text = "";
        if (home_TopUsername != null) home_TopUsername.text = "";
        if (profile_Username != null) profile_Username.text = "";
        if (profile_Email != null) profile_Email.text = "";
        if (profile_Phone != null) profile_Phone.text = "";

        // ── All Game Panel ─────────────────────────────────────────
        if (allGame_UsernameText != null) allGame_UsernameText.text = "";
        if (allGame_ProfilePicture != null) allGame_ProfilePicture.sprite = null;

        // ── Visohunt Panel ─────────────────────────────────────────
        if (visoHunt_UsernameText != null) visoHunt_UsernameText.text = "";
        if (visoHunt_ProfilePicture != null) visoHunt_ProfilePicture.sprite = null;

        if (home_ProfilePicture != null) home_ProfilePicture.sprite = null;
        if (profile_ProfilePicture != null) profile_ProfilePicture.sprite = null;

        SetLoginStatus("You have been logged out.");
        ShowPanel("login");
    }

    // ── SWITCH PANELS ──────────────────────────────────────────────
    public void OnGoToRegister()
    {
        if (isProcessing) { isProcessing = false; ShowLoading(false); }
        ClearLoginFields();
        SetLoginStatus("");
        ShowPanel("register");
    }

    public void OnGoToLogin()
    {
        if (isProcessing) { isProcessing = false; ShowLoading(false); }
        ClearRegisterFields();
        SetRegisterStatus("");
        ShowPanel("login");
    }

    // ── PANEL SWITCHER ─────────────────────────────────────────────
    void ShowPanel(string panel)
    {
        if (loginPanel) loginPanel.SetActive(panel == "login");
        if (registerPanel) registerPanel.SetActive(panel == "register");
        if (homepagePanel) homepagePanel.SetActive(panel == "homepage");
    }

    // ── CLEAR FIELDS ───────────────────────────────────────────────
    void ClearLoginFields()
    {
        if (login_Email) login_Email.text = "";
        if (login_Password) login_Password.text = "";
    }

    void ClearRegisterFields()
    {
        if (reg_Username) reg_Username.text = "";
        if (reg_Email) reg_Email.text = "";
        if (reg_Phone) reg_Phone.text = "";
        if (reg_Password) reg_Password.text = "";
        if (reg_ConfirmPassword) reg_ConfirmPassword.text = "";
    }

    // ── STATUS ─────────────────────────────────────────────────────
    void SetLoginStatus(string msg) { if (login_Status != null) login_Status.text = msg; }
    void SetRegisterStatus(string msg) { if (reg_Status != null) reg_Status.text = msg; }

    // ── BUTTONS ────────────────────────────────────────────────────
    void SetLoginConfirmInteractable(bool v) { if (login_ConfirmButton) login_ConfirmButton.interactable = v; }
    void SetRegisterConfirmInteractable(bool v) { if (reg_ConfirmButton) reg_ConfirmButton.interactable = v; }

    // ── LOADING ────────────────────────────────────────────────────
    void ShowLoading(bool show) { if (loadingPanel) loadingPanel.SetActive(show); }

    // ── VALIDATION ─────────────────────────────────────────────────
    bool IsValidEmail(string email) =>
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.IgnoreCase);

    bool IsValidPhone(string phone) =>
        Regex.IsMatch(phone, @"^\+?[0-9]{7,15}$");

    bool IsValidUsername(string username) =>
        Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");

    // ── PLAYFAB ERROR ──────────────────────────────────────────────
    string GetPlayFabError(PlayFabError error)
    {
        return error.Error switch
        {
            PlayFabErrorCode.EmailAddressNotAvailable => "Email already in use. Try logging in.",
            PlayFabErrorCode.UsernameNotAvailable => "❌ Username already taken. Choose another.",
            PlayFabErrorCode.InvalidEmailAddress => "Invalid email address.",
            PlayFabErrorCode.InvalidPassword => "Password is too weak.",
            PlayFabErrorCode.InvalidEmailOrPassword => "Wrong email or password. Try again.",
            PlayFabErrorCode.AccountNotFound => "No account found. Please register.",
            PlayFabErrorCode.ConnectionError => "Network error. Check connection.",
            _ => error.ErrorMessage
        };
    }

    // ── CHANGE PICTURE BUTTON ──────────────────────────────────────
    public void OnChangePictureButton()
    {
#if UNITY_EDITOR
        // In Editor — use file dialog
        string path = UnityEditor.EditorUtility.OpenFilePanel(
            "Select Profile Picture", "", "png,jpg,jpeg");

        if (!string.IsNullOrEmpty(path))
        {
            Debug.Log("Image selected: " + path);
            StartCoroutine(UploadProfilePicture(path));
        }

#elif UNITY_ANDROID || UNITY_IOS
    // On device — needs NativeGallery plugin
    NativeGallery.GetImageFromGallery((path) =>
    {
        if (path == null) { Debug.Log("No image selected."); return; }
        Debug.Log("Image selected: " + path);
        StartCoroutine(UploadProfilePicture(path));
    }, "Select Profile Picture", "image/*");

#endif
    }

    // ── UPLOAD PROFILE PICTURE (stored as Base64 JSON in PlayFab Public Player Data) ──
    IEnumerator UploadProfilePicture(string imagePath)
    {
        ShowLoading(true);

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageBytes);
        TextureScale.Bilinear(tex, 256, 256);
        byte[] resizedBytes = tex.EncodeToJPG(75);
        string base64 = Convert.ToBase64String(resizedBytes);

        var publicData = new PublicProfileData { profilePicBase64 = base64 };
        var updateTask = new TaskCompletionFlag();

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { PUBLIC_DATA_KEY, JsonUtility.ToJson(publicData) } },
            Permission = UserDataPermission.Public // readable by other players (leaderboard)
        },
        result =>
        {
            updateTask.Done = true;
            updateTask.Success = true;
        },
        error =>
        {
            updateTask.Done = true;
            updateTask.Success = false;
            Debug.LogError("Upload failed: " + error.GenerateErrorReport());
        });

        yield return new WaitUntil(() => updateTask.Done);

        ShowLoading(false);

        if (!updateTask.Success)
            yield break;

        Debug.Log("✅ Profile picture saved to PlayFab player data.");
        ApplyProfilePictureFromBase64(base64);
    }

    private class TaskCompletionFlag
    {
        public bool Done;
        public bool Success;
    }

    // ── APPLY PICTURE FROM BASE64 ──────────────────────────────────
    void ApplyProfilePictureFromBase64(string base64)
    {
        byte[] bytes = Convert.FromBase64String(base64);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(bytes);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        if (home_ProfilePicture != null) home_ProfilePicture.sprite = sprite;
        if (profile_ProfilePicture != null) profile_ProfilePicture.sprite = sprite;
        if (allGame_ProfilePicture != null) allGame_ProfilePicture.sprite = sprite;
        if (visoHunt_ProfilePicture != null) visoHunt_ProfilePicture.sprite = sprite;

        Debug.Log("✅ Profile picture updated!");
    }
}
