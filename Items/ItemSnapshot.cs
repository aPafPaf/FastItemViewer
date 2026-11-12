using System.Collections.Generic;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;

namespace FastItemViewer.Items;

public sealed class ItemSnapshot
{
    public Entity Entity { get; set; }
    public long Address => Entity?.Address ?? 0;

    public string ClassName { get; set; } = string.Empty;
    public string BaseName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public bool Identified { get; set; }
    public int ItemLevel { get; set; }
    public int RequiredLevel { get; set; }

    public Dictionary<GameStat, int> Stats { get; } = new();

    public int Armour { get; set; }
    public int Evasion { get; set; }
    public int EnergyShield { get; set; }
    public bool HasLocalDefence { get; set; }

    public bool IsWeapon { get; set; }
    public float Dps { get; set; }
    public float Aps { get; set; }
    public float Crit { get; set; }

    public int Quality { get; set; }
    public int Sockets { get; set; }

    public bool RequirementsMet { get; set; } = true;
    public string RequirementsText { get; set; } = string.Empty;

    public int GetStat(GameStat stat)
    {
        return Stats.TryGetValue(stat, out var value) ? value : 0;
    }
}
