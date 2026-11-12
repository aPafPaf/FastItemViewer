using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace FastItemViewer;

public class FastItemViewerSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(true);

    public ToggleNode OnlyWhenInventoryOpen { get; set; } = new(true);

    public ToggleNode ShowOnItems { get; set; } = new(true);
    public ToggleNode ShowHoverPanel { get; set; } = new(true);
    public ToggleNode ShowBothSlotComparisons { get; set; } = new(true);
    public ToggleNode ShowItemFrame { get; set; } = new(true);

    public ToggleNode ShowProjectedResistances { get; set; } = new(true);
    public ToggleNode ShowProjectedLife { get; set; } = new(true);
    public ToggleNode ShowProjectedMana { get; set; } = new(true);
    public ToggleNode ShowProjectedEnergyShield { get; set; } = new(true);
    public ToggleNode ShowProjectedAttributes { get; set; } = new(true);
    public ToggleNode ShowWeaponDps { get; set; } = new(true);
    public ToggleNode ShowUnknownStats { get; set; } = new(false);

    public RangeNode<int> MaxLinesOnItem { get; set; } = new(4, 1, 10);
    public RangeNode<int> FontSize { get; set; } = new(12, 8, 20);
    public RangeNode<int> HoverFontSize { get; set; } = new(12, 8, 24);
    public RangeNode<int> HoverMaxWidth { get; set; } = new(700, 300, 1400);
    public RangeNode<int> DebugFontSize { get; set; } = new(16, 10, 32);
    public RangeNode<int> DebugMaxWidth { get; set; } = new(900, 300, 1600);
    public RangeNode<int> BackgroundAlpha { get; set; } = new(170, 0, 255);

    public ColorNode PositiveColor { get; set; } = new(new Color(90, 230, 90, 255));
    public ColorNode NegativeColor { get; set; } = new(new Color(240, 90, 90, 255));
    public ColorNode NeutralColor { get; set; } = new(new Color(210, 210, 210, 255));
    public ColorNode FireResistanceColor { get; set; } = new(new Color(255, 120, 70, 255));
    public ColorNode ColdResistanceColor { get; set; } = new(new Color(90, 190, 255, 255));
    public ColorNode LightningResistanceColor { get; set; } = new(new Color(255, 235, 90, 255));
    public ColorNode ChaosResistanceColor { get; set; } = new(new Color(190, 110, 255, 255));
    public ColorNode HeaderColor { get; set; } = new(new Color(255, 215, 120, 255));
    public ColorNode WarningColor { get; set; } = new(new Color(255, 165, 70, 255));
    public ColorNode PanelBackgroundColor { get; set; } = new(new Color(10, 10, 10, 225));
    public ColorNode FrameUpgradeColor { get; set; } = new(new Color(90, 230, 90, 220));
    public ColorNode FrameDowngradeColor { get; set; } = new(new Color(240, 90, 90, 220));

    public ToggleNode EnableDebugWindow { get; set; } = new(false);
    public ButtonNode DumpHoveredItem { get; set; } = new();
}
