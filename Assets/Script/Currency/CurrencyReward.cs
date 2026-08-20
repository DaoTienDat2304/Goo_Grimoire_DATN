using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CurrencyReward
{
    public List<CurrencyData> rewards = new List<CurrencyData>();

    public CurrencyReward()
    {
        rewards = new List<CurrencyData>();
    }

    public CurrencyReward(CurrencyType type, int amount)
    {
        rewards = new List<CurrencyData> { new CurrencyData(type, amount) };
    }

    public void AddReward(CurrencyType type, int amount)
    {
        rewards.Add(new CurrencyData(type, amount));
    }

    public void GiveRewards()
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("CurrencyManager instance not found!");
            return;
        }

        foreach (var reward in rewards)
        {
            CurrencyManager.Instance.AddCurrency(reward.type, reward.amount);
        }
    }

    public string GetRewardDescription()
    {
        if (rewards.Count == 0) return "No reward";

        List<string> descriptions = new List<string>();
        foreach (var reward in rewards)
        {
            string currencyName = reward.type == CurrencyType.Coins ? "Coins" : "Gems";
            descriptions.Add($"{reward.amount} {currencyName}");
        }

        return string.Join(", ", descriptions);
    }
}

