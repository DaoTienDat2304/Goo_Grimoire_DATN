// ============================================================
// RemoteBalance.cs
//
//
//
// ============================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public static class RemoteBalance
{
    // ------------------------------------------------------------------
    // ------------------------------------------------------------------

    public class BattleTuning
    {
        public float critRateCap = 0.75f;
        public float critDmgCap = 2.50f;
        public float defReductionPerPoint = 0.00008f;
        public float maxDefReduction = 0.80f;
        public float critOverflowToAtk = 5f;
        public float poisonPercentHp = 0.04f;
        public int poisonMaxStacks = 3;
        public int energyPerAction = 10;
        public float skillPowerMult = 1.5f;
        public float legacyBossMultiplier = 3f;
    }

    public class RewardTuning
    {
        public float missionGold = 1f;
        public float dailyGold = 1f;
        public float achievementGem = 1f;
        public float farmCoins = 1f;
        public float tower = 1f;
        public int dailyCount = 3;
        public int dailyStreakBonusGold = 500;
    }

    public class QualityBands
    {
        private readonly List<RcQualityBand> bands;
        private readonly float totalWeight;

        public QualityBands(List<RcQualityBand> rows)
        {
            bands = rows;
            totalWeight = 0f;
            foreach (var b in bands) totalWeight += Mathf.Max(0f, b.weight);
        }

        public bool IsValid => bands != null && bands.Count > 0 && totalWeight > 0f;

        public string Roll(out float t)
        {
            float pick = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;
            foreach (var b in bands)
            {
                cumulative += Mathf.Max(0f, b.weight);
                if (pick <= cumulative)
                {
                    t = UnityEngine.Random.Range(b.min, b.max);
                    return b.name;
                }
            }
            var last = bands[bands.Count - 1];
            t = UnityEngine.Random.Range(last.min, last.max);
            return last.name;
        }
    }

    // ------------------------------------------------------------------
    // ------------------------------------------------------------------

    public static BattleTuning Battle { get; private set; } = new BattleTuning();
    public static RewardTuning Reward { get; private set; } = new RewardTuning();
    public static RcFeatureFlags Flags { get; private set; } = new RcFeatureFlags();

    public static float BreedingGemPerMinute { get; private set; } = 0.8f;

    public static float BreedingDiffRarityBias { get; private set; } = 0.20f;

    public static QualityBands BreedingQuality { get; private set; }
    public static QualityBands EggQuality { get; private set; }
    public static QualityBands AdventureQuality { get; private set; }
    public static RcTowerGrowth TowerGrowth { get; private set; }
    public static RcTowerStars TowerStars { get; private set; }
    public static List<RcFarmRow> FarmRows { get; private set; }

    private static readonly Dictionary<Rarity, StatBalance.Range> statRanges = new Dictionary<Rarity, StatBalance.Range>();
    private static readonly Dictionary<Rarity, BossStatScaling.Mult> bossMults = new Dictionary<Rarity, BossStatScaling.Mult>();
    private static readonly Dictionary<Rarity, SelectiveBreeding.TierCost> breedTiers = new Dictionary<Rarity, SelectiveBreeding.TierCost>();
    private static readonly Dictionary<Rarity, float> mutationRates = new Dictionary<Rarity, float>();
    private static readonly List<RcRarityWeightRow> eggRarityWeights = new List<RcRarityWeightRow>();
    private static float eggRarityTotalWeight;

    public static bool IsApplied { get; private set; }

    // ------------------------------------------------------------------
    // ------------------------------------------------------------------

    public static bool TryGetStatRange(Rarity rarity, out StatBalance.Range range)
        => statRanges.TryGetValue(rarity, out range);

    public static bool TryGetBossMult(Rarity rarity, out BossStatScaling.Mult mult)
        => bossMults.TryGetValue(rarity, out mult);

    public static bool TryGetBreedingTier(Rarity rarity, out SelectiveBreeding.TierCost tier)
        => breedTiers.TryGetValue(rarity, out tier);

    public static bool TryGetMutationRate(Rarity rarity, out float rate)
        => mutationRates.TryGetValue(rarity, out rate);

    public static bool TryRollEggRarity(out Rarity rarity)
    {
        rarity = Rarity.Common;
        if (eggRarityWeights.Count == 0 || eggRarityTotalWeight <= 0f) return false;

        float pick = UnityEngine.Random.value * eggRarityTotalWeight;
        float cumulative = 0f;
        foreach (var row in eggRarityWeights)
        {
            cumulative += Mathf.Max(0f, row.weight);
            if (pick <= cumulative)
            {
                rarity = ParseRarity(row.rarity, Rarity.Common);
                return true;
            }
        }
        rarity = ParseRarity(eggRarityWeights[eggRarityWeights.Count - 1].rarity, Rarity.Common);
        return true;
    }

    public static RcFarmRow GetFarmRow(string key)
    {
        if (FarmRows == null || string.IsNullOrEmpty(key)) return null;
        foreach (var row in FarmRows)
            if (row != null && string.Equals(row.key, key, StringComparison.OrdinalIgnoreCase))
                return row;
        return null;
    }

    public static RcFarmRow GetFarmRowAt(int index)
    {
        if (FarmRows == null || index < 0 || index >= FarmRows.Count) return null;
        return FarmRows[index];
    }

    // ------------------------------------------------------------------
    // ------------------------------------------------------------------

    public static void Apply(RemoteConfigManager rc)
    {
        if (rc == null) return;

        Clear();

        var battle = new BattleTuning();
        battle.critRateCap = rc.GetFloat(RemoteConfigKeys.BattleCritRateCap, battle.critRateCap);
        battle.critDmgCap = rc.GetFloat(RemoteConfigKeys.BattleCritDmgCap, battle.critDmgCap);
        battle.defReductionPerPoint = rc.GetFloat(RemoteConfigKeys.BattleDefReductionPer, battle.defReductionPerPoint);
        if (battle.defReductionPerPoint > 0.001f) battle.defReductionPerPoint /= 100f;
        battle.maxDefReduction = rc.GetFloat(RemoteConfigKeys.BattleMaxDefReduction, battle.maxDefReduction);
        battle.critOverflowToAtk = rc.GetFloat(RemoteConfigKeys.BattleCritOverflowToAtk, battle.critOverflowToAtk);
        battle.poisonPercentHp = rc.GetFloat(RemoteConfigKeys.BattlePoisonPercentHp, battle.poisonPercentHp);
        battle.poisonMaxStacks = Mathf.Max(1, rc.GetInt(RemoteConfigKeys.BattlePoisonMaxStacks, battle.poisonMaxStacks));
        battle.energyPerAction = Mathf.Max(0, rc.GetInt(RemoteConfigKeys.BattleEnergyPerAction, battle.energyPerAction));
        battle.skillPowerMult = rc.GetFloat(RemoteConfigKeys.BattleSkillPowerMult, battle.skillPowerMult);
        battle.legacyBossMultiplier = rc.GetFloat(RemoteConfigKeys.BattleLegacyBossMult, battle.legacyBossMultiplier);
        Battle = battle;

        var statTable = rc.GetJson<RcStatTable>(RemoteConfigKeys.StatBalanceTable);
        if (statTable != null && statTable.rows != null)
        {
            foreach (var row in statTable.rows)
            {
                if (row == null || !TryParseRarity(row.rarity, out var r)) continue;
                statRanges[r] = new StatBalance.Range
                {
                    hpMin = row.hpMin, hpMax = row.hpMax,
                    atkMin = row.atkMin, atkMax = row.atkMax,
                    magMin = row.magMin, magMax = row.magMax,
                    defMin = row.defMin, defMax = row.defMax,
                    spdMin = row.spdMin, spdMax = row.spdMax,
                    critRate = row.critRate, critDmg = row.critDmg
                };
            }
        }

        var bossTable = rc.GetJson<RcBossTable>(RemoteConfigKeys.BossScalingTable);
        if (bossTable != null && bossTable.rows != null)
        {
            foreach (var row in bossTable.rows)
            {
                if (row == null || !TryParseRarity(row.rarity, out var r)) continue;
                bossMults[r] = new BossStatScaling.Mult
                {
                    hp = row.hp, atk = row.atk, magic = row.magic, def = row.def, speed = row.speed
                };
            }
        }

        var tierTable = rc.GetJson<RcBreedTierTable>(RemoteConfigKeys.BreedingTierTable);
        if (tierTable != null && tierTable.rows != null)
        {
            foreach (var row in tierTable.rows)
            {
                if (row == null || !TryParseRarity(row.rarity, out var r)) continue;
                breedTiers[r] = new SelectiveBreeding.TierCost { gold = row.gold, minutes = row.minutes };
                mutationRates[r] = row.mutation;
            }
        }
        BreedingQuality = ParseBands(rc, RemoteConfigKeys.BreedingQualityBands);
        BreedingGemPerMinute = Mathf.Max(0f, rc.GetFloat(RemoteConfigKeys.BreedingGemPerMinute, BreedingGemPerMinute));
        BreedingDiffRarityBias = rc.GetFloat(RemoteConfigKeys.BreedingDiffBias, BreedingDiffRarityBias);

        EggQuality = ParseBands(rc, RemoteConfigKeys.EggQualityBands);

        var weights = rc.GetJson<RcRarityWeightTable>(RemoteConfigKeys.EggRarityWeights);
        if (weights != null && weights.rows != null)
        {
            foreach (var row in weights.rows)
            {
                if (row == null || !TryParseRarity(row.rarity, out _)) continue;
                eggRarityWeights.Add(row);
                eggRarityTotalWeight += Mathf.Max(0f, row.weight);
            }
        }

        AdventureQuality = ParseBands(rc, RemoteConfigKeys.AdventureQualityBands);

        var farm = rc.GetJson<RcFarmTable>(RemoteConfigKeys.FarmDifficultyTable);
        FarmRows = (farm != null && farm.rows != null && farm.rows.Count > 0) ? farm.rows : null;

        TowerGrowth = rc.GetJson<RcTowerGrowth>(RemoteConfigKeys.TowerGrowth);
        TowerStars = rc.GetJson<RcTowerStars>(RemoteConfigKeys.TowerStarThresholds);

        var reward = new RewardTuning();
        reward.missionGold = Mathf.Max(0f, rc.GetFloat(RemoteConfigKeys.RewardMultMissionGold, reward.missionGold));
        reward.dailyGold = Mathf.Max(0f, rc.GetFloat(RemoteConfigKeys.RewardMultDailyGold, reward.dailyGold));
        reward.achievementGem = Mathf.Max(0f, rc.GetFloat(RemoteConfigKeys.RewardMultAchievementGem, reward.achievementGem));
        reward.farmCoins = Mathf.Max(0f, rc.GetFloat(RemoteConfigKeys.RewardMultFarmCoins, reward.farmCoins));
        reward.tower = Mathf.Max(0f, rc.GetFloat(RemoteConfigKeys.RewardMultTower, reward.tower));
        reward.dailyCount = Mathf.Max(1, rc.GetInt(RemoteConfigKeys.DailyCount, reward.dailyCount));
        reward.dailyStreakBonusGold = Mathf.Max(0, rc.GetInt(RemoteConfigKeys.DailyStreakBonusGold, reward.dailyStreakBonusGold));
        Reward = reward;

        Flags = rc.GetJson<RcFeatureFlags>(RemoteConfigKeys.FeatureFlags) ?? new RcFeatureFlags();

        IsApplied = true;

        Debug.Log($"[RemoteBalance] Loaded overrides — stat:{statRanges.Count} boss:{bossMults.Count} " +
                  $"breed:{breedTiers.Count} farm:{(FarmRows != null ? FarmRows.Count : 0)} " +
                  $"tower:{(TowerGrowth != null ? "yes" : "no")}");
    }

    public static void Clear()
    {
        statRanges.Clear();
        bossMults.Clear();
        breedTiers.Clear();
        mutationRates.Clear();
        eggRarityWeights.Clear();
        eggRarityTotalWeight = 0f;
        BreedingQuality = null;
        EggQuality = null;
        AdventureQuality = null;
        TowerGrowth = null;
        TowerStars = null;
        FarmRows = null;
        Battle = new BattleTuning();
        Reward = new RewardTuning();
        Flags = new RcFeatureFlags();
        BreedingGemPerMinute = 0.8f;
        BreedingDiffRarityBias = 0.20f;
        IsApplied = false;
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    // ------------------------------------------------------------------
    //
    // ------------------------------------------------------------------

    public static float FloatOr(string key, float fallback)
    {
        var rc = RemoteConfigManager.Instance;
        return rc != null ? rc.GetFloat(key, fallback) : fallback;
    }

    public static int IntOr(string key, int fallback)
    {
        var rc = RemoteConfigManager.Instance;
        return rc != null ? rc.GetInt(key, fallback) : fallback;
    }

    public static bool BoolOr(string key, bool fallback)
    {
        var rc = RemoteConfigManager.Instance;
        return rc != null ? rc.GetBool(key, fallback) : fallback;
    }

    public static string StringOr(string key, string fallback)
    {
        var rc = RemoteConfigManager.Instance;
        return rc != null ? rc.GetString(key, fallback) : fallback;
    }

    public static int ScaleReward(int baseAmount, float multiplier)
    {
        if (baseAmount <= 0) return baseAmount;
        if (Mathf.Approximately(multiplier, 1f)) return baseAmount;
        return Mathf.Max(1, Mathf.RoundToInt(baseAmount * multiplier));
    }

    private static QualityBands ParseBands(RemoteConfigManager rc, string key)
    {
        var table = rc.GetJson<RcQualityTable>(key);
        if (table == null || table.rows == null || table.rows.Count == 0) return null;
        var bands = new QualityBands(table.rows);
        return bands.IsValid ? bands : null;
    }

    private static bool TryParseRarity(string s, out Rarity rarity)
    {
        rarity = Rarity.Common;
        if (string.IsNullOrEmpty(s)) return false;
        return Enum.TryParse(s.Replace(" ", string.Empty), true, out rarity);
    }

    private static Rarity ParseRarity(string s, Rarity fallback)
        => TryParseRarity(s, out var r) ? r : fallback;
}
