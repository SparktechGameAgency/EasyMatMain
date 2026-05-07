using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Attach this to your Referral Panel root GameObject.
/// Assign all UI references in the Inspector.
/// </summary>
public class ReferralUIManager : MonoBehaviour
{
    [Header("Tab Buttons")]
    public Button inviteTabButton;
    public Button raffleTabButton;
    public GameObject invitePanel;
    public GameObject rafflePanel;

    [Header("Referral Code")]
    public TextMeshProUGUI referralCodeText;
    public Button copyButton;
    public TextMeshProUGUI copyButtonLabel;

    [Header("Share Buttons")]
    public Button shareButton;
    public Button emailButton;
    public Button textButton;

    [Header("QR Code")]
    public RawImage qrCodeImage;         // UI RawImage to display QR
    public TextMeshProUGUI qrScanLabel;

    [Header("Toast Notification")]
    public GameObject toastObject;
    public TextMeshProUGUI toastText;

    [Header("Raffle Panel")]
    public TextMeshProUGUI ticketCountText;

    [Header("Settings")]
    [Tooltip("Your app's referral base URL")]
    public string referralBaseURL = "https://yourapp.com/join?ref=";

    // ── Runtime state ─────────────────────────────────────────────────────────
    private string referralCode = "9TEAE";   // Set this from your backend
    private int ticketCount = 3;
    private bool onInviteTab = true;
    private Coroutine toastCoroutine;

    // ── Tab colours ───────────────────────────────────────────────────────────
    private Color tabActiveColor = new Color(0.18f, 0.12f, 0.43f, 1f);
    private Color tabInactiveColor = new Color(0.10f, 0.08f, 0.21f, 1f);
    private Color tabActiveText = new Color(0.88f, 0.85f, 0.97f, 1f);
    private Color tabInactiveText = new Color(0.58f, 0.54f, 0.83f, 1f);

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Wire up all button listeners
        inviteTabButton.onClick.AddListener(() => SwitchTab(true));
        raffleTabButton.onClick.AddListener(() => SwitchTab(false));
        copyButton.onClick.AddListener(OnCopyCode);
        shareButton.onClick.AddListener(OnShare);
        emailButton.onClick.AddListener(OnEmail);
        textButton.onClick.AddListener(OnText);

        // Init UI state
        referralCodeText.text = referralCode;
        ticketCountText.text = ticketCount.ToString();
        toastObject.SetActive(false);

        // Start on Invite tab and generate QR
        SwitchTab(true);
        // StartCoroutine(GenerateQRCode());
        StartCoroutine(QRCodeFetcher.FetchQRCode(
           referralBaseURL + referralCode, qrCodeImage));
    }

    // ── PUBLIC API (call from backend / PlayerPrefs after login) ──────────────

    /// <summary>Call this after fetching the user's referral code from your server.</summary>
    public void SetReferralCode(string code)
    {
        referralCode = code;
        referralCodeText.text = code;
        StartCoroutine(GenerateQRCode());  // Refresh QR with new code
    }

    /// <summary>Call this to update the displayed ticket count.</summary>
    public void SetTicketCount(int count)
    {
        ticketCount = count;
        ticketCountText.text = count.ToString();
    }

    // ── TAB SWITCHING ─────────────────────────────────────────────────────────

    void SwitchTab(bool showInvite)
    {
        onInviteTab = showInvite;
        invitePanel.SetActive(showInvite);
        rafflePanel.SetActive(!showInvite);

        SetTabStyle(inviteTabButton, showInvite);
        SetTabStyle(raffleTabButton, !showInvite);
    }

    void SetTabStyle(Button btn, bool active)
    {
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? tabActiveColor : tabInactiveColor;

        var label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.color = active ? tabActiveText : tabInactiveText;
    }

    // ── COPY BUTTON ───────────────────────────────────────────────────────────

    void OnCopyCode()
    {
        GUIUtility.systemCopyBuffer = referralCode;
        ShowToast("Referral code copied!");

        // Briefly change button label to "✓ Copied"
        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        StartCoroutine(FlashCopyButton());
    }

    IEnumerator FlashCopyButton()
    {
        copyButtonLabel.text = "✓ Copied";
        yield return new WaitForSeconds(2f);
        copyButtonLabel.text = "Copy";
    }

    // ── SHARE BUTTON ──────────────────────────────────────────────────────────

    void OnShare()
    {
        string link = referralBaseURL + referralCode;

#if UNITY_ANDROID
        ShareAndroid(link);
#elif UNITY_IOS
        ShareIOS(link);
#else
        // Editor fallback
        GUIUtility.systemCopyBuffer = link;
        ShowToast("Link copied to clipboard!");
#endif
    }

    // ── EMAIL BUTTON ──────────────────────────────────────────────────────────

    void OnEmail()
    {
        string subject = UnityEngine.Networking.UnityWebRequest.EscapeURL(
            "Join me on the AR App — get a free ticket!");
        string body = UnityEngine.Networking.UnityWebRequest.EscapeURL(
            "Hey!\n\nUse my referral code " + referralCode +
            " when you sign up and you'll get a FREE ticket:\n\n" +
            referralBaseURL + referralCode +
            "\n\nSee you in AR!");

        Application.OpenURL("mailto:?subject=" + subject + "&body=" + body);
        ShowToast("Opening email app...");
    }

    // ── TEXT BUTTON ───────────────────────────────────────────────────────────

    void OnText()
    {
        string msg = UnityEngine.Networking.UnityWebRequest.EscapeURL(
            "Join me on the AR app! Use code " + referralCode +
            " for a free ticket: " + referralBaseURL + referralCode);

#if UNITY_ANDROID
        Application.OpenURL("sms:?body=" + msg);
#elif UNITY_IOS
        Application.OpenURL("sms:&body=" + msg);
#else
        GUIUtility.systemCopyBuffer = referralBaseURL + referralCode;
        ShowToast("Link copied — paste in your message app!");
#endif

        ShowToast("Opening messages app...");
    }

    // ── QR CODE GENERATION ────────────────────────────────────────────────────

    IEnumerator GenerateQRCode()
    {
        string url = referralBaseURL + referralCode;

        // Generate via ZXing (see QRCodeGenerator.cs for the helper)
        Texture2D qrTexture = QRCodeGenerator.Generate(url, 256);
        qrCodeImage.texture = qrTexture;

        yield return null;
    }

    // ── TOAST NOTIFICATION ────────────────────────────────────────────────────

    public void ShowToast(string message)
    {
        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(ToastRoutine(message));
    }

    IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;
        toastObject.SetActive(true);

        // Fade in
        CanvasGroup cg = toastObject.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            float t = 0f;
            while (t < 0.2f) { t += Time.deltaTime; cg.alpha = t / 0.2f; yield return null; }
            cg.alpha = 1f;
        }

        yield return new WaitForSeconds(2f);

        // Fade out
        if (cg != null)
        {
            float t = 0f;
            while (t < 0.3f) { t += Time.deltaTime; cg.alpha = 1f - (t / 0.3f); yield return null; }
        }

        toastObject.SetActive(false);
    }

    // ── ANDROID NATIVE SHARE ──────────────────────────────────────────────────

    void ShareAndroid(string link)
    {
#if UNITY_ANDROID
        AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
        AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
        intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
        intentObject.Call<AndroidJavaObject>("setType", "text/plain");
        intentObject.Call<AndroidJavaObject>("putExtra",
            intentClass.GetStatic<string>("EXTRA_TEXT"),
            "Join me on the AR app! Use code " + referralCode + " for a free ticket: " + link);

        AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>(
            "createChooser", intentObject, "Share via");
        currentActivity.Call("startActivity", chooser);
#endif
    }

    void ShareIOS(string link)
    {
#if UNITY_IOS
        // Uses NativeShare plugin or custom ObjC bridge
        // If using NativeShare (recommended free asset):
        // new NativeShare().SetText("Join me! Code: " + referralCode + " " + link).Share();
        
        // Fallback: copy to clipboard
        GUIUtility.systemCopyBuffer = link;
        ShowToast("Link copied! Paste to share.");
#endif
    }
}