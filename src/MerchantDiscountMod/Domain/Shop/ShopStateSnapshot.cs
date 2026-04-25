namespace MerchantDiscountMod.Domain.Shop;

public sealed record ShopStateSnapshot(
    bool MerchantVisible,
    bool InventoryAvailable,
    bool RestockAllowed,
    bool PricesAreZero,
    ShopInventoryMode InventoryMode,
    bool FutureStockSuppressed,
    bool CurrentInventoryPreserved,
    string PresentationHint);
