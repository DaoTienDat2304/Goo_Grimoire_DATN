using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

public class SaveAndLoadSystem : MonoBehaviour
{
    public static SaveAndLoadSystem Instance { get; private set; }
    [SerializeField] private SlimeWorldManager SlimeWorldManager;
    [SerializeField] private BreedingUIManager breedingUI;
    [SerializeField] private BreedingManager breedingManager;
    [SerializeField] private GameObject TeamPanel;
    public WildSlimes wildSlimes;
    [SerializeField] private SlimeInventory slimeInventory;
    [SerializeField] private Team teamSlime;
    [SerializeField] private TowerSlimeBosses towerDatabase;
    [SerializeField] private FarmDatabaseSO farmDatabase;

    private GameSaveData _cachedSaveData;


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    void Start()
    {
        StartCoroutine(InitializeAsync());
    }

    /// <summary>
    /// </summary>
    IEnumerator InitializeAsync()
    {
        Debug.Log("[Save] Waiting for Auth...");
        yield return new WaitUntil(() =>
            AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn);

        Debug.Log($"[Save] Auth xong. uid={AuthManager.Instance.CurrentUserId}");

        yield return new WaitUntil(() =>
            CloudSaveProvider.Instance == null || CloudSaveProvider.Instance.CloudCheckDone);

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.firstLoadDone = false;

        string cloudJson = (CloudSaveProvider.Instance != null && CloudSaveProvider.Instance.HasCloudSave)
            ? CloudSaveProvider.Instance.GetCachedJson()
            : null;
        string localJson = LocalSaveStore.Load(AuthManager.Instance.LocalSaveId);

        string chosenJson;
        if (!string.IsNullOrEmpty(cloudJson) && !string.IsNullOrEmpty(localJson))
        {
            long localTime = LocalSaveStore.GetSavedAt(localJson);
            long cloudTime = LocalSaveStore.GetSavedAt(cloudJson);

            bool localBetter = localTime >= cloudTime;
            chosenJson = localBetter ? localJson : cloudJson;
            Debug.Log($"[Save] Cloud and local found. Using {(localBetter ? "local" : "cloud")} (Local time: {localTime}, Cloud time: {cloudTime}).");
        }
        else
        {
            chosenJson = !string.IsNullOrEmpty(localJson) ? localJson : cloudJson;
        }

        if (!string.IsNullOrEmpty(chosenJson))
        {
            Load(chosenJson);
        }
        else
        {
            Debug.Log("[Save] Tai khoan moi — bat dau game voi du lieu default.");
            ResetGameState();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DevAccountInitializer.IsDevAccount())
            {
                DevAccountInitializer.InitializeDevSlimes();
            }
            else
#endif
            {
                if (BreedingManager.Instance != null)
                    BreedingManager.Instance.CreateInitialSlimes();
            }
            DailyMissionManager.Instance?.ApplyLoad(null, null, null, false);
        }

        _initialized = true;

        ApplyTowerResultCache();
        ApplyFarmResultCache();

        // 5. Load world
        yield return StartCoroutine(LoadWorld());

        if (autoSaveEnabled) StartCoroutine(AutoSaveLoop());
    }

    // ---------- Auto Save ----------
    [Header("Auto Save")]
    [SerializeField] private bool autoSaveEnabled = true;
    [Tooltip("Autosave interval (s).")]
    [SerializeField] private float autoSaveInterval = 60f;

    private bool _initialized;

    IEnumerator AutoSaveLoop()
    {
        var wait = new WaitForSeconds(autoSaveInterval);
        while (autoSaveEnabled)
        {
            yield return wait;
            if (_initialized) Save();
        }
    }

    void OnApplicationQuit()
    {
        if (_initialized) Save();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && _initialized) Save();
    }

    IEnumerator LoadWorld()
    {
        yield return new WaitForSeconds(0.1f);
        if (wildSlimes != null && wildSlimes.tamedSlimes != null && wildSlimes.tamedSlimes.Count > 0)
        {
            BreedingManager manager = breedingManager != null ? breedingManager : BreedingManager.Instance;
            if (manager != null)
            {
                manager.GenTamedSlime();
            }
            else
            {
                Debug.LogWarning("[Save] Tamed slimes found, but BreedingManager is missing. Conversion postponed.");
            }
            Save();
        }
        if (SlimeWorldManager != null) SlimeWorldManager.RefreshWorldSlimes();
        else FindAnyObjectByType<SlimeWorldManager>()?.RefreshWorldSlimes();
        if (breedingUI != null) breedingUI.RefreshAllUI();
    }
    public void Save()
    {
        string localId = AuthManager.Instance != null ? AuthManager.Instance.LocalSaveId : "guest";

        if (_cachedSaveData == null)
        {
            string existingJson = LocalSaveStore.Load(localId);
            if (!string.IsNullOrEmpty(existingJson))
            {
                _cachedSaveData = JsonUtility.FromJson<GameSaveData>(existingJson);
            }
        }

        var data = new GameSaveData();
        data.lastSavedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        SerializeSlimes(data);
        SerializeBreedingSession(data);
        SerializeUnlockedTraits(data);
        SerializeBuildings(data);
        SerializeQuests(data);
        SerializeAchievements(data);
        SerializeCurrencies(data);
        SerializeResources(data);
        SerializeTeam(data);
        SerializeTamedSlimes(data);
        SerializeTowerFloors(data);
        SerializeFarmDifficulties(data);
        SerializeStats(data);
        SerializeDaily(data);
        SerializeSacrifice(data);

        _cachedSaveData = data;

        // Compact JSON is substantially cheaper to allocate and persist on mobile.
        // Pretty printing is useful for diagnostics, but this is a runtime save path.
        var json = JsonUtility.ToJson(data, false);

        LocalSaveStore.Save(localId, json);

        if (CloudSaveProvider.Instance != null
            && AuthManager.Instance != null
            && AuthManager.Instance.IsLoggedIn)
        {
            CloudSaveProvider.Instance.StartSave(
                AuthManager.Instance.CurrentUserId, json);
            Debug.Log($"[Save] Saving cloud. savedAt={data.lastSavedAt}");
        }
    }

    public void Load(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[Save] Load: empty json, ignored.");
            return;
        }

        var data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null) { Debug.LogWarning("Failed to parse save file."); return; }
        _cachedSaveData = data;
        
        if (TeamPanel != null) TeamPanel.SetActive(false);

        try { DeserializeUnlockedTraits(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeUnlockedTraits: {e}"); }
        try { DeserializeSlimes(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeSlimes: {e}"); }
        try { DeserializeTamedSlimes(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeTamedSlimes: {e}"); }
        try { DeserializeBreedingSession(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeBreedingSession: {e}"); }
        try { DeserializeTeam(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeTeam: {e}"); }
        try { DeserializeBuildings(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeBuildings: {e}"); }
        try { DeserializeDaily(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeDaily: {e}"); }
        try { DeserializeQuests(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeQuests: {e}"); }
        try { DeserializeAchievements(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeAchievements: {e}"); }
        try { DeserializeSacrifice(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeSacrifice: {e}"); }
        
        if (CurrencyManager.Instance != null && !CurrencyManager.Instance.firstLoadDone)
        {
            CurrencyManager.Instance.firstLoadDone = true;
            try { DeserializeCurrencies(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeCurrencies: {e}"); }
            try { DeserializeTowerFloors(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeTowerFloors: {e}"); }
            try { DeserializeFarmDifficulties(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeFarmDifficulties: {e}"); }
        }  
        
        try { DeserializeResources(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeResources: {e}"); }
        try { DeserializeStats(data); } catch (Exception e) { Debug.LogError($"[Save] Error in DeserializeStats: {e}"); }

        if (breedingUI != null) breedingUI.RefreshAllUI();
        if (SlimeWorldManager != null) SlimeWorldManager.RefreshWorldSlimes();
        if (slimeInventory != null) slimeInventory.RefreshAllUI();

        Debug.Log($"[Save] Game loaded. Slimes count: {data.slimes?.Count ?? 0}, Placed Buildings count: {data.placedBuildings?.Count ?? 0}");
    }

    public Team GetTeam() => teamSlime;

    /// <summary>
    /// </summary>
    void ApplyTowerResultCache()
    {
        if (towerDatabase == null || !towerDatabase.hasPendingResult) return;

        int completedFloor = towerDatabase.cachedCompletedFloorNumber;
        towerDatabase.currentFloor       = towerDatabase.cachedCurrentFloor;
        towerDatabase.highestFloorReached = towerDatabase.cachedHighestFloor;

        var floor = towerDatabase.GetFloor(completedFloor);
        if (floor != null) floor.completed = true;

        towerDatabase.hasPendingResult         = false;
        towerDatabase.cachedCurrentFloor       = 0;
        towerDatabase.cachedHighestFloor       = 0;
        towerDatabase.cachedCompletedFloorNumber = 0;

        Save();
        Debug.Log($"[Save] Applied tower cache: floor {completedFloor} completed, currentFloor={towerDatabase.currentFloor}");
    }

    void ApplyFarmResultCache()
    {
        if (farmDatabase != null && farmDatabase.hasPendingResult)
        {
            int completedIndex = farmDatabase.cachedCompletedIndex;
            int pCoins = farmDatabase.cachedRewardCoins;
            int pGems = farmDatabase.cachedRewardGems;

            var difficulties = farmDatabase.difficulties;
            if (difficulties != null && completedIndex >= 0 && completedIndex < difficulties.Count)
            {
                difficulties[completedIndex].completed = true;
                if (completedIndex + 1 < difficulties.Count)
                {
                    difficulties[completedIndex + 1].unlocked = true;
                }
            }

            if (CurrencyManager.Instance != null)
            {
                if (pCoins > 0) CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, pCoins);
                if (pGems > 0) CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, pGems);
            }

            farmDatabase.ClearPendingResult();
            Save();
            Debug.Log($"[Save] Applied farm result cache from FarmDatabaseSO: completedIndex={completedIndex}, coins={pCoins}, gems={pGems}");
            return;
        }

        if (PlayerPrefs.HasKey("PendingFarm_Index"))
        {
            int completedIndex = PlayerPrefs.GetInt("PendingFarm_Index", -1);
            int pCoins = PlayerPrefs.GetInt("PendingFarm_Coins", 0);
            int pGems = PlayerPrefs.GetInt("PendingFarm_Gems", 0);

            PlayerPrefs.DeleteKey("PendingFarm_Index");
            PlayerPrefs.DeleteKey("PendingFarm_Coins");
            PlayerPrefs.DeleteKey("PendingFarm_Gems");

            var difficulties = farmDatabase != null ? farmDatabase.difficulties : (FarmModeManager.Instance != null ? FarmModeManager.Instance.GetDifficulties() : null);

            if (difficulties != null && completedIndex >= 0 && completedIndex < difficulties.Count)
            {
                difficulties[completedIndex].completed = true;
                if (completedIndex + 1 < difficulties.Count)
                {
                    difficulties[completedIndex + 1].unlocked = true;
                }
            }

            if (CurrencyManager.Instance != null)
            {
                if (pCoins > 0) CurrencyManager.Instance.AddCurrency(CurrencyType.Coins, pCoins);
                if (pGems > 0) CurrencyManager.Instance.AddCurrency(CurrencyType.Gems, pGems);
            }

            Save();
            Debug.Log($"[Save] Applied farm result cache from PlayerPrefs fallback: completedIndex={completedIndex}");
        }
    }

    void ResetGameState()
    {
        if (teamSlime != null) teamSlime.team.Clear();

        var bm = BreedingManager.Instance;
        if (bm != null) bm.SetAllSlimes(new List<Slime>());

        PlayerStatsManager.Instance?.ResetAll();
    }

    // ---------- Team ----------
    void SerializeTeam(GameSaveData data)
    {
        data.teamSlimeIDs.Clear();
        if (teamSlime != null && teamSlime.team != null && teamSlime.team.Count > 0)
        {
            foreach (var s in teamSlime.team)
            {
                if (s != null)
                    data.teamSlimeIDs.Add(s.id);
            }
            return;
        }

        if (_cachedSaveData != null && _cachedSaveData.teamSlimeIDs != null)
        {
            data.teamSlimeIDs = new List<int>(_cachedSaveData.teamSlimeIDs);
        }
    }

    void DeserializeTeam(GameSaveData data)
    {
        if (teamSlime == null) return;

        var bm = BreedingManager.Instance;
        if (bm == null) return;

        var all = bm.GetAllSlimes();
        if (all == null || all.Count == 0) return;

        teamSlime.team.Clear();
        if (data.teamSlimeIDs != null && data.teamSlimeIDs.Count > 0)
        {
            foreach (var id in data.teamSlimeIDs)
            {
                var s = all.FirstOrDefault(x => x != null && x.id == id);
                if (s != null && !teamSlime.team.Contains(s)) 
                {
                    s.isPicked = true;
                    teamSlime.team.Add(s);
                }
            }
        }

        if (teamSlime.team.Count == 0)
        {
            foreach (var s in all)
            {
                if (s != null && s.isPicked && !teamSlime.team.Contains(s))
                {
                    teamSlime.team.Add(s);
                }
            }
        }
    }

    // ---------- Currency ----------
    void SerializeCurrencies(GameSaveData data)
    {
        if (CurrencyManager.Instance == null) return;

        data.currencies.Clear();
        foreach (CurrencyType t in Enum.GetValues(typeof(CurrencyType)))
        {
            data.currencies.Add(new CurrencyEntry
            {
                type = t,
                amount = CurrencyManager.Instance.GetCurrency(t)
            });
        }
    }

    void DeserializeCurrencies(GameSaveData data)
    {
        if (CurrencyManager.Instance == null || data.currencies == null) return;

        foreach (var entry in data.currencies)
        {
            CurrencyManager.Instance.SetCurrency(entry.type, entry.amount);
        }
    }

    // ---------- Resources ----------
    void SerializeResources(GameSaveData data)
    {
        if (ResourceManager.Instance == null) return;

        data.resources.Clear();
        foreach (ResourceType t in Enum.GetValues(typeof(ResourceType)))
        {
            data.resources.Add(new ResourceEntry
            {
                type = t,
                amount = ResourceManager.Instance.GetResource(t)
            });
        }
    }

    void DeserializeResources(GameSaveData data)
    {
        if (ResourceManager.Instance == null || data.resources == null) return;

        foreach (var entry in data.resources)
        {
            ResourceManager.Instance.SetResource(entry.type, entry.amount);
        }
    }

    // ---------- Daily missions ----------
    void SerializeDaily(GameSaveData data)
    {
        DailyMissionManager.Instance?.WriteTo(data);
    }

    void DeserializeDaily(GameSaveData data)
    {
        DailyMissionManager.Instance?.ApplyLoad(
            data.lastDailyResetDate, data.todayDailyIDs, data.todayDailyBaselines, data.dailyStreakClaimed);
    }

    void SerializeStats(GameSaveData data)
    {
        var st = PlayerStatsManager.Instance;
        if (st == null) return;
        st.WriteTo(data);
    }

    void DeserializeStats(GameSaveData data)
    {
        var st = PlayerStatsManager.Instance;
        if (st == null) return;
        st.LoadFrom(data);

        var bm = BreedingManager.Instance;
        if (bm != null) st.MergeOwnedSlimeTraits(bm.GetAllSlimes());
    }

    private static int GetSlimesCount(string json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        try
        {
            var d = JsonUtility.FromJson<GameSaveData>(json);
            return d?.slimes?.Count ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    // ---------- Slimes ----------
    void SerializeSlimes(GameSaveData data)
    {
        var bm = BreedingManager.Instance;

        bool isBattleScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "TurnBaseGame";

        if (bm != null && !isBattleScene) 
        {
            var all = bm.GetAllSlimes();
            int cachedCount = (_cachedSaveData != null && _cachedSaveData.slimes != null) ? _cachedSaveData.slimes.Count : 0;
            
            if (all != null && all.Count >= cachedCount && all.Count > 0)
            {
                data.slimes.Clear();
                foreach (var s in all)
                {
                    if (s == null) continue;
                    var dto = new SlimeDTO
                    {
                        slimeName = s.slimeName,
                        generation = s.generation,
                        breedingCooldown = s.breedingCooldown,
                        canBreed = s.canBreed,
                        parents = new List<string>(s.parents ?? new List<string>()),
                        happiness = s.happiness,
                        experience = s.experience,
                        isPicked = s.isPicked,
                        totalHP = s.totalHP,
                        totalAttack = s.totalAttack,
                        totalMagicAttack = s.totalMagicAttack,
                        totalDefense = s.totalDefense,
                        totalSpeed = s.totalSpeed,
                        totalCritRate = s.totalCritRate,
                        totalCritDMG = s.totalCritDMG,
                        eggStatRollPercent = s.eggStatRollPercent,
                        eggStatQuality = s.eggStatQuality,
                        body = ToTraitDTO(s.body),
                        armor = ToTraitDTO(s.armor),
                        weapon = ToTraitDTO(s.weapon),
                        id = s.id
                    };
                    data.slimes.Add(dto);
                }
                return;
            }
        }

        if (_cachedSaveData != null && _cachedSaveData.slimes != null && _cachedSaveData.slimes.Count > 0)
        {
            data.slimes = new List<SlimeDTO>(_cachedSaveData.slimes);
        }
    }

    void DeserializeSlimes(GameSaveData data)
    {
        var bm = BreedingManager.Instance;
        if (bm == null) return;

        if (data.slimes == null || data.slimes.Count == 0)
        {
            Debug.Log("[Save] Save has 0 slimes. Keep current or create starter.");
            if (bm.GetAllSlimes() == null || bm.GetAllSlimes().Count == 0)
            {
                bm.CreateInitialSlimes();
            }
            return;
        }

        var list = new List<Slime>();
        foreach (var dto in data.slimes)
        {
            if (dto == null) continue;
            var s = new Slime();
            s.slimeName = dto.slimeName;
            s.generation = dto.generation;
            s.breedingCooldown = dto.breedingCooldown;
            s.canBreed = dto.canBreed;
            s.parents = dto.parents ?? new List<string>();
            s.happiness = dto.happiness;
            s.experience = dto.experience;
            s.isPicked = dto.isPicked;
            s.eggStatRollPercent = dto.eggStatRollPercent;
            s.eggStatQuality = dto.eggStatQuality;

            s.body = FromTraitDTO(dto.body);
            s.armor = FromTraitDTO(dto.armor);
            s.weapon = FromTraitDTO(dto.weapon);

            s.CalculateStats();
            s.id = dto.id;
            list.Add(s);
        }

        if (list.Count > 0)
        {
            var uniqueList = new List<Slime>();
            var seenIds = new HashSet<int>();
            foreach (var slime in list)
            {
                if (slime == null) continue;
                if (slime.id != 0 && seenIds.Contains(slime.id)) continue;
                if (slime.id != 0) seenIds.Add(slime.id);
                uniqueList.Add(slime);
            }
            list = uniqueList;

            StatStandardMigration.NormalizeAll(list);
            bm.SetAllSlimes(list);
        }

        // Refresh any world UI/actors that reflect slimes
        var swm = FindFirstObjectByType<SlimeWorldManager>();
        if (swm != null)
        {
            swm.RefreshWorldSlimes();
        }
    }

    void SerializeBreedingSession(GameSaveData data)
    {
        var bm = BreedingManager.Instance;
        if (bm == null)
        {
            if (_cachedSaveData != null)
                data.breedingSession = _cachedSaveData.breedingSession;
            return;
        }

        var session = bm.GetActiveSessionForSave();
        if (session == null || session.parent1 == null || session.parent2 == null)
        {
            data.breedingSession = new BreedingSessionDTO { active = false };
            return;
        }
        data.breedingSession = new BreedingSessionDTO
        {
            active = true,
            parent1Id = session.parent1.id,
            parent2Id = session.parent2.id,
            eggRarity = (int)session.eggRarity,
            startUnixMs = session.startUnixMs,
            duration = session.duration,
            goldPaid = session.goldPaid
        };
    }

    void DeserializeBreedingSession(GameSaveData data)
    {
        var bm = BreedingManager.Instance;
        if (bm == null || data.breedingSession == null || !data.breedingSession.active) return;

        var s = data.breedingSession;
        bm.RestoreSession(s.parent1Id, s.parent2Id, (Rarity)s.eggRarity, s.startUnixMs, s.duration, s.goldPaid);
    }

    TraitInstanceDTO ToTraitDTO(TraitInstance ti)
    {
        if (ti == null) return null;
        string tName = ti.baseTrait != null ? (string.IsNullOrEmpty(ti.baseTrait.traitName) ? ti.baseTrait.name : ti.baseTrait.traitName) : ti.traitname;
        if (string.IsNullOrEmpty(tName)) return null;

        return new TraitInstanceDTO
        {
            traitName = tName,
            rarity = ti.Rarity,
            type = ti.TraitType,
            HP = ti.HP,
            attack = ti.attack,
            magicAttack = ti.magicAttack,
            defense = ti.defense,
            speed = ti.speed,
            critRate = ti.critRate,
            critDMG = ti.critDMG,
            baseHP = ti.baseHP,
            baseAttack = ti.baseAttack,
            baseMagicAttack = ti.baseMagicAttack,
            baseDefense = ti.baseDefense,
            baseSpeed = ti.baseSpeed,
            baseCritRate = ti.baseCritRate,
            baseCritDMG = ti.baseCritDMG,
            skillName = ti.skill?.baseSkill != null ? ti.skill.baseSkill.name : null,
            ultimateSkillName = ti.ultimateSkill?.baseSkill != null ? ti.ultimateSkill.baseSkill.name : null
        };
    }

    TraitInstance FromTraitDTO(TraitInstanceDTO dto)
    {
        if (dto == null) return null;

        var so = ResolveTraitSO(dto.traitName, dto.type);
        if (so == null)
        {
            Debug.LogWarning($"[Save] ResolveTraitSO not found '{dto.traitName}' (type={dto.type}). Use fallback trait.");
            if (SlimeGen.Instance != null && SlimeGen.Instance.allTraits != null && SlimeGen.Instance.allTraits.Count > 0)
            {
                so = SlimeGen.Instance.allTraits.FirstOrDefault(t => t != null && t.type == dto.type)
                     ?? SlimeGen.Instance.allTraits.FirstOrDefault(t => t != null);
            }
        }

        if (so == null) return null;

        var ti = new TraitInstance(so)
        {
            Rarity = dto.rarity,
            TraitType = dto.type
        };
        ti.HP = dto.HP;
        ti.magicAttack = dto.magicAttack;
        ti.critRate = dto.critRate;
        ti.critDMG = dto.critDMG;
        ti.attack = dto.attack;
        ti.defense = dto.defense;
        ti.speed = dto.speed;

        if (dto.baseAttack == 0 && dto.attack > 0)
        {
            float defaultMult = ti.GetRarityMultiplier(dto.rarity);
            ti.baseAttack       = Mathf.RoundToInt(dto.attack  / defaultMult);
            ti.baseDefense      = Mathf.RoundToInt(dto.defense / defaultMult);
            ti.baseSpeed        = Mathf.RoundToInt(dto.speed   / defaultMult);
            ti.baseMagicAttack  = dto.magicAttack;
            ti.baseCritRate     = dto.critRate;
            ti.baseCritDMG      = dto.critDMG;
            ti.baseHP           = dto.HP;
        }
        else
        {
            ti.baseHP           = dto.baseHP > 0 ? dto.baseHP : dto.HP;
            ti.baseAttack       = dto.baseAttack > 0 ? dto.baseAttack : dto.attack;
            ti.baseDefense      = dto.baseDefense > 0 ? dto.baseDefense : dto.defense;
            ti.baseSpeed        = dto.baseSpeed > 0 ? dto.baseSpeed : dto.speed;
            ti.baseMagicAttack  = dto.baseMagicAttack > 0 ? dto.baseMagicAttack : dto.magicAttack;
            ti.baseCritRate     = dto.baseCritRate > 0 ? dto.baseCritRate : dto.critRate;
            ti.baseCritDMG      = dto.baseCritDMG > 0 ? dto.baseCritDMG : dto.critDMG;
        }

        float currentMult = ti.GetRarityMultiplier(dto.rarity);
        ti.RecalculateStats(currentMult);

        var gen = SlimeGen.Instance;
        if (gen != null)
        {
            gen.EnsureSkillDatabasePublic();
            if (!string.IsNullOrEmpty(dto.skillName))
            {
                var skillSO = gen.allSkillsDatabase?.FirstOrDefault(s => s != null && s.name == dto.skillName);
                if (skillSO != null) ti.skill = new SkillInstance(skillSO);
            }


            // Fallback nếu chưa có skill: lấy từ Trait gốc hoặc bốc random theo độ hiếm
            if (ti.skill == null)
            {
                if (so != null && so.skill != null)
                {
                    ti.skill = new SkillInstance(so.skill);
                }
                else
                {
                    var fallbackSkill = dto.type == TraitType.Weapon 
                        ? gen.GetRandomWeaponSkill(dto.rarity, false) 
                        : gen.GetRandomSkill(dto.type, dto.rarity);
                    if (fallbackSkill != null) ti.skill = new SkillInstance(fallbackSkill);
                }
            }

            // Khôi phục ultimate skill từ tên đã lưu hoặc tự động ghép nếu là Rare+ Weapon
            if (!string.IsNullOrEmpty(dto.ultimateSkillName))
            {
                var ultSO = gen.allSkillsDatabase?.FirstOrDefault(s => s != null && s.name == dto.ultimateSkillName);
                if (ultSO != null) ti.ultimateSkill = new SkillInstance(ultSO);
            }
            else if (ti.ultimateSkill == null && dto.type == TraitType.Weapon && dto.rarity != Rarity.Common && dto.rarity != Rarity.Uncommon)
            {
                if (ti.skill?.baseSkill != null)
                {
                    var matchedUlt = gen.GetMatchingUltimateWeaponSkill(ti.skill.baseSkill);
                    if (matchedUlt != null) ti.ultimateSkill = new SkillInstance(matchedUlt);
                }
            }
        }

        return ti;
    }

    TraitSO ResolveTraitSO(string traitName, TraitType type)
    {
        if (string.IsNullOrEmpty(traitName)) return null;

        string cleanName = traitName.Replace(" ", "").Trim();

        // 1. Prefer SlimeGen registry
        var gen = SlimeGen.Instance;
        if (gen != null && gen.allTraits != null && gen.allTraits.Count > 0)
        {
            var found = gen.allTraits.FirstOrDefault(t =>
                t != null && t.type == type &&
                (string.Equals(t.traitName, traitName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(t.name, traitName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(t.traitName?.Replace(" ", ""), cleanName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(t.name?.Replace(" ", ""), cleanName, StringComparison.OrdinalIgnoreCase)));
            if (found != null) return found;

            var loose = gen.allTraits.FirstOrDefault(t =>
                t != null &&
                (string.Equals(t.traitName, traitName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(t.name, traitName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(t.traitName?.Replace(" ", ""), cleanName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(t.name?.Replace(" ", ""), cleanName, StringComparison.OrdinalIgnoreCase)));
            if (loose != null) return loose;
        }

        // 2. Fallback: load from Resources if set up
        var loaded = Resources.LoadAll<TraitSO>(string.Empty);
        if (loaded != null && loaded.Length > 0)
        {
            foreach (var t in loaded)
            {
                if (t != null && t.type == type &&
                    (string.Equals(t.traitName, traitName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(t.name, traitName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(t.traitName?.Replace(" ", ""), cleanName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(t.name?.Replace(" ", ""), cleanName, StringComparison.OrdinalIgnoreCase)))
                    return t;
            }
            foreach (var t in loaded)
            {
                if (t != null &&
                    (string.Equals(t.traitName, traitName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(t.name, traitName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(t.traitName?.Replace(" ", ""), cleanName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(t.name?.Replace(" ", ""), cleanName, StringComparison.OrdinalIgnoreCase)))
                    return t;
            }
        }
        return null;
    }

    // ---------- Traits (unlocked flags) ----------
    void SerializeUnlockedTraits(GameSaveData data)
    {
        var gen = SlimeGen.Instance;
        if (gen != null && gen.allTraits != null && gen.allTraits.Count > 0)
        {
            data.unlockedTraits.Clear();
            foreach (var t in gen.allTraits)
            {
                if (t != null && t.unlocked)
                {
                    data.unlockedTraits.Add(t.traitName);
                }
            }
            return;
        }

        if (_cachedSaveData != null && _cachedSaveData.unlockedTraits != null)
        {
            data.unlockedTraits = new List<string>(_cachedSaveData.unlockedTraits);
        }
    }

    void DeserializeUnlockedTraits(GameSaveData data)
    {
        var gen = SlimeGen.Instance;
        if (gen == null || gen.allTraits == null) return;

        var set = new HashSet<string>(data.unlockedTraits ?? new List<string>());
        foreach (var t in gen.allTraits)
        {
            if (t == null) continue;
            t.unlocked = set.Contains(t.traitName);
        }
    }

    // ---------- Buildings ----------
    void SerializeBuildings(GameSaveData data)
    {
        bool isBattleScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "TurnBaseGame";

        if (!isBattleScene)
        {
            var slots = FindObjectsByType<BuildingSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (slots != null && slots.Length > 0 && slots.Any(s => s.isOccupied))
            {
                data.placedBuildings.Clear();
                foreach (var s in slots)
                {
                    var dto = new PlacedBuildingDTO
                    {
                        slotIndex = s.slotIndex,
                        buildingID = s.slotID,
                        isOccupied = s.isOccupied
                    };
                    data.placedBuildings.Add(dto);
                }
                return;
            }
        }

        if (_cachedSaveData != null && _cachedSaveData.placedBuildings != null && _cachedSaveData.placedBuildings.Count > 0)
        {
            data.placedBuildings = new List<PlacedBuildingDTO>(_cachedSaveData.placedBuildings);
        }
    }

    void DeserializeBuildings(GameSaveData data)
    {
        if (data.placedBuildings == null) return;

        var slots = FindObjectsByType<BuildingSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (slots == null || slots.Length == 0) return;
        
        var lookup = slots.ToLookup(s => s.slotIndex);

        foreach (var pb in data.placedBuildings)
        {
            var slot = lookup[pb.slotIndex]?.FirstOrDefault();
            if (slot == null) continue;

            slot.isOccupied = pb.isOccupied;
            slot.slotID = pb.buildingID;

            if (slot.placedBuildingIcon == null)
                slot.placedBuildingIcon = slot.GetComponent<UnityEngine.UI.Image>();

            if (slot.isOccupied)
            {
                var building = ResolveBuildingByID(pb.buildingID);
                if (building != null && slot.placedBuildingIcon != null)
                {
                    slot.placedBuildingIcon.sprite = building.sprite;
                    slot.placedBuildingIcon.enabled = true;
                }
            }
            else
            {
                if (slot.placedBuildingIcon != null)
                {
                    slot.placedBuildingIcon.enabled = true;
                }
            }

            slot.RefreshBuildingCollider();
        }
    }

    Building ResolveBuildingByID(int id)
    {
        var bm = BuildingManager.Instance != null ? BuildingManager.Instance : FindFirstObjectByType<BuildingManager>(FindObjectsInactive.Include);
        if (bm == null || bm.allBuildings == null) return null;
        return bm.allBuildings.FirstOrDefault(b => b != null && b.buildingID == id);
    }

    // ---------- Quests ----------
    void SerializeQuests(GameSaveData data)
    {
        var qm = QuestManager.Instance;
        if (qm == null || qm.allQuests == null) return;

        foreach (var q in qm.allQuests)
        {
            if (q == null) continue;
            var dto = new QuestDTO
            {
                questID = q.questID,
                state = (int)q.state
            };

            if (q is BreedingQuest bq) dto.curSlime = bq.curSlime;
            if (q is TimeQuest tq) dto.currentTime = tq.current;
            if (q is BattleQuest battleQ) dto.curBattles = battleQ.curBattles;

            data.quests.Add(dto);
        }
    }

    void DeserializeQuests(GameSaveData data)
    {
        var qm = QuestManager.Instance;
        if (qm == null || qm.allQuests == null) return;

        foreach (var dto in data.quests)
        {
            var q = qm.allQuests.FirstOrDefault(x => x != null && x.questID == dto.questID);
            if (q == null) continue;

            q.state = (Quest.QuestState)dto.state;
            if (q is BreedingQuest bq) bq.curSlime = dto.curSlime;
            if (q is TimeQuest tq) tq.current = dto.currentTime;
            if (q is BattleQuest battleQ) battleQ.curBattles = dto.curBattles;

            if (qm.questUIManager != null) qm.questUIManager.UpdateQuestState(q);
        }
    }

    // ---------- Achievements ----------
    void SerializeAchievements(GameSaveData data)
    {
        foreach (var def in AchievementCatalog.All)
        {
            string key = "ACH_" + def.Id;
            data.achievements.Add(new AchievementDTO
            {
                name = key,
                unlocked = PlayerPrefs.GetInt(key, 0) == 1
            });
        }
    }

    void DeserializeAchievements(GameSaveData data)
    {
        if (data.achievements != null)
        {
            foreach (var a in data.achievements)
            {
                if (string.IsNullOrEmpty(a.name)) continue;
                PlayerPrefs.SetInt(a.name, a.unlocked ? 1 : 0);
            }
            PlayerPrefs.Save();
        }

        ArchievementManager.Instance?.ReloadUnlockStates();
    }

    // ---------- Tamed Slimes ----------
    void SerializeTamedSlimes(GameSaveData data)
    {
        if (wildSlimes == null || wildSlimes.tamedSlimes == null) return;
        
        data.tamedSlimes.Clear();
        foreach (var tamed in wildSlimes.tamedSlimes)
        {
            if (tamed == null || tamed.wildSlimeTraits == null) continue;
            
            var dto = new WildSlimeTraitsDTO();
            dto.slimeID = tamed.slimeID;
            dto.slimeType = (int)tamed.slimeType;
            dto.traitNames = new string[3];
            dto.traitTypes = new TraitType[3];
            
            for (int i = 0; i < 3 && i < tamed.wildSlimeTraits.Length; i++)
            {
                if (tamed.wildSlimeTraits[i] != null)
                {
                    dto.traitNames[i] = tamed.wildSlimeTraits[i].traitName;
                    dto.traitTypes[i] = tamed.wildSlimeTraits[i].type;
                }
            }
            
            data.tamedSlimes.Add(dto);
        }
    }
    
    void DeserializeTamedSlimes(GameSaveData data)
    {
        if (wildSlimes == null || data.tamedSlimes == null) return;
        
        if (wildSlimes.tamedSlimes == null)
        {
            wildSlimes.tamedSlimes = new System.Collections.Generic.List<WildSlimes.WildSlimeTraits>();
        }
        
        wildSlimes.tamedSlimes.Clear();
        
        foreach (var dto in data.tamedSlimes)
        {
            if (dto == null) continue;
            
            var tamed = new WildSlimes.WildSlimeTraits();
            tamed.slimeID = dto.slimeID;
            tamed.slimeType = (WildSlimeType)dto.slimeType;
            tamed.wildSlimeTraits = new TraitSO[3];
            
            for (int i = 0; i < 3 && i < dto.traitNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(dto.traitNames[i]))
                {
                    tamed.wildSlimeTraits[i] = ResolveTraitSO(dto.traitNames[i], dto.traitTypes[i]);
                }
            }
            
            wildSlimes.tamedSlimes.Add(tamed);
        }
        
        Debug.Log($"DeserializeTamedSlimes: Loaded {wildSlimes.tamedSlimes.Count} tamed slimes");
    }

    // ---------- Tower Floors ----------
    void SerializeTowerFloors(GameSaveData data)
    {
        if (towerDatabase == null)
        {
            var turnSystem = FindAnyObjectByType<TurnSystem>();
            if (turnSystem != null)
            {
                var field = typeof(TurnSystem).GetField("towerBosses", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    towerDatabase = field.GetValue(turnSystem) as TowerSlimeBosses;
                    if (towerDatabase != null)
                    {
                        Debug.Log("Found TowerDatabase from TurnSystem");
                    }
                }
            }
            
            if (towerDatabase == null)
            {
                var allTowers = Resources.FindObjectsOfTypeAll<TowerSlimeBosses>();
                if (allTowers != null && allTowers.Length > 0)
                {
                    towerDatabase = allTowers[0];
                    Debug.Log("Found TowerDatabase from Resources");
                }
            }
        }
        
        if (towerDatabase == null)
        {
            Debug.LogWarning("TowerDatabase missing. Cannot save tower progress.");
            return;
        }
        
        if (towerDatabase.floors == null)
        {
            Debug.LogWarning("TowerDatabase.floors is null!");
            return;
        }
        
        data.towerCurrentFloor  = towerDatabase.currentFloor;
        data.towerHighestFloor  = towerDatabase.highestFloorReached;

        data.towerFloors.Clear();
        int claimedCount = 0;

        foreach (var floor in towerDatabase.floors)
        {
            if (floor == null) continue;

            if (floor.claimed) claimedCount++;

            data.towerFloors.Add(new TowerFloorProgressDTO
            {
                floorNumber = floor.floorNumber,
                completed   = floor.completed,
                claimed     = floor.claimed
            });
        }

        Debug.Log($"SerializeTowerFloors: {data.towerFloors.Count} floors, highest={data.towerHighestFloor}, current={data.towerCurrentFloor}, claimed={claimedCount}");
    }
    
    void DeserializeTowerFloors(GameSaveData data)
    {
        if (towerDatabase == null)
        {
            var turnSystem = FindAnyObjectByType<TurnSystem>();
            if (turnSystem != null)
            {
                var field = typeof(TurnSystem).GetField("towerBosses", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    towerDatabase = field.GetValue(turnSystem) as TowerSlimeBosses;
                    if (towerDatabase != null)
                    {
                        Debug.Log("Found TowerDatabase from TurnSystem");
                    }
                }
            }
            
            if (towerDatabase == null)
            {
                var allTowers = Resources.FindObjectsOfTypeAll<TowerSlimeBosses>();
                if (allTowers != null && allTowers.Length > 0)
                {
                    towerDatabase = allTowers[0];
                    Debug.Log("Found TowerDatabase from Resources");
                }
            }
        }
        
        if (towerDatabase == null)
        {
            Debug.LogWarning("TowerDatabase missing. Cannot load tower progress.");
            return;
        }
        
        if (towerDatabase.floors == null)
        {
            Debug.LogWarning("TowerDatabase.floors is null!");
            return;
        }
        
        if (data.towerFloors == null)
        {
            Debug.Log("No tower floor data in save.");
            return;
        }
        
        var floorDict = new Dictionary<int, TowerSlimeBosses.TowerFloor>();
        foreach (var floor in towerDatabase.floors)
        {
            if (floor != null)
            {
                floorDict[floor.floorNumber] = floor;
            }
        }
        
        int loadedClaimed = 0;
        
        towerDatabase.currentFloor       = data.towerCurrentFloor;
        towerDatabase.highestFloorReached = data.towerHighestFloor;

        foreach (var dto in data.towerFloors)
        {
            if (floorDict.TryGetValue(dto.floorNumber, out var floor))
            {
                floor.completed = dto.completed;
                floor.claimed   = dto.claimed;
                if (dto.claimed) loadedClaimed++;
            }
            else
            {
                Debug.LogWarning($"Floor not found {dto.floorNumber} in towerDatabase!");
            }
        }
        
        Debug.Log($"DeserializeTowerFloors: Loaded {data.towerFloors.Count} floor progress (claimed: {loadedClaimed})");
    }

    // ---------- Farm Difficulties ----------
    void SerializeFarmDifficulties(GameSaveData data)
    {
        var difficulties = farmDatabase != null ? farmDatabase.difficulties : (FarmModeManager.Instance != null ? FarmModeManager.Instance.GetDifficulties() : null);
        if (difficulties == null) return;

        if (data.farmDifficulties == null) data.farmDifficulties = new List<FarmDifficultyDTO>();

        data.farmDifficulties.Clear();
        for (int i = 0; i < difficulties.Count; i++)
        {
            if (difficulties[i] != null)
            {
                data.farmDifficulties.Add(new FarmDifficultyDTO
                {
                    difficultyIndex = i,
                    unlocked = difficulties[i].unlocked,
                    completed = difficulties[i].completed
                });
            }
        }

        Debug.Log($"SerializeFarmDifficulties: Save {data.farmDifficulties.Count} difficulties");
    }

    void DeserializeFarmDifficulties(GameSaveData data)
    {
        if (data.farmDifficulties == null) return;
        
        var difficulties = farmDatabase != null ? farmDatabase.difficulties : (FarmModeManager.Instance != null ? FarmModeManager.Instance.GetDifficulties() : null);
        if (difficulties == null) return;
        
        foreach (var dto in data.farmDifficulties)
        {
            if (dto.difficultyIndex >= 0 && dto.difficultyIndex < difficulties.Count)
            {
                difficulties[dto.difficultyIndex].unlocked = dto.unlocked;
                difficulties[dto.difficultyIndex].completed = dto.completed;
                
                Debug.Log($"Deserialize Farm Difficulty {dto.difficultyIndex}: unlocked={dto.unlocked}, completed={dto.completed}");
            }
        }
        
        if (difficulties.Count > 0)
        {
            difficulties[0].unlocked = true;
        }
        
        Debug.Log($"DeserializeFarmDifficulties: Loaded {data.farmDifficulties.Count} difficulty progress");
    }
    
    /// <summary>
    /// </summary>
    public void LoadFarmDifficulties()
    {
        string json = CloudSaveProvider.Instance?.GetCachedJson();
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[Save] LoadFarmDifficulties: no cloud JSON.");
            return;
        }

        var data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null) { Debug.LogWarning("[Save] LoadFarmDifficulties: parse failed."); return; }

        DeserializeFarmDifficulties(data);
    }

    void SerializeSacrifice(GameSaveData data)
    {
        var inv = slimeInventory != null ? slimeInventory : FindAnyObjectByType<SlimeInventory>();
        if (inv != null)
        {
            data.sacrificePoints = inv.sacrifice;
        }
        else if (_cachedSaveData != null)
        {
            data.sacrificePoints = _cachedSaveData.sacrificePoints;
        }
    }

    void DeserializeSacrifice(GameSaveData data)
    {
        if (data == null) return;
        var inv = slimeInventory != null ? slimeInventory : FindAnyObjectByType<SlimeInventory>();
        if (inv != null)
        {
            inv.sacrifice = data.sacrificePoints;
            if (inv.Slider != null) inv.Slider.value = data.sacrificePoints;
        }
    }

    // ---------- Public API ----------
    public GameSaveData GetCachedSaveData() => _cachedSaveData;
    public void QuickSave() => Save();
    public void QuickLoad()
    {
        string json = CloudSaveProvider.Instance?.GetCachedJson();
        if (!string.IsNullOrEmpty(json)) Load(json);
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.P)) Save();
        if (Input.GetKeyUp(KeyCode.Q)) QuickLoad();
    }
}
