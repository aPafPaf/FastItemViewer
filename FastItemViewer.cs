using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using FastItemViewer.Items;
using FastItemViewer.UI;

namespace FastItemViewer;

public class FastItemViewer : BaseSettingsPlugin<FastItemViewerSettings>
{
    private readonly Dictionary<long, ItemSnapshot> _snapshotCache = new();
    private readonly Dictionary<long, ItemComparison> _comparisons = new();

    private int _lastInventoryCounter = int.MinValue;
    private int _lastEquipmentHash = int.MinValue;
    private int _lastOptionsHash = int.MinValue;
    private bool _dumpRequested;

    public override bool Initialise()
    {
        Settings.DumpHoveredItem.OnPressed = () => _dumpRequested = true;
        LogMessage("[FastItemViewer] Loaded. Open the inventory to see equip comparisons.", 5f);
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        _comparisons.Clear();
        _snapshotCache.Clear();
        _lastInventoryCounter = int.MinValue;
        _lastEquipmentHash = int.MinValue;
    }

    public override Job Tick()
    {
        if (!Settings.Enable.Value)
            return null;

        var ingameState = GameController?.IngameState;
        if (ingameState == null)
            return null;

        var inventoryPanel = ingameState.IngameUi?.InventoryPanel;
        if (inventoryPanel == null)
            return null;

        if (Settings.OnlyWhenInventoryOpen.Value && !inventoryPanel.IsVisible)
            return null;

        var serverData = ingameState.ServerData;
        if (serverData?.PlayerInventories == null)
            return null;

        var baseItemTypes = GameController?.Files?.BaseItemTypes;
        if (baseItemTypes == null || baseItemTypes.Contents.Count == 0)
            return null;

        var mainHolder = serverData.PlayerInventories
            .FirstOrDefault(x => x.TypeId == InventoryNameE.MainInventory1);

        var mainInventory = mainHolder?.Inventory;
        if (mainInventory == null)
            return null;

        var inventoryCounter = mainInventory.ServerRequestCounter;
        var equipmentHash = ComputeEquipmentHash(serverData);
        var optionsHash = HashCode.Combine(
            Settings.ShowWeaponDps.Value,
            Settings.ShowUnknownStats.Value);

        if (inventoryCounter != _lastInventoryCounter ||
            equipmentHash != _lastEquipmentHash ||
            optionsHash != _lastOptionsHash ||
            _lastInventoryCounter == int.MinValue)
        {
            try
            {
                Rebuild(serverData, mainInventory);
            }
            catch (Exception e)
            {
                LogError($"[FastItemViewer] Rebuild failed: {e.Message}", 5f);
            }

            _lastInventoryCounter = inventoryCounter;
            _lastEquipmentHash = equipmentHash;
            _lastOptionsHash = optionsHash;
        }

        if (_dumpRequested)
        {
            _dumpRequested = false;
            DumpHoveredItem();
        }

        return null;
    }

    public override void Render()
    {
        if (!Settings.Enable.Value)
            return;

        var ingameState = GameController?.IngameState;
        if (ingameState == null)
            return;

        var inventoryPanel = ingameState.IngameUi?.InventoryPanel;
        if (inventoryPanel == null || !inventoryPanel.IsVisible)
            return;

        var inventory = inventoryPanel[InventoryIndex.PlayerInventory];
        var visibleItems = inventory?.VisibleInventoryItems;
        if (visibleItems == null || visibleItems.Count == 0)
            return;

        var player = PlayerContext.Capture(GameController);
        ItemOverlayRenderer.Render(this, visibleItems, _comparisons, player);

        if (Settings.EnableDebugWindow.Value)
        {
            try
            {
                ItemDebugOverlay.Render(this, visibleItems);
            }
            catch
            {
                // Debug overlay must never break the main render.
            }
        }
    }

    private void Rebuild(ServerData serverData, ServerInventory mainInventory)
    {
        _comparisons.Clear();
        _snapshotCache.Clear();

        var player = PlayerContext.Capture(GameController);
        var equipped = EquippedItems.Capture(serverData);
        var engine = new ItemComparisonEngine(GameController, player, equipped, _snapshotCache, Settings);

        foreach (var slotItem in mainInventory.InventorySlotItems)
        {
            var item = slotItem?.Item;
            if (item == null)
                continue;

            try
            {
                var comparison = engine.Compare(item);
                if (comparison != null)
                    _comparisons[item.Address] = comparison;
            }
            catch (Exception e)
            {
                LogError($"[FastItemViewer] Failed to compare item '{item.Path}': {e.Message}", 3f);
            }
        }
    }

    private int ComputeEquipmentHash(ServerData serverData)
    {
        var hash = new HashCode();

        var playerEntity = GameController?.Player;
        if (playerEntity != null && playerEntity.TryGetComponent(out ExileCore.PoEMemory.Components.Player player))
        {
            hash.Add(player.Level);
            hash.Add(player.Strength);
            hash.Add(player.Dexterity);
            hash.Add(player.Intelligence);
        }

        foreach (var holder in serverData.PlayerInventories)
        {
            if (holder?.Inventory == null)
                continue;

            if (!EquippedItems.IsEquipmentType(holder.TypeId))
                continue;

            hash.Add((int)holder.TypeId);

            foreach (var slotItem in holder.Inventory.InventorySlotItems)
            {
                if (slotItem?.Item == null)
                    continue;

                hash.Add(slotItem.Item.Address);
                hash.Add(slotItem.Item.GetComponent<Mods>()?.Hash ?? 0);
            }
        }

        return hash.ToHashCode();
    }

    private void DumpHoveredItem()
    {
        var ingameState = GameController?.IngameState;
        var inventoryPanel = ingameState?.IngameUi?.InventoryPanel;
        if (ingameState == null || inventoryPanel == null || !inventoryPanel.IsVisible)
        {
            LogMessage("[FastItemViewer] Open the inventory and hover an item first.", 5f);
            return;
        }

        var visibleItems = inventoryPanel[InventoryIndex.PlayerInventory]?.VisibleInventoryItems;
        var hovered = ItemOverlayRenderer.FindHoveredItem(this, visibleItems);
        if (hovered?.Item == null)
        {
            LogMessage("[FastItemViewer] Hover an inventory item first.", 5f);
            return;
        }

        var snapshot = ItemStatsBuilder.Build(GameController, PlayerContext.Capture(GameController), hovered.Item);
        var lines = ItemDebugOverlay.BuildDumpLines(GameController, snapshot);

        var equipped = EquippedItems.Capture(ingameState.ServerData);
        lines.Add(string.Empty);
        lines.Add("-- Equipped rings --");
        lines.Add($"Ring 1: {DescribeEntity(equipped.Ring1)}");
        lines.Add($"Ring 2: {DescribeEntity(equipped.Ring2)}");

        if (_comparisons.TryGetValue(snapshot.Address, out var comparison))
        {
            lines.Add(string.Empty);
            lines.Add("-- Comparison --");
            lines.Add($"Targets: {comparison.Targets.Count}  best: {comparison.BestTarget?.Label}  score: {comparison.Score}  upgrade: {comparison.IsUpgrade}");

            foreach (var target in comparison.Targets)
            {
                lines.Add($"{target.Label}: score={target.Score}  empty={target.EmptySlot}  replaced={target.ReplacedNames}");

                foreach (var line in target.Lines.Take(8))
                    lines.Add($"  {line.CompactText}");
            }
        }

        LogMessage("[FastItemViewer] ===== Item dump =====", 20f);
        foreach (var line in lines)
            LogMessage($"[FastItemViewer] {line}", 20f);
    }

    private static string DescribeEntity(Entity entity)
    {
        if (entity == null)
            return "<empty>";

        return entity.TryGetComponent(out Base baseComponent) && !string.IsNullOrWhiteSpace(baseComponent.Name)
            ? baseComponent.Name
            : entity.Path;
    }
}
