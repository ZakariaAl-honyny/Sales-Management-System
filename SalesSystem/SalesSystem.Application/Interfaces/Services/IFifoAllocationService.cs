using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for FIFO (First In First Out) and FEFO (First Expiry First Out)
/// inventory batch allocation during stock-out operations.
/// </summary>
public interface IFifoAllocationService
{
    /// <summary>
    /// Adds purchase batches when stock is received.
    /// Creates one or more InventoryBatch records that are tracked by EF Core
    /// but NOT saved — the caller is responsible for SaveChangesAsync within their transaction.
    /// </summary>
    Task<Result<List<InventoryBatch>>> AddPurchaseBatchesAsync(
        int productId,
        int warehouseId,
        decimal quantity,
        decimal unitCost,
        string? batchNo,
        DateTime? manufactureDate,
        DateTime? expiryDate,
        int? purchaseInvoiceItemId,
        bool isOpeningBatch,
        CancellationToken ct);

    /// <summary>
    /// Deducts quantity from the earliest batches using FIFO (or FEFO if any batch has an ExpiryDate).
    /// Returns the list of allocations created with their unit costs.
    /// Modifies InventoryBatch entities in the change tracker — caller must save.
    /// </summary>
    Task<Result<List<InventoryBatchAllocation>>> DeductFromBatchesAsync(
        int productId,
        int warehouseId,
        decimal quantityNeeded,
        int? salesInvoiceItemId,
        int? createdByUserId,
        CancellationToken ct);

    /// <summary>
    /// Returns quantity back to the original batch (for sales returns).
    /// Creates a new return InventoryBatch since allocations are consumed.
    /// The new batch is tracked by EF Core but NOT saved — caller must save.
    /// </summary>
    Task<Result> ReturnToBatchAsync(
        int batchId,
        decimal quantityReturned,
        int? salesReturnItemId,
        int? createdByUserId,
        CancellationToken ct);

    /// <summary>
    /// Gets the current stock breakdown by batch for a product+warehouse.
    /// Returns all active (non-zero quantity) batches sorted by FIFO/FEFO order.
    /// </summary>
    Task<Result<List<BatchStockInfo>>> GetBatchBreakdownAsync(
        int productId, int warehouseId, CancellationToken ct);

    /// <summary>
    /// Gets the current weighted average cost from all active batches with remaining quantity.
    /// Returns 0 if no stock exists.
    /// </summary>
    Task<Result<decimal>> GetCurrentStockCostAsync(
        int productId, int warehouseId, CancellationToken ct);
}

/// <summary>
/// Tracks allocation from a specific batch to a sales invoice line.
/// Used as a return DTO — no dedicated entity exists; allocations
/// are reconstructed from InventoryMovement records if needed later.
/// </summary>
/// <param name="BatchId">The ID of the source inventory batch.</param>
/// <param name="Quantity">Quantity allocated from this batch.</param>
/// <param name="UnitCost">Per-unit cost at time of allocation.</param>
/// <param name="SalesInvoiceItemId">FK to the sales invoice item, if applicable.</param>
public record InventoryBatchAllocation(
    int BatchId,
    decimal Quantity,
    decimal UnitCost,
    int? SalesInvoiceItemId = null
);

/// <summary>
/// Snapshot of a single batch's current stock state for display/reporting.
/// </summary>
/// <param name="BatchId">The inventory batch ID.</param>
/// <param name="BatchNo">Supplier or system batch reference number.</param>
/// <param name="RemainingQuantity">Current available quantity in base units.</param>
/// <param name="OriginalQuantity">Original quantity when the batch was created (estimated from movements).</param>
/// <param name="UnitCost">Per-unit cost.</param>
/// <param name="ExpiryDate">Optional expiry date.</param>
/// <param name="ReceivedDate">Date the batch was created/received.</param>
/// <param name="IsExpired">Whether the batch has expired as of now.</param>
public record BatchStockInfo(
    int BatchId,
    string BatchNo,
    decimal RemainingQuantity,
    decimal OriginalQuantity,
    decimal UnitCost,
    DateTime? ExpiryDate,
    DateTime ReceivedDate,
    bool IsExpired
);
