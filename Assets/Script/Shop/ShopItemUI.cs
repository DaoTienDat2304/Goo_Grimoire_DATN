using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TMP_Text nameTmpText;
    public TMP_Text priceTmpText;
    public TMP_Text descriptionTmpText;
    public TMP_Text amountTmpText;
    public Button buyButton;
    public TMP_Text buyButtonLabel;

    [Header("Rewarded Ad Look")]
    [Tooltip("Chu mac dinh tren nut khi o nay la o xem quang cao.")]
    public string defaultAdButtonLabel = "WatchADS";
    [Tooltip("Chu hien khi quang cao chua tai xong.")]
    public string adLoadingLabel = "Loading...";
    [Tooltip("Chu mac dinh o o gia khi la o quang cao.")]
    public string defaultAdPriceLabel = "FREE";
    [Tooltip("Co chu 'WatchADS' = co chu 'Buy' goc nhan he so nay. Chu dai hon nen de nho lai cho vua nut.")]
    [Range(0.2f, 1f)]
    public float adLabelFontSizeScale = 0.6f;

    public ShopItemsSpawner shopItemsSpawner;
    private ShopItems.ShopItemData data;

    private bool labelDefaultsCaptured;
    private string defaultLabelText;
    private float defaultLabelFontSize;
    private float defaultLabelFontSizeMin;
    private float defaultLabelFontSizeMax;
    private bool defaultLabelAutoSize;

    private bool IsRewardedAdSlot => data != null && data.isRewardedAd;

    public void Setup(ShopItems.ShopItemData itemData)
    {
        AutoWire();
        data = itemData;

        if (data == null) return;

        SetText(nameTmpText, data.itemName);
        SetText(priceTmpText, ResolvePriceLabel());
        SetText(descriptionTmpText, data.description);
        SetText(amountTmpText, data.resourceAmount.ToString());
        ApplyBuyButtonAppearance();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => OnBuyButtonClicked());
        }

        // O quang cao thi tai truoc ngay de nguoi choi bam la co ad.
        if (IsRewardedAdSlot)
            RewardedAdsManager.Instance?.LoadAd();
    }

    private void OnEnable()
    {
        RewardedAdsManager.OnAdReadyChanged += HandleAdReadyChanged;
        IAPManager.OnProductsFetched += HandleIapProductsFetched;
        ApplyBuyButtonAppearance();
    }

    private void OnDisable()
    {
        RewardedAdsManager.OnAdReadyChanged -= HandleAdReadyChanged;
        IAPManager.OnProductsFetched -= HandleIapProductsFetched;
    }

    private void HandleAdReadyChanged(bool _)
    {
        ApplyBuyButtonAppearance();
    }

    /// <summary>Lay duoc gia that tu store roi thi ve lai o gia.</summary>
    private void HandleIapProductsFetched()
    {
        if (data == null) return;
        SetText(priceTmpText, ResolvePriceLabel());
    }

    private string ResolvePriceLabel()
    {
        if (IsRewardedAdSlot)
        {
            return !string.IsNullOrWhiteSpace(data.adPriceLabel)
                ? data.adPriceLabel
                : defaultAdPriceLabel;
        }

        if (data.isIAP)
        {
            // Google Play bat buoc hien gia noi te that cua nguoi choi, khong phai chuoi cung.
            var iap = IAPManager.Instance;
            if (iap != null && iap.TryGetLocalizedPrice(data.ResolveIapProductId(), out string storePrice))
                return storePrice;
            // Chua ket noi duoc store thi tam dung nhan trong asset ($4.99...).
        }

        return !string.IsNullOrWhiteSpace(data.priceLabelOverride)
            ? data.priceLabelOverride
            : $"{data.price} {data.currencyType}";
    }

    private string ResolveAdButtonLabel()
    {
        var ads = RewardedAdsManager.Instance;
        if (ads != null && !ads.IsAdReady)
            return adLoadingLabel;

        return !string.IsNullOrWhiteSpace(data.adButtonLabel)
            ? data.adButtonLabel
            : defaultAdButtonLabel;
    }

    private void ApplyBuyButtonAppearance()
    {
        CaptureLabelDefaults();
        if (buyButtonLabel == null) return;

        if (IsRewardedAdSlot)
        {
            buyButtonLabel.text = ResolveAdButtonLabel();

            // "WatchADS" dai hon "Buy" nhieu nen ha tran co chu xuong cho vua nut.
            // San co auto-size cua prefab lam luoi an toan, va khong cho nho hon muc san co cua prefab.
            float targetSize = Mathf.Max(1f, defaultLabelFontSize * adLabelFontSizeScale);
            buyButtonLabel.fontSize = targetSize;
            buyButtonLabel.enableAutoSizing = true;
            buyButtonLabel.fontSizeMax = targetSize;
            buyButtonLabel.fontSizeMin = Mathf.Min(defaultLabelFontSizeMin, targetSize);
        }
        else
        {
            buyButtonLabel.enableAutoSizing = defaultLabelAutoSize;
            buyButtonLabel.fontSizeMin = defaultLabelFontSizeMin;
            buyButtonLabel.fontSizeMax = defaultLabelFontSizeMax;
            buyButtonLabel.fontSize = defaultLabelFontSize;
            if (!string.IsNullOrEmpty(defaultLabelText))
                buyButtonLabel.text = defaultLabelText;
        }
    }

    private void CaptureLabelDefaults()
    {
        if (labelDefaultsCaptured || buyButtonLabel == null) return;

        defaultLabelText = buyButtonLabel.text;
        defaultLabelFontSize = buyButtonLabel.fontSize;
        defaultLabelFontSizeMin = buyButtonLabel.fontSizeMin;
        defaultLabelFontSizeMax = buyButtonLabel.fontSizeMax;
        defaultLabelAutoSize = buyButtonLabel.enableAutoSizing;
        labelDefaultsCaptured = true;
    }

    private void OnBuyButtonClicked()
    {
        if (data == null)
        {
            Debug.LogWarning($"{nameof(ShopItemUI)} on {name} has no shop item data.", this);
            return;
        }

        if (shopItemsSpawner == null)
        {
            Debug.LogWarning($"{nameof(ShopItemUI)} on {name} cannot open confirm popup because ShopItemsSpawner is missing.", this);
            return;
        }

        // O quang cao thi khong qua popup xac nhan mua - bam la xem ad luon.
        if (IsRewardedAdSlot)
        {
            shopItemsSpawner.WatchAdForItem(data);
            ApplyBuyButtonAppearance();
            return;
        }

        shopItemsSpawner.SelectItem(data);
    }

    private void AutoWire()
    {
        if (buyButton == null)
            buyButton = FindChild("BuyButton")?.GetComponent<Button>();

        if (buyButtonLabel == null && buyButton != null)
            buyButtonLabel = FindChildRecursive(buyButton.transform, "Label")?.GetComponent<TMP_Text>()
                             ?? buyButton.GetComponentInChildren<TMP_Text>(true);

        WireText("NameText", ref nameTmpText);
        if (nameTmpText == null) WireText("Name", ref nameTmpText);
        WireText("Price", ref priceTmpText);
        WireText("Description", ref descriptionTmpText);
        WireText("Amount", ref amountTmpText);
    }

    private void WireText(string childName, ref TMP_Text tmp)
    {
        var child = FindChild(childName);
        if (child == null) return;
        if (tmp == null) tmp = child.GetComponent<TMP_Text>();
    }

    private void SetText(TMP_Text tmp, string value)
    {
        if (tmp != null) tmp.text = value;
    }

    private Transform FindChild(string childName)
    {
        return FindChildRecursive(transform, childName);
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
}
