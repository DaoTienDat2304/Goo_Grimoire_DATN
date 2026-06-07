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
    public List<int> questreq;   // Quest ID phải hoàn thành trước
    public CurrencyReward currencyReward;   // Phần thưởng tiền tệ
    
    [Header("Reward")]
    public QuestReward reward;
    public Sprite rewardIcon; // Icon phần thưởng

    public enum QuestState
    {
        Locked,
        Available,
        InProgress,
        Completed,
        Rewarded
    }
    public QuestState state = QuestState.Locked; // Trạng thái
    
    public abstract bool CheckCompletion();
    
    public virtual void StartQuest()
    {
        state = QuestState.InProgress;
        Debug.Log("Bắt đầu quest: " + questName);
    }

    public virtual void CompleteQuest()
    {
        state = QuestState.Completed;
        Debug.Log("Hoàn thành quest: " + questName);
        // Không tự động claim reward, để player tự claim
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
            // Trao phần thưởng tiền tệ
            if (currencyReward != null)
            {
                currencyReward.GiveRewards();
                Debug.Log($"Nhận thưởng cho quest: {questName} - {currencyReward.GetRewardDescription()}");
            }
            
            // Trao phần thưởng khác
            if (reward != null)
            {
                Debug.Log($"Nhận thưởng cho quest {questName}: {reward.amount} {reward.rewardType}");
                ApplyReward();
            }

            SaveAndLoadSystem.Instance?.Save();
        }
    }
    
    protected virtual void ApplyReward()
    {
        // Override trong subclass để áp dụng reward cụ thể
        switch (reward.rewardType.ToLower())
        {
            case "coins":
                // Thêm coins vào game
                Debug.Log($"Nhận {reward.amount} coins");
                break;
            case "slime":
                // Thêm slime vào game
                Debug.Log($"Nhận {reward.amount} slime");
                break;
            default:
                Debug.Log($"Nhận reward: {reward.description}");
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


