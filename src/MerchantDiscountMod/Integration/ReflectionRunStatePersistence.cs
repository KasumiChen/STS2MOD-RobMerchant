using MerchantDiscountMod.Persistence;

namespace MerchantDiscountMod.Integration;

public sealed class ReflectionRunStatePersistence : IRunStatePersistence
{
    private readonly ReflectionMerchantShopContext context;
    private readonly Func<MerchantRunStateSnapshot> snapshotProvider;
    private readonly Action<MerchantRunStateSnapshot> saveSnapshot;

    public ReflectionRunStatePersistence(
        ReflectionMerchantShopContext context,
        Func<MerchantRunStateSnapshot> loadSnapshot,
        Action<MerchantRunStateSnapshot> saveSnapshot)
    {
        this.context = context;
        snapshotProvider = loadSnapshot;
        this.saveSnapshot = saveSnapshot;
    }

    public void SaveRunState()
    {
        var snapshot = snapshotProvider();
        context.SaveRunStateSnapshot(snapshot);
        saveSnapshot(snapshot);
    }
}
