using System.Collections.Generic;
using UnityEngine;

#if FIREBASE_REMOTE_CONFIG
using Firebase;
using Firebase.RemoteConfig;
using Firebase.Extensions;
#endif

public class RemoteConfigManager : MonoBehaviour
{
    public static RemoteConfigManager Instance { get; private set; }
    public bool IsReady { get; private set; } = false;

    public bool IsFirebaseReady { get; private set; } = false;

    /// IsReady chi bao "da co default de doc". IsFetchComplete bao "da fetch xong tu server
    /// (hoac da chac chan khong fetch nua)". Cac gia tri chi ton tai tren Console —
    /// vi du dev_account_email — chi dang tin cay sau khi co dat cai nay.
    public bool IsFetchComplete { get; private set; } = false;

    [Header("Dev Settings")]
    [Tooltip("Bat khi dev: ignore cache 12h, luon fetch tu server each lan chay game.")]
    public bool forceFetchOnStart = true;
    [Tooltip("Editor Windows can crash with Firebase SDK 13.5.0 during automatic network fetches. Enable only when testing Remote Config in Editor.")]
    public bool fetchRemoteConfigInEditor = false;


    public int ConfigVersion => GetInt(RemoteConfigKeys.ConfigVersion, 1);

    public bool MaintenanceEnabled => GetBool(RemoteConfigKeys.MaintenanceEnabled, false);
    public string MaintenanceMessage => GetString(RemoteConfigKeys.MaintenanceMessage, "");

    public string MinSupportedVersion => GetString(RemoteConfigKeys.MinSupportedVersion, "");

    public bool NeedsForceUpdate
    {
        get
        {
            string min = MinSupportedVersion;
            if (string.IsNullOrWhiteSpace(min)) return false;
            return CompareVersion(Application.version, min) < 0;
        }
    }

    public string ActiveShopId => GetString(RemoteConfigKeys.ActiveShopId, "default");
    public string SaveHmacSalt => GetString(RemoteConfigKeys.SaveHmacSalt, RemoteConfigKeys.DefaultSaveHmacSalt);

    public string DevAccountEmail => GetString(RemoteConfigKeys.DevAccountEmail, "");

    public int MaxSlimes => GetInt(RemoteConfigKeys.BreedingMaxSlimes, 30);

    private readonly Dictionary<string, float> _floats = new Dictionary<string, float>();
    private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
    private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
    private readonly Dictionary<string, bool> _bools = new Dictionary<string, bool>();

    private readonly Dictionary<string, object> _jsonCache = new Dictionary<string, object>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

#if FIREBASE_REMOTE_CONFIG
        InitializeFirebase();
#else
        IsReady = true;
        IsFetchComplete = true;
        ReapplyBalance();
        Debug.Log("[RemoteConfig] Offline mode — using code defaults.");
#endif
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

#if FIREBASE_REMOTE_CONFIG
    void InitializeFirebase()
    {
        Debug.Log("[RemoteConfig] Checking Firebase dependencies...");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(checkTask =>
        {
            if (checkTask.IsFaulted)
            {
                Debug.LogError($"[RemoteConfig] CheckDependencies error: {checkTask.Exception}. Using defaults.");
                IsReady = true;
                IsFetchComplete = true;
                ReapplyBalance();
                return;
            }

            var status = checkTask.Result;
            if (status != DependencyStatus.Available)
            {
                Debug.LogError($"[RemoteConfig] Firebase init failed: {status}. Using defaults.");
                IsReady = true;
                IsFetchComplete = true;
                ReapplyBalance();
                return;
            }

            IsFirebaseReady = true;
            Debug.Log("[RemoteConfig] ✓ Firebase dependencies OK.");

            SetDefaults();
        });
    }

    void SetDefaults()
    {
        var defaults = RemoteConfigKeys.BuildDefaults();
        Debug.Log($"[RemoteConfig] Setting {defaults.Count} default values...");

        FirebaseRemoteConfig.DefaultInstance
            .SetDefaultsAsync(defaults)
            .ContinueWithOnMainThread(_ =>
            {
                IsReady = true;
                _jsonCache.Clear();
                ReapplyBalance();
                Debug.Log($"[RemoteConfig] ✓ Defaults set ({defaults.Count} keys) — game ready.");

#if UNITY_EDITOR
                if (!fetchRemoteConfigInEditor)
                {
                    Debug.LogWarning("[RemoteConfig] Skipping FetchAndActivate in Editor to avoid Firebase native crash. Player builds still fetch normally.");
                    Debug.LogWarning("[RemoteConfig] Vi khong fetch, cac key chi co tren Console (dev_account_email, ...) se rong.");
                    IsFetchComplete = true;
                    return;
                }
#endif
                FetchAndActivate();
            });
    }

    void FetchAndActivate()
    {
        var rc = FirebaseRemoteConfig.DefaultInstance;

        ulong intervalMs = forceFetchOnStart ? 0UL : 3600000UL;
        Debug.Log($"[RemoteConfig] Setting fetch interval: {(forceFetchOnStart ? "0s — always fetch fresh" : "1h")}...");

        rc.SetConfigSettingsAsync(new ConfigSettings
        {
            MinimumFetchIntervalInMilliseconds = intervalMs
        })
        .ContinueWithOnMainThread(_ =>
        {
            Debug.Log($"[RemoteConfig] Fetching config...");
            var fetchStart = System.DateTime.Now;

            rc.FetchAndActivateAsync().ContinueWithOnMainThread(task =>
            {
                var elapsed = (System.DateTime.Now - fetchStart).TotalMilliseconds;

                if (task.IsFaulted)
                {
                    Debug.LogWarning($"[RemoteConfig] ✗ Fetch failed ({elapsed:F0}ms): {task.Exception?.InnerException?.Message ?? "unknown error"}");
                    Debug.LogWarning("[RemoteConfig] Kiem tra: (1) Co nextt noi mang? (2) google-services.json right project? (3) Remote Config published in Console?");
                }
                else if (task.IsCanceled)
                {
                    Debug.LogWarning($"[RemoteConfig] ✗ Fetch canceled ({elapsed:F0}ms).");
                }
                else
                {
                    bool newDataFetched = task.Result;
                    if (newDataFetched)
                        Debug.Log($"[RemoteConfig] ✓ Fetch done ({elapsed:F0}ms) — got fresh server config.");
                    else
                        Debug.Log($"[RemoteConfig] ✓ Fetch done ({elapsed:F0}ms) — using cached config.");
                }

                IsReady = true;
                IsFetchComplete = true;
                OnConfigFetched();
            });
        });
    }
#endif

    public void ReapplyBalance()
    {
        _jsonCache.Clear();
        RemoteBalance.Apply(this);
    }

    void OnConfigFetched()
    {
        _jsonCache.Clear();
        RemoteBalance.Apply(this);
        LogAllValues();

        var bm = BreedingManager.Instance;
        int slimeCount = (bm != null && bm.GetAllSlimes() != null) ? bm.GetAllSlimes().Count : 0;
        Debug.Log($"[RemoteConfig] Apply config — recalculate {slimeCount} slimes + farm difficulties...");

        RecalculateAllSlimes();
        if (FarmModeManager.Instance != null) FarmModeManager.Instance.RefreshDifficultyStats();

        if (MaintenanceEnabled)
            Debug.LogWarning($"[RemoteConfig] ⚠ MAINTENANCE ON: \"{MaintenanceMessage}\"");
        if (NeedsForceUpdate)
            Debug.LogWarning($"[RemoteConfig] ⚠ Build {Application.version} is below min_supported_version {MinSupportedVersion}.");

        Debug.Log("[RemoteConfig] ✓ Applied.");
    }

    void LogAllValues()
    {
        var b = RemoteBalance.Battle;
        var r = RemoteBalance.Reward;

        Debug.Log("[RemoteConfig] ══════════ Current values ══════════");
        Debug.Log($"[RemoteConfig] [Meta]     config_version={ConfigVersion} | maintenance={MaintenanceEnabled} | min_version=\"{MinSupportedVersion}\" | shop=\"{ActiveShopId}\"");
        Debug.Log($"[RemoteConfig] [Dev]      dev_account_email=\"{DevAccountEmail}\" (rong = tat dev account)");
        Debug.Log($"[RemoteConfig] [Battle]   critRateCap={b.critRateCap:P0} critDmgCap={b.critDmgCap:F2} defPerPoint={b.defReductionPerPoint} maxDefRed={b.maxDefReduction:P0} overflow→ATK={b.critOverflowToAtk} skillPower={b.skillPowerMult}");
        Debug.Log($"[RemoteConfig] [Breeding] maxSlimes={MaxSlimes} gemPerMinute={RemoteBalance.BreedingGemPerMinute} diffBias={RemoteBalance.BreedingDiffRarityBias}");
        Debug.Log($"[RemoteConfig] [Egg]      interval={GetFloat(RemoteConfigKeys.EggCheckInterval, 60f)}s chance={GetFloat(RemoteConfigKeys.EggChance, 0.5f):P0} max={GetInt(RemoteConfigKeys.EggMaxUnhatched, 3)} required={GetInt(RemoteConfigKeys.EggRequiredSlimes, 2)} incubation={GetFloat(RemoteConfigKeys.EggIncubationSecs, 600f)}s gemPer={GetFloat(RemoteConfigKeys.EggSecondsPerGem, 60f)}s");
        Debug.Log($"[RemoteConfig] [Reward]   mission×{r.missionGold} daily×{r.dailyGold} achievement×{r.achievementGem} farm×{r.farmCoins} tower×{r.tower} | dailyCount={r.dailyCount} streak={r.dailyStreakBonusGold} | start={GetInt(RemoteConfigKeys.StartingCoins, 5000)} gold / {GetInt(RemoteConfigKeys.StartingGems, 5000)} gem");

        if (RemoteBalance.FarmRows != null)
            foreach (var row in RemoteBalance.FarmRows)
                Debug.Log($"[RemoteConfig] [Farm] {row.key,-8} HP={row.hp} ATK={row.atk} MAG={row.magic} DEF={row.def} SPD={row.speed} → {row.coins} gold + {row.gems} gem");

        var salt = SaveHmacSalt;
        var saltPreview = salt.Length > 4 ? salt.Substring(0, 4) + "****" : "****";
        Debug.Log($"[RemoteConfig] [Integrity] save_hmac_salt=\"{saltPreview}\" (length={salt.Length})");
        Debug.Log("[RemoteConfig] ════════════════════════════════════");
    }

    public void RecalculateAllSlimes()
    {
        var bm = BreedingManager.Instance;
        if (bm == null)
        {
            Debug.Log("[RemoteConfig] RecalculateAllSlimes: BreedingManager missing — ignore.");
            return;
        }

        var allSlimes = bm.GetAllSlimes();
        if (allSlimes == null || allSlimes.Count == 0)
        {
            Debug.Log("[RemoteConfig] RecalculateAllSlimes: No slime  — ignore.");
            return;
        }

        foreach (var slime in allSlimes)
        {
            if (slime == null) continue;
            slime.body?.RecalculateStats();
            slime.armor?.RecalculateStats();
            slime.weapon?.RecalculateStats();
            slime.CalculateStats();
        }

        Debug.Log($"[RemoteConfig] ✓ Recalculated {allSlimes.Count} slimes.");
    }

    public float GetFloat(string key, float fallback)
    {
        if (_floats.TryGetValue(key, out var over)) return over;
#if FIREBASE_REMOTE_CONFIG
        if (!IsReady) return fallback;
        var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        return v.Source != ValueSource.StaticValue ? (float)v.DoubleValue : fallback;
#else
        return fallback;
#endif
    }

    public int GetInt(string key, int fallback)
    {
        if (_ints.TryGetValue(key, out var over)) return over;
#if FIREBASE_REMOTE_CONFIG
        if (!IsReady) return fallback;
        var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        return v.Source != ValueSource.StaticValue ? (int)v.LongValue : fallback;
#else
        return fallback;
#endif
    }

    public string GetString(string key, string fallback)
    {
        if (_strings.TryGetValue(key, out var over)) return over;
#if FIREBASE_REMOTE_CONFIG
        if (!IsReady) return fallback;
        var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        return v.Source != ValueSource.StaticValue ? v.StringValue : fallback;
#else
        return fallback;
#endif
    }

    public bool GetBool(string key, bool fallback)
    {
        if (_bools.TryGetValue(key, out var over)) return over;
#if FIREBASE_REMOTE_CONFIG
        if (!IsReady) return fallback;
        var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        return v.Source != ValueSource.StaticValue ? v.BooleanValue : fallback;
#else
        return fallback;
#endif
    }
    public T GetJson<T>(string key) where T : class
    {
        if (_jsonCache.TryGetValue(key, out var cached)) return cached as T;

        T result = null;
        string raw = GetString(key, null);
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                result = JsonUtility.FromJson<T>(raw);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RemoteConfig] Key \"{key}\" cannot parse as {typeof(T).Name}: {ex.Message}. Using hardcode.");
                result = null;
            }
        }

        _jsonCache[key] = result;
        return result;
    }

    // -------------------------------------------------------
    // -------------------------------------------------------
    public void SetFloat(string key, float value) { _floats[key] = value; }
    public void SetInt(string key, int value) { _ints[key] = value; }
    public void SetString(string key, string value) { _strings[key] = value; _jsonCache.Remove(key); }
    public void SetBool(string key, bool value) { _bools[key] = value; }
    public void SetJson(string key, string json) { SetString(key, json); }

    public void ClearOverrides()
    {
        _floats.Clear();
        _ints.Clear();
        _strings.Clear();
        _bools.Clear();
        _jsonCache.Clear();
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    private static int CompareVersion(string a, string b)
    {
        string[] pa = (a ?? "").Split('.');
        string[] pb = (b ?? "").Split('.');
        int len = Mathf.Max(pa.Length, pb.Length);
        for (int i = 0; i < len; i++)
        {
            int va = i < pa.Length && int.TryParse(pa[i], out var x) ? x : 0;
            int vb = i < pb.Length && int.TryParse(pb[i], out var y) ? y : 0;
            if (va != vb) return va < vb ? -1 : 1;
        }
        return 0;
    }
}