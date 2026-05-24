using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Updates;
using SalesSystem.Application.Updates.Models;
using SalesSystem.Contracts.Common;

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

    public async Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            var versionFileUrl = _configuration["UpdateCheckUrl"];
            if (string.IsNullOrWhiteSpace(versionFileUrl))
            {
                _logger.LogWarning("UpdateCheckUrl not configured");
                return Result<UpdateCheckResult>.Failure("UpdateCheckUrl not configured");
            }

            _logger.LogInformation("Checking for updates at {Url}", versionFileUrl);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            var response = await _httpClient.GetAsync(versionFileUrl, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Update server returned {Status}", response.StatusCode);
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
                _logger.LogInformation("Version {Version} was skipped by user", skippedVersion);
                return Result<UpdateCheckResult>.Success(UpdateCheckResult.NoUpdate());
            }

            if (updateInfo.IsUpdateAvailable(currentVersion))
            {
                _logger.LogInformation("Update available: {Current} -> {Latest}",
                    currentVersion, updateInfo.LatestVersion);
                return Result<UpdateCheckResult>.Success(UpdateCheckResult.Available(updateInfo));
            }

            _logger.LogInformation("App is up to date ({Version})", currentVersion);
            return Result<UpdateCheckResult>.Success(UpdateCheckResult.NoUpdate());
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Update check timed out");
            return Result<UpdateCheckResult>.Failure("Connection timeout");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No internet connection for update check");
            return Result<UpdateCheckResult>.Failure("No internet connection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during update check");
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
                    return Result<string>.Failure("Checksum verification failed");
                }
            }

            _logger.LogInformation("Download complete: {Path}", tempPath);
            return Result<string>.Success(tempPath);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Download cancelled by user");
            return Result<string>.Failure("Download cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed");
            return Result<string>.Failure($"Download failed: {ex.Message}");
        }
    }

    public Task<Result<bool>> LaunchInstallerAndExitAsync(string installerPath)
    {
        if (!File.Exists(installerPath))
        {
            _logger.LogError("Installer not found at {Path}", installerPath);
            return Task.FromResult(Result<bool>.Failure("Installer file not found"));
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

        // Return true to signal caller should exit the application
        return Task.FromResult(Result<bool>.Success(true));
    }

    public Result<string> GetCurrentVersion()
    {
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = version != null
                ? $"{version.Major}.{version.Minor}.{version.Build}"
                : "0.0.0";
            return Result<string>.Success(versionString);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read assembly version");
            return Result<string>.Failure("Unable to determine version");
        }
    }

    private static string GetAppDataSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "SalesSystem");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    public Result SkipVersion(string version)
    {
        try
        {
            var path = GetAppDataSettingsPath();
            var json = File.Exists(path) ? File.ReadAllText(path) : "{}";
            var obj = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)
                      ?? new Dictionary<string, object?>();
            obj["SkippedVersion"] = version;
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(obj, options));

            _logger.LogInformation("Version {Version} marked as skipped in {Path}", version, path);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist skipped version");
            return Result.Failure("Failed to save skipped version");
        }
    }

    public Result<string> GetSkippedVersion()
    {
        try
        {
            var path = GetAppDataSettingsPath();
            if (!File.Exists(path))
                return Result<string>.Success(string.Empty);

            var json = File.ReadAllText(path);
            var obj = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            if (obj != null && obj.TryGetValue("SkippedVersion", out var v) && v is string version)
                return Result<string>.Success(version);

            return Result<string>.Success(string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read skipped version from {Path}",
                GetAppDataSettingsPath());
            return Result<string>.Failure("Failed to read skipped version");
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
