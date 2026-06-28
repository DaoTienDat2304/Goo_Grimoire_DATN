using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Ký và xác minh save data bằng HMAC-SHA256.
///
/// Key derivation: SHA256(uid + salt)
///   — uid   : ràng buộc chữ ký với tài khoản, ngăn copy save giữa account
///   — salt  : lấy từ Firebase Remote Config ("save_hmac_salt"), fallback hardcode
///
/// Threat model: ngăn casual cheater chỉnh sửa document trực tiếp trên Firestore.
/// Không bảo vệ khỏi attacker reverse-engineer APK để lấy fallback salt.
/// </summary>
public static class SaveIntegrity
{
    // Dùng khi RemoteConfig chưa sẵn sàng hoặc không có FIREBASE_REMOTE_CONFIG.
    // Giá trị thực sự bảo mật nằm trên Firebase Remote Config.
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

    /// <summary>Tạo chữ ký HMAC-SHA256 cho payload.</summary>
    public static string Sign(string payload, string uid)
    {
        var key = DeriveKey(uid, GetSalt());
        using var hmac = new HMACSHA256(key);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    /// <summary>
    /// Xác minh chữ ký. Trả về false nếu sig rỗng hoặc không khớp.
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
