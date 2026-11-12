using System.Linq;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace FastItemViewer.Items;

public sealed class EquippedItems
{
    public Entity Helmet { get; private set; }
    public Entity BodyArmour { get; private set; }
    public Entity Gloves { get; private set; }
    public Entity Boots { get; private set; }
    public Entity Belt { get; private set; }
    public Entity Amulet { get; private set; }
    public Entity Ring1 { get; private set; }
    public Entity Ring2 { get; private set; }
    public Entity Weapon1 { get; private set; }
    public Entity Offhand1 { get; private set; }
    public Entity Weapon2 { get; private set; }
    public Entity Offhand2 { get; private set; }
    public Entity[] Flasks { get; private set; } = [];

    public static EquippedItems Capture(ServerData serverData)
    {
        var result = new EquippedItems();

        if (serverData?.PlayerInventories == null)
            return result;

        foreach (var holder in serverData.PlayerInventories)
        {
            if (holder?.Inventory == null)
                continue;

            var slotItems = holder.Inventory.InventorySlotItems;
            if (slotItems == null || slotItems.Count == 0)
                continue;

            var firstItem = GetUsableItem(slotItems.FirstOrDefault()?.Item);

            switch (holder.TypeId)
            {
                case InventoryNameE.Helm1:
                    result.Helmet = firstItem;
                    break;
                case InventoryNameE.BodyArmour1:
                    result.BodyArmour = firstItem;
                    break;
                case InventoryNameE.Gloves1:
                    result.Gloves = firstItem;
                    break;
                case InventoryNameE.Boots1:
                    result.Boots = firstItem;
                    break;
                case InventoryNameE.Belt1:
                    result.Belt = firstItem;
                    break;
                case InventoryNameE.Amulet1:
                    result.Amulet = firstItem;
                    break;
                case InventoryNameE.Ring1:
                    result.Ring1 = firstItem;
                    break;
                case InventoryNameE.Ring2:
                    result.Ring2 = firstItem;
                    break;
                case InventoryNameE.Weapon1:
                    result.Weapon1 = firstItem;
                    break;
                case InventoryNameE.Offhand1:
                    result.Offhand1 = firstItem;
                    break;
                case InventoryNameE.Weapon2:
                    result.Weapon2 = firstItem;
                    break;
                case InventoryNameE.Offhand2:
                    result.Offhand2 = firstItem;
                    break;
                case InventoryNameE.Flask1:
                    result.Flasks = slotItems
                        .Select(x => GetUsableItem(x.Item))
                        .Where(x => x != null)
                        .ToArray();
                    break;
            }
        }

        // A broken/duplicated ring entity must not make both ring slots look occupied.
        if (result.Ring1 != null && result.Ring2 != null && result.Ring1.Address == result.Ring2.Address)
            result.Ring2 = null;

        return result;
    }

    private static Entity GetUsableItem(Entity entity)
    {
        if (entity == null || entity.Address == 0 || !entity.IsValid)
            return null;

        return entity;
    }

    public static bool IsEquipmentType(InventoryNameE type)
    {
        return type is InventoryNameE.BodyArmour1
            or InventoryNameE.Weapon1
            or InventoryNameE.Offhand1
            or InventoryNameE.Helm1
            or InventoryNameE.Amulet1
            or InventoryNameE.Ring1
            or InventoryNameE.Ring2
            or InventoryNameE.Gloves1
            or InventoryNameE.Boots1
            or InventoryNameE.Belt1
            or InventoryNameE.Flask1
            or InventoryNameE.Weapon2
            or InventoryNameE.Offhand2;
    }
}
