using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Online egg production and incubation. Add once to a persistent scene and wire
/// StartIncubation/Hatch/FinishWithGems to UI Buttons.
/// </summary>
public class SlimeEggSystem : MonoBehaviour
{
    public static SlimeEggSystem Instance { get; private set; }

    [Serializable]
    public class Egg
    {
        public string id;
        public bool isIncubating;
        public float incubationElapsed;
    }

    [Serializable]
    private class EggSave { public List<Egg> eggs = new List<Egg>(); public float layTimer; }

    public enum StatQuality { Poor, Normal, Good, Excellent, Perfect, GodRoll }

    [Header("Egg production")]
    [Min(1f)] public float checkIntervalSeconds = 60f;
    [Range(0f, 1f)] public float eggChance = 0.5f;
    [Min(1)] public int maxUnhatchedEggs = 3;
    [Min(2)] public int requiredSlimes = 2;

    [Header("Incubation")]
    [Min(1f)] public float incubationDurationSeconds = 600f;
    [Min(1f)] public float secondsPerGem = 60f;

    [SerializeField] private List<Egg> eggs = new List<Egg>();
    private float layTimer;
    private float saveTimer;
    private const string SaveKey = "SlimeEggSystem_v1";

    public event Action EggsChanged;
    public event Action<Slime> SlimeHatched;

    public IReadOnlyList<Egg> Eggs => eggs;
    public int EggCount => eggs.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadState();
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime; // production only advances while the player is in-game
        TickProduction(dt);
        foreach (var egg in eggs)
            if (egg.isIncubating)
                egg.incubationElapsed = Mathf.Min(incubationDurationSeconds, egg.incubationElapsed + dt);

        saveTimer += dt;
        if (saveTimer >= 5f) { saveTimer = 0f; SaveState(); }
    }

    private void TickProduction(float dt)
    {
        var manager = BreedingManager.Instance;
        if (manager == null || manager.GetCurrentSlimeCount() < requiredSlimes || eggs.Count >= maxUnhatchedEggs)
            return;

        layTimer += dt;
        while (layTimer >= checkIntervalSeconds && eggs.Count < maxUnhatchedEggs)
        {
            layTimer -= checkIntervalSeconds;
            if (UnityEngine.Random.value < eggChance)
            {
                eggs.Add(new Egg { id = Guid.NewGuid().ToString("N") });
                SaveAndNotify();
            }
        }
    }

    public bool StartIncubation(int eggIndex)
    {
        if (!TryGetEgg(eggIndex, out var egg) || egg.isIncubating) return false;
        egg.isIncubating = true;
        SaveAndNotify();
        return true;
    }

    public float GetRemainingSeconds(int eggIndex)
    {
        return TryGetEgg(eggIndex, out var egg) && egg.isIncubating
            ? Mathf.Max(0f, incubationDurationSeconds - egg.incubationElapsed)
            : incubationDurationSeconds;
    }

    public int GetFinishGemCost(int eggIndex)
    {
        return Mathf.CeilToInt(GetRemainingSeconds(eggIndex) / Mathf.Max(1f, secondsPerGem));
    }

    public bool FinishWithGems(int eggIndex)
    {
        if (!TryGetEgg(eggIndex, out var egg) || !egg.isIncubating) return false;
        int cost = GetFinishGemCost(eggIndex);
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendCurrency(CurrencyType.Gems, cost)) return false;
        egg.incubationElapsed = incubationDurationSeconds;
        SaveAndNotify();
        return true;
    }

    public Slime Hatch(int eggIndex)
    {
        if (!TryGetEgg(eggIndex, out var egg) || !egg.isIncubating || egg.incubationElapsed < incubationDurationSeconds)
            return null;
        if (BreedingManager.Instance == null)
        {
            Debug.LogWarning("[Egg] BreedingManager is not ready; cannot add the hatched slime yet.");
            return null;
        }
        var slime = GenerateEggSlime();
        if (slime == null) return null;
        eggs.RemoveAt(eggIndex);
        BreedingManager.Instance.GetAllSlimes().Add(slime);
        SaveAndLoadSystem.Instance?.Save();
        SaveAndNotify();
        SlimeHatched?.Invoke(slime);
        return slime;
    }

    private Slime GenerateEggSlime()
    {
        Rarity rarity = RollRarity();
        if (SlimeGen.Instance == null || SlimeGen.Instance.allTraits == null) return null;
        TraitSO body = PickTrait(TraitType.Body, rarity);
        TraitSO armor = PickTrait(TraitType.Armor, rarity);
        TraitSO weapon = PickTrait(TraitType.Weapon, rarity);
        if (body == null || armor == null || weapon == null)
        {
            Debug.LogError($"[Egg] Missing Body/Armor/Weapon TraitSO for {rarity}.");
            return null;
        }

        var slime = new Slime
        {
            slimeName = $"Egg_{rarity}_{BreedingManager.Instance.GetCurrentSlimeCount() + 1}",
            body = body.GenerateInstance(), armor = armor.GenerateInstance(), weapon = weapon.GenerateInstance()
        };
        StatQuality quality = RollQuality(out float roll);
        StatRange range = GetRange(rarity);

        // One quality roll is shared by all stats: a God Roll is consistently strong.
        slime.body.HP = slime.body.baseHP = LerpInt(range.hpMin, range.hpMax, roll);
        slime.body.defense = slime.body.baseDefense = LerpInt(range.defMin, range.defMax, roll);
        slime.body.speed = slime.body.baseSpeed = LerpInt(range.speedMin, range.speedMax, roll);
        slime.weapon.attack = slime.weapon.baseAttack = LerpInt(range.atkMin, range.atkMax, roll);
        slime.totalMagicAttack = LerpInt(range.magicMin, range.magicMax, roll);
        slime.critRate = Mathf.Lerp(range.critRateMin, range.critRateMax, roll);
        slime.critDamage = Mathf.Lerp(range.critDamageMin, range.critDamageMax, roll);
        slime.eggStatRollPercent = roll * 100f;
        slime.eggStatQuality = quality.ToString();
        slime.CalculateStats();
        return slime;
    }

    private TraitSO PickTrait(TraitType type, Rarity rarity)
    {
        var pool = SlimeGen.Instance.allTraits.Where(t => t != null && t.type == type && t.rarity == rarity && t.dropRate > 0f).ToList();
        return pool.Count == 0 ? null : pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    private static Rarity RollRarity()
    {
        float r = UnityEngine.Random.value * 100f;
        if (r < 45f) return Rarity.Common;
        if (r < 80f) return Rarity.Uncommon;
        if (r < 94f) return Rarity.Rare;
        if (r < 99f) return Rarity.SuperRare;
        return Rarity.UltraRare;
    }

    private static StatQuality RollQuality(out float roll)
    {
        float r = UnityEngine.Random.value * 100f;
        float min, max; StatQuality quality;
        if (r < 15f) { quality = StatQuality.Poor; min = 0f; max = .20f; }
        else if (r < 45f) { quality = StatQuality.Normal; min = .20f; max = .40f; }
        else if (r < 75f) { quality = StatQuality.Good; min = .40f; max = .60f; }
        else if (r < 93f) { quality = StatQuality.Excellent; min = .60f; max = .80f; }
        else if (r < 99f) { quality = StatQuality.Perfect; min = .80f; max = .95f; }
        else { quality = StatQuality.GodRoll; min = .95f; max = 1f; }
        roll = UnityEngine.Random.Range(min, max);
        return quality;
    }

    private struct StatRange
    {
        public int hpMin, hpMax, atkMin, atkMax, magicMin, magicMax, defMin, defMax, speedMin, speedMax;
        public float critRateMin, critRateMax, critDamageMin, critDamageMax;
    }

    private static StatRange GetRange(Rarity rarity)
    {
        switch (rarity)
        {
            case Rarity.Uncommon: return R(2000,2700,450,650,600,850,700,1000,88,103,20,28,130,140);
            case Rarity.Rare: return R(2700,3700,600,850,800,1100,900,1300,95,110,28,36,140,155);
            case Rarity.SuperRare: return R(3700,5000,800,1100,1050,1450,1200,1700,100,118,36,45,155,170);
            case Rarity.UltraRare: return R(5000,6500,1000,1400,1350,1850,1600,2300,108,125,45,55,170,190);
            default: return R(1500,2000,350,500,450,650,500,800,80,95,15,20,120,130);
        }
    }

    private static StatRange R(int a,int b,int c,int d,int e,int f,int g,int h,int i,int j,float k,float l,float m,float n)
    { return new StatRange { hpMin=a,hpMax=b,atkMin=c,atkMax=d,magicMin=e,magicMax=f,defMin=g,defMax=h,speedMin=i,speedMax=j,critRateMin=k,critRateMax=l,critDamageMin=m,critDamageMax=n }; }
    private static int LerpInt(int min, int max, float t) => Mathf.RoundToInt(Mathf.Lerp(min, max, t));
    private bool TryGetEgg(int index, out Egg egg) { egg = index >= 0 && index < eggs.Count ? eggs[index] : null; return egg != null; }
    private void SaveAndNotify() { SaveState(); EggsChanged?.Invoke(); }
    private void SaveState() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new EggSave { eggs = eggs, layTimer = layTimer })); PlayerPrefs.Save(); }
    private void LoadState() { if (!PlayerPrefs.HasKey(SaveKey)) return; var s = JsonUtility.FromJson<EggSave>(PlayerPrefs.GetString(SaveKey)); if (s != null) { eggs = s.eggs ?? new List<Egg>(); layTimer = s.layTimer; } }
    private void OnApplicationPause(bool paused) { if (paused) SaveState(); }
    private void OnApplicationQuit() { SaveState(); }
}
