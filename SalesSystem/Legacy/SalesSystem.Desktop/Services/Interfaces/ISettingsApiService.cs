using SalesSystem.Contracts.Common;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface ISettingsApiService
{
    Task<Result<dynamic>> GetSettingsAsync(CancellationToken ct = default);
    Task<Result> UpdateSettingsAsync(object settings, CancellationToken ct = default);
}

