using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Desktop.Services.Api.Interfaces;

public interface IInventoryApiService
{
    Task<Result<IReadOnlyList<WarehouseStockDto>>> GetStockByWarehouseAsync(int warehouseId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WarehouseStockDto>>> GetStockByProductAsync(int productId, CancellationToken ct = default);
}

