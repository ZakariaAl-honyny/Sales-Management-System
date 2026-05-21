using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IWarehouseApiService
{
    Task<Result<IReadOnlyList<WarehouseDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);
    Task<Result<WarehouseDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WarehouseStockDto>>> GetStockAsync(int warehouseId, CancellationToken ct = default);
    Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest r, CancellationToken ct = default);
    Task<Result<WarehouseDto>> UpdateAsync(int id, UpdateWarehouseRequest r, CancellationToken ct = default);
    Task<Result> SetDefaultAsync(int id, CancellationToken ct = default);
}

