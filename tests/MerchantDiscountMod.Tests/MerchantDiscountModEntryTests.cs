using MerchantDiscountMod.ModEntry;

namespace MerchantDiscountMod.Tests;

public sealed class MerchantDiscountModEntryTests
{
    [Fact]
    public void OfflineEntryBuildsHostWithMetadataAndRuntime()
    {
        var host = MerchantDiscountModEntry.CreateOfflineHost();

        Assert.Equal(MerchantDiscountModInfo.ModId, host.ModId);
        Assert.NotNull(host.Runtime);
        Assert.NotNull(host.ShopBridge);

        var shopState = host.Runtime.EnterShop();

        Assert.True(shopState.MerchantVisible);
        Assert.True(shopState.InventoryAvailable);
    }

    [Fact]
    public void ModInfoExposesBuildStampForLiveLogVerification()
    {
        Assert.Equal("RobMerchant", MerchantDiscountModInfo.ModId);
        Assert.Equal("Rob the Merchant", MerchantDiscountModInfo.DisplayName);
        Assert.Equal("RobMerchant.json", MerchantDiscountModInfo.ManifestFileName);
        Assert.Equal("new-run-reset-fix-v0.1.5", MerchantDiscountModInfo.BuildStamp);
    }
}
