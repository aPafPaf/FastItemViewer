using System.Collections.Generic;

namespace FastItemViewer.Items;

public sealed class ItemComparison
{
    public ItemSnapshot Candidate { get; set; }
    public EquipSlotKind Kind { get; set; }

    public bool IsUnidentified { get; set; }
    public List<SlotComparison> Targets { get; } = new();
    public SlotComparison BestTarget { get; set; }

    public bool HasMeaningfulChanges { get; set; }
    public bool? IsUpgrade { get; set; }
    public int Score { get; set; }
}

public sealed class SlotComparison
{
    public string Label { get; set; } = string.Empty;
    public string ShortLabel { get; set; } = string.Empty;
    public bool EmptySlot { get; set; }
    public List<ItemSnapshot> Replaced { get; } = new();
    public List<StatLine> Lines { get; } = new();
    public List<FlagLine> Flags { get; } = new();

    public Dictionary<ProjectionKind, int> NewContribution { get; } = new();
    public Dictionary<ProjectionKind, int> OldContribution { get; } = new();

    public int Score { get; set; }
    public bool RequirementsMet { get; set; } = true;
    public string RequirementsText { get; set; } = string.Empty;

    public string ReplacedNames =>
        Replaced.Count == 0
            ? "Empty slot"
            : string.Join(" + ", Replaced.ConvertAll(x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.BaseName : x.DisplayName));
}

public sealed class StatLine
{
    public string Label { get; set; } = string.Empty;
    public string ValueText { get; set; } = string.Empty;
    public string CompactText { get; set; } = string.Empty;
    public string DetailText { get; set; } = string.Empty;

    public int Delta { get; set; }
    public int OldValue { get; set; }
    public int NewValue { get; set; }
    public int Priority { get; set; }

    public bool IsTotal { get; set; }
    public bool IsWarning { get; set; }
}

public sealed class FlagLine
{
    public string Label { get; set; } = string.Empty;
    public bool Gained { get; set; }
}
