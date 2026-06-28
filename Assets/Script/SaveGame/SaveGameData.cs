using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    /// <summary>Unix timestamp (ms) lúc save — dùng để conflict resolution giữa local và cloud.</summary>
    public long lastSavedAt;

    public List<SlimeDTO> slimes = new List<SlimeDTO>();
    public List<string> unlockedTraits = new List<string>();
    public List<int> teamSlimeIDs = new List<int>();
    public List<PlacedBuildingDTO> placedBuildings = new List<PlacedBuildingDTO>();
    public List<QuestDTO> quests = new List<QuestDTO>();
    public List<AchievementDTO> achievements = new List<AchievementDTO>();
    public List<CurrencyEntry> currencies = new List<CurrencyEntry>();
    public List<ResourceEntry> resources = new List<ResourceEntry>(); // Lưu tài nguyên (marshmallow, etc.)
    public List<WildSlimeTraitsDTO> tamedSlimes = new List<WildSlimeTraitsDTO>(); // Lưu tamedSlimes
    public List<TowerFloorProgressDTO> towerFloors = new List<TowerFloorProgressDTO>(); // Lưu trạng thái tower floors
    public int towerCurrentFloor = 0;    // currentFloor của TowerSlimeBosses
    public int towerHighestFloor = 0;    // highestFloorReached của TowerSlimeBosses
    public List<FarmDifficultyDTO> farmDifficulties = new List<FarmDifficultyDTO>(); // Lưu trạng thái farm difficulties
}

[Serializable]
public class WildSlimeTraitsDTO
{
    public int slimeID;
    public string[] traitNames = new string[3]; // Lưu tên của TraitSO
    public TraitType[] traitTypes = new TraitType[3]; // Lưu type để tìm lại đúng
    public int slimeType; // 0 = Friendly, 1 = Aggressive
}

[Serializable]
public class CurrencyEntry
{
    public CurrencyType type;
    public int amount;
}

[Serializable]
public class ResourceEntry
{
    public ResourceType type;
    public int amount;
}

[Serializable]
public class SlimeDTO
{
    public string slimeName;
    public int generation;
    public float breedingCooldown;
    public bool canBreed;
    public List<string> parents = new List<string>();
    public float happiness;
    public int experience;
    public bool isPicked;
    public int id;

    public TraitInstanceDTO body;
    public TraitInstanceDTO armor;
    public TraitInstanceDTO weapon;

    public int totalHP;
    public int totalAttack;
    public int totalMagicAttack;
    public int totalDefense;
    public int totalSpeed;
    public float totalCritRate;
    public float totalCritDMG;
}

[Serializable]
public class TraitInstanceDTO
{
    public string traitName;
    public Rarity rarity;
    public TraitType type;
    public int HP;
    public int attack;
    public int magicAttack;
    public int defense;
    public int speed;
    public float critRate;
    public float critDMG;

    // Base stats trước multiplier
    public int baseHP;
    public int baseAttack;
    public int baseMagicAttack;
    public int baseDefense;
    public int baseSpeed;
    public float baseCritRate;
    public float baseCritDMG;
}

[Serializable]
public class PlacedBuildingDTO
{
    public int slotIndex;
    public int buildingID;
    public bool isOccupied;
}

[Serializable]
public class QuestDTO
{
    public int questID;
    public int state;
    public int curSlime;
    public float currentTime;
    public int curBattles;
}

[Serializable]
public class AchievementDTO
{
    public string name;
    public bool unlocked;
}

[Serializable]
public class TowerFloorProgressDTO
{
    public int floorNumber;
    public bool completed; // Đã thắng floor này (chờ claim reward)
    public bool claimed;   // Đã nhận thưởng
}

[Serializable]
public class FarmDifficultyDTO
{
    public int difficultyIndex;
    public bool unlocked;
    public bool completed;
}
