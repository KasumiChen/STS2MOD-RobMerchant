namespace MerchantDiscountMod.Domain.Shop;

public sealed class ShopInventoryState
{
    public ShopInventoryMode Mode { get; private set; } = ShopInventoryMode.StockedWithMerchant;

    public bool MerchantVisible => Mode == ShopInventoryMode.StockedWithMerchant;

    public bool RestockAllowed => Mode == ShopInventoryMode.StockedWithMerchant;

    public bool InventoryAvailable => Mode != ShopInventoryMode.EmptyFutureShopAfterMerchantVictory;

    public bool PricesAreZero => Mode == ShopInventoryMode.FreeCurrentShopAfterMerchantVictory;

    public bool FutureStockSuppressed => Mode == ShopInventoryMode.EmptyFutureShopAfterMerchantVictory;

    public bool CurrentInventoryPreserved => Mode != ShopInventoryMode.EmptyFutureShopAfterMerchantVictory;

    public string PresentationHint => Mode switch
    {
        ShopInventoryMode.FreeCurrentShopAfterMerchantVictory =>
            "Hide merchant, preserve current shelves, set visible shop prices to zero, and block restocks.",
        ShopInventoryMode.EmptyFutureShopAfterMerchantVictory =>
            "Render a merchantless shop with no generated stock for the rest of the run.",
        _ => "Render a normal merchant shop with generated inventory."
    };

    public void ResetToDefaultShop()
    {
        Mode = ShopInventoryMode.StockedWithMerchant;
    }

    public void ApplyMerchantVictoryOutcomeToCurrentShop()
    {
        Mode = ShopInventoryMode.FreeCurrentShopAfterMerchantVictory;
    }

    public void ApplyMerchantDefeatedFutureShopOutcome()
    {
        Mode = ShopInventoryMode.EmptyFutureShopAfterMerchantVictory;
    }
}
