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

    public ShopItemsSpawner shopItemsSpawner;
    private ShopItems.ShopItemData data;
    public void Setup(ShopItems.ShopItemData itemData)
    {
        AutoWire();
        data = itemData;

        if (data == null) return;

        SetText(nameTmpText, data.itemName);

        string priceLabel = !string.IsNullOrWhiteSpace(data.priceLabelOverride)
            ? data.priceLabelOverride
            : $"{data.price} {data.currencyType}";
        SetText(priceTmpText, priceLabel);

        SetText(descriptionTmpText, data.description);
        SetText(amountTmpText, data.resourceAmount.ToString());
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => OnBuyButtonClicked());
        }
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

        shopItemsSpawner.SelectItem(data);
    }

    private void AutoWire()
    {
        if (buyButton == null)
            buyButton = FindChild("BuyButton")?.GetComponent<Button>();

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










