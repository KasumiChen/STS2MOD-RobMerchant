using MerchantDiscountMod.Bootstrap;
using MerchantDiscountMod.Integration;

namespace MerchantDiscountMod.ModEntry;

public sealed class MerchantDiscountOfflineHost
{
    public MerchantDiscountOfflineHost(
        string modId,
        MerchantDiscountRuntime runtime,
        MerchantShopEventBridge shopBridge)
    {
        ModId = modId;
        Runtime = runtime;
        ShopBridge = shopBridge;
    }

    public string ModId { get; }

    public MerchantDiscountRuntime Runtime { get; }

    public MerchantShopEventBridge ShopBridge { get; }
}
