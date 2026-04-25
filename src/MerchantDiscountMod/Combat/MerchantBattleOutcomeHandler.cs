using MerchantDiscountMod.Domain.Run;
using MerchantDiscountMod.Domain.Shop;

namespace MerchantDiscountMod.Combat;

public sealed class MerchantBattleOutcomeHandler
{
    private readonly MerchantRunState runState;
    private readonly ShopInventoryState shopInventoryState;

    public MerchantBattleOutcomeHandler(
        MerchantRunState runState,
        ShopInventoryState shopInventoryState)
    {
        this.runState = runState;
        this.shopInventoryState = shopInventoryState;
    }

    public MerchantBattleResolution Apply(MerchantBattleResult result)
    {
        if (!runState.MerchantChallengePending)
        {
            return CreateResolution(
                result,
                applied: false,
                "Ignored battle result because no merchant challenge is pending.");
        }

        switch (result)
        {
            case MerchantBattleResult.Victory:
                runState.MarkMerchantDefeated();
                shopInventoryState.ApplyMerchantVictoryOutcomeToCurrentShop();
                return CreateResolution(
                    result,
                    applied: true,
                    "Merchant defeated: current shop inventory is preserved and all visible prices become zero.");
            case MerchantBattleResult.Defeat:
                runState.ClearPendingChallenge();
                return CreateResolution(
                    result,
                    applied: true,
                    "Merchant challenge lost: discounts are not granted and normal defeat handling remains external.");
            default:
                return CreateResolution(
                    result,
                    applied: false,
                    "Unknown merchant battle result left run and shop state unchanged.");
        }
    }

    private MerchantBattleResolution CreateResolution(
        MerchantBattleResult result,
        bool applied,
        string consequence) =>
        new(
            result,
            applied,
            runState.Phase,
            shopInventoryState.Mode,
            consequence);
}
