using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Database Maintenance and Backups
/// </summary>
public class BackupApiService : ApiServiceBase, IBackupApiService
{
    private const string BasePath = "api/v1/backup";

    public BackupApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<string>> CreateBackupAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<string>(
            () => _httpClient.PostAsync($"{BasePath}/create", null, ct),
            "BackupApiService.CreateBackupAsync");
    }

    public async Task<Result<List<string>>> GetBackupListAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<List<string>>(
            () => _httpClient.GetAsync($"{BasePath}/list", ct),
            "BackupApiService.GetBackupListAsync");
    }

    public async Task<Result> RestoreBackupAsync(string fileName, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"{BasePath}/restore?fileName={Uri.EscapeDataString(fileName)}", null, ct),
            "BackupApiService.RestoreBackupAsync");
    }
}
