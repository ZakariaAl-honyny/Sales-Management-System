using System.IO;
using System.Net.Http;
using System.Text.Json;
using SalesSystem.Application.Updates;
using SalesSystem.Application.Updates.Models;
using SalesSystem.Contracts.Common;

namespace SalesSystem.DesktopPWF.Services.App;

public class UpdaterService : IUpdaterService
{
    private readonly HttpClient _httpClient;
    private readonly string _versionFileUrl;

    public UpdaterService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _versionFileUrl = LoadVersionFileUrl();
    }

    private static string LoadVersionFileUrl()
    {
        try
        {
            var settingsPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (System.IO.File.Exists(settingsPath))
            {
                var json = System.IO.File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (settings != null && settings.TryGetValue("UpdateCheckUrl", out var url)
                    && !string.IsNullOrWhiteSpace(url))
                    return url;
            }
        }
        catch
        {
        }
        return "https://your-server.com/updates/version.json";
    }

    public async Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            Serilog.Log.Information("Checking for updates at {Url}", _versionFileUrl);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            var response = await _httpClient.GetAsync(_versionFileUrl, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                Serilog.Log.Warning("Update server returned {Status}", response.StatusCode);
                return Result<UpdateCheckResult>.Failure($"Server returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);

            var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (updateInfo == null)
                return Result<UpdateCheckResult>.Failure("Invalid version.json format");

            var currentVersion = GetCurrentVersion().Value ?? "0.0.0";
            var skippedVersion = GetSkippedVersion().Value ?? string.Empty;

            if (!updateInfo.IsForceUpdate(currentVersion) &&
                updateInfo.LatestVersion == skippedVersion)
            {
                Serilog.Log.Information("Version {Version} was skipped by user", skippedVersion);
                return Result<UpdateCheckResult>.Success(UpdateCheckResult.NoUpdate());
            }

            if (updateInfo.IsUpdateAvailable(currentVersion))
            {
                Serilog.Log.Information("Update available: {Current} → {Latest}",
                    currentVersion, updateInfo.LatestVersion);
                return Result<UpdateCheckResult>.Success(UpdateCheckResult.Available(updateInfo));
            }

            Serilog.Log.Information("App is up to date ({Version})", currentVersion);
            return Result<UpdateCheckResult>.Success(UpdateCheckResult.NoUpdate());
        }
        catch (OperationCanceledException)
        {
            Serilog.Log.Warning("Update check timed out");
            return Result<UpdateCheckResult>.Failure("Connection timeout");
        }
        catch (HttpRequestException ex)
        {
            Serilog.Log.Warning(ex, "No internet connection for update check");
            return Result<UpdateCheckResult>.Failure("No internet connection");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Unexpected error during update check");
            return Result<UpdateCheckResult>.Failure(ex.Message);
        }
    }

    public async Task<Result<string>> DownloadUpdateAsync(
        string downloadUrl,
        string expectedChecksum,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default)
    {
        try
        {
            var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
            var tempPath = Path.Combine(Path.GetTempPath(), "SalesSystemUpdate", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

            Serilog.Log.Information("Downloading update from {Url} to {Path}", downloadUrl, tempPath);

            using var response = await _httpClient.GetAsync(
                downloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
            await using var fileStream = new FileStream(
                tempPath, FileMode.Create, FileAccess.Write,
                FileShare.None, bufferSize: 81920, useAsync: true);

            var buffer = new byte[81920];
            long bytesReceived = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                bytesReceived += bytesRead;

                var elapsed = stopwatch.Elapsed.TotalSeconds;
                var speedKbps = elapsed > 0
                    ? bytesReceived / 1024.0 / elapsed
                    : 0;

                var percentage = totalBytes > 0
                    ? bytesReceived * 100.0 / totalBytes
                    : 0;

                progress.Report(new DownloadProgress(
                    bytesReceived, totalBytes, percentage, speedKbps));
            }

            if (!string.IsNullOrWhiteSpace(expectedChecksum))
            {
                var isValid = await VerifyChecksumAsync(tempPath, expectedChecksum);
                if (!isValid)
                {
                    File.Delete(tempPath);
                    Serilog.Log.Error("Checksum verification failed for {File}", tempPath);
                    return Result<string>.Failure("Checksum verification failed");
                }
            }

            Serilog.Log.Information("Download complete: {Path}", tempPath);
            return Result<string>.Success(tempPath);
        }
        catch (OperationCanceledException)
        {
            Serilog.Log.Information("Download cancelled by user");
            return Result<string>.Failure("Download cancelled");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Download failed");
            return Result<string>.Failure($"Download failed: {ex.Message}");
        }
    }

    public Task<Result<bool>> LaunchInstallerAndExitAsync(string installerPath)
    {
        if (!File.Exists(installerPath))
        {
            Serilog.Log.Error("Installer not found at {Path}", installerPath);
            return Task.FromResult(Result<bool>.Failure("Installer file not found"));
        }

        Serilog.Log.Information("Launching installer: {Path}", installerPath);

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            UseShellExecute = true,
            Verb = "runas"
        };

        System.Diagnostics.Process.Start(startInfo);

        // Signal caller to shut down. The WPF app should handle graceful shutdown.
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Result<string> GetCurrentVersion()
    {
        try
        {
            var version = System.Reflection.Assembly
                .GetExecutingAssembly()
                .GetName()
                .Version;

            var versionString = version != null
                ? $"{version.Major}.{version.Minor}.{version.Build}"
                : "0.0.0";
            return Result<string>.Success(versionString);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to read assembly version");
            return Result<string>.Failure("Unable to determine version");
        }
    }

    public Result SkipVersion(string version)
    {
        try
        {
            var settings = LoadLocalSettings();
            settings["SkippedVersion"] = version;
            SaveLocalSettings(settings);
            Serilog.Log.Information("Version {Version} marked as skipped", version);
            return Result.Success();
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to persist skipped version");
            return Result.Failure("Failed to save skipped version");
        }
    }

    public Result<string> GetSkippedVersion()
    {
        try
        {
            var settings = LoadLocalSettings();
            return settings.TryGetValue("SkippedVersion", out var v)
                ? Result<string>.Success(v)
                : Result<string>.Success(string.Empty);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to read skipped version");
            return Result<string>.Failure("Failed to read skipped version");
        }
    }

    private static string LocalSettingsPath =>
        Path.Combine(Path.GetTempPath(), "SalesSystemUpdate", "localsettings.json");

    private static Dictionary<string, string> LoadLocalSettings()
    {
        try
        {
            var path = LocalSettingsPath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                    ?? new Dictionary<string, string>();
            }
        }
        catch
        {
        }
        return new Dictionary<string, string>();
    }

    private static void SaveLocalSettings(Dictionary<string, string> settings)
    {
        var dir = Path.GetDirectoryName(LocalSettingsPath);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(LocalSettingsPath, json);
    }

    private static async Task<bool> VerifyChecksumAsync(string filePath, string expectedSha256)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return actualHash == expectedSha256.ToLowerInvariant();
    }
}
