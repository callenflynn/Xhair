using System.IO;
using System.Text.Json;
using Xhair.Models;

namespace Xhair.Services;

public sealed class ProfileStorage
{
    private readonly string _filePath;

    public ProfileStorage()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Xhair");
        _filePath = Path.Combine(folder, "profiles.json");
    }

    public ProfileStore Load()
    {
        if (!File.Exists(_filePath))
        {
            return CreateDefaultStore();
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            ProfileStore? store = JsonSerializer.Deserialize<ProfileStore>(json);
            if (store == null || store.Profiles.Count == 0)
            {
                return CreateDefaultStore();
            }

            return store;
        }
        catch
        {
            return CreateDefaultStore();
        }
    }

    public void Save(ProfileStore store)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(store, options);
        File.WriteAllText(_filePath, json);
    }

    private static ProfileStore CreateDefaultStore()
    {
        var store = new ProfileStore();
        store.Profiles.Add(new CrosshairProfile { Name = "Default" });
        store.CurrentProfile = "Default";
        store.StartInTray = true;
        store.StartWithWindows = false;
        store.ToggleHotkey = (int)System.Windows.Forms.Keys.F5;
        store.CycleHotkey = (int)System.Windows.Forms.Keys.F6;
        return store;
    }
}
