namespace MerchantDiscountMod.Integration;

public sealed class Sts2BindingPlan
{
    public string MerchantRoomType { get; init; } = string.Empty;

    public string MerchantRoomNodeType { get; init; } = string.Empty;

    public string MerchantButtonNodeType { get; init; } = string.Empty;

    public string MerchantInventoryNodeType { get; init; } = string.Empty;

    public string MerchantInventoryModelType { get; init; } = string.Empty;

    public string MerchantEntryType { get; init; } = string.Empty;

    public string MerchantDialogueType { get; init; } = string.Empty;

    public string ModInitializerAttributeType { get; init; } = string.Empty;

    public string ModManagerType { get; init; } = string.Empty;

    public string ModHelperType { get; init; } = string.Empty;

    public IReadOnlyList<VerifiedHookTarget> DocumentedTargets { get; init; } = [];

    public static Sts2BindingPlan CreateConfirmed()
    {
        return new Sts2BindingPlan
        {
            MerchantRoomType = "MegaCrit.Sts2.Core.Rooms.MerchantRoom",
            MerchantRoomNodeType = "MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom",
            MerchantButtonNodeType = "MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantButton",
            MerchantInventoryNodeType = "MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantInventory",
            MerchantInventoryModelType = "MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory",
            MerchantEntryType = "MegaCrit.Sts2.Core.Entities.Merchant.MerchantEntry",
            MerchantDialogueType = "MegaCrit.Sts2.Core.Entities.Merchant.MerchantDialogueSet",
            ModInitializerAttributeType = "MegaCrit.Sts2.Core.Modding.ModInitializerAttribute",
            ModManagerType = "MegaCrit.Sts2.Core.Modding.ModManager",
            ModHelperType = "MegaCrit.Sts2.Core.Modding.ModHelper",
            DocumentedTargets =
            [
                new(
                    "MegaCrit.Sts2.Core.Rooms.MerchantRoom",
                    "EnterInternal",
                    "MegaCrit.Sts2.Core.Runs.IRunState",
                    "System.Boolean"),
                new(
                    "MegaCrit.Sts2.Core.Rooms.MerchantRoom",
                    "Resume",
                    "MegaCrit.Sts2.Core.Rooms.AbstractRoom",
                    "MegaCrit.Sts2.Core.Runs.IRunState"),
                new(
                    "MegaCrit.Sts2.Core.Rooms.MerchantRoom",
                    "Exit",
                    "MegaCrit.Sts2.Core.Runs.IRunState"),
                new(
                    "MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom",
                    "OnMerchantOpened",
                    "MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantButton"),
                new(
                    "MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom",
                    "Create",
                    "MegaCrit.Sts2.Core.Rooms.MerchantRoom",
                    "System.Collections.Generic.IReadOnlyList`1<MegaCrit.Sts2.Core.Entities.Players.Player>"),
                new(
                    "MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom",
                    "AfterRoomIsLoaded"),
                new(
                    "MegaCrit.Sts2.Core.Nodes.Rooms.NMerchantRoom",
                    "OpenInventory"),
                new(
                    "MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantInventory",
                    "Initialize",
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory",
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantDialogueSet"),
                new(
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantEntry",
                    "OnTryPurchaseWrapper",
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory",
                    "System.Boolean"),
                new(
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantEntry",
                    "InvokePurchaseFailed",
                    "MegaCrit.Sts2.Core.Entities.Merchant.PurchaseStatus"),
                new(
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantCardEntry",
                    "RestockAfterPurchase",
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory"),
                new(
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantRelicEntry",
                    "RestockAfterPurchase",
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory"),
                new(
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantPotionEntry",
                    "RestockAfterPurchase",
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory"),
                new(
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantCardRemovalEntry",
                    "RestockAfterPurchase",
                    "MegaCrit.Sts2.Core.Entities.Merchant.MerchantInventory"),
                new(
                    "MegaCrit.Sts2.Core.Rooms.CombatRoom",
                    "Exit",
                    "MegaCrit.Sts2.Core.Runs.IRunState"),
                new(
                    "MegaCrit.Sts2.Core.Nodes.NGame",
                    "StartNewSingleplayerRun",
                    "MegaCrit.Sts2.Core.Models.CharacterModel",
                    "System.Boolean",
                    "System.Collections.Generic.IReadOnlyList`1<MegaCrit.Sts2.Core.Models.ActModel>",
                    "System.Collections.Generic.IReadOnlyList`1<MegaCrit.Sts2.Core.Models.ModifierModel>",
                    "System.String",
                    "MegaCrit.Sts2.Core.Runs.GameMode",
                    "System.Int32",
                    "System.Nullable`1<System.DateTimeOffset>"),
                new(
                    "MegaCrit.Sts2.Core.Nodes.NGame",
                    "StartNewMultiplayerRun",
                    "MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby",
                    "System.Boolean",
                    "System.Collections.Generic.IReadOnlyList`1<MegaCrit.Sts2.Core.Models.ActModel>",
                    "System.Collections.Generic.IReadOnlyList`1<MegaCrit.Sts2.Core.Models.ModifierModel>",
                    "System.String",
                    "System.Int32",
                    "System.Nullable`1<System.DateTimeOffset>"),
                new(
                    "MegaCrit.Sts2.Core.Modding.ModHelper",
                    "SubscribeForRunStateHooks",
                    "System.String",
                    "MegaCrit.Sts2.Core.Modding.RunHookSubscriptionDelegate"),
                new(
                    "MegaCrit.Sts2.Core.Modding.ModHelper",
                    "SubscribeForCombatStateHooks",
                    "System.String",
                    "MegaCrit.Sts2.Core.Modding.CombatHookSubscriptionDelegate")
            ]
        };
    }
}
