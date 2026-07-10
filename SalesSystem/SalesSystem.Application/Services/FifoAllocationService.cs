using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
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
    private readonly ISystemSettingsRepository _systemSettingsRepo;

    public FifoAllocationService(IUnitOfWork uow, ILogger<FifoAllocationService> logger, ISystemSettingsRepository systemSettingsRepo)
    {
        _uow = uow;
        _logger = logger;
        _systemSettingsRepo = systemSettingsRepo;
    }

    // ─── Add Purchase Batches ──────────────────────────────────────────

    public async Task<Result<List<InventoryBatch>>> AddPurchaseBatchesAsync(
        int productId,
        short warehouseId,
        decimal quantity,
        decimal unitCost,
        string? batchNo,
        DateOnly? expiryDate,
        int? purchaseInvoiceId,
        bool isOpeningBatch,
        CancellationToken ct)
    {
        if (quantity <= 0)
            return Result<List<InventoryBatch>>.Failure("الكمية يجب أن تكون أكبر من الصفر");

        if (unitCost < 0)
            return Result<List<InventoryBatch>>.Failure("تكلفة الوحدة لا يمكن أن تكون سالبة");

        try
        {
            var lotNumberStr = batchNo ?? GenerateLotNumber(isOpeningBatch).ToString();

            var batch = InventoryBatch.Create(
                lotNumberStr,
                productId,
                warehouseId,
                quantity,
                unitCost,
                purchaseInvoiceId);

            await _uow.InventoryBatches.AddAsync(batch, ct);

            _logger.LogInformation(
                "InventoryBatch {LotNumber} created: Product {ProductId}, Qty {Quantity}, Cost {UnitCost}, Warehouse {WarehouseId}",
                lotNumberStr, productId, quantity, unitCost, warehouseId);

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
        short warehouseId,
        decimal quantityNeeded,
        int? SalesInvoiceLineId,
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
                     && b.QuantityRemaining > 0,
                queryConfig: null,
                ct: ct);

            if (batches.Count == 0)
                return Result<List<InventoryBatchAllocation>>.Failure(
                    "لا توجد دفعات متاحة في المخزون", ErrorCodes.InsufficientStock);

            // Determine strategy: FEFO only if EnableFefo setting is true AND any batch has an expiry date
            var enableFefo = await _systemSettingsRepo.GetBoolAsync("EnableFefo", false, ct);
            var useFefo = enableFefo && batches.Any(b => b.ExpiryDate.HasValue);

            List<InventoryBatch> sortedBatches;
            if (useFefo)
            {
                // FEFO: sort by ExpiryDate ascending
                sortedBatches = batches
                    .OrderBy(b => b.ExpiryDate.HasValue ? b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MaxValue)
                    .ThenBy(b => b.Id)
                    .ToList();

                _logger.LogInformation(
                    "FEFO allocation for Product {ProductId}, Warehouse {WarehouseId}: {BatchCount} batches sorted by expiry",
                    productId, warehouseId, sortedBatches.Count);
            }
            else
            {
                // FIFO: sort by Id (receive order)
                sortedBatches = batches
                    .OrderBy(b => b.Id)
                    .ToList();

                _logger.LogInformation(
                    "FIFO allocation for Product {ProductId}, Warehouse {WarehouseId}: {BatchCount} batches sorted by Id",
                    productId, warehouseId, sortedBatches.Count);
            }

            var allocations = new List<InventoryBatchAllocation>();
            var remaining = quantityNeeded;

            foreach (var batch in sortedBatches)
            {
                if (remaining <= 0) break;

                var takeQuantity = Math.Min(remaining, batch.QuantityRemaining);

                // Domain method validates and deducts
                batch.Deduct(takeQuantity);

                // Record the allocation
                allocations.Add(new InventoryBatchAllocation(
                    BatchId: batch.Id,
                    Quantity: takeQuantity,
                    UnitCost: batch.UnitCost,
                    SalesInvoiceLineId: SalesInvoiceLineId));

                _logger.LogInformation(
                    "FIFO allocation: {Quantity} units from Batch {BatchId} ({BatchNo}) " +
                    "for Product {ProductId}, InvoiceItem {InvoiceItemId}",
                    takeQuantity, batch.Id, batch.BatchNo, productId, SalesInvoiceLineId);

                remaining -= takeQuantity;
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

            // Generate a new batch number for the return batch
            var returnBatchNo = GenerateReturnBatchNo();

            var newBatch = InventoryBatch.Create(
                batchNo: returnBatchNo.ToString(),
                productId: batch.ProductId,
                warehouseId: batch.WarehouseId,
                quantityReceived: quantityReturned,
                unitCost: batch.UnitCost,
                purchaseInvoiceId: null,
                purchaseInvoiceLineId: null,
                supplierBatchNo: null,
                expiryDate: batch.ExpiryDate,
                createdByUserId: createdByUserId);

            await _uow.InventoryBatches.AddAsync(newBatch, ct);

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
        int productId, short warehouseId, CancellationToken ct)
    {
        try
        {
            var batches = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == productId && b.WarehouseId == warehouseId,
                q => q.OrderBy(b => b.Id),
                ct: ct);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var breakdown = batches.Select(b => new BatchStockInfo(
                BatchId: b.Id,
                BatchNo: b.BatchNo,
                RemainingQuantity: b.QuantityRemaining,
                OriginalQuantity: b.QuantityReceived,
                UnitCost: b.UnitCost,
                ExpiryDate: b.ExpiryDate.HasValue ? b.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) : null,
                ReceivedDate: b.CreatedAt,
                IsExpired: b.ExpiryDate.HasValue && b.ExpiryDate.Value <= today
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
        int productId, short warehouseId, CancellationToken ct)
    {
        try
        {
            var batches = await _uow.InventoryBatches.ToListAsync(
                b => b.ProductId == productId
                     && b.WarehouseId == warehouseId
                     && b.QuantityRemaining > 0,
                ct: ct);

            if (batches.Count == 0)
                return Result<decimal>.Success(0m);

            var totalValue = batches.Sum(b => b.QuantityRemaining * b.UnitCost);
            var totalQuantity = batches.Sum(b => b.QuantityRemaining);

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

    private static int GenerateLotNumber(bool isOpeningBatch)
    {
        var ticks = (int)(DateTime.UtcNow.Ticks % 100000000);
        return ticks;
    }

    private static int GenerateReturnBatchNo()
    {
        return (int)(DateTime.UtcNow.Ticks % 100000000);
    }
}
