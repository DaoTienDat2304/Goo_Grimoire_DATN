// ============================================================
// AuthManager.cs
//
// Define symbols cần có:
//   FIREBASE_AUTH         — bật Firebase Authentication
//   GOOGLE_SIGN_IN        — bật Google Sign-In (cần cài thêm
//                           "Google Sign-In for Unity" plugin)
//
// Không có symbol → game compile bình thường, IsLoggedIn = true
// với uid giả (offline dev mode).
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

    // ── Trạng thái ──────────────────────────────────────────
    public bool   IsLoggedIn    { get; private set; }
    public bool   IsAnonymous   { get; private set; }
    public string CurrentUserId { get; private set; }
    public string DisplayName   { get; private set; }
    public string Email         { get; private set; }

    /// <summary>
    /// Khóa dùng cho save CỤC BỘ (PlayerPrefs), ổn định theo máy.
    /// Guest (đăng nhập ẩn danh) dùng "guest" cố định vì Firebase cấp uid mới mỗi
    /// phiên đăng nhập ẩn danh — nếu key theo uid thì save cũ không bao giờ khớp,
    /// khiến lần nào cũng bị coi là game mới. Tài khoản thật (email/google) dùng uid.
    /// </summary>
    public string LocalSaveId =>
        (IsAnonymous || string.IsNullOrEmpty(CurrentUserId)) ? "guest" : CurrentUserId;

    // ── Events ───────────────────────────────────────────────
    /// <summary>Login thành công — truyền uid.</summary>
    public Action<string> OnLoginSuccess;
    /// <summary>Login thất bại — truyền thông báo lỗi.</summary>
    public Action<string> OnLoginFailed;
    public Action         OnLoggedOut;
    /// <summary>Gửi email đặt lại mật khẩu thành công — truyền địa chỉ email.</summary>
    public Action<string> OnPasswordResetSent;
    /// <summary>Gửi email đặt lại mật khẩu thất bại — truyền thông báo lỗi.</summary>
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
        // Dev mode không có Firebase — giả lập user để game chạy được
        Debug.Log("[Auth] Offline dev mode — dùng uid giả.");
        ApplyOfflineUser();
#endif
    }

#if FIREBASE_AUTH
    // ── Firebase init ────────────────────────────────────────
    void InitFirebase()
    {
        // RemoteConfigManager đã gọi CheckAndFixDependenciesAsync() rồi.
        // Gọi lại lần hai song song trên Windows sẽ làm callback không bao giờ chạy.
        // Chờ RemoteConfigManager hoàn tất rồi lấy FirebaseAuth trực tiếp.
        StartCoroutine(WaitForFirebaseAndInit());
    }

    IEnumerator WaitForFirebaseAndInit()
    {
        Debug.Log("[Auth] Đang chờ Firebase khởi tạo xong...");

        // Nếu RemoteConfigManager không có trong scene, fallback về CheckAndFixDependencies
        if (RemoteConfigManager.Instance == null)
        {
            Debug.Log("[Auth] RemoteConfigManager chưa có — tự kiểm tra dependencies.");
            bool done = false;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.Result != DependencyStatus.Available)
                    Debug.LogError($"[Auth] Firebase không khởi động được: {task.Result}");
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
            Debug.Log($"[Auth] Tìm thấy session cũ: {_auth.CurrentUser.UserId}");
            ApplyUser(_auth.CurrentUser);
        }
        else
        {
            Debug.Log("[Auth] Chưa login — cần hiển thị màn đăng nhập.");
        }
    }

    // ── Sign-in methods ──────────────────────────────────────
    public void SignInAnonymously()
    {
        if (!_firebaseReady) { OnLoginFailed?.Invoke("Firebase chưa sẵn sàng."); return; }
        Debug.Log("[Auth] Đang đăng nhập ẩn danh...");

        _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = task.Exception?.InnerException?.Message ?? "Lỗi không xác định";
                Debug.LogWarning($"[Auth] ✗ Đăng nhập ẩn danh thất bại: {err}");
                OnLoginFailed?.Invoke(err);
                return;
            }
            Debug.Log("[Auth] ✓ Đăng nhập ẩn danh thành công.");
            FirebaseAnalyticsManager.LogLogin("anonymous");
            ApplyUser(task.Result.User);
        });
    }

    public void SignInWithEmail(string email, string password)
    {
        if (!_firebaseReady) { OnLoginFailed?.Invoke("Firebase chưa sẵn sàng."); return; }
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            OnLoginFailed?.Invoke("Email và mật khẩu không được để trống.");
            return;
        }
        Debug.Log($"[Auth] Đang đăng nhập email: {email}");

        _auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = ParseFirebaseError(task.Exception);
                Debug.LogWarning($"[Auth] ✗ Đăng nhập email thất bại: {err}");
                OnLoginFailed?.Invoke(err);
                return;
            }
            Debug.Log($"[Auth] ✓ Đăng nhập email thành công: {email}");
            FirebaseAnalyticsManager.LogLogin("email");
            ApplyUser(task.Result.User);
        });
    }

    public void RegisterWithEmail(string email, string password)
    {
        if (!_firebaseReady) { OnLoginFailed?.Invoke("Firebase chưa sẵn sàng."); return; }
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            OnLoginFailed?.Invoke("Email và mật khẩu không được để trống.");
            return;
        }
        Debug.Log($"[Auth] Đang đăng ký tài khoản: {email}");

        _auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = ParseFirebaseError(task.Exception);
                Debug.LogWarning($"[Auth] ✗ Đăng ký thất bại: {err}");
                OnLoginFailed?.Invoke(err);
                return;
            }
            Debug.Log($"[Auth] ✓ Đăng ký thành công: {email}");
            FirebaseAnalyticsManager.LogSignUp("email");
            FirebaseAnalyticsManager.LogLogin("email");
            ApplyUser(task.Result.User);
        });
    }

#if GOOGLE_SIGN_IN
    public void SignInWithGoogle()
    {
        if (!_firebaseReady) { OnLoginFailed?.Invoke("Firebase chưa sẵn sàng."); return; }

#if UNITY_EDITOR
        Debug.LogWarning("[Auth] Google Sign-In không hỗ trợ trong Unity Editor.");
        OnLoginFailed?.Invoke("Google Sign-In không hỗ trợ trong môi trường này.");
#elif UNITY_ANDROID || UNITY_IOS
        Debug.Log("[Auth] Đang đăng nhập Google (mobile)...");
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            RequestIdToken = true,
            // OAuth client_id từ google-services.json → oauth_client[client_type=3].client_id
            WebClientId = "588098512233-fkt9odnls27hrla0u31t48lqcr9d72m0.apps.googleusercontent.com"
        };
        SignInWithGoogleMobileAsync();
#elif UNITY_STANDALONE
        Debug.Log("[Auth] Đang đăng nhập Google (desktop PKCE)...");
        SignInWithGoogleDesktopAsync();
#else
        Debug.LogWarning("[Auth] Google Sign-In không hỗ trợ trên platform này.");
        OnLoginFailed?.Invoke("Google Sign-In chỉ hỗ trợ Android, iOS và Windows/Mac/Linux.");
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
            Debug.Log("[Auth] ✓ Đăng nhập Google thành công.");
            FirebaseAnalyticsManager.LogLogin("google");
            ApplyUser(result.User);
        }
        catch (Exception ex)
        {
            string err = ParseFirebaseError(ex);
            Debug.LogWarning($"[Auth] ✗ Google Sign-In (mobile) thất bại: {err}");
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
            Debug.Log("[Auth] ✓ Đăng nhập Google (desktop) thành công.");
            FirebaseAnalyticsManager.LogLogin("google");
            ApplyUser(result);
        }
        catch (Exception ex)
        {
            string err = ParseFirebaseError(ex);
            Debug.LogWarning($"[Auth] ✗ Google Sign-In (desktop) thất bại: {err}");
            OnLoginFailed?.Invoke(err);
        }
    }
#endif

#else
    public void SignInWithGoogle()
    {
        Debug.LogWarning("[Auth] Google Sign-In chưa được cài đặt. Thêm define symbol GOOGLE_SIGN_IN và import plugin.");
        OnLoginFailed?.Invoke("Google Sign-In chưa được cài đặt.");
    }
#endif

    public void SendPasswordResetEmail(string email)
    {
        if (!_firebaseReady) { OnPasswordResetFailed?.Invoke("Firebase chưa sẵn sàng."); return; }
        if (string.IsNullOrEmpty(email))
        {
            OnPasswordResetFailed?.Invoke("Email không được để trống.");
            return;
        }
        Debug.Log($"[Auth] Đang gửi email đặt lại mật khẩu tới: {email}");

        _auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string err = ParseFirebaseError(task.Exception);
                Debug.LogWarning($"[Auth] ✗ Gửi email đặt lại mật khẩu thất bại: {err}");
                OnPasswordResetFailed?.Invoke(err);
                return;
            }
            Debug.Log($"[Auth] ✓ Đã gửi email đặt lại mật khẩu tới: {email}");
            OnPasswordResetSent?.Invoke(email);
        });
    }

    public void SignOut()
    {
        if (!_firebaseReady || _auth == null) return;
        _auth.SignOut();
        IsLoggedIn    = false;
        IsAnonymous   = false;
        CurrentUserId = null;
        DisplayName   = null;
        Email         = null;
        Debug.Log("[Auth] Đã đăng xuất.");
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
                17011 => "Email không tồn tại.",
                17009 => "Mật khẩu không đúng.",
                17007 => "Email đã được sử dụng.",
                17026 => "Mật khẩu phải có ít nhất 6 ký tự.",
                17010 => "Tài khoản bị khóa tạm thời do đăng nhập sai nhiều lần.",
                _     => firebaseEx.Message
            };
        }
        return inner?.Message ?? "Lỗi không xác định";
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
    public void SignOut()             { IsLoggedIn = false; OnLoggedOut?.Invoke(); }
    public void SignInWithEmail(string email, string password) => ApplyOfflineUser();
    public void RegisterWithEmail(string email, string password) => ApplyOfflineUser();
    public void SendPasswordResetEmail(string email) => OnPasswordResetSent?.Invoke(email);
#endif
}
