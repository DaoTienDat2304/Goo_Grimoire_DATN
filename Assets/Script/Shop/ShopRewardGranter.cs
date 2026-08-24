/// <summary>
/// Mot cho duy nhat de phat thuong cho item shop, dung chung cho ca 3 duong:
/// mua bang tien trong game, xem rewarded ad, va IAP.
/// </summary>
public static class ShopRewardGranter
{
    /// <param name="source">Ghi vao analytics: "Coins" / "Gems" / "RewardedAd" / "IAP".</param>
    /// <param name="pricePaid">Gia da tra bang tien trong game; ads va IAP thi la 0.</param>
    public static void Grant(ShopItems.ShopItemData itemData, string source, int pricePaid)
    {
        if (itemData == null) return;

        if (itemData.grantCurrency)
            CurrencyManager.Instance?.AddCurrency(itemData.currencyGranted, itemData.resourceAmount);
        else
            ResourceManager.Instance?.AddResource(itemData.resourceGranted, itemData.resourceAmount);

        FirebaseAnalyticsManager.LogShopPurchase(
            source,
            pricePaid,
            itemData.grantCurrency ? itemData.currencyGranted.ToString() : itemData.resourceGranted.ToString(),
            itemData.resourceAmount);

        SaveAndLoadSystem.Instance?.Save();
    }
}
