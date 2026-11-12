using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace FastItemViewer.Items;

public sealed class ItemComparisonEngine
{
    private readonly GameController _gameController;
    private readonly PlayerContext _player;
    private readonly EquippedItems _equipped;
    private readonly Dictionary<long, ItemSnapshot> _snapshots;
    private readonly FastItemViewerSettings _settings;
    private readonly StatLabelProvider _labels;

    public ItemComparisonEngine(
        GameController gameController,
        PlayerContext player,
        EquippedItems equipped,
        Dictionary<long, ItemSnapshot> snapshots,
        FastItemViewerSettings settings)
    {
        _gameController = gameController;
        _player = player;
        _equipped = equipped;
        _snapshots = snapshots;
        _settings = settings;
        _labels = new StatLabelProvider(gameController);
    }

    public ItemComparison Compare(Entity item)
    {
        if (item == null || item.Address == 0)
            return null;

        var candidate = GetSnapshot(item);
        if (candidate == null || string.IsNullOrWhiteSpace(candidate.ClassName))
            return null;

        var kind = EquipSlotMap.Get(candidate.ClassName);
        if (kind == EquipSlotKind.None)
            return null;

        var comparison = new ItemComparison
        {
            Candidate = candidate,
            Kind = kind
        };

        if (!candidate.Identified)
        {
            comparison.IsUnidentified = true;
            return comparison;
        }

        foreach (var target in BuildTargets(candidate, kind))
            comparison.Targets.Add(target);

        if (comparison.Targets.Count == 0)
            return comparison;

        comparison.BestTarget = comparison.Targets.FirstOrDefault(x => x.EmptySlot)
                               ?? comparison.Targets.OrderByDescending(x => x.Score).First();

        comparison.Score = comparison.BestTarget.Score;
        comparison.HasMeaningfulChanges = comparison.Targets.Any(x => x.Lines.Count > 0 || x.Flags.Count > 0);
        comparison.IsUpgrade = comparison.BestTarget.EmptySlot && comparison.Score >= 0 && comparison.HasMeaningfulChanges
            ? true
            : comparison.Score > 0.5f
                ? true
                : comparison.Score < -0.5f
                    ? false
                    : null;

        return comparison;
    }

    private List<SlotComparison> BuildTargets(ItemSnapshot candidate, EquipSlotKind kind)
    {
        var result = new List<SlotComparison>();

        switch (kind)
        {
            case EquipSlotKind.Ring:
                if (_equipped.Ring1 == null && _equipped.Ring2 == null)
                {
                    result.Add(CreateTarget(candidate, "Ring", "Ring"));
                }
                else
                {
                    result.Add(CreateTarget(candidate, "Ring 1", "R1", _equipped.Ring1));
                    result.Add(CreateTarget(candidate, "Ring 2", "R2", _equipped.Ring2));
                }
                break;

            case EquipSlotKind.OneHandWeapon:
                var mainKind = GetEquippedKind(_equipped.Weapon1);

                if (mainKind == EquipSlotKind.TwoHandWeapon)
                {
                    // Switching from a bow (quiver) or another two-hander frees both hands.
                    result.Add(CreateTarget(candidate, "Main Hand", "MH", _equipped.Weapon1, _equipped.Offhand1));
                }
                else
                {
                    result.Add(CreateTarget(candidate, "Main Hand", "MH", _equipped.Weapon1));

                    var offhandKind = GetEquippedKind(_equipped.Offhand1);
                    if (_equipped.Offhand1 == null || offhandKind == EquipSlotKind.OneHandWeapon)
                        result.Add(CreateTarget(candidate, "Off Hand", "OH", _equipped.Offhand1));
                }
                break;

            case EquipSlotKind.TwoHandWeapon:
                if (IsBow(candidate.ClassName))
                {
                    // A quiver can stay in the off hand, everything else is removed.
                    var offhandSnapshot = GetSnapshot(_equipped.Offhand1);
                    var offhandIsQuiver = offhandSnapshot != null && IsQuiver(offhandSnapshot.ClassName);

                    if (_equipped.Offhand1 == null || offhandIsQuiver)
                        result.Add(CreateTarget(candidate, "Main Hand", "MH", _equipped.Weapon1));
                    else
                        result.Add(CreateTarget(candidate, "Main + Off Hand", "2H", _equipped.Weapon1, _equipped.Offhand1));
                }
                else
                {
                    result.Add(CreateTarget(candidate, "Main + Off Hand", "2H", _equipped.Weapon1, _equipped.Offhand1));
                }
                break;

            case EquipSlotKind.Offhand:
                result.Add(CreateTarget(candidate, "Off Hand", "OH", _equipped.Offhand1));
                break;

            case EquipSlotKind.Helmet:
                result.Add(CreateTarget(candidate, "Helmet", "Helm", _equipped.Helmet));
                break;

            case EquipSlotKind.BodyArmour:
                result.Add(CreateTarget(candidate, "Body Armour", "Body", _equipped.BodyArmour));
                break;

            case EquipSlotKind.Gloves:
                result.Add(CreateTarget(candidate, "Gloves", "Gloves", _equipped.Gloves));
                break;

            case EquipSlotKind.Boots:
                result.Add(CreateTarget(candidate, "Boots", "Boots", _equipped.Boots));
                break;

            case EquipSlotKind.Belt:
                result.Add(CreateTarget(candidate, "Belt", "Belt", _equipped.Belt));
                break;

            case EquipSlotKind.Amulet:
                result.Add(CreateTarget(candidate, "Amulet", "Amulet", _equipped.Amulet));
                break;
        }

        return result;
    }

    private static bool IsBow(string className)
    {
        return string.Equals(className, "Bow", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "Bows", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsQuiver(string className)
    {
        return string.Equals(className, "Quiver", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "Quivers", StringComparison.OrdinalIgnoreCase);
    }

    private SlotComparison CreateTarget(ItemSnapshot candidate, string label, string shortLabel, params Entity[] equippedEntities)    {
        var slot = new SlotComparison
        {
            Label = label,
            ShortLabel = shortLabel
        };

        foreach (var entity in equippedEntities)
        {
            if (entity == null)
                continue;

            var snapshot = GetSnapshot(entity);
            if (snapshot != null)
                slot.Replaced.Add(snapshot);
        }

        slot.EmptySlot = slot.Replaced.Count == 0;
        BuildSlot(candidate, slot);
        return slot;
    }

    private void BuildSlot(ItemSnapshot candidate, SlotComparison slot)
    {
        var useDefenceTotals = candidate.HasLocalDefence &&
                               (candidate.Armour > 0 ||
                                candidate.Evasion > 0 ||
                                candidate.EnergyShield > 0 ||
                                slot.Replaced.Any(x => x.Armour > 0 || x.Evasion > 0 || x.EnergyShield > 0));

        foreach (var definition in StatCatalog.Definitions)
        {
            if (definition.Stats.Length == 0)
                continue;

            if (useDefenceTotals && definition.SkipWhenLocalDefenceTotals)
                continue;

            var newValue = Sum(definition, candidate.Stats);
            var oldValue = slot.Replaced.Sum(x => Sum(definition, x.Stats));

            if (newValue == oldValue)
                continue;

            var delta = newValue - oldValue;
            AddProjection(slot, definition.Projection, newValue, oldValue);

            var valueText = definition.FormatValue(delta);
            slot.Lines.Add(new StatLine
            {
                Label = definition.GetLabel(),
                ValueText = valueText,
                CompactText = definition.FormatLine(delta),
                DetailText = $"{definition.GetLabel()}: {oldValue} -> {newValue} ({valueText})",
                Delta = delta,
                OldValue = oldValue,
                NewValue = newValue,
                Priority = definition.Priority,
                Element = StatCatalog.GetResistanceElement(definition.Projection)
            });

            slot.Score += (int)Math.Round(GetScoreDelta(definition, delta) * definition.ScoreWeight);
        }

        if (useDefenceTotals)
            AddDefenceTotals(candidate, slot);

        if (candidate.IsWeapon && _settings.ShowWeaponDps.Value)
            AddWeaponTotals(candidate, slot);

        AddUnknownStats(candidate, slot);

        if (!candidate.RequirementsMet)
        {
            var requirements = string.IsNullOrWhiteSpace(candidate.RequirementsText)
                ? "requirements"
                : candidate.RequirementsText;

            slot.Lines.Insert(0, new StatLine
            {
                Label = "Requirements",
                ValueText = "!",
                CompactText = $"! {requirements}",
                DetailText = $"Requires: {requirements}",
                Delta = 0,
                Priority = 0,
                IsWarning = true
            });
        }

        slot.RequirementsMet = candidate.RequirementsMet;
        slot.RequirementsText = candidate.RequirementsText;
        slot.Lines.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }

    private int GetScoreDelta(StatDefinition definition, int delta)
    {
        if (delta == 0 || !_player.HasResistances)
            return delta;

        int current;
        int cap;

        switch (definition.Projection)
        {
            case ProjectionKind.FireResistance:
                current = _player.FireResistance;
                cap = _player.MaxFireResistance > 0 ? _player.MaxFireResistance : 75;
                break;
            case ProjectionKind.ColdResistance:
                current = _player.ColdResistance;
                cap = _player.MaxColdResistance > 0 ? _player.MaxColdResistance : 75;
                break;
            case ProjectionKind.LightningResistance:
                current = _player.LightningResistance;
                cap = _player.MaxLightningResistance > 0 ? _player.MaxLightningResistance : 75;
                break;
            case ProjectionKind.ChaosResistance:
                current = _player.ChaosResistance;
                cap = _player.MaxChaosResistance > 0 ? _player.MaxChaosResistance : 60;
                break;
            default:
                return delta;
        }

        if (delta < 0)
        {
            var overcap = Math.Max(0, current - cap);
            return Math.Min(0, delta + overcap);
        }

        var missing = Math.Max(0, cap - current);
        return Math.Min(delta, missing);
    }

    private void AddDefenceTotals(ItemSnapshot candidate, SlotComparison slot)
    {
        var oldArmour = slot.Replaced.Sum(x => x.Armour);
        var oldEvasion = slot.Replaced.Sum(x => x.Evasion);
        var oldEnergyShield = slot.Replaced.Sum(x => x.EnergyShield);

        AddTotal(slot, "Armour", candidate.Armour, oldArmour, 25, 0.005f);
        AddTotal(slot, "Evasion", candidate.Evasion, oldEvasion, 26, 0.005f);
        AddTotal(slot, "ES", candidate.EnergyShield, oldEnergyShield, 20, 0.05f);

        AddProjection(slot, ProjectionKind.EnergyShieldFlat, candidate.EnergyShield, oldEnergyShield);
    }

    private static void AddTotal(SlotComparison slot, string label, int newValue, int oldValue, int priority, float weight)
    {
        if (newValue == oldValue)
            return;

        var delta = newValue - oldValue;
        var valueText = FormatSigned(delta);

        slot.Lines.Add(new StatLine
        {
            Label = label,
            ValueText = valueText,
            CompactText = $"{valueText} {label}",
            DetailText = $"{label}: {oldValue} -> {newValue} ({valueText})",
            Delta = delta,
            OldValue = oldValue,
            NewValue = newValue,
            Priority = priority,
            IsTotal = true
        });

        slot.Score += (int)Math.Round(delta * weight);
    }

    private void AddWeaponTotals(ItemSnapshot candidate, SlotComparison slot)
    {
        var oldDps = slot.Replaced.Where(x => x.IsWeapon).Sum(x => x.Dps);
        var newDps = candidate.Dps;

        if (newDps > 0.1f)
        {
            if (oldDps > 0.1f)
            {
                var percent = (newDps - oldDps) / oldDps * 100f;
                var valueText = $"{percent:+0.0;-0.0}%";

                slot.Lines.Add(new StatLine
                {
                    Label = "DPS",
                    ValueText = valueText,
                    CompactText = $"{valueText} DPS",
                    DetailText = $"DPS: {oldDps:0} -> {newDps:0} ({valueText})",
                    Delta = (int)Math.Round(percent),
                    OldValue = (int)Math.Round(oldDps),
                    NewValue = (int)Math.Round(newDps),
                    Priority = 9,
                    IsTotal = true
                });

                slot.Score += (int)Math.Round(percent * 0.5f);
            }
            else
            {
                var valueText = $"+{newDps:0}";

                slot.Lines.Add(new StatLine
                {
                    Label = "DPS",
                    ValueText = valueText,
                    CompactText = $"{valueText} DPS",
                    DetailText = $"DPS: {newDps:0}",
                    Delta = 1,
                    OldValue = 0,
                    NewValue = (int)Math.Round(newDps),
                    Priority = 9,
                    IsTotal = true
                });

                slot.Score += 5;
            }
        }

        var oldAps = slot.Replaced.Where(x => x.IsWeapon).Sum(x => x.Aps);
        var newAps = candidate.Aps;
        if (newAps > 0.01f && Math.Abs(newAps - oldAps) > 0.01f)
        {
            var delta = newAps - oldAps;
            var valueText = $"{delta:+0.00;-0.00}";

            slot.Lines.Add(new StatLine
            {
                Label = "APS",
                ValueText = valueText,
                CompactText = $"{valueText} APS",
                DetailText = $"APS: {oldAps:0.00} -> {newAps:0.00}",
                Delta = (int)Math.Round(delta * 100),
                OldValue = (int)Math.Round(oldAps * 100),
                NewValue = (int)Math.Round(newAps * 100),
                Priority = 42,
                IsTotal = true
            });

            slot.Score += (int)Math.Round(delta * 2f);
        }

        var oldCrit = slot.Replaced.Where(x => x.IsWeapon).Sum(x => x.Crit);
        var newCrit = candidate.Crit;
        if (newCrit > 0.01f && Math.Abs(newCrit - oldCrit) > 0.01f)
        {
            var delta = newCrit - oldCrit;
            var valueText = $"{delta:+0.0;-0.0}%";

            slot.Lines.Add(new StatLine
            {
                Label = "Crit",
                ValueText = valueText,
                CompactText = $"{valueText} Crit",
                DetailText = $"Crit: {oldCrit:0.0}% -> {newCrit:0.0}%",
                Delta = (int)Math.Round(delta * 10),
                OldValue = (int)Math.Round(oldCrit * 10),
                NewValue = (int)Math.Round(newCrit * 10),
                Priority = 43,
                IsTotal = true
            });

            slot.Score += (int)Math.Round(delta * 0.5f);
        }
    }

    private void AddUnknownStats(ItemSnapshot candidate, SlotComparison slot)
    {
        var allStats = new HashSet<GameStat>(candidate.Stats.Keys);
        foreach (var replaced in slot.Replaced)
        {
            foreach (var stat in replaced.Stats.Keys)
                allStats.Add(stat);
        }

        foreach (var stat in allStats)
        {
            if (StatCatalog.KnownStats.Contains(stat))
                continue;

            var newValue = candidate.GetStat(stat);
            var oldValue = slot.Replaced.Sum(x => x.GetStat(stat));

            if (newValue == oldValue)
                continue;

            var description = _labels.Describe(stat);

            if (description.IsFlag)
            {
                var gained = newValue != 0;
                slot.Flags.Add(new FlagLine
                {
                    Label = description.Label,
                    Gained = gained
                });

                slot.Score += gained ? 3 : -3;
                continue;
            }

            if (!_settings.ShowUnknownStats.Value)
                continue;

            var delta = newValue - oldValue;
            var valueText = description.FormatValue(delta);

            slot.Lines.Add(new StatLine
            {
                Label = description.Label,
                ValueText = valueText,
                CompactText = $"{valueText} {description.Label}",
                DetailText = $"{description.Label}: {oldValue} -> {newValue} ({valueText})",
                Delta = delta,
                OldValue = oldValue,
                NewValue = newValue,
                Priority = 200
            });

            slot.Score += (int)Math.Round(delta * 0.001f);
        }
    }

    private ItemSnapshot GetSnapshot(Entity entity)
    {
        if (entity == null)
            return null;

        if (_snapshots.TryGetValue(entity.Address, out var snapshot))
            return snapshot;

        snapshot = ItemStatsBuilder.Build(_gameController, _player, entity);
        _snapshots[entity.Address] = snapshot;
        return snapshot;
    }

    private EquipSlotKind GetEquippedKind(Entity entity)
    {
        if (entity == null)
            return EquipSlotKind.None;

        var snapshot = GetSnapshot(entity);
        return snapshot == null ? EquipSlotKind.None : EquipSlotMap.Get(snapshot.ClassName);
    }

    private static int Sum(StatDefinition definition, Dictionary<ExileCore.Shared.Enums.GameStat, int> stats)
    {
        var total = 0;
        foreach (var stat in definition.Stats)
        {
            if (stats.TryGetValue(stat, out var value))
                total += value;
        }

        return total;
    }

    private static void AddProjection(SlotComparison slot, ProjectionKind kind, int newValue, int oldValue)
    {
        if (kind == ProjectionKind.None)
            return;

        if (kind == ProjectionKind.AllAttributes)
        {
            AddProjection(slot, ProjectionKind.Strength, newValue, oldValue);
            AddProjection(slot, ProjectionKind.Dexterity, newValue, oldValue);
            AddProjection(slot, ProjectionKind.Intelligence, newValue, oldValue);
            return;
        }

        slot.NewContribution[kind] = slot.NewContribution.GetValueOrDefault(kind) + newValue;
        slot.OldContribution[kind] = slot.OldContribution.GetValueOrDefault(kind) + oldValue;
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }
}
