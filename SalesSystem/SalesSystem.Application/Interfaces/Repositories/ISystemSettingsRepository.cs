using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Interfaces.Repositories;

public interface ISystemSettingsRepository
{
    Task<CostingMethod> GetCostingMethodAsync(CancellationToken ct = default);
    Task SetCostingMethodAsync(CostingMethod method, CancellationToken ct = default);
    Task<string?> GetStringAsync(string key, string? defaultValue = null, CancellationToken ct = default);
    Task SetStringAsync(string key, string value, int? userId = null, CancellationToken ct = default);
}