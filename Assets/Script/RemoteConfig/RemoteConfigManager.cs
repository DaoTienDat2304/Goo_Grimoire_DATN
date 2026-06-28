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
//   → Firebase sẽ init, fetch config, rồi recalculate toàn bộ slime.
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

    // ---- Breeding ----
    public float BreedingTime        => GetFloat("breeding_time_seconds",    5f);
    public int   BreedingCost        => GetInt  ("breeding_cost_coins",      1);
    public int   MaxSlimes           => GetInt  ("breeding_max_slimes",      30);
    public float MutationChance      => GetFloat("breeding_mutation_chance", 0.1f);
    public float BreedingCooldown    => GetFloat("breeding_cooldown_seconds",2f);

    // ---- Rarity multipliers ----
    public float RarityMultCommon    => GetFloat("rarity_mult_common",       1f);
    public float RarityMultUncommon  => GetFloat("rarity_mult_uncommon",     1.2f);
    public float RarityMultRare      => GetFloat("rarity_mult_rare",         1.4f);
    public float RarityMultSuperRare => GetFloat("rarity_mult_super_rare",   1.6f);
    public float RarityMultUltraRare => GetFloat("rarity_mult_ultra_rare",   1.8f);
    public float RarityMultLegendary => GetFloat("rarity_mult_legendary",    2f);
    public float RarityMultMythic    => GetFloat("rarity_mult_mythic",       2.25f);
    public float RaritySkillPowerMult=> GetFloat("rarity_skill_power_mult",  1.5f);

    // ---- Battle ----
    public float BossStatMultiplier   => GetFloat("boss_stat_multiplier",    3f);
    public float CritDamageMultiplier => GetFloat("crit_damage_multiplier",  1.5f);

    // ---- Farm — Easy ----
    public int FarmEasyBossHP        => GetInt("farm_easy_boss_hp",              100);
    public int FarmEasyBossAttack    => GetInt("farm_easy_boss_attack",          30);
    public int FarmEasyBossMagicAttack => GetInt("farm_easy_boss_magic_attack",    60);
    public int FarmEasyBossDefense   => GetInt("farm_easy_boss_defense",         20);
    public int FarmEasyBossSpeed     => GetInt("farm_easy_boss_speed",           15);
    public int FarmEasyReward        => GetInt("farm_easy_reward_coins",         50);
    // ---- Farm — Medium ----
    public int FarmMediumBossHP      => GetInt("farm_medium_boss_hp",            200);
    public int FarmMediumBossAttack  => GetInt("farm_medium_boss_attack",        60);
    public int FarmMediumBossMagicAttack => GetInt("farm_medium_boss_magic_attack", 120);
    public int FarmMediumBossDefense => GetInt("farm_medium_boss_defense",       40);
    public int FarmMediumBossSpeed   => GetInt("farm_medium_boss_speed",         25);
    public int FarmMediumReward      => GetInt("farm_medium_reward_coins",       150);
    // ---- Farm — Hard ----
    public int FarmHardBossHP        => GetInt("farm_hard_boss_hp",              400);
    public int FarmHardBossAttack    => GetInt("farm_hard_boss_attack",          120);
    public int FarmHardBossMagicAttack => GetInt("farm_hard_boss_magic_attack",  240);
    public int FarmHardBossDefense   => GetInt("farm_hard_boss_defense",         80);
    public int FarmHardBossSpeed     => GetInt("farm_hard_boss_speed",           40);
    public int FarmHardReward        => GetInt("farm_hard_reward_coins",         300);
    // ---- Farm — Extreme ----
    public int FarmExtremeBossHP     => GetInt("farm_extreme_boss_hp",           800);
    public int FarmExtremeBossAttack => GetInt("farm_extreme_boss_attack",       200);
    public int FarmExtremeBossMagicAttack => GetInt("farm_extreme_boss_magic_attack", 400);
    public int FarmExtremeBossDefense=> GetInt("farm_extreme_boss_defense",      150);
    public int FarmExtremeBossSpeed  => GetInt("farm_extreme_boss_speed",        60);
    public int FarmExtremeReward     => GetInt("farm_extreme_reward_coins",      600);
    // ---- Farm — Hell ----
    public int FarmHellBossHP        => GetInt("farm_hell_boss_hp",              1500);
    public int FarmHellBossAttack    => GetInt("farm_hell_boss_attack",          350);
    public int FarmHellBossMagicAttack => GetInt("farm_hell_boss_magic_attack",  700);
    public int FarmHellBossDefense   => GetInt("farm_hell_boss_defense",         250);
    public int FarmHellBossSpeed     => GetInt("farm_hell_boss_speed",           90);
    public int FarmHellReward        => GetInt("farm_hell_reward_coins",         1200);

    /// <summary>
    /// Helper dùng trong InitializeDefaultDifficulties — lấy stat theo tên difficulty và loại stat.
    /// </summary>
    public int GetFarmStat(string diffKey, string stat, int fallback)
    {
        string key = $"farm_{diffKey}_boss_{stat}";
        if (stat == "reward") key = $"farm_{diffKey}_reward_coins";
        return GetInt(key, fallback);
    }

    // ---- Shop ----
    public string ActiveShopId       => GetString("active_shop_id",         "default");

    // ---- Save integrity ----
    /// <summary>
    /// Salt dùng để derive HMAC key cho save data.
    /// Đặt giá trị thực sự bí mật trên Firebase Remote Config console.
    /// Fallback hardcode trong SaveIntegrity.cs chỉ là lưới an toàn khi offline.
    /// </summary>
    public string SaveHmacSalt       => GetString("save_hmac_salt",         "GooGrimoire_HmacFallback_v1");

    // ---- Dev ----
    /// <summary>Email của tài khoản dev. Set trên Firebase Console. Trống = không có dev account.</summary>
    public string DevAccountEmail    => GetString("dev_account_email",       "");

    // Internal override storage (dùng để test mà không cần Firebase)
    private readonly Dictionary<string, float>  _floats  = new();
    private readonly Dictionary<string, int>    _ints    = new();
    private readonly Dictionary<string, string> _strings = new();

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
        // Không có Firebase — ready ngay với defaults
        IsReady = true;
        Debug.Log("[RemoteConfig] Offline mode — dùng default values.");
#endif
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
                return;
            }

            var status = checkTask.Result;
            if (status != DependencyStatus.Available)
            {
                Debug.LogError($"[RemoteConfig] Firebase không khởi động được: {status}. Dùng defaults.");
                IsReady = true;
                return;
            }

            IsFirebaseReady = true;
            Debug.Log("[RemoteConfig] ✓ Firebase dependencies OK.");

            SetDefaults();
            FetchAndActivate();
        });
    }

    void SetDefaults()
    {
        Debug.Log("[RemoteConfig] Đang set default values...");
        var defaults = new Dictionary<string, object>
        {
            // Breeding
            { "breeding_time_seconds",    5.0  },
            { "breeding_cost_coins",      1    },
            { "breeding_max_slimes",      30   },
            { "breeding_mutation_chance", 0.1  },
            { "breeding_cooldown_seconds",2.0  },
            // Rarity
            { "rarity_mult_common",       1.0  },
            { "rarity_mult_uncommon",     1.2  },
            { "rarity_mult_rare",         1.4  },
            { "rarity_mult_super_rare",   1.6  },
            { "rarity_mult_ultra_rare",   1.8  },
            { "rarity_mult_legendary",    2.0  },
            { "rarity_mult_mythic",       2.25 },
            { "rarity_skill_power_mult",  1.5  },
            // Battle
            { "boss_stat_multiplier",     3.0  },
            { "crit_damage_multiplier",   1.5  },
            // Farm — Easy
            { "farm_easy_boss_hp",            100  },
            { "farm_easy_boss_attack",        30   },
            { "farm_easy_boss_magic_attack",  60   },
            { "farm_easy_boss_defense",       20   },
            { "farm_easy_boss_speed",         15   },
            { "farm_easy_reward_coins",       50   },
            // Farm — Medium
            { "farm_medium_boss_hp",          200  },
            { "farm_medium_boss_attack",      60   },
            { "farm_medium_boss_magic_attack", 120  },
            { "farm_medium_boss_defense",     40   },
            { "farm_medium_boss_speed",       25   },
            { "farm_medium_reward_coins",     150  },
            // Farm — Hard
            { "farm_hard_boss_hp",            400  },
            { "farm_hard_boss_attack",        120  },
            { "farm_hard_boss_magic_attack",  240  },
            { "farm_hard_boss_defense",       80   },
            { "farm_hard_boss_speed",         40   },
            { "farm_hard_reward_coins",       300  },
            // Farm — Extreme
            { "farm_extreme_boss_hp",         800  },
            { "farm_extreme_boss_attack",     200  },
            { "farm_extreme_boss_magic_attack", 400  },
            { "farm_extreme_boss_defense",    150  },
            { "farm_extreme_boss_speed",      60   },
            { "farm_extreme_reward_coins",    600  },
            // Farm — Hell
            { "farm_hell_boss_hp",            1500 },
            { "farm_hell_boss_attack",        350  },
            { "farm_hell_boss_magic_attack",  700  },
            { "farm_hell_boss_defense",       250  },
            { "farm_hell_boss_speed",         90   },
            { "farm_hell_reward_coins",       1200 },
            // Shop
            { "active_shop_id",               "default" },
            // Save integrity — đặt giá trị bí mật khác trên Firebase Console
            { "save_hmac_salt",               "GooGrimoire_HmacFallback_v1" },
            // Dev account — đặt email trên Firebase Console, để trống ở đây
            { "dev_account_email",            "" },
        };

        FirebaseRemoteConfig.DefaultInstance
            .SetDefaultsAsync(defaults)
            .ContinueWithOnMainThread(_ =>
            {
                IsReady = true;
                Debug.Log($"[RemoteConfig] ✓ Defaults set ({defaults.Count} keys) — game sẵn sàng chạy.");
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

                    LogAllValues();
                }

                IsReady = true;
                OnConfigFetched();
            });
        });
    }


    void LogAllValues()
    {
        var rc = FirebaseRemoteConfig.DefaultInstance;
        Debug.Log("[RemoteConfig] ══════════ Giá trị hiện tại ══════════");
        Debug.Log($"[RemoteConfig] [Breeding] time={rc.GetValue("breeding_time_seconds").DoubleValue}s | cost={rc.GetValue("breeding_cost_coins").LongValue} coins | max={rc.GetValue("breeding_max_slimes").LongValue} slimes | mutation={rc.GetValue("breeding_mutation_chance").DoubleValue:P0} | cooldown={rc.GetValue("breeding_cooldown_seconds").DoubleValue}s");
        Debug.Log($"[RemoteConfig] [Rarity]   common={rc.GetValue("rarity_mult_common").DoubleValue} | uncommon={rc.GetValue("rarity_mult_uncommon").DoubleValue} | rare={rc.GetValue("rarity_mult_rare").DoubleValue} | SR={rc.GetValue("rarity_mult_super_rare").DoubleValue} | UR={rc.GetValue("rarity_mult_ultra_rare").DoubleValue} | legend={rc.GetValue("rarity_mult_legendary").DoubleValue} | mythic={rc.GetValue("rarity_mult_mythic").DoubleValue}");
        Debug.Log($"[RemoteConfig] [Battle]   boss_mult={rc.GetValue("boss_stat_multiplier").DoubleValue}x | crit_mult={rc.GetValue("crit_damage_multiplier").DoubleValue}x");
        Debug.Log($"[RemoteConfig] [Farm] Easy    HP={rc.GetValue("farm_easy_boss_hp").LongValue} ATK={rc.GetValue("farm_easy_boss_attack").LongValue} MAG_ATK={rc.GetValue("farm_easy_boss_magic_attack").LongValue} DEF={rc.GetValue("farm_easy_boss_defense").LongValue} SPD={rc.GetValue("farm_easy_boss_speed").LongValue} → {rc.GetValue("farm_easy_reward_coins").LongValue} coins");
        Debug.Log($"[RemoteConfig] [Farm] Medium  HP={rc.GetValue("farm_medium_boss_hp").LongValue} ATK={rc.GetValue("farm_medium_boss_attack").LongValue} MAG_ATK={rc.GetValue("farm_medium_boss_magic_attack").LongValue} DEF={rc.GetValue("farm_medium_boss_defense").LongValue} SPD={rc.GetValue("farm_medium_boss_speed").LongValue} → {rc.GetValue("farm_medium_reward_coins").LongValue} coins");
        Debug.Log($"[RemoteConfig] [Farm] Hard    HP={rc.GetValue("farm_hard_boss_hp").LongValue} ATK={rc.GetValue("farm_hard_boss_attack").LongValue} MAG_ATK={rc.GetValue("farm_hard_boss_magic_attack").LongValue} DEF={rc.GetValue("farm_hard_boss_defense").LongValue} SPD={rc.GetValue("farm_hard_boss_speed").LongValue} → {rc.GetValue("farm_hard_reward_coins").LongValue} coins");
        Debug.Log($"[RemoteConfig] [Farm] Extreme HP={rc.GetValue("farm_extreme_boss_hp").LongValue} ATK={rc.GetValue("farm_extreme_boss_attack").LongValue} MAG_ATK={rc.GetValue("farm_extreme_boss_magic_attack").LongValue} DEF={rc.GetValue("farm_extreme_boss_defense").LongValue} SPD={rc.GetValue("farm_extreme_boss_speed").LongValue} → {rc.GetValue("farm_extreme_reward_coins").LongValue} coins");
        Debug.Log($"[RemoteConfig] [Farm] Hell    HP={rc.GetValue("farm_hell_boss_hp").LongValue} ATK={rc.GetValue("farm_hell_boss_attack").LongValue} MAG_ATK={rc.GetValue("farm_hell_boss_magic_attack").LongValue} DEF={rc.GetValue("farm_hell_boss_defense").LongValue} SPD={rc.GetValue("farm_hell_boss_speed").LongValue} → {rc.GetValue("farm_hell_reward_coins").LongValue} coins");
        Debug.Log($"[RemoteConfig] [Shop]     active_shop_id=\"{rc.GetValue("active_shop_id").StringValue}\"");
        var rawSalt = rc.GetValue("save_hmac_salt").StringValue;
        var saltPreview = rawSalt.Length > 4 ? rawSalt[..4] + "****" : "****";
        Debug.Log($"[RemoteConfig] [Integrity] save_hmac_salt=\"{saltPreview}\" (length={rawSalt.Length})");
        Debug.Log("[RemoteConfig] ════════════════════════════════════");
    }

    void OnConfigFetched()
    {
        var bm = BreedingManager.Instance;
        int slimeCount = (bm != null && bm.GetAllSlimes() != null) ? bm.GetAllSlimes().Count : 0;
        Debug.Log($"[RemoteConfig] Áp dụng config mới — recalculate {slimeCount} slimes + farm difficulties...");
        RecalculateAllSlimes();
        if (FarmModeManager.Instance != null) FarmModeManager.Instance.RefreshDifficultyStats();
        Debug.Log("[RemoteConfig] ✓ Áp dụng xong.");
    }
#endif

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
            RecalculateTrait(slime.body);
            RecalculateTrait(slime.armor);
            RecalculateTrait(slime.weapon);
            slime.CalculateStats();
        }

        Debug.Log($"[RemoteConfig] ✓ Recalculated {allSlimes.Count} slimes.");
    }

    void RecalculateTrait(TraitInstance ti)
    {
        if (ti == null) return;
        ti.RecalculateStats(GetRarityMultiplier(ti.Rarity));
    }

    public float GetRarityMultiplier(Rarity rarity) => rarity switch
    {
        Rarity.Common    => RarityMultCommon,
        Rarity.Uncommon  => RarityMultUncommon,
        Rarity.Rare      => RarityMultRare,
        Rarity.SuperRare => RarityMultSuperRare,
        Rarity.UltraRare => RarityMultUltraRare,
        Rarity.Legendary => RarityMultLegendary,
        Rarity.Mythic    => RarityMultMythic,
        Rarity.Secret    => RarityMultLegendary,
        _                => 1f
    };

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    float GetFloat(string key, float fallback)
    {
#if FIREBASE_REMOTE_CONFIG
        if (!IsReady) return fallback;
        var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        return v.Source != ValueSource.StaticValue ? (float)v.DoubleValue : fallback;
#else
        return _floats.TryGetValue(key, out var val) ? val : fallback;
#endif
    }

    int GetInt(string key, int fallback)
    {
#if FIREBASE_REMOTE_CONFIG
        if (!IsReady) return fallback;
        var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        return v.Source != ValueSource.StaticValue ? (int)v.LongValue : fallback;
#else
        return _ints.TryGetValue(key, out var val) ? val : fallback;
#endif
    }

    string GetString(string key, string fallback)
    {
#if FIREBASE_REMOTE_CONFIG
        if (!IsReady) return fallback;
        var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        return v.Source != ValueSource.StaticValue ? v.StringValue : fallback;
#else
        return _strings.TryGetValue(key, out var val) ? val : fallback;
#endif
    }

    // Override thủ công — dùng để test trong Editor không cần Firebase
    public void SetFloat(string key, float value)  => _floats[key]  = value;
    public void SetInt(string key, int value)       => _ints[key]    = value;
    public void SetString(string key, string value) => _strings[key] = value;
}
