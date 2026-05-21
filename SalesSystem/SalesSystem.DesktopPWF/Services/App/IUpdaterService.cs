using SalesSystem.Application.Updates.Models;
using SalesSystem.Contracts.Common;

namespace SalesSystem.DesktopPWF.Services.App;

public interface IUpdaterService
{
    Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(CancellationToken ct = default);

    Task<Result<string>> DownloadUpdateAsync(
        string downloadUrl,
        string expectedChecksum,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default);

    Task<Result<bool>> LaunchInstallerAndExitAsync(string installerPath);

    Result<string> GetCurrentVersion();

    Result SkipVersion(string version);

    Result<string> GetSkippedVersion();
}
