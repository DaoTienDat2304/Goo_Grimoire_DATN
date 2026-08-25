using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

/// <summary>
/// Quan ly mua hang bang tien that qua Google Play (Unity IAP 5.x).
/// Tu tao khi game chay de ket noi store som - don treo tu lan choi truoc se duoc
/// phat thuong lai ngay ma khong can mo Shop.
/// </summary>
public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance { get; private set; }

    /// <summary>Ban ra khi da lay duoc gia that tu store, de UI ve lai o gia.</summary>
    public static event Action OnProductsFetched;

    // productId -> du lieu item. Do ShopItemsSpawner dang ky luc mo shop.
    private static readonly Dictionary<string, ShopItems.ShopItemData> catalog =
        new Dictionary<string, ShopItems.ShopItemData>();

    // Don ve truoc khi catalog kip dang ky (vd don treo luc khoi dong) - giu lai phat sau.
    private static readonly List<string> deferredGrants = new List<string>();

    private StoreController storeController;
    private bool connected;
    private bool productsFetched;
    private Action<bool, string> activeCallback;

    /// <summary>Da lay xong danh sach san pham va gia tu store.</summary>
    public bool ProductsReady => productsFetched;

    /// <summary>Dang co mot giao dich chay do.</summary>
    public bool PurchaseInProgress => activeCallback != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject(nameof(IAPManager));
        go.AddComponent<IAPManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        storeController = UnityIAPServices.StoreController();
        storeController.OnPurchasePending += HandlePurchasePending;
        storeController.OnPurchaseConfirmed += HandlePurchaseConfirmed;
        storeController.OnPurchaseFailed += HandlePurchaseFailed;
        storeController.OnProductsFetched += HandleProductsFetched;
        storeController.OnProductsFetchFailed += HandleProductsFetchFailed;

        try
        {
            await storeController.Connect();
            connected = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[IAP] Khong ket noi duoc cua hang: {e.Message}");
            return;
        }

        FetchRegisteredProducts();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (storeController != null)
        {
            storeController.OnPurchasePending -= HandlePurchasePending;
            storeController.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
            storeController.OnPurchaseFailed -= HandlePurchaseFailed;
            storeController.OnProductsFetched -= HandleProductsFetched;
            storeController.OnProductsFetchFailed -= HandleProductsFetchFailed;
        }

        Instance = null;
    }

    /// <summary>
    /// Shop dang ky cac item IAP co trong database. Goi lai nhieu lan cung an toan.
    /// </summary>
    public static void RegisterCatalog(ShopItems database)
    {
        if (database == null) return;

        bool added = false;
        foreach (var item in database.items)
        {
            if (item == null || !item.isIAP) continue;

            string productId = item.ResolveIapProductId();
            if (string.IsNullOrWhiteSpace(productId)) continue;

            catalog[productId] = item;
            added = true;
        }

        if (!added || Instance == null) return;

        Instance.FetchRegisteredProducts();
        Instance.FlushDeferredGrants();
    }

    /// <summary>Gia that lay tu store (vd "119.000 d"). Chua lay duoc thi tra false.</summary>
    public bool TryGetLocalizedPrice(string productId, out string price)
    {
        price = null;
        if (!productsFetched || storeController == null || string.IsNullOrWhiteSpace(productId))
            return false;

        var product = storeController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
        if (product?.metadata == null) return false;

        price = product.metadata.localizedPriceString;
        return !string.IsNullOrWhiteSpace(price);
    }

    /// <summary>
    /// Bat dau mua. <paramref name="onComplete"/> nhan (thanh cong, ly do that bai).
    /// Thuong duoc phat o OnPurchasePending truoc khi callback nay chay.
    /// </summary>
    public void Purchase(string productId, Action<bool, string> onComplete)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            onComplete?.Invoke(false, "Item nay chua khai product ID.");
            return;
        }

        if (!connected)
        {
            onComplete?.Invoke(false, "Chua ket noi duoc Google Play.");
            return;
        }

        if (PurchaseInProgress)
        {
            onComplete?.Invoke(false, "Dang co giao dich khac chua xong.");
            return;
        }

        var product = storeController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
        if (product == null)
        {
            onComplete?.Invoke(false, $"Cua hang khong co san pham '{productId}'.");
            return;
        }

        activeCallback = onComplete;
        storeController.PurchaseProduct(product);
    }

    private void FetchRegisteredProducts()
    {
        if (!connected || catalog.Count == 0) return;

        // Gem pack la consumable: mua lai duoc nhieu lan.
        var definitions = catalog.Keys
            .Select(id => new ProductDefinition(id, ProductType.Consumable))
            .ToList();

        storeController.FetchProducts(definitions);
    }

    private void HandleProductsFetched(List<Product> products)
    {
        productsFetched = true;
        OnProductsFetched?.Invoke();
    }

    private void HandleProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning($"[IAP] Lay danh sach san pham that bai: {failure?.FailureReason}");
    }

    private void HandlePurchasePending(PendingOrder order)
    {
        // Phat thuong TRUOC khi confirm: neu game tat giua chung, don van con treo
        // va se duoc phat lai o lan chay sau thay vi mat trang.
        GrantProduct(GetProductId(order));
        storeController.ConfirmPurchase(order);
    }

    private void HandlePurchaseConfirmed(Order order)
    {
        if (order is FailedOrder failedOrder)
        {
            CompleteActivePurchase(false, $"Xac nhan giao dich that bai: {failedOrder.FailureReason}");
            return;
        }

        CompleteActivePurchase(true, null);
    }

    private void HandlePurchaseFailed(FailedOrder failedOrder)
    {
        CompleteActivePurchase(false, failedOrder != null
            ? failedOrder.FailureReason.ToString()
            : "Khong ro ly do.");
    }

    private void GrantProduct(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId)) return;

        if (!catalog.TryGetValue(productId, out var itemData))
        {
            deferredGrants.Add(productId);
            return;
        }

        ShopRewardGranter.Grant(itemData, "IAP", 0);
    }

    private void FlushDeferredGrants()
    {
        if (deferredGrants.Count == 0) return;

        // Clear truoc roi moi phat lai: id nao van chua co trong catalog se tu quay lai hang doi.
        var pending = deferredGrants.ToArray();
        deferredGrants.Clear();
        foreach (var productId in pending)
            GrantProduct(productId);
    }

    private void CompleteActivePurchase(bool success, string error)
    {
        var callback = activeCallback;
        activeCallback = null;
        callback?.Invoke(success, error);
    }

    private static string GetProductId(Order order)
    {
        return order?.CartOrdered?.Items()?.FirstOrDefault()?.Product?.definition?.id;
    }
}
