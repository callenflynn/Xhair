namespace Xhair.Models;

public sealed class CrosshairSettings
{
    public string ColorHex { get; set; } = "#FFFFFF";
    public string OutlineColorHex { get; set; } = "#000000";
    public double LineLength { get; set; } = 10;
    public double Thickness { get; set; } = 2;
    public double Gap { get; set; } = 4;
    public double DotSize { get; set; } = 3;
    public double OutlineThickness { get; set; } = 1;
    public double Opacity { get; set; } = 1;
    public double OffsetX { get; set; } = 0;
    public double OffsetY { get; set; } = 0;
    public CrosshairShape Shape { get; set; } = CrosshairShape.Cross;
    public bool IsEnabled { get; set; } = true;
    public bool StartEnabledOnLaunch { get; set; } = false;
    public bool OutlineEnabled { get; set; } = true;
    public bool ShowEditorGuides { get; set; } = true;
    public bool LivePreviewEnabled { get; set; } = false;
    public string? EditorImagePath { get; set; }
    public double EditorImageX { get; set; }
    public double EditorImageY { get; set; }
    public List<EditorStroke> EditorStrokes { get; set; } = new();
}
