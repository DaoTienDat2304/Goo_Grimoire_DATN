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
    public int bossHP = 6000;
    public int bossAttack = 180;
    public int bossMagicAttack = 360;
    public int bossDefense = 780;
    public int bossSpeed = 90;
    public float bossCritRate = 0.05f;
    public float bossCritDMG = 1.30f;
    [Tooltip("Không còn dùng — hệ evade đã bị thay bằng hệ crit. Giữ lại cho tương thích save cũ.")]
    public int bossEvade = 0;

    [Header("Reward")]
    public int rewardCoins = 500;
    public int rewardGems = 0;

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
    private int rewardGems = 0;

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

    /// <summary>Key Remote Config của từng bậc độ khó, theo đúng thứ tự trong danh sách.</summary>
    private static readonly string[] DifficultyKeys = { "easy", "medium", "hard", "extreme", "hell" };

    /// <summary>
    /// Gọi từ RemoteConfigManager sau khi fetch xong để cập nhật lại stats boss.
    /// Nguồn: key `farm_difficulty_table` (JSON). Không có bảng → giữ nguyên số hiện tại.
    /// </summary>
    public void RefreshDifficultyStats()
    {
        if (difficulties == null || difficulties.Count == 0) return;
        if (RemoteBalance.FarmRows == null) return;

        int applied = 0;
        for (int i = 0; i < difficulties.Count && i < DifficultyKeys.Length; i++)
        {
            var row = RemoteBalance.GetFarmRow(DifficultyKeys[i]) ?? RemoteBalance.GetFarmRowAt(i);
            if (row == null) continue;
            ApplyRow(difficulties[i], row);
            applied++;
        }

        Debug.Log($"FarmModeManager: Đã cập nhật {applied} bậc độ khó từ Remote Config.");
    }

    /// <summary>Đổ 1 dòng `farm_difficulty_table` vào 1 bậc độ khó (giữ nguyên tên hiển thị nếu row không có).</summary>
    private static void ApplyRow(FarmDifficulty target, RcFarmRow row)
    {
        if (target == null || row == null) return;
        if (!string.IsNullOrEmpty(row.name)) target.difficultyName = row.name;
        target.bossHP          = row.hp;
        target.bossAttack      = row.atk;
        target.bossMagicAttack = row.magic;
        target.bossDefense     = row.def;
        target.bossSpeed       = row.speed;
        target.bossCritRate    = row.critRate;
        target.bossCritDMG     = row.critDmg;
        target.rewardCoins     = row.coins;
        target.rewardGems      = row.gems;
    }
    
    void Start()
    {
        // Load trạng thái unlock từ save file
        LoadUnlockStatus();
    }
    
    /// <summary>
    /// Bảng mặc định trong code — đã tái cân bằng theo thang chỉ số mới
    /// (mid-range StatBalance của độ hiếm tương ứng × hệ số BossStatScaling).
    /// Remote Config `farm_difficulty_table` sẽ ghi đè ngay sau khi fetch xong.
    /// </summary>
    private void InitializeDefaultDifficulties()
    {
        FarmDifficulty Make(string name, string desc,
            int hp, int atk, int magic, int def, int spd, float critRate, float critDmg, int coins, int gems) =>
            new FarmDifficulty
            {
                difficultyName  = name,
                description     = desc,
                bossHP          = hp,
                bossAttack      = atk,
                bossMagicAttack = magic,
                bossDefense     = def,
                bossSpeed       = spd,
                bossCritRate    = critRate,
                bossCritDMG     = critDmg,
                rewardCoins     = coins,
                rewardGems      = gems,
            };

        difficulties = new List<FarmDifficulty>
        {
            Make("Dễ",         "Boss yếu, reward ít",       6000,  180,  360,  780,  90,  0.05f, 1.30f, 500,   0),
            Make("Trung Bình", "Boss vừa, reward vừa",      11000, 325,  650,  1480, 105, 0.06f, 1.35f, 1200,  0),
            Make("Khó",        "Boss mạnh, reward nhiều",   24000, 640,  1290, 2790, 121, 0.08f, 1.45f, 3000,  2),
            Make("Cực Khó",    "Boss cực mạnh, reward lớn", 46000, 1160, 2325, 5270, 141, 0.10f, 1.55f, 7000,  5),
            Make("Địa Ngục",   "Thử thách tối thượng",      87000, 2125, 4250, 9500, 162, 0.13f, 1.70f, 15000, 10),
        };

        // Nếu Remote Config đã sẵn sàng ngay lúc này thì áp luôn.
        RefreshDifficultyStats();
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
        // Hệ số thưởng remote (`reward_mult_farm_coins`) áp ngay lúc chọn độ khó.
        rewardCoins = RemoteBalance.ScaleReward(selectedDifficulty.rewardCoins, RemoteBalance.Reward.farmCoins);
        rewardGems = selectedDifficulty.rewardGems;

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
        // Hệ evade đã được thay bằng hệ crit — magic/crit nay lấy từ bảng độ khó
        // (`farm_difficulty_table` trên Remote Config), không còn hardcode.
        bossSlime.totalMagicAttack = difficulty.bossMagicAttack;
        bossSlime.totalCritRate = difficulty.bossCritRate;
        bossSlime.totalCritDMG = difficulty.bossCritDMG;

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
        
        // Đếm lifetime: 1 lần thắng Farm (cho Thành tựu "Nông dân").
        PlayerStatsManager.Instance?.AddFarmWin();

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
        
        // Thêm coins (+ gems nếu bậc độ khó có)
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, rewardCoins);
            if (rewardGems > 0) CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, rewardGems);
            Debug.Log($"Farm Victory! Nhận được {rewardCoins} coins" + (rewardGems > 0 ? $" + {rewardGems} gems!" : "!"));
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
        rewardGems = 0;
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

