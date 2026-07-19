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
    }

    public enum StatQuality { Poor, Normal, Good, Excellent, Perfect, GodRoll }

    [Header("Egg production")]
    [Min(1f)] public float checkIntervalSeconds = 60f;
    [Range(0f, 1f)] public float eggChance = 0.5f;
    [Min(1)] public int maxUnhatchedEggs = 3;
    [Min(2)] public int requiredSlimes = 2;
    [Tooltip("BẬT (mặc định) = trứng rơi ngoài world phải đi nhặt. Nếu scene không có " +
             "SlimeSpawner/Player thì trứng rơi trong tầm nhìn camera. " +
             "TẮT = trứng sinh thẳng vào túi trứng.")]
    public bool spawnAsWorldEgg = true;

    [Header("World egg spawning")]
    [Tooltip("Optional. When empty, Resources/SlimeEgg.prefab is loaded automatically.")]
    public GameObject worldEggPrefab;
    public Transform worldEggSpawnCenter;
    [Min(0.5f)] public float worldEggSpawnRadius = 4f;
    [Min(0f)] public float minimumDistanceFromPlayer = 2f;
    public LayerMask worldEggObstacleMask;

    [Header("Incubation")]
    [Min(1f)] public float incubationDurationSeconds = 600f;
    [Min(1f)] public float secondsPerGem = 60f;

    [SerializeField] private List<Egg> eggs = new List<Egg>();
    [SerializeField] private List<WorldEggData> worldEggs = new List<WorldEggData>();
    private readonly Dictionary<string, WorldEggPickup> activeWorldEggs = new Dictionary<string, WorldEggPickup>();
    private float layTimer;
    private float saveTimer;
    private const string SaveKey = "SlimeEggSystem_v1";

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
        if (manager == null || manager.GetCurrentSlimeCount() < requiredSlimes || TotalUnhatchedEggCount >= maxUnhatchedEggs)
            return;

        layTimer += dt;
        while (layTimer >= checkIntervalSeconds && TotalUnhatchedEggCount < maxUnhatchedEggs)
        {
            layTimer -= checkIntervalSeconds;
            if (UnityEngine.Random.value < eggChance)
            {
                if (spawnAsWorldEgg) SpawnWorldEgg();
                else eggs.Add(new Egg { id = Guid.NewGuid().ToString("N") }); // thẳng vào túi trứng
                SaveAndNotify();
            }
        }
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

        // Nếu không có mốc (worldEggSpawnCenter) và không có Player, rơi trứng vào trong
        // tầm nhìn camera thay vì gốc (0,0,0) — để trứng luôn hiện trên màn hình ở mọi scene.
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

    /// <summary>Điểm ngẫu nhiên trong vùng nhìn của camera chính, trên mặt phẳng z = 0.</summary>
    private Vector3 GetRandomPointInCameraView()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;
        // Chừa lề 20% mỗi bên để trứng không dính sát mép màn hình.
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
        eggObject.name = $"SlimeEgg_{data.id.Substring(0, Mathf.Min(6, data.id.Length))}";
        WorldEggPickup pickup = eggObject.GetComponent<WorldEggPickup>();
        if (pickup == null) pickup = eggObject.AddComponent<WorldEggPickup>();
        pickup.Initialize(data.id, data.position);
        activeWorldEggs[data.id] = pickup;
    }

    public bool CollectWorldEgg(string eggId, Vector3 worldPosition, Sprite icon)
    {
        int index = worldEggs.FindIndex(item => item != null && item.id == eggId);
        if (index < 0 || eggs.Count >= maxUnhatchedEggs) return false;

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
        // Map stats vào đúng model của main: HP/DEF/Speed ở Body, ATK/Magic ở Weapon,
        // Crit ở Armor (Head). CalculateStats() sẽ cộng dồn thành total tương ứng.
        slime.body.HP = slime.body.baseHP = LerpInt(range.hpMin, range.hpMax, roll);
        slime.body.defense = slime.body.baseDefense = LerpInt(range.defMin, range.defMax, roll);
        slime.body.speed = slime.body.baseSpeed = LerpInt(range.speedMin, range.speedMax, roll);
        slime.weapon.attack = slime.weapon.baseAttack = LerpInt(range.atkMin, range.atkMax, roll);
        slime.weapon.magicAttack = slime.weapon.baseMagicAttack = LerpInt(range.magicMin, range.magicMax, roll);
        // Range crit lưu theo % (15–20, 120–130) → đổi sang đơn vị của main: rate = phân số, dmg = hệ số nhân.
        slime.armor.critRate = slime.armor.baseCritRate = Mathf.Lerp(range.critRateMin, range.critRateMax, roll) / 100f;
        slime.armor.critDMG = slime.armor.baseCritDMG = Mathf.Lerp(range.critDamageMin, range.critDamageMax, roll) / 100f;
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
    private void SaveState() { PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new EggSave { eggs = eggs, worldEggs = worldEggs, layTimer = layTimer })); PlayerPrefs.Save(); }
    private void LoadState() { if (!PlayerPrefs.HasKey(SaveKey)) return; var s = JsonUtility.FromJson<EggSave>(PlayerPrefs.GetString(SaveKey)); if (s != null) { eggs = s.eggs ?? new List<Egg>(); worldEggs = s.worldEggs ?? new List<WorldEggData>(); layTimer = s.layTimer; } }
    private void OnApplicationPause(bool paused) { if (paused) SaveState(); }
    private void OnApplicationQuit() { SaveState(); }
}
