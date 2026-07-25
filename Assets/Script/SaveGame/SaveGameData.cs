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
    public BreedingSessionDTO breedingSession; // Phiên lai tạo đang chạy (mục 3), null nếu không có

    // ─── Bộ đếm tích luỹ (lifetime) — nền tảng cho Thành tựu & Nhiệm vụ ───
    public long totalSlimesBred;
    public int  totalFarmWins;
    public int  totalCaptures;
    public int  totalBattleWins;
    public int  totalMutations;
    public long totalCoinsEarned;
    public long totalGemsEarned;
    public int  towerHighestFloorStat;                            // tầng tháp cao nhất (cho thành tựu leo tháp)
    public List<int> rarityObtainedCount = new List<int>();      // đếm theo (int)Rarity, 8 phần tử
    public List<string> unlockedTraitsEver = new List<string>(); // ledger trait KHÁC NHAU đã-từng-thấy

    // ─── Nhiệm vụ hàng ngày (Daily) ───
    public string lastDailyResetDate;                            // "yyyy-MM-dd" của bộ daily hiện tại
    public List<int> todayDailyIDs = new List<int>();            // ID các daily được chọn hôm nay
    public List<long> todayDailyBaselines = new List<long>();    // baseline counter lúc sang ngày (song song IDs)
    public bool dailyStreakClaimed;                              // đã nhận bonus hoàn thành cả 3 chưa
}

[Serializable]
public class BreedingSessionDTO
{
    public bool active;
    public int parent1Id;
    public int parent2Id;
    public int eggRarity;    // (int)Rarity
    public long startUnixMs; // mốc bắt đầu (thời gian thực) — để lai tạo chạy nền/offline
    public float duration;
    public int goldPaid;
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
    // Metadata từ hệ thống trứng (mục 1) & lai tạo (mục 3): chất lượng roll khi sinh ra.
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

    // Base stats trước multiplier — nếu = 0 là save cũ, cần migrate
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
