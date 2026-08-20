using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Archievement
{
    public string Name;
    public string Description;
    public bool Unlock;
    public GameObject ArchievementRef;

    private int curProgression;
    private int MaxProgression;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Archievement(string name,string description,GameObject Archievement,int MaxValue)
    {
        this.Name = name;
        this.Description = description;
        this.Unlock = false;
        this.ArchievementRef = Archievement;
        this.MaxProgression = MaxValue;
        LoadArchievement();
    }

    public bool EarnArchievement()
    {
        if (!Unlock && checkvalue())
        {
            saveArchievement(true);
            ArchievementRef.GetComponent<Image>().color = Color.yellow;
            ArchievementRef.transform.GetChild(2).GetComponentInChildren<Image>().color = Color.white;
            Unlock = true;
            return true;
        }
        return false;
    }
    
    public void GiveCurrencyReward(AchievementCurrencyReward currencyReward)
    {
        if (currencyReward != null && currencyReward.giveRewardOnUnlock)
        {
            currencyReward.GiveReward();
        }
    }

    public void saveArchievement(bool value)
    {
        Unlock = value;
        PlayerPrefs.SetInt(Name, value ? 1 : 0);
        PlayerPrefs.Save();


    }

    public void LoadArchievement()
    {
        Unlock = PlayerPrefs.GetInt(Name) == 1 ? true:false;
        if (Unlock)
        {
            ArchievementRef.GetComponent<Image>().color = Color.yellow;
            ArchievementRef.transform.GetChild(2).GetComponentInChildren<Image>().color = Color.white;
        }
    }

    public bool checkvalue()
    {
        curProgression++;
        if(MaxProgression == 0)
        {
            return true;
        }
        if (curProgression >= MaxProgression)
        {
            return true;
        }

        return false ;
    }
}
