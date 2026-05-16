using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Storage;
using Mapbox.Examples.Voxels;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FirebaseAuthManager : MonoBehaviour
{
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseStorage storage;
    private StorageReference storageRef;
    private bool firebaseReady = false;
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

    // ── START ──────────────────────────────────────────────────────
    void Start()
    {
        // ✅ Use static flag instead of PlayerPrefs
        bool panelRouterHandling = FirebaseAuthManager.returningFromGame;
        FirebaseAuthManager.returningFromGame = false; // reset after reading

        if (!panelRouterHandling)
        {
            ShowPanel("login");
        }
        else
        {
            // ✅ Hide all auth panels
            if (loginPanel) loginPanel.SetActive(false);
            if (registerPanel) registerPanel.SetActive(false);
            if (homepagePanel) homepagePanel.SetActive(false);
        }

        SetLoginConfirmInteractable(false);
        SetRegisterConfirmInteractable(false);
        SetLoginStatus("Initializing...");

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    auth = FirebaseAuth.DefaultInstance;
                    db = FirebaseFirestore.DefaultInstance;
                    storage = FirebaseStorage.DefaultInstance;
                    storageRef = storage.RootReference;
                    firebaseReady = true;

                    Debug.Log("✅ Firebase Ready!");
                    SetLoginConfirmInteractable(true);
                    SetRegisterConfirmInteractable(true);

                    if (panelRouterHandling && auth.CurrentUser != null)
                    {
                        Debug.Log("✅ Returning from game — loading silently.");
                        StartCoroutine(LoadUserDataSilentlyDelayed(auth.CurrentUser));
                    }
                    else
                    {
                        SetLoginStatus("Please login or register.");
                    }
                }
                else
                {
                    SetLoginStatus("Firebase failed to load.");
                    Debug.LogError("Firebase FAILED: " + task.Result);
                }
            });
        });
    }

    IEnumerator LoadUserDataSilentlyDelayed(FirebaseUser user)
    {
        yield return new WaitForSeconds(1f);
        LoadUserDataSilently(user);
    }

    void LoadUserDataSilently(FirebaseUser user)
    {
        db.Collection("users").Document(user.UserId).GetSnapshotAsync()
            .ContinueWith(task =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    string displayUsername = user.DisplayName ?? "";
                    string displayEmail = user.Email ?? "";
                    string displayPhone = "";

                    if (!task.IsFaulted && task.Result.Exists)
                    {
                        if (task.Result.TryGetValue("username", out string fsUsername)
                            && !string.IsNullOrEmpty(fsUsername))
                            displayUsername = fsUsername;

                        if (task.Result.TryGetValue("phone", out string fsPhone))
                            displayPhone = fsPhone;
                    }

                    if (string.IsNullOrEmpty(displayUsername))
                        displayUsername = user.Email ?? "";

                    // ✅ Update all UI texts
                    if (home_UsernameText != null) home_UsernameText.text = displayUsername;
                    if (home_TopUsername != null) home_TopUsername.text = displayUsername;
                    if (profile_Username != null) profile_Username.text = displayUsername;
                    if (profile_Email != null) profile_Email.text = displayEmail;
                    if (profile_Phone != null) profile_Phone.text = displayPhone;

                    // ✅ All Game Panel
                    if (allGame_UsernameText != null) allGame_UsernameText.text = displayUsername;

                    // ✅ Visohunt Panel
                    if (visoHunt_UsernameText != null) visoHunt_UsernameText.text = displayUsername;

                    // ✅ Load profile picture
                    if (!task.IsFaulted && task.Result.Exists)
                        LoadSavedProfilePicture(task.Result);

                    Debug.Log("✅ User data loaded silently for: " + displayUsername);
                    // ✅ NO ShowPanel call here — PanelRouter handles it
                });
            });
    }

    // ── LOGIN CONFIRM ──────────────────────────────────────────────
    public void OnLoginConfirmButton()
    {
        if (isProcessing) { SetLoginStatus("Please wait..."); return; }
        if (!firebaseReady) { SetLoginStatus("Not ready, wait..."); return; }

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
        if (!firebaseReady) { SetRegisterStatus("Not ready, wait..."); return; }

        string username = reg_Username != null ? reg_Username.text.Trim() : "";
        string email = reg_Email != null ? reg_Email.text.Trim() : "";
        string phone = reg_Phone != null ? reg_Phone.text.Trim() : "";
        string password = reg_Password != null ? reg_Password.text : "";
        string confirmPassword = reg_ConfirmPassword != null ? reg_ConfirmPassword.text : "";

        if (string.IsNullOrEmpty(username)) { SetRegisterStatus("Please enter a username."); return; }
        if (username.Length < 3) { SetRegisterStatus("Username min 3 characters."); return; }
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

    // ── CHECK USERNAME UNIQUE THEN REGISTER ────────────────────────
    void CheckUsernameAndRegister(string username, string email, string password, string phone)
    {
        isProcessing = true;
        SetRegisterConfirmInteractable(false);
        ShowLoading(true);
        SetRegisterStatus("Checking username...");

        db.Collection("usernames").Document(username.ToLower()).GetSnapshotAsync()
            .ContinueWith(task =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (task.IsFaulted)
                    {
                        isProcessing = false;
                        ShowLoading(false);
                        SetRegisterConfirmInteractable(true);
                        SetRegisterStatus("Error checking username. Try again.");
                        return;
                    }

                    if (task.Result.Exists)
                    {
                        isProcessing = false;
                        ShowLoading(false);
                        SetRegisterConfirmInteractable(true);
                        SetRegisterStatus("❌ Username already taken. Choose another.");
                        return;
                    }

                    Register(email, password, username, phone);
                });
            });
    }

    // ── REGISTER ───────────────────────────────────────────────────
    void Register(string email, string password, string username, string phone)
    {
        SetRegisterStatus("Creating account...");

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (task.IsCanceled)
                {
                    isProcessing = false;
                    ShowLoading(false);
                    SetRegisterConfirmInteractable(true);
                    SetRegisterStatus("Registration cancelled.");
                    return;
                }

                if (task.IsFaulted)
                {
                    isProcessing = false;
                    ShowLoading(false);
                    SetRegisterConfirmInteractable(true);
                    SetRegisterStatus(GetFirebaseError(task.Exception));
                    return;
                }

                FirebaseUser user = task.Result.User;

                // ── Save DisplayName to Firebase Auth ──────────────
                UserProfile profile = new UserProfile { DisplayName = username };
                user.UpdateUserProfileAsync(profile);

                // ── Reserve username in Firestore ──────────────────
                db.Collection("usernames").Document(username.ToLower()).SetAsync(
                    new Dictionary<string, object> { { "uid", user.UserId } }
                );

                // ── Save full user data to Firestore ───────────────
                db.Collection("users").Document(user.UserId).SetAsync(
                    new Dictionary<string, object>
                    {
                        { "username",  username                  },
                        { "email",     email                     },
                        { "phone",     phone                     },
                        { "xp",        0                          },
                        { "createdAt", FieldValue.ServerTimestamp }
                    }
                ).ContinueWith(dbTask =>
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        isProcessing = false;
                        ShowLoading(false);
                        SetRegisterConfirmInteractable(true);

                        if (dbTask.IsFaulted)
                        {
                            SetRegisterStatus("Account created but data save failed.");
                            Debug.LogError("Firestore save failed: " + dbTask.Exception);
                            return;
                        }

                        Debug.Log("✅ Registered: " + username);
                        ClearRegisterFields();
                        ShowPanel("login");
                        SetLoginStatus("✅ Registered! Please login.");
                    });
                });
            });
        });
    }

    // ── LOGIN ──────────────────────────────────────────────────────
    void Login(string email, string password)
    {
        isProcessing = true;
        SetLoginConfirmInteractable(false);
        ShowLoading(true);
        SetLoginStatus("Logging in...");

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                isProcessing = false;
                ShowLoading(false);
                SetLoginConfirmInteractable(true);

                if (task.IsCanceled) { SetLoginStatus("Login cancelled."); return; }

                if (task.IsFaulted)
                {
                    SetLoginStatus(GetFirebaseError(task.Exception));
                    return;
                }

                FirebaseUser user = task.Result.User;
                Debug.Log("✅ Logged in: " + user.Email);
                LoadUserDataAndShowHomepage(user);
            });
        });
    }

    // ── LOAD USER DATA → HOMEPAGE ──────────────────────────────────
    void LoadUserDataAndShowHomepage(FirebaseUser user)
    {
        SetLoginStatus("Loading profile...");
        ShowLoading(true);

        db.Collection("users").Document(user.UserId).GetSnapshotAsync()
            .ContinueWith(task =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ShowLoading(false);

                    string displayUsername = user.DisplayName ?? "";
                    string displayEmail = user.Email ?? "";
                    string displayPhone = "";

                    if (!task.IsFaulted && task.Result.Exists)
                    {
                        if (task.Result.TryGetValue("username", out string fsUsername)
                            && !string.IsNullOrEmpty(fsUsername))
                            displayUsername = fsUsername;

                        if (task.Result.TryGetValue("phone", out string fsPhone))
                            displayPhone = fsPhone;
                    }

                    // Fallback if DisplayName empty
                    if (string.IsNullOrEmpty(displayUsername))
                        if (!task.IsFaulted && task.Result.Exists)
                            task.Result.TryGetValue("username", out displayUsername);

                    // ── Update all UI texts ────────────────────────
                    if (home_UsernameText != null) home_UsernameText.text = displayUsername ?? "";
                    if (home_TopUsername != null) home_TopUsername.text = displayUsername ?? "";
                    if (profile_Username != null) profile_Username.text = displayUsername ?? "";
                    if (profile_Email != null) profile_Email.text = displayEmail ?? "";
                    if (profile_Phone != null) profile_Phone.text = displayPhone ?? "";

                    // ── All Game Panel ─────────────────────────────
                    if (allGame_UsernameText != null) allGame_UsernameText.text = displayUsername ?? "";

                    // ── Visohunt Panel ─────────────────────────────
                    if (visoHunt_UsernameText != null) visoHunt_UsernameText.text = displayUsername ?? "";

                    // ── Load profile picture ───────────────────────
                    LoadSavedProfilePicture(task.Result);

                    ClearLoginFields();
                    ShowPanel("homepage");
                });
            });
    }

    // ── LOGOUT ─────────────────────────────────────────────────────
    public void Logout()
    {
        auth?.SignOut();
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

    // ── FIREBASE ERROR ─────────────────────────────────────────────
    string GetFirebaseError(System.AggregateException exception)
    {
        Firebase.FirebaseException firebaseEx = null;
        foreach (var e in exception.Flatten().InnerExceptions)
        {
            firebaseEx = e as Firebase.FirebaseException;
            if (firebaseEx != null) break;
        }

        if (firebaseEx != null)
        {
            var errorCode = (AuthError)firebaseEx.ErrorCode;
            return errorCode switch
            {
                AuthError.EmailAlreadyInUse => "Email already in use. Try logging in.",
                AuthError.InvalidEmail => "Invalid email address.",
                AuthError.WeakPassword => "Password is too weak.",
                AuthError.WrongPassword => "Wrong password. Try again.",
                AuthError.UserNotFound => "No account found. Please register.",
                AuthError.NetworkRequestFailed => "Network error. Check connection.",
                AuthError.TooManyRequests => "Too many attempts. Try later.",
                _ => firebaseEx.Message
            };
        }

        return exception.Message;
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

    // ── UPLOAD PROFILE PICTURE ─────────────────────────────────────
    IEnumerator UploadProfilePicture(string imagePath)
    {
        ShowLoading(true);

        byte[] imageBytes = File.ReadAllBytes(imagePath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageBytes);
        TextureScale.Bilinear(tex, 256, 256);
        byte[] resizedBytes = tex.EncodeToJPG(75);

        string userId = auth.CurrentUser.UserId;
        StorageReference pictureRef = storageRef
            .Child("profilePictures")
            .Child(userId)
            .Child("profile.jpg");

        var uploadTask = pictureRef.PutBytesAsync(resizedBytes);
        yield return new WaitUntil(() => uploadTask.IsCompleted);

        if (uploadTask.IsFaulted)
        {
            ShowLoading(false);
            Debug.LogError("Upload failed: " + uploadTask.Exception);
            yield break;
        }

        var urlTask = pictureRef.GetDownloadUrlAsync();
        yield return new WaitUntil(() => urlTask.IsCompleted);

        if (urlTask.IsFaulted)
        {
            ShowLoading(false);
            Debug.LogError("URL fetch failed: " + urlTask.Exception);
            yield break;
        }

        string downloadUrl = urlTask.Result.ToString();
        Debug.Log("✅ Download URL: " + downloadUrl);

        // ── Save URL to Firestore ──────────────────────────────────
        db.Collection("users").Document(userId).UpdateAsync("profilePicUrl", downloadUrl)
            .ContinueWith(task =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    if (task.IsFaulted)
                        Debug.LogError("Failed to save picture URL: " + task.Exception);
                    else
                        Debug.Log("✅ Picture URL saved to Firestore.");
                });
            });

        // ── Save URL to Firebase Auth photoURL ────────────────────
        UserProfile profile = new UserProfile
        {
            DisplayName = auth.CurrentUser.DisplayName,
            PhotoUrl = new System.Uri(downloadUrl)
        };
        auth.CurrentUser.UpdateUserProfileAsync(profile);

        // ── Show new picture immediately ───────────────────────────
        StartCoroutine(LoadProfilePicture(downloadUrl));
    }

    // ── LOAD PICTURE FROM URL ──────────────────────────────────────
    IEnumerator LoadProfilePicture(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            ShowLoading(false);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load picture: " + request.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            if (home_ProfilePicture != null) home_ProfilePicture.sprite = sprite;
            if (profile_ProfilePicture != null) profile_ProfilePicture.sprite = sprite;

            // ── All Game Panel ─────────────────────────────────────
            if (allGame_ProfilePicture != null) allGame_ProfilePicture.sprite = sprite;

            // ── Visohunt Panel ─────────────────────────────────────
            if (visoHunt_ProfilePicture != null) visoHunt_ProfilePicture.sprite = sprite;

            Debug.Log("✅ Profile picture updated!");
        }
    }

    // ── LOAD SAVED PICTURE ON LOGIN ────────────────────────────────
    void LoadSavedProfilePicture(DocumentSnapshot doc)
    {
        // Try Firebase Auth photoURL first
        if (auth.CurrentUser.PhotoUrl != null)
        {
            string photoUrl = auth.CurrentUser.PhotoUrl.ToString();
            if (!string.IsNullOrEmpty(photoUrl))
            {
                StartCoroutine(LoadProfilePicture(photoUrl));
                return;
            }
        }

        // Fallback: Firestore URL
        if (doc != null && doc.Exists)
        {
            if (doc.TryGetValue("profilePicUrl", out string savedUrl)
                && !string.IsNullOrEmpty(savedUrl))
            {
                StartCoroutine(LoadProfilePicture(savedUrl));
            }
        }
    }
}