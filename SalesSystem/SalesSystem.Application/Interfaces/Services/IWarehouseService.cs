using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IWarehouseService
{
    Task<Result<WarehouseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<WarehouseDto>>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default);
    Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest request, CancellationToken ct);
    Task<Result<WarehouseDto>> UpdateAsync(int id, UpdateWarehouseRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result> PermanentDeleteAsync(int id, CancellationToken ct);
}