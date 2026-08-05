using System.Collections.Generic;
using UnityEngine;
using static TowerTurnSystem;



[System.Serializable]
public class EnemySpawnConfig
{
    public TowerEnemyType enemyType;
    public int level = 1;
}

[System.Serializable]
public class TowerWaveConfig
{
    public List<EnemySpawnConfig> enemies = new List<EnemySpawnConfig>();
}

[CreateAssetMenu(fileName = "TowerSlimeBosses", menuName = "Tower/TowerSlimeBosses")]
public class TowerSlimeBosses : ScriptableObject
{
    [System.Serializable]
    public class TowerFloor
    {
        [Header("Floor Info")]
        public int floorNumber;
        public string floorName;

        [Header("Waves Setup (If empty, spawns default single boss)")]
        public List<TowerWaveConfig> waves = new List<TowerWaveConfig>();

        [Header("Boss Traits")]
        public TraitSO bodyTrait;
        public TraitSO armorTrait;
        public TraitSO weaponTrait;

        [Header("Boss Stats")]
        public int baseHP = 100;
        public int baseAttack = 50;
        public int baseDefense = 30;
        public int baseSpeed = 20;
        public int baseMagicAttack = 50;
        public float baseCritRate = 0.05f;
        public float baseCritDMG = 1.30f;

        [Header("Rewards (Optional)")]
        public int rewardCoins = 0;
        public int rewardGems = 0;
        [Tooltip("Deprecated: Use rewardCoins and rewardGems instead")]
        public int rewardCurrency = 0; // Giữ lại để tương thích với dữ liệu cũ
        public List<TraitSO> rewardTraits = new List<TraitSO>();

        // Runtime-only — không serialize vào asset, luôn reset về false khi build/start mới
        // Được fill bởi SaveAndLoadSystem.DeserializeTowerFloors()
        [System.NonSerialized] public bool completed;
        [System.NonSerialized] public bool claimed;
        [System.NonSerialized] public int stars;
        [System.NonSerialized] public int bestTurnCount;
    }

    public static int CalculateStars(int turns)
    {
        if (turns <= 0) return 3; // Màn đã thắng nhưng chưa có lượt turn: mặc định 3 sao

        // Ngưỡng sao chỉnh từ xa qua key `tower_star_thresholds`.
        var t = RemoteBalance.TowerStars;
        int three = t != null ? t.threeStarMaxTurns : 50;
        int two = t != null ? t.twoStarMaxTurns : 80;

        if (turns <= three) return 3;
        if (turns <= two) return 2;
        return 1;
    }

    [Header("Tower Floors")]
    public List<TowerFloor> floors = new List<TowerFloor>();

    // Runtime-only — không serialize vào asset, luôn reset về 0 khi build/start mới
    // Được fill bởi SaveAndLoadSystem.DeserializeTowerFloors()
    [System.NonSerialized] public int currentFloor;
    [System.NonSerialized] public int highestFloorReached;
    // 0 = đánh tầng hiện tại bình thường; >0 = đang chơi lại tầng đã qua (không nhận thưởng lại)
    [System.NonSerialized] public int replayFloor = 0;

    // Cache kết quả tower battle — được set bởi TurnSystem sau khi thắng,
    // được apply bởi SaveAndLoadSystem sau khi load cloud xong
    [System.NonSerialized] public bool hasPendingResult = false;
    [System.NonSerialized] public int pendingRewardFloor = 0;
    [System.NonSerialized] public int cachedCurrentFloor = 0;
    [System.NonSerialized] public int cachedHighestFloor = 0;
    [System.NonSerialized] public int cachedCompletedFloorNumber = 0;
    [System.NonSerialized] public int cachedCompletedStars = 0;
    [System.NonSerialized] public int cachedCompletedTurnCount = 0;

    public void EnsureFloorCount(int targetCount)
    {
        if (floors == null) floors = new List<TowerFloor>();

        var growth = RemoteBalance.TowerGrowth;

        while (floors.Count < targetCount)
        {
            int nextNum = floors.Count + 1;
            TowerFloor template = floors.Count > 0 ? floors[floors.Count - 1] : null;

            TowerFloor newFloor = new TowerFloor
            {
                floorNumber = nextNum,
                floorName = $"FLOOR {nextNum}",
                bodyTrait = template?.bodyTrait,
                armorTrait = template?.armorTrait,
                weaponTrait = template?.weaponTrait
            };

            if (growth != null)
            {
                // Remote Config (`tower_growth`): chỉ số luỹ tiến theo số tầng.
                ApplyGrowth(newFloor, growth);
            }
            else
            {
                newFloor.baseHP = template != null ? template.baseHP + 20 : 80 + (nextNum * 15);
                newFloor.baseAttack = template != null ? template.baseAttack + 5 : 20 + (nextNum * 5);
                newFloor.baseDefense = template != null ? template.baseDefense + 3 : 10 + (nextNum * 3);
                newFloor.baseSpeed = template != null ? template.baseSpeed + 1 : 12 + nextNum;
                newFloor.rewardCoins = 50 + (nextNum * 10);
                newFloor.rewardGems = (nextNum % 5 == 0) ? 5 : 0;
            }

            floors.Add(newFloor);
        }

        // Chỉ khi bật cờ mới kéo cả các tầng đã thiết kế tay về thang chỉ số mới.
        if (growth != null && growth.applyToAuthoredFloors)
        {
            foreach (var floor in floors)
                if (floor != null) ApplyGrowth(floor, growth);
        }
    }

    /// <summary>Áp công thức luỹ tiến `tower_growth` lên 1 tầng (không đụng traits/waves).</summary>
    private static void ApplyGrowth(TowerFloor floor, RcTowerGrowth g)
    {
        if (floor == null || g == null) return;

        int step = Mathf.Max(0, floor.floorNumber - 1);
        float statScale = Mathf.Pow(Mathf.Max(1f, g.statGrowthPerFloor), step);
        float rewardScale = Mathf.Pow(Mathf.Max(1f, g.rewardGrowthPerFloor), step);

        floor.baseHP = Mathf.RoundToInt(g.baseHP * statScale);
        floor.baseAttack = Mathf.RoundToInt(g.baseAttack * statScale);
        floor.baseMagicAttack = Mathf.RoundToInt(g.baseMagicAttack * statScale);
        floor.baseDefense = Mathf.RoundToInt(g.baseDefense * statScale);
        floor.baseSpeed = Mathf.RoundToInt(g.baseSpeed * statScale);
        floor.rewardCoins = Mathf.RoundToInt(g.rewardCoinsBase * rewardScale);
        floor.rewardGems = (g.gemEveryNFloors > 0 && floor.floorNumber % g.gemEveryNFloors == 0) ? g.gemAmount : 0;
    }

    public TowerFloor GetCurrentFloor()
    {
        if (floors == null || floors.Count == 0) return null;

        int index = currentFloor - 1;
        if (index < 0 || index >= floors.Count) return null;

        return floors[index];
    }

    public TowerFloor GetFloor(int floorNumber)
    {
        if (floors == null) return null;

        foreach (var floor in floors)
        {
            if (floor != null && floor.floorNumber == floorNumber)
            {
                return floor;
            }
        }
        return null;
    }

    public Slime CreateBossSlimeFromFloor(TowerFloor floor)
    {
        if (floor == null) return null;

        Slime bossSlime = new Slime();
        bossSlime.slimeName = floor.floorName;

        if (floor.bodyTrait != null)
            bossSlime.body = floor.bodyTrait.GenerateInstance();
        if (floor.armorTrait != null)
            bossSlime.armor = floor.armorTrait.GenerateInstance();
        if (floor.weaponTrait != null)
            bossSlime.weapon = floor.weaponTrait.GenerateInstance();

        bossSlime.totalHP = floor.baseHP;
        bossSlime.totalAttack = floor.baseAttack;
        bossSlime.totalMagicAttack = floor.baseMagicAttack;
        bossSlime.totalDefense = floor.baseDefense;
        bossSlime.totalSpeed = floor.baseSpeed;
        bossSlime.totalCritRate = floor.baseCritRate;
        bossSlime.totalCritDMG = floor.baseCritDMG;

        bossSlime.CalculateStats();

        bossSlime.totalHP = floor.baseHP;
        bossSlime.totalAttack = floor.baseAttack;
        bossSlime.totalMagicAttack = floor.baseMagicAttack;
        bossSlime.totalDefense = floor.baseDefense;
        bossSlime.totalSpeed = floor.baseSpeed;
        bossSlime.totalCritRate = floor.baseCritRate;
        bossSlime.totalCritDMG = floor.baseCritDMG;

        return bossSlime;
    }

    public void AdvanceToNextFloor()
    {
        currentFloor++;
        if (currentFloor > highestFloorReached)
        {
            highestFloorReached = currentFloor;
        }
    }

    public void ResetProgress()
    {
        currentFloor = 0;
        highestFloorReached = 0;
    }

    /// <summary>
    /// Kiểm tra xem còn floor sau không (sau khi đã advance)
    /// </summary>
    public bool HasNextFloor()
    {
        if (floors == null || floors.Count == 0) return false;
        // currentFloor là 1-based, sau khi advance đã tăng lên
        // Kiểm tra xem floor tiếp theo (index = currentFloor) có tồn tại không
        int nextFloorIndex = currentFloor; // Index của floor tiếp theo (0-based)
        return nextFloorIndex >= 0 && nextFloorIndex < floors.Count;
    }
}