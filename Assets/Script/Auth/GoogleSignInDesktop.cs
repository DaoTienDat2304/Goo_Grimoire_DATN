// GoogleSignInDesktop.cs
// OAuth2 Authorization Code + PKCE flow cho Windows/Mac/Linux.
//
//   Google Cloud Console → APIs & Services → Credentials
//   → Create Credentials → OAuth 2.0 Client ID → Desktop app

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
    const string DesktopClientId     = "1082236547825-c6i9iv5avnr05knnuqu82u8umu4r503n.apps.googleusercontent.com";
    const string DesktopClientSecret = "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";

    const string AuthEndpoint  = "https://accounts.google.com/o/oauth2/v2/auth";
    const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    const string Scope         = "openid email profile";
    const int    TimeoutSeconds = 120;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// </summary>
    public static async Task<string> GetIdTokenAsync()
    {
        // 1. PKCE
        string codeVerifier  = GenerateCodeVerifier();
        string codeChallenge = GenerateCodeChallenge(codeVerifier);

        // 2. Port + redirect URI
        int    port        = FindFreePort();
        string redirectUri = $"http://localhost:{port}";

        string state   = Guid.NewGuid().ToString("N");
        string authUrl = BuildAuthUrl(redirectUri, codeChallenge, state);
        Application.OpenURL(authUrl);
        Debug.Log("[GoogleSignInDesktop] Opening browser for Google login...");

        string code = await ListenForCodeAsync(port, state);
        if (string.IsNullOrEmpty(code))
            throw new Exception("No authorization code.");

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
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutSeconds));

        try
        {
            var contextTask = Task.Run(() => listener.GetContext(), cts.Token);
            var context     = await contextTask;

            string query = context.Request.Url?.Query ?? "";
            var    pairs  = ParseQueryString(query);

            string html = "<html><body style='font-family:sans-serif;text-align:center;padding-top:80px'>" +
                          "<h2>&#10003; Login successful!</h2>" +
                          "<p>Close this window and return to the game.</p>" +
                          "</body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType     = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();

            if (pairs.TryGetValue("error", out string error))
            {
                Debug.LogWarning($"[GoogleSignInDesktop] Google returned error: {error}");
                return null;
            }

            if (!pairs.TryGetValue("state", out string returnedState) || returnedState != expectedState)
            {
                Debug.LogWarning("[GoogleSignInDesktop] State mismatch — yes the la CSRF attack.");
                return null;
            }

            pairs.TryGetValue("code", out string code);
            return code;
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning($"[GoogleSignInDesktop] Timed out after {TimeoutSeconds}s waiting for login.");
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

        var tcs = new TaskCompletionSource<string>();

        var op = req.SendWebRequest();
        op.completed += _ =>
        {
            if (req.result != UnityWebRequest.Result.Success)
            {
                string body = req.downloadHandler?.text ?? "(empty)";
                Debug.LogError($"[GoogleSignInDesktop] Token exchange body error: {body}");
                tcs.SetException(new Exception($"Token exchange failed: {req.error} | {body}"));
                return;
            }

            string json    = req.downloadHandler.text;
            var    wrapper = JsonUtility.FromJson<TokenResponse>(json);
            if (string.IsNullOrEmpty(wrapper?.id_token))
            {
                tcs.SetException(new Exception($"id_token not found in response: {json}"));
                return;
            }

            tcs.SetResult(wrapper.id_token);
        };

        return await tcs.Task;
    }

    // ── PKCE helpers ──────────────────────────────────────────────────────

    static string GenerateCodeVerifier()
    {
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
