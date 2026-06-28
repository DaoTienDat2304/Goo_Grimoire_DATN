// QuestTestSpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class QuestTestSpawner : MonoBehaviour
{
    public List<Quest> quests; // kéo vào vài Quest asset (TimeQuest hoặc Quest)
    public BreedingManager breedingManager; // tham chiếu đến BreedingManager để kiểm tra slime
    public QuestUIManager questUIManager; // tham chiếu đến QuestUIManager để cập nhật UI

    public bool ConditonMet(Quest quest)
    {
        if (breedingManager == null)
        {
            return false;
        }
        if (quest.state == Quest.QuestState.Locked && breedingManager.GetAllSlimes().Count >= quest.slimeRequirement)
        {
            for (int i = 0; i < quest.questreq.Count; i++)
            {
                if (quests[quest.questreq[i]].state != Quest.QuestState.Rewarded)
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }
    public void UnlockQuest()
    {
        foreach (var quest in quests)
        {
            if (quest.state == Quest.QuestState.Locked && breedingManager.GetAllSlimes().Count >= quest.slimeRequirement && ConditonMet(quest))
            {
                quest.state = Quest.QuestState.Available;
            }
        }
    }

    void Start()
    {
        foreach (var quest in quests)
        {
            quest.state = Quest.QuestState.Locked;
            // Thêm quest vào UI
            if (questUIManager != null)
            {
                questUIManager.AddQuest(quest);
            }
        }
    }

    void Update()
    {
        UnlockQuest();
        
        foreach (var quest in quests)
        {
            if (quest.state == Quest.QuestState.Available) 
            {
                quest.StartQuest();
                // Cập nhật UI khi quest bắt đầu
                if (questUIManager != null)
                {
                    questUIManager.UpdateQuestState(quest);
                }
            }
        }
        
        // Xử lý TimeQuest - chỉ khi đang InProgress
        foreach (var quest in quests)
        {
            if (quest is TimeQuest timeQuest && timeQuest.state == Quest.QuestState.InProgress)
            {
                timeQuest.RegisterTime();
                // Cập nhật UI mỗi frame cho TimeQuest để hiển thị progress
                if (questUIManager != null)
                {
                    questUIManager.UpdateQuestState(quest);
                }
            }
        }
        
        // Xử lý BreedingQuest - chỉ khi đang InProgress
        foreach (var quest in quests)
        {
            if (quest is BreedingQuest breedingQuest && breedingQuest.state == Quest.QuestState.InProgress)
            {
                // Chỉ update khi số slime thực tế thay đổi
                int actualSlimeCount = breedingManager.GetAllSlimes().Count;
                if (actualSlimeCount != breedingQuest.curSlime)
                {
                    breedingQuest.curSlime = actualSlimeCount;
                    // Cập nhật UI khi progress thay đổi
                    if (questUIManager != null)
                    {
                        questUIManager.UpdateQuestState(quest);
                    }
                }
                
                // Kiểm tra completion
                if (breedingQuest.CheckCompletion())
                {
                    breedingQuest.CompleteQuest();
                    // Cập nhật UI khi quest hoàn thành
                    if (questUIManager != null)
                    {
                        questUIManager.UpdateQuestState(quest);
                    }
                }
            }
        }
    }
}
