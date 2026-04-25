using MerchantDiscountMod.Domain.Shop;

namespace MerchantDiscountMod.Integration;

public sealed class ReflectionMerchantShopStateRenderer : IShopStateRenderer
{
    private readonly ReflectionMerchantShopContext context;

    public ReflectionMerchantShopStateRenderer(ReflectionMerchantShopContext context)
    {
        this.context = context;
    }

    public void Render(ShopStateSnapshot snapshot)
    {
        var merchantRoom = context.CurrentMerchantRoom;
        if (merchantRoom is null)
        {
            return;
        }

        if (!snapshot.MerchantVisible)
        {
            HideMerchantButton(merchantRoom);
        }

        if (snapshot.PricesAreZero)
        {
            ZeroCurrentInventoryPrices(merchantRoom);
            RefreshInventorySlots(merchantRoom);
        }

        if (snapshot.FutureStockSuppressed)
        {
            EmptyCurrentInventory(merchantRoom);
            RefreshInventorySlots(merchantRoom);
        }
    }

    private static void HideMerchantButton(object merchantRoom)
    {
        var merchantButton = ReflectionMemberAccess.GetPropertyValue(merchantRoom, "MerchantButton");
        if (merchantButton is null)
        {
            return;
        }

        ReflectionMemberAccess.InvokeParameterless(merchantButton, "Hide");
        var visibleProperty = merchantButton.GetType().GetProperty("Visible", ReflectionMemberAccess.InstanceMemberFlags);
        if (visibleProperty is not null && visibleProperty.SetMethod is not null)
        {
            ReflectionMemberAccess.TrySetValue(merchantButton, visibleProperty, false);
        }

        ReflectionMemberAccess.Invoke(merchantButton, "SetProcess", false);
        ReflectionMemberAccess.Invoke(merchantButton, "SetProcessInput", false);
        ReflectionMemberAccess.Invoke(merchantButton, "SetProcessUnhandledInput", false);
    }

    private static void ZeroCurrentInventoryPrices(object merchantRoom)
    {
        var inventoryModel = GetInventoryModel(merchantRoom);
        if (inventoryModel is null)
        {
            return;
        }

        foreach (var entry in ReflectionMemberAccess.EnumerateProperty(inventoryModel, "AllEntries"))
        {
            ZeroEntryCost(entry);
            ReflectionMemberAccess.InvokeParameterless(entry, "UpdateEntry");
        }
    }

    private static void EmptyCurrentInventory(object merchantRoom)
    {
        var inventoryModel = GetInventoryModel(merchantRoom);
        if (inventoryModel is null)
        {
            return;
        }

        foreach (var entry in ReflectionMemberAccess.EnumerateProperty(inventoryModel, "AllEntries"))
        {
            ReflectionMemberAccess.InvokeParameterless(entry, "ClearAfterPurchase");
            ReflectionMemberAccess.InvokeParameterless(entry, "UpdateEntry");
        }
    }

    private static void RefreshInventorySlots(object merchantRoom)
    {
        var inventoryNode = GetInventoryNode(merchantRoom);
        var slots = inventoryNode is null
            ? null
            : ReflectionMemberAccess.InvokeParameterless(inventoryNode, "GetAllSlots") as System.Collections.IEnumerable;
        if (slots is null)
        {
            return;
        }

        foreach (var slot in slots)
        {
            if (slot is not null)
            {
                ReflectionMemberAccess.InvokeParameterless(slot, "UpdateVisual");
            }
        }
    }

    private static object? GetInventoryModel(object merchantRoom)
    {
        var inventoryNode = GetInventoryNode(merchantRoom);
        return inventoryNode is null
            ? ReflectionMemberAccess.GetPropertyValue(merchantRoom, "Inventory")
            : ReflectionMemberAccess.GetPropertyValue(inventoryNode, "Inventory") ?? inventoryNode;
    }

    private static object? GetInventoryNode(object merchantRoom) =>
        ReflectionMemberAccess.GetPropertyValue(merchantRoom, "Inventory");

    private static void ZeroEntryCost(object entry)
    {
        var entryType = entry.GetType();
        var costField = entryType.GetField("_cost", ReflectionMemberAccess.InstanceMemberFlags);
        if (costField is not null)
        {
            ReflectionMemberAccess.TrySetValue(entry, costField, ReflectionMemberAccess.CreateDefaultValue(costField.FieldType));
            return;
        }

        var costProperty = entryType.GetProperty("Cost", ReflectionMemberAccess.InstanceMemberFlags);
        if (costProperty is not null && costProperty.SetMethod is not null)
        {
            ReflectionMemberAccess.TrySetValue(entry, costProperty, ReflectionMemberAccess.CreateDefaultValue(costProperty.PropertyType));
        }
    }
}
