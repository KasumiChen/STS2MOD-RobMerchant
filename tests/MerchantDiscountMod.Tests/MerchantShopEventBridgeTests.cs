using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Bootstrap;
using MerchantDiscountMod.Integration;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Tests;

public sealed class MerchantShopEventBridgeTests
{
    [Fact]
    public void OpeningShopRendersInitialShopState()
    {
        var runtime = new MerchantDiscountRuntime();
        var renderer = new RecordingShopStateRenderer();
        var bridge = CreateBridge(runtime, renderer);

        bridge.OnShopEntered();

        Assert.NotNull(renderer.LastSnapshot);
        Assert.True(renderer.LastSnapshot!.MerchantVisible);
        Assert.True(renderer.LastSnapshot.InventoryAvailable);
    }

    [Fact]
    public void MerchantAndUnaffordablePurchaseAttemptsFlowFromDialogueToPromptToCombatLaunch()
    {
        var runtime = new MerchantDiscountRuntime();
        var dialogue = new RecordingMerchantDialoguePort();
        var prompt = new RecordingDiscountPromptPort();
        var combat = new RecordingShopCombatPort();
        var bridge = CreateBridge(
            runtime,
            new RecordingShopStateRenderer(),
            dialogue,
            prompt,
            combat,
            new RecordingRunStatePersistence());

        bridge.OnShopEntered();

        for (var attempt = 1; attempt <= 3; attempt += 1)
        {
            bridge.OnMerchantSelected();
        }

        for (var attempt = 1; attempt <= 2; attempt += 1)
        {
            bridge.OnUnaffordablePurchaseAttempt();
        }

        Assert.Equal(3, dialogue.ShownLines.Count);
        Assert.Null(prompt.LastPrompt);
        Assert.Null(combat.LastRequest);

        bridge.OnUnaffordablePurchaseAttempt();
        bridge.OnDiscountAccepted();

        Assert.NotNull(prompt.LastPrompt);
        Assert.Equal(DiscountPromptText.EnglishTitle, prompt.LastPrompt!.Title);
        Assert.NotNull(combat.LastRequest);
        Assert.Equal("merchant_discount_placeholder", combat.LastRequest!.EncounterId);
    }

    [Fact]
    public void AcceptingAndResolvingMerchantBattlePersistsRunState()
    {
        var runtime = new MerchantDiscountRuntime();
        var persistence = new RecordingRunStatePersistence();
        var renderer = new RecordingShopStateRenderer();
        var bridge = CreateBridge(
            runtime,
            renderer,
            new RecordingMerchantDialoguePort(),
            new RecordingDiscountPromptPort(),
            new RecordingShopCombatPort(),
            persistence);

        bridge.OnShopEntered();

        for (var attempt = 1; attempt <= 6; attempt += 1)
        {
            bridge.OnUnaffordablePurchaseAttempt();
        }

        bridge.OnDiscountAccepted();
        bridge.OnMerchantBattleResolved(MerchantBattleResult.Victory);
        bridge.OnShopExited();

        Assert.Equal(3, persistence.SaveCallCount);
        Assert.NotNull(renderer.LastSnapshot);
        Assert.False(renderer.LastSnapshot!.MerchantVisible);
        Assert.True(renderer.LastSnapshot.PricesAreZero);
    }

    private static MerchantShopEventBridge CreateBridge(
        MerchantDiscountRuntime runtime,
        RecordingShopStateRenderer renderer,
        RecordingMerchantDialoguePort? dialogue = null,
        RecordingDiscountPromptPort? prompt = null,
        RecordingShopCombatPort? combat = null,
        RecordingRunStatePersistence? persistence = null) =>
        new(
            runtime,
            renderer,
            dialogue ?? new RecordingMerchantDialoguePort(),
            prompt ?? new RecordingDiscountPromptPort(),
            combat ?? new RecordingShopCombatPort(),
            persistence ?? new RecordingRunStatePersistence());

    private sealed class RecordingShopStateRenderer : IShopStateRenderer
    {
        public MerchantDiscountMod.Domain.Shop.ShopStateSnapshot? LastSnapshot { get; private set; }

        public void Render(MerchantDiscountMod.Domain.Shop.ShopStateSnapshot snapshot) => LastSnapshot = snapshot;
    }

    private sealed class RecordingMerchantDialoguePort : IMerchantDialoguePort
    {
        public List<string> ShownLines { get; } = [];

        public void ShowMerchantLine(string line) => ShownLines.Add(line);
    }

    private sealed class RecordingDiscountPromptPort : IDiscountPromptPort
    {
        public MerchantDiscountMod.UI.DiscountPromptRequest? LastPrompt { get; private set; }

        public void ShowPrompt(MerchantDiscountMod.UI.DiscountPromptRequest request) => LastPrompt = request;
    }

    private sealed class RecordingShopCombatPort : IShopCombatPort
    {
        public MerchantBattleRequest? LastRequest { get; private set; }

        public void Launch(MerchantBattleRequest request) => LastRequest = request;
    }

    private sealed class RecordingRunStatePersistence : IRunStatePersistence
    {
        public int SaveCallCount { get; private set; }

        public void SaveRunState() => SaveCallCount += 1;
    }
}
