using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Interfaces.Repositories;

public interface ISystemSettingsRepository
{
    Task<CostingMethod> GetCostingMethodAsync(CancellationToken ct = default);
    Task SetCostingMethodAsync(CostingMethod method, CancellationToken ct = default);
}