using System;
using System.Collections.Generic;
using UnityEngine;
public class PlayerStatsManager : MonoBehaviour
{
    private static PlayerStatsManager _instance;
    public static PlayerStatsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<PlayerStatsManager>();
                if (_instance == null)
                {
                    var go = new GameObject("PlayerStatsManager");
                    _instance = go.AddComponent<PlayerStatsManager>();
                }
            }
            return _instance;
        }
    }

    public static event Action OnStatsChanged;

    public long TotalSlimesBred   { get; private set; }
    public int  TotalFarmWins     { get; private set; }
    public int  TotalCaptures     { get; private set; }
    public int  TotalBattleWins   { get; private set; }
    public int  TotalMutations    { get; private set; }
    public long TotalCoinsEarned  { get; private set; }
    public long TotalGemsEarned   { get; private set; }
    public int  HighestTowerFloor { get; private set; }

    private const int RarityCount = 8;
    private readonly int[] rarityObtained = new int[RarityCount];
    private readonly HashSet<string> traitLedger = new HashSet<string>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyAdded += HandleCurrencyAdded;
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyAdded -= HandleCurrencyAdded;
    }


    public void RecordBreed(Slime offspring)
    {
        TotalSlimesBred++;
        if (offspring != null && offspring.eggStatQuality == "Mutation")
            TotalMutations++;
        RecordSlimeObtained(offspring);
        Changed();
    }

    public void RecordSecretObtained(Slime s)
    {
        RecordSlimeObtained(s);
        Changed();
    }

    public void RecordCapture(TraitSO[] traits)
    {
        TotalCaptures++;
        BumpRarity(MaxRarity(traits));
        RecordTraits(traits);
        Changed();
    }

    public void AddBattleWin() { TotalBattleWins++; Changed(); }
    public void AddFarmWin()   { TotalFarmWins++;   Changed(); }

    public void RecordTowerFloor(int floor)
    {
        if (floor > HighestTowerFloor) { HighestTowerFloor = floor; Changed(); }
    }

    private void HandleCurrencyAdded(CurrencyType type, int amount)
    {
        if (amount <= 0) return;
        if (type == CurrencyType.Coins) TotalCoinsEarned += amount;
        else if (type == CurrencyType.Gems) TotalGemsEarned += amount;
        Changed();
    }

    public int DistinctTraitsCount => traitLedger.Count;
    public int GetRarityObtained(Rarity r)
    {
        int i = (int)r;
        return (i >= 0 && i < RarityCount) ? rarityObtained[i] : 0;
    }

    public int GetRarityObtainedAtLeast(Rarity r)
    {
        int sum = 0;
        for (int i = (int)r; i < RarityCount; i++) sum += rarityObtained[i];
        return sum;
    }

    public long ReadMetric(AchievementMetric metric, Rarity rarity = Rarity.Common)
    {
        switch (metric)
        {
            case AchievementMetric.TotalBred: return TotalSlimesBred;
            case AchievementMetric.DistinctTraits: return DistinctTraitsCount;
            case AchievementMetric.CoinsEarned: return TotalCoinsEarned;
            case AchievementMetric.GemsEarned: return TotalGemsEarned;
            case AchievementMetric.FarmWins: return TotalFarmWins;
            case AchievementMetric.Captures: return TotalCaptures;
            case AchievementMetric.RarityObtained: return GetRarityObtained(rarity);
            case AchievementMetric.TowerFloor: return HighestTowerFloor;
            case AchievementMetric.BattleWins: return TotalBattleWins;
            case AchievementMetric.Mutations: return TotalMutations;
            case AchievementMetric.OwnedSlimes:
                return BreedingManager.Instance != null ? BreedingManager.Instance.GetAllSlimes().Count : 0;
            default: return 0;
        }
    }

    private void RecordSlimeObtained(Slime s)
    {
        if (s == null) return;
        BumpRarity(SelectiveBreeding.GetSlimeRarity(s));
        RecordTraitName(s.body?.baseTrait);
        RecordTraitName(s.armor?.baseTrait);
        RecordTraitName(s.weapon?.baseTrait);
    }

    private void BumpRarity(Rarity r)
    {
        int i = (int)r;
        if (i >= 0 && i < RarityCount) rarityObtained[i]++;
    }

    private void RecordTraits(TraitSO[] traits)
    {
        if (traits == null) return;
        foreach (var t in traits) RecordTraitName(t);
    }

    private void RecordTraitName(TraitSO t)
    {
        if (t != null && !string.IsNullOrEmpty(t.traitName))
            traitLedger.Add(t.traitName);
    }

    private static Rarity MaxRarity(TraitSO[] traits)
    {
        Rarity best = Rarity.Common;
        bool anySecret = false;
        if (traits != null)
        {
            foreach (var t in traits)
            {
                if (t == null) continue;
                if (t.rarity == Rarity.Secret) { anySecret = true; continue; }
                if (t.rarity > best) best = t.rarity;
            }
        }
        if (best == Rarity.Common && anySecret) best = Rarity.Secret;
        return best;
    }

    private void Changed() => OnStatsChanged?.Invoke();

    public void WriteTo(GameSaveData data)
    {
        data.totalSlimesBred  = TotalSlimesBred;
        data.totalFarmWins    = TotalFarmWins;
        data.totalCaptures    = TotalCaptures;
        data.totalBattleWins  = TotalBattleWins;
        data.totalMutations   = TotalMutations;
        data.totalCoinsEarned = TotalCoinsEarned;
        data.totalGemsEarned  = TotalGemsEarned;
        data.towerHighestFloorStat = HighestTowerFloor;

        data.rarityObtainedCount = new List<int>(rarityObtained);
        data.unlockedTraitsEver  = new List<string>(traitLedger);
    }

    public void LoadFrom(GameSaveData data)
    {
        if (data == null) return;
        TotalSlimesBred  = data.totalSlimesBred;
        TotalFarmWins    = data.totalFarmWins;
        TotalCaptures    = data.totalCaptures;
        TotalBattleWins  = data.totalBattleWins;
        TotalMutations   = data.totalMutations;
        TotalCoinsEarned = data.totalCoinsEarned;
        TotalGemsEarned  = data.totalGemsEarned;
        HighestTowerFloor = data.towerHighestFloorStat;

        Array.Clear(rarityObtained, 0, RarityCount);
        if (data.rarityObtainedCount != null)
            for (int i = 0; i < data.rarityObtainedCount.Count && i < RarityCount; i++)
                rarityObtained[i] = data.rarityObtainedCount[i];

        traitLedger.Clear();
        if (data.unlockedTraitsEver != null)
            foreach (var n in data.unlockedTraitsEver)
                if (!string.IsNullOrEmpty(n)) traitLedger.Add(n);

        Changed();
    }

    public IReadOnlyCollection<string> GetTraitLedger() => traitLedger;


    public void MergeOwnedSlimeTraits(IEnumerable<Slime> slimes)
    {
        if (slimes == null) return;
        bool any = false;
        foreach (var s in slimes)
        {
            if (s == null) continue;
            if (s.body?.baseTrait != null   && traitLedger.Add(s.body.baseTrait.traitName))   any = true;
            if (s.armor?.baseTrait != null  && traitLedger.Add(s.armor.baseTrait.traitName))  any = true;
            if (s.weapon?.baseTrait != null && traitLedger.Add(s.weapon.baseTrait.traitName)) any = true;
        }
        if (any) Changed();
    }

    public void ResetAll()
    {
        TotalSlimesBred = 0;
        TotalFarmWins = TotalCaptures = TotalBattleWins = TotalMutations = 0;
        TotalCoinsEarned = TotalGemsEarned = 0;
        HighestTowerFloor = 0;
        Array.Clear(rarityObtained, 0, RarityCount);
        traitLedger.Clear();
        Changed();
    }
}
