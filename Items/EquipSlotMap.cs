using System;
using System.Collections.Generic;

namespace FastItemViewer.Items;

public enum EquipSlotKind
{
    None,
    Helmet,
    BodyArmour,
    Gloves,
    Boots,
    Belt,
    Amulet,
    Ring,
    OneHandWeapon,
    TwoHandWeapon,
    Offhand
}

public static class EquipSlotMap
{
    private static readonly Dictionary<string, EquipSlotKind> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Helmet"] = EquipSlotKind.Helmet,
            ["Helmets"] = EquipSlotKind.Helmet,

            ["Body Armour"] = EquipSlotKind.BodyArmour,
            ["Body Armours"] = EquipSlotKind.BodyArmour,

            ["Gloves"] = EquipSlotKind.Gloves,
            ["Boots"] = EquipSlotKind.Boots,

            ["Belt"] = EquipSlotKind.Belt,
            ["Belts"] = EquipSlotKind.Belt,

            ["Amulet"] = EquipSlotKind.Amulet,
            ["Amulets"] = EquipSlotKind.Amulet,

            ["Ring"] = EquipSlotKind.Ring,
            ["Rings"] = EquipSlotKind.Ring,

            ["Shield"] = EquipSlotKind.Offhand,
            ["Shields"] = EquipSlotKind.Offhand,
            ["Quiver"] = EquipSlotKind.Offhand,
            ["Quivers"] = EquipSlotKind.Offhand,

            ["One Hand Sword"] = EquipSlotKind.OneHandWeapon,
            ["One Hand Swords"] = EquipSlotKind.OneHandWeapon,
            ["One Hand Axe"] = EquipSlotKind.OneHandWeapon,
            ["One Hand Axes"] = EquipSlotKind.OneHandWeapon,
            ["One Hand Mace"] = EquipSlotKind.OneHandWeapon,
            ["One Hand Maces"] = EquipSlotKind.OneHandWeapon,
            ["Thrusting One Hand Sword"] = EquipSlotKind.OneHandWeapon,
            ["Thrusting One Hand Swords"] = EquipSlotKind.OneHandWeapon,
            ["Claw"] = EquipSlotKind.OneHandWeapon,
            ["Claws"] = EquipSlotKind.OneHandWeapon,
            ["Dagger"] = EquipSlotKind.OneHandWeapon,
            ["Daggers"] = EquipSlotKind.OneHandWeapon,
            ["Rune Dagger"] = EquipSlotKind.OneHandWeapon,
            ["Rune Daggers"] = EquipSlotKind.OneHandWeapon,
            ["Wand"] = EquipSlotKind.OneHandWeapon,
            ["Wands"] = EquipSlotKind.OneHandWeapon,
            ["Sceptre"] = EquipSlotKind.OneHandWeapon,
            ["Sceptres"] = EquipSlotKind.OneHandWeapon,
            ["Fishing Rod"] = EquipSlotKind.OneHandWeapon,
            ["Fishing Rods"] = EquipSlotKind.OneHandWeapon,

            ["Two Hand Sword"] = EquipSlotKind.TwoHandWeapon,
            ["Two Hand Swords"] = EquipSlotKind.TwoHandWeapon,
            ["Two Hand Axe"] = EquipSlotKind.TwoHandWeapon,
            ["Two Hand Axes"] = EquipSlotKind.TwoHandWeapon,
            ["Two Hand Mace"] = EquipSlotKind.TwoHandWeapon,
            ["Two Hand Maces"] = EquipSlotKind.TwoHandWeapon,
            ["Bow"] = EquipSlotKind.TwoHandWeapon,
            ["Bows"] = EquipSlotKind.TwoHandWeapon,
            ["Staff"] = EquipSlotKind.TwoHandWeapon,
            ["Staves"] = EquipSlotKind.TwoHandWeapon,
            ["Warstaff"] = EquipSlotKind.TwoHandWeapon,
            ["Warstaves"] = EquipSlotKind.TwoHandWeapon
        };

    public static EquipSlotKind Get(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return EquipSlotKind.None;

        return Map.TryGetValue(className, out var kind) ? kind : EquipSlotKind.None;
    }

    public static bool IsArmourClass(string className)
    {
        return Get(className) is EquipSlotKind.Helmet
            or EquipSlotKind.BodyArmour
            or EquipSlotKind.Gloves
            or EquipSlotKind.Boots
            or EquipSlotKind.Offhand;
    }

    public static string GetShortLabel(EquipSlotKind kind)
    {
        return kind switch
        {
            EquipSlotKind.Helmet => "Helm",
            EquipSlotKind.BodyArmour => "Body",
            EquipSlotKind.Gloves => "Gloves",
            EquipSlotKind.Boots => "Boots",
            EquipSlotKind.Belt => "Belt",
            EquipSlotKind.Amulet => "Amulet",
            EquipSlotKind.Ring => "Ring",
            EquipSlotKind.OneHandWeapon => "Weapon",
            EquipSlotKind.TwoHandWeapon => "2H",
            EquipSlotKind.Offhand => "Offhand",
            _ => "?"
        };
    }
}
