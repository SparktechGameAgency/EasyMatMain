/*using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using System.Threading.Tasks;

/// <summary>
/// Attach this to a persistent GameObject (e.g. GameManager).
/// After Firebase login succeeds, it fetches the user's referral code
/// and ticket count from Firestore, then pushes them to ReferralUIManager.
///
/// SETUP:
///   1. Import Firebase Unity SDK (Auth + Firestore packages)
///   2. Add google-services.json (Android) to Assets/
///   3. Attach this script to a GameManager or similar persistent object
///   4. Your Firestore user document should look like:
///        users/{uid}
///          referralCode: "9TEAE"
///          tickets: 3
///          referredBy: "XXXXX"   (optional)
/// </summary>
public class FirebaseReferralConnector : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The ReferralUIManager in your scene")]
    public ReferralUIManager referralUI;

    // ── Firebase instances ────────────────────────────────────────────────────
    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private FirebaseUser currentUser;

    // ─────────────────────────────────────────────────────────────────────────
    async void Start()
    {
        // 1. Check Firebase is ready on this device
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (dependencyStatus != DependencyStatus.Available)
        {
            Debug.LogError("Firebase unavailable: " + dependencyStatus);
            return;
        }

        // 2. Init Auth and Firestore
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // 3. Listen for auth state changes (login / logout)
        auth.StateChanged += OnAuthStateChanged;

        // 4. If user is already logged in (e.g. remembered session), load now
        if (auth.CurrentUser != null)
            await LoadUserReferralData(auth.CurrentUser);
    }

    // ── AUTH STATE LISTENER ───────────────────────────────────────────────────

    /// <summary>Fires whenever the user logs in or out.</summary>
    async void OnAuthStateChanged(object sender, System.EventArgs e)
    {
        FirebaseUser user = auth.CurrentUser;

        if (user != null && user != currentUser)
        {
            // New login
            currentUser = user;
            Debug.Log("Firebase: user signed in → " + user.UserId);
            await LoadUserReferralData(user);
        }
        else if (user == null)
        {
            // Logged out
            currentUser = null;
            Debug.Log("Firebase: user signed out");
        }
    }

    // ── LOAD REFERRAL DATA FROM FIRESTORE ─────────────────────────────────────

    /// <summary>
    /// Reads users/{uid} from Firestore.
    /// If no document exists yet (first-time user), creates one with a
    /// fresh generated code and 0 tickets.
    /// </summary>
    async Task LoadUserReferralData(FirebaseUser user)
    {
        string uid = user.UserId;
        DocumentReference docRef = db.Collection("users").Document(uid);

        try
        {
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            string referralCode;
            int tickets;

            if (snapshot.Exists)
            {
                // ── Existing user ─────────────────────────────────────────
                referralCode = snapshot.GetValue<string>("referralCode");
                tickets = snapshot.GetValue<int>("tickets");

                Debug.Log($"Loaded referral data — code: {referralCode}, tickets: {tickets}");
            }
            else
            {
                // ── First-time user: create their document ─────────────────
                referralCode = GenerateReferralCode(uid);
                tickets = 0;

                await docRef.SetAsync(new System.Collections.Generic.Dictionary<string, object>
                {
                    { "referralCode", referralCode },
                    { "tickets",      tickets      },
                    { "uid",          uid          },
                    { "createdAt",    FieldValue.ServerTimestamp }
                });

                Debug.Log($"Created new user document — code: {referralCode}");
            }

            // ── Push to UI (must run on main thread) ──────────────────────
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (referralUI != null)
                {
                    referralUI.SetReferralCode(referralCode);
                    referralUI.SetTicketCount(tickets);
                }
            });
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Firestore read failed: " + ex.Message);
        }
    }

    // ── AWARD TICKETS WHEN A FRIEND USES YOUR CODE ───────────────────────────

    /// <summary>
    /// Call this after a new user signs up with a referral code.
    /// Adds 3 tickets to the referrer's document and 1 to the new user's.
    ///
    /// Usage:
    ///   await FirebaseReferralConnector.AwardReferralTickets(db, "9TEAE", newUserUid);
    /// </summary>
    public static async Task AwardReferralTickets(
        FirebaseFirestore firestore,
        string referralCode,
        string newUserUid)
    {
        try
        {
            // Find the referrer by their code
            QuerySnapshot query = await firestore
                .Collection("users")
                .WhereEqualTo("referralCode", referralCode)
                .Limit(1)
                .GetSnapshotAsync();

            if (query.Count == 0)
            {
                Debug.LogWarning("No user found with referral code: " + referralCode);
                return;
            }

            DocumentSnapshot referrerDoc = query.Documents[0];
            string referrerUid = referrerDoc.Id;

            // Batch write: +3 tickets to referrer, +1 to new user
            WriteBatch batch = firestore.StartBatch();

            batch.Update(firestore.Collection("users").Document(referrerUid),
                new System.Collections.Generic.Dictionary<string, object>
                { { "tickets", FieldValue.Increment(3) } });

            batch.Update(firestore.Collection("users").Document(newUserUid),
                new System.Collections.Generic.Dictionary<string, object>
                { { "tickets",    FieldValue.Increment(1) },
                  { "referredBy", referralCode            } });

            await batch.CommitAsync();
            Debug.Log($"Referral awarded: +3 to {referrerUid}, +1 to {newUserUid}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Referral award failed: " + ex.Message);
        }
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a short 5-character code from the user's UID.
    /// Always uppercase letters + numbers, no ambiguous chars (0, O, I, 1).
    /// </summary>
    static string GenerateReferralCode(string uid)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var sb = new System.Text.StringBuilder();
        int hash = Mathf.Abs(uid.GetHashCode());
        for (int i = 0; i < 5; i++)
        {
            sb.Append(chars[hash % chars.Length]);
            hash /= chars.Length;
        }
        return sb.ToString();
    }

    void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;
    }
}*/