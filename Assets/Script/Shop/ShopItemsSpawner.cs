using Unity.VisualScripting;
using UnityEngine;
public class ShopItemsSpawner : MonoBehaviour
{
    [Header("Database")]
    public ShopItems shopItemsDatabase;
    public ShopItems summerShopItemsDatabase;

    [Header("Prefabs & Layout")]
    public GameObject shopItemPrefab;
    public Transform itemsParent;
    public GameObject confirmPopUp;

    [Header("Chosen Item")]
    public int price;                  
    public CurrencyType currencyType;
    public ResourceType resourceGranted;
    public int resourceAmount;

    private void Start()
    {
        var rc = RemoteConfigManager.Instance;
        if (rc != null && rc.ActiveShopId == "summer" && summerShopItemsDatabase != null)
        {
            shopItemsDatabase = summerShopItemsDatabase;
        }
        SpawnAllItems();
    }

    /// <summary>
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
            Debug.LogWarning("ShopItemsSpawner: Missing ShopItems database!");
            return;
        }

        if (shopItemPrefab == null)
        {
            Debug.LogWarning("ShopItemsSpawner: Missing prefab shop item!");
            return;
        }

        if (itemsParent == null)
        {
            Debug.LogWarning("ShopItemsSpawner: Missing itemsParent (item parent)!");
            return;
        }

        for (int i = itemsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsParent.GetChild(i).gameObject);
        }

        foreach (var itemData in shopItemsDatabase.items)
        {
            if (itemData == null) continue;

            GameObject go = Instantiate(shopItemPrefab, itemsParent);


            var ui = go.GetComponent<ShopItemUI>();
            ui.shopItemsSpawner = this;
            if (ui != null)
            {
                ui.Setup(itemData);
            }
            else
            {
                Debug.LogWarning("Prefab shop item no yes ShopItemUI, handle UI manually.");
            }
        }
    }
}










