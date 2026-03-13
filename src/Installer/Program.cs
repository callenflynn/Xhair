using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

const string Owner = "callenflynn";
const string Repo = "Xhair";
const string AssetName = "release.zip";

try
{
    string installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Xhair");
    Directory.CreateDirectory(installDir);

    await KillRunningAppAsync("Xhair");

    ReleaseAsset? asset = await GetLatestReleaseAssetAsync();
    if (asset == null)
    {
        Console.WriteLine("Could not find latest release asset.");
        return;
    }

    string tempZip = Path.Combine(Path.GetTempPath(), "Xhair_release.zip");
    await DownloadFileAsync(asset.DownloadUrl, tempZip);

    ZipFile.ExtractToDirectory(tempZip, installDir, true);
    File.Delete(tempZip);

    string exePath = Path.Combine(installDir, "Xhair.exe");
    if (File.Exists(exePath))
    {
        Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
    }

    Console.WriteLine("Install completed.");
}
catch (Exception ex)
{
    Console.WriteLine("Install failed: " + ex.Message);
}

static async Task KillRunningAppAsync(string processName)
{
    foreach (Process process in Process.GetProcessesByName(processName))
    {
        try
        {
            process.CloseMainWindow();
            if (!process.WaitForExit(2000))
            {
                process.Kill(true);
            }
        }
        catch
        {
            // Ignore failed process kills.
        }
    }

    await Task.Delay(300);
}

static async Task<ReleaseAsset?> GetLatestReleaseAssetAsync()
{
    using HttpClient client = new();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("XhairInstaller");

    string url = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
    using HttpResponseMessage response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();

    using Stream stream = await response.Content.ReadAsStreamAsync();
    using JsonDocument doc = await JsonDocument.ParseAsync(stream);

    if (!doc.RootElement.TryGetProperty("assets", out JsonElement assets))
    {
        return null;
    }

    foreach (JsonElement asset in assets.EnumerateArray())
    {
        string? name = asset.GetProperty("name").GetString();
        if (string.Equals(name, AssetName, StringComparison.OrdinalIgnoreCase))
        {
            string? downloadUrl = asset.GetProperty("browser_download_url").GetString();
            if (!string.IsNullOrWhiteSpace(downloadUrl))
            {
                return new ReleaseAsset(downloadUrl);
            }
        }
    }

    return null;
}

static async Task DownloadFileAsync(string url, string destination)
{
    using HttpClient client = new();
    client.DefaultRequestHeaders.UserAgent.ParseAdd("XhairInstaller");

    using HttpResponseMessage response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();

    await using FileStream fileStream = new(destination, FileMode.Create, FileAccess.Write, FileShare.None);
    await response.Content.CopyToAsync(fileStream);
}

sealed record ReleaseAsset(string DownloadUrl);
