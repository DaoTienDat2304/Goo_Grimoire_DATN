// QuestTestSpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class QuestTestSpawner : MonoBehaviour
{
    public List<Quest> quests;
    public BreedingManager breedingManager;
    public QuestUIManager questUIManager;

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

    [Tooltip("Replaced by MissionCatalog through QuestManager. Keep off to avoid duplicate quests.")]
    public bool disabled = true;

    void Start()
    {
        if (disabled) return;
        foreach (var quest in quests)
        {
            quest.state = Quest.QuestState.Locked;
            if (questUIManager != null)
            {
                questUIManager.AddQuest(quest);
            }
        }
    }

    void Update()
    {
        if (disabled) return;
        UnlockQuest();
        
        foreach (var quest in quests)
        {
            if (quest.state == Quest.QuestState.Available) 
            {
                quest.StartQuest();
                if (questUIManager != null)
                {
                    questUIManager.UpdateQuestState(quest);
                }
            }
        }
        
        foreach (var quest in quests)
        {
            if (quest is TimeQuest timeQuest && timeQuest.state == Quest.QuestState.InProgress)
            {
                timeQuest.RegisterTime();
                if (questUIManager != null)
                {
                    questUIManager.UpdateQuestState(quest);
                }
            }
        }
        
        foreach (var quest in quests)
        {
            if (quest is BreedingQuest breedingQuest && breedingQuest.state == Quest.QuestState.InProgress)
            {
                int actualSlimeCount = breedingManager.GetAllSlimes().Count;
                if (actualSlimeCount != breedingQuest.curSlime)
                {
                    breedingQuest.curSlime = actualSlimeCount;
                    if (questUIManager != null)
                    {
                        questUIManager.UpdateQuestState(quest);
                    }
                }
                
                if (breedingQuest.CheckCompletion())
                {
                    breedingQuest.CompleteQuest();
                    if (questUIManager != null)
                    {
                        questUIManager.UpdateQuestState(quest);
                    }
                }
            }
        }
    }
}
