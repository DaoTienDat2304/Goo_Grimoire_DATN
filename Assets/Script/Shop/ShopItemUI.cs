using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn script này lên prefab Shop Item.
/// Bạn có thể tự tùy chỉnh code trong hàm Setup hoặc sửa trực tiếp script này cho phù hợp UI của bạn.
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
    /// Gọi hàm này sau khi Instantiate prefab để đổ dữ liệu item vào UI.
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
            // Hiển thị giá + đơn vị tiền
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
        shopItemsSpawner.confirmPopUp.SetActive(true);
        shopItemsSpawner.price = data.price;
        shopItemsSpawner.currencyType = data.currencyType;
        shopItemsSpawner.resourceGranted = data.resourceGranted;
        shopItemsSpawner.resourceAmount = data.resourceAmount;
    }
}










