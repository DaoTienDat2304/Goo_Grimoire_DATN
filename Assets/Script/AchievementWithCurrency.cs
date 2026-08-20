using UnityEngine;
using UnityEngine.UI;

public class AchievementWithCurrency
{
    public string Name;
    public string Description;
    public bool Unlock;
    public GameObject AchievementRef;
    public AchievementCurrencyReward currencyReward;

    private int curProgression;
    private int MaxProgression;

    public AchievementWithCurrency(string name, string description, GameObject achievement, int maxValue, AchievementCurrencyReward reward = null)
    {
        this.Name = name;
        this.Description = description;
        this.Unlock = false;
        this.AchievementRef = achievement;
        this.MaxProgression = maxValue;
        this.currencyReward = reward;
        LoadAchievement();
    }

    public bool EarnAchievement()
    {
        if (!Unlock && checkvalue())
        {
            saveAchievement(true);
            AchievementRef.GetComponent<Image>().color = Color.yellow;
            AchievementRef.transform.GetChild(2).GetComponentInChildren<Image>().color = Color.white;
            Unlock = true;
            
            if (currencyReward != null && currencyReward.giveRewardOnUnlock)
            {
                currencyReward.GiveReward();
            }
            
            return true;
        }
        return false;
    }

    public void saveAchievement(bool value)
    {
        Unlock = value;
        PlayerPrefs.SetInt(Name, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void LoadAchievement()
    {
        Unlock = PlayerPrefs.GetInt(Name) == 1 ? true : false;
        if (Unlock)
        {
            AchievementRef.GetComponent<Image>().color = Color.yellow;
            AchievementRef.transform.GetChild(2).GetComponentInChildren<Image>().color = Color.white;
        }
    }

    public bool checkvalue()
    {
        curProgression++;
        if (MaxProgression == 0)
        {
            return true;
        }
        if (curProgression >= MaxProgression)
        {
            return true;
        }

        return false;
    }
    
    public string GetRewardDescription()
    {
        if (currencyReward != null)
        {
            return currencyReward.GetRewardDescription();
        }
        return "No reward";
    }
}
