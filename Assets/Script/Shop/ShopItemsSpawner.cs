using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ShopItemsSpawner : MonoBehaviour
{
    [Header("Database")]
    public ShopItems shopItemsDatabase;
    public ShopItems summerShopItemsDatabase;

    [Header("Hierarchy References")]
    public Transform itemsParent;
    public Transform gemPacksParent;
    public GameObject confirmPopUp;

    [Header("Currency Bar")]
    [SerializeField] private TMP_Text coinsBalanceText;
    [SerializeField] private TMP_Text gemsBalanceText;

    [Header("Confirm Popup")]
    [Tooltip("Dong chu trong popup xac nhan. De trong = tu tim 'NoticeText' trong popup.")]
    [SerializeField] private TMP_Text confirmNoticeText;
    [Tooltip("{0} = phan thuong nhan duoc, {1} = gia phai tra.")]
    [SerializeField] private string confirmMessageFormat = "Buy {0} for {1}?";

    [Header("Chosen Item")]
    public int price;                  
    public CurrencyType currencyType;
    public ResourceType resourceGranted;
    public int resourceAmount;
    public bool grantCurrency;
    public CurrencyType currencyGranted;

    private ShopItems.ShopItemData selectedItem;
    private bool confirmButtonsWired;
    private bool adRequestInFlight;
    private readonly List<ShopItemUI> itemSlots = new List<ShopItemUI>();
    private readonly List<ShopItemUI> gemSlots = new List<ShopItemUI>();

    private void Awake()
    {
        AutoWire();
        WireConfirmButtons();
    }

    private void OnEnable()
    {
        CurrencyManager.OnCurrencyChanged -= HandleCurrencyChanged;
        CurrencyManager.OnCurrencyChanged += HandleCurrencyChanged;
        AutoWireCurrencyBar();
        RefreshCurrencyBar();
    }

    private void OnDisable()
    {
        CurrencyManager.OnCurrencyChanged -= HandleCurrencyChanged;
        RestoreSlimes();
    }

    private void Start()
    {
        AutoWire();
        WireConfirmButtons();
        var rc = RemoteConfigManager.Instance;
        if (rc != null && rc.ActiveShopId == "summer" && summerShopItemsDatabase != null)
        {
            shopItemsDatabase = summerShopItemsDatabase;
        }
        IAPManager.RegisterCatalog(shopItemsDatabase);
        SpawnAllItems();
        RefreshCurrencyBar();
    }
    public void SelectItem(ShopItems.ShopItemData itemData)
    {
        if (itemData == null) return;

        selectedItem = itemData;
        price = itemData.price;
        currencyType = itemData.currencyType;
        resourceGranted = itemData.resourceGranted;
        resourceAmount = itemData.resourceAmount;
        grantCurrency = itemData.grantCurrency;
        currencyGranted = itemData.currencyGranted;

        // Popup dung chung cho moi o, nen phai viet ro dang mua goi nao -
        // nguoi choi tra tien that thi khong duoc de ho doan.
        SetText(confirmNoticeText, BuildConfirmMessage(itemData));

        if (confirmPopUp != null)
            confirmPopUp.SetActive(true);
        else
            Debug.LogWarning($"{nameof(ShopItemsSpawner)} cannot open confirm popup because confirmPopUp is missing.", this);
    }

    /// <summary>
    /// Cau xac nhan cho dung goi vua bam. Voi o IAP thi uu tien gia noi te that
    /// lay tu store, vi do moi la so tien Google Play se tru cua nguoi choi.
    /// </summary>
    private string BuildConfirmMessage(ShopItems.ShopItemData itemData)
    {
        string reward = itemData.grantCurrency
            ? $"{itemData.resourceAmount} {itemData.currencyGranted}"
            : $"{itemData.resourceAmount} {itemData.resourceGranted}";

        string cost = null;

        if (itemData.isIAP)
        {
            var iap = IAPManager.Instance;
            if (iap != null)
                iap.TryGetLocalizedPrice(itemData.ResolveIapProductId(), out cost);

            // Chua noi duoc store thi tam dung nhan trong asset ($4.99...).
            if (string.IsNullOrWhiteSpace(cost))
                cost = itemData.priceLabelOverride;
        }
        else
        {
            cost = $"{itemData.price} {itemData.currencyType}";
        }

        if (string.IsNullOrWhiteSpace(cost))
            cost = "?";

        return string.Format(confirmMessageFormat, reward, cost);
    }

    public void Confirmed()
    {
        ShopItems.ShopItemData itemData = selectedItem;
        if (itemData == null)
        {
            Cancel();
            return;
        }

        // Goi tra bang tien that: khong tru tien trong game, day sang Google Play.
        if (itemData.isIAP)
        {
            StartIapPurchase(itemData);
            return;
        }

        if (itemData.resourceAmount <= 0)
        {
            Debug.LogWarning($"Shop purchase blocked because {itemData.itemName} has no reward amount.", this);
            return;
        }

        if (itemData.grantCurrency && CurrencyManager.Instance == null)
        {
            Debug.LogWarning("Shop purchase blocked because the currency reward manager is missing.", this);
            return;
        }

        if (!itemData.grantCurrency && ResourceManager.Instance == null)
        {
            Debug.LogWarning("Shop purchase blocked because the resource reward manager is missing.", this);
            return;
        }

        if (itemData.price > 0)
        {
            if (CurrencyManager.Instance == null)
            {
                Debug.LogWarning("Shop purchase blocked because CurrencyManager is missing.", this);
                return;
            }

            if (!CurrencyManager.Instance.SpendCurrency(itemData.currencyType, itemData.price))
                return;
        }

        ShopRewardGranter.Grant(itemData, itemData.currencyType.ToString(), itemData.price);
        RefreshCurrencyBar();
        Cancel();
    }

    /// <summary>
    /// O shop dang "xem quang cao nhan thuong": show rewarded ad, xem xong moi phat thuong.
    /// Khong di qua popup xac nhan vi khong ton tien.
    /// </summary>
    public void WatchAdForItem(ShopItems.ShopItemData itemData)
    {
        if (itemData == null || !itemData.isRewardedAd) return;
        if (adRequestInFlight) return;

        var ads = RewardedAdsManager.Instance;
        if (ads == null)
        {
            Debug.LogWarning("Khong xem duoc quang cao vi thieu RewardedAdsManager.", this);
            return;
        }

        adRequestInFlight = true;
        ads.ShowRewardedAd(
            onRewardEarned: () =>
            {
                adRequestInFlight = false;
                GrantAdReward(itemData);
            },
            onUnavailable: reason =>
            {
                adRequestInFlight = false;
                Debug.LogWarning($"Chua xem duoc quang cao cho {itemData.itemName}: {reason}", this);
            });
    }

    private void GrantAdReward(ShopItems.ShopItemData itemData)
    {
        ShopRewardGranter.Grant(itemData, "RewardedAd", 0);
    }

    /// <summary>
    /// O IAP: bam la mua luon, khong qua popup xac nhan. Google Play da co man
    /// xac nhan thanh toan rieng cua no, hoi hai lan la thua va de nguoi choi bo giua chung.
    /// </summary>
    public void BuyIapItem(ShopItems.ShopItemData itemData)
    {
        if (itemData == null || !itemData.isIAP) return;

        StartIapPurchase(itemData);
    }

    /// <summary>
    /// Day giao dich sang Google Play. Thuong duoc phat trong IAPManager ngay khi don
    /// chuyen sang pending, nen o day chi lo dong popup va bao loi.
    /// </summary>
    private void StartIapPurchase(ShopItems.ShopItemData itemData)
    {
        if (itemData == null) return;

        var iap = IAPManager.Instance;
        if (iap == null)
        {
            Debug.LogWarning("Khong mua duoc vi thieu IAPManager.", this);
            Cancel();
            return;
        }

        string productId = itemData.ResolveIapProductId();
        Cancel();

        iap.Purchase(productId, (success, error) =>
        {
            if (!success)
                Debug.LogWarning($"Mua '{productId}' that bai: {error}", this);
        });
    }

    public void Cancel()
    {
        selectedItem = null;
        price = 0;
        currencyType = CurrencyType.Coins;
        resourceGranted = ResourceType.Marshmallow;
        resourceAmount = 0;
        grantCurrency = false;
        currencyGranted = CurrencyType.Coins;
        if (confirmPopUp != null)
            confirmPopUp.SetActive(false);
    }
    public void SpawnAllItems()
    {
        AutoWire();
        if (shopItemsDatabase == null)
        {
            Debug.LogWarning("ShopItemsSpawner: Missing ShopItems database!");
            return;
        }

        if (itemsParent == null)
        {
            Debug.LogWarning("ShopItemsSpawner: Missing itemsParent (item parent)!");
            return;
        }

        itemSlots.Clear();
        gemSlots.Clear();
        CollectPlacedSlots(itemsParent, itemSlots);
        if (gemPacksParent != null && gemPacksParent != itemsParent)
            CollectPlacedSlots(gemPacksParent, gemSlots);

        int itemIndex = 0;
        int gemIndex = 0;

        foreach (var itemData in shopItemsDatabase.items)
        {
            if (itemData == null) continue;

            bool useGemParent = itemData.isGemPack && gemPacksParent != null && gemPacksParent != itemsParent;
            List<ShopItemUI> slots = useGemParent ? gemSlots : itemSlots;
            int slotIndex = useGemParent ? gemIndex++ : itemIndex++;
            var ui = GetPlacedSlot(slots, slotIndex);
            if (ui != null)
            {
                ui.shopItemsSpawner = this;
                ui.Setup(itemData);
            }
            else
            {
                Debug.LogWarning($"ShopItemsSpawner: Missing hierarchy ShopItemUI slot for {itemData.itemName}.", this);
            }
        }
    }

    public void CloseShop()
    {
        Cancel();
        gameObject.SetActive(false);
        RestoreSlimes();
    }

    public void RestoreSlimes()
    {
        var worldManager = SlimeWorldManager.Instance ?? Object.FindFirstObjectByType<SlimeWorldManager>();
        if (worldManager != null)
        {
            worldManager.StartWorldView();
        }
    }

    private void AutoWire()
    {
        if (itemsParent == null)
            itemsParent = ResolveShopSlotParent("ItemsScrollView", "ItemsGrid", "ItemsParent", "ItemsContent");
        if (gemPacksParent == null)
            gemPacksParent = ResolveShopSlotParent("GemPacksScrollView", "GemGrid", "GemPacksGrid", "GemPacksParent");
        if (confirmPopUp == null)
        {
            var confirm = FindChildRecursive(transform, "ConfirmPopup") ?? FindChildRecursive(transform, "ConfirmPopUp") ?? FindChildRecursive(transform, "confirm");
            if (confirm != null) confirmPopUp = confirm.gameObject;
        }

        if (confirmNoticeText == null && confirmPopUp != null)
        {
            confirmNoticeText = (FindChildRecursive(confirmPopUp.transform, "NoticeText")
                                 ?? FindChildRecursive(confirmPopUp.transform, "Notice"))?.GetComponent<TMP_Text>();
        }

        AutoWireCurrencyBar();
    }

    public void RefreshCurrencyBar()
    {
        AutoWireCurrencyBar();
        if (CurrencyManager.Instance == null)
            return;

        SetCurrencyText(coinsBalanceText, CurrencyManager.Instance.GetCurrency(CurrencyType.Coins));
        SetCurrencyText(gemsBalanceText, CurrencyManager.Instance.GetCurrency(CurrencyType.Gems));
    }

    private void HandleCurrencyChanged(CurrencyType type, int oldAmount, int newAmount)
    {
        switch (type)
        {
            case CurrencyType.Coins:
                SetCurrencyText(coinsBalanceText, newAmount);
                break;
            case CurrencyType.Gems:
                SetCurrencyText(gemsBalanceText, newAmount);
                break;
        }
    }

    private void AutoWireCurrencyBar()
    {
        if (coinsBalanceText != null && gemsBalanceText != null)
            return;

        Transform currencyBar = FindChildRecursive(transform, "CurrencyBar");
        if (currencyBar == null)
            return;

        if (coinsBalanceText == null)
            coinsBalanceText = FindChildRecursive(currencyBar, "Coin")?.GetComponent<TMP_Text>();
        if (gemsBalanceText == null)
            gemsBalanceText = FindChildRecursive(currencyBar, "Gem")?.GetComponent<TMP_Text>();
    }

    private static void SetCurrencyText(TMP_Text target, int amount)
    {
        if (target != null)
            target.text = CurrencyAmountFormatter.Format(amount);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    private void WireConfirmButtons()
    {
        if (confirmButtonsWired) return;
        if (confirmPopUp == null) return;

        Button confirmButton = FindChildRecursive(confirmPopUp.transform, "ConfirmButton")?.GetComponent<Button>();
        Button cancelButton = FindChildRecursive(confirmPopUp.transform, "CancelButton")?.GetComponent<Button>();

        if (confirmButton != null && !HasPersistentMethod(confirmButton, nameof(Confirmed)))
            confirmButton.onClick.AddListener(Confirmed);
        if (cancelButton != null && !HasPersistentMethod(cancelButton, nameof(Cancel)))
            cancelButton.onClick.AddListener(Cancel);

        confirmButtonsWired = true;
    }

    private ShopItemUI GetPlacedSlot(List<ShopItemUI> slots, int index)
    {
        if (index < slots.Count)
            return slots[index];

        return null;
    }

    private void CollectPlacedSlots(Transform parent, List<ShopItemUI> slots)
    {
        if (parent == null) return;
        CollectDirectSlots(parent, slots);

        if (slots.Count > 0)
            return;

        Transform viewport = FindDirectChild(parent, "Viewport");
        Transform content = viewport != null
            ? FindDirectChild(viewport, "ItemsGrid") ?? FindDirectChild(viewport, "GemGrid") ?? FindDirectChild(viewport, "Content")
            : null;

        if (content != null)
            CollectDirectSlots(content, slots);
    }

    private void CollectDirectSlots(Transform parent, List<ShopItemUI> slots)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            ShopItemUI ui = parent.GetChild(i).GetComponent<ShopItemUI>();
            if (ui != null)
                slots.Add(ui);
        }
    }

    private bool HasPersistentMethod(Button button, string methodName)
    {
        if (button == null) return false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            if (button.onClick.GetPersistentMethodName(i) == methodName)
                return true;
        return false;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName) return child;
            var found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private Transform ResolveShopSlotParent(string scrollViewName, params string[] contentNames)
    {
        Transform scrollView = FindChildRecursive(transform, scrollViewName);
        if (scrollView != null)
        {
            Transform viewport = FindDirectChild(scrollView, "Viewport");
            if (viewport != null)
            {
                foreach (string contentName in contentNames)
                {
                    Transform content = FindDirectChild(viewport, contentName);
                    if (content != null)
                        return content;
                }
            }

            return scrollView;
        }

        foreach (string contentName in contentNames)
        {
            Transform content = FindChildRecursive(transform, contentName);
            if (content != null)
                return content;
        }

        return null;
    }

    private Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }
}










