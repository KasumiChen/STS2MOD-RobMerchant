using MerchantDiscountMod.Bootstrap;
using MerchantDiscountMod.Combat;

namespace MerchantDiscountMod.Tests;

public sealed class MerchantDiscountRuntimeTests
{
    [Fact]
    public void FreshShopStartsWithMerchantAndInventoryAvailable()
    {
        var runtime = new MerchantDiscountRuntime();

        var shopState = runtime.EnterShop();

        Assert.True(shopState.MerchantVisible);
        Assert.True(shopState.InventoryAvailable);
        Assert.True(shopState.RestockAllowed);
        Assert.False(shopState.PricesAreZero);
    }

    [Fact]
    public void WinningMerchantBattleMakesCurrentShopFreeAndMerchantless()
    {
        var runtime = new MerchantDiscountRuntime();
        runtime.EnterShop();

        for (var attempt = 1; attempt <= 6; attempt += 1)
        {
            runtime.TryUnaffordablePurchase();
        }

        runtime.ConfirmDiscount();
        var shopState = runtime.ResolveMerchantBattle(MerchantBattleResult.Victory);

        Assert.False(shopState.MerchantVisible);
        Assert.True(shopState.InventoryAvailable);
        Assert.False(shopState.RestockAllowed);
        Assert.True(shopState.PricesAreZero);
    }

    [Fact]
    public void FutureShopsAfterMerchantVictoryContainNoMerchantAndNoGoods()
    {
        var runtime = new MerchantDiscountRuntime();
        runtime.EnterShop();

        for (var attempt = 1; attempt <= 6; attempt += 1)
        {
            runtime.TryUnaffordablePurchase();
        }

        runtime.ConfirmDiscount();
        runtime.ResolveMerchantBattle(MerchantBattleResult.Victory);
        runtime.LeaveShop();

        var futureShopState = runtime.EnterShop();

        Assert.False(futureShopState.MerchantVisible);
        Assert.False(futureShopState.InventoryAvailable);
        Assert.False(futureShopState.RestockAllowed);
        Assert.False(futureShopState.PricesAreZero);
    }

    [Fact]
    public void MerchantClickDoesNothingWhenShopAlreadyHasNoMerchant()
    {
        var runtime = new MerchantDiscountRuntime();
        runtime.EnterShop();

        for (var attempt = 1; attempt <= 6; attempt += 1)
        {
            runtime.TryUnaffordablePurchase();
        }

        runtime.ConfirmDiscount();
        runtime.ResolveMerchantBattle(MerchantBattleResult.Victory);

        var response = runtime.ClickMerchant();

        Assert.Equal(MerchantDiscountMod.Domain.Shop.MerchantInteractionOutcome.IgnoredMerchantUnavailable, response.Outcome);
        Assert.Equal("This shop has no merchant interaction target.", response.IgnoredReason);
    }
}
