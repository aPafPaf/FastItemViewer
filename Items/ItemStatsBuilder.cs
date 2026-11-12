using System;
using System.Collections.Generic;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace FastItemViewer.Items;

public static class ItemStatsBuilder
{
    public static ItemSnapshot Build(GameController gameController, PlayerContext player, Entity item)
    {
        var snapshot = new ItemSnapshot
        {
            Entity = item
        };

        if (item == null || !item.IsValid)
            return snapshot;

        try
        {
            var baseItemType = gameController?.Files?.BaseItemTypes?.Translate(item.Path);
            snapshot.ClassName = baseItemType?.ClassName ?? string.Empty;
            snapshot.BaseName = baseItemType?.BaseName ?? string.Empty;

            if (item.TryGetComponent(out Base baseComponent) && !string.IsNullOrWhiteSpace(baseComponent.Name))
                snapshot.DisplayName = baseComponent.Name;
            else
                snapshot.DisplayName = snapshot.BaseName;

            if (item.TryGetComponent(out Mods mods))
            {
                snapshot.Identified = mods.Identified;
                snapshot.ItemLevel = mods.ItemLevel;
                snapshot.RequiredLevel = mods.RequiredLevel;

                AccumulateMods(mods, snapshot.Stats);

                if (item.HasComponent<Weapon>())
                {
                    snapshot.IsWeapon = true;

                    try
                    {
                        var itemStats = mods.ItemStats;
                        snapshot.Dps = itemStats.GetStat(ItemStatEnum.DPS);
                        snapshot.Aps = itemStats.GetStat(ItemStatEnum.AttackPerSecond);
                        snapshot.Crit = itemStats.GetStat(ItemStatEnum.WeaponCritChance);
                    }
                    catch
                    {
                        // ItemStats is best-effort; mod deltas still work without it.
                    }
                }
            }

            if (item.TryGetComponent(out Armour armour))
            {
                snapshot.Armour = armour.ArmourScore;
                snapshot.Evasion = armour.EvasionScore;
                snapshot.EnergyShield = armour.EnergyShieldScore;
            }

            if (item.TryGetComponent(out Quality quality))
                snapshot.Quality = quality.ItemQuality;

            if (item.TryGetComponent(out Sockets sockets))
                snapshot.Sockets = sockets.NumberOfSockets;

            snapshot.HasLocalDefence = EquipSlotMap.IsArmourClass(snapshot.ClassName);

            CheckRequirements(mods, item, player, snapshot);
        }
        catch
        {
            // Never let a single broken item break the whole inventory scan.
        }

        return snapshot;
    }

    private static void AccumulateMods(Mods mods, Dictionary<GameStat, int> result)
    {
        if (mods == null)
            return;

        var itemMods = mods.ItemMods;
        if (itemMods == null)
            return;

        foreach (var mod in itemMods)
        {
            if (mod == null)
                continue;

            var record = mod.ModRecord;
            var statNames = record?.StatNames;
            if (statNames == null)
                continue;

            var values = mod.Values;
            if (values == null || values.Count == 0)
                continue;

            var count = Math.Min(statNames.Length, values.Count);
            for (var i = 0; i < count; i++)
            {
                var statRecord = statNames[i];
                if (statRecord == null)
                    continue;

                var stat = statRecord.MatchingStat;
                var value = values[i];
                result[stat] = result.TryGetValue(stat, out var existing)
                    ? existing + value
                    : value;
            }
        }
    }

    private static void CheckRequirements(Mods mods, Entity item, PlayerContext player, ItemSnapshot snapshot)
    {
        if (player.Level <= 0)
            return;

        var missing = new List<string>();

        if (mods != null && mods.RequiredLevel > player.Level)
            missing.Add($"lvl {mods.RequiredLevel}");

        if (item.TryGetComponent(out AttributeRequirements requirements))
        {
            if (requirements.strength > player.Strength)
                missing.Add($"{requirements.strength} Str");
            if (requirements.dexterity > player.Dexterity)
                missing.Add($"{requirements.dexterity} Dex");
            if (requirements.intelligence > player.Intelligence)
                missing.Add($"{requirements.intelligence} Int");
        }

        snapshot.RequirementsMet = missing.Count == 0;
        snapshot.RequirementsText = missing.Count == 0 ? string.Empty : string.Join(", ", missing);
    }
}
