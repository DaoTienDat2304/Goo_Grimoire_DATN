// ============================================================
// AuthManager.cs
//
//                           "Google Sign-In for Unity" plugin)
//
// ============================================================

using System;
using System.Collections;
using UnityEngine;

#if FIREBASE_AUTH
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
#endif

#if GOOGLE_SIGN_IN
using Google;
#endif

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    public bool   IsLoggedIn    { get; private set; }
    public bool   IsAnonymous   { get; private set; }
    public string CurrentUserId { get; private set; }
    public string DisplayName   { get; private set; }
    public string Email         { get; private set; }
    public string LocalSaveId =>
        (IsAnonymous || string.IsNullOrEmpty(CurrentUserId)) ? "guest" : CurrentUserId;

    // ── Events ───────────────────────────────────────────────
    public Action<string> OnLoginSuccess;
    public Action<string> OnLoginFailed;
    /// Ban TRUOC khi phien Firebase bi xoa, luc uid va token con dung.
    /// Cho ai con du lieu chua ghi kip day not xuong save cua tai khoan dang choi.
    public Action         OnBeforeSignOut;
    public Action         OnLoggedOut;
    public Action<string> OnPasswordResetSent;
    public Action<string> OnPasswordResetFailed;

#if FIREBASE_AUTH
    FirebaseAuth _auth;
    bool _firebaseReady;
#endif

    // ── Lifecycle ────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

#if FIREBASE_AUTH
        InitFirebase();
#else
        Debug.Log("[Auth] Offline dev mode: fake uid.");
        ApplyOfflineUser();
#endif
    }

#if FIREBASE_AUTH
    // ── Firebase init ────────────────────────────────────────
    void InitFirebase()
    {
        StartCoroutine(WaitForFirebaseAndInit());
    }

    IEnumerator WaitForFirebaseAndInit()
    {
        Debug.Log("[Auth] Waiting for Firebase...");

        if (RemoteConfigManager.Instance == null)
        {
            Debug.Log("[Auth] RemoteConfigManager missing — checking dependencies.");
            bool done = false;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.Result != DependencyStatus.Available)
                    Debug.LogError($"[Auth] Firebase init failed: {task.Result}");
                else
                    FinishFirebaseInit();
                done = true;
            });
            yield return new UnityEngine.WaitUntil(() => done);
            yield break;
        }

        yield return new UnityEngine.WaitUntil(() => RemoteConfigManager.Instance.IsFirebaseReady);

        FinishFirebaseInit();
    }

    void FinishFirebaseInit()
    {
        _auth = FirebaseAuth.DefaultInstance;
        _firebaseReady = true;
        Debug.Log("[Auth] ✓ Firebase Auth ready.");

        if (_auth.CurrentUser != null)
        {
            Debug.Log($"[Auth] Existing session: {_auth.CurrentUser.UserId}");
            ApplyUser(_auth.CurrentUser);
        }
        else
        {
            Debug.Log("[Auth] Not logged in. Show login.");
        }
    }

    // ── Sign-in methods ──────────────────────────────────────
    public void SignInAnonymously()
    {
        if (!_firebaseReady) { OnLoginFailed?.Invoke("Firebase not ready."); return; }
        Debug.Log("[Auth] Signing in anonymously...");

        _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = task.Exception?.InnerException?.Message ?? "Unknown error";
                Debug.LogWarning($"[Auth] ✗ Anonymous login failed: {err}");
                OnLoginFailed?.Invoke(err);
                return;
            }
            Debug.Log("[Auth] ✓ Anonymous login OK.");
            FirebaseAnalyticsManager.LogLogin("anonymous");
            ApplyUser(task.Result.User);
        });
    }

    public void SignInWithEmail(string email, string password)
    {
        if (!_firebaseReady) { OnLoginFailed?.Invoke("Firebase not ready."); return; }
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            OnLoginFailed?.Invoke("Email and password required.");
            return;
        }
        Debug.Log($"[Auth] Email login: {email}");

        _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = ParseFirebaseError(task.Exception);
                Debug.LogWarning($"[Auth] ✗ Email login failed: {err}");
                OnLoginFailed?.Invoke(err);
                return;
            }
            Debug.Log($"[Auth] ✓ Email login OK: {email}");
            FirebaseAnalyticsManager.LogLogin("email");
            ApplyUser(task.Result.User);
        });
    }

    public void RegisterWithEmail(string email, string password)
    {
        if (!_firebaseReady) { OnLoginFailed?.Invoke("Firebase not ready."); return; }
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            OnLoginFailed?.Invoke("Email and password required.");
            return;
        }
        Debug.Log($"[Auth] Registering: {email}");

        _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = ParseFirebaseError(task.Exception);
                Debug.LogWarning($"[Auth] ✗ Register failed: {err}");
                OnLoginFailed?.Invoke(err);
                return;
            }
            Debug.Log($"[Auth] ✓ Register OK: {email}");
            FirebaseAnalyticsManager.LogSignUp("email");
            FirebaseAnalyticsManager.LogLogin("email");
            ApplyUser(task.Result.User);
        });
    }

#if GOOGLE_SIGN_IN
    public void SignInWithGoogle()
    {
        if (!_firebaseReady) { OnLoginFailed?.Invoke("Firebase not ready."); return; }

#if UNITY_EDITOR
        Debug.LogWarning("[Auth] Google Sign-In unsupported trong Unity Editor.");
        OnLoginFailed?.Invoke("Google Sign-In unsupported trong here.");
#elif UNITY_ANDROID || UNITY_IOS
        Debug.Log("[Auth] Google login (mobile)...");
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            RequestIdToken = true,
            WebClientId = "588098512233-fkt9odnls27hrla0u31t48lqcr9d72m0.apps.googleusercontent.com"
        };
        SignInWithGoogleMobileAsync();
#elif UNITY_STANDALONE
        Debug.Log("[Auth] Google login (desktop PKCE)...");
        SignInWithGoogleDesktopAsync();
#else
        Debug.LogWarning("[Auth] Google Sign-In unsupported on this platform.");
        OnLoginFailed?.Invoke("Google Sign-In supports Android, iOS, Windows, Mac, Linux.");
#endif
    }

#if UNITY_ANDROID || UNITY_IOS
    async void SignInWithGoogleMobileAsync()
    {
        try
        {
            var googleUser = await GoogleSignIn.DefaultInstance.SignIn();
            var credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
            var result     = await _auth.SignInAndRetrieveDataWithCredentialAsync(credential);
            Debug.Log("[Auth] ✓ Google login OK.");
            FirebaseAnalyticsManager.LogLogin("google");
            ApplyUser(result.User);
        }
        catch (Exception ex)
        {
            string err = ParseFirebaseError(ex);
            Debug.LogWarning($"[Auth] ✗ Google Sign-In mobile failed: {err}");
            OnLoginFailed?.Invoke(err);
        }
    }
#endif

#if UNITY_STANDALONE
    async void SignInWithGoogleDesktopAsync()
    {
        try
        {
            string idToken = await GoogleSignInDesktop.GetIdTokenAsync();
            var credential = GoogleAuthProvider.GetCredential(idToken, null);
            var result     = await _auth.SignInWithCredentialAsync(credential);
            Debug.Log("[Auth] ✓ Google desktop login OK.");
            FirebaseAnalyticsManager.LogLogin("google");
            ApplyUser(result);
        }
        catch (Exception ex)
        {
            string err = ParseFirebaseError(ex);
            Debug.LogWarning($"[Auth] ✗ Google Sign-In desktop failed: {err}");
            OnLoginFailed?.Invoke(err);
        }
    }
#endif

#else
    public void SignInWithGoogle()
    {
        Debug.LogWarning("[Auth] Google Sign-In not installed. Add define symbol GOOGLE_SIGN_IN and import plugin.");
        OnLoginFailed?.Invoke("Google Sign-In not installed.");
    }
#endif

    public void SendPasswordResetEmail(string email)
    {
        if (!_firebaseReady) { OnPasswordResetFailed?.Invoke("Firebase not ready."); return; }
        if (string.IsNullOrEmpty(email))
        {
            OnPasswordResetFailed?.Invoke("Email required.");
            return;
        }
        Debug.Log($"[Auth] Sending reset email to: {email}");

        _auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = ParseFirebaseError(task.Exception);
                Debug.LogWarning($"[Auth] ✗ Reset email failed: {err}");
                OnPasswordResetFailed?.Invoke(err);
                return;
            }
            Debug.Log($"[Auth] ✓ Reset email sent to: {email}");
            OnPasswordResetSent?.Invoke(email);
        });
    }

    public void SignOut()
    {
        if (!_firebaseReady || _auth == null) return;

        // Phai chay truoc SignOut: sau do CurrentUserId ve null va token het hieu luc,
        // khong con day duoc save cua tai khoan nay len Firestore nua.
        OnBeforeSignOut?.Invoke();

        _auth.SignOut();
        IsLoggedIn    = false;
        IsAnonymous   = false;
        CurrentUserId = null;
        DisplayName   = null;
        Email         = null;
        Debug.Log("[Auth] Signed out.");
        FirebaseAnalyticsManager.LogLogout();
        OnLoggedOut?.Invoke();
    }

    // ── Helpers ──────────────────────────────────────────────
    void ApplyUser(FirebaseUser user)
    {
        IsLoggedIn    = true;
        IsAnonymous   = user.IsAnonymous;
        CurrentUserId = user.UserId;
        DisplayName   = string.IsNullOrEmpty(user.DisplayName) ? (user.IsAnonymous ? "Guest" : user.Email) : user.DisplayName;
        Email         = user.Email;

        Debug.Log($"[Auth] User: uid={CurrentUserId} | name={DisplayName} | anonymous={IsAnonymous}");
        OnLoginSuccess?.Invoke(CurrentUserId);
    }

    string ParseFirebaseError(Exception ex)
    {
        var inner = ex?.InnerException ?? ex;
        if (inner is FirebaseException firebaseEx)
        {
            return firebaseEx.ErrorCode switch
            {
                // Auth error codes
                17011 => "Email not found.",
                17009 => "Wrong password.",
                17007 => "Email in use.",
                17026 => "Password needs 6+ chars.",
                17010 => "Account temporarily locked.",
                _     => firebaseEx.Message
            };
        }
        return inner?.Message ?? "Unknown error";
    }

#else
    // ── Offline dev mode ─────────────────────────────────────
    void ApplyOfflineUser()
    {
        IsLoggedIn    = true;
        IsAnonymous   = false;
        CurrentUserId = "offline_dev_user";
        DisplayName   = "Dev";
        Email         = "dev@local";
        OnLoginSuccess?.Invoke(CurrentUserId);
    }

    public void SignInAnonymously()   => ApplyOfflineUser();
    public void SignInWithGoogle()    => ApplyOfflineUser();
    public void SignOut()             { OnBeforeSignOut?.Invoke(); IsLoggedIn = false; OnLoggedOut?.Invoke(); }
    public void SignInWithEmail(string email, string password) => ApplyOfflineUser();
    public void RegisterWithEmail(string email, string password) => ApplyOfflineUser();
    public void SendPasswordResetEmail(string email) => OnPasswordResetSent?.Invoke(email);
#endif
}
