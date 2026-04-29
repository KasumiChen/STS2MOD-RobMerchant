using MerchantDiscountMod.Combat;
using MerchantDiscountMod.Persistence;
using MerchantDiscountMod.UI;

namespace MerchantDiscountMod.Integration;

public sealed class ReflectionMerchantShopContext
{
    private object? currentRunState;
    private object? currentMerchantRoom;
    private object? currentMerchantCombatRoom;
    private object? pendingMerchantCombatResumeRoom;
    private object? preservedMerchantInventory;

    public object? CurrentRunState => currentRunState;

    public object? CurrentMerchantRoom => currentMerchantRoom;

    public object? CurrentMerchantCombatRoom => currentMerchantCombatRoom;

    public bool MerchantCombatLaunchInProgress => currentMerchantCombatRoom is not null;

    public bool HasPreservedMerchantInventory => preservedMerchantInventory is not null;

    public string? LastMerchantDialogueLine { get; private set; }

    public DiscountPromptRequest? PendingPrompt { get; private set; }

    public MerchantBattleRequest? LastCombatRequest { get; private set; }

    public MerchantRunStateSnapshot? SavedRunStateSnapshot { get; private set; }

    public void CaptureRunState(object? runState)
    {
        currentRunState = runState;
    }

    public void CaptureMerchantRoom(object? merchantRoom)
    {
        currentMerchantRoom = merchantRoom;
    }

    public void ClearMerchantRoom()
    {
        currentMerchantRoom = null;
    }

    public void CaptureMerchantCombatRoom(object? combatRoom)
    {
        currentMerchantCombatRoom = combatRoom;
    }

    public void ClearMerchantCombatRoom()
    {
        currentMerchantCombatRoom = null;
    }

    public void MarkMerchantCombatResumePending(object combatRoom)
    {
        pendingMerchantCombatResumeRoom = combatRoom;
    }

    public bool ConsumeMerchantCombatResume(object? previousRoom)
    {
        if (pendingMerchantCombatResumeRoom is null || !ReferenceEquals(pendingMerchantCombatResumeRoom, previousRoom))
        {
            return false;
        }

        pendingMerchantCombatResumeRoom = null;
        return true;
    }

    public bool CaptureCurrentMerchantInventory()
    {
        var inventory = TryGetCurrentMerchantInventory();
        if (inventory is null)
        {
            preservedMerchantInventory = null;
            return false;
        }

        preservedMerchantInventory = inventory;
        return true;
    }

    public bool TryRestorePreservedMerchantInventory(object merchantRoom)
    {
        if (preservedMerchantInventory is null)
        {
            return false;
        }

        if (TrySetMerchantInventory(merchantRoom, preservedMerchantInventory))
        {
            return true;
        }

        return false;
    }

    public void ClearPreservedMerchantInventory()
    {
        preservedMerchantInventory = null;
    }

    public void RecordMerchantDialogueLine(string line)
    {
        LastMerchantDialogueLine = line;
    }

    public void RecordPendingPrompt(DiscountPromptRequest prompt)
    {
        PendingPrompt = prompt;
    }

    public void ClearPendingPrompt()
    {
        PendingPrompt = null;
    }

    public void RecordCombatLaunch(MerchantBattleRequest request)
    {
        LastCombatRequest = request;
    }

    public void SaveRunStateSnapshot(MerchantRunStateSnapshot snapshot)
    {
        SavedRunStateSnapshot = snapshot;
    }

    private object? TryGetCurrentMerchantInventory()
    {
        if (currentMerchantRoom is null)
        {
            return null;
        }

        var roomModel = ReflectionMemberAccess.GetPropertyValue(currentMerchantRoom, "Room");
        var roomModelInventory = roomModel is null
            ? null
            : ReflectionMemberAccess.GetPropertyValue(roomModel, "Inventory");
        if (roomModelInventory is not null)
        {
            return roomModelInventory;
        }

        var inventoryNode = ReflectionMemberAccess.GetPropertyValue(currentMerchantRoom, "Inventory");
        var inventoryNodeInventory = inventoryNode is null
            ? null
            : ReflectionMemberAccess.GetPropertyValue(inventoryNode, "Inventory");
        if (inventoryNodeInventory is not null)
        {
            return inventoryNodeInventory;
        }

        return ReflectionMemberAccess.GetPropertyValue(currentMerchantRoom, "Inventory");
    }

    private static bool TrySetMerchantInventory(object merchantRoom, object inventory)
    {
        var property = merchantRoom
            .GetType()
            .GetProperty("Inventory", ReflectionMemberAccess.InstanceMemberFlags);
        if (property is not null && property.GetIndexParameters().Length == 0)
        {
            ReflectionMemberAccess.TrySetValue(merchantRoom, property, inventory);
            if (ReferenceEquals(ReflectionMemberAccess.GetPropertyValue(merchantRoom, "Inventory"), inventory))
            {
                return true;
            }
        }

        var backingField = merchantRoom
            .GetType()
            .GetField("<Inventory>k__BackingField", ReflectionMemberAccess.InstanceMemberFlags);
        if (backingField is not null)
        {
            ReflectionMemberAccess.TrySetValue(merchantRoom, backingField, inventory);
            if (ReferenceEquals(ReflectionMemberAccess.GetPropertyValue(merchantRoom, "Inventory"), inventory))
            {
                return true;
            }
        }

        var field = merchantRoom
            .GetType()
            .GetField("Inventory", ReflectionMemberAccess.InstanceMemberFlags);
        if (field is not null)
        {
            ReflectionMemberAccess.TrySetValue(merchantRoom, field, inventory);
            return ReferenceEquals(field.GetValue(merchantRoom), inventory);
        }

        return false;
    }
}
