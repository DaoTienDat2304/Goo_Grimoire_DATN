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
    [SerializeField] private Team teamSlime; // drag Team.asset vào đây trong Inspector
    [SerializeField] private TowerSlimeBosses towerDatabase; // Kéo TowerSlimeBosses asset vào đây


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
    /// Chờ user login → chờ cloud check → Load từ cloud JSON → LoadWorld.
    /// </summary>
    IEnumerator InitializeAsync()
    {
        // 1. Chờ AuthManager sẵn sàng và user đã login
        Debug.Log("[Save] Đang chờ Auth...");
        yield return new WaitUntil(() =>
            AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn);

        Debug.Log($"[Save] Auth xong. uid={AuthManager.Instance.CurrentUserId}");

        // 2. Chờ CloudSaveProvider kiểm tra xong cloud save cho tài khoản này
        yield return new WaitUntil(() =>
            CloudSaveProvider.Instance == null || CloudSaveProvider.Instance.CloudCheckDone);

        // Reset flag để đảm bảo Load() chạy đầy đủ khi đăng nhập lại trong cùng session
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.firstLoadDone = false;

        // 3. Chọn save mới hơn giữa cloud và local (PlayerPrefs)
        string cloudJson = (CloudSaveProvider.Instance != null && CloudSaveProvider.Instance.HasCloudSave)
            ? CloudSaveProvider.Instance.GetCachedJson()
            : null;
        string localJson = LocalSaveStore.Load(AuthManager.Instance.LocalSaveId);

        string chosenJson;
        if (!string.IsNullOrEmpty(cloudJson) && !string.IsNullOrEmpty(localJson))
        {
            bool localNewer = LocalSaveStore.GetSavedAt(localJson) > LocalSaveStore.GetSavedAt(cloudJson);
            chosenJson = localNewer ? localJson : cloudJson;
            Debug.Log($"[Save] Có cả cloud lẫn local — dùng {(localNewer ? "local" : "cloud")} (mới hơn).");
        }
        else
        {
            chosenJson = cloudJson ?? localJson;
        }

        if (!string.IsNullOrEmpty(chosenJson))
        {
            Load(chosenJson);
        }
        else
        {
            Debug.Log("[Save] Tài khoản mới — bắt đầu game với dữ liệu mặc định.");
            ResetGameState();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DevAccountInitializer.IsDevAccount())
            {
                DevAccountInitializer.InitializeDevSlimes();
            }
            else
#endif
            {
                // Tài khoản mới thường: tạo 2 slime khởi đầu (Starter_1, Starter_2).
                // ResetGameState() vừa xóa sạch slime nên phải tạo lại ở đây,
                // nếu không người chơi vào game sau tutorial sẽ không có slime nào.
                if (BreedingManager.Instance != null)
                    BreedingManager.Instance.CreateInitialSlimes();
            }
            // Tài khoản mới: khởi tạo bộ daily đầu tiên.
            DailyMissionManager.Instance?.ApplyLoad(null, null, null, false);
            Save(); // lưu ngay để lần sau login/replay có sẵn
        }

        // 4. Nếu có kết quả tower chưa được lưu, apply lên dữ liệu vừa load rồi save lại
        ApplyTowerResultCache();

        // 5. Load world
        yield return StartCoroutine(LoadWorld());

        // 6. Bật auto-save sau khi đã load xong (tránh ghi đè save thật bằng dữ liệu rỗng)
        _initialized = true;
        if (autoSaveEnabled) StartCoroutine(AutoSaveLoop());
    }

    // ---------- Auto Save ----------
    [Header("Auto Save")]
    [SerializeField] private bool autoSaveEnabled = true;
    [Tooltip("Khoảng thời gian (giây) giữa các lần tự lưu định kỳ.")]
    [SerializeField] private float autoSaveInterval = 60f;

    /// <summary>Chỉ true sau khi InitializeAsync load xong — gate cho mọi auto-save.</summary>
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
        // Trên mobile OnApplicationQuit thường không bắn — lưu khi app xuống nền.
        if (paused && _initialized) Save();
    }

    IEnumerator LoadWorld()
    {
        yield return new WaitForSeconds(0.1f); // delay 0.1 gi�y
        if (wildSlimes.tamedSlimes != null) breedingManager.GenTamedSlime();
        SlimeWorldManager.RefreshWorldSlimes();
        breedingUI.RefreshAllUI();
    }
    public void Save()
    {
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

        var json = JsonUtility.ToJson(data, true);

        // Luôn lưu cục bộ bằng PlayerPrefs — không mất save khi thoát/replay,
        // kể cả ở offline dev mode khi cloud chưa bật. Dùng LocalSaveId (guest = key
        // cố định) để save không bị lệch key mỗi phiên đăng nhập ẩn danh.
        string localId = AuthManager.Instance != null ? AuthManager.Instance.LocalSaveId : "guest";
        LocalSaveStore.Save(localId, json);

        // Lưu cloud (khi đã đăng nhập và bật Firebase)
        if (CloudSaveProvider.Instance != null
            && AuthManager.Instance != null
            && AuthManager.Instance.IsLoggedIn)
        {
            CloudSaveProvider.Instance.StartSave(
                AuthManager.Instance.CurrentUserId, json);
            Debug.Log($"[Save] Đang lưu cloud. savedAt={data.lastSavedAt}");
        }
    }

    public void Load(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[Save] Load: json trống, bỏ qua.");
            return;
        }

        var data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null) { Debug.LogWarning("Failed to parse save file."); return; }
        TeamPanel.SetActive(false);

        DeserializeUnlockedTraits(data); // traits first (used by slimes visuals)
        DeserializeSlimes(data);
        DeserializeBreedingSession(data); // sau khi slimes đã có (tra bố mẹ theo id)
        DeserializeTeam(data);
        DeserializeBuildings(data);
        DeserializeDaily(data);   // trước Quests để daily quest có mặt khi khôi phục state
        DeserializeQuests(data);
        DeserializeAchievements(data);
        if (!CurrencyManager.Instance.firstLoadDone)
        {
            CurrencyManager.Instance.firstLoadDone = true;
            DeserializeCurrencies(data);
            DeserializeTowerFloors(data);
            DeserializeFarmDifficulties(data);
        }  
        DeserializeResources(data);
        DeserializeStats(data);
        //DeserializeTamedSlimes(data);

        breedingUI.RefreshAllUI();
        SlimeWorldManager.RefreshWorldSlimes();
        slimeInventory.RefreshAllUI();

        Debug.Log("[Save] Game loaded từ cloud JSON.");
    }

    /// <summary>Trả về Team asset để các hệ thống khác kiểm tra team trước khi vào battle.</summary>
    public Team GetTeam() => teamSlime;

    /// <summary>
    /// Apply kết quả tower battle đã cache vào dữ liệu in-memory (sau khi Load từ cloud),
    /// sau đó save lên cloud một lần và xóa cache.
    /// </summary>
    void ApplyTowerResultCache()
    {
        if (towerDatabase == null || !towerDatabase.hasPendingResult) return;

        int completedFloor = towerDatabase.cachedCompletedFloorNumber;
        towerDatabase.currentFloor       = towerDatabase.cachedCurrentFloor;
        towerDatabase.highestFloorReached = towerDatabase.cachedHighestFloor;

        var floor = towerDatabase.GetFloor(completedFloor);
        if (floor != null) floor.completed = true;

        // Xóa cache trước khi save
        towerDatabase.hasPendingResult         = false;
        towerDatabase.cachedCurrentFloor       = 0;
        towerDatabase.cachedHighestFloor       = 0;
        towerDatabase.cachedCompletedFloorNumber = 0;

        Save();
        Debug.Log($"[Save] Applied tower cache: floor {completedFloor} completed, currentFloor={towerDatabase.currentFloor}");
    }

    void ResetGameState()
    {
        if (teamSlime != null) teamSlime.team.Clear();

        var bm = BreedingManager.Instance;
        if (bm != null) bm.SetAllSlimes(new List<Slime>());

        // Tài khoản mới → xoá sạch bộ đếm lifetime.
        PlayerStatsManager.Instance?.ResetAll();
    }

    // ---------- Team ----------
    void SerializeTeam(GameSaveData data)
    {
        data.teamSlimeIDs.Clear();
        if (teamSlime != null && teamSlime.team != null)
        {
            foreach (var s in teamSlime.team)
            {
                if (s != null)
                    data.teamSlimeIDs.Add(s.id);
            }
        }
    }

    void DeserializeTeam(GameSaveData data)
    {
        if (teamSlime == null || data.teamSlimeIDs == null) return;

        var bm = BreedingManager.Instance;
        if (bm == null) return;

        var all = bm.GetAllSlimes();
        if (all == null) return;

        teamSlime.team.Clear();
        foreach (var id in data.teamSlimeIDs)
        {
            var s = all.FirstOrDefault(x => x != null && x.id == id);
            if (s != null) 
            {
                s.isPicked = true; // Đảm bảo slime trong team có isPicked = true
                teamSlime.team.Add(s);
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

    // ---------- Stats (bộ đếm lifetime cho Thành tựu/Nhiệm vụ) ----------
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

        // Bootstrap ledger trait cho save cũ: gộp trait của các slime đang sở hữu.
        var bm = BreedingManager.Instance;
        if (bm != null) st.MergeOwnedSlimeTraits(bm.GetAllSlimes());
    }

    // ---------- Slimes ----------
    void SerializeSlimes(GameSaveData data)
    {
        var bm = BreedingManager.Instance;
        if (bm == null) return;

        var all = bm.GetAllSlimes();
        if (all == null) return;

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
    }

    void DeserializeSlimes(GameSaveData data)
    {
        var bm = BreedingManager.Instance;
        if (bm == null) return;

        var list = new List<Slime>();
        foreach (var dto in data.slimes)
        {
            var s = new Slime();
            s.slimeName = dto.slimeName;
            s.generation = dto.generation;
            s.breedingCooldown = dto.breedingCooldown;
            s.canBreed = dto.canBreed;
            s.parents = dto.parents ?? new List<string>();
            s.happiness = dto.happiness;
            s.experience = dto.experience;
            s.isPicked = dto.isPicked;
            // totalMagicAttack/crit sẽ được CalculateStats() tính lại từ trait bên dưới.
            // Chỉ giữ metadata chất lượng roll (không tái tạo được từ trait).
            s.eggStatRollPercent = dto.eggStatRollPercent;
            s.eggStatQuality = dto.eggStatQuality;

            s.body = FromTraitDTO(dto.body);
            s.armor = FromTraitDTO(dto.armor);
            s.weapon = FromTraitDTO(dto.weapon);

            // Tính lại totals từ traits (đã được recalculate theo Remote Config trong FromTraitDTO)
            // KHÔNG dùng dto.totalXxx vì đó là giá trị cũ trước khi Remote Config thay đổi
            s.CalculateStats();
            s.id = dto.id;
            list.Add(s);
        }
        bm.SetAllSlimes(list);

        // Refresh any world UI/actors that reflect slimes
        var swm = FindFirstObjectByType<SlimeWorldManager>();
        if (swm != null)
        {
            // you may have a method to refresh; if not, scene reload may be needed
        }
    }

    // ---------- Breeding Session (mục 3) ----------
    void SerializeBreedingSession(GameSaveData data)
    {
        var bm = BreedingManager.Instance;
        var session = bm != null ? bm.GetActiveSessionForSave() : null;
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
        if (ti == null || ti.baseTrait == null) return null;
        return new TraitInstanceDTO
        {
            traitName = ti.baseTrait.traitName,
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
            baseCritDMG = ti.baseCritDMG
        };
    }

    TraitInstance FromTraitDTO(TraitInstanceDTO dto)
    {
        if (dto == null || string.IsNullOrEmpty(dto.traitName)) return null;

        var so = ResolveTraitSO(dto.traitName, dto.type);
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

        // Migration: save cũ không có base stats → ước tính từ multiplier mặc định
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
            ti.baseHP           = dto.baseHP;
            ti.baseAttack       = dto.baseAttack;
            ti.baseDefense      = dto.baseDefense;
            ti.baseSpeed        = dto.baseSpeed;
            ti.baseMagicAttack  = dto.baseMagicAttack;
            ti.baseCritRate     = dto.baseCritRate;
            ti.baseCritDMG      = dto.baseCritDMG;
        }

        // Áp dụng multiplier hiện tại (có thể đã thay đổi qua Remote Config)
        float currentMult = ti.GetRarityMultiplier(dto.rarity);
        ti.RecalculateStats(currentMult);
        return ti;
    }

    TraitSO ResolveTraitSO(string traitName, TraitType type)
    {
        if (string.IsNullOrEmpty(traitName)) return null;

        // Prefer SlimeGen registry
        var gen = SlimeGen.Instance;
        if (gen != null && gen.allTraits != null && gen.allTraits.Count > 0)
        {
            return gen.allTraits.FirstOrDefault(t =>
                t != null && t.traitName == traitName && t.type == type);
        }

        // Fallback: load from Resources if set up
        var loaded = Resources.LoadAll<TraitSO>(string.Empty);
        foreach (var t in loaded)
        {
            if (t != null && t.traitName == traitName && t.type == type) return t;
        }
        return null;
    }

    // ---------- Traits (unlocked flags) ----------
    void SerializeUnlockedTraits(GameSaveData data)
    {
        var gen = SlimeGen.Instance;
        if (gen == null || gen.allTraits == null) return;
        foreach (var t in gen.allTraits)
        {
            if (t != null && t.unlocked)
            {
                data.unlockedTraits.Add(t.traitName);
            }
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
        var slots = FindObjectsByType<BuildingSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in slots)
        {
            var dto = new PlacedBuildingDTO
            {
                slotIndex = s.slotIndex, // or a custom index you assign
                buildingID = s.slotID,                     // slot stores buildingID after place
                isOccupied = s.isOccupied
            };
            data.placedBuildings.Add(dto);
        }
    }

    void DeserializeBuildings(GameSaveData data)
    {
        var slots = FindObjectsByType<BuildingSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        // Sử dụng ToLookup để xử lý trường hợp nhiều slot có cùng slotIndex
        var lookup = slots.ToLookup(s => s.slotIndex);

        foreach (var pb in data.placedBuildings)
        {
            // Lấy slot đầu tiên nếu có nhiều slot cùng slotIndex
            var slot = lookup[pb.slotIndex]?.FirstOrDefault();
            if (slot == null) continue;

            slot.isOccupied = pb.isOccupied;
            slot.slotID = pb.buildingID;

            // Refresh icon if occupied
            if (slot.isOccupied && slot.placedBuildingIcon != null)
            {
                var building = ResolveBuildingByID(pb.buildingID);
                if (building != null)
                {
                    slot.placedBuildingIcon.sprite = building.sprite;
                    slot.placedBuildingIcon.enabled = true;
                }
            }
        }
    }

    Building ResolveBuildingByID(int id)
    {
        if (BuildingManager.Instance == null) return null;
        return BuildingManager.Instance.allBuildings.FirstOrDefault(b => b != null && b.buildingID == id);
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
        // Lưu theo AchievementCatalog (định nghĩa bằng code). Trạng thái mở khóa nằm ở PlayerPrefs "ACH_{id}".
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

        // Đồng bộ lại UI/visual thành tựu theo trạng thái vừa nạp.
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
        
        // Khởi tạo list nếu chưa có
        if (wildSlimes.tamedSlimes == null)
        {
            wildSlimes.tamedSlimes = new System.Collections.Generic.List<WildSlimes.WildSlimeTraits>();
        }
        
        // Clear và load lại từ save file để đảm bảo đồng bộ
        wildSlimes.tamedSlimes.Clear();
        
        foreach (var dto in data.tamedSlimes)
        {
            if (dto == null) continue;
            
            // Tạo lại WildSlimeTraits từ DTO
            var tamed = new WildSlimes.WildSlimeTraits();
            tamed.slimeID = dto.slimeID;
            tamed.slimeType = (WildSlimeType)dto.slimeType;
            tamed.wildSlimeTraits = new TraitSO[3];
            
            for (int i = 0; i < 3 && i < dto.traitNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(dto.traitNames[i]))
                {
                    // Tìm TraitSO bằng tên và type
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
        // Tìm towerDatabase nếu chưa được gán
        if (towerDatabase == null)
        {
            // Tìm từ TurnSystem
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
                        Debug.Log("Tự động tìm thấy TowerDatabase từ TurnSystem");
                    }
                }
            }
            
            // Nếu vẫn null, tìm từ Resources
            if (towerDatabase == null)
            {
                var allTowers = Resources.FindObjectsOfTypeAll<TowerSlimeBosses>();
                if (allTowers != null && allTowers.Length > 0)
                {
                    towerDatabase = allTowers[0];
                    Debug.Log("Tự động tìm thấy TowerDatabase từ Resources");
                }
            }
        }
        
        if (towerDatabase == null)
        {
            Debug.LogWarning("TowerDatabase không tồn tại! Không thể lưu tower progress.");
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
        // Tìm towerDatabase nếu chưa được gán
        if (towerDatabase == null)
        {
            // Tìm từ TurnSystem
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
                        Debug.Log("Tự động tìm thấy TowerDatabase từ TurnSystem");
                    }
                }
            }
            
            // Nếu vẫn null, tìm từ Resources
            if (towerDatabase == null)
            {
                var allTowers = Resources.FindObjectsOfTypeAll<TowerSlimeBosses>();
                if (allTowers != null && allTowers.Length > 0)
                {
                    towerDatabase = allTowers[0];
                    Debug.Log("Tự động tìm thấy TowerDatabase từ Resources");
                }
            }
        }
        
        if (towerDatabase == null)
        {
            Debug.LogWarning("TowerDatabase không tồn tại! Không thể load tower progress.");
            return;
        }
        
        if (towerDatabase.floors == null)
        {
            Debug.LogWarning("TowerDatabase.floors is null!");
            return;
        }
        
        if (data.towerFloors == null)
        {
            Debug.Log("Không có tower floors data trong save file.");
            return;
        }
        
        // Tạo dictionary để tìm floor nhanh
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
                Debug.LogWarning($"Không tìm thấy floor {dto.floorNumber} trong towerDatabase!");
            }
        }
        
        Debug.Log($"DeserializeTowerFloors: Loaded {data.towerFloors.Count} floor progress (claimed: {loadedClaimed})");
    }

    // ---------- Farm Difficulties ----------
    void SerializeFarmDifficulties(GameSaveData data)
    {
        if (FarmModeManager.Instance == null) return;
        
        var difficulties = FarmModeManager.Instance.GetDifficulties();
        if (difficulties == null) return;
        
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
        
        Debug.Log($"SerializeFarmDifficulties: Lưu {data.farmDifficulties.Count} difficulties");
    }
    
    void DeserializeFarmDifficulties(GameSaveData data)
    {
        if (FarmModeManager.Instance == null || data.farmDifficulties == null) return;
        
        var difficulties = FarmModeManager.Instance.GetDifficulties();
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
        
        // Đảm bảo độ khó đầu tiên luôn unlock
        if (difficulties.Count > 0)
        {
            difficulties[0].unlocked = true;
        }
        
        Debug.Log($"DeserializeFarmDifficulties: Loaded {data.farmDifficulties.Count} difficulty progress");
    }
    
    /// <summary>
    /// Public method để FarmModeManager gọi khi cần load farm difficulties.
    /// Dùng JSON đã cache sẵn từ cloud (không load lại).
    /// </summary>
    public void LoadFarmDifficulties()
    {
        string json = CloudSaveProvider.Instance?.GetCachedJson();
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[Save] LoadFarmDifficulties: chưa có cloud JSON.");
            return;
        }

        var data = JsonUtility.FromJson<GameSaveData>(json);
        if (data == null) { Debug.LogWarning("[Save] LoadFarmDifficulties: parse thất bại."); return; }

        DeserializeFarmDifficulties(data);
    }

    // ---------- Public API ----------
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
