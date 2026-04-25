using MerchantDiscountMod.Domain.Run;

namespace MerchantDiscountMod.Persistence;

public static class MerchantRunStateMapper
{
    public static MerchantRunStateSnapshot ToSnapshot(MerchantRunState state) =>
        new()
        {
            Phase = state.Phase,
            MerchantChallengePending = state.MerchantChallengePending,
            MerchantDefeatedThisRun = state.MerchantDefeatedThisRun,
            CurrentShopUnlockedFreeInventory = state.CurrentShopUnlockedFreeInventory
        };

    public static MerchantRunState FromSnapshot(MerchantRunStateSnapshot snapshot)
    {
        var state = new MerchantRunState();

        if (snapshot.Phase != MerchantRunPhase.Available)
        {
            state.RestorePhase(snapshot.Phase);
            return state;
        }

        if (snapshot.MerchantDefeatedThisRun)
        {
            state.MarkMerchantDefeated();
            if (!snapshot.CurrentShopUnlockedFreeInventory)
            {
                state.ResetCurrentShopFlags();
            }
        }
        else if (snapshot.MerchantChallengePending)
        {
            state.MarkChallengePending();
        }

        return state;
    }
}
