using SalesSystem.Application.Updates.Models;

namespace SalesSystem.Application.Updates;

public interface IUpdaterService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default);

    Task<string?> DownloadUpdateAsync(
        string downloadUrl,
        string expectedChecksum,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default);

    void LaunchInstallerAndExit(string installerPath);

    string GetCurrentVersion();

    void SkipVersion(string version);

    string GetSkippedVersion();
}
