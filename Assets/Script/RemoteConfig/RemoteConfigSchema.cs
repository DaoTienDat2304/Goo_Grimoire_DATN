// ============================================================
// RemoteConfigSchema.cs
//
// DTO cho các bảng dữ liệu dạng JSON trên Firebase Remote Config.
//
// Vì JsonUtility KHÔNG parse được mảng ở gốc ("[...]"), mọi bảng
// đều được bọc trong { "rows": [ ... ] }.
//
// Độ hiếm luôn ghi bằng CHUỖI ("Common", "SuperRare", ...) để người
// chỉnh trên Console đọc được; code parse qua Enum.TryParse.
//
// Xem danh sách key + default: REMOTE_CONFIG_KEYS.md (cùng thư mục)
// và file import remote_config_defaults.json.
// ============================================================

using System;
using System.Collections.Generic;

// ---------- Nhóm 1: Chỉ số theo độ hiếm (stat_balance_table) ----------

[Serializable]
public class RcStatRow
{
    public string rarity;
    public int hpMin, hpMax;
    public int atkMin, atkMax;
    public int magMin, magMax;
    public int defMin, defMax;
    public int spdMin, spdMax;
    public float critRate;
    public float critDmg;
}

[Serializable]
public class RcStatTable
{
    public List<RcStatRow> rows = new List<RcStatRow>();
}

// ---------- Nhóm 1: Hệ số boss theo độ hiếm (boss_scaling_table) ----------

[Serializable]
public class RcBossRow
{
    public string rarity;
    public float hp, atk, magic, def, speed;
}

[Serializable]
public class RcBossTable
{
    public List<RcBossRow> rows = new List<RcBossRow>();
}

// ---------- Nhóm 2: Bảng tier lai tạo (breeding_tier_table) ----------

[Serializable]
public class RcBreedTierRow
{
    public string rarity;
    public int gold;
    public float minutes;
    public float mutation; // 0..1
}

[Serializable]
public class RcBreedTierTable
{
    public List<RcBreedTierRow> rows = new List<RcBreedTierRow>();
}

// ---------- Dải chất lượng roll (breeding / egg / adventure) ----------

/// <summary>
/// 1 dải chất lượng roll. weight = trọng số (không cần cộng đủ 100 — code tự chuẩn hoá),
/// min/max = khoảng roll 0..1 áp lên range chỉ số.
/// </summary>
[Serializable]
public class RcQualityBand
{
    public string name;
    public float weight;
    public float min;
    public float max;
}

[Serializable]
public class RcQualityTable
{
    public List<RcQualityBand> rows = new List<RcQualityBand>();
}

// ---------- Nhóm 3: Trọng số độ hiếm khi nở trứng (egg_rarity_weights) ----------

[Serializable]
public class RcRarityWeightRow
{
    public string rarity;
    public float weight;
}

[Serializable]
public class RcRarityWeightTable
{
    public List<RcRarityWeightRow> rows = new List<RcRarityWeightRow>();
}

// ---------- Nhóm 5: Bảng độ khó Farm (farm_difficulty_table) ----------

[Serializable]
public class RcFarmRow
{
    public string key;      // easy | medium | hard | extreme | hell
    public string name;     // tên hiển thị
    public int hp, atk, magic, def, speed;
    public float critRate, critDmg;
    public int coins, gems;
}

[Serializable]
public class RcFarmTable
{
    public List<RcFarmRow> rows = new List<RcFarmRow>();
}

// ---------- Nhóm 6: Tháp (tower_growth / tower_star_thresholds) ----------

[Serializable]
public class RcTowerGrowth
{
    public int baseHP = 6000;
    public int baseAttack = 180;
    public int baseMagicAttack = 360;
    public int baseDefense = 780;
    public int baseSpeed = 90;
    public float statGrowthPerFloor = 1.12f;
    public int rewardCoinsBase = 400;
    public float rewardGrowthPerFloor = 1.08f;
    public int gemEveryNFloors = 5;
    public int gemAmount = 5;

    /// <summary>
    /// false (mặc định): công thức chỉ áp cho các tầng SINH THÊM ngoài asset —
    /// tầng 1..N đã thiết kế tay trong TowerSlimeBosses.asset giữ nguyên.
    /// true: ghi đè chỉ số + thưởng của MỌI tầng theo công thức (traits/waves không đụng).
    /// Dùng khi muốn kéo cả tháp về thang chỉ số mới.
    /// </summary>
    public bool applyToAuthoredFloors = false;
}

[Serializable]
public class RcTowerStars
{
    public int threeStarMaxTurns = 50;
    public int twoStarMaxTurns = 80;
}

// ---------- Nhóm 0: Feature flags ----------

[Serializable]
public class RcFeatureFlags
{
    public bool eggSystem = true;
    public bool tower = true;
    public bool dailyMissions = true;
    public bool shop = true;
    public bool adventureCapture = true;
}

/// <summary>
/// Tên key + chuỗi JSON mặc định. Dùng cho SetDefaultsAsync (Firebase) và
/// để sinh file import remote_config_defaults.json.
/// </summary>
public static class RemoteConfigKeys
{
    // ── Nhóm 0: Vận hành ──
    public const string ConfigVersion       = "config_version";
    public const string MaintenanceEnabled  = "maintenance_enabled";
    public const string MaintenanceMessage  = "maintenance_message";
    public const string MinSupportedVersion = "min_supported_version";
    public const string FeatureFlags        = "feature_flags";
    public const string ActiveShopId        = "active_shop_id";
    public const string SaveHmacSalt        = "save_hmac_salt";
    public const string DevAccountEmail     = "dev_account_email";

    // ── Nhóm 1: Chỉ số & chiến đấu ──
    public const string StatBalanceTable        = "stat_balance_table";
    public const string BossScalingTable        = "boss_scaling_table";
    public const string BattleCritRateCap       = "battle_crit_rate_cap";
    public const string BattleCritDmgCap        = "battle_crit_dmg_cap";
    public const string BattleDefReductionPer   = "battle_def_reduction_per_point";
    public const string BattleMaxDefReduction   = "battle_max_def_reduction";
    public const string BattleCritOverflowToAtk = "battle_crit_overflow_to_atk";
    public const string BattlePoisonPercentHp   = "battle_poison_percent_hp";
    public const string BattlePoisonMaxStacks   = "battle_poison_max_stacks";
    public const string BattleEnergyPerAction   = "battle_energy_per_action";
    public const string BattleSkillPowerMult    = "battle_skill_power_mult";
    public const string BattleLegacyBossMult    = "battle_legacy_boss_multiplier";

    // ── Nhóm 2: Lai tạo ──
    public const string BreedingTierTable    = "breeding_tier_table";
    public const string BreedingQualityBands = "breeding_quality_bands";
    public const string BreedingGemPerMinute = "breeding_gem_per_minute";
    public const string BreedingDiffBias     = "breeding_diff_rarity_bias";
    public const string BreedingMaxSlimes    = "breeding_max_slimes";

    // ── Nhóm 3: Trứng ──
    public const string EggCheckInterval   = "egg_check_interval_seconds";
    public const string EggChance          = "egg_chance";
    public const string EggMaxUnhatched    = "egg_max_unhatched";
    public const string EggRequiredSlimes  = "egg_required_slimes";
    public const string EggIncubationSecs  = "egg_incubation_seconds";
    public const string EggSecondsPerGem   = "egg_seconds_per_gem";
    public const string EggRarityWeights   = "egg_rarity_weights";
    public const string EggQualityBands    = "egg_quality_bands";

    // ── Nhóm 4: Adventure ──
    public const string AdventureQualityBands = "adventure_quality_bands";

    // ── Nhóm 5: Farm ──
    public const string FarmDifficultyTable = "farm_difficulty_table";

    // ── Nhóm 6: Tháp ──
    public const string TowerGrowth         = "tower_growth";
    public const string TowerStarThresholds = "tower_star_thresholds";

    // ── Nhóm 7: Thưởng & tiến trình ──
    public const string RewardMultMissionGold     = "reward_mult_mission_gold";
    public const string RewardMultDailyGold       = "reward_mult_daily_gold";
    public const string RewardMultAchievementGem  = "reward_mult_achievement_gem";
    public const string RewardMultFarmCoins       = "reward_mult_farm_coins";
    public const string RewardMultTower           = "reward_mult_tower";
    public const string DailyCount                = "daily_count";
    public const string DailyStreakBonusGold      = "daily_streak_bonus_gold";
    public const string StartingCoins             = "starting_coins";
    public const string StartingGems              = "starting_gems";

    // ------------------------------------------------------------------
    // Chuỗi JSON mặc định — GIỐNG HỆT bảng hardcode trong code
    // (StatBalance / BossStatScaling / SelectiveBreeding / ...), trừ Farm
    // đã được tái cân bằng theo thang chỉ số mới.
    // ------------------------------------------------------------------

    public const string DefaultStatBalanceTable =
        "{\"rows\":[" +
        "{\"rarity\":\"Common\",\"hpMin\":1000,\"hpMax\":2000,\"atkMin\":100,\"atkMax\":200,\"magMin\":200,\"magMax\":400,\"defMin\":400,\"defMax\":800,\"spdMin\":80,\"spdMax\":100,\"critRate\":0.05,\"critDmg\":1.30}," +
        "{\"rarity\":\"Uncommon\",\"hpMin\":1800,\"hpMax\":3500,\"atkMin\":180,\"atkMax\":320,\"magMin\":320,\"magMax\":640,\"defMin\":720,\"defMax\":1400,\"spdMin\":90,\"spdMax\":110,\"critRate\":0.06,\"critDmg\":1.35}," +
        "{\"rarity\":\"Rare\",\"hpMin\":3200,\"hpMax\":6000,\"atkMin\":320,\"atkMax\":600,\"magMin\":640,\"magMax\":1200,\"defMin\":1280,\"defMax\":2400,\"spdMin\":100,\"spdMax\":120,\"critRate\":0.08,\"critDmg\":1.45}," +
        "{\"rarity\":\"SuperRare\",\"hpMin\":5500,\"hpMax\":10000,\"atkMin\":550,\"atkMax\":1000,\"magMin\":1100,\"magMax\":2000,\"defMin\":2200,\"defMax\":4000,\"spdMin\":110,\"spdMax\":135,\"critRate\":0.10,\"critDmg\":1.55}," +
        "{\"rarity\":\"UltraRare\",\"hpMin\":9000,\"hpMax\":16000,\"atkMin\":900,\"atkMax\":1600,\"magMin\":1800,\"magMax\":3200,\"defMin\":3600,\"defMax\":6400,\"spdMin\":120,\"spdMax\":150,\"critRate\":0.13,\"critDmg\":1.70}," +
        "{\"rarity\":\"Legendary\",\"hpMin\":14000,\"hpMax\":25000,\"atkMin\":1400,\"atkMax\":2500,\"magMin\":2800,\"magMax\":5000,\"defMin\":5600,\"defMax\":10000,\"spdMin\":135,\"spdMax\":165,\"critRate\":0.16,\"critDmg\":1.90}," +
        "{\"rarity\":\"Mythic\",\"hpMin\":22000,\"hpMax\":50000,\"atkMin\":2200,\"atkMax\":5000,\"magMin\":4400,\"magMax\":10000,\"defMin\":8800,\"defMax\":20000,\"spdMin\":150,\"spdMax\":180,\"critRate\":0.20,\"critDmg\":2.20}," +
        "{\"rarity\":\"Secret\",\"hpMin\":9000,\"hpMax\":16000,\"atkMin\":90,\"atkMax\":160,\"magMin\":180,\"magMax\":320,\"defMin\":1440,\"defMax\":2560,\"spdMin\":120,\"spdMax\":150,\"critRate\":0.25,\"critDmg\":2.50}" +
        "]}";

    public const string DefaultBossScalingTable =
        "{\"rows\":[" +
        "{\"rarity\":\"Common\",\"hp\":4.0,\"atk\":1.2,\"magic\":1.2,\"def\":1.3,\"speed\":1.00}," +
        "{\"rarity\":\"Uncommon\",\"hp\":4.5,\"atk\":1.3,\"magic\":1.3,\"def\":1.4,\"speed\":1.05}," +
        "{\"rarity\":\"Rare\",\"hp\":5.2,\"atk\":1.4,\"magic\":1.4,\"def\":1.5,\"speed\":1.10}," +
        "{\"rarity\":\"SuperRare\",\"hp\":6.0,\"atk\":1.5,\"magic\":1.5,\"def\":1.7,\"speed\":1.15}," +
        "{\"rarity\":\"UltraRare\",\"hp\":7.0,\"atk\":1.7,\"magic\":1.7,\"def\":1.9,\"speed\":1.20}," +
        "{\"rarity\":\"Legendary\",\"hp\":8.2,\"atk\":1.9,\"magic\":1.9,\"def\":2.2,\"speed\":1.25}," +
        "{\"rarity\":\"Mythic\",\"hp\":9.5,\"atk\":2.2,\"magic\":2.2,\"def\":2.5,\"speed\":1.30}," +
        "{\"rarity\":\"Secret\",\"hp\":9.5,\"atk\":2.2,\"magic\":2.2,\"def\":2.5,\"speed\":1.30}" +
        "]}";

    public const string DefaultBreedingTierTable =
        "{\"rows\":[" +
        "{\"rarity\":\"Common\",\"gold\":200,\"minutes\":1,\"mutation\":0.35}," +
        "{\"rarity\":\"Uncommon\",\"gold\":600,\"minutes\":10,\"mutation\":0.30}," +
        "{\"rarity\":\"Rare\",\"gold\":2500,\"minutes\":25,\"mutation\":0.25}," +
        "{\"rarity\":\"SuperRare\",\"gold\":6000,\"minutes\":50,\"mutation\":0.20}," +
        "{\"rarity\":\"UltraRare\",\"gold\":12000,\"minutes\":90,\"mutation\":0.15}," +
        "{\"rarity\":\"Legendary\",\"gold\":25000,\"minutes\":120,\"mutation\":0.12}," +
        "{\"rarity\":\"Mythic\",\"gold\":45000,\"minutes\":240,\"mutation\":0.10}," +
        "{\"rarity\":\"Secret\",\"gold\":45000,\"minutes\":240,\"mutation\":0.10}" +
        "]}";

    public const string DefaultBreedingQualityBands =
        "{\"rows\":[" +
        "{\"name\":\"Good\",\"weight\":55,\"min\":0.40,\"max\":0.60}," +
        "{\"name\":\"Excellent\",\"weight\":28,\"min\":0.60,\"max\":0.80}," +
        "{\"name\":\"Perfect\",\"weight\":12,\"min\":0.80,\"max\":0.95}," +
        "{\"name\":\"God Roll\",\"weight\":5,\"min\":0.95,\"max\":1.00}" +
        "]}";

    public const string DefaultEggQualityBands =
        "{\"rows\":[" +
        "{\"name\":\"Poor\",\"weight\":15,\"min\":0.00,\"max\":0.20}," +
        "{\"name\":\"Normal\",\"weight\":30,\"min\":0.20,\"max\":0.40}," +
        "{\"name\":\"Good\",\"weight\":30,\"min\":0.40,\"max\":0.60}," +
        "{\"name\":\"Excellent\",\"weight\":18,\"min\":0.60,\"max\":0.80}," +
        "{\"name\":\"Perfect\",\"weight\":6,\"min\":0.80,\"max\":0.95}," +
        "{\"name\":\"GodRoll\",\"weight\":1,\"min\":0.95,\"max\":1.00}" +
        "]}";

    public const string DefaultAdventureQualityBands =
        "{\"rows\":[" +
        "{\"name\":\"Good\",\"weight\":55,\"min\":0.40,\"max\":0.60}," +
        "{\"name\":\"Excellent\",\"weight\":28,\"min\":0.60,\"max\":0.80}," +
        "{\"name\":\"Perfect\",\"weight\":12,\"min\":0.80,\"max\":0.95}," +
        "{\"name\":\"God\",\"weight\":5,\"min\":0.95,\"max\":1.00}" +
        "]}";

    public const string DefaultEggRarityWeights =
        "{\"rows\":[" +
        "{\"rarity\":\"Common\",\"weight\":45}," +
        "{\"rarity\":\"Uncommon\",\"weight\":35}," +
        "{\"rarity\":\"Rare\",\"weight\":14}," +
        "{\"rarity\":\"SuperRare\",\"weight\":5}," +
        "{\"rarity\":\"UltraRare\",\"weight\":1}" +
        "]}";

    /// <summary>
    /// Farm đã TÁI CÂN BẰNG theo thang chỉ số mới:
    /// mid-range StatBalance của độ hiếm tương ứng × hệ số BossStatScaling.
    /// (Hệ evade đã bị bỏ khỏi combat nên không còn trường evade.)
    /// </summary>
    public const string DefaultFarmDifficultyTable =
        "{\"rows\":[" +
        "{\"key\":\"easy\",\"name\":\"Dễ\",\"hp\":6000,\"atk\":180,\"magic\":360,\"def\":780,\"speed\":90,\"critRate\":0.05,\"critDmg\":1.30,\"coins\":500,\"gems\":0}," +
        "{\"key\":\"medium\",\"name\":\"Trung Bình\",\"hp\":11000,\"atk\":325,\"magic\":650,\"def\":1480,\"speed\":105,\"critRate\":0.06,\"critDmg\":1.35,\"coins\":1200,\"gems\":0}," +
        "{\"key\":\"hard\",\"name\":\"Khó\",\"hp\":24000,\"atk\":640,\"magic\":1290,\"def\":2790,\"speed\":121,\"critRate\":0.08,\"critDmg\":1.45,\"coins\":3000,\"gems\":2}," +
        "{\"key\":\"extreme\",\"name\":\"Cực Khó\",\"hp\":46000,\"atk\":1160,\"magic\":2325,\"def\":5270,\"speed\":141,\"critRate\":0.10,\"critDmg\":1.55,\"coins\":7000,\"gems\":5}," +
        "{\"key\":\"hell\",\"name\":\"Địa Ngục\",\"hp\":87000,\"atk\":2125,\"magic\":4250,\"def\":9500,\"speed\":162,\"critRate\":0.13,\"critDmg\":1.70,\"coins\":15000,\"gems\":10}" +
        "]}";

    public const string DefaultTowerGrowth =
        "{\"baseHP\":6000,\"baseAttack\":180,\"baseMagicAttack\":360,\"baseDefense\":780,\"baseSpeed\":90," +
        "\"statGrowthPerFloor\":1.12,\"rewardCoinsBase\":400,\"rewardGrowthPerFloor\":1.08," +
        "\"gemEveryNFloors\":5,\"gemAmount\":5,\"applyToAuthoredFloors\":false}";

    public const string DefaultTowerStarThresholds =
        "{\"threeStarMaxTurns\":50,\"twoStarMaxTurns\":80}";

    public const string DefaultFeatureFlags =
        "{\"eggSystem\":true,\"tower\":true,\"dailyMissions\":true,\"shop\":true,\"adventureCapture\":true}";

    public const string DefaultSaveHmacSalt = "GooGrimoire_HmacFallback_v1";

    /// <summary>Toàn bộ default — dùng cho FirebaseRemoteConfig.SetDefaultsAsync().</summary>
    public static Dictionary<string, object> BuildDefaults() => new Dictionary<string, object>
    {
        // Nhóm 0 — Vận hành
        { ConfigVersion,       1 },
        { MaintenanceEnabled,  false },
        { MaintenanceMessage,  "" },
        { MinSupportedVersion, "" },
        { FeatureFlags,        DefaultFeatureFlags },
        { ActiveShopId,        "default" },
        { SaveHmacSalt,        DefaultSaveHmacSalt },
        { DevAccountEmail,     "" },

        // Nhóm 1 — Chỉ số & chiến đấu
        { StatBalanceTable,        DefaultStatBalanceTable },
        { BossScalingTable,        DefaultBossScalingTable },
        { BattleCritRateCap,       0.75 },
        { BattleCritDmgCap,        2.50 },
        { BattleDefReductionPer,   0.008 },
        { BattleMaxDefReduction,   0.80 },
        { BattleCritOverflowToAtk, 5.0 },
        { BattlePoisonPercentHp,   0.04 },
        { BattlePoisonMaxStacks,   3 },
        { BattleEnergyPerAction,   10 },
        { BattleSkillPowerMult,    1.5 },
        { BattleLegacyBossMult,    3.0 },

        // Nhóm 2 — Lai tạo
        { BreedingTierTable,    DefaultBreedingTierTable },
        { BreedingQualityBands, DefaultBreedingQualityBands },
        { BreedingGemPerMinute, 0.8 },
        { BreedingDiffBias,     0.20 },
        { BreedingMaxSlimes,    30 },

        // Nhóm 3 — Trứng
        { EggCheckInterval,  60.0 },
        { EggChance,         0.5 },
        { EggMaxUnhatched,   3 },
        { EggRequiredSlimes, 2 },
        { EggIncubationSecs, 600.0 },
        { EggSecondsPerGem,  60.0 },
        { EggRarityWeights,  DefaultEggRarityWeights },
        { EggQualityBands,   DefaultEggQualityBands },

        // Nhóm 4 — Adventure
        { AdventureQualityBands, DefaultAdventureQualityBands },

        // Nhóm 5 — Farm
        { FarmDifficultyTable, DefaultFarmDifficultyTable },

        // Nhóm 6 — Tháp
        { TowerGrowth,         DefaultTowerGrowth },
        { TowerStarThresholds, DefaultTowerStarThresholds },

        // Nhóm 7 — Thưởng & tiến trình
        { RewardMultMissionGold,    1.0 },
        { RewardMultDailyGold,      1.0 },
        { RewardMultAchievementGem, 1.0 },
        { RewardMultFarmCoins,      1.0 },
        { RewardMultTower,          1.0 },
        { DailyCount,               3 },
        { DailyStreakBonusGold,     500 },
        { StartingCoins,            5000 },
        { StartingGems,             5000 },
    };
}
