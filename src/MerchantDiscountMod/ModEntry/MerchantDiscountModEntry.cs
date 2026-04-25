using MerchantDiscountMod.Bootstrap;
using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Domain.Shop;
using MerchantDiscountMod.Integration;
using MerchantDiscountMod.Persistence;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.ModEntry;

public static class MerchantDiscountModEntry
{
    public static MerchantDiscountOfflineHost CreateOfflineHost() =>
        CreateHost(new NullShopStateRenderer());

    public static MerchantDiscountLiveBootstrap CreateLiveBootstrap()
    {
        var shopContext = new ReflectionMerchantShopContext();
        var runtime = new MerchantDiscountRuntime(
            new MerchantDiscountMod.Domain.Run.MerchantRunState(),
            new MerchantDiscountMod.Domain.Shop.MerchantInteractionState(),
            new MerchantDiscountMod.Domain.Shop.ShopInventoryState(),
            () => DiscountPromptText.ForLocale(ReflectionLocaleProvider.GetCurrentLocale()));
        var promptPort = new ReflectionDiscountPromptPort(shopContext);
        var persistence = new ReflectionRunStatePersistence(
            shopContext,
            () => MerchantRunStateMapper.ToSnapshot(runtime.RunState),
            _ => { });
        var bridge = new MerchantShopEventBridge(
            runtime,
            new ReflectionMerchantShopStateRenderer(shopContext),
            new ReflectionMerchantDialoguePort(shopContext),
            promptPort,
            new ReflectionShopCombatPort(shopContext),
            persistence);
        promptPort.Connect(bridge.OnDiscountAccepted, bridge.OnDiscountDeclined);

        return new MerchantDiscountLiveBootstrap(
            new MerchantDiscountOfflineHost(MerchantDiscountModInfo.ModId, runtime, bridge),
            shopContext);
    }

    private static MerchantDiscountOfflineHost CreateHost(IShopStateRenderer shopStateRenderer)
    {
        var runtime = new MerchantDiscountRuntime();
        var bridge = new MerchantShopEventBridge(
            runtime,
            shopStateRenderer,
            new NullMerchantDialoguePort(),
            new NullDiscountPromptPort(),
            new NullShopCombatPort(),
            new NullRunStatePersistence());

        return new MerchantDiscountOfflineHost(MerchantDiscountModInfo.ModId, runtime, bridge);
    }

    private sealed class NullShopStateRenderer : IShopStateRenderer
    {
        public void Render(ShopStateSnapshot snapshot)
        {
        }
    }

    private sealed class NullMerchantDialoguePort : IMerchantDialoguePort
    {
        public void ShowMerchantLine(string line)
        {
        }
    }

    private sealed class NullDiscountPromptPort : IDiscountPromptPort
    {
        public void ShowPrompt(DiscountPromptRequest request)
        {
        }
    }

    private sealed class NullShopCombatPort : IShopCombatPort
    {
        public void Launch(MerchantBattleRequest request)
        {
        }
    }

    private sealed class NullRunStatePersistence : IRunStatePersistence
    {
        public void SaveRunState()
        {
        }
    }
}
