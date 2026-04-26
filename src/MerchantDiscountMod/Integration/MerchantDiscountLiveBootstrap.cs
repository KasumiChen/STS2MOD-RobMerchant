using System.Reflection;
using System.Threading.Tasks;
using MerchantDiscountMod.Combat;
using MerchantDiscountMod.ModEntry;

namespace MerchantDiscountMod.Integration;

public sealed class MerchantDiscountLiveBootstrap
{
    private static readonly object SyncRoot = new();
    private static MerchantDiscountLiveBootstrap? activeBootstrap;

    public MerchantDiscountLiveBootstrap(MerchantDiscountOfflineHost host, ReflectionMerchantShopContext shopContext)
    {
        Host = host;
        ShopContext = shopContext;
        BindingPlan = Sts2BindingPlan.CreateConfirmed();
        VerifiedTargets = BindingPlan.DocumentedTargets
            .Where(target => target.MethodName is "EnterInternal" or "Resume" or "Exit" or "OnMerchantOpened" or "Create" or "AfterRoomIsLoaded" or "OnTryPurchaseWrapper" or "InvokePurchaseFailed" or "RestockAfterPurchase")
            .ToArray();
    }

    public MerchantDiscountOfflineHost Host { get; }

    public ReflectionMerchantShopContext ShopContext { get; }

    public Sts2BindingPlan BindingPlan { get; }

    public string HarmonyId => $"{Host.ModId}.Harmony";

    public IReadOnlyList<VerifiedHookTarget> VerifiedTargets { get; }

    public void ApplyHarmonyPatches()
    {
        lock (SyncRoot)
        {
            MerchantDiscountDiagnostics.Info("Applying Harmony patches.");
            activeBootstrap = this;

            var harmonyAssembly = Assembly.Load("0Harmony");
            var harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", throwOnError: true)!;
            var harmonyMethodType = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod", throwOnError: true)!;
            var hasAnyPatches = harmonyType.GetMethod("HasAnyPatches", BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException(harmonyType.FullName, "HasAnyPatches");

            if ((bool)(hasAnyPatches.Invoke(null, [HarmonyId]) ?? false))
            {
                MerchantDiscountDiagnostics.Info("Harmony patches already applied.");
                return;
            }

            var harmony = Activator.CreateInstance(harmonyType, HarmonyId)
                ?? throw new InvalidOperationException($"Unable to create {harmonyType.FullName}.");

            PatchPostfix(harmony, harmonyType, harmonyMethodType, VerifiedTargets.Single(target => target.MethodName == "EnterInternal"), nameof(OnMerchantRoomEntered));
            PatchPrefix(
                harmony,
                harmonyType,
                harmonyMethodType,
                VerifiedTargets.Single(target => target.DeclaringTypeName == "MegaCrit.Sts2.Core.Rooms.MerchantRoom" && target.MethodName == "Resume"),
                nameof(OnMerchantRoomResume));
            PatchPostfix(
                harmony,
                harmonyType,
                harmonyMethodType,
                VerifiedTargets.Single(target => target.DeclaringTypeName == "MegaCrit.Sts2.Core.Rooms.MerchantRoom" && target.MethodName == "Exit"),
                nameof(OnMerchantRoomExited));
            PatchPrefix(harmony, harmonyType, harmonyMethodType, VerifiedTargets.Single(target => target.MethodName == "OnMerchantOpened"), nameof(OnMerchantOpened));
            PatchPostfix(harmony, harmonyType, harmonyMethodType, VerifiedTargets.Single(target => target.MethodName == "Create"), nameof(OnMerchantRoomCreated));
            PatchPostfix(harmony, harmonyType, harmonyMethodType, VerifiedTargets.Single(target => target.MethodName == "AfterRoomIsLoaded"), nameof(OnMerchantRoomLoaded));
            PatchPrefix(harmony, harmonyType, harmonyMethodType, VerifiedTargets.Single(target => target.MethodName == "OnTryPurchaseWrapper"), nameof(OnMerchantEntryPurchaseAttempt));
            PatchPostfix(harmony, harmonyType, harmonyMethodType, VerifiedTargets.Single(target => target.MethodName == "InvokePurchaseFailed"), nameof(OnMerchantEntryPurchaseFailed));
            foreach (var restockTarget in VerifiedTargets.Where(target => target.MethodName == "RestockAfterPurchase"))
            {
                PatchPrefix(harmony, harmonyType, harmonyMethodType, restockTarget, nameof(ShouldAllowMerchantEntryRestockAfterPurchase));
            }
            PatchPostfix(
                harmony,
                harmonyType,
                harmonyMethodType,
                VerifiedTargets.Single(target => target.DeclaringTypeName == "MegaCrit.Sts2.Core.Rooms.CombatRoom" && target.MethodName == "Exit"),
                nameof(OnCombatRoomExited));
            MerchantDiscountDiagnostics.Info("Harmony patches applied.");
        }
    }

    private static void PatchPostfix(object harmony, Type harmonyType, Type harmonyMethodType, VerifiedHookTarget target, string postfixMethodName) =>
        Patch(harmony, harmonyType, harmonyMethodType, target, prefixMethodName: null, postfixMethodName);

    private static void PatchPrefix(object harmony, Type harmonyType, Type harmonyMethodType, VerifiedHookTarget target, string prefixMethodName) =>
        Patch(harmony, harmonyType, harmonyMethodType, target, prefixMethodName, postfixMethodName: null);

    private static void Patch(
        object harmony,
        Type harmonyType,
        Type harmonyMethodType,
        VerifiedHookTarget target,
        string? prefixMethodName,
        string? postfixMethodName)
    {
        var original = target.ResolveMethod()
            ?? throw new InvalidOperationException($"Unable to resolve verified STS2 hook target: {target}");
        var prefix = prefixMethodName is null
            ? null
            : typeof(MerchantDiscountLiveBootstrap).GetMethod(prefixMethodName, BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(typeof(MerchantDiscountLiveBootstrap).FullName, prefixMethodName);
        var postfix = postfixMethodName is null
            ? null
            : typeof(MerchantDiscountLiveBootstrap).GetMethod(postfixMethodName, BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(typeof(MerchantDiscountLiveBootstrap).FullName, postfixMethodName);
        var patchMethod = harmonyType.GetMethod("Patch", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new MissingMethodException(harmonyType.FullName, "Patch");
        var harmonyPrefix = prefix is null
            ? null
            : Activator.CreateInstance(harmonyMethodType, prefix)
                ?? throw new InvalidOperationException($"Unable to create {harmonyMethodType.FullName}.");
        var harmonyPostfix = postfix is null
            ? null
            : Activator.CreateInstance(harmonyMethodType, postfix)
                ?? throw new InvalidOperationException($"Unable to create {harmonyMethodType.FullName}.");

        if (harmonyPrefix is null && harmonyPostfix is null)
        {
            throw new InvalidOperationException($"No Harmony patch method supplied for {target}.");
        }

        MerchantDiscountDiagnostics.Info(
            $"Preparing patch target={target} resolved={original.DeclaringType?.FullName}.{original.Name} isAbstract={original.IsAbstract} isVirtual={original.IsVirtual}.");
        patchMethod.Invoke(harmony, [original, harmonyPrefix, harmonyPostfix, null, null]);
        MerchantDiscountDiagnostics.Info($"Patched {target}.");
    }

    private static void OnMerchantRoomEntered(object? runState)
    {
        MerchantDiscountDiagnostics.Info($"Merchant room entered runState={runState?.GetType().FullName ?? "<null>"}.");
        activeBootstrap?.ShopContext.CaptureRunState(runState);
        activeBootstrap?.Host.ShopBridge.OnShopEntered();
    }

    private static bool OnMerchantRoomResume(object? __instance, object? __0, object? __1, ref Task __result)
    {
        var bootstrap = activeBootstrap;
        if (__instance is null || bootstrap is null || !bootstrap.ShopContext.ConsumeMerchantCombatResume(__0))
        {
            return true;
        }

        __result = ResumeMerchantRoomAfterMerchantCombat(bootstrap, __instance, __1);
        return false;
    }

    private static async Task ResumeMerchantRoomAfterMerchantCombat(
        MerchantDiscountLiveBootstrap bootstrap,
        object merchantRoom,
        object? runState)
    {
        if (runState is null)
        {
            throw new InvalidOperationException("Cannot resume merchant room after combat without a run state.");
        }

        MerchantDiscountDiagnostics.Info("Resuming merchant room after merchant combat victory.");
        bootstrap.ShopContext.CaptureRunState(runState);

        var enterResult = ReflectionMemberAccess.InvokeRequired(merchantRoom, "EnterInternal", runState, false);
        if (enterResult is not Task enterTask)
        {
            throw new InvalidOperationException(
                $"{merchantRoom.GetType().FullName}.EnterInternal did not return a Task.");
        }

        await enterTask;

        var merchantRoomNode = bootstrap.ShopContext.CurrentMerchantRoom
            ?? throw new InvalidOperationException("Merchant room resume did not produce an NMerchantRoom instance.");
        var openInventoryResult = ReflectionMemberAccess.InvokeRequired(merchantRoomNode, "OpenInventory");
        if (openInventoryResult is Task openInventoryTask)
        {
            await openInventoryTask;
        }

        bootstrap.Host.ShopBridge.RefreshShopPresentation();
        MerchantDiscountDiagnostics.Info("Merchant room resumed into free inventory after merchant combat victory.");
    }

    internal static async Task LaunchMerchantCombatFromSynchronizedAction(object? runState)
    {
        var bootstrap = activeBootstrap
            ?? throw new InvalidOperationException("Cannot launch synchronized merchant combat before the mod is initialized.");
        if (runState is null)
        {
            throw new InvalidOperationException("Cannot launch synchronized merchant combat without a run state.");
        }

        if (bootstrap.ShopContext.MerchantCombatLaunchInProgress)
        {
            MerchantDiscountDiagnostics.Warn("Ignoring duplicate synchronized merchant combat launch request.");
            return;
        }

        MerchantDiscountDiagnostics.Info("Launching synchronized multiplayer merchant combat.");
        bootstrap.ShopContext.CaptureRunState(runState);
        bootstrap.Host.ShopBridge.OnSynchronizedMerchantBattleStarted();

        var combatRoom = ReflectionShopCombatPort.CreateSts2CombatRoom(MerchantBattleRequest.Placeholder(), runState)
            ?? throw new InvalidOperationException("Could not create synchronized merchant combat room.");
        bootstrap.ShopContext.CaptureMerchantCombatRoom(combatRoom);

        var transitionTask = ReflectionShopCombatPort.EnterSts2CombatRoomAsync(combatRoom, fadeToBlack: true)
            ?? throw new InvalidOperationException("Could not enter synchronized merchant combat room.");
        await transitionTask;

        MerchantDiscountDiagnostics.Info("Synchronized multiplayer merchant combat transition completed.");
    }

    private static void OnMerchantRoomExited()
    {
        MerchantDiscountDiagnostics.Info("Merchant room exited.");
        activeBootstrap?.ShopContext.ClearMerchantRoom();
        activeBootstrap?.Host.ShopBridge.OnShopExited();
    }

    private static bool OnMerchantOpened(object? __instance)
    {
        var bootstrap = activeBootstrap;
        MerchantDiscountDiagnostics.Info($"Merchant opened room={__instance?.GetType().FullName ?? "<null>"}.");
        bootstrap?.ShopContext.CaptureMerchantRoom(__instance);
        if (bootstrap is null)
        {
            return true;
        }

        if (!bootstrap.Host.ShopBridge.CanOpenMerchant())
        {
            MerchantDiscountDiagnostics.Info("Blocked merchant open because this shop no longer has an active merchant.");
            bootstrap.Host.ShopBridge.RefreshShopPresentation();
            return false;
        }

        bootstrap.Host.ShopBridge.OnMerchantSelected();
        return true;
    }

    private static void OnMerchantRoomCreated(object? __result)
    {
        MerchantDiscountDiagnostics.Info($"Merchant room node created result={__result?.GetType().FullName ?? "<null>"}.");
        activeBootstrap?.ShopContext.CaptureMerchantRoom(__result);
        activeBootstrap?.Host.ShopBridge.RefreshShopPresentation();
    }

    private static void OnMerchantRoomLoaded(object? __instance)
    {
        MerchantDiscountDiagnostics.Info($"Merchant room loaded instance={__instance?.GetType().FullName ?? "<null>"}.");
        activeBootstrap?.ShopContext.CaptureMerchantRoom(__instance);
        activeBootstrap?.Host.ShopBridge.RefreshShopPresentation();
    }

    private static void OnMerchantEntryPurchaseAttempt(object? __instance, bool ignoreCost)
    {
        if (__instance is null)
        {
            MerchantDiscountDiagnostics.Warn("Purchase attempt hook received null merchant entry.");
            return;
        }

        var isStocked = ReflectionMemberAccess.GetPropertyValue(__instance, "IsStocked");
        var enoughGold = ReflectionMemberAccess.GetPropertyValue(__instance, "EnoughGold");
        MerchantDiscountDiagnostics.Info(
            $"Purchase attempt entry={__instance.GetType().FullName} ignoreCost={ignoreCost} isStocked={isStocked ?? "<unknown>"} enoughGold={enoughGold ?? "<unknown>"}.");

        if (ignoreCost)
        {
            return;
        }

        if (isStocked is false)
        {
            return;
        }

        if (enoughGold is false)
        {
            MerchantDiscountDiagnostics.Info("Purchase attempt prefix saw insufficient gold; waiting for native purchase-failure event.");
        }
    }

    private static void OnMerchantEntryPurchaseFailed(object? __instance, object? status)
    {
        MerchantDiscountDiagnostics.Info(
            $"Purchase failed entry={__instance?.GetType().FullName ?? "<null>"} status={status ?? "<null>"}.");

        if (status?.ToString() != "FailureGold")
        {
            return;
        }

        MerchantDiscountDiagnostics.Info("Dispatching unaffordable purchase pressure attempt from purchase-failure event.");
        activeBootstrap?.Host.ShopBridge.OnUnaffordablePurchaseAttempt();
    }

    private static bool ShouldAllowMerchantEntryRestockAfterPurchase() =>
        activeBootstrap?.Host.Runtime.ShopInventoryState.RestockAllowed ?? true;

    private static void OnCombatRoomExited(object? __instance)
    {
        var bootstrap = activeBootstrap;
        if (__instance is null || bootstrap is null || !ReferenceEquals(__instance, bootstrap.ShopContext.CurrentMerchantCombatRoom))
        {
            return;
        }

        MerchantDiscountDiagnostics.Info("Merchant combat room exited; resolving merchant battle victory.");
        bootstrap.ShopContext.MarkMerchantCombatResumePending(__instance);
        bootstrap.ShopContext.ClearMerchantCombatRoom();
        bootstrap.Host.ShopBridge.OnMerchantBattleResolved(MerchantBattleResult.Victory);
    }
}
