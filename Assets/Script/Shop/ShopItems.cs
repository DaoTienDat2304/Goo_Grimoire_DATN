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

        [Header("Rewarded Ad")]
        [Tooltip("Bat = o nay khong ban bang tien, nguoi choi xem quang cao de nhan thuong.")]
        public bool isRewardedAd;
        [Tooltip("Chu tren nut khi la o quang cao. De trong = WatchADS.")]
        public string adButtonLabel;
        [Tooltip("Chu o o gia khi la o quang cao. De trong = FREE.")]
        public string adPriceLabel;
    }

    [Header("Shop items")]
    public List<ShopItemData> items = new List<ShopItemData>();
}










