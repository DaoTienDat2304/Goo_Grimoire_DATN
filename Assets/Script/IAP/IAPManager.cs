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
    private static readonly List<PendingGrant> deferredGrants = new List<PendingGrant>();

    // Google gui lai don PENDING o moi lan mo game cho den khi ConfirmPurchase thanh cong.
    // Neu khong nho lai don nao da phat thuong thi nguoi choi se nhan gem nhieu lan cho
    // mot lan tra tien. Luu bang PlayerPrefs de doc lap voi save game.
    private const string ProcessedOrdersKey = "IAP_ProcessedOrders";
    private const int MaxRememberedOrders = 64;
    private static readonly List<string> processedOrders = new List<string>();
    private static bool processedOrdersLoaded;

    /// <summary>Don cho phat thuong: giu ca transaction id de con chan trung.</summary>
    private readonly struct PendingGrant
    {
        public readonly string ProductId;
        public readonly string TransactionId;

        public PendingGrant(string productId, string transactionId)
        {
            ProductId = productId;
            TransactionId = transactionId;
        }
    }

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
        // Doi lai, don treo se ve lai moi lan mo game -> chan trung bang TransactionID.
        GrantProduct(GetProductId(order), GetTransactionId(order));
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

    private void GrantProduct(string productId, string transactionId)
    {
        if (string.IsNullOrWhiteSpace(productId)) return;

        if (IsOrderProcessed(transactionId))
        {
            Debug.Log($"[IAP] Bo qua don '{transactionId}' vi da phat thuong tu truoc.");
            return;
        }

        if (string.IsNullOrWhiteSpace(transactionId) && !Application.isEditor)
            Debug.LogWarning($"[IAP] Don '{productId}' khong co TransactionID - khong chan duoc phat trung.");

        if (!catalog.TryGetValue(productId, out var itemData))
        {
            deferredGrants.Add(new PendingGrant(productId, transactionId));
            return;
        }

        ShopRewardGranter.Grant(itemData, "IAP", 0);

        // Chi danh dau SAU khi thuong da vao tay nguoi choi.
        MarkOrderProcessed(transactionId);
    }

    private void FlushDeferredGrants()
    {
        if (deferredGrants.Count == 0) return;

        // Clear truoc roi moi phat lai: id nao van chua co trong catalog se tu quay lai hang doi.
        var pending = deferredGrants.ToArray();
        deferredGrants.Clear();
        foreach (var grant in pending)
            GrantProduct(grant.ProductId, grant.TransactionId);
    }

    private static void LoadProcessedOrders()
    {
        if (processedOrdersLoaded) return;
        processedOrdersLoaded = true;

        string raw = PlayerPrefs.GetString(ProcessedOrdersKey, string.Empty);
        if (string.IsNullOrEmpty(raw)) return;

        foreach (string id in raw.Split('|'))
            if (!string.IsNullOrEmpty(id))
                processedOrders.Add(id);
    }

    private static bool IsOrderProcessed(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId)) return false;

        LoadProcessedOrders();
        return processedOrders.Contains(transactionId);
    }

    private static void MarkOrderProcessed(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId)) return;

        LoadProcessedOrders();
        if (processedOrders.Contains(transactionId)) return;

        processedOrders.Add(transactionId);

        // Don da confirm thi khong bao gio quay lai nua, giu vai chuc id gan nhat la du.
        while (processedOrders.Count > MaxRememberedOrders)
            processedOrders.RemoveAt(0);

        PlayerPrefs.SetString(ProcessedOrdersKey, string.Join("|", processedOrders));
        PlayerPrefs.Save();
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

    /// <summary>
    /// Id dinh danh don hang. Voi consumable, Unity IAP chi tra ve gia tri nay khi don
    /// con o trang thai PendingOrder - dung luc chung ta can no de chan phat trung.
    /// </summary>
    private static string GetTransactionId(Order order)
    {
        return order?.Info?.TransactionID;
    }
}
