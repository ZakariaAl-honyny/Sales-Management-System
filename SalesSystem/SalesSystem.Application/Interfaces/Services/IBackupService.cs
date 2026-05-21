using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

public interface IBackupService
{
    Task<Result<string>> CreateBackupAsync(string? folderPath = null, CancellationToken ct = default);
    Task<Result> RestoreBackupAsync(string filePath, CancellationToken ct = default);
    Task<Result<List<string>>> GetBackupListAsync(CancellationToken ct = default);
}
