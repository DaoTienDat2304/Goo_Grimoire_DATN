using UnityEngine;

[System.Serializable]
public class AchievementCurrencyReward
{
    [Header("Currency Rewards")]
    public CurrencyReward currencyReward;
    
    [Header("Settings")]
    public bool giveRewardOnUnlock = true; // Tự động thưởng khi unlock
    public bool showNotification = true; // Hiển thị thông báo
    
    public AchievementCurrencyReward()
    {
        currencyReward = new CurrencyReward();
    }
    
    public AchievementCurrencyReward(CurrencyType type, int amount)
    {
        currencyReward = new CurrencyReward(type, amount);
    }
    
    public void GiveReward()
    {
        if (currencyReward != null)
        {
            currencyReward.GiveRewards();
            
            if (showNotification)
            {
                Debug.Log($"Achievement Reward: {currencyReward.GetRewardDescription()}");
            }
        }
    }
    
    public string GetRewardDescription()
    {
        if (currencyReward == null) return "Không có phần thưởng";
        return currencyReward.GetRewardDescription();
    }
}
