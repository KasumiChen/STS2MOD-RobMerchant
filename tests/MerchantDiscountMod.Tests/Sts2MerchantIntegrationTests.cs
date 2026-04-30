using MerchantDiscountMod.Integration;
using MerchantDiscountMod.ModEntry;

namespace MerchantDiscountMod.Tests;

public sealed class Sts2MerchantIntegrationTests
{
    [Fact]
    public void LiveBootstrapDocumentsVerifiedMerchantHookTargets()
    {
        var bootstrap = MerchantDiscountModEntry.CreateLiveBootstrap();

        Assert.Equal(MerchantDiscountModInfo.ModId, bootstrap.Host.ModId);
        Assert.Equal($"{MerchantDiscountModInfo.ModId}.Harmony", bootstrap.HarmonyId);
        Assert.NotEmpty(bootstrap.VerifiedTargets);
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Rooms.MerchantRoom.EnterInternal(MegaCrit.Sts2.Core.Runs.IRunState, System.Boolean)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Rooms.MerchantRoom.Resume(MegaCrit.Sts2.Core.Rooms.AbstractRoom, MegaCrit.Sts2.Core.Runs.IRunState)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Rooms.MerchantRoom.Exit(MegaCrit.Sts2.Core.Runs.IRunState)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom.OnMerchantOpened(MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantButton)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom.Create(MegaCrit.Sts2.Core.Rooms.MerchantRoom, System.Collections.Generic.IReadOnlyList`1<MegaCrit.Sts2.Core.Entities.Players.Player>)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom.AfterRoomIsLoaded()");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Entities.Merchant.MerchantEntry.OnTryPurchaseWrapper(MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory, System.Boolean)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Entities.Merchant.MerchantEntry.InvokePurchaseFailed(MegaCrit.Sts2.Core.Entities.Merchant.PurchaseStatus)");
        Assert.DoesNotContain(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Entities.Merchant.MerchantEntry.RestockAfterPurchase(MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Entities.Merchant.MerchantCardEntry.RestockAfterPurchase(MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Entities.Merchant.MerchantRelicEntry.RestockAfterPurchase(MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Entities.Merchant.MerchantPotionEntry.RestockAfterPurchase(MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Entities.Merchant.MerchantCardRemovalEntry.RestockAfterPurchase(MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.ToString() == "MegaCrit.Sts2.Core.Rooms.CombatRoom.Exit(MegaCrit.Sts2.Core.Runs.IRunState)");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.MethodName == "StartNewSingleplayerRun");
        Assert.Contains(bootstrap.VerifiedTargets, target => target.MethodName == "StartNewMultiplayerRun");
    }

    [Fact]
    public void VerifiedHookTargetResolvesMethodsFromLoadedAssemblies()
    {
        var target = new VerifiedHookTarget(
            typeof(SampleHookTarget).FullName!,
            nameof(SampleHookTarget.SampleMethod),
            typeof(string).FullName!);

        var method = target.ResolveMethod();

        Assert.NotNull(method);
        Assert.Equal(nameof(SampleHookTarget.SampleMethod), method!.Name);
    }

    [Fact]
    public void VerifiedHookTargetResolvesGenericParameterMethodsFromLoadedAssemblies()
    {
        var target = new VerifiedHookTarget(
            typeof(SampleHookTarget).FullName!,
            nameof(SampleHookTarget.SampleGenericMethod),
            "System.Collections.Generic.IReadOnlyList`1<System.String>");

        var method = target.ResolveMethod();

        Assert.NotNull(method);
        Assert.Equal(nameof(SampleHookTarget.SampleGenericMethod), method!.Name);
    }

    [Fact]
    public void BindingPlanDocumentsConfirmedMerchantTypes()
    {
        var plan = Sts2BindingPlan.CreateConfirmed();

        Assert.Equal("MegaCrit.Sts2.Core.Rooms.MerchantRoom", plan.MerchantRoomType);
        Assert.Equal("MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom", plan.MerchantRoomNodeType);
        Assert.Equal("MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantButton", plan.MerchantButtonNodeType);
        Assert.Equal("MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantInventory", plan.MerchantInventoryNodeType);
        Assert.Equal("MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory", plan.MerchantInventoryModelType);
        Assert.Contains(plan.DocumentedTargets, target => target.MethodName == "OnMerchantOpened");
        Assert.Contains(plan.DocumentedTargets, target => target.MethodName == "AfterRoomIsLoaded");
        Assert.Contains(plan.DocumentedTargets, target => target.MethodName == "EnterInternal");
        Assert.Contains(plan.DocumentedTargets, target => target.DeclaringTypeName == "MegaCrit.Sts2.Core.Rooms.MerchantRoom" && target.MethodName == "Resume");
        Assert.Contains(plan.DocumentedTargets, target => target.MethodName == "OnTryPurchaseWrapper");
        Assert.Contains(plan.DocumentedTargets, target => target.MethodName == "InvokePurchaseFailed");
        Assert.Contains(plan.DocumentedTargets, target => target.MethodName == "RestockAfterPurchase");
        Assert.Contains(plan.DocumentedTargets, target => target.DeclaringTypeName == "MegaCrit.Sts2.Core.Rooms.CombatRoom" && target.MethodName == "Exit");
        Assert.Contains(plan.DocumentedTargets, target => target.MethodName == "StartNewSingleplayerRun");
        Assert.Contains(plan.DocumentedTargets, target => target.MethodName == "StartNewMultiplayerRun");
    }

    private sealed class SampleHookTarget
    {
        public void SampleMethod(string value)
        {
        }

        public void SampleGenericMethod(IReadOnlyList<string> values)
        {
        }
    }
}
