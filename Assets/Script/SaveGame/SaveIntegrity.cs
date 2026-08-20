using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
///
/// Key derivation: SHA256(uid + salt)
///
/// </summary>
public static class SaveIntegrity
{
    const string FallbackSalt = "GooGrimoire_HmacFallback_v1";

    static string GetSalt()
    {
        var rc = RemoteConfigManager.Instance;
        if (rc != null && rc.IsReady)
        {
            var salt = rc.SaveHmacSalt;
            if (!string.IsNullOrEmpty(salt)) return salt;
        }
        return FallbackSalt;
    }

    public static string Sign(string payload, string uid)
    {
        var key = DeriveKey(uid, GetSalt());
        using var hmac = new HMACSHA256(key);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    /// <summary>
    /// </summary>
    public static bool Verify(string payload, string uid, string sig)
    {
        if (string.IsNullOrEmpty(sig)) return false;
        return Sign(payload, uid) == sig;
    }

    static byte[] DeriveKey(string uid, string salt)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(uid + salt));
    }
}
