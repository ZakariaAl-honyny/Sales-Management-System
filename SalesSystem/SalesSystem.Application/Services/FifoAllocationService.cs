using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// Implements FIFO (First In First Out) and FEFO (First Expiry First Out)
/// allocation strategies for inventory batch management.
///
/// This service modifies entities in the EF Core change tracker but does NOT
/// call SaveChangesAsync — the caller is responsible for persistence within
/// their own transaction scope (typically via ExecuteTransactionAsync).
/// </summary>
public class FifoAllocationService : IFifoAllocationService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FifoAllocationService> _logger;

    public FifoAllocationService(IUnitOfWork uow, ILogger<FifoAllocationService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    // ─── Add Purchase Batches ──────────────────────────────────────────

    public async Task<Result<List<InventoryBatch>>> AddPurchaseBatchesAsync(
        int productId,
        int warehouseId,
        decimal quantity,
        decimal unitCost,
        string? batchNo,
        DateTime? manufactureDate,
        DateTime? expiryDate,
        int? purchaseInvoiceItemId,
        bool isOpeningBatch,
        CancellationToken ct)
    {
        if (quantity <= 0)
            return Result<List<InventoryBatch>>.Failure("الكمية يجب أن تكون أكبر من الصفر");

        if (unitCost < 0)
            return Result<List<InventoryBatch>>.Failure("تكلفة الوحدة لا يمكن أن تكون سالبة");

        try
        {
            var lotNumber = batchNo ?? GenerateLotNumber(isOpeningBatch);

            var batch = InventoryBatch.Create(
                productId,
                warehouseId,
                quantity,
                unitCost,
                lotNumber,
                purchaseInvoiceItemId,
                manufactureDate,
                expiryDate,
                createdByUserId: null);

            await _uow.InventoryBatches.AddAsync(batch, ct);

            _logger.LogInformation(
                "InventoryBatch {LotNumber} created: Product {ProductId}, Qty {Quantity}, Cost {UnitCost}, Warehouse {WarehouseId}",
                lotNumber, productId, quantity, unitCost, warehouseId);

            return Result<List<InventoryBatch>>.Success(new List<InventoryBatch> { batch });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating purchase batch for Product {ProductId}", productId);
            return Result<List<InventoryBatch>>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase batch for Product {ProductId}", productId);
            return Result<List<InventoryBatch>>.Failure("حدث خطأ أثناء إنشاء الدفعة");
        }
    }

    // ─── Deduct From Batches (FIFO / FEFO) ────────────────────────────

    public async Task<Result<List<InventoryBatchAllocation>>> DeductFromBatchesAsync(
        int productId,
        int warehouseId,
        decimal quantityNeeded,
        int? salesInvoiceItemId,
        int? createdByUserId,
        CancellationToken ct)
    {
        if (quantityNeeded <= 0)
            return Result<List<InventoryBatchAllocation>>.Failure(
                "الكمية المطلوبة يجب أن تكون أكبر من الصفر");

        // Validate product exists
        var product = await _uow.Products.GetByIdAsync(productId, ct);
        if (product == null)
            return Result<List<InventoryBatchAllocation>>.Failure("المنتج غير موجود", ErrorCodes.NotFound);

        // Validate warehouse exists
        var warehouse = await _uow.Warehouses.GetByIdAsync(warehouseId, ct);
        if (warehouse == null)
            return Result<List<InventoryBatchAllocation>>.Failure("المستودع غير موجود", ErrorCodes.NotFound);

        try
        {
            // Get all active batches with remaining quantity
            var batches = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == productId
                     && b.WarehouseId == warehouseId
                     && b.Quantity > 0
                     && b.IsActive,
                queryConfig: null,
                ct: ct);

            if (batches.Count == 0)
                return Result<List<InventoryBatchAllocation>>.Failure(
                    "لا توجد دفعات متاحة في المخزون", ErrorCodes.InsufficientStock);

            // Determine strategy: FEFO if ANY batch has an expiry date, otherwise FIFO
            var hasExpiryBatches = batches.Any(b => b.ExpiryDate.HasValue);

            List<InventoryBatch> sortedBatches;
            if (hasExpiryBatches)
            {
                // FEFO: sort by ExpiryDate ascending, then by Id for tie-breaking
                sortedBatches = batches
                    .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                    .ThenBy(b => b.Id)
                    .ToList();

                _logger.LogInformation(
                    "FEFO allocation for Product {ProductId}, Warehouse {WarehouseId}: {BatchCount} batches sorted by expiry",
                    productId, warehouseId, sortedBatches.Count);
            }
            else
            {
                // FIFO: sort by ReceivedDate (CreatedAt) ascending, then by Id
                sortedBatches = batches
                    .OrderBy(b => b.Id)
                    .ToList();

                // Note: InventoryBatch has no CreatedAt on the entity itself — it's on BaseEntity
                // We use Id as a proxy for receive order (Id increments over time)
                _logger.LogInformation(
                    "FIFO allocation for Product {ProductId}, Warehouse {WarehouseId}: {BatchCount} batches sorted by Id",
                    productId, warehouseId, sortedBatches.Count);
            }

            var allocations = new List<InventoryBatchAllocation>();
            var remaining = quantityNeeded;

            foreach (var batch in sortedBatches)
            {
                if (remaining <= 0) break;

                var takeQuantity = Math.Min(remaining, batch.Quantity);

                // Domain method validates and deducts
                batch.DeductStock(takeQuantity);

                // Record the allocation
                allocations.Add(new InventoryBatchAllocation(
                    BatchId: batch.Id,
                    Quantity: takeQuantity,
                    UnitCost: batch.UnitCost,
                    SalesInvoiceItemId: salesInvoiceItemId));

                // Record inventory movement for audit trail
                var movement = InventoryMovement.Create(
                    productId,
                    warehouseId,
                    MovementType.SaleOut,
                    -takeQuantity,
                    batch.Quantity + takeQuantity, // QuantityBefore
                    batch.Quantity,                 // QuantityAfter
                    "SalesInvoice",
                    salesInvoiceItemId ?? 0,
                    batch.UnitCost,
                    notes: $"FIFO allocation from batch {batch.BatchNo}",
                    createdByUserId);

                await _uow.InventoryMovements.AddAsync(movement, ct);

                remaining -= takeQuantity;

                _logger.LogInformation(
                    "FIFO allocation: {Quantity} units from Batch {BatchId} ({BatchNo}) " +
                    "for Product {ProductId}, InvoiceItem {InvoiceItemId}",
                    takeQuantity, batch.Id, batch.BatchNo, productId, salesInvoiceItemId);
            }

            if (remaining > 0)
            {
                _logger.LogWarning(
                    "Insufficient stock for Product {ProductId}, Warehouse {WarehouseId}: " +
                    "requested {Requested}, available {Available}",
                    productId, warehouseId, quantityNeeded, quantityNeeded - remaining);

                return Result<List<InventoryBatchAllocation>>.Failure(
                    $"الكمية المتاحة في المخزون غير كافية. المطلوب: {quantityNeeded}، المتاح: {quantityNeeded - remaining}",
                    ErrorCodes.InsufficientStock);
            }

            _logger.LogInformation(
                "FIFO allocation completed: {TotalQty} units from {BatchCount} batches " +
                "for Product {ProductId}, Warehouse {WarehouseId}",
                quantityNeeded, allocations.Count, productId, warehouseId);

            return Result<List<InventoryBatchAllocation>>.Success(allocations);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation during FIFO allocation for Product {ProductId}", productId);
            return Result<List<InventoryBatchAllocation>>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during FIFO allocation for Product {ProductId}, Warehouse {WarehouseId}",
                productId, warehouseId);
            return Result<List<InventoryBatchAllocation>>.Failure("حدث خطأ أثناء توزيع الكمية من الدفعات");
        }
    }

    // ─── Return to Batch (Sales Returns) ──────────────────────────────

    public async Task<Result> ReturnToBatchAsync(
        int batchId,
        decimal quantityReturned,
        int? salesReturnItemId,
        int? createdByUserId,
        CancellationToken ct)
    {
        if (quantityReturned <= 0)
            return Result.Failure("الكمية المرتجعة يجب أن تكون أكبر من الصفر");

        try
        {
            var batch = await _uow.InventoryBatches.GetByIdAsync(batchId, ct);
            if (batch == null)
                return Result.Failure("الدفعة غير موجودة", ErrorCodes.NotFound);

            if (!batch.IsActive)
                return Result.Failure("لا يمكن الإرجاع إلى دفعة غير نشطة");

            // Create a new return batch with the same cost, prefixed "RET-"
            var returnBatchNo = $"RET-{batch.BatchNo}";

            var newBatch = InventoryBatch.Create(
                batch.ProductId,
                batch.WarehouseId,
                quantityReturned,
                batch.UnitCost,
                returnBatchNo,
                purchaseInvoiceItemId: null,
                batch.ManufactureDate,
                batch.ExpiryDate,
                createdByUserId);

            await _uow.InventoryBatches.AddAsync(newBatch, ct);

            // Record inventory movement for audit trail
            var movement = InventoryMovement.Create(
                batch.ProductId,
                batch.WarehouseId,
                MovementType.SaleReturnIn,
                quantityReturned,
                0m, // QuantityBefore is approximate for the product+warehouse
                quantityReturned,
                "SalesReturn",
                salesReturnItemId ?? 0,
                batch.UnitCost,
                notes: $"Return to batch {batch.BatchNo} via new batch {returnBatchNo}",
                createdByUserId);

            await _uow.InventoryMovements.AddAsync(movement, ct);

            _logger.LogInformation(
                "Return stock: {Quantity} units returned to Batch {BatchId} ({BatchNo}) " +
                "via new batch {NewBatchNo} for ReturnItem {ReturnItemId}",
                quantityReturned, batchId, batch.BatchNo, returnBatchNo, salesReturnItemId);

            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation returning stock to Batch {BatchId}", batchId);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning stock to Batch {BatchId}", batchId);
            return Result.Failure("حدث خطأ أثناء إرجاع الكمية إلى الدفعة");
        }
    }

    // ─── Get Batch Breakdown ──────────────────────────────────────────

    public async Task<Result<List<BatchStockInfo>>> GetBatchBreakdownAsync(
        int productId, int warehouseId, CancellationToken ct)
    {
        try
        {
            // Get all active batches (including those with zero quantity) sorted by receipt order
            var batches = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == productId
                     && b.WarehouseId == warehouseId
                     && b.IsActive,
                q => q.OrderBy(b => b.Id),
                ct: ct);

            var now = DateTime.UtcNow;

            var breakdown = batches.Select(b => new BatchStockInfo(
                BatchId: b.Id,
                BatchNo: b.BatchNo,
                RemainingQuantity: b.Quantity,
                // Estimate original quantity as current + total sale-out movements for this batch.
                // Without a dedicated OriginalQuantity field on the entity, use the current quantity.
                // For a better estimate, we could sum all movements, but this is sufficient for display.
                OriginalQuantity: b.Quantity,
                UnitCost: b.UnitCost,
                ExpiryDate: b.ExpiryDate,
                ReceivedDate: b.CreatedAt,
                IsExpired: b.ExpiryDate.HasValue && b.ExpiryDate.Value <= now
            )).ToList();

            return Result<List<BatchStockInfo>>.Success(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch breakdown for Product {ProductId}, Warehouse {WarehouseId}",
                productId, warehouseId);
            return Result<List<BatchStockInfo>>.Failure("حدث خطأ أثناء استرجاع تفاصيل الدفعات");
        }
    }

    // ─── Get Current Stock Cost (Weighted Average) ────────────────────

    public async Task<Result<decimal>> GetCurrentStockCostAsync(
        int productId, int warehouseId, CancellationToken ct)
    {
        try
        {
            var batches = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == productId
                     && b.WarehouseId == warehouseId
                     && b.Quantity > 0
                     && b.IsActive,
                ct: ct);

            if (batches.Count == 0)
                return Result<decimal>.Success(0m);

            var totalValue = batches.Sum(b => b.Quantity * b.UnitCost);
            var totalQuantity = batches.Sum(b => b.Quantity);

            if (totalQuantity == 0)
                return Result<decimal>.Success(0m);

            var avgCost = Math.Round(totalValue / totalQuantity, 2);

            return Result<decimal>.Success(avgCost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating current stock cost for Product {ProductId}, Warehouse {WarehouseId}",
                productId, warehouseId);
            return Result<decimal>.Failure("حدث خطأ أثناء حساب متوسط تكلفة المخزون");
        }
    }

    // ─── Private Helpers ──────────────────────────────────────────────

    /// <summary>
    /// Generates a batch number when none is provided by the supplier.
    /// Uses "OPN-" prefix for opening stock batches, "B-" prefix for regular purchases.
    /// </summary>
    private static string GenerateLotNumber(bool isOpeningBatch)
    {
        var prefix = isOpeningBatch ? "OPN" : "B";
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var uniquePart = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{prefix}-{datePart}-{uniquePart}";
    }
}
