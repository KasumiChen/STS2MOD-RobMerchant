using MerchantDiscountMod.Domain.Run;

namespace MerchantDiscountMod.Persistence;

public sealed class MerchantRunStateSnapshot
{
    public MerchantRunPhase Phase { get; init; } = MerchantRunPhase.Available;

    public bool MerchantChallengePending { get; init; }

    public bool MerchantDefeatedThisRun { get; init; }

    public bool CurrentShopUnlockedFreeInventory { get; init; }
}
