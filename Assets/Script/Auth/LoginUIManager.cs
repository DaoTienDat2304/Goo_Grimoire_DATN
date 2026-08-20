// ============================================================
// LoginUIManager.cs
//
// Setup trong Inspector:
//   emailField          — InputField cho email
//   passwordField       — InputField cho password
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
    public Text       forgotStatusText;

    [Header("Texts")]
    public Text errorText;
    public Text userInfoText;

    [Header("Buttons")]
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
            Debug.LogWarning("[LoginUI] AuthManager missing trong scene.");
            return;
        }

        AuthManager.Instance.OnLoginSuccess      += HandleLoginSuccess;
        AuthManager.Instance.OnLoginFailed       += HandleLoginFailed;
        AuthManager.Instance.OnLoggedOut         += HandleLoggedOut;
        AuthManager.Instance.OnPasswordResetSent += HandlePasswordResetSent;
        AuthManager.Instance.OnPasswordResetFailed += HandlePasswordResetFailed;

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
            userInfoText.text = $"Hi, {AuthManager.Instance.DisplayName}";
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
            if (forgotStatusText != null) forgotStatusText.text = "Enter email.";
            return;
        }
        if (forgotStatusText != null) forgotStatusText.text = "Sending...";
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
        Debug.Log($"[LoginUI] Login OK: {uid}");
        HideAllPanels();
    }

    void HandleLoginFailed(string error)
    {
        Debug.LogWarning($"[LoginUI] Login failed: {error}");
        ShowEmailPanel();
        ShowError(error);
    }

    void HandleLoggedOut()
    {
        ShowLoginPanel();
    }

    void HandlePasswordResetSent(string email)
    {
        Debug.Log($"[LoginUI] Reset email sent to: {email}");
        if (forgotStatusText != null)
            forgotStatusText.text = $"Sent. Check inbox: {email}";
    }

    void HandlePasswordResetFailed(string error)
    {
        Debug.LogWarning($"[LoginUI] Reset email failed: {error}");
        if (forgotStatusText != null)
            forgotStatusText.text = error;
    }
}
