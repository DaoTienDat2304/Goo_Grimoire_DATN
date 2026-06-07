using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopItems", menuName = "GooGrimoire/Shop/Shop Items Database")]
public class ShopItems : ScriptableObject
{
    [System.Serializable]
    public class ShopItemData
    {
        [Header("Basic Info")]
        public string itemId;              // ID duy nhất (tùy chọn, dùng cho logic mua/bán)
        public string itemName;            // Tên item hiển thị
        [TextArea]
        public string description;         // Mô tả item

        [Header("Visual")]
        public Sprite icon;                // Ảnh icon item

        [Header("Price")]
        public int price;                  // Giá
        public CurrencyType currencyType;  // Đơn vị tiền (Coins / Gems, ... dùng lại CurrencyType có sẵn)

        [Header("Item")]
        public ResourceType resourceGranted; // Loại tài nguyên được nhận khi mua (nếu là item tài nguyên)
        public int resourceAmount;         // Số lượng tài nguyên được nhận khi mua
    }

    [Header("Danh sách item trong shop")]
    public List<ShopItemData> items = new List<ShopItemData>();
}










