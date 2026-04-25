using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Domain.Shop;
using MerchantDiscountMod.Integration;
using MerchantDiscountMod.ModEntry;

namespace MerchantDiscountMod.Tests;

public sealed class ReflectionMerchantShopStateRendererTests
{
    [Fact]
    public void VictoryStateHidesMerchantZerosEntryCostsAndRefreshesVisuals()
    {
        var room = new FakeMerchantRoomNode(
            new FakeMerchantEntry(45),
            new FakeMerchantEntry(92));
        var context = new ReflectionMerchantShopContext();
        var renderer = new ReflectionMerchantShopStateRenderer(context);

        context.CaptureMerchantRoom(room);
        renderer.Render(new ShopStateSnapshot(
            MerchantVisible: false,
            InventoryAvailable: true,
            RestockAllowed: false,
            PricesAreZero: true,
            InventoryMode: ShopInventoryMode.FreeCurrentShopAfterMerchantVictory,
            FutureStockSuppressed: false,
            CurrentInventoryPreserved: true,
            PresentationHint: "The merchant fled. Current stock is free."));

        Assert.True(room.MerchantButton.Hidden);
        Assert.All(room.Inventory.Inventory.AllEntries, entry => Assert.Equal(0, entry.Cost));
        Assert.All(room.Inventory.Inventory.AllEntries, entry => Assert.Equal(1, entry.UpdateEntryCallCount));
        Assert.All(room.Inventory.Slots, slot => Assert.Equal(1, slot.UpdateVisualCallCount));
    }

    [Fact]
    public void FutureSuppressionStateHidesMerchantClearsStockWithoutBlockingProceedInput()
    {
        var room = new FakeMerchantRoomNode(
            new FakeMerchantEntry(45),
            new FakeMerchantEntry(92));
        var context = new ReflectionMerchantShopContext();
        var renderer = new ReflectionMerchantShopStateRenderer(context);

        context.CaptureMerchantRoom(room);
        renderer.Render(new ShopStateSnapshot(
            MerchantVisible: false,
            InventoryAvailable: false,
            RestockAllowed: false,
            PricesAreZero: false,
            InventoryMode: ShopInventoryMode.EmptyFutureShopAfterMerchantVictory,
            FutureStockSuppressed: true,
            CurrentInventoryPreserved: false,
            PresentationHint: "Render a merchantless shop with no generated stock for the rest of the run."));

        Assert.True(room.MerchantButton.Hidden);
        Assert.False(room.Inventory.InputBlocked);
        Assert.All(room.Inventory.Inventory.AllEntries, entry => Assert.False(entry.IsStocked));
        Assert.All(room.Inventory.Inventory.AllEntries, entry => Assert.Equal(1, entry.UpdateEntryCallCount));
        Assert.All(room.Inventory.Slots, slot => Assert.Equal(1, slot.UpdateVisualCallCount));
    }

    [Fact]
    public void RenderWithoutCapturedMerchantRoomDoesNothing()
    {
        var context = new ReflectionMerchantShopContext();
        var renderer = new ReflectionMerchantShopStateRenderer(context);

        renderer.Render(new ShopStateSnapshot(
            MerchantVisible: false,
            InventoryAvailable: true,
            RestockAllowed: false,
            PricesAreZero: true,
            InventoryMode: ShopInventoryMode.FreeCurrentShopAfterMerchantVictory,
            FutureStockSuppressed: false,
            CurrentInventoryPreserved: true,
            PresentationHint: "The merchant fled. Current stock is free."));
    }

    [Fact]
    public void LiveBootstrapRendersCapturedMerchantRoomAfterMerchantVictory()
    {
        var room = new FakeMerchantRoomNode(new FakeMerchantEntry(60));
        var bootstrap = MerchantDiscountModEntry.CreateLiveBootstrap();

        bootstrap.ShopContext.CaptureMerchantRoom(room);
        bootstrap.Host.ShopBridge.OnShopEntered();
        for (var attempt = 1; attempt <= 6; attempt += 1)
        {
            bootstrap.Host.ShopBridge.OnUnaffordablePurchaseAttempt();
        }

        bootstrap.Host.Runtime.ConfirmDiscount();
        bootstrap.Host.ShopBridge.OnMerchantBattleResolved(MerchantBattleResult.Victory);

        Assert.True(room.MerchantButton.Hidden);
        Assert.Equal(0, room.Inventory.Inventory.AllEntries.Single().Cost);
        Assert.Equal(1, room.Inventory.Inventory.AllEntries.Single().UpdateEntryCallCount);
        Assert.Equal(1, room.Inventory.Slots.Single().UpdateVisualCallCount);
    }

    [Fact]
    public void LiveBootstrapRendersFutureShopWhenMerchantRoomNodeIsCreatedAfterShopEntry()
    {
        var room = new FakeMerchantRoomNode(new FakeMerchantEntry(60));
        var bootstrap = MerchantDiscountModEntry.CreateLiveBootstrap();

        bootstrap.Host.Runtime.EnterShop();
        for (var attempt = 1; attempt <= 6; attempt += 1)
        {
            bootstrap.Host.Runtime.TryUnaffordablePurchase();
        }

        bootstrap.Host.Runtime.ConfirmDiscount();
        bootstrap.Host.Runtime.ResolveMerchantBattle(MerchantBattleResult.Victory);
        bootstrap.Host.Runtime.LeaveShop();
        bootstrap.Host.Runtime.EnterShop();

        Assert.True(bootstrap.Host.Runtime.GetShopStateSnapshot().FutureStockSuppressed);

        InvokeMerchantRoomCreated(bootstrap, room);

        Assert.True(room.MerchantButton.Hidden);
        Assert.False(room.Inventory.InputBlocked);
        Assert.False(room.Inventory.Inventory.AllEntries.Single().IsStocked);
        Assert.Equal(1, room.Inventory.Inventory.AllEntries.Single().UpdateEntryCallCount);
        Assert.Equal(1, room.Inventory.Slots.Single().UpdateVisualCallCount);
    }

    [Fact]
    public void LiveBootstrapBlocksFutureShopMerchantOpen()
    {
        var room = new FakeMerchantRoomNode(new FakeMerchantEntry(60));
        var bootstrap = MerchantDiscountModEntry.CreateLiveBootstrap();

        EnterFutureSuppressedShop(bootstrap);
        InvokeMerchantRoomCreated(bootstrap, room);

        var shouldRunOriginal = InvokeMerchantOpened(bootstrap, room);

        Assert.False(shouldRunOriginal);
        Assert.True(room.MerchantButton.Hidden);
        Assert.False(room.MerchantButton.Visible);
    }

    private static void InvokeMerchantRoomCreated(MerchantDiscountMod.Integration.MerchantDiscountLiveBootstrap bootstrap, object room)
    {
        InvokeLiveBootstrapHook(bootstrap, "OnMerchantRoomCreated", room);
    }

    private static bool InvokeMerchantOpened(MerchantDiscountMod.Integration.MerchantDiscountLiveBootstrap bootstrap, object room)
    {
        var result = InvokeLiveBootstrapHook(bootstrap, "OnMerchantOpened", room);
        return Assert.IsType<bool>(result);
    }

    private static object? InvokeLiveBootstrapHook(MerchantDiscountMod.Integration.MerchantDiscountLiveBootstrap bootstrap, string methodName, params object?[] args)
    {
        var bootstrapType = typeof(MerchantDiscountMod.Integration.MerchantDiscountLiveBootstrap);
        var activeBootstrapField = bootstrapType.GetField(
            "activeBootstrap",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var method = bootstrapType.GetMethod(
            methodName,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(activeBootstrapField);
        Assert.NotNull(method);

        activeBootstrapField.SetValue(null, bootstrap);
        try
        {
            return method.Invoke(null, args);
        }
        finally
        {
            activeBootstrapField.SetValue(null, null);
        }
    }

    private static void EnterFutureSuppressedShop(MerchantDiscountMod.Integration.MerchantDiscountLiveBootstrap bootstrap)
    {
        bootstrap.Host.Runtime.EnterShop();
        for (var attempt = 1; attempt <= 6; attempt += 1)
        {
            bootstrap.Host.Runtime.TryUnaffordablePurchase();
        }

        bootstrap.Host.Runtime.ConfirmDiscount();
        bootstrap.Host.Runtime.ResolveMerchantBattle(MerchantBattleResult.Victory);
        bootstrap.Host.Runtime.LeaveShop();
        bootstrap.Host.Runtime.EnterShop();
    }

    private sealed class FakeMerchantRoomNode
    {
        private Action? accepted;
        private Action? declined;

        public FakeMerchantRoomNode(params FakeMerchantEntry[] entries)
        {
            Inventory = new FakeMerchantInventoryNode(entries);
        }

        public FakeMerchantButton MerchantButton { get; } = new();

        public FakeMerchantInventoryNode Inventory { get; }

        public void ShowDiscountPrompt(string title, string body, Action onAccepted, Action onDeclined)
        {
            accepted = onAccepted;
            declined = onDeclined;
        }

        public void AcceptPrompt() => accepted?.Invoke();

        public void DeclinePrompt() => declined?.Invoke();
    }

    private sealed class FakeMerchantButton
    {
        public bool Hidden { get; private set; }

        public bool Visible { get; set; } = true;

        public void Hide() => Hidden = true;
    }

    private sealed class FakeMerchantInventoryNode
    {
        public FakeMerchantInventoryNode(FakeMerchantEntry[] entries)
        {
            Inventory = new FakeMerchantInventory(entries);
            Slots = entries.Select(entry => new FakeMerchantSlot(entry)).ToArray();
        }

        public FakeMerchantInventory Inventory { get; }

        public IReadOnlyList<FakeMerchantSlot> Slots { get; }

        public bool InputBlocked { get; private set; }

        public IReadOnlyList<FakeMerchantSlot> GetAllSlots() => Slots;

        public void BlockInput() => InputBlocked = true;
    }

    private sealed class FakeMerchantInventory
    {
        public FakeMerchantInventory(FakeMerchantEntry[] entries)
        {
            AllEntries = entries;
        }

        public IReadOnlyList<FakeMerchantEntry> AllEntries { get; }
    }

    private sealed class FakeMerchantEntry
    {
        private int _cost;

        public FakeMerchantEntry(int cost)
        {
            _cost = cost;
        }

        public int Cost => _cost;

        public bool IsStocked { get; private set; } = true;

        public int UpdateEntryCallCount { get; private set; }

        private void ClearAfterPurchase() => IsStocked = false;

        private void UpdateEntry() => UpdateEntryCallCount += 1;
    }

    private sealed class FakeMerchantSlot
    {
        public FakeMerchantSlot(FakeMerchantEntry entry)
        {
            Entry = entry;
        }

        public FakeMerchantEntry Entry { get; }

        public int UpdateVisualCallCount { get; private set; }

        private void UpdateVisual() => UpdateVisualCallCount += 1;
    }
}
