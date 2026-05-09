using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Application.Interfaces.Services;

public interface IWarehouseService
{
    Task<Result<WarehouseDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<WarehouseDto>>> GetAllAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest request, CancellationToken ct);
    Task<Result<WarehouseDto>> UpdateAsync(int id, UpdateWarehouseRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
