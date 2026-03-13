using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Xhair.Services;

public sealed class UpdateInfo
{
    public bool IsUpdateAvailable { get; init; }
    public string? LatestTag { get; init; }
    public string? InstallerUrl { get; init; }
}

public static class UpdateService
{
    private const string Owner = "callenflynn";
    private const string Repo = "Xhair";
    private const string InstallerAssetName = "XhairInstaller.exe";

    public static async Task<UpdateInfo> CheckForUpdateAsync(string currentVersion)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Xhair");

        string url = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
        using HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument doc = await JsonDocument.ParseAsync(stream);

        string? tag = doc.RootElement.GetProperty("tag_name").GetString();
        string? installerUrl = null;

        if (doc.RootElement.TryGetProperty("assets", out JsonElement assets))
        {
            foreach (JsonElement asset in assets.EnumerateArray())
            {
                string? name = asset.GetProperty("name").GetString();
                if (string.Equals(name, InstallerAssetName, StringComparison.OrdinalIgnoreCase))
                {
                    installerUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
        }

        bool updateAvailable = IsNewerVersion(currentVersion, tag);
        return new UpdateInfo
        {
            IsUpdateAvailable = updateAvailable && !string.IsNullOrWhiteSpace(installerUrl),
            LatestTag = tag,
            InstallerUrl = installerUrl
        };
    }

    public static async Task<string?> DownloadInstallerAsync(string installerUrl)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Xhair");

        using HttpResponseMessage response = await client.GetAsync(installerUrl);
        response.EnsureSuccessStatusCode();

        string tempPath = Path.Combine(Path.GetTempPath(), "XhairInstaller.exe");
        await using FileStream fileStream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream);
        return tempPath;
    }

    private static bool IsNewerVersion(string currentVersion, string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        string cleanTag = tag.TrimStart('v', 'V');
        if (!Version.TryParse(currentVersion, out Version? current))
        {
            return true;
        }

        if (!Version.TryParse(cleanTag, out Version? latest))
        {
            return true;
        }

        return latest > current;
    }
}
