// GoogleSignInDesktop.cs
// OAuth2 Authorization Code + PKCE flow cho Windows/Mac/Linux.
// Không cần embed client_secret — PKCE đảm bảo bảo mật cho native apps.
//
// Bước chuẩn bị (1 lần):
//   Google Cloud Console → APIs & Services → Credentials
//   → Create Credentials → OAuth 2.0 Client ID → Desktop app
//   → Copy Client ID, paste vào DesktopClientId bên dưới.

#if UNITY_STANDALONE

using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class GoogleSignInDesktop
{
    // ── Điền Desktop OAuth2 Client ID + Secret từ Google Cloud Console vào đây ──
    // client_secret của Desktop app KHÔNG thực sự bí mật (Google biết điều này)
    // nhưng vẫn phải gửi trong token exchange request.
    const string DesktopClientId     = "1082236547825-c6i9iv5avnr05knnuqu82u8umu4r503n.apps.googleusercontent.com";
    const string DesktopClientSecret = "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";

    const string AuthEndpoint  = "https://accounts.google.com/o/oauth2/v2/auth";
    const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    const string Scope         = "openid email profile";
    const int    TimeoutSeconds = 120;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Mở browser để user đăng nhập Google, chờ callback, trả về Google id_token.
    /// Gọi từ main thread; await trên main thread (dùng ContinueWithOnMainThread phía ngoài).
    /// </summary>
    public static async Task<string> GetIdTokenAsync()
    {
        // 1. PKCE
        string codeVerifier  = GenerateCodeVerifier();
        string codeChallenge = GenerateCodeChallenge(codeVerifier);

        // 2. Port + redirect URI
        // Không có trailing slash — Google normalize bỏ slash khi verify redirect_uri
        int    port        = FindFreePort();
        string redirectUri = $"http://localhost:{port}";

        // 3. Mở browser
        string state   = Guid.NewGuid().ToString("N");
        string authUrl = BuildAuthUrl(redirectUri, codeChallenge, state);
        Application.OpenURL(authUrl);
        Debug.Log("[GoogleSignInDesktop] Mở browser để đăng nhập Google...");

        // 4. Chờ callback
        string code = await ListenForCodeAsync(port, state);
        if (string.IsNullOrEmpty(code))
            throw new Exception("Không nhận được authorization code (hết thời gian hoặc bị huỷ).");

        // 5. Đổi code → tokens
        string idToken = await ExchangeCodeForIdTokenAsync(code, codeVerifier, redirectUri);
        return idToken;
    }

    // ── Build auth URL ────────────────────────────────────────────────────

    static string BuildAuthUrl(string redirectUri, string codeChallenge, string state)
    {
        return $"{AuthEndpoint}" +
               $"?client_id={Uri.EscapeDataString(DesktopClientId)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
               $"&response_type=code" +
               $"&scope={Uri.EscapeDataString(Scope)}" +
               $"&code_challenge={codeChallenge}" +
               $"&code_challenge_method=S256" +
               $"&state={state}" +
               $"&access_type=online" +
               $"&prompt=select_account";
    }

    // ── Local HTTP listener ───────────────────────────────────────────────

    static async Task<string> ListenForCodeAsync(int port, string expectedState)
    {
        using var listener = new HttpListener();
        // HttpListener prefix bắt buộc có trailing slash; redirect_uri gửi Google thì không
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));

        try
        {
            // Chờ request từ browser trên thread pool (không block main thread)
            var contextTask = Task.Run(() => listener.GetContext(), cts.Token);
            var context     = await contextTask;

            string query = context.Request.Url?.Query ?? "";
            var    pairs  = ParseQueryString(query);

            // Serve trang thành công cho browser
            string html = "<html><body style='font-family:sans-serif;text-align:center;padding-top:80px'>" +
                          "<h2>&#10003; Đăng nhập thành công!</h2>" +
                          "<p>Bạn có thể đóng cửa sổ này và quay lại game.</p>" +
                          "</body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType     = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();

            if (pairs.TryGetValue("error", out string error))
            {
                Debug.LogWarning($"[GoogleSignInDesktop] Google trả về lỗi: {error}");
                return null;
            }

            if (!pairs.TryGetValue("state", out string returnedState) || returnedState != expectedState)
            {
                Debug.LogWarning("[GoogleSignInDesktop] State mismatch — có thể là CSRF attack.");
                return null;
            }

            pairs.TryGetValue("code", out string code);
            return code;
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[GoogleSignInDesktop] Hết {TimeoutSeconds}s chờ đăng nhập.");
            return null;
        }
        finally
        {
            listener.Stop();
        }
    }

    // ── Token exchange ────────────────────────────────────────────────────

    static async Task<string> ExchangeCodeForIdTokenAsync(string code, string codeVerifier, string redirectUri)
    {
        string body = $"code={Uri.EscapeDataString(code)}" +
                      $"&client_id={Uri.EscapeDataString(DesktopClientId)}" +
                      $"&client_secret={Uri.EscapeDataString(DesktopClientSecret)}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&code_verifier={Uri.EscapeDataString(codeVerifier)}" +
                      $"&grant_type=authorization_code";

        using var req = new UnityWebRequest(TokenEndpoint, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        // UnityWebRequest phải chạy trên main thread — dùng TaskCompletionSource
        var tcs = new TaskCompletionSource<string>();

        // Gửi request và chờ bằng cách poll (UnityWebRequest không hỗ trợ async/await native)
        var op = req.SendWebRequest();
        op.completed += _ =>
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                string body = req.downloadHandler?.text ?? "(empty)";
                Debug.LogError($"[GoogleSignInDesktop] Token exchange lỗi body: {body}");
                tcs.SetException(new Exception($"Token exchange thất bại: {req.error} | {body}"));
                return;
            }

            string json    = req.downloadHandler.text;
            var    wrapper = JsonUtility.FromJson<TokenResponse>(json);
            if (string.IsNullOrEmpty(wrapper?.id_token))
            {
                tcs.SetException(new Exception($"Không tìm thấy id_token trong response: {json}"));
                return;
            }

            tcs.SetResult(wrapper.id_token);
        };

        return await tcs.Task;
    }

    // ── PKCE helpers ──────────────────────────────────────────────────────

    static string GenerateCodeVerifier()
    {
        // RFC 7636: 43–128 ký tự, unreserved characters
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    static string GenerateCodeChallenge(string verifier)
    {
        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }

    static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    // ── Network helpers ───────────────────────────────────────────────────

    static int FindFreePort()
    {
        // Dùng OS để cấp port trống
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    static Dictionary<string, string> ParseQueryString(string query)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return dict;
        string raw = query.TrimStart('?');
        foreach (string pair in raw.Split('&'))
        {
            int idx = pair.IndexOf('=');
            if (idx < 0) continue;
            string key = Uri.UnescapeDataString(pair[..idx]);
            string val = Uri.UnescapeDataString(pair[(idx + 1)..]);
            dict[key] = val;
        }
        return dict;
    }

    // ── DTOs ──────────────────────────────────────────────────────────────

    [Serializable]
    class TokenResponse
    {
        public string access_token;
        public string id_token;
        public string token_type;
        public int    expires_in;
    }
}

#endif
