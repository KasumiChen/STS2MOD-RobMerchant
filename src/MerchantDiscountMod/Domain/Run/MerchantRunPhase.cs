namespace MerchantDiscountMod.Domain.Run;

public enum MerchantRunPhase
{
    Available = 0,
    BattlePending = 1,
    DefeatedCurrentShopReward = 2,
    DefeatedFutureShopsClosed = 3
}
