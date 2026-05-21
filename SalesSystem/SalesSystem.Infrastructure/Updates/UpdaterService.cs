using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Updates;
using SalesSystem.Application.Updates.Models;

namespace SalesSystem.Infrastructure.Updates;

public class UpdaterService : IUpdaterService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdaterService> _logger;
    private readonly IConfiguration _configuration;

    public UpdaterService(
        HttpClient httpClient,
        ILogger<UpdaterService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            var versionFileUrl = _configuration["UpdateCheckUrl"];
            if (string.IsNullOrWhiteSpace(versionFileUrl))
            {
                _logger.LogWarning("UpdateCheckUrl not configured");
                return UpdateCheckResult.Failed("UpdateCheckUrl not configured");
            }

            _logger.LogInformation("Checking for updates at {Url}", versionFileUrl);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            var response = await _httpClient.GetAsync(versionFileUrl, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Update server returned {Status}", response.StatusCode);
                return UpdateCheckResult.Failed($"Server returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);

            var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (updateInfo == null)
                return UpdateCheckResult.Failed("Invalid version.json format");

            var currentVersion = GetCurrentVersion();
            var skippedVersion = GetSkippedVersion();

            if (!updateInfo.IsForceUpdate(currentVersion) &&
                updateInfo.LatestVersion == skippedVersion)
            {
                _logger.LogInformation("Version {Version} was skipped by user", skippedVersion);
                return UpdateCheckResult.NoUpdate();
            }

            if (updateInfo.IsUpdateAvailable(currentVersion))
            {
                _logger.LogInformation("Update available: {Current} -> {Latest}",
                    currentVersion, updateInfo.LatestVersion);
                return UpdateCheckResult.Available(updateInfo);
            }

            _logger.LogInformation("App is up to date ({Version})", currentVersion);
            return UpdateCheckResult.NoUpdate();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Update check timed out");
            return UpdateCheckResult.Failed("Connection timeout");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No internet connection for update check");
            return UpdateCheckResult.Failed("No internet connection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during update check");
            return UpdateCheckResult.Failed(ex.Message);
        }
    }

    public async Task<string?> DownloadUpdateAsync(
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

            _logger.LogInformation("Downloading update from {Url} to {Path}", downloadUrl, tempPath);

            using var response = await _httpClient.GetAsync(
                downloadUrl,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var stopwatch = Stopwatch.StartNew();

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
                    _logger.LogError("Checksum verification failed for {File}", tempPath);
                    return null;
                }
            }

            _logger.LogInformation("Download complete: {Path}", tempPath);
            return tempPath;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Download cancelled by user");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed");
            return null;
        }
    }

    public void LaunchInstallerAndExit(string installerPath)
    {
        if (!File.Exists(installerPath))
        {
            _logger.LogError("Installer not found at {Path}", installerPath);
            return;
        }

        _logger.LogInformation("Launching installer: {Path}", installerPath);

        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            UseShellExecute = true,
            Verb = "runas"
        };

        Process.Start(startInfo);

        try
        {
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to shut down application");
        }
    }

    public string GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version != null
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : "0.0.0";
    }

    public void SkipVersion(string version)
    {
        try
        {
            var appSettingsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            if (File.Exists(appSettingsPath))
            {
                var json = File.ReadAllText(appSettingsPath);
                var obj = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                if (obj != null)
                {
                    obj["SkippedVersion"] = version;
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(appSettingsPath, JsonSerializer.Serialize(obj, options));
                }
            }

            _logger.LogInformation("Version {Version} marked as skipped", version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist skipped version");
        }
    }

    public string GetSkippedVersion()
    {
        try
        {
            return _configuration["SkippedVersion"] ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read skipped version");
            return string.Empty;
        }
    }

    private static async Task<bool> VerifyChecksumAsync(string filePath, string expectedSha256)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        var actualHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return actualHash == expectedSha256.ToLowerInvariant();
    }
}
