using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Interfaces.Services;

public interface IStoreSettingsService
{
    Task<Result<StoreSettingsDto>> GetSettingsAsync(CancellationToken ct = default);
    Task<Result<StoreSettingsDto>> UpdateSettingsAsync(UpdateSettingsRequest request, int userId, CancellationToken ct = default);
    Task<Result<CostingMethod?>> GetCostingMethodAsync(CancellationToken ct = default);
    Task<Result> SetCostingMethodAsync(CostingMethod method, int userId, CancellationToken ct = default);
}
