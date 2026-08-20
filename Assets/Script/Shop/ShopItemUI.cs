using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public Text nameText;
    public Text priceText;
    public Text descriptionText;
    public Text amountText;
    public Button buyButton;

    public ShopItemsSpawner shopItemsSpawner;
    private ShopItems.ShopItemData data;

    /// <summary>
    /// </summary>
    public void Setup(ShopItems.ShopItemData itemData)
    {
        data = itemData;

        if (data == null) return;

        if (iconImage != null)
            iconImage.sprite = data.icon;

        if (nameText != null)
            nameText.text = data.itemName;

        if (priceText != null)
        {
            string currencyShort = data.currencyType.ToString(); // Coins / Gems...
            priceText.text = $"{data.price} {currencyShort}";
        }

        if (descriptionText != null)
            descriptionText.text = data.description;
        if (amountText != null) 
            amountText.text = data.resourceAmount.ToString();
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => OnBuyButtonClicked());
        }
    }

    private void OnBuyButtonClicked()
    {
        if (shopItemsSpawner == null)
        {
            Debug.LogWarning($"{nameof(ShopItemUI)} on {name} cannot open confirm popup because ShopItemsSpawner is missing.", this);
            return;
        }

        if (shopItemsSpawner.confirmPopUp != null)
            shopItemsSpawner.confirmPopUp.SetActive(true);
        else
            Debug.LogWarning($"{nameof(ShopItemUI)} on {name} cannot open confirm popup because confirmPopUp is missing.", this);

        shopItemsSpawner.price = data.price;
        shopItemsSpawner.currencyType = data.currencyType;
        shopItemsSpawner.resourceGranted = data.resourceGranted;
        shopItemsSpawner.resourceAmount = data.resourceAmount;
    }
}










