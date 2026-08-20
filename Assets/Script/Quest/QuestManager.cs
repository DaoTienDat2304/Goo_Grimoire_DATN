using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [Header("Quest System")]
    public List<Quest> allQuests = new List<Quest>();
    public QuestUIManager questUIManager;
    [Header("Dependencies")]
    public BreedingManager breedingManager;
    public TowerSlimeBosses towerDatabase;

    [Header("Mission Catalog")]
    [Tooltip("Tu charge chuoi missions chinh tu MissionCatalog khi Start.")]
    public bool autoLoadMissionCatalog = true;
    [Tooltip("Clear old active quests before loading the catalog.")]
    public bool clearExistingOnLoad = true;
    
    private static QuestManager instance;
    public static QuestManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<QuestManager>();
            }
            return instance;
        }
    }
    
    void Awake()
    {
        foreach (var quest in allQuests)
        {
            if (quest != null)
            {
                quest.state = Quest.QuestState.Locked;
            }
        }
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (questUIManager == null) questUIManager = FindAnyObjectByType<QuestUIManager>();

        InitializeQuests();

        if (autoLoadMissionCatalog) LoadMissionCatalog();
    }

    /// <summary>
    /// </summary>
    public void LoadMissionCatalog()
    {
        if (clearExistingOnLoad && allQuests != null)
        {
            foreach (var old in new List<Quest>(allQuests))
                if (old != null) RemoveQuest(old);
        }

        foreach (var def in MissionCatalog.All)
        {
            var q = ScriptableObject.CreateInstance<CatalogQuest>();
            q.questID = def.Id;
            q.questName = def.Name;
            q.description = def.Description;
            q.slimeRequirement = 0;
            q.questreq = def.PrereqId >= 0 ? new List<int> { def.PrereqId } : new List<int>();

            q.metric = def.Metric;
            q.rarityTarget = def.RarityTarget;
            q.target = def.Target;

            int gold = RemoteBalance.ScaleReward(def.GoldReward, RemoteBalance.Reward.missionGold);

            q.currencyReward = new CurrencyReward(CurrencyType.Coins, gold);
            q.reward = new QuestReward
            {
                rewardType = "coins",
                amount = gold,
                description = $"{gold} gold"
            };
            q.state = Quest.QuestState.Locked;

            AddQuest(q);
        }

        Debug.Log($"[Mission] Loaded {MissionCatalog.All.Count} main missions.");
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime >= 0.1f)
        {
            UpdateQuestSystem();
            lastUpdateTime = Time.time;
        }
    }
    
    private float lastUpdateTime = 0f;
    
    private void InitializeQuests()
    {
        foreach (var quest in allQuests)
        {
            quest.state = Quest.QuestState.Locked;
            
            if (questUIManager != null)
            {
                questUIManager.AddQuest(quest);
            }
        }
    }
    
    private void UpdateQuestSystem()
    {
        CheckQuestUnlock();
        
        UpdateActiveQuests();
    }
    
    private void CheckQuestUnlock()
    {
        foreach (var quest in allQuests)
        {
            if (quest.state == Quest.QuestState.Locked && CanUnlockQuest(quest))
            {
                quest.state = Quest.QuestState.Available;
                Debug.Log($"Quest '{quest.questName}' unlocked!");

                string unlockType = quest is TimeQuest ? "time"
                    : quest is BreedingQuest ? "breeding"
                    : quest is BattleQuest ? "battle"
                    : quest is CollectQuest ? "collect"
                    : quest is TowerQuest ? "tower"
                    : "unknown";
                FirebaseAnalyticsManager.LogQuestUnlock(quest.questID, quest.questName, unlockType);

                if (questUIManager != null)
                {
                    questUIManager.UpdateQuestState(quest);
                }
            }
        }
    }
    
    private bool CanUnlockQuest(Quest quest)
    {
        if (breedingManager != null && breedingManager.GetAllSlimes().Count < quest.slimeRequirement)
        {
            return false;
        }
        
        foreach (var requiredQuestID in quest.questreq)
        {
            var requiredQuest = allQuests.Find(q => q.questID == requiredQuestID);
            if (requiredQuest == null || requiredQuest.state != Quest.QuestState.Rewarded)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private void UpdateActiveQuests()
    {
        foreach (var quest in allQuests)
        {
            if (quest.state == Quest.QuestState.Available)
            {
                quest.StartQuest();
                if (questUIManager != null)
                {
                    questUIManager.UpdateQuestState(quest);
                }
            }
            
            if (quest.state == Quest.QuestState.InProgress)
            {
                UpdateQuestProgress(quest);
            }
        }
    }
    
    private void UpdateQuestProgress(Quest quest)
    {
        bool questCompleted = false;
        
        if (quest is TimeQuest timeQuest)
        {
            timeQuest.RegisterTime();
            questCompleted = timeQuest.CheckCompletion();
        }
        else if (quest is BreedingQuest breedingQuest)
        {
            if (breedingManager != null)
            {
                int actualSlimeCount = breedingManager.GetAllSlimes().Count;
                if (actualSlimeCount != breedingQuest.curSlime)
                    breedingQuest.curSlime = actualSlimeCount;
            }
            questCompleted = breedingQuest.CheckCompletion();
        }
        else if (quest is BattleQuest battleQuest)
        {
            questCompleted = battleQuest.CheckCompletion();
        }
        else if (quest is CollectQuest collectQuest)
        {
            questCompleted = collectQuest.CheckCompletion();
        }
        else if (quest is TowerQuest towerQuest)
        {
            if (towerQuest.towerDatabase == null && towerDatabase != null)
                towerQuest.towerDatabase = towerDatabase;
            questCompleted = towerQuest.CheckCompletion();
        }
        else
        {
            questCompleted = quest.CheckCompletion();
        }

        if (questUIManager != null)
        {
            questUIManager.UpdateQuestState(quest);
        }
        
        if (questCompleted && quest.state == Quest.QuestState.InProgress)
        {
            quest.CompleteQuest();
            Debug.Log($"Quest '{quest.questName}' completed!");

            string completeType = quest is TimeQuest ? "time"
                    : quest is BreedingQuest ? "breeding"
                    : quest is BattleQuest ? "battle"
                    : quest is CollectQuest ? "collect"
                    : quest is TowerQuest ? "tower"
                    : "unknown";
            FirebaseAnalyticsManager.LogQuestComplete(quest.questID, quest.questName, completeType);

            if (questUIManager != null)
            {
                questUIManager.UpdateQuestState(quest);
            }
        }
    }
    
    public void RegisterBattleWin(BattleMode mode)
    {
        foreach (var quest in allQuests)
        {
            if (quest is BattleQuest bq && bq.state == Quest.QuestState.InProgress)
                bq.RegisterBattleWin(mode);
        }
    }

    public void AddQuest(Quest quest)
    {
        if (!allQuests.Contains(quest))
        {
            allQuests.Add(quest);
            quest.state = Quest.QuestState.Locked;
            
            if (questUIManager != null)
            {
                questUIManager.AddQuest(quest);
            }
        }
    }
    
    public void RemoveQuest(Quest quest)
    {
        if (allQuests.Contains(quest))
        {
            allQuests.Remove(quest);
            
            if (questUIManager != null)
            {
                questUIManager.RemoveQuest(quest.questID);
            }
        }
    }
    
    public Quest GetQuest(int questID)
    {
        return allQuests.Find(q => q.questID == questID);
    }
    
    public List<Quest> GetQuestsByState(Quest.QuestState state)
    {
        return allQuests.FindAll(q => q.state == state);
    }
    
    public void RefreshQuestUI()
    {
        if (questUIManager != null)
        {
            questUIManager.RefreshAllQuests();
        }
    }
}

