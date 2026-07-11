using UnityEngine;
using System.Collections.Generic;



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
    [System.NonSerialized] public int cachedCurrentFloor = 0;
    [System.NonSerialized] public int cachedHighestFloor = 0;
    [System.NonSerialized] public int cachedCompletedFloorNumber = 0;

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
