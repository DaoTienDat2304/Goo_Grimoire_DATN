// ============================================================
// RemoteConfigManager.cs
//
// Đặt GameObject này vào scene "menu" — DontDestroyOnLoad sẽ
// giữ nó tồn tại xuyên suốt toàn bộ game.
//
// Khi Firebase chưa cài đặt (mặc định):
//   → Compile bình thường, dùng hardcode defaults ngay lập tức.
//
// Khi Firebase đã sẵn sàng:
//   1. Import Firebase SDK vào project.
//   2. Project Settings → Player → Scripting Define Symbols → thêm: FIREBASE_REMOTE_CONFIG
//   → Firebase sẽ init, fetch config, rồi nạp lại toàn bộ bảng cân bằng.
//
// Danh sách key: xem REMOTE_CONFIG_KEYS.md (cùng thư mục).
// Giá trị mặc định: RemoteConfigKeys.BuildDefaults() trong RemoteConfigSchema.cs.
// Bảng cân bằng sau khi parse nằm ở RemoteBalance.
// ============================================================

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

    /// <summary>
    /// True khi đã sẵn sàng trả giá trị (Firebase fetch xong HOẶC offline mode).
    /// </summary>
    public bool IsReady { get; private set; } = false;

    /// <summary>True nếu Firebase init thành công (chỉ có nghĩa khi có FIREBASE_REMOTE_CONFIG).</summary>
    public bool IsFirebaseReady { get; private set; } = false;

    [Header("Dev Settings")]
    [Tooltip("Bật khi dev: bỏ qua cache 12h, luôn fetch từ server mỗi lần chạy game.")]
    public bool forceFetchOnStart = true;
    [Tooltip("Editor Windows can crash with Firebase SDK 13.5.0 during automatic network fetches. Enable only when testing Remote Config in Editor.")]
    public bool fetchRemoteConfigInEditor = false;

    // -------------------------------------------------------
    // Nhóm 0 — Vận hành
    // -------------------------------------------------------

    /// <summary>Số hiệu bộ config đang chạy — dùng để đối chiếu log với Console.</summary>
    public int ConfigVersion => GetInt(RemoteConfigKeys.ConfigVersion, 1);

    /// <summary>Bật cờ bảo trì. UI chặn màn hình chưa được gắn — hiện chỉ expose + log.</summary>
    public bool MaintenanceEnabled => GetBool(RemoteConfigKeys.MaintenanceEnabled, false);
    public string MaintenanceMessage => GetString(RemoteConfigKeys.MaintenanceMessage, "");

    /// <summary>Version tối thiểu được hỗ trợ (vd "1.2.0"). Rỗng = tắt kiểm tra.</summary>
    public string MinSupportedVersion => GetString(RemoteConfigKeys.MinSupportedVersion, "");

    /// <summary>True khi Application.version thấp hơn min_supported_version.</summary>
    public bool NeedsForceUpdate
    {
        get
        {
            string min = MinSupportedVersion;
            if (string.IsNullOrWhiteSpace(min)) return false;
            return CompareVersion(Application.version, min) < 0;
        }
    }

    /// <summary>Chọn database shop đang hiển thị ("default" | "summer" | ...).</summary>
    public string ActiveShopId => GetString(RemoteConfigKeys.ActiveShopId, "default");

    /// <summary>
    /// Salt dùng để derive HMAC key cho save data.
    /// Đặt giá trị thực sự bí mật trên Firebase Remote Config console.
    /// Fallback hardcode trong SaveIntegrity.cs chỉ là lưới an toàn khi offline.
    /// </summary>
    public string SaveHmacSalt => GetString(RemoteConfigKeys.SaveHmacSalt, RemoteConfigKeys.DefaultSaveHmacSalt);

    /// <summary>Email của tài khoản dev. Set trên Firebase Console. Trống = không có dev account.</summary>
    public string DevAccountEmail => GetString(RemoteConfigKeys.DevAccountEmail, "");

    // -------------------------------------------------------
    // Nhóm 2 — Giới hạn bộ sưu tập
    // -------------------------------------------------------

    /// <summary>Giới hạn tối đa slime trong bộ sưu tập.</summary>
    public int MaxSlimes => GetInt(RemoteConfigKeys.BreedingMaxSlimes, 30);

    // Internal override storage (dùng để test mà không cần Firebase)
    private readonly Dictionary<string, float> _floats = new Dictionary<string, float>();
    private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
    private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
    private readonly Dictionary<string, bool> _bools = new Dictionary<string, bool>();

    // Cache object đã parse từ JSON — tránh parse lại mỗi frame.
    private readonly Dictionary<string, object> _jsonCache = new Dictionary<string, object>();

    // -------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------
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
        // Không có Firebase — ready ngay với defaults hardcode trong code.
        IsReady = true;
        ReapplyBalance();
        Debug.Log("[RemoteConfig] Offline mode — dùng default values trong code.");
#endif
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

#if FIREBASE_REMOTE_CONFIG
    // -------------------------------------------------------
    // Firebase init flow (bắt buộc theo thứ tự):
    //   1. CheckAndFixDependenciesAsync  — kiểm tra Google Play Services / dependencies
    //   2. SetDefaultsAsync             — đặt giá trị mặc định (game chạy được ngay cả khi offline)
    //   3. FetchAndActivateAsync        — kéo config mới nhất từ server
    // -------------------------------------------------------
    void InitializeFirebase()
    {
        Debug.Log("[RemoteConfig] Bắt đầu kiểm tra Firebase dependencies...");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(checkTask =>
        {
            if (checkTask.IsFaulted)
            {
                Debug.LogError($"[RemoteConfig] CheckDependencies lỗi: {checkTask.Exception}. Dùng defaults.");
                IsReady = true;
                ReapplyBalance();
                return;
            }

            var status = checkTask.Result;
            if (status != DependencyStatus.Available)
            {
                Debug.LogError($"[RemoteConfig] Firebase không khởi động được: {status}. Dùng defaults.");
                IsReady = true;
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
        Debug.Log($"[RemoteConfig] Đang set {defaults.Count} default values...");

        FirebaseRemoteConfig.DefaultInstance
            .SetDefaultsAsync(defaults)
            .ContinueWithOnMainThread(_ =>
            {
                IsReady = true;
                _jsonCache.Clear();
                ReapplyBalance();
                Debug.Log($"[RemoteConfig] ✓ Defaults set ({defaults.Count} keys) — game sẵn sàng chạy.");

#if UNITY_EDITOR
                if (!fetchRemoteConfigInEditor)
                {
                    Debug.LogWarning("[RemoteConfig] Skipping FetchAndActivate in Editor to avoid Firebase native crash. Player builds still fetch normally.");
                    return;
                }
#endif
                FetchAndActivate();
            });
    }

    void FetchAndActivate()
    {
        var rc = FirebaseRemoteConfig.DefaultInstance;

        // Nguyên nhân #1: Cache 12 giờ mặc định
        // forceFetchOnStart = true → set interval = 0 để luôn gọi server (dùng khi dev)
        // forceFetchOnStart = false → dùng interval 1 giờ (phù hợp production)
        ulong intervalMs = forceFetchOnStart ? 0UL : 3600000UL;
        Debug.Log($"[RemoteConfig] Đang set fetch interval: {(forceFetchOnStart ? "0s — luôn fetch mới" : "1h")}...");

        rc.SetConfigSettingsAsync(new ConfigSettings
        {
            MinimumFetchIntervalInMilliseconds = intervalMs
        })
        .ContinueWithOnMainThread(_ =>
        {
            Debug.Log($"[RemoteConfig] Đang fetch config từ server...");
            var fetchStart = System.DateTime.Now;

            rc.FetchAndActivateAsync().ContinueWithOnMainThread(task =>
            {
                var elapsed = (System.DateTime.Now - fetchStart).TotalMilliseconds;

                if (task.IsFaulted)
                {
                    Debug.LogWarning($"[RemoteConfig] ✗ Fetch thất bại ({elapsed:F0}ms): {task.Exception?.InnerException?.Message ?? "unknown error"}");
                    Debug.LogWarning("[RemoteConfig] Kiểm tra: (1) Có kết nối mạng? (2) google-services.json đúng project? (3) Remote Config đã Publish trên Console?");
                }
                else if (task.IsCanceled)
                {
                    Debug.LogWarning($"[RemoteConfig] ✗ Fetch bị huỷ ({elapsed:F0}ms).");
                }
                else
                {
                    bool newDataFetched = task.Result;
                    if (newDataFetched)
                        Debug.Log($"[RemoteConfig] ✓ Fetch xong ({elapsed:F0}ms) — đã lấy config MỚI từ server.");
                    else
                        Debug.Log($"[RemoteConfig] ✓ Fetch xong ({elapsed:F0}ms) — dùng CACHED config (không có thay đổi mới).");
                }

                IsReady = true;
                OnConfigFetched();
            });
        });
    }
#endif

    // -------------------------------------------------------
    // Áp dụng config
    // -------------------------------------------------------

    /// <summary>
    /// Nạp lại toàn bộ bảng cân bằng vào RemoteBalance.
    /// Gọi thủ công sau khi dùng SetFloat/SetInt/SetString/SetJson để test trong Editor.
    /// </summary>
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
        Debug.Log($"[RemoteConfig] Áp dụng config mới — recalculate {slimeCount} slimes + farm difficulties...");

        RecalculateAllSlimes();
        if (FarmModeManager.Instance != null) FarmModeManager.Instance.RefreshDifficultyStats();

        if (MaintenanceEnabled)
            Debug.LogWarning($"[RemoteConfig] ⚠ CHẾ ĐỘ BẢO TRÌ đang BẬT: \"{MaintenanceMessage}\"");
        if (NeedsForceUpdate)
            Debug.LogWarning($"[RemoteConfig] ⚠ Bản build {Application.version} thấp hơn min_supported_version {MinSupportedVersion}.");

        Debug.Log("[RemoteConfig] ✓ Áp dụng xong.");
    }

    void LogAllValues()
    {
        var b = RemoteBalance.Battle;
        var r = RemoteBalance.Reward;

        Debug.Log("[RemoteConfig] ══════════ Giá trị hiện tại ══════════");
        Debug.Log($"[RemoteConfig] [Meta]     config_version={ConfigVersion} | maintenance={MaintenanceEnabled} | min_version=\"{MinSupportedVersion}\" | shop=\"{ActiveShopId}\"");
        Debug.Log($"[RemoteConfig] [Battle]   critRateCap={b.critRateCap:P0} critDmgCap={b.critDmgCap:F2} defPerPoint={b.defReductionPerPoint} maxDefRed={b.maxDefReduction:P0} overflow→ATK={b.critOverflowToAtk} skillPower={b.skillPowerMult}");
        Debug.Log($"[RemoteConfig] [Breeding] maxSlimes={MaxSlimes} gemPerMinute={RemoteBalance.BreedingGemPerMinute} diffBias={RemoteBalance.BreedingDiffRarityBias}");
        // Nhóm egg_* / starting_* dùng Inspector làm fallback nên log giá trị THÔ từ server.
        Debug.Log($"[RemoteConfig] [Egg]      interval={GetFloat(RemoteConfigKeys.EggCheckInterval, 60f)}s chance={GetFloat(RemoteConfigKeys.EggChance, 0.5f):P0} max={GetInt(RemoteConfigKeys.EggMaxUnhatched, 3)} required={GetInt(RemoteConfigKeys.EggRequiredSlimes, 2)} incubation={GetFloat(RemoteConfigKeys.EggIncubationSecs, 600f)}s gemPer={GetFloat(RemoteConfigKeys.EggSecondsPerGem, 60f)}s");
        Debug.Log($"[RemoteConfig] [Reward]   mission×{r.missionGold} daily×{r.dailyGold} achievement×{r.achievementGem} farm×{r.farmCoins} tower×{r.tower} | dailyCount={r.dailyCount} streak={r.dailyStreakBonusGold} | start={GetInt(RemoteConfigKeys.StartingCoins, 5000)} vàng / {GetInt(RemoteConfigKeys.StartingGems, 5000)} gem");

        if (RemoteBalance.FarmRows != null)
            foreach (var row in RemoteBalance.FarmRows)
                Debug.Log($"[RemoteConfig] [Farm] {row.key,-8} HP={row.hp} ATK={row.atk} MAG={row.magic} DEF={row.def} SPD={row.speed} → {row.coins} vàng + {row.gems} gem");

        var salt = SaveHmacSalt;
        var saltPreview = salt.Length > 4 ? salt.Substring(0, 4) + "****" : "****";
        Debug.Log($"[RemoteConfig] [Integrity] save_hmac_salt=\"{saltPreview}\" (length={salt.Length})");
        Debug.Log("[RemoteConfig] ════════════════════════════════════");
    }

    // -------------------------------------------------------
    // Recalculate slime stats
    // -------------------------------------------------------
    public void RecalculateAllSlimes()
    {
        var bm = BreedingManager.Instance;
        if (bm == null)
        {
            Debug.Log("[RemoteConfig] RecalculateAllSlimes: BreedingManager chưa có — bỏ qua.");
            return;
        }

        var allSlimes = bm.GetAllSlimes();
        if (allSlimes == null || allSlimes.Count == 0)
        {
            Debug.Log("[RemoteConfig] RecalculateAllSlimes: Chưa có slime nào — bỏ qua.");
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

    // -------------------------------------------------------
    // Đọc giá trị (public — RemoteBalance và code game dùng chung)
    // -------------------------------------------------------
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

    /// <summary>
    /// Đọc 1 key kiểu JSON và parse thành T. Trả null khi key trống / JSON hỏng
    /// — nơi gọi phải tự rơi về bảng hardcode.
    /// </summary>
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
                Debug.LogWarning($"[RemoteConfig] Key \"{key}\" không parse được thành {typeof(T).Name}: {ex.Message}. Dùng bảng hardcode.");
                result = null;
            }
        }

        _jsonCache[key] = result;
        return result;
    }

    // -------------------------------------------------------
    // Override thủ công — dùng để test trong Editor không cần Firebase.
    // Nhớ gọi ReapplyBalance() sau khi set xong.
    // -------------------------------------------------------
    public void SetFloat(string key, float value) { _floats[key] = value; }
    public void SetInt(string key, int value) { _ints[key] = value; }
    public void SetString(string key, string value) { _strings[key] = value; _jsonCache.Remove(key); }
    public void SetBool(string key, bool value) { _bools[key] = value; }
    /// <summary>Override 1 key JSON bằng chuỗi thô.</summary>
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

    /// <summary>So sánh version dạng "1.2.3". &lt;0 nghĩa là a cũ hơn b.</summary>
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
