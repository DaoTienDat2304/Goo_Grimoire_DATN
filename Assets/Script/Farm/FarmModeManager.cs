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
    [Tooltip("Unused. Kept for old saves.")]
    public int bossEvade = 0;

    [Header("Reward")]
    public int rewardCoins = 500;
    public int rewardGems = 0;

    [Header("Unlock Status")]
    public bool unlocked = false;
    public bool completed = false;
}

public class FarmModeManager : MonoBehaviour
{
    public static FarmModeManager Instance { get; private set; }
    
    [Header("Farm Database (ScriptableObject)")]
    [SerializeField] private FarmDatabaseSO farmDatabase;

    [Header("Farm Difficulties (Fallback if no SO)")]
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

    [Header("Pending Result Cache")]
    public bool hasPendingResult = false;
    public int cachedCompletedIndex = -1;
    public int cachedRewardCoins = 0;
    public int cachedRewardGems = 0;

    public string SelectedDifficultyName => selectedDifficulty?.difficultyName ?? "none";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (difficulties == null || difficulties.Count == 0)
        {
            InitializeDefaultDifficulties();
        }

        if (difficulties.Count > 0)
        {
            difficulties[0].unlocked = true;
        }
    }

    private static readonly string[] DifficultyKeys = { "easy", "medium", "hard", "extreme", "hell" };

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

        Debug.Log($"FarmModeManager: Updated {applied} difficulties from Remote Config.");
    }

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
        LoadUnlockStatus();
    }
    
    /// <summary>
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
            Make("Easy",         "Weak boss, low reward",       6000,  180,  360,  780,  90,  0.05f, 1.30f, 500,   0),
            Make("Normal", "Mid boss, mid reward",      11000, 325,  650,  1480, 105, 0.06f, 1.35f, 1200,  0),
            Make("Hard",        "Strong boss, high reward",   24000, 640,  1290, 2790, 121, 0.08f, 1.45f, 3000,  2),
            Make("Extreme",    "Very strong boss, big reward", 46000, 1160, 2325, 5270, 141, 0.10f, 1.55f, 7000,  5),
            Make("Inferno",   "Ultimate challenge",      87000, 2125, 4250, 9500, 162, 0.13f, 1.70f, 15000, 10),
        };

        RefreshDifficultyStats();
    }
    
    public void SelectDifficulty(int difficultyIndex)
    {
        var diffList = GetDifficulties();
        if (difficultyIndex < 0 || difficultyIndex >= diffList.Count)
        {
            Debug.LogError($"Invalid difficulty index: {difficultyIndex}");
            return;
        }
        
        var saveSystem = SaveAndLoadSystem.Instance;
        var team = saveSystem != null ? saveSystem.GetTeam() : null;
        if (team == null || team.team == null || team.team.Count == 0)
        {
            Debug.LogWarning("Need 1 slime for Farm.");
            ShowWarning();
            return;
        }

        if (warningText != null) warningText.SetActive(false);

        if (!IsDifficultyUnlocked(difficultyIndex))
        {
            Debug.LogWarning($"Difficulty {diffList[difficultyIndex].difficultyName} locked!");
            return;
        }
        
        selectedDifficulty = diffList[difficultyIndex];
        rewardCoins = RemoteBalance.ScaleReward(selectedDifficulty.rewardCoins, RemoteBalance.Reward.farmCoins);
        rewardGems = selectedDifficulty.rewardGems;

        if (farmDatabase != null)
        {
            farmDatabase.activeSelectedDifficultyIndex = difficultyIndex;
        }

        FirebaseAnalyticsManager.LogFarmDifficultySelect(selectedDifficulty.difficultyName, difficultyIndex);

        PlayerPrefs.SetInt("ActiveFarm_Index", difficultyIndex);
        PlayerPrefs.SetInt("ActiveFarm_Coins", rewardCoins);
        PlayerPrefs.SetInt("ActiveFarm_Gems", rewardGems);
        PlayerPrefs.SetString("ActiveFarm_Name", selectedDifficulty.difficultyName);
        PlayerPrefs.Save();

        Slime bossSlime = CreateFarmBoss(selectedDifficulty);
        
        if (bossSlime == null)
        {
            Debug.LogError("Failed to create farm boss!");
            return;
        }
        
        if (BattleDataManager.Instance == null)
        {
            GameObject battleDataManagerGO = new GameObject("BattleDataManager");
            battleDataManagerGO.AddComponent<BattleDataManager>();
        }
        
        BattleDataManager.Instance.SetBossData(bossSlime, BattleMode.Farm);

        if (SaveAndLoadSystem.Instance != null)
        {
            SaveAndLoadSystem.Instance.Save();
            Debug.Log("[FarmModeManager] Saved before Farm battle.");
        }
        
        StartCoroutine(SceneLoader.LoadSceneWithLoadingCoroutine(battleSceneName));
    }
    
    private Slime CreateFarmBoss(FarmDifficulty difficulty)
    {
        if (SlimeGen.Instance == null)
        {
            Debug.LogError("SlimeGen.Instance is null!");
            return null;
        }
        
        TraitSO bodyTrait = RollRandomTraitExcludingSecret(TraitType.Body);
        TraitSO armorTrait = RollRandomTraitExcludingSecret(TraitType.Armor);
        TraitSO weaponTrait = RollRandomTraitExcludingSecret(TraitType.Weapon);
        
        if (bodyTrait == null || armorTrait == null || weaponTrait == null)
        {
            Debug.LogError("Failed to roll traits for farm boss!");
            return null;
        }
        
        Slime bossSlime = new Slime();
        bossSlime.slimeName = $"Farm Boss - {difficulty.difficultyName}";
        bossSlime.body = bodyTrait.GenerateInstance();
        bossSlime.armor = armorTrait.GenerateInstance();
        bossSlime.weapon = weaponTrait.GenerateInstance();
        
        bossSlime.totalHP = difficulty.bossHP;
        bossSlime.totalAttack = difficulty.bossAttack;
        bossSlime.totalDefense = difficulty.bossDefense;
        bossSlime.totalSpeed = difficulty.bossSpeed;
        bossSlime.totalMagicAttack = difficulty.bossMagicAttack;
        bossSlime.totalCritRate = difficulty.bossCritRate;
        bossSlime.totalCritDMG = difficulty.bossCritDMG;

        return bossSlime;
    }
    
    private TraitSO RollRandomTraitExcludingSecret(TraitType type)
    {
        if (SlimeGen.Instance == null || SlimeGen.Instance.allTraits == null)
        {
            Debug.LogError("SlimeGen.Instance or allTraits is null!");
            return null;
        }
        
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
        
        float totalRate = pool.Sum(t => t.dropRate);
        if (totalRate <= 0f)
        {
            Debug.LogError($"Total drop rate is 0 for type: {type}");
            return pool[0];
        }
        
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
        
        return pool[0]; 
    }
    
    public int GetRewardCoins()
    {
        return rewardCoins;
    }

  
    public void SaveUnlockStatus()
    {
        if (SaveAndLoadSystem.Instance != null)
        {
            SaveAndLoadSystem.Instance.Save();
        }
    }

    private void LoadUnlockStatus()
    {
        if (SaveAndLoadSystem.Instance != null)
        {
            SaveAndLoadSystem.Instance.LoadFarmDifficulties();
        }
    }

    public bool IsDifficultyUnlocked(int difficultyIndex)
    {
        var diffList = GetDifficulties();
        if (difficultyIndex < 0 || difficultyIndex >= diffList.Count)
        {
            return false;
        }
        if (difficultyIndex == 0) return true;
        return diffList[difficultyIndex].unlocked;
    }
    
    public bool IsDifficultyCompleted(int difficultyIndex)
    {
        var diffList = GetDifficulties();
        if (difficultyIndex < 0 || difficultyIndex >= diffList.Count)
        {
            return false;
        }
        return diffList[difficultyIndex].completed;
    }

    public List<FarmDifficulty> GetDifficulties()
    {
        if (farmDatabase != null && farmDatabase.difficulties != null && farmDatabase.difficulties.Count > 0)
        {
            return farmDatabase.difficulties;
        }
        return difficulties;
    }

    public int GetDifficultyCount()
    {
        var list = GetDifficulties();
        return list != null ? list.Count : 0;
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

