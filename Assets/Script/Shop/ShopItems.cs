using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopItems", menuName = "GooGrimoire/Shop/Shop Items Database")]
public class ShopItems : ScriptableObject
{
    [System.Serializable]
    public class ShopItemData
    {
        [Header("Basic Info")]
        public string itemId;
        public string itemName;
        [TextArea]
        public string description;

        [Header("Visual")]
        public Sprite icon;

        [Header("Price")]
        public int price;
        public CurrencyType currencyType;
        public string priceLabelOverride;
        public bool isGemPack;

        [Header("Item")]
        public ResourceType resourceGranted;
        public int resourceAmount;
        public bool grantCurrency;
        public CurrencyType currencyGranted;
    }

    [Header("Shop items")]
    public List<ShopItemData> items = new List<ShopItemData>();
}










