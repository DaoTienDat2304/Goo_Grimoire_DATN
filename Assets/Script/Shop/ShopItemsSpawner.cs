using Unity.VisualScripting;
using UnityEngine;
public class ShopItemsSpawner : MonoBehaviour
{
    [Header("Database")]
    public ShopItems shopItemsDatabase;          // Kéo asset ShopItems vào đây
    public ShopItems summerShopItemsDatabase;          // Kéo asset ShopItems mùa hè vào đây (nếu có)

    [Header("Prefabs & Layout")]
    public GameObject shopItemPrefab;            // Prefab cửa hàng (đã gắn ShopItemUI)
    public Transform itemsParent;                // Vị trí/parent để chứa các item (ví dụ: Content trong ScrollView)
    public GameObject confirmPopUp;

    [Header("Chosen Item")]
    public int price;                  
    public CurrencyType currencyType;
    public ResourceType resourceGranted;
    public int resourceAmount;

    private void Start()
    {
        // Chọn database theo Remote Config (nếu có)
        var rc = RemoteConfigManager.Instance;
        if (rc != null && rc.ActiveShopId == "summer" && summerShopItemsDatabase != null)
        {
            shopItemsDatabase = summerShopItemsDatabase;
        }
        SpawnAllItems();
    }

    /// <summary>
    /// Xóa item cũ và spawn lại toàn bộ từ database.
    /// </summary>
    /// 
    public void Confirmed()
    {
        CurrencyManager.Instance.SpendCurrency(currencyType, price);
        ResourceManager.Instance.AddResource(resourceGranted, resourceAmount);
        FirebaseAnalyticsManager.LogShopPurchase(
            currencyType.ToString(),
            price,
            resourceGranted.ToString(),
            resourceAmount);

        SaveAndLoadSystem.Instance?.Save();
    }

    public void Cancel()
    {
        price = 0;
        currencyType = CurrencyType.Coins;
        resourceGranted = ResourceType.Marshmallow;
        resourceAmount = 0;
    }
    public void SpawnAllItems()
    {
        if (shopItemsDatabase == null)
        {
            Debug.LogWarning("ShopItemsSpawner: Chưa gán ShopItems database!");
            return;
        }

        if (shopItemPrefab == null)
        {
            Debug.LogWarning("ShopItemsSpawner: Chưa gán prefab shop item!");
            return;
        }

        if (itemsParent == null)
        {
            Debug.LogWarning("ShopItemsSpawner: Chưa gán itemsParent (parent để chứa các item)!");
            return;
        }

        // Xóa item cũ
        for (int i = itemsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsParent.GetChild(i).gameObject);
        }

        // Spawn item mới từ database
        foreach (var itemData in shopItemsDatabase.items)
        {
            if (itemData == null) continue;

            GameObject go = Instantiate(shopItemPrefab, itemsParent);

            // Tự động scale và position theo LayoutGroup/Content Size Fitter nếu có

            // Lấy component ShopItemUI để set dữ liệu
            var ui = go.GetComponent<ShopItemUI>();
            ui.shopItemsSpawner = this; // Gán tham chiếu ngược lại nếu cần
            if (ui != null)
            {
                ui.Setup(itemData);
            }
            else
            {
                // Nếu bạn muốn tự xử lý UI, có thể bỏ ShopItemUI khỏi prefab
                // và xử lý thủ công ở đây.
                Debug.LogWarning("Prefab shop item không có ShopItemUI, bạn tự xử lý UI cho item này.");
            }
        }
    }
}










