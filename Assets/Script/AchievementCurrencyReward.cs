using UnityEngine;

[System.Serializable]
public class AchievementCurrencyReward
{
    [Header("Currency Rewards")]
    public CurrencyReward currencyReward;
    
    [Header("Settings")]
    public bool giveRewardOnUnlock = true;
    public bool showNotification = true;
    
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
        if (currencyReward == null) return "No reward";
        return currencyReward.GetRewardDescription();
    }
}
