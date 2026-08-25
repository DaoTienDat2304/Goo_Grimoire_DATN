using System;
using System.Collections;
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

    /// <summary>
    /// Ban ra moi khi trang thai cua hang doi (noi duoc store, bat dau / ket thuc giao dich).
    /// UI dung tin hieu nay de khoa hoac mo lai nut mua.
    /// </summary>
    public static event Action OnStoreStateChanged;

    /// <summary>
    /// Ban ra khi mot giao dich ket thuc: (productId, thanh cong, ly do that bai).
    /// That bai ngay tu dau (chua noi duoc store, sai product ID...) cung ban qua day.
    /// </summary>
    public static event Action<string, bool, string> OnPurchaseFinished;

    /// <summary>
    /// Bat = ve dong trang thai IAP len man hinh. Shop tu bat luc mo, tat luc dong,
    /// de con biet vi sao khong mua duoc khi chay tren may that (khong co console).
    /// </summary>
    public static bool ShowStatusOverlay;

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

    // Google Play co the chua san sang ngay luc mo game (dang cap nhat, chua dang nhap...).
    private const int MaxConnectAttempts = 4;
    private const float ConnectRetryDelaySeconds = 6f;
    private const int MaxFetchAttempts = 4;
    private const float FetchRetryDelaySeconds = 6f;

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
    private bool connecting;
    private bool productsFetched;
    private bool fetchInFlight;
    private int connectAttempts;
    private int fetchAttempts;
    private Action<bool, string> activeCallback;
    private string activeProductId;
    private string statusMessage = "IAP: chua khoi tao.";
    private GUIStyle overlayStyle;

    /// <summary>Da lay xong danh sach san pham va gia tu store.</summary>
    public bool ProductsReady => productsFetched;

    /// <summary>Da noi duoc voi Google Play chua. Chua noi duoc thi khong mua duoc.</summary>
    public bool StoreConnected => connected;

    /// <summary>Dang co mot giao dich chay do.</summary>
    public bool PurchaseInProgress => activeCallback != null;

    /// <summary>Mo ta ngan gon tinh trang IAP hien tai - dung de chan doan tren may that.</summary>
    public string StatusMessage => statusMessage;

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

    private void Start()
    {
        storeController = UnityIAPServices.StoreController();

        // Phai dang ky het su kien TRUOC khi Connect: don treo tu lan choi truoc
        // co the ban ve ngay trong luc ket noi.
        storeController.OnStoreConnected += HandleStoreConnected;
        storeController.OnStoreDisconnected += HandleStoreDisconnected;
        storeController.OnPurchasePending += HandlePurchasePending;
        storeController.OnPurchaseConfirmed += HandlePurchaseConfirmed;
        storeController.OnPurchaseFailed += HandlePurchaseFailed;
        storeController.OnPurchaseDeferred += HandlePurchaseDeferred;
        storeController.OnProductsFetched += HandleProductsFetched;
        storeController.OnProductsFetchFailed += HandleProductsFetchFailed;

        ConnectToStore();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (storeController != null)
        {
            storeController.OnStoreConnected -= HandleStoreConnected;
            storeController.OnStoreDisconnected -= HandleStoreDisconnected;
            storeController.OnPurchasePending -= HandlePurchasePending;
            storeController.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
            storeController.OnPurchaseFailed -= HandlePurchaseFailed;
            storeController.OnPurchaseDeferred -= HandlePurchaseDeferred;
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
            ReportPurchaseFailed(productId, onComplete, "Item nay chua khai product ID.");
            return;
        }

        if (PurchaseInProgress)
        {
            ReportPurchaseFailed(productId, onComplete, "Dang co giao dich khac chua xong.");
            return;
        }

        if (!connected || storeController == null)
        {
            // Thu noi lai ngay: nhieu khi Google Play chi ban luc mo game.
            ConnectToStore();
            ReportPurchaseFailed(productId, onComplete,
                "Chua ket noi duoc Google Play. Dang thu ket noi lai, doi vai giay roi bam lai.");
            return;
        }

        var product = storeController.GetProducts().FirstOrDefault(p => p.definition.id == productId);
        if (product == null)
        {
            // Chua fetch duoc thi khong the mua - keo lai danh sach san pham.
            fetchAttempts = 0;
            FetchRegisteredProducts();
            ReportPurchaseFailed(productId, onComplete,
                $"Google Play khong tra ve san pham '{productId}'. Kiem tra product ID va trang thai Active tren Play Console.");
            return;
        }

        activeCallback = onComplete;
        activeProductId = productId;

        SetStatus($"Dang mo giao dien thanh toan cho '{productId}'...");

        // Khoa nut ngay truoc khi mo giao dien Google Play, dung de bam them lan nua.
        OnStoreStateChanged?.Invoke();

        storeController.PurchaseProduct(product);
    }

    /// <summary>Buoc nguoi choi thu noi lai store (vd nut "Thu lai" trong shop).</summary>
    public void RetryConnection()
    {
        connectAttempts = 0;
        fetchAttempts = 0;
        ConnectToStore();
    }

    /// <summary>Giao dich hong truoc khi kip bat dau: bao ca callback lan UI.</summary>
    private void ReportPurchaseFailed(string productId, Action<bool, string> onComplete, string reason)
    {
        SetStatus($"Mua that bai: {reason}");
        Debug.LogWarning($"[IAP] Mua '{productId}' that bai: {reason}");

        onComplete?.Invoke(false, reason);
        OnPurchaseFinished?.Invoke(productId, false, reason);
    }

    private async void ConnectToStore()
    {
        if (storeController == null || connected || connecting) return;

        connecting = true;
        connectAttempts++;
        SetStatus($"Dang ket noi Google Play... (lan {connectAttempts})");

        try
        {
            // Luu y: Task nay hoan tat CA khi ket noi hong. Trang thai that chi den
            // tu OnStoreConnected / OnStoreDisconnected, khong duoc suy ra tu day.
            await storeController.Connect();
        }
        catch (Exception e)
        {
            SetStatus($"Loi khi ket noi Google Play: {e.Message}");
            Debug.LogWarning($"[IAP] Khong ket noi duoc cua hang: {e}");
        }
        finally
        {
            connecting = false;
        }
    }

    private void HandleStoreConnected()
    {
        connected = true;
        connecting = false;
        connectAttempts = 0;
        SetStatus("Da ket noi Google Play. Dang lay danh sach san pham...");

        // Bao UI mo khoa cac nut mua bang tien that.
        OnStoreStateChanged?.Invoke();

        fetchAttempts = 0;
        FetchRegisteredProducts();
    }

    private void HandleStoreDisconnected(StoreConnectionFailureDescription description)
    {
        connected = false;
        connecting = false;
        productsFetched = false;

        string reason = string.IsNullOrWhiteSpace(description?.message)
            ? "khong ro ly do"
            : description.message;
        SetStatus($"Mat ket noi Google Play: {reason}");
        Debug.LogWarning($"[IAP] Store disconnected: {reason}");

        OnStoreStateChanged?.Invoke();

        if (connectAttempts < MaxConnectAttempts)
            StartCoroutine(RetryConnectAfterDelay());
        else
            SetStatus($"Khong ket noi duoc Google Play sau {connectAttempts} lan: {reason}");
    }

    private IEnumerator RetryConnectAfterDelay()
    {
        yield return new WaitForSecondsRealtime(ConnectRetryDelaySeconds);
        ConnectToStore();
    }

    private void FetchRegisteredProducts()
    {
        if (!connected || storeController == null) return;

        if (catalog.Count == 0)
        {
            SetStatus("Chua co san pham IAP nao duoc dang ky (mo Shop de dang ky).");
            return;
        }

        if (fetchInFlight) return;

        fetchInFlight = true;
        fetchAttempts++;

        // Gem pack la consumable: mua lai duoc nhieu lan.
        var definitions = catalog.Keys
            .Select(id => new ProductDefinition(id, ProductType.Consumable))
            .ToList();

        SetStatus($"Dang lay {definitions.Count} san pham tu Google Play... (lan {fetchAttempts})");
        storeController.FetchProducts(definitions);
    }

    private void HandleProductsFetched(List<Product> products)
    {
        fetchInFlight = false;
        productsFetched = true;
        fetchAttempts = 0;

        SetStatus($"San sang. Lay duoc {products?.Count ?? 0} san pham tu Google Play.");
        OnProductsFetched?.Invoke();
        OnStoreStateChanged?.Invoke();
    }

    private void HandleProductsFetchFailed(ProductFetchFailed failure)
    {
        fetchInFlight = false;

        string missing = failure?.FailedFetchProducts == null
            ? string.Empty
            : string.Join(", ", failure.FailedFetchProducts.Select(p => p.id));
        SetStatus($"Google Play tu choi tra san pham ({failure?.FailureReason}). Thieu: {missing}");
        Debug.LogWarning($"[IAP] Lay danh sach san pham that bai: {failure?.FailureReason} - {missing}");

        OnStoreStateChanged?.Invoke();

        if (fetchAttempts < MaxFetchAttempts)
            StartCoroutine(RetryFetchAfterDelay());
    }

    private IEnumerator RetryFetchAfterDelay()
    {
        yield return new WaitForSecondsRealtime(FetchRetryDelaySeconds);
        FetchRegisteredProducts();
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
            ? $"{failedOrder.FailureReason} ({failedOrder.Details})"
            : "Khong ro ly do.");
    }

    /// <summary>
    /// Don cho duyet (Ask-to-Buy, tra sau qua cua hang tien loi). Chua duoc phat thuong -
    /// khi nao duyet xong thi don se quay lai o OnPurchasePending.
    /// </summary>
    private void HandlePurchaseDeferred(DeferredOrder order)
    {
        SetStatus("Giao dich dang cho duyet, chua tru tien. Se nhan thuong khi duoc duyet.");
        CompleteActivePurchase(false, "Giao dich dang cho duyet.");
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
        SetStatus($"Da phat thuong cho '{productId}'.");

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
        string productId = activeProductId;

        // Xoa truoc khi goi callback: PurchaseInProgress phai la false ngay luc UI ve lai.
        activeCallback = null;
        activeProductId = null;

        if (success)
            SetStatus($"Mua '{productId}' thanh cong.");
        else if (!string.IsNullOrEmpty(error))
            SetStatus($"Giao dich '{productId}' khong hoan tat: {error}");

        callback?.Invoke(success, error);

        OnPurchaseFinished?.Invoke(productId, success, error);
        OnStoreStateChanged?.Invoke();
    }

    private void SetStatus(string message)
    {
        statusMessage = $"IAP: {message}";
        Debug.Log($"[IAP] {message}");
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

    /// <summary>
    /// Tren may that khong co console, day la cach duy nhat thay duoc vi sao khong mua duoc.
    /// Chi ve khi Shop dang mo.
    /// </summary>
    private void OnGUI()
    {
        if (!ShowStatusOverlay) return;

        if (overlayStyle == null)
        {
            overlayStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(14, Screen.height / 45),
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            overlayStyle.normal.textColor = Color.yellow;
        }

        float width = Screen.width * 0.9f;
        var rect = new Rect(Screen.width * 0.05f, Screen.height * 0.02f, width, Screen.height * 0.2f);

        var previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.Box(rect, GUIContent.none);
        GUI.color = previous;

        GUI.Label(rect, statusMessage, overlayStyle);
    }
}
