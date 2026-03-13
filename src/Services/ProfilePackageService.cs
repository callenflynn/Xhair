using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Xhair.Models;

namespace Xhair.Services;

public static class ProfilePackageService
{
    private const string ProfileJsonName = "profile.json";
    private const string ImageFileName = "image.png";

    public static void ExportProfile(string filePath, CrosshairProfile profile)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        CrosshairProfile exportProfile = new()
        {
            Name = profile.Name,
            Settings = CopySettings(profile.Settings)
        };

        if (!string.IsNullOrWhiteSpace(exportProfile.Settings.EditorImagePath) && File.Exists(exportProfile.Settings.EditorImagePath))
        {
            exportProfile.Settings.EditorImagePath = ImageFileName;
        }

        using ZipArchive archive = ZipFile.Open(filePath, ZipArchiveMode.Create);
        ZipArchiveEntry profileEntry = archive.CreateEntry(ProfileJsonName);
        using (StreamWriter writer = new(profileEntry.Open()))
        {
            string json = JsonSerializer.Serialize(exportProfile, new JsonSerializerOptions { WriteIndented = true });
            writer.Write(json);
        }

        if (!string.IsNullOrWhiteSpace(profile.Settings.EditorImagePath))
        {
            string imagePath = profile.Settings.EditorImagePath;
            if (File.Exists(imagePath))
            {
                archive.CreateEntryFromFile(imagePath, ImageFileName);
            }
        }
    }

    public static CrosshairProfile? ImportProfile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        using ZipArchive archive = ZipFile.OpenRead(filePath);
        ZipArchiveEntry? profileEntry = archive.GetEntry(ProfileJsonName);
        if (profileEntry == null)
        {
            return null;
        }

        CrosshairProfile? profile;
        using (StreamReader reader = new(profileEntry.Open()))
        {
            string json = reader.ReadToEnd();
            profile = JsonSerializer.Deserialize<CrosshairProfile>(json);
        }

        if (profile == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(profile.Settings.EditorImagePath))
        {
            ZipArchiveEntry? imageEntry = archive.GetEntry(ImageFileName);
            if (imageEntry != null)
            {
                string destFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Xhair",
                    "profiles",
                    SanitizeProfileName(profile.Name) + "_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(destFolder);
                string destPath = Path.Combine(destFolder, ImageFileName);
                imageEntry.ExtractToFile(destPath, true);
                profile.Settings.EditorImagePath = destPath;
            }
        }

        return profile;
    }

    private static CrosshairSettings CopySettings(CrosshairSettings settings)
    {
        return new CrosshairSettings
        {
            ColorHex = settings.ColorHex,
            OutlineColorHex = settings.OutlineColorHex,
            LineLength = settings.LineLength,
            Thickness = settings.Thickness,
            Gap = settings.Gap,
            DotSize = settings.DotSize,
            OutlineThickness = settings.OutlineThickness,
            Opacity = settings.Opacity,
            OffsetX = settings.OffsetX,
            OffsetY = settings.OffsetY,
            Shape = settings.Shape,
            IsEnabled = settings.IsEnabled,
            StartEnabledOnLaunch = settings.StartEnabledOnLaunch,
            OutlineEnabled = settings.OutlineEnabled,
            ShowEditorGuides = settings.ShowEditorGuides,
            LivePreviewEnabled = settings.LivePreviewEnabled,
            EditorImagePath = settings.EditorImagePath,
            EditorImageX = settings.EditorImageX,
            EditorImageY = settings.EditorImageY,
            EditorStrokes = settings.EditorStrokes.Select(stroke => new EditorStroke
            {
                Points = stroke.Points.Select(point => new EditorPoint { X = point.X, Y = point.Y }).ToList()
            }).ToList()
        };
    }

    private static string SanitizeProfileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name.Trim();
    }
}
