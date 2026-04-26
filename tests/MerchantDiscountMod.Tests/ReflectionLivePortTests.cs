using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Integration;
using MerchantDiscountMod.Persistence;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Tests;

public sealed class ReflectionLivePortTests
{
    [Fact]
    public void DialoguePortShowsLineOnCapturedRoomAndRecordsFallbackLine()
    {
        var room = new FakeInteractiveRoom();
        var context = new ReflectionMerchantShopContext();
        var port = new ReflectionMerchantDialoguePort(context);

        context.CaptureMerchantRoom(room);
        port.ShowMerchantLine("Your persistence is expensive.");

        Assert.Equal("Your persistence is expensive.", room.LastMerchantLine);
        Assert.Equal("Your persistence is expensive.", context.LastMerchantDialogueLine);
    }

    [Fact]
    public void PromptPortShowsPromptOnCapturedRoomAndForwardsAcceptedAndDeclinedCallbacks()
    {
        var accepted = 0;
        var declined = 0;
        var room = new FakeInteractiveRoom();
        var context = new ReflectionMerchantShopContext();
        var port = new ReflectionDiscountPromptPort(context);
        port.Connect(
            onAccepted: () => accepted += 1,
            onDeclined: () => declined += 1);

        context.CaptureMerchantRoom(room);
        port.ShowPrompt(new DiscountPromptRequest("Demand a discount?", "This may turn violent."));
        Assert.Equal("Demand a discount?", context.PendingPrompt?.Title);

        room.AcceptPrompt();
        room.DeclinePrompt();

        Assert.Equal("Demand a discount?", room.LastPromptTitle);
        Assert.Equal("This may turn violent.", room.LastPromptBody);
        Assert.Equal(1, accepted);
        Assert.Equal(1, declined);
    }

    [Fact]
    public void PromptPortUsesNativePresenterWhenCapturedRoomHasNoPromptShim()
    {
        var accepted = 0;
        var declined = 0;
        var room = new object();
        var context = new ReflectionMerchantShopContext();
        var presenter = new RecordingPromptPresenter();
        var port = new ReflectionDiscountPromptPort(context, presenter);
        port.Connect(
            onAccepted: () => accepted += 1,
            onDeclined: () => declined += 1);

        context.CaptureMerchantRoom(room);
        port.ShowPrompt(new DiscountPromptRequest("Demand a discount?", "This may turn violent."));

        Assert.Equal("Demand a discount?", context.PendingPrompt?.Title);
        Assert.Equal("Demand a discount?", presenter.LastRequest?.Title);

        presenter.AcceptPrompt();

        Assert.Null(context.PendingPrompt);
        Assert.Equal(1, accepted);
        Assert.Equal(0, declined);
    }

    [Fact]
    public void PromptPortThrowsWhenNativePresenterCannotShow()
    {
        var accepted = 0;
        var declined = 0;
        var context = new ReflectionMerchantShopContext();
        var port = new ReflectionDiscountPromptPort(context, new FailingPromptPresenter());
        port.Connect(
            onAccepted: () => accepted += 1,
            onDeclined: () => declined += 1);

        context.CaptureMerchantRoom(new object());
        var exception = Assert.Throws<InvalidOperationException>(
            () => port.ShowPrompt(new DiscountPromptRequest("Demand a discount?", "This may turn violent.")));

        Assert.Contains("Discount prompt could not be shown", exception.Message);
        Assert.Null(context.PendingPrompt);
        Assert.Equal(0, accepted);
        Assert.Equal(0, declined);
    }

    [Fact]
    public void ReflectionMemberAccessInvokesParameterlessStaticFactories()
    {
        var memberAccessType = typeof(ReflectionDiscountPromptPort).Assembly
            .GetType("MerchantDiscountMod.Integration.ReflectionMemberAccess", throwOnError: true)!;
        var invokeStaticParameterless = memberAccessType.GetMethod(
            "InvokeStaticParameterless",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = invokeStaticParameterless?.Invoke(null, [typeof(FakeStaticFactory), nameof(FakeStaticFactory.Create)]);

        var popup = Assert.IsType<FakeStaticFactory>(result);
        Assert.Equal("created", popup.Source);
    }

    [Fact]
    public void ReflectionMemberAccessCreatesTypedSingleParameterActions()
    {
        var memberAccessType = typeof(ReflectionDiscountPromptPort).Assembly
            .GetType("MerchantDiscountMod.Integration.ReflectionMemberAccess", throwOnError: true)!;
        var createAction = memberAccessType.GetMethod(
            "CreateSingleParameterAction",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var target = new FakeActionTarget();

        var action = createAction?.Invoke(null, [typeof(FakeButton), target, "Press"]);

        Assert.IsType<Action<FakeButton>>(action);
        ((Action<FakeButton>)action!).Invoke(new FakeButton());
        Assert.Equal(1, target.PressCount);
    }

    [Fact]
    public void ReflectionMemberAccessCreatesTypedSingleParameterActionsThroughReflectionAdapter()
    {
        var memberAccessType = typeof(ReflectionDiscountPromptPort).Assembly
            .GetType("MerchantDiscountMod.Integration.ReflectionMemberAccess", throwOnError: true)!;
        var createAction = memberAccessType.GetMethod(
            "CreateSingleParameterAction",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var target = new FakeActionTarget();

        var action = createAction?.Invoke(null, [typeof(FakeButton), target, "PressViaReflectionAdapter"]);

        Assert.IsType<Action<FakeButton>>(action);
        ((Action<FakeButton>)action!).Invoke(new FakeButton());
        Assert.Equal(1, target.PressCount);
    }

    [Fact]
    public void CombatPortPushesFakeMerchantCombatRoomOntoCapturedRunState()
    {
        var runState = new FakeRunState();
        var context = new ReflectionMerchantShopContext();
        var port = new ReflectionShopCombatPort(
            context,
            (_, capturedRunState) => new FakeCombatRoom(capturedRunState));

        context.CaptureRunState(runState);
        port.Launch(MerchantBattleRequest.Placeholder());

        var pushedRoom = Assert.IsType<FakeCombatRoom>(runState.PushedRooms.Single());
        Assert.Same(runState, pushedRoom.RunState);
        Assert.Equal("merchant_discount_placeholder", context.LastCombatRequest?.EncounterId);
        Assert.Same(pushedRoom, context.CurrentMerchantCombatRoom);
    }

    [Fact]
    public void CombatPortRequestsSynchronizedLaunchForMultiplayerRuns()
    {
        var runState = new FakeRunState();
        var context = new ReflectionMerchantShopContext();
        var directFactoryCalls = 0;
        MerchantBattleRequest? synchronizedRequest = null;
        object? synchronizedRunState = null;
        var port = new ReflectionShopCombatPort(
            context,
            (_, _) =>
            {
                directFactoryCalls += 1;
                return new FakeCombatRoom(runState);
            },
            (_, _) => throw new InvalidOperationException("Direct combat launch should not be used in multiplayer."),
            isMultiplayerRun: () => true,
            multiplayerCombatLauncher: (request, capturedRunState) =>
            {
                synchronizedRequest = request;
                synchronizedRunState = capturedRunState;
                return true;
            });

        context.CaptureRunState(runState);
        port.Launch(MerchantBattleRequest.Placeholder());

        Assert.Equal(0, directFactoryCalls);
        Assert.Equal("merchant_discount_placeholder", synchronizedRequest?.EncounterId);
        Assert.Same(runState, synchronizedRunState);
        Assert.Null(context.CurrentMerchantCombatRoom);
    }

    [Fact]
    public void CombatPortResolvesMutableEncounterThroughModelDbAccessor()
    {
        var combatPortType = typeof(ReflectionShopCombatPort);
        var createMutableModel = combatPortType.GetMethod(
            "CreateMutableModel",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        var encounter = createMutableModel?.Invoke(
            null,
            [typeof(FakeModelDb), "Encounter", typeof(FakeCanonicalEncounter)]);

        var mutableEncounter = Assert.IsType<FakeMutableEncounter>(encounter);
        Assert.Same(FakeCanonicalEncounter.Instance, mutableEncounter.Source);
        Assert.Equal(typeof(FakeCanonicalEncounter), FakeModelDb.LastRequestedType);
    }

    [Fact]
    public void CombatPortUsesRunManagerNestedRoomTransition()
    {
        var combatPortType = typeof(ReflectionShopCombatPort);
        var invokeTransition = combatPortType.GetMethod(
            "InvokeEnterRoomWithoutExitingCurrentRoom",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var runManager = new FakeRunManager();
        var combatRoom = new FakeAbstractRoom();

        var launched = invokeTransition?.Invoke(null, [runManager, combatRoom, true]);

        Assert.Equal(true, launched);
        Assert.Same(combatRoom, runManager.LastRoom);
        Assert.True(runManager.LastFadeToBlack);
    }

    [Fact]
    public async Task MerchantCombatResumeReentersMerchantRoomWithoutStackRestorationMode()
    {
        var bootstrap = MerchantDiscountMod.ModEntry.MerchantDiscountModEntry.CreateLiveBootstrap();
        var resumeMethod = typeof(MerchantDiscountLiveBootstrap).GetMethod(
            "ResumeMerchantRoomAfterMerchantCombat",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var merchantRoom = new FakeMerchantRoom();
        var merchantRoomNode = new FakeResumableMerchantRoomNode();
        var runState = new object();

        bootstrap.ShopContext.CaptureMerchantRoom(merchantRoomNode);

        var task = Assert.IsAssignableFrom<Task>(resumeMethod?.Invoke(null, [bootstrap, merchantRoom, runState]));
        await task;

        Assert.Equal(false, merchantRoom.LastIsRestoringRoomStackBase);
        Assert.Equal(1, merchantRoomNode.OpenInventoryCallCount);
    }

    [Fact]
    public void ShopContextConsumesOnlyMatchingMerchantCombatResume()
    {
        var context = new ReflectionMerchantShopContext();
        var merchantCombatRoom = new object();
        var unrelatedRoom = new object();

        context.MarkMerchantCombatResumePending(merchantCombatRoom);

        Assert.False(context.ConsumeMerchantCombatResume(unrelatedRoom));
        Assert.True(context.ConsumeMerchantCombatResume(merchantCombatRoom));
        Assert.False(context.ConsumeMerchantCombatResume(merchantCombatRoom));
    }

    [Fact]
    public void ShopContextReportsWhenMerchantCombatLaunchIsAlreadyInProgress()
    {
        var context = new ReflectionMerchantShopContext();

        Assert.False(context.MerchantCombatLaunchInProgress);

        context.CaptureMerchantCombatRoom(new object());

        Assert.True(context.MerchantCombatLaunchInProgress);

        context.ClearMerchantCombatRoom();

        Assert.False(context.MerchantCombatLaunchInProgress);
    }

    [Fact]
    public void PersistencePortStoresLatestRuntimeSnapshotInLiveContext()
    {
        var context = new ReflectionMerchantShopContext();
        var snapshot = new MerchantRunStateSnapshot
        {
            MerchantChallengePending = true
        };
        var persistence = new ReflectionRunStatePersistence(
            context,
            loadSnapshot: () => snapshot,
            saveSnapshot: _ => { });

        persistence.SaveRunState();

        Assert.True(context.SavedRunStateSnapshot?.MerchantChallengePending);
    }

    private sealed class FakeInteractiveRoom
    {
        private Action? accepted;
        private Action? declined;

        public string? LastMerchantLine { get; private set; }

        public string? LastPromptTitle { get; private set; }

        public string? LastPromptBody { get; private set; }

        public void ShowMerchantLine(string line)
        {
            LastMerchantLine = line;
        }

        public void ShowDiscountPrompt(string title, string body, Action onAccepted, Action onDeclined)
        {
            LastPromptTitle = title;
            LastPromptBody = body;
            accepted = onAccepted;
            declined = onDeclined;
        }

        public void AcceptPrompt() => accepted?.Invoke();

        public void DeclinePrompt() => declined?.Invoke();
    }

    private sealed class RecordingPromptPresenter : IReflectionDiscountPromptPresenter
    {
        private Action? accepted;
        private Action? declined;

        public DiscountPromptRequest? LastRequest { get; private set; }

        public bool TryShow(DiscountPromptRequest request, Action onAccepted, Action onDeclined)
        {
            LastRequest = request;
            accepted = onAccepted;
            declined = onDeclined;
            return true;
        }

        public void AcceptPrompt() => accepted?.Invoke();

        public void DeclinePrompt() => declined?.Invoke();
    }

    private sealed class FailingPromptPresenter : IReflectionDiscountPromptPresenter
    {
        public bool TryShow(DiscountPromptRequest request, Action onAccepted, Action onDeclined) => false;
    }

    private sealed class FakeRunState
    {
        public List<object> PushedRooms { get; } = [];

        public void PushRoom(object room) => PushedRooms.Add(room);
    }

    private sealed class FakeCombatRoom
    {
        public FakeCombatRoom(object? runState)
        {
            RunState = runState;
        }

        public object? RunState { get; }
    }

    private sealed class FakeRunManager
    {
        public object? LastRoom { get; private set; }

        public bool LastFadeToBlack { get; private set; }

        public Task EnterRoomWithoutExitingCurrentRoom(FakeAbstractRoom room, bool fadeToBlack)
        {
            LastRoom = room;
            LastFadeToBlack = fadeToBlack;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAbstractRoom
    {
    }

    private sealed class FakeMerchantRoom
    {
        public bool? LastIsRestoringRoomStackBase { get; private set; }

        public Task EnterInternal(object runState, bool isRestoringRoomStackBase)
        {
            _ = runState;
            LastIsRestoringRoomStackBase = isRestoringRoomStackBase;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeResumableMerchantRoomNode
    {
        public int OpenInventoryCallCount { get; private set; }

        public void OpenInventory() => OpenInventoryCallCount += 1;
    }

    private sealed class FakeModelDb
    {
        public static Type? LastRequestedType { get; private set; }

        public static T Encounter<T>()
            where T : class
        {
            LastRequestedType = typeof(T);
            return (T)(object)FakeCanonicalEncounter.Instance;
        }
    }

    private sealed class FakeCanonicalEncounter
    {
        public static FakeCanonicalEncounter Instance { get; } = new();

        public FakeMutableEncounter ToMutable() => new(this);
    }

    private sealed class FakeMutableEncounter
    {
        public FakeMutableEncounter(FakeCanonicalEncounter source)
        {
            Source = source;
        }

        public FakeCanonicalEncounter Source { get; }
    }

    private sealed class FakeStaticFactory
    {
        private FakeStaticFactory(string source)
        {
            Source = source;
        }

        public string Source { get; }

        public static FakeStaticFactory Create() => new("created");
    }

    private sealed class FakeButton
    {
    }

    private sealed class FakeActionTarget
    {
        public int PressCount { get; private set; }

        private void Press(object? _)
        {
            PressCount += 1;
        }

        private void PressViaReflectionAdapter()
        {
            PressCount += 1;
        }
    }
}
