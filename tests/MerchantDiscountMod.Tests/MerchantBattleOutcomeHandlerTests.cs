using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Domain.Run;
using MerchantDiscountMod.Domain.Shop;

namespace MerchantDiscountMod.Tests;

public sealed class MerchantBattleOutcomeHandlerTests
{
    [Fact]
    public void VictoryMarksRunAndUnlocksFreeCurrentShop()
    {
        var runState = new MerchantRunState();
        var shopInventoryState = new ShopInventoryState();
        var handler = new MerchantBattleOutcomeHandler(runState, shopInventoryState);
        runState.MarkChallengePending();

        handler.Apply(MerchantBattleResult.Victory);

        Assert.True(runState.MerchantDefeatedThisRun);
        Assert.True(runState.CurrentShopUnlockedFreeInventory);
        Assert.False(shopInventoryState.MerchantVisible);
        Assert.True(shopInventoryState.InventoryAvailable);
        Assert.True(shopInventoryState.PricesAreZero);
        Assert.False(shopInventoryState.RestockAllowed);
    }

    [Fact]
    public void DefeatClearsPendingChallengeWithoutGrantingDiscounts()
    {
        var runState = new MerchantRunState();
        var shopInventoryState = new ShopInventoryState();
        var handler = new MerchantBattleOutcomeHandler(runState, shopInventoryState);
        runState.MarkChallengePending();

        handler.Apply(MerchantBattleResult.Defeat);

        Assert.False(runState.MerchantChallengePending);
        Assert.False(runState.MerchantDefeatedThisRun);
        Assert.True(shopInventoryState.MerchantVisible);
        Assert.False(shopInventoryState.PricesAreZero);
    }
}
