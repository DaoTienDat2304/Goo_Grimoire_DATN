// ============================================================
// LoginUIManager.cs
//
// Đặt script này vào scene "menu".
// Setup trong Inspector:
//   loginPanel          — panel chứa 3 nút: Google, Email, Guest
//   emailPanel          — panel nhập email + password
//   forgotPasswordPanel — panel nhập email để đặt lại mật khẩu
//   loadingPanel        — hiển thị khi đang chờ auth
//   errorText           — Text hiển thị lỗi (trong emailPanel)
//   emailField          — InputField cho email
//   passwordField       — InputField cho password
//   forgotEmailField    — InputField nhập email trong forgotPasswordPanel
//   forgotStatusText    — Text hiển thị kết quả gửi email đặt lại mật khẩu
//   userInfoText        — Text hiện tên user sau khi login (optional)
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoginUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject emailPanel;
    public GameObject forgotPasswordPanel;
    public GameObject loadingPanel;

    [Header("Login Panel — Email Input")]
    public InputField emailField;
    public InputField passwordField;

    [Header("Login Panel — Forgot Password")]
    public InputField forgotEmailField;
    public Text       forgotStatusText;  // Hiển thị kết quả gửi ("Đã gửi!" hoặc lỗi)

    [Header("Texts")]
    public Text errorText;
    public Text userInfoText;  // Hiển thị "Xin chào, {DisplayName}" sau login

    [Header("Buttons — ẩn khi đang loading")]
    public Button googleButton;
    public Button guestButton;
    public Button emailPanelOpenButton;
    public Button signInButton;
    public Button registerButton;

    // ── Lifecycle ────────────────────────────────────────────
    void Start()
    {
        if (AuthManager.Instance == null)
        {
            Debug.LogWarning("[LoginUI] AuthManager chưa có trong scene.");
            return;
        }

        AuthManager.Instance.OnLoginSuccess      += HandleLoginSuccess;
        AuthManager.Instance.OnLoginFailed       += HandleLoginFailed;
        AuthManager.Instance.OnLoggedOut         += HandleLoggedOut;
        AuthManager.Instance.OnPasswordResetSent += HandlePasswordResetSent;
        AuthManager.Instance.OnPasswordResetFailed += HandlePasswordResetFailed;

        // Nếu đã có session từ lần trước → ẩn login panel ngay
        if (AuthManager.Instance.IsLoggedIn)
            HideAllPanels();
        else
            ShowLoginPanel();
    }

    void OnDestroy()
    {
        if (AuthManager.Instance == null) return;
        AuthManager.Instance.OnLoginSuccess        -= HandleLoginSuccess;
        AuthManager.Instance.OnLoginFailed         -= HandleLoginFailed;
        AuthManager.Instance.OnLoggedOut           -= HandleLoggedOut;
        AuthManager.Instance.OnPasswordResetSent   -= HandlePasswordResetSent;
        AuthManager.Instance.OnPasswordResetFailed -= HandlePasswordResetFailed;
    }

    // ── Panel helpers ─────────────────────────────────────────
    void ShowLoginPanel()
    {
        if (loginPanel          != null) loginPanel.SetActive(true);
        if (emailPanel          != null) emailPanel.SetActive(false);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        if (loadingPanel        != null) loadingPanel.SetActive(false);
        ClearError();
    }

    void ShowEmailPanel()
    {
        if (loginPanel          != null) loginPanel.SetActive(false);
        if (emailPanel          != null) emailPanel.SetActive(true);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        if (loadingPanel        != null) loadingPanel.SetActive(false);
        ClearError();
    }

    void ShowForgotPasswordPanel()
    {
        if (loginPanel          != null) loginPanel.SetActive(false);
        if (emailPanel          != null) emailPanel.SetActive(false);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(true);
        if (loadingPanel        != null) loadingPanel.SetActive(false);
        if (forgotStatusText    != null) forgotStatusText.text = "";
        // Điền sẵn email nếu người dùng đã nhập ở emailPanel
        if (forgotEmailField != null && emailField != null && string.IsNullOrEmpty(forgotEmailField.text))
            forgotEmailField.text = emailField.text;
    }

    void ShowLoading()
    {
        if (loginPanel          != null) loginPanel.SetActive(false);
        if (emailPanel          != null) emailPanel.SetActive(false);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        if (loadingPanel        != null) loadingPanel.SetActive(true);
        ClearError();
    }

    void HideAllPanels()
    {
        if (loginPanel          != null) loginPanel.SetActive(false);
        if (emailPanel          != null) emailPanel.SetActive(false);
        if (forgotPasswordPanel != null) forgotPasswordPanel.SetActive(false);
        if (loadingPanel        != null) loadingPanel.SetActive(false);

        if (userInfoText != null && AuthManager.Instance != null)
        {
            userInfoText.text = $"Xin chào, {AuthManager.Instance.DisplayName}";
            userInfoText.gameObject.SetActive(true);
            StartCoroutine(HideUserInfoAfterDelay(2f));
        }
    }

    IEnumerator HideUserInfoAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (userInfoText != null)
            userInfoText.gameObject.SetActive(false);
    }

    void ClearError()
    {
        if (errorText != null) errorText.text = "";
    }

    void ShowError(string message)
    {
        if (errorText != null) errorText.text = message;
    }

    // ── Button callbacks (kéo vào OnClick trong Inspector) ───
    public void OnGoogleSignIn()
    {
        ShowLoading();
        AuthManager.Instance.SignInWithGoogle();
    }

    public void OnGuestSignIn()
    {
        ShowLoading();
        AuthManager.Instance.SignInAnonymously();
    }

    public void OnOpenEmailPanel()
    {
        ShowEmailPanel();
    }

    public void OnBackToLoginPanel()
    {
        ShowLoginPanel();
    }

    public void OnOpenForgotPassword()
    {
        ShowForgotPasswordPanel();
    }

    public void OnBackFromForgotPassword()
    {
        ShowEmailPanel();
    }

    public void OnSendPasswordReset()
    {
        if (forgotEmailField == null) return;
        string email = forgotEmailField.text.Trim();
        if (string.IsNullOrEmpty(email))
        {
            if (forgotStatusText != null) forgotStatusText.text = "Vui lòng nhập email.";
            return;
        }
        if (forgotStatusText != null) forgotStatusText.text = "Đang gửi...";
        AuthManager.Instance.SendPasswordResetEmail(email);
    }

    public void OnSignIn()
    {
        if (emailField == null || passwordField == null) return;
        ShowLoading();
        AuthManager.Instance.SignInWithEmail(
            emailField.text.Trim(),
            passwordField.text
        );
    }

    public void OnRegister()
    {
        if (emailField == null || passwordField == null) return;
        ShowLoading();
        AuthManager.Instance.RegisterWithEmail(
            emailField.text.Trim(),
            passwordField.text
        );
    }

    public void OnLogout()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.SignOut();
    }

    // ── Auth event handlers ──────────────────────────────────
    void HandleLoginSuccess(string uid)
    {
        Debug.Log($"[LoginUI] Login thành công: {uid}");
        HideAllPanels();
    }

    void HandleLoginFailed(string error)
    {
        Debug.LogWarning($"[LoginUI] Login thất bại: {error}");
        ShowEmailPanel();
        ShowError(error);
    }

    void HandleLoggedOut()
    {
        ShowLoginPanel();
    }

    void HandlePasswordResetSent(string email)
    {
        Debug.Log($"[LoginUI] Đã gửi email đặt lại mật khẩu tới: {email}");
        if (forgotStatusText != null)
            forgotStatusText.text = $"Đã gửi! Kiểm tra hộp thư của {email}";
    }

    void HandlePasswordResetFailed(string error)
    {
        Debug.LogWarning($"[LoginUI] Gửi email đặt lại mật khẩu thất bại: {error}");
        if (forgotStatusText != null)
            forgotStatusText.text = error;
    }
}
