using UnityEngine;

[System.Serializable]
public enum CurrencyType
{
    Coins,
    Gems
}

[System.Serializable]
public class CurrencyData
{
    public CurrencyType type;
    public int amount;

    public CurrencyData(CurrencyType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}
