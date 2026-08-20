using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class QuestReward
{
    public string rewardType; // "coins", "slime", "building", etc.
    public int amount;
    public string description;
}

public abstract class Quest : ScriptableObject
{
    [Header("Quest Info")]
    public int questID;
    public string questName;
    public string description;
    public int slimeRequirement;
    public List<int> questreq;
    public CurrencyReward currencyReward;
    
    [Header("Reward")]
    public QuestReward reward;
    public Sprite rewardIcon;

    public enum QuestState
    {
        Locked,
        Available,
        InProgress,
        Completed,
        Rewarded
    }
    public QuestState state = QuestState.Locked;
    
    public abstract bool CheckCompletion();
    
    public virtual void StartQuest()
    {
        state = QuestState.InProgress;
        Debug.Log("Start quest: " + questName);
    }

    public virtual void CompleteQuest()
    {
        state = QuestState.Completed;
        Debug.Log("Done quest: " + questName);
    }

    public virtual void ClaimReward()
    {
        if (state == QuestState.Completed)
        {
            state = QuestState.Rewarded;
            if (ArchievementManager.Instance != null)
            {
                ArchievementManager.Instance.GetArchivement(3); // 0 = Breed achievement
            }
            if (currencyReward != null)
            {
                currencyReward.GiveRewards();
                Debug.Log($"Claim quest: {questName} - {currencyReward.GetRewardDescription()}");
            }
            
            if (reward != null)
            {
                Debug.Log($"Get quest reward {questName}: {reward.amount} {reward.rewardType}");
                ApplyReward();
            }

            SaveAndLoadSystem.Instance?.Save();
        }
    }
    
    protected virtual void ApplyReward()
    {
        switch (reward.rewardType.ToLower())
        {
            case "coins":
                Debug.Log($"Get {reward.amount} coins");
                break;
            case "slime":
                Debug.Log($"Get {reward.amount} slime");
                break;
            default:
                Debug.Log($"Get reward: {reward.description}");
                break;
        }
    }
    
    public virtual string GetProgressText()
    {
        return "Progress: " + GetProgressPercentage() + "%";
    }
    
    public virtual float GetProgressPercentage()
    {
        return 0f;
    }
}


