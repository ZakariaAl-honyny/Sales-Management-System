using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Desktop.Services.Interfaces;

public interface IInventoryApiService
{
    Task<Result<IReadOnlyList<InventoryMovementDto>>> GetMovementsAsync(string? search = null, int? productId = null, int? warehouseId = null, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WarehouseStockDto>>> GetStocksAsync(int? warehouseId = null, int? productId = null, CancellationToken ct = default);
}
