using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Inventory management service.
/// Handles stock levels, inventory transactions, and warehouse transfers.
/// </summary>
public interface IInventoryService
{
    // ─── Stock Level Operations ────────────────────────
    Task<Result<decimal>> GetStockAsync(int productId, int warehouseId, CancellationToken ct);
    Task<Result> IncreaseStockAsync(int productId, int warehouseId, decimal quantity, decimal? unitCost = null, int? userId = null, CancellationToken ct = default);
    Task<Result> DecreaseStockAsync(int productId, int warehouseId, decimal quantity, decimal? unitCost = null, int? userId = null, CancellationToken ct = default);
    Task<Result> ValidateStockAsync(int productId, int warehouseId, decimal requiredQty, bool allowNegativeStock = false, CancellationToken ct = default);

    // ─── Warehouse Stock Query ────────────────────────
    Task<Result<IEnumerable<WarehouseStockDto>>> GetWarehouseStockAsync(int warehouseId, string? search, CancellationToken ct);
    Task<Result<PagedResult<WarehouseStockDto>>> GetWarehouseStocksAsync(int? warehouseId, int? productId, int page, int pageSize, CancellationToken ct);

    // ─── Inventory Transaction ────────────────────────
    Task<Result<InventoryTransactionDto>> CreateTransactionAsync(CreateInventoryTransactionRequest request, int userId, CancellationToken ct);
    Task<Result<InventoryTransactionDto>> PostTransactionAsync(int transactionId, int userId, CancellationToken ct);
    Task<Result<InventoryTransactionDto>> CancelTransactionAsync(int transactionId, int userId, CancellationToken ct);
    Task<Result<InventoryTransactionDto>> GetTransactionByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<InventoryTransactionDto>>> GetAllTransactionsAsync(int? warehouseId, int? transactionType, int page, int pageSize, CancellationToken ct);

    // ─── Warehouse Transfer ───────────────────────────
    Task<Result<WarehouseTransferDto>> CreateTransferAsync(CreateWarehouseTransferRequest request, int userId, CancellationToken ct);
    Task<Result<WarehouseTransferDto>> PostTransferAsync(int transferId, int userId, CancellationToken ct);
    Task<Result<WarehouseTransferDto>> CancelTransferAsync(int transferId, int userId, CancellationToken ct);
    Task<Result<WarehouseTransferDto>> GetTransferByIdAsync(int id, CancellationToken ct);
    Task<Result<PagedResult<WarehouseTransferDto>>> GetAllTransfersAsync(int? sourceWarehouseId, int? destinationWarehouseId, int page, int pageSize, CancellationToken ct);

    // ─── Movement History ────────────────────────────
    Task<Result<PagedResult<InventoryTransactionDto>>> GetMovementsAsync(int? productId, int? warehouseId, int? transactionType, int page, int pageSize, CancellationToken ct);
}
