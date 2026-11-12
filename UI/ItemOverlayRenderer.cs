using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Enums;
using FastItemViewer.Items;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace FastItemViewer.UI;

public static class ItemOverlayRenderer
{
    public static void Render(
        FastItemViewer plugin,
        IList<NormalInventoryItem> visibleItems,
        Dictionary<long, ItemComparison> comparisons,
        PlayerContext player)
    {
        if (visibleItems == null || visibleItems.Count == 0 || comparisons.Count == 0)
            return;

        var settings = plugin.Settings;
        var graphics = plugin.Graphics;
        var hoveredItem = FindHoveredItem(plugin, visibleItems);
        var hoveredAddress = hoveredItem?.Item?.Address ?? 0;
        var fontSize = settings.FontSize.Value;

        foreach (var item in visibleItems)
        {
            try
            {
                if (item?.Item == null)
                    continue;

                if (!comparisons.TryGetValue(item.Item.Address, out var comparison))
                    continue;

                var rect = item.GetClientRectCache;
                if (rect.Width <= 0 || rect.Height <= 0)
                    continue;

                if (comparison.IsUnidentified)
                {
                    DrawUnidentified(graphics, rect, fontSize);
                    continue;
                }

                if (!comparison.HasMeaningfulChanges)
                    continue;

                if (settings.ShowItemFrame.Value && comparison.IsUpgrade.HasValue)
                {
                    var frameColor = comparison.IsUpgrade.Value
                        ? settings.FrameUpgradeColor.Value
                        : settings.FrameDowngradeColor.Value;

                    graphics.DrawFrame(rect, frameColor, 2);
                }

                if (!settings.ShowOnItems.Value || item.Item.Address == hoveredAddress)
                    continue;

                DrawCompact(plugin, rect, comparison, fontSize);
            }
            catch
            {
                // A single malformed item should not break the whole inventory overlay.
            }
        }

        if (settings.ShowHoverPanel.Value &&
            hoveredItem?.Item != null &&
            comparisons.TryGetValue(hoveredItem.Item.Address, out var hoveredComparison))
        {
            try
            {
                DrawHoverPanel(plugin, hoveredItem, hoveredComparison, player);
            }
            catch
            {
                // Ignore drawing errors for the hover panel.
            }
        }
    }

    private static void DrawCompact(
        FastItemViewer plugin,
        RectangleF rect,
        ItemComparison comparison,
        int fontSize)
    {
        var settings = plugin.Settings;
        var graphics = plugin.Graphics;
        var maxLines = settings.MaxLinesOnItem.Value;

        var targets = GetCompactTargets(comparison, settings.ShowBothSlotComparisons.Value);

        if (targets == null || targets.Count == 0)
            return;

        var perTarget = targets.Count > 1
            ? Math.Max(1, maxLines / targets.Count)
            : maxLines;

        var lines = new List<OverlayLine>();

        foreach (var target in targets)
        {
            if (target == null)
                continue;

            var targetLines = target.Lines
                .Where(x => !x.IsWarning)
                .Take(perTarget)
                .ToList();

            var warning = target.Lines.FirstOrDefault(x => x.IsWarning);
            if (warning != null)
                targetLines.Insert(0, warning);

            var prefix = targets.Count > 1 ? target.ShortLabel + " " : string.Empty;

            foreach (var line in targetLines)
            {
                lines.Add(new OverlayLine(
                    prefix + line.CompactText,
                    line.IsWarning ? settings.WarningColor.Value : GetDeltaColor(settings, line.Delta)));
            }
        }

        if (lines.Count == 0)
        {
            foreach (var flag in comparison.Targets.SelectMany(x => x.Flags).Take(maxLines))
            {
                lines.Add(new OverlayLine(
                    (flag.Gained ? "+ " : "- ") + flag.Label,
                    flag.Gained ? settings.PositiveColor.Value : settings.NegativeColor.Value));
            }
        }

        if (lines.Count == 0)
            return;

        if (lines.Count > maxLines)
            lines = lines.Take(maxLines).ToList();

        using (graphics.SetTextScale(GetTextScale(graphics, fontSize)))
        {
            var width = 0f;
            foreach (var line in lines)
                width = Math.Max(width, graphics.MeasureText(line.Text).X);

            var lineHeight = Math.Max(fontSize + 1f, graphics.MeasureText("Ag").Y + 1f);
            var background = new RectangleF(
                rect.X + 1,
                rect.Y + 1,
                width + 6,
                lines.Count * lineHeight + 4);

            graphics.DrawBox(background, new Color(0, 0, 0, settings.BackgroundAlpha.Value));

            for (var i = 0; i < lines.Count; i++)
            {
                graphics.DrawText(
                    lines[i].Text,
                    new Vector2(background.X + 3, background.Y + 2 + i * lineHeight),
                    lines[i].Color,
                    FontAlign.Left);
            }
        }
    }

    private static List<SlotComparison> GetCompactTargets(ItemComparison comparison, bool showBoth)
    {
        if (comparison?.Targets == null || comparison.Targets.Count == 0)
            return new List<SlotComparison>();

        var emptyTargets = comparison.Targets.Where(x => x.EmptySlot).ToList();
        if (emptyTargets.Count > 0)
        {
            // If one of the slots is empty, this is the best place to put the item:
            // show that comparison first and do not mix in the occupied slot's red lines.
            return emptyTargets.Count == comparison.Targets.Count
                ? new List<SlotComparison> { emptyTargets[0] }
                : emptyTargets;
        }

        if (showBoth)
            return comparison.Targets;

        return new List<SlotComparison> { comparison.BestTarget ?? comparison.Targets[0] };
    }

    private static void DrawHoverPanel(
        FastItemViewer plugin,
        NormalInventoryItem item,
        ItemComparison comparison,
        PlayerContext player)
    {
        var settings = plugin.Settings;
        var graphics = plugin.Graphics;
        var fontSize = settings.HoverFontSize.Value;
        var lines = new List<PanelLine>();

        var itemName = string.IsNullOrWhiteSpace(comparison.Candidate.DisplayName)
            ? comparison.Candidate.BaseName
            : comparison.Candidate.DisplayName;

        lines.Add(new PanelLine(itemName, settings.HeaderColor.Value));
        lines.Add(new PanelLine(
            $"[{comparison.Candidate.ClassName}, ilvl {comparison.Candidate.ItemLevel}]",
            settings.NeutralColor.Value));

        if (comparison.IsUnidentified)
        {
            lines.Add(new PanelLine(
                "Item is unidentified",
                settings.WarningColor.Value));
        }
        else
        {
            foreach (var target in comparison.Targets)
            {
                lines.Add(new PanelLine(string.Empty, settings.NeutralColor.Value));
                lines.Add(new PanelLine(
                    target == comparison.BestTarget
                        ? $"-- {target.Label} (best) --"
                        : $"-- {target.Label} --",
                    settings.HeaderColor.Value));

                lines.Add(new PanelLine(
                    target.EmptySlot
                        ? "empty slot"
                        : $"vs {target.ReplacedNames}",
                    settings.NeutralColor.Value));

                foreach (var line in target.Lines)
                {
                    lines.Add(new PanelLine(
                        line.DetailText,
                        line.IsWarning ? settings.WarningColor.Value : GetDeltaColor(settings, line.Delta)));
                }

                foreach (var flag in target.Flags)
                {
                    lines.Add(new PanelLine(
                        (flag.Gained ? "+ " : "- ") + flag.Label,
                        flag.Gained ? settings.PositiveColor.Value : settings.NegativeColor.Value));
                }

                var projections = BuildProjections(plugin, target, player);
                if (projections.Count > 0)
                {
                    lines.Add(new PanelLine(
                        "Total:",
                        settings.HeaderColor.Value));

                    lines.AddRange(projections);
                }
            }
        }

        if (lines.Count == 0)
            return;

        if (lines.Count > 200)
            lines = lines.Take(200).ToList();

        var itemRect = item.GetClientRectCache;
        var windowRect = plugin.GameController.Window.GetWindowRectangleTimeCache;
        var screenWidth = windowRect.Width > 100 ? windowRect.Width : 1920f;
        var screenHeight = windowRect.Height > 100 ? windowRect.Height : 1080f;
        var maxTextWidth = Math.Max(200f, Math.Min(settings.HoverMaxWidth.Value, screenWidth - 40f));

        using (graphics.SetTextScale(GetTextScale(graphics, fontSize)))
        {
            var lineHeight = Math.Max(fontSize + 1f, graphics.MeasureText("Ag").Y + 1f);

            var wrapped = new List<PanelLine>();
            foreach (var line in lines)
            {
                foreach (var piece in TextWrapper.Wrap(graphics, new[] { line.Text }, maxTextWidth))
                    wrapped.Add(new PanelLine(piece, line.Color));
            }

            var maxPanelLines = Math.Max(1, (int)((screenHeight - 8f) / lineHeight));
            if (wrapped.Count > maxPanelLines)
            {
                wrapped = wrapped.Take(Math.Max(0, maxPanelLines - 1)).ToList();
                wrapped.Add(new PanelLine("... truncated ...", settings.WarningColor.Value));
            }

            const float padding = 7f;
            var width = 0f;
            foreach (var line in wrapped)
                width = Math.Max(width, graphics.MeasureText(line.Text).X);

            var panelWidth = Math.Min(screenWidth - 8f, Math.Max(180f, width + padding * 2));
            var panelHeight = wrapped.Count * lineHeight + padding * 2;

            var x = itemRect.Left - panelWidth - 8;
            if (x < 4)
                x = itemRect.Right + 8;

            x = Math.Clamp(x, 4, Math.Max(4, screenWidth - panelWidth - 4));
            var y = Math.Clamp(itemRect.Top, 4, Math.Max(4, screenHeight - panelHeight - 4));

            var panelRect = new RectangleF(x, y, panelWidth, panelHeight);
            graphics.DrawBox(panelRect, settings.PanelBackgroundColor.Value);
            graphics.DrawFrame(panelRect, settings.HeaderColor.Value, 1);

            for (var i = 0; i < wrapped.Count; i++)
            {
                graphics.DrawText(
                    wrapped[i].Text,
                    new Vector2(panelRect.X + padding, panelRect.Y + padding + i * lineHeight),
                    wrapped[i].Color,
                    FontAlign.Left);
            }
        }
    }

    private static List<PanelLine> BuildProjections(
        FastItemViewer plugin,
        SlotComparison target,
        PlayerContext player)
    {
        var settings = plugin.Settings;
        var result = new List<PanelLine>();

        var newContribution = target.NewContribution;
        var oldContribution = target.OldContribution;

        int Delta(ProjectionKind kind) =>
            newContribution.GetValueOrDefault(kind) - oldContribution.GetValueOrDefault(kind);

        if (settings.ShowProjectedResistances.Value && player.HasResistances)
        {
            AddResistance(
                result, settings,
                "Fire",
                player.FireResistance,
                Delta(ProjectionKind.FireResistance),
                player.MaxFireResistance > 0 ? player.MaxFireResistance : 75);

            AddResistance(
                result, settings,
                "Cold",
                player.ColdResistance,
                Delta(ProjectionKind.ColdResistance),
                player.MaxColdResistance > 0 ? player.MaxColdResistance : 75);

            AddResistance(
                result, settings,
                "Lightning",
                player.LightningResistance,
                Delta(ProjectionKind.LightningResistance),
                player.MaxLightningResistance > 0 ? player.MaxLightningResistance : 75);

            AddResistance(
                result, settings,
                "Chaos",
                player.ChaosResistance,
                Delta(ProjectionKind.ChaosResistance),
                player.MaxChaosResistance > 0 ? player.MaxChaosResistance : 60);

            AddMaxResistance(result, settings, "Max Fire",
                player.MaxFireResistance, Delta(ProjectionKind.MaxFireResistance));
            AddMaxResistance(result, settings, "Max Cold",
                player.MaxColdResistance, Delta(ProjectionKind.MaxColdResistance));
            AddMaxResistance(result, settings, "Max Lightning",
                player.MaxLightningResistance, Delta(ProjectionKind.MaxLightningResistance));
            AddMaxResistance(result, settings, "Max Chaos",
                player.MaxChaosResistance, Delta(ProjectionKind.MaxChaosResistance));
        }

        if (settings.ShowProjectedLife.Value && player.MaxLife > 0)
        {
            var flatDelta = Delta(ProjectionKind.LifeFlat);
            var pctDelta = Delta(ProjectionKind.LifePct);

            // Percentage life on an item is not additive with the passive tree,
            // so we only show a projected total when the percentage did not change.
            if (flatDelta != 0 && pctDelta == 0)
                AddProjectedTotal(result, settings, "HP", player.MaxLife, player.MaxLife + flatDelta, false);
        }

        if (settings.ShowProjectedMana.Value && player.MaxMana > 0)
        {
            var delta = Delta(ProjectionKind.ManaFlat);
            if (delta != 0)
                AddProjectedTotal(result, settings, "Mana", player.MaxMana, player.MaxMana + delta, false);
        }

        if (settings.ShowProjectedEnergyShield.Value && player.MaxEnergyShield > 0)
        {
            var delta = Delta(ProjectionKind.EnergyShieldFlat);
            if (delta != 0)
                AddProjectedTotal(result, settings, "ES", player.MaxEnergyShield, player.MaxEnergyShield + delta, false);
        }

        if (settings.ShowProjectedAttributes.Value)
        {
            AddAttribute(result, settings, "Str", player.Strength,
                Delta(ProjectionKind.Strength));
            AddAttribute(result, settings, "Dex", player.Dexterity,
                Delta(ProjectionKind.Dexterity));
            AddAttribute(result, settings, "Int", player.Intelligence,
                Delta(ProjectionKind.Intelligence));
        }

        return result;
    }

    private static void AddResistance(
        List<PanelLine> lines,
        FastItemViewerSettings settings,
        string label,
        int before,
        int delta,
        int cap)
    {
        if (delta == 0)
            return;

        var after = before + delta;
        var color = after < cap
            ? settings.NegativeColor.Value
            : delta > 0
                ? settings.PositiveColor.Value
                : settings.NeutralColor.Value;

        lines.Add(new PanelLine($"{label}: {before} -> {after} ({FormatSigned(delta)}) cap {cap}", color));
    }

    private static void AddMaxResistance(
        List<PanelLine> lines,
        FastItemViewerSettings settings,
        string label,
        int before,
        int delta)
    {
        if (delta == 0)
            return;

        lines.Add(new PanelLine(
            $"{label}: {before} -> {before + delta} ({FormatSigned(delta)})",
            GetDeltaColor(settings, delta)));
    }

    private static void AddProjectedTotal(
        List<PanelLine> lines,
        FastItemViewerSettings settings,
        string label,
        int before,
        int after,
        bool approximate)
    {
        var delta = after - before;
        if (delta == 0)
            return;

        lines.Add(new PanelLine(
            $"{(approximate ? "~" : string.Empty)}{label}: {before} -> {after} ({FormatSigned(delta)})",
            GetDeltaColor(settings, delta)));
    }

    private static void AddAttribute(
        List<PanelLine> lines,
        FastItemViewerSettings settings,
        string label,
        int before,
        int delta)
    {
        if (delta == 0)
            return;

        lines.Add(new PanelLine(
            $"{label}: {before} -> {before + delta} ({FormatSigned(delta)})",
            GetDeltaColor(settings, delta)));
    }

    private static void DrawUnidentified(Graphics graphics, RectangleF rect, int fontSize)
    {
        using (graphics.SetTextScale(GetTextScale(graphics, fontSize)))
        {
            graphics.DrawText(
                "?",
                new Vector2(rect.X + 4, rect.Y + 2),
                Color.Yellow,
                FontAlign.Left);
        }
    }

    private static float GetTextScale(Graphics graphics, int fontSize)
    {
        var baseSize = graphics.Font.Size;
        if (baseSize <= 0)
            return 1f;

        return Math.Clamp(fontSize / (float)baseSize, 0.25f, 4f);
    }

    public static NormalInventoryItem FindHoveredItem(
        FastItemViewer plugin,
        IList<NormalInventoryItem> visibleItems)
    {
        var ingameState = plugin.GameController?.IngameState;
        if (ingameState == null || visibleItems == null)
            return null;

        var mouse = new SharpDX.Vector2(ingameState.MousePosX, ingameState.MousePosY);

        foreach (var item in visibleItems)
        {
            if (item?.Item == null)
                continue;

            var rect = item.GetClientRectCache;
            if (rect.Width <= 0 || rect.Height <= 0)
                continue;

            if (rect.Contains(mouse))
                return item;
        }

        return null;
    }

    private static Color GetDeltaColor(FastItemViewerSettings settings, int delta)
    {
        return delta > 0
            ? settings.PositiveColor.Value
            : delta < 0
                ? settings.NegativeColor.Value
                : settings.NeutralColor.Value;
    }

    private static string FormatSigned(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    private readonly record struct OverlayLine(string Text, Color Color);
    private readonly record struct PanelLine(string Text, Color Color);
}
