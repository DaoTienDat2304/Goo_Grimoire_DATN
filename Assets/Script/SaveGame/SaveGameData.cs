using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public long lastSavedAt;

    public List<SlimeDTO> slimes = new List<SlimeDTO>();
    public List<string> unlockedTraits = new List<string>();
    public List<int> teamSlimeIDs = new List<int>();
    public List<PlacedBuildingDTO> placedBuildings = new List<PlacedBuildingDTO>();
    public List<QuestDTO> quests = new List<QuestDTO>();
    public List<AchievementDTO> achievements = new List<AchievementDTO>();
    public List<CurrencyEntry> currencies = new List<CurrencyEntry>();
    public List<ResourceEntry> resources = new List<ResourceEntry>();
    public List<WildSlimeTraitsDTO> tamedSlimes = new List<WildSlimeTraitsDTO>();
    public List<TowerFloorProgressDTO> towerFloors = new List<TowerFloorProgressDTO>();
    public int towerCurrentFloor = 0;
    public int towerHighestFloor = 0;
    public List<FarmDifficultyDTO> farmDifficulties = new List<FarmDifficultyDTO>();
    public BreedingSessionDTO breedingSession;
    public int sacrificePoints;

    public long totalSlimesBred;
    public int  totalFarmWins;
    public int  totalCaptures;
    public int  totalBattleWins;
    public int  totalMutations;
    public long totalCoinsEarned;
    public long totalGemsEarned;
    public int  towerHighestFloorStat;
    public List<int> rarityObtainedCount = new List<int>();
    public List<string> unlockedTraitsEver = new List<string>();

    public string lastDailyResetDate;
    public List<int> todayDailyIDs = new List<int>();
    public List<long> todayDailyBaselines = new List<long>();
    public bool dailyStreakClaimed;
}

[Serializable]
public class BreedingSessionDTO
{
    public bool active;
    public int parent1Id;
    public int parent2Id;
    public int eggRarity;    // (int)Rarity
    public long startUnixMs;
    public float duration;
    public int goldPaid;
}

[Serializable]
public class WildSlimeTraitsDTO
{
    public int slimeID;
    public string[] traitNames = new string[3];
    public TraitType[] traitTypes = new TraitType[3];
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
    public float eggStatRollPercent;
    public string eggStatQuality;
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

    public int baseHP;
    public int baseAttack;
    public int baseMagicAttack;
    public int baseDefense;
    public int baseSpeed;
    public float baseCritRate;
    public float baseCritDMG;

    public string skillName;
    public string ultimateSkillName;
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
    public bool completed;
    public bool claimed;
    public int stars;
    public int bestTurnCount;
}

[Serializable]
public class FarmDifficultyDTO
{
    public int difficultyIndex;
    public bool unlocked;
    public bool completed;
}
