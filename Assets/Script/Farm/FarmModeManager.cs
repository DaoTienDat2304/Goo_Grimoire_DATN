using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

[System.Serializable]
public class FarmDifficulty
{
    [Header("Difficulty Info")]
    public string difficultyName;
    public string description;
    
    [Header("Boss Stats (Fixed)")]
    public int bossHP = 100;
    public int bossAttack = 50;
    public int bossDefense = 30;
    public int bossSpeed = 20;
    public int bossEvade = 10;
    
    [Header("Reward")]
    public int rewardCoins = 50;
    
    [Header("Unlock Status")]
    public bool unlocked = false;  // Độ khó đã được mở khóa chưa
    public bool completed = false;  // Đã hoàn thành độ khó này chưa
}

public class FarmModeManager : MonoBehaviour
{
    public static FarmModeManager Instance { get; private set; }
    
    [Header("Farm Difficulties")]
    [SerializeField] private List<FarmDifficulty> difficulties = new List<FarmDifficulty>();
    
    [Header("Warning UI")]
    public GameObject warningText;

    private static readonly WaitForSeconds WarningDelay = new(3f);

    [Header("Scene Settings")]
    [SerializeField] private string battleSceneName = "TurnBaseGame";
    [SerializeField] private string returnSceneName = "firstsave";
    
    private FarmDifficulty selectedDifficulty;
    private int rewardCoins = 0;

    /// <summary>Tên độ khó đang được chọn — dùng cho analytics.</summary>
    public string SelectedDifficultyName => selectedDifficulty?.difficultyName ?? "none";
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Khởi tạo mặc định nếu chưa có difficulties
        if (difficulties == null || difficulties.Count == 0)
        {
            InitializeDefaultDifficulties();
        }

        // Độ khó đầu tiên luôn được unlock
        if (difficulties.Count > 0)
        {
            difficulties[0].unlocked = true;
        }
    }

    /// <summary>
    /// Gọi từ RemoteConfigManager sau khi fetch xong để cập nhật lại stats boss.
    /// </summary>
    public void RefreshDifficultyStats()
    {
        if (difficulties == null || difficulties.Count == 0) return;
        var rc = RemoteConfigManager.Instance;
        if (rc == null) return;

        void Apply(int i, int hp, int atk, int def, int spd, int eva, int reward)
        {
            if (i >= difficulties.Count) return;
            difficulties[i].bossHP      = hp;
            difficulties[i].bossAttack  = atk;
            difficulties[i].bossDefense = def;
            difficulties[i].bossSpeed   = spd;
            difficulties[i].bossEvade   = eva;
            difficulties[i].rewardCoins = reward;
        }

        Apply(0, rc.FarmEasyBossHP,    rc.FarmEasyBossAttack,    rc.FarmEasyBossDefense,    rc.FarmEasyBossSpeed,    rc.FarmEasyBossEvade,    rc.FarmEasyReward);
        Apply(1, rc.FarmMediumBossHP,  rc.FarmMediumBossAttack,  rc.FarmMediumBossDefense,  rc.FarmMediumBossSpeed,  rc.FarmMediumBossEvade,  rc.FarmMediumReward);
        Apply(2, rc.FarmHardBossHP,    rc.FarmHardBossAttack,    rc.FarmHardBossDefense,    rc.FarmHardBossSpeed,    rc.FarmHardBossEvade,    rc.FarmHardReward);
        Apply(3, rc.FarmExtremeBossHP, rc.FarmExtremeBossAttack, rc.FarmExtremeBossDefense, rc.FarmExtremeBossSpeed, rc.FarmExtremeBossEvade, rc.FarmExtremeReward);
        Apply(4, rc.FarmHellBossHP,    rc.FarmHellBossAttack,    rc.FarmHellBossDefense,    rc.FarmHellBossSpeed,    rc.FarmHellBossEvade,    rc.FarmHellReward);

        Debug.Log("FarmModeManager: Đã cập nhật difficulty stats từ Remote Config.");
    }
    
    void Start()
    {
        // Load trạng thái unlock từ save file
        LoadUnlockStatus();
    }
    
    private void InitializeDefaultDifficulties()
    {
        var rc = RemoteConfigManager.Instance;

        FarmDifficulty Make(string name, string desc,
            int hp, int atk, int def, int spd, int eva, int reward) =>
            new FarmDifficulty
            {
                difficultyName = name,
                description    = desc,
                bossHP         = rc != null ? rc.GetFarmStat(name, "hp",      hp)     : hp,
                bossAttack     = rc != null ? rc.GetFarmStat(name, "attack",   atk)   : atk,
                bossDefense    = rc != null ? rc.GetFarmStat(name, "defense",  def)   : def,
                bossSpeed      = rc != null ? rc.GetFarmStat(name, "speed",    spd)   : spd,
                bossEvade      = rc != null ? rc.GetFarmStat(name, "evade",    eva)   : eva,
                rewardCoins    = rc != null ? rc.GetFarmStat(name, "reward",   reward): reward,
            };

        difficulties = new List<FarmDifficulty>
        {
            Make("easy",    "Boss yếu, reward ít",       100,  30,  20,  15,  5,   50),
            Make("medium",  "Boss vừa, reward vừa",      200,  60,  40,  25,  10,  150),
            Make("hard",    "Boss mạnh, reward nhiều",   400,  120, 80,  40,  20,  300),
            Make("extreme", "Boss cực mạnh, reward lớn", 800,  200, 150, 60,  35,  600),
            Make("hell",    "Thử thách tối thượng",      1500, 350, 250, 90,  50,  1200),
        };

        // Gán tên hiển thị riêng (khác với key RC)
        difficulties[0].difficultyName = "Dễ";
        difficulties[1].difficultyName = "Trung Bình";
        difficulties[2].difficultyName = "Khó";
        difficulties[3].difficultyName = "Cực Khó";
        difficulties[4].difficultyName = "Địa Ngục";
    }
    
    /// <summary>
    /// Chọn độ khó và bắt đầu farm mode
    /// </summary>
    public async void SelectDifficulty(int difficultyIndex)
    {
        if (difficultyIndex < 0 || difficultyIndex >= difficulties.Count)
        {
            Debug.LogError($"Invalid difficulty index: {difficultyIndex}");
            return;
        }
        
        // Kiểm tra team phải có ít nhất 1 slime
        var saveSystem = SaveAndLoadSystem.Instance;
        var team = saveSystem != null ? saveSystem.GetTeam() : null;
        if (team == null || team.team == null || team.team.Count == 0)
        {
            Debug.LogWarning("Cần ít nhất 1 slime trong team để vào Farm!");
            ShowWarning();
            return;
        }

        if (warningText != null) warningText.SetActive(false);

        // Kiểm tra unlock
        if (!difficulties[difficultyIndex].unlocked)
        {
            Debug.LogWarning($"Độ khó {difficulties[difficultyIndex].difficultyName} chưa được mở khóa!");
            return;
        }
        
        selectedDifficulty = difficulties[difficultyIndex];
        rewardCoins = selectedDifficulty.rewardCoins;

        FirebaseAnalyticsManager.LogFarmDifficultySelect(selectedDifficulty.difficultyName, difficultyIndex);

        // Tạo boss với random traits nhưng stats cố định
        Slime bossSlime = CreateFarmBoss(selectedDifficulty);
        
        if (bossSlime == null)
        {
            Debug.LogError("Failed to create farm boss!");
            return;
        }
        
        // Setup BattleDataManager
        if (BattleDataManager.Instance == null)
        {
            GameObject battleDataManagerGO = new GameObject("BattleDataManager");
            battleDataManagerGO.AddComponent<BattleDataManager>();
        }
        
        BattleDataManager.Instance.SetBossData(bossSlime, BattleMode.Farm);
        
        // Load battle scene với loading
        await SceneLoader.LoadSceneWithLoading(battleSceneName);
    }
    
    /// <summary>
    /// Tạo boss với random traits (không Secret) nhưng stats cố định
    /// </summary>
    private Slime CreateFarmBoss(FarmDifficulty difficulty)
    {
        if (SlimeGen.Instance == null)
        {
            Debug.LogError("SlimeGen.Instance is null!");
            return null;
        }
        
        // Lấy random traits (không Secret)
        TraitSO bodyTrait = RollRandomTraitExcludingSecret(TraitType.Body);
        TraitSO armorTrait = RollRandomTraitExcludingSecret(TraitType.Armor);
        TraitSO weaponTrait = RollRandomTraitExcludingSecret(TraitType.Weapon);
        
        if (bodyTrait == null || armorTrait == null || weaponTrait == null)
        {
            Debug.LogError("Failed to roll traits for farm boss!");
            return null;
        }
        
        // Tạo slime với traits
        Slime bossSlime = new Slime();
        bossSlime.slimeName = $"Farm Boss - {difficulty.difficultyName}";
        bossSlime.body = bodyTrait.GenerateInstance();
        bossSlime.armor = armorTrait.GenerateInstance();
        bossSlime.weapon = weaponTrait.GenerateInstance();
        
        // Đặt stats cố định (không dùng CalculateStats vì nó sẽ tính từ traits)
        bossSlime.totalHP = difficulty.bossHP;
        bossSlime.totalAttack = difficulty.bossAttack;
        bossSlime.totalDefense = difficulty.bossDefense;
        bossSlime.totalSpeed = difficulty.bossSpeed;
        bossSlime.totalEvade = difficulty.bossEvade;
        
        return bossSlime;
    }
    
    /// <summary>
    /// Roll random trait loại trừ Secret rarity
    /// </summary>
    private TraitSO RollRandomTraitExcludingSecret(TraitType type)
    {
        if (SlimeGen.Instance == null || SlimeGen.Instance.allTraits == null)
        {
            Debug.LogError("SlimeGen.Instance or allTraits is null!");
            return null;
        }
        
        // Lọc traits: cùng type, không phải Secret, có dropRate > 0
        var pool = SlimeGen.Instance.allTraits
            .Where(t => t != null 
                && t.type == type 
                && t.rarity != Rarity.Secret 
                && t.dropRate > 0f)
            .ToList();
        
        if (pool.Count == 0)
        {
            Debug.LogError($"No valid traits found for type: {type}");
            return null;
        }
        
        // Tính tổng dropRate
        float totalRate = pool.Sum(t => t.dropRate);
        if (totalRate <= 0f)
        {
            Debug.LogError($"Total drop rate is 0 for type: {type}");
            return pool[0]; // Fallback
        }
        
        // Roll random
        float roll = Random.Range(0f, totalRate);
        float cumulative = 0f;
        
        foreach (var trait in pool)
        {
            cumulative += trait.dropRate;
            if (roll <= cumulative)
            {
                return trait;
            }
        }
        
        return pool[0]; // Fallback
    }
    
    /// <summary>
    /// Lấy reward coins khi thắng
    /// </summary>
    public int GetRewardCoins()
    {
        return rewardCoins;
    }
    
    /// <summary>
    /// Xử lý khi thắng farm battle
    /// </summary>
    public void OnFarmVictory()
    {
        // Tìm difficulty index đã chọn
        int completedIndex = -1;
        if (selectedDifficulty != null)
        {
            for (int i = 0; i < difficulties.Count; i++)
            {
                if (difficulties[i] == selectedDifficulty)
                {
                    completedIndex = i;
                    break;
                }
            }
        }
        
        // Đánh dấu đã hoàn thành
        if (completedIndex >= 0 && completedIndex < difficulties.Count)
        {
            difficulties[completedIndex].completed = true;
            
            // Unlock độ khó tiếp theo
            if (completedIndex + 1 < difficulties.Count)
            {
                difficulties[completedIndex + 1].unlocked = true;
                Debug.Log($"Đã mở khóa độ khó: {difficulties[completedIndex + 1].difficultyName}");
            }
        }
        
        // Thêm coins
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, rewardCoins);
            Debug.Log($"Farm Victory! Nhận được {rewardCoins} coins!");
        }
        else
        {
            Debug.LogWarning("CurrencyManager.Instance is null! Không thể thêm coins.");
        }
        
        // Lưu game (bao gồm unlock status)
        if (SaveAndLoadSystem.Instance != null)
        {
            SaveAndLoadSystem.Instance.Save();
        }
        
        // Clear battle data
        if (BattleDataManager.Instance != null)
        {
            BattleDataManager.Instance.ClearBossData();
        }
        
        // Reset
        selectedDifficulty = null;
        rewardCoins = 0;
    }
    
    /// <summary>
    /// Lưu trạng thái unlock
    /// </summary>
    public void SaveUnlockStatus()
    {
        if (SaveAndLoadSystem.Instance != null)
        {
            SaveAndLoadSystem.Instance.Save();
        }
    }
    
    /// <summary>
    /// Load trạng thái unlock từ save file
    /// </summary>
    private void LoadUnlockStatus()
    {
        if (SaveAndLoadSystem.Instance != null)
        {
            SaveAndLoadSystem.Instance.LoadFarmDifficulties();
        }
    }
    
    /// <summary>
    /// Kiểm tra độ khó có được unlock không
    /// </summary>
    public bool IsDifficultyUnlocked(int difficultyIndex)
    {
        if (difficultyIndex < 0 || difficultyIndex >= difficulties.Count)
        {
            return false;
        }
        return difficulties[difficultyIndex].unlocked;
    }
    
    /// <summary>
    /// Kiểm tra độ khó đã hoàn thành chưa
    /// </summary>
    public bool IsDifficultyCompleted(int difficultyIndex)
    {
        if (difficultyIndex < 0 || difficultyIndex >= difficulties.Count)
        {
            return false;
        }
        return difficulties[difficultyIndex].completed;
    }
    
    /// <summary>
    /// Lấy danh sách difficulties (để UI hiển thị)
    /// </summary>
    public List<FarmDifficulty> GetDifficulties()
    {
        return difficulties;
    }
    
    /// <summary>
    /// Lấy số lượng difficulties
    /// </summary>
    public int GetDifficultyCount()
    {
        return difficulties != null ? difficulties.Count : 0;
    }

    private void ShowWarning()
    {
        if (warningText == null) return;
        StopCoroutine(nameof(HideWarningAfterDelay));
        warningText.SetActive(true);
        StartCoroutine(nameof(HideWarningAfterDelay));
    }

    private IEnumerator HideWarningAfterDelay()
    {
        yield return WarningDelay;
        if (warningText != null)
            warningText.SetActive(false);
    }
}

