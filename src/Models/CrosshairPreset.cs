namespace Xhair.Models;

public sealed class CrosshairPreset
{
    public string Name { get; set; } = string.Empty;
    public CrosshairSettings Settings { get; set; } = new();
}
