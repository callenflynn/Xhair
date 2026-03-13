namespace Xhair.Models;

public sealed class ProfileStore
{
    public string CurrentProfile { get; set; } = "Default";
    public bool StartInTray { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public int ToggleHotkey { get; set; } = (int)System.Windows.Forms.Keys.F5;
    public int CycleHotkey { get; set; } = (int)System.Windows.Forms.Keys.F6;
    public List<CrosshairProfile> Profiles { get; set; } = new();
}
