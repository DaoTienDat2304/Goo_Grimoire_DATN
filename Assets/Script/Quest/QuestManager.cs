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
        InitializeQuests();
    }
    
    void Update()
    {
        // Chỉ update quest system mỗi 0.1 giây để tránh performance issue
        if (Time.time - lastUpdateTime >= 0.1f)
        {
            UpdateQuestSystem();
            lastUpdateTime = Time.time;
        }
    }
    
    private float lastUpdateTime = 0f;
    
    private void InitializeQuests()
    {
        // Khởi tạo tất cả quest về trạng thái Locked
        foreach (var quest in allQuests)
        {
            quest.state = Quest.QuestState.Locked;
            
            // Thêm quest vào UI
            if (questUIManager != null)
            {
                questUIManager.AddQuest(quest);
            }
        }
    }
    
    private void UpdateQuestSystem()
    {
        // Kiểm tra và unlock quest
        CheckQuestUnlock();
        
        // Cập nhật quest đang active
        UpdateActiveQuests();
    }
    
    private void CheckQuestUnlock()
    {
        foreach (var quest in allQuests)
        {
            if (quest.state == Quest.QuestState.Locked && CanUnlockQuest(quest))
            {
                quest.state = Quest.QuestState.Available;
                Debug.Log($"Quest '{quest.questName}' đã được unlock!");

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
        // Kiểm tra slime requirement
        if (breedingManager != null && breedingManager.GetAllSlimes().Count < quest.slimeRequirement)
        {
            return false;
        }
        
        // Kiểm tra quest requirements
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
            // Tự động start quest khi available
            if (quest.state == Quest.QuestState.Available)
            {
                quest.StartQuest();
                if (questUIManager != null)
                {
                    questUIManager.UpdateQuestState(quest);
                }
            }
            
            // Cập nhật quest đang in progress
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
        
        // Cập nhật UI
        if (questUIManager != null)
        {
            questUIManager.UpdateQuestState(quest);
        }
        
        // Hoàn thành quest nếu đạt điều kiện
        if (questCompleted && quest.state == Quest.QuestState.InProgress)
        {
            quest.CompleteQuest();
            Debug.Log($"Quest '{quest.questName}' đã hoàn thành!");

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
    
    // Gọi khi player thắng một trận chiến
    public void RegisterBattleWin(BattleMode mode)
    {
        foreach (var quest in allQuests)
        {
            if (quest is BattleQuest bq && bq.state == Quest.QuestState.InProgress)
                bq.RegisterBattleWin(mode);
        }
    }

    // Public methods để các system khác có thể gọi
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

