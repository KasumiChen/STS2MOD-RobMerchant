#if STS2_LIVE || NET9_0_OR_GREATER
using MegaCrit.Sts2.Core.Modding;
using MerchantDiscountMod.Integration;

namespace MerchantDiscountMod.ModEntry;

[ModInitializer(nameof(Initialize))]
public static class Sts2ModInitializer
{
    public static void Initialize()
    {
        try
        {
            MerchantDiscountDiagnostics.Info($"STS2 mod initializer running build={MerchantDiscountModInfo.BuildStamp}.");
            MerchantDiscountModEntry.CreateLiveBootstrap().ApplyHarmonyPatches();
            MerchantDiscountDiagnostics.Info("STS2 mod initializer completed.");
        }
        catch (Exception exception)
        {
            MerchantDiscountDiagnostics.Error("STS2 mod initializer failed", exception);
            Console.Error.WriteLine($"[{MerchantDiscountModInfo.ModId}] {exception}");
            throw;
        }
    }
}
#endif
