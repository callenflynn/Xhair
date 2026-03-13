namespace Xhair.Models;

public sealed class CrosshairProfile
{
    public string Name { get; set; } = "Default";
    public CrosshairSettings Settings { get; set; } = new();
}
