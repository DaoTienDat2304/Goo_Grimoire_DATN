using System;

/// <summary>
/// Bọc game JSON với chữ ký HMAC-SHA256 trước khi lưu lên Firestore.
/// Firestore field "json" chứa serialized SaveEnvelope thay vì raw JSON.
/// </summary>
[Serializable]
public class SaveEnvelope
{
    /// <summary>Game save JSON gốc (GameSaveData serialized).</summary>
    public string payload;

    /// <summary>HMAC-SHA256 của payload, key = SHA256(uid + salt). Base64 encoded.</summary>
    public string sig;

    /// <summary>Versioning cho format envelope — dùng khi cần migrate sau này.</summary>
    public int schemaVersion = 1;
}
