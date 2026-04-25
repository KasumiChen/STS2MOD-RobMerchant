using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Domain.Run;
using MerchantDiscountMod.Domain.Shop;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Bootstrap;

public sealed class MerchantDiscountRuntime
{
    private readonly MerchantRunState runState;
    private readonly MerchantInteractionState interactionState;
    private readonly ShopInventoryState shopInventoryState;
    private readonly MerchantInteractionController interactionController;
    private readonly MerchantBattleOutcomeHandler battleOutcomeHandler;

    public MerchantDiscountRuntime()
        : this(
            new MerchantRunState(),
            new MerchantInteractionState(),
            new ShopInventoryState())
    {
    }

    public MerchantDiscountRuntime(
        MerchantRunState runState,
        MerchantInteractionState interactionState,
        ShopInventoryState shopInventoryState,
        Func<DiscountPromptRequest>? discountPromptFactory = null)
    {
        this.runState = runState;
        this.interactionState = interactionState;
        this.shopInventoryState = shopInventoryState;
        interactionController = new MerchantInteractionController(
            interactionState,
            runState,
            discountPromptFactory);
        battleOutcomeHandler = new MerchantBattleOutcomeHandler(runState, shopInventoryState);
    }

    public MerchantRunState RunState => runState;

    public ShopInventoryState ShopInventoryState => shopInventoryState;

    public MerchantInteractionState InteractionState => interactionState;

    public ShopStateSnapshot EnterShop()
    {
        interactionState.Reset();

        if (runState.FutureShopsSuppressed)
        {
            shopInventoryState.ApplyMerchantDefeatedFutureShopOutcome();
        }
        else if (!runState.CurrentShopUnlockedFreeInventory)
        {
            shopInventoryState.ResetToDefaultShop();
        }

        return GetShopStateSnapshot();
    }

    public void LeaveShop()
    {
        interactionState.Reset();
        runState.ResetCurrentShopFlags();
    }

    public MerchantInteractionResponse ClickMerchant()
    {
        if (!shopInventoryState.MerchantVisible)
        {
            return MerchantInteractionResponse.Ignored(
                MerchantInteractionOutcome.IgnoredMerchantUnavailable,
                "This shop has no merchant interaction target.");
        }

        return interactionController.OnMerchantClicked();
    }

    public MerchantInteractionResponse TryUnaffordablePurchase()
    {
        if (!shopInventoryState.MerchantVisible)
        {
            return MerchantInteractionResponse.Ignored(
                MerchantInteractionOutcome.IgnoredMerchantUnavailable,
                "This shop has no active merchant to negotiate with.");
        }

        return interactionController.OnUnaffordablePurchaseAttempt();
    }

    public MerchantInteractionResponse ConfirmDiscount()
    {
        if (!shopInventoryState.MerchantVisible)
        {
            return MerchantInteractionResponse.Ignored(
                MerchantInteractionOutcome.IgnoredMerchantUnavailable,
                "This shop has no merchant interaction target.");
        }

        return interactionController.OnDiscountConfirmed();
    }

    public void CancelDiscount()
    {
        interactionController.OnDiscountCanceled();
    }

    public ShopStateSnapshot ResolveMerchantBattle(MerchantBattleResult result)
    {
        battleOutcomeHandler.Apply(result);
        return GetShopStateSnapshot();
    }

    public ShopStateSnapshot GetShopStateSnapshot() =>
        new(
            shopInventoryState.MerchantVisible,
            shopInventoryState.InventoryAvailable,
            shopInventoryState.RestockAllowed,
            shopInventoryState.PricesAreZero,
            shopInventoryState.Mode,
            shopInventoryState.FutureStockSuppressed,
            shopInventoryState.CurrentInventoryPreserved,
            shopInventoryState.PresentationHint);
}
