using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Enums;

namespace FastItemViewer.Items;

public sealed class PlayerContext
{
    public int Level { get; private set; }
    public int Strength { get; private set; }
    public int Dexterity { get; private set; }
    public int Intelligence { get; private set; }

    public int MaxLife { get; private set; }
    public int MaxMana { get; private set; }
    public int MaxEnergyShield { get; private set; }

    public int FireResistance { get; private set; }
    public int ColdResistance { get; private set; }
    public int LightningResistance { get; private set; }
    public int ChaosResistance { get; private set; }

    public int MaxFireResistance { get; private set; }
    public int MaxColdResistance { get; private set; }
    public int MaxLightningResistance { get; private set; }
    public int MaxChaosResistance { get; private set; }

    public bool HasResistances { get; private set; }

    public static PlayerContext Capture(GameController gameController)
    {
        var context = new PlayerContext();
        var playerEntity = gameController?.Player;
        if (playerEntity == null)
            return context;

        if (playerEntity.TryGetComponent(out Player player))
        {
            context.Level = player.Level;
            context.Strength = player.Strength;
            context.Dexterity = player.Dexterity;
            context.Intelligence = player.Intelligence;
        }

        if (playerEntity.TryGetComponent(out Life life))
        {
            context.MaxLife = life.MaxHP;
            context.MaxMana = life.MaxMana;
            context.MaxEnergyShield = life.MaxES;
        }

        if (playerEntity.TryGetComponent(out ExileCore.PoEMemory.Components.Stats stats) &&
            stats.StatDictionary != null)
        {
            var dictionary = stats.StatDictionary;
            var foundAny = false;

            if (dictionary.TryGetValue(GameStat.FireDamageResistancePct, out var fire))
            {
                context.FireResistance = fire;
                foundAny = true;
            }

            if (dictionary.TryGetValue(GameStat.ColdDamageResistancePct, out var cold))
            {
                context.ColdResistance = cold;
                foundAny = true;
            }

            if (dictionary.TryGetValue(GameStat.LightningDamageResistancePct, out var lightning))
            {
                context.LightningResistance = lightning;
                foundAny = true;
            }

            if (dictionary.TryGetValue(GameStat.ChaosDamageResistancePct, out var chaos))
            {
                context.ChaosResistance = chaos;
                foundAny = true;
            }

            dictionary.TryGetValue(GameStat.MaximumFireDamageResistancePct, out var maxFire);
            dictionary.TryGetValue(GameStat.MaximumColdDamageResistancePct, out var maxCold);
            dictionary.TryGetValue(GameStat.MaximumLightningDamageResistancePct, out var maxLightning);
            dictionary.TryGetValue(GameStat.MaximumChaosDamageResistancePct, out var maxChaos);

            context.MaxFireResistance = maxFire;
            context.MaxColdResistance = maxCold;
            context.MaxLightningResistance = maxLightning;
            context.MaxChaosResistance = maxChaos;

            context.HasResistances = foundAny;
        }

        return context;
    }
}
