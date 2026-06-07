using UnityEngine;

[System.Serializable]
public enum CurrencyType
{
    Coins,      // Tiền xu - kiếm được từ quest, bán slime, hoạt động hàng ngày
    Gems        // Đá quý - tiền tệ premium, kiếm được từ achievement, quest đặc biệt
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
