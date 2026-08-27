using UnityEngine;
public static class LocalSaveStore
{
    const string Prefix = "localsave_";

    static string Key(string uid) => Prefix + (string.IsNullOrEmpty(uid) ? "anon" : uid);

    public static void Save(string uid, string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        PlayerPrefs.SetString(Key(uid), json);
        PlayerPrefs.Save();
    }

    public static bool Has(string uid) => PlayerPrefs.HasKey(Key(uid));

    public static string Load(string uid) =>
        Has(uid) ? PlayerPrefs.GetString(Key(uid)) : null;

    public static void Clear(string uid)
    {
        if (!Has(uid)) return;
        PlayerPrefs.DeleteKey(Key(uid));
        PlayerPrefs.Save();
    }

    public static long GetSavedAt(string json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        var data = JsonUtility.FromJson<GameSaveData>(json);
        return data != null ? data.lastSavedAt : 0;
    }
}
