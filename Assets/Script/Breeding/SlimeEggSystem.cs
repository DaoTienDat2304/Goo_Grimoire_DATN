using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public class WorldEggData
    {
        public string id;
        public string sceneName;
        public Vector3 position;
    }

    [Serializable]
    private class EggSave
    {
        public List<Egg> eggs = new List<Egg>();
        public List<WorldEggData> worldEggs = new List<WorldEggData>();
        public float layTimer;
        public long lastTickUnixMs;
    }

    public enum StatQuality { Poor, Normal, Good, Excellent, Perfect, GodRoll }

    [Header("Egg production (fallback — Remote Config Remote Config override)")]
    [Min(1f)] public float checkIntervalSeconds = 60f;
    [Range(0f, 1f)] public float eggChance = 0.5f;
    [Min(1)] public int maxUnhatchedEggs = 3;
    [Min(2)] public int requiredSlimes = 2;
    [Tooltip("ON (default) = eggs drop in world. If scene lacks " +
             "SlimeSpawner/Player, eggs drop in camera. " +
             "OFF: eggs go to bag.")]
    public bool spawnAsWorldEgg = true;

    [Header("World egg spawning")]
    [Tooltip("Optional. When empty, Resources/SlimeEgg.prefab is loaded automatically.")]
    public GameObject worldEggPrefab;
    public Transform worldEggSpawnCenter;
    [Min(0.5f)] public float worldEggSpawnRadius = 4f;
    [Min(0f)] public float minimumDistanceFromPlayer = 2f;
    public LayerMask worldEggObstacleMask;
    
    [Header("Hierarchy Optimization")]
    [SerializeField] private Transform eggsContainer;

    [Header("Incubation (fallback — Remote Config Remote Config override)")]
    [Min(1f)] public float incubationDurationSeconds = 600f;
    [Min(1f)] public float secondsPerGem = 60f;

    private float CheckInterval      => Mathf.Max(1f, RemoteBalance.FloatOr(RemoteConfigKeys.EggCheckInterval, checkIntervalSeconds));
    private float EggChance          => Mathf.Clamp01(RemoteBalance.FloatOr(RemoteConfigKeys.EggChance, eggChance));
    private int   MaxUnhatchedEggs   => Mathf.Max(1, RemoteBalance.IntOr(RemoteConfigKeys.EggMaxUnhatched, maxUnhatchedEggs));
    private int   RequiredSlimes     => Mathf.Max(1, RemoteBalance.IntOr(RemoteConfigKeys.EggRequiredSlimes, requiredSlimes));
    private float IncubationDuration => Mathf.Max(1f, RemoteBalance.FloatOr(RemoteConfigKeys.EggIncubationSecs, incubationDurationSeconds));
    private float SecondsPerGem      => Mathf.Max(1f, RemoteBalance.FloatOr(RemoteConfigKeys.EggSecondsPerGem, secondsPerGem));

    [SerializeField] private List<Egg> eggs = new List<Egg>();
    [SerializeField] private List<WorldEggData> worldEggs = new List<WorldEggData>();
    private readonly Dictionary<string, WorldEggPickup> activeWorldEggs = new Dictionary<string, WorldEggPickup>();
    private float layTimer;
    private float saveTimer;
    private long lastTickUnixMs;
    private const string SaveKey = "SlimeEggSystem_v1";

    private static long NowMs() => System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public event Action EggsChanged;
    public event Action<Vector3, Sprite> WorldEggCollected;
    public event Action<Slime> SlimeHatched;

    public IReadOnlyList<Egg> Eggs => eggs;
    public int EggCount => eggs.Count;
    public int WorldEggCount => worldEggs.Count;
    public int TotalUnhatchedEggCount => eggs.Count + worldEggs.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadState();
        if (lastTickUnixMs <= 0) lastTickUnixMs = NowMs();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start() => RestoreWorldEggsForActiveScene();

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    private void Update()
    {
        long now = NowMs();
        float dt = Mathf.Max(0f, (now - lastTickUnixMs) / 1000f);
        lastTickUnixMs = now;

        TickProduction(dt);
        float incubation = IncubationDuration;
        foreach (var egg in eggs)
            if (egg.isIncubating)
                egg.incubationElapsed = Mathf.Min(incubation, egg.incubationElapsed + dt);

        saveTimer += dt;
        if (saveTimer >= 5f) { saveTimer = 0f; SaveState(); }
    }

    private void TickProduction(float dt)
    {
        float interval = CheckInterval;
        int maxEggs = MaxUnhatchedEggs;

        layTimer = Mathf.Min(layTimer + dt, interval * (maxEggs + 1));

        var manager = BreedingManager.Instance;
        if (manager == null || manager.GetCurrentSlimeCount() < RequiredSlimes)
            return;

        float chance = EggChance;
        while (layTimer >= interval && TotalUnhatchedEggCount < maxEggs)
        {
            layTimer -= interval;
            if (UnityEngine.Random.value < chance)
            {
                if (spawnAsWorldEgg) SpawnWorldEgg();
                else eggs.Add(new Egg { id = Guid.NewGuid().ToString("N") });
                SaveAndNotify();
            }
        }

        if (TotalUnhatchedEggCount >= maxEggs)
            layTimer = Mathf.Min(layTimer, interval);
    }

    private void SpawnWorldEgg()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Vector3 position = FindWorldEggSpawnPosition();
        var data = new WorldEggData
        {
            id = Guid.NewGuid().ToString("N"),
            sceneName = sceneName,
            position = position
        };
        worldEggs.Add(data);
        CreateWorldEggObject(data);
    }

    private Vector3 FindWorldEggSpawnPosition()
    {
        SlimeSpawner slimeSpawner = FindAnyObjectByType<SlimeSpawner>();
        if (slimeSpawner != null)
        {
            Vector3 spawnerPosition = slimeSpawner.GetRandomSpawnPosition();
            if (spawnerPosition != Vector3.zero) return spawnerPosition;
        }

        Transform player = null;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;

        if (worldEggSpawnCenter == null && player == null)
        {
            Vector3 camPoint = GetRandomPointInCameraView();
            if (camPoint != Vector3.zero) return camPoint;
        }

        Vector3 center = worldEggSpawnCenter != null
            ? worldEggSpawnCenter.position
            : player != null ? player.position : transform.position;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * worldEggSpawnRadius;
            Vector3 candidate = center + new Vector3(offset.x, offset.y, 0f);
            if (player != null && Vector3.Distance(candidate, player.position) < minimumDistanceFromPlayer)
                continue;
            if (worldEggObstacleMask.value != 0 && Physics2D.OverlapCircle(candidate, 0.5f, worldEggObstacleMask) != null)
                continue;
            return candidate;
        }
        return center;
    }

    private Vector3 GetRandomPointInCameraView()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;
        float vx = UnityEngine.Random.Range(0.2f, 0.8f);
        float vy = UnityEngine.Random.Range(0.2f, 0.8f);
        float depth = cam.orthographic ? Mathf.Abs(cam.transform.position.z) : 10f;
        Vector3 world = cam.ViewportToWorldPoint(new Vector3(vx, vy, depth));
        world.z = 0f;
        return world;
    }

    private void CreateWorldEggObject(WorldEggData data)
    {
        if (data == null || data.sceneName != SceneManager.GetActiveScene().name || activeWorldEggs.ContainsKey(data.id))
            return;

        GameObject prefab = worldEggPrefab != null ? worldEggPrefab : Resources.Load<GameObject>("SlimeEgg");
        GameObject eggObject = prefab != null
            ? Instantiate(prefab, data.position, Quaternion.identity)
            : new GameObject("SlimeEgg");
        if (eggsContainer != null)
        {
            eggObject.transform.SetParent(eggsContainer, false);
        }
        eggObject.name = $"SlimeEgg_{data.id.Substring(0, Mathf.Min(6, data.id.Length))}";
        WorldEggPickup pickup = eggObject.GetComponent<WorldEggPickup>();
        if (pickup == null) pickup = eggObject.AddComponent<WorldEggPickup>();
        pickup.Initialize(data.id, data.position);
        activeWorldEggs[data.id] = pickup;
    }

    public bool CollectWorldEgg(string eggId, Vector3 worldPosition, Sprite icon)
    {
        int index = worldEggs.FindIndex(item => item != null && item.id == eggId);
        if (index < 0 || eggs.Count >= MaxUnhatchedEggs) return false;

        worldEggs.RemoveAt(index);
        eggs.Add(new Egg { id = eggId });
        activeWorldEggs.TryGetValue(eggId, out WorldEggPickup pickup);
        activeWorldEggs.Remove(eggId);

        WorldEggCollected?.Invoke(worldPosition, icon);
        SaveAndNotify();
        if (pickup != null) Destroy(pickup.gameObject);
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RestoreWorldEggsForActiveScene();

    private void RestoreWorldEggsForActiveScene()
    {
        foreach (WorldEggPickup pickup in activeWorldEggs.Values)
            if (pickup != null) Destroy(pickup.gameObject);
        activeWorldEggs.Clear();

        string sceneName = SceneManager.GetActiveScene().name;
        foreach (WorldEggData data in worldEggs)
            if (data != null && data.sceneName == sceneName)
                CreateWorldEggObject(data);
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
            ? Mathf.Max(0f, IncubationDuration - egg.incubationElapsed)
            : IncubationDuration;
    }

    public int GetFinishGemCost(int eggIndex)
    {
        return Mathf.CeilToInt(GetRemainingSeconds(eggIndex) / Mathf.Max(1f, SecondsPerGem));
    }

    public bool FinishWithGems(int eggIndex)
    {
        if (!TryGetEgg(eggIndex, out var egg) || !egg.isIncubating) return false;
        int cost = GetFinishGemCost(eggIndex);
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendCurrency(CurrencyType.Gems, cost)) return false;
        egg.incubationElapsed = IncubationDuration;
        SaveAndNotify();
        return true;
    }

    public Slime Hatch(int eggIndex)
    {
        if (!TryGetEgg(eggIndex, out var egg) || !egg.isIncubating || egg.incubationElapsed < IncubationDuration)
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

        var worldManager = FindAnyObjectByType<SlimeWorldManager>();
        if (worldManager != null) worldManager.RefreshWorldSlimes();

        SaveAndLoadSystem.Instance?.Save();
        SaveAndNotify();
        SlimeHatched?.Invoke(slime);
        return slime;
    }

    private Slime GenerateEggSlime()
    {
        Rarity rarity = RollRarity();
        if (SlimeGen.Instance == null) return null;

        string name = $"Egg_{rarity}_{BreedingManager.Instance.GetCurrentSlimeCount() + 1}";
        var slime = SlimeGen.Instance.GenerateSlimeOfRarity(name, rarity);
        if (slime == null)
        {
            Debug.LogError("[Egg] Khong tao duoc slime — SlimeGen has no trait ? Check allTraits.");
            return null;
        }

        StatQuality quality = RollQuality(out float roll);
        StatBalance.Range range = StatBalance.Get(rarity);

        if (slime.body != null)
        {
            slime.body.HP = slime.body.baseHP = LerpInt(range.hpMin, range.hpMax, roll);
            slime.body.defense = slime.body.baseDefense = LerpInt(range.defMin, range.defMax, roll);
            slime.body.speed = slime.body.baseSpeed = LerpInt(range.spdMin, range.spdMax, roll);
        }
        if (slime.weapon != null)
        {
            slime.weapon.attack = slime.weapon.baseAttack = LerpInt(range.atkMin, range.atkMax, roll);
            slime.weapon.magicAttack = slime.weapon.baseMagicAttack = LerpInt(range.magMin, range.magMax, roll);
        }
        if (slime.armor != null)
        {
            slime.armor.critRate = slime.armor.baseCritRate = range.critRate;
            slime.armor.critDMG = slime.armor.baseCritDMG = range.critDmg;
        }
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
        if (RemoteBalance.TryRollEggRarity(out var remote)) return remote;

        float r = UnityEngine.Random.value * 100f;
        if (r < 45f) return Rarity.Common;
        if (r < 80f) return Rarity.Uncommon;
        if (r < 94f) return Rarity.Rare;
        if (r < 99f) return Rarity.SuperRare;
        return Rarity.UltraRare;
    }

    private static StatQuality RollQuality(out float roll)
    {
        var bands = RemoteBalance.EggQuality;
        if (bands != null)
        {
            string name = bands.Roll(out roll);
            return Enum.TryParse(name.Replace(" ", string.Empty), true, out StatQuality parsed)
                ? parsed
                : StatQuality.Normal;
        }

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

    private static int LerpInt(int min, int max, float t) => Mathf.RoundToInt(Mathf.Lerp(min, max, t));
    private bool TryGetEgg(int index, out Egg egg) { egg = index >= 0 && index < eggs.Count ? eggs[index] : null; return egg != null; }
    private void SaveAndNotify() { SaveState(); EggsChanged?.Invoke(); }
    private void SaveState() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new EggSave { eggs = eggs, worldEggs = worldEggs, layTimer = layTimer, lastTickUnixMs = lastTickUnixMs })); PlayerPrefs.Save(); }
    private void LoadState() { if (!PlayerPrefs.HasKey(SaveKey)) return; var s = JsonUtility.FromJson<EggSave>(PlayerPrefs.GetString(SaveKey)); if (s != null) { eggs = s.eggs ?? new List<Egg>(); worldEggs = s.worldEggs ?? new List<WorldEggData>(); layTimer = s.layTimer; lastTickUnixMs = s.lastTickUnixMs; } }
    private void OnApplicationPause(bool paused) { if (paused) { lastTickUnixMs = NowMs(); SaveState(); } }
    private void OnApplicationQuit() { SaveState(); }
}
