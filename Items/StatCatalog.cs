using System;
using System.Collections.Generic;
using ExileCore;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.Shared.Enums;

namespace FastItemViewer.Items;

public enum StatCategory
{
    Resistance,
    Defence,
    Attribute,
    Speed,
    Damage,
    Utility
}

public enum ProjectionKind
{
    None,
    LifeFlat,
    LifePct,
    ManaFlat,
    ManaPct,
    EnergyShieldFlat,
    EnergyShieldPct,
    FireResistance,
    ColdResistance,
    LightningResistance,
    ChaosResistance,
    MaxFireResistance,
    MaxColdResistance,
    MaxLightningResistance,
    MaxChaosResistance,
    Strength,
    Dexterity,
    Intelligence,
    AllAttributes
}

public sealed class StatDefinition
{
    public StatDefinition(
        string id,
        string labelRu,
        string labelEn,
        string unit,
        int priority,
        StatCategory category,
        ProjectionKind projection,
        float scoreWeight,
        bool skipWhenLocalDefenceTotals,
        params string[] aliasNames)
    {
        Id = id;
        LabelRu = labelRu;
        LabelEn = labelEn;
        Unit = unit;
        Priority = priority;
        Category = category;
        Projection = projection;
        ScoreWeight = scoreWeight;
        SkipWhenLocalDefenceTotals = skipWhenLocalDefenceTotals;

        var stats = new List<GameStat>();
        foreach (var alias in aliasNames)
        {
            if (Enum.TryParse<GameStat>(alias, true, out var stat))
                stats.Add(stat);
        }

        Stats = stats.ToArray();
    }

    public string Id { get; }
    public string LabelRu { get; }
    public string LabelEn { get; }
    public string Unit { get; }
    public int Priority { get; }
    public StatCategory Category { get; }
    public ProjectionKind Projection { get; }
    public float ScoreWeight { get; }
    public bool SkipWhenLocalDefenceTotals { get; }
    public GameStat[] Stats { get; }

    public string GetLabel() => LabelEn;

    public string FormatValue(int value)
    {
        var text = value > 0 ? $"+{value}" : value.ToString();
        return text + Unit;
    }

    public string FormatLine(int value)
    {
        var label = GetLabel();
        if (Unit == "%" && label.EndsWith("%", StringComparison.Ordinal))
            label = label[..^1].TrimEnd();

        return $"{FormatValue(value)} {label}";
    }
}

public static class StatCatalog
{
    public static readonly IReadOnlyList<StatDefinition> Definitions = new List<StatDefinition>
    {
        Def("fire_res", "Огонь", "Fire", "%", 1, StatCategory.Resistance, ProjectionKind.FireResistance, 1.0f, false,
            "BaseFireDamageResistancePct", "FireDamageResistancePct"),
        Def("cold_res", "Холод", "Cold", "%", 2, StatCategory.Resistance, ProjectionKind.ColdResistance, 1.0f, false,
            "BaseColdDamageResistancePct", "ColdDamageResistancePct"),
        Def("lightning_res", "Молния", "Lightning", "%", 3, StatCategory.Resistance, ProjectionKind.LightningResistance, 1.0f, false,
            "BaseLightningDamageResistancePct", "LightningDamageResistancePct"),
        Def("chaos_res", "Хаос", "Chaos", "%", 4, StatCategory.Resistance, ProjectionKind.ChaosResistance, 0.5f, false,
            "BaseChaosDamageResistancePct", "ChaosDamageResistancePct"),

        Def("max_fire_res", "Макс. огонь", "Max Fire", "%", 5, StatCategory.Resistance, ProjectionKind.MaxFireResistance, 2.0f, false,
            "MaximumFireDamageResistancePct", "AdditionalMaximumAllElementalResistancesPct", "AdditionalMaximumAllResistancesPct"),
        Def("max_cold_res", "Макс. холод", "Max Cold", "%", 6, StatCategory.Resistance, ProjectionKind.MaxColdResistance, 2.0f, false,
            "MaximumColdDamageResistancePct", "AdditionalMaximumAllElementalResistancesPct", "AdditionalMaximumAllResistancesPct"),
        Def("max_lightning_res", "Макс. молния", "Max Lightning", "%", 7, StatCategory.Resistance, ProjectionKind.MaxLightningResistance, 2.0f, false,
            "MaximumLightningDamageResistancePct", "AdditionalMaximumAllElementalResistancesPct", "AdditionalMaximumAllResistancesPct"),
        Def("max_chaos_res", "Макс. хаос", "Max Chaos", "%", 8, StatCategory.Resistance, ProjectionKind.MaxChaosResistance, 1.0f, false,
            "MaximumChaosDamageResistancePct", "AdditionalMaximumAllResistancesPct"),

        Def("life", "HP", "HP", "", 10, StatCategory.Defence, ProjectionKind.LifeFlat, 0.10f, false,
            "BaseMaximumLife", "TotalBaseMaximumLife", "MaximumLife"),
        Def("life_pct", "HP %", "HP %", "%", 55, StatCategory.Defence, ProjectionKind.LifePct, 0.05f, false,
            "MaximumLifePct"),
        Def("mana", "Мана", "Mana", "", 30, StatCategory.Defence, ProjectionKind.ManaFlat, 0.03f, false,
            "BaseMaximumMana", "TotalBaseMaximumMana", "MaximumMana"),
        Def("mana_pct", "Мана %", "Mana %", "%", 65, StatCategory.Defence, ProjectionKind.ManaPct, 0.02f, false,
            "MaximumManaPct"),
        Def("energy_shield", "ES", "ES", "", 20, StatCategory.Defence, ProjectionKind.EnergyShieldFlat, 0.05f, true,
            "BaseMaximumEnergyShield", "TotalBaseMaximumEnergyShield", "MaximumEnergyShield", "LocalEnergyShield"),
        Def("energy_shield_pct", "ES %", "ES %", "%", 60, StatCategory.Defence, ProjectionKind.EnergyShieldPct, 0.03f, true,
            "MaximumEnergyShieldPct", "LocalEnergyShieldPct"),

        Def("armour", "Броня", "Armour", "", 25, StatCategory.Defence, ProjectionKind.None, 0.005f, true,
            "BasePhysicalDamageReductionRating", "PhysicalDamageReductionRating", "LocalBasePhysicalDamageReductionRating"),
        Def("armour_pct", "Броня %", "Armour %", "%", 70, StatCategory.Defence, ProjectionKind.None, 0.01f, true,
            "PhysicalDamageReductionRatingPct", "LocalPhysicalDamageReductionRatingPct"),
        Def("evasion", "Уклон.", "Evasion", "", 26, StatCategory.Defence, ProjectionKind.None, 0.005f, true,
            "BaseEvasionRating", "EvasionRating", "LocalBaseEvasionRating"),
        Def("evasion_pct", "Уклон. %", "Evasion %", "%", 71, StatCategory.Defence, ProjectionKind.None, 0.01f, true,
            "EvasionRatingPct", "LocalEvasionRatingPct"),

        Def("strength", "Сила", "Str", "", 31, StatCategory.Attribute, ProjectionKind.Strength, 0.05f, false,
            "AdditionalStrength", "BaseStrength", "Strength"),
        Def("dexterity", "Ловк.", "Dex", "", 32, StatCategory.Attribute, ProjectionKind.Dexterity, 0.05f, false,
            "AdditionalDexterity", "BaseDexterity", "Dexterity"),
        Def("intelligence", "Инт.", "Int", "", 33, StatCategory.Attribute, ProjectionKind.Intelligence, 0.05f, false,
            "AdditionalIntelligence", "BaseIntelligence", "Intelligence"),
        Def("all_attributes", "Все атрибуты", "All Attr", "", 34, StatCategory.Attribute, ProjectionKind.AllAttributes, 0.05f, false,
            "AdditionalAllAttributes"),

        Def("movement_speed", "Скорость", "Move", "%", 15, StatCategory.Speed, ProjectionKind.None, 0.20f, false,
            "BaseMovementVelocityPct", "MovementVelocityPct"),
        Def("attack_speed", "Атак. скор.", "Atk Speed", "%", 40, StatCategory.Speed, ProjectionKind.None, 0.10f, false,
            "LocalAttackSpeedPct", "AttackSpeedPct"),
        Def("cast_speed", "Скор. каста", "Cast Speed", "%", 41, StatCategory.Speed, ProjectionKind.None, 0.10f, false,
            "BaseCastSpeedPct", "CastSpeedPct"),

        Def("suppression", "Подавление", "Suppress", "%", 12, StatCategory.Defence, ProjectionKind.None, 0.50f, false,
            "BaseSpellSuppressionChancePct", "SpellSuppressionChancePct", "CombinedSpellSuppressionChancePct"),
        Def("block", "Блок", "Block", "%", 13, StatCategory.Defence, ProjectionKind.None, 0.30f, false,
            "AdditionalBlockPct", "BlockChancePct", "CombinedBaseBlockPct", "ShieldBlockPct"),
        Def("spell_block", "Спелл блок", "Spell Block", "%", 14, StatCategory.Defence, ProjectionKind.None, 0.30f, false,
            "BaseSpellBlockPct", "AdditionalSpellBlockPct", "SpellBlockPct", "CombinedBaseSpellBlockPct")
    };

    public static readonly HashSet<GameStat> KnownStats = BuildKnownStats();

    public static ResistanceElement GetResistanceElement(ProjectionKind projection)
    {
        return projection switch
        {
            ProjectionKind.FireResistance or ProjectionKind.MaxFireResistance => ResistanceElement.Fire,
            ProjectionKind.ColdResistance or ProjectionKind.MaxColdResistance => ResistanceElement.Cold,
            ProjectionKind.LightningResistance or ProjectionKind.MaxLightningResistance => ResistanceElement.Lightning,
            ProjectionKind.ChaosResistance or ProjectionKind.MaxChaosResistance => ResistanceElement.Chaos,
            _ => ResistanceElement.None
        };
    }

    private static StatDefinition Def(
        string id,
        string labelRu,
        string labelEn,
        string unit,
        int priority,
        StatCategory category,
        ProjectionKind projection,
        float scoreWeight,
        bool skipWhenLocalDefenceTotals,
        params string[] aliases)
    {
        return new StatDefinition(id, labelRu, labelEn, unit, priority, category, projection, scoreWeight,
            skipWhenLocalDefenceTotals, aliases);
    }

    private static HashSet<GameStat> BuildKnownStats()
    {
        var result = new HashSet<GameStat>();
        foreach (var definition in Definitions)
        {
            foreach (var stat in definition.Stats)
                result.Add(stat);
        }

        return result;
    }
}

public sealed class StatLabelProvider
{
    private readonly StatsDat _stats;
    private readonly Dictionary<GameStat, StatLabelDescription> _cache = new();

    public StatLabelProvider(GameController gameController)
    {
        _stats = gameController?.Files?.Stats;
    }

    public StatLabelDescription Describe(GameStat stat)
    {
        if (_cache.TryGetValue(stat, out var cached))
            return cached;

        StatsDat.StatRecord record = null;
        if (_stats != null)
            _stats.recordsById.TryGetValue((int)stat, out record);

        var label = record?.UserFriendlyName;
        if (string.IsNullOrWhiteSpace(label))
            label = Beautify(record?.Key ?? stat.ToString());

        var unit = record?.Type is StatType.Percents or StatType.Precents5 ? "%" : string.Empty;
        var isFlag = record?.Type == StatType.Boolean;

        var description = new StatLabelDescription(label, unit, isFlag);
        _cache[stat] = description;
        return description;
    }

    private static string Beautify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Unknown";

        var text = value.Replace('_', ' ').Trim();
        if (text.EndsWith(" %", StringComparison.Ordinal))
        {
            text = text[..^2].TrimEnd();
        }

        if (text.Length == 0)
            return "Unknown";

        return char.ToUpperInvariant(text[0]) + text[1..];
    }
}

public readonly record struct StatLabelDescription(string Label, string Unit, bool IsFlag)
{
    public string FormatValue(int value)
    {
        var text = value > 0 ? $"+{value}" : value.ToString();
        return text + Unit;
    }
}
