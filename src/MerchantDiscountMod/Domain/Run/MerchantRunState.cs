namespace MerchantDiscountMod.Domain.Run;

public sealed class MerchantRunState
{
    public MerchantRunPhase Phase { get; private set; } = MerchantRunPhase.Available;

    public bool MerchantChallengePending => Phase == MerchantRunPhase.BattlePending;

    public bool MerchantDefeatedThisRun =>
        Phase is MerchantRunPhase.DefeatedCurrentShopReward or MerchantRunPhase.DefeatedFutureShopsClosed;

    public bool CurrentShopUnlockedFreeInventory => Phase == MerchantRunPhase.DefeatedCurrentShopReward;

    public bool FutureShopsSuppressed => Phase == MerchantRunPhase.DefeatedFutureShopsClosed;

    public bool MerchantCanBeChallenged => Phase == MerchantRunPhase.Available;

    public void MarkChallengePending()
    {
        if (Phase == MerchantRunPhase.Available)
        {
            Phase = MerchantRunPhase.BattlePending;
        }
    }

    public void ClearPendingChallenge()
    {
        if (Phase == MerchantRunPhase.BattlePending)
        {
            Phase = MerchantRunPhase.Available;
        }
    }

    public void MarkMerchantDefeated()
    {
        Phase = MerchantRunPhase.DefeatedCurrentShopReward;
    }

    public void ResetCurrentShopFlags()
    {
        Phase = Phase switch
        {
            MerchantRunPhase.BattlePending => MerchantRunPhase.Available,
            MerchantRunPhase.DefeatedCurrentShopReward => MerchantRunPhase.DefeatedFutureShopsClosed,
            _ => Phase
        };
    }

    public void ResetForNewRun()
    {
        Phase = MerchantRunPhase.Available;
    }

    public void RestorePhase(MerchantRunPhase phase)
    {
        Phase = phase;
    }
}
