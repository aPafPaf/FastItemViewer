using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.Shared.Enums;
using FastItemViewer.Items;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace FastItemViewer.UI;

public static class ItemDebugOverlay
{
    private static readonly Dictionary<long, (long Hash, List<string> Lines)> Cache = new();

    public static void Render(FastItemViewer plugin, IList<NormalInventoryItem> visibleItems)
    {
        var hovered = ItemOverlayRenderer.FindHoveredItem(plugin, visibleItems);
        var graphics = plugin.Graphics;
        var settings = plugin.Settings;
        var fontSize = settings.DebugFontSize.Value;

        var lines = new List<string> { "FastItemViewer debug" };

        if (hovered?.Item == null)
        {
            lines.Add("Hover an inventory item to see its stats.");
        }
        else
        {
            var item = hovered.Item;
            var mods = item.GetComponent<Mods>();
            var hash = mods?.Hash ?? 0;

            if (!Cache.TryGetValue(item.Address, out var cached) || cached.Hash != hash)
            {
                var snapshot = ItemStatsBuilder.Build(plugin.GameController, PlayerContext.Capture(plugin.GameController), item);
                cached = (hash, BuildDumpLines(plugin.GameController, snapshot));
                Cache[item.Address] = cached;
            }

            lines.AddRange(cached.Lines);
        }

        if (lines.Count > 250)
            lines = lines.Take(250).ToList();

        var window = plugin.GameController.Window.GetWindowRectangleTimeCache;
        var screenWidth = window.Width > 100 ? window.Width : 1920f;
        var screenHeight = window.Height > 100 ? window.Height : 1080f;
        var maxWidth = Math.Max(200f, Math.Min(settings.DebugMaxWidth.Value, screenWidth - 80f));

        using (graphics.SetTextScale(GetTextScale(graphics, fontSize)))
        {
            var lineHeight = Math.Max(fontSize + 1f, graphics.MeasureText("Ag").Y + 1f);

            var wrapped = new List<(string Text, Color Color)>();
            for (var i = 0; i < lines.Count; i++)
            {
                var color = i == 0 ? settings.HeaderColor.Value : settings.NeutralColor.Value;
                foreach (var piece in TextWrapper.Wrap(graphics, new[] { lines[i] }, maxWidth))
                    wrapped.Add((piece, color));
            }

            var maxLines = Math.Max(1, (int)((screenHeight - 140f) / lineHeight));
            if (wrapped.Count > maxLines)
            {
                wrapped = wrapped.Take(Math.Max(0, maxLines - 1)).ToList();
                wrapped.Add(("... truncated ...", settings.WarningColor.Value));
            }

            var width = 0f;
            foreach (var line in wrapped)
                width = Math.Max(width, graphics.MeasureText(line.Text).X);

            var panelWidth = Math.Min(maxWidth + 12f, Math.Max(180f, width + 12f));
            var panelHeight = wrapped.Count * lineHeight + 10f;
            var panelY = Math.Max(4f, Math.Min(120f, screenHeight - panelHeight - 10f));

            var panel = new RectangleF(40, panelY, panelWidth, panelHeight);

            graphics.DrawBox(panel, new Color(0, 0, 0, settings.BackgroundAlpha.Value));
            graphics.DrawFrame(panel, settings.HeaderColor.Value, 1);

            for (var i = 0; i < wrapped.Count; i++)
            {
                graphics.DrawText(
                    wrapped[i].Text,
                    new Vector2(panel.X + 5, panel.Y + 5 + i * lineHeight),
                    wrapped[i].Color,
                    FontAlign.Left);
            }
        }
    }

    private static float GetTextScale(Graphics graphics, int fontSize)
    {
        var baseSize = graphics.Font.Size;
        if (baseSize <= 0)
            return 1f;

        return Math.Clamp(fontSize / (float)baseSize, 0.25f, 4f);
    }

    public static List<string> BuildDumpLines(GameController gameController, ItemSnapshot snapshot)
    {
        var lines = new List<string>();

        if (snapshot?.Entity == null)
        {
            lines.Add("Item is null.");
            return lines;
        }

        lines.Add($"Name: {snapshot.DisplayName}");
        lines.Add($"Base: {snapshot.BaseName}");
        lines.Add($"Class: {snapshot.ClassName}");
        lines.Add($"Identified: {snapshot.Identified}  ilvl: {snapshot.ItemLevel}  quality: {snapshot.Quality}  sockets: {snapshot.Sockets}");
        lines.Add($"Armour: {snapshot.Armour}  Evasion: {snapshot.Evasion}  ES: {snapshot.EnergyShield}");
        lines.Add($"Weapon: {snapshot.IsWeapon}  DPS: {snapshot.Dps:0.0}  APS: {snapshot.Aps:0.00}  Crit: {snapshot.Crit:0.0}%");
        lines.Add($"Requirements: met={snapshot.RequirementsMet} {snapshot.RequirementsText}");

        var item = snapshot.Entity;

        if (item.TryGetComponent(out Mods mods))
        {
            lines.Add(string.Empty);
            lines.Add("-- HumanStats --");
            if (mods.HumanStats is { Count: > 0 })
                AddLimited(lines, mods.HumanStats);
            else
                lines.Add("(empty)");

            if (mods.HumanImpStats is { Count: > 0 })
            {
                lines.Add("-- HumanImpStats --");
                AddLimited(lines, mods.HumanImpStats);
            }

            lines.Add(string.Empty);
            lines.Add("-- ItemMods --");

            try
            {
                foreach (var mod in mods.ItemMods.Take(30))
                {
                    if (mod == null)
                        continue;

                    var values = string.Join(",", mod.Values ?? new List<int>());
                    var stats = mod.ModRecord?.StatNames?
                        .Where(x => x != null)
                        .Select(x => $"{x.Key}->{x.MatchingStat}")
                        .ToList() ?? new List<string>();

                    lines.Add($"MOD {mod.RawName} [{mod.Group}] v={values} : {string.Join(" | ", stats)}");
                }
            }
            catch (Exception e)
            {
                lines.Add($"ItemMods error: {e.Message}");
            }
        }

        if (item.TryGetComponent(out LocalStats localStats) && localStats.StatDictionary != null)
        {
            lines.Add(string.Empty);
            lines.Add("-- LocalStats --");

            if (localStats.StatDictionary.Count == 0)
                lines.Add("(empty)");
            else
                foreach (var pair in localStats.StatDictionary.OrderBy(x => x.Key.ToString()))
                    lines.Add($"{pair.Key} = {pair.Value}");
        }

        lines.Add(string.Empty);
        lines.Add("-- Aggregated stats --");

        var statsDat = gameController?.Files?.Stats;
        foreach (var pair in snapshot.Stats.OrderBy(x => x.Key.ToString()))
        {
            StatsDat.StatRecord record = null;
            statsDat?.recordsById.TryGetValue((int)pair.Key, out record);

            lines.Add($"{pair.Key} = {pair.Value}  key={record?.Key}  ufn={record?.UserFriendlyName}  type={record?.Type}");
        }

        return lines;
    }

    private static void AddLimited(List<string> target, List<string> source)
    {
        if (source == null)
            return;

        foreach (var line in source.Take(30))
            target.Add(line);
    }
}
