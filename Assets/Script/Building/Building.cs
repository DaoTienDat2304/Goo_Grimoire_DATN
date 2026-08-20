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
    public CurrencyReward currencyCosts;
    public string description;
    public int slimeRequirement;
    public bool buildable = false;

    /// <summary>
    /// </summary>
    public bool CanAfford()
    {
        if (BuildingManager.FreeBuildMode) return true;
        if (CurrencyManager.Instance == null) return false;
        if (currencyCosts == null) return true;

        return CurrencyManager.Instance.HasEnoughCurrency(currencyCosts.rewards);
    }

    /// <summary>
    /// </summary>
    public bool Purchase()
    {
        if (BuildingManager.FreeBuildMode) return true;
        if (!CanAfford()) return false;
        if (currencyCosts == null) return true;

        return CurrencyManager.Instance.SpendCurrency(currencyCosts.rewards);
    }

    /// <summary>
    /// </summary>
    public string GetCostDescription()
    {
        if (currencyCosts == null) return "Free";
        return currencyCosts.GetRewardDescription();
    }
}
