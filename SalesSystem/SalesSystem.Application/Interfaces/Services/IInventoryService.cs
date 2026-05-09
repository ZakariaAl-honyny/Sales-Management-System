using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Interfaces.Services;

public interface IInventoryService
{
    // Foundational methods (T003)
    Task<Result<decimal>> GetStockAsync(int productId, int warehouseId, CancellationToken ct);
    Task<Result> IncreaseStockAsync(int productId, int warehouseId, decimal quantity, MovementType movementType, string referenceType, int referenceId, decimal? unitCost, int? userId, CancellationToken ct);
    Task<Result> DecreaseStockAsync(int productId, int warehouseId, decimal quantity, MovementType movementType, string referenceType, int referenceId, decimal? unitCost, int? userId, CancellationToken ct);
    Task<Result> ValidateStockAsync(int productId, int warehouseId, decimal requiredQty, CancellationToken ct);

    // Stock Transfer and Querying
    Task<Result<StockTransferDto>> GetTransferByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<StockTransferDto>>> GetAllTransfersAsync(int? fromWarehouseId, int? toWarehouseId, int page, int pageSize, CancellationToken ct);
    Task<Result<StockTransferDto>> CreateTransferAsync(CreateStockTransferRequest request, int userId, CancellationToken ct);
    Task<Result<IEnumerable<WarehouseStockDto>>> GetWarehouseStockAsync(int warehouseId, string? search, CancellationToken ct);
    Task<Result<PagedResult<WarehouseStockDto>>> GetWarehouseStocksAsync(int? warehouseId, int? productId, int page, int pageSize, CancellationToken ct);
    Task<Result<PagedResult<InventoryMovementDto>>> GetMovementsAsync(int? productId, int? warehouseId, int? movementType, int page, int pageSize, CancellationToken ct);
}
