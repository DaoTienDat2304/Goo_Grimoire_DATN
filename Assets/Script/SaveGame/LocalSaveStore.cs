using UnityEngine;

/// <summary>
/// Lưu game JSON xuống máy bằng PlayerPrefs để dữ liệu KHÔNG mất khi thoát game
/// hoặc restart Play Mode — đặc biệt ở offline dev mode khi không có cloud save.
///
/// Key tách theo uid để không lẫn save giữa các tài khoản. Đây là bản local mirror
/// của cloud save: mỗi lần SaveAndLoadSystem.Save() chạy đều ghi xuống đây.
/// </summary>
public static class LocalSaveStore
{
    const string Prefix = "localsave_";

    static string Key(string uid) => Prefix + (string.IsNullOrEmpty(uid) ? "anon" : uid);

    /// <summary>Ghi game JSON xuống PlayerPrefs cho uid này.</summary>
    public static void Save(string uid, string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        PlayerPrefs.SetString(Key(uid), json);
        PlayerPrefs.Save();
        Debug.Log($"[LocalSave] Đã lưu PlayerPrefs cho uid={uid}");
    }

    public static bool Has(string uid) => PlayerPrefs.HasKey(Key(uid));

    /// <summary>Đọc game JSON đã lưu cục bộ; null nếu chưa có.</summary>
    public static string Load(string uid) =>
        Has(uid) ? PlayerPrefs.GetString(Key(uid)) : null;

    /// <summary>Xóa save cục bộ của uid (vd: khi reset tài khoản).</summary>
    public static void Clear(string uid)
    {
        if (!Has(uid)) return;
        PlayerPrefs.DeleteKey(Key(uid));
        PlayerPrefs.Save();
        Debug.Log($"[LocalSave] Đã xóa PlayerPrefs cho uid={uid}");
    }

    /// <summary>Đọc nhanh lastSavedAt để so sánh save nào mới hơn (cloud vs local).</summary>
    public static long GetSavedAt(string json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        var data = JsonUtility.FromJson<GameSaveData>(json);
        return data != null ? data.lastSavedAt : 0;
    }
}
