# FastItemViewer

A lightweight ExileApi plugin that shows **what changes when you equip an item** directly in the player inventory.

## Features

- Compact stat deltas drawn on top of equippable inventory items.
- Detailed comparison panel when hovering an item.
- Compares against the currently equipped item in the matching slot.
- Shows both ring comparisons (`Ring 1` / `Ring 2`).
- Handles one-hand weapons, dual wield, two-hand weapons, bows and quivers.
- Colors resistance names by element:
  - Fire, Cold, Lightning, Chaos.
- Green/red feedback for gains and losses.
- Optional upgrade/downgrade frame around the item.
- Projected totals for resistances, life, mana, energy shield and attributes.
- Weapon DPS / APS / critical strike chance comparison.
- Warns about unmet level or attribute requirements.
- Built-in debug overlay for inspecting raw item stats.

## Requirements

- ExileApi with `net10.0-windows` plugin support.
- Plugin source placed in `Plugins/Source/FastItemViewer`.
- The HUD must be able to compile source plugins.

## Installation

1. Copy the `FastItemViewer` folder into `Plugins/Source`.
2. Start or restart the HUD.
3. Enable **FastItemViewer** in the plugin list.
4. Open your inventory in game.

## Usage

- Open the inventory to see compact deltas on equippable items.
- Hover an item to see the full breakdown.
- Empty slots are treated as the best replacement target and are preferred in the compact view.
- If one ring slot is empty, the compact view uses the empty slot instead of mixing in red lines from the occupied ring.

## Main Settings

| Setting | Description |
|---|---|
| `Enable` | Enables the plugin. |
| `OnlyWhenInventoryOpen` | Stops processing while the inventory is closed. |
| `ShowOnItems` | Draws compact deltas on items. |
| `ShowHoverPanel` | Shows the detailed panel on hover. |
| `ShowBothSlotComparisons` | Shows both ring/weapon comparisons when available. |
| `ShowItemFrame` | Draws a green/red frame around items. |
| `ShowProjected...` | Toggles projected resistances, life, mana, ES and attributes. |
| `ShowWeaponDps` | Shows DPS / APS / crit comparison for weapons. |
| `ShowUnknownStats` | Shows stats that are not in the curated list (can be noisy). |
| `MaxLinesOnItem` | Maximum number of compact lines per item. |
| `FontSize` / `HoverFontSize` | Item overlay and hover panel font sizes. |
| `HoverMaxWidth` | Maximum hover panel text width before wrapping. |
| `DebugFontSize` / `DebugMaxWidth` | Debug overlay font size and width. |
| `FireResistanceColor`, etc. | Element colors for resistance names. |

## Debug

- Enable `Enable Debug Window`.
- Hover an inventory item to see its class, mods, aggregated `GameStat` values, `Key`, `UserFriendlyName`, `LocalStats`, defences, DPS and requirements.
- `Dump Hovered Item` writes the same information to the HUD log.
- The dump also includes equipped rings and comparison target scores.

## Limitations

- This is not a Path of Building replacement. It shows item contributions and additive totals, not a full build recalculation.
- Weapon DPS is local weapon DPS, not total build DPS.
- Percentage-based life/ES changes are not projected as exact totals.
- Flasks, jewels and tinctures are not supported yet.
- Weapon swap set 2 is not compared.
- Labels are intentionally English-only because the HUD font does not support Cyrillic.

## Build

```powershell
dotnet build FastItemViewer.csproj -c Release
```

Development notes and progress are tracked in `PLAN.md`.
