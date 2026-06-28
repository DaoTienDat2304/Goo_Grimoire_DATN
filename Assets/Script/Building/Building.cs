using UnityEngine;

[CreateAssetMenu(fileName = "Building", menuName = "Buildings/Building")]
public class Building : ScriptableObject
{ 
    public int buildingID;
    public string buildingName;
    public Sprite sprite;
    [System.Obsolete("Use currencyCosts instead")]
    public string cost;
    public float buildTime;
    public CurrencyReward currencyCosts;   // Chi phí xây dựng
    public string description;
    public int slimeRequirement;
    public bool buildable = false;

    /// <summary>
    /// Kiểm tra có đủ tiền để xây dựng không
    /// </summary>
    public bool CanAfford()
    {
        if (CurrencyManager.Instance == null) return false;
        if (currencyCosts == null) return true;
        
        return CurrencyManager.Instance.HasEnoughCurrency(currencyCosts.rewards);
    }

    /// <summary>
    /// Trừ tiền khi xây dựng
    /// </summary>
    public bool Purchase()
    {
        if (!CanAfford()) return false;
        if (currencyCosts == null) return true;
        
        return CurrencyManager.Instance.SpendCurrency(currencyCosts.rewards);
    }

    /// <summary>
    /// Lấy mô tả chi phí
    /// </summary>
    public string GetCostDescription()
    {
        if (currencyCosts == null) return "Miễn phí";
        return currencyCosts.GetRewardDescription();
    }
}
