using MerchantDiscountMod.Bootstrap;
using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Domain.Shop;

namespace MerchantDiscountMod.Integration;

public sealed class MerchantShopEventBridge
{
    private readonly MerchantDiscountRuntime runtime;
    private readonly IShopStateRenderer shopStateRenderer;
    private readonly IMerchantDialoguePort merchantDialoguePort;
    private readonly IDiscountPromptPort discountPromptPort;
    private readonly IShopCombatPort shopCombatPort;
    private readonly IRunStatePersistence runStatePersistence;

    public MerchantShopEventBridge(
        MerchantDiscountRuntime runtime,
        IShopStateRenderer shopStateRenderer,
        IMerchantDialoguePort merchantDialoguePort,
        IDiscountPromptPort discountPromptPort,
        IShopCombatPort shopCombatPort,
        IRunStatePersistence runStatePersistence)
    {
        this.runtime = runtime;
        this.shopStateRenderer = shopStateRenderer;
        this.merchantDialoguePort = merchantDialoguePort;
        this.discountPromptPort = discountPromptPort;
        this.shopCombatPort = shopCombatPort;
        this.runStatePersistence = runStatePersistence;
    }

    public void OnShopEntered()
    {
        MerchantDiscountDiagnostics.Info("Shop entered.");
        TryPrepareMultiplayerVoteBridge();
        shopStateRenderer.Render(runtime.EnterShop());
    }

    public void OnNewRunStarted()
    {
        MerchantDiscountDiagnostics.Info("New run started; resetting merchant robbery state.");
        runtime.StartNewRun();
        runStatePersistence.SaveRunState();
    }

    public void OnMerchantSelected()
    {
        var response = runtime.ClickMerchant();
        MerchantDiscountDiagnostics.Info(
            $"Merchant selected pressure attempt outcome={response.Outcome} count={runtime.InteractionState.PressureAttemptCount}");
        Dispatch(response);
    }

    public bool CanOpenMerchant() => runtime.GetShopStateSnapshot().MerchantVisible;

    public void OnUnaffordablePurchaseAttempt()
    {
        var response = runtime.TryUnaffordablePurchase();
        MerchantDiscountDiagnostics.Info(
            $"Unaffordable purchase pressure attempt outcome={response.Outcome} count={runtime.InteractionState.PressureAttemptCount}");
        Dispatch(response);
    }

    public void OnDiscountAccepted()
    {
        MerchantDiscountDiagnostics.Info("Discount prompt accepted; requesting merchant combat.");
        Dispatch(runtime.ConfirmDiscount());
        runStatePersistence.SaveRunState();
    }

    public void OnDiscountDeclined()
    {
        MerchantDiscountDiagnostics.Info("Discount prompt declined.");
        runtime.CancelDiscount();
    }

    public void OnMerchantBattleResolved(MerchantBattleResult result)
    {
        var snapshot = runtime.ResolveMerchantBattle(result);
        shopStateRenderer.Render(snapshot);
        runStatePersistence.SaveRunState();
    }

    public void OnSynchronizedMerchantBattleStarted()
    {
        runtime.StartSynchronizedMerchantBattle();
        runStatePersistence.SaveRunState();
    }

    public void RefreshShopPresentation()
    {
        shopStateRenderer.Render(runtime.GetShopStateSnapshot());
    }

    public void OnShopExited()
    {
        MerchantDiscountDiagnostics.Info("Shop exited.");
        runtime.LeaveShop();
        runStatePersistence.SaveRunState();
    }

    private void Dispatch(MerchantInteractionResponse response)
    {
        if (response.DialogueLine is not null)
        {
            merchantDialoguePort.ShowMerchantLine(response.DialogueLine);
        }

        if (response.Prompt is not null)
        {
            discountPromptPort.ShowPrompt(response.Prompt);
        }

        if (response.BattleRequest is not null)
        {
            shopCombatPort.Launch(response.BattleRequest);
        }
    }

    private static void TryPrepareMultiplayerVoteBridge()
    {
        var bridgeType = typeof(MerchantShopEventBridge).Assembly.GetType(
            "MerchantDiscountMod.Integration.MerchantCombatMultiplayerBridge",
            throwOnError: false);
        var prepare = bridgeType?.GetMethod(
            "PrepareForMultiplayerRun",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        try
        {
            prepare?.Invoke(null, []);
        }
        catch (ArgumentException exception)
        {
            MerchantDiscountDiagnostics.Warn($"Merchant combat multiplayer vote bridge preparation failed: {exception.Message}");
        }
        catch (System.Reflection.TargetInvocationException exception)
        {
            MerchantDiscountDiagnostics.Warn(
                $"Merchant combat multiplayer vote bridge preparation failed: {exception.InnerException?.Message ?? exception.Message}");
        }
    }
}
