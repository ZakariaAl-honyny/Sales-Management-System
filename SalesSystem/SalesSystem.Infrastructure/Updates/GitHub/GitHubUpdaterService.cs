using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Updates;
using SalesSystem.Application.Updates.Models;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Infrastructure.Updates.GitHub;

/// <summary>
/// Alternative updater service that checks GitHub releases instead of a custom version.json endpoint.
/// To use: register in DI instead of UpdaterService, and configure GitHub:Owner + GitHub:Repository.
/// </summary>
public class GitHubUpdaterService : IUpdaterService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubUpdaterService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _owner;
    private readonly string _repo;
    private const string GitHubApiBase = "https://api.github.com";

    public GitHubUpdaterService(
        HttpClient httpClient,
        ILogger<GitHubUpdaterService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _owner = configuration["GitHub:Owner"] ?? string.Empty;
        _repo = configuration["GitHub:Repository"] ?? string.Empty;
    }

    public async Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_owner) || string.IsNullOrWhiteSpace(_repo))
            {
                _logger.LogWarning("GitHub owner or repository not configured");
                return Result<UpdateCheckResult>.Failure("GitHub configuration missing");
            }

            var url = $"{GitHubApiBase}/repos/{_owner}/{_repo}/releases/latest";
            _logger.LogInformation("Checking GitHub releases at {Url}", url);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            var response = await _httpClient.GetAsync(url, cts.Token);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("GitHub API rate limit exceeded");
                return Result<UpdateCheckResult>.Failure("API rate limit exceeded - try again later");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("No GitHub releases found for {Owner}/{Repo}", _owner, _repo);
                return Result<UpdateCheckResult>.Failure("No releases found");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub API returned {Status}", response.StatusCode);
                return Result<UpdateCheckResult>.Failure($"GitHub API returned {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync(cts.Token);
            var release = JsonSerializer.Deserialize<GitHubReleaseDto>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (release == null || release.Draft)
                return Result<UpdateCheckResult>.Failure("No valid release found");

            var installerAsset = release.Assets.Find(a =>
                a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                a.Name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase));

            if (installerAsset == null)
            {
                _logger.LogWarning("No installer asset found in release {Tag}", release.TagName);
                return Result<UpdateCheckResult>.Failure("No installer asset in release");
            }

            var versionTag = release.TagName.TrimStart('v', 'V');
            var changelog = ParseChangelog(release.Body);

            var updateInfo = new UpdateInfo
            {
                LatestVersion = versionTag,
                ReleaseDate = release.PublishedAt?.ToString("yyyy-MM-dd") ?? string.Empty,
                DownloadUrl = installerAsset.BrowserDownloadUrl,
                ChecksumSHA256 = string.Empty,
                MinimumRequiredVersion = _configuration["GitHub:MinimumRequiredVersion"] ?? "0.0.0",
                IsCritical = release.Prerelease == false,
                Changelog = changelog
            };

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
            _logger.LogWarning("GitHub update check timed out");
            return Result<UpdateCheckResult>.Failure("Connection timeout");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No internet connection for GitHub update check");
            return Result<UpdateCheckResult>.Failure("No internet connection");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GitHub update check");
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

            _logger.LogInformation("Downloading from {Url} to {Path}", downloadUrl, tempPath);

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

        // Return true to signal caller should exit the application.
        // The caller (Desktop app) is responsible for shutting down gracefully.
        // This avoids Environment.Exit(0) which would kill Windows Service hosts.
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

    public Result SkipVersion(string version)
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
            return Result<string>.Success(_configuration["SkippedVersion"] ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read skipped version");
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

    private static List<string> ParseChangelog(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return new List<string>();

        return body
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimStart('-', ' ', '*'))
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Take(50)
            .ToList();
    }
}
