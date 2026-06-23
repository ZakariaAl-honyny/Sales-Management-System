using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class SalesReturnService : ISalesReturnService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ICashBoxService _cashBoxService;
    private readonly IAccountingIntegrationService _accountingService;
    private readonly IProductCostService _productCostService;
    private readonly IFifoAllocationService _fifoAllocationService;
    private readonly ILogger<SalesReturnService> _logger;

    public SalesReturnService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        ICashBoxService cashBoxService,
        IAccountingIntegrationService accountingService,
        IProductCostService productCostService,
        IFifoAllocationService fifoAllocationService,
        ILogger<SalesReturnService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _cashBoxService = cashBoxService;
        _accountingService = accountingService;
        _productCostService = productCostService;
        _fifoAllocationService = fifoAllocationService;
        _logger = logger;
    }

    public async Task<Result<SalesReturnDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var sr = await _uow.SalesReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Customer", "Warehouse", "Lines.SalesInvoiceLine.Product");

        if (sr == null)
            return Result<SalesReturnDto>.Failure("مرتجع المبيعات غير موجود", ErrorCodes.NotFound);

        return Result<SalesReturnDto>.Success(MapToDto(sr));
    }

    public async Task<Result<PagedResult<SalesReturnDto>>> GetAllAsync(int? customerId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        System.Linq.Expressions.Expression<System.Func<SalesReturn, bool>> predicate = r =>
            (!customerId.HasValue || r.CustomerId == customerId.Value) &&
            (includeInactive || r.Status != InvoiceStatus.Cancelled);

        var includes = new[] { "Customer", "Warehouse" };

        var (items, total) = await _uow.SalesReturns.GetPagedAsync(
            predicate, q => q.OrderByDescending(r => r.ReturnDate), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<SalesReturnDto>>.Success(PagedResult<SalesReturnDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, int userId, CancellationToken ct)
    {
        // 1. Validation
        if (request.SalesInvoiceId <= 0)
            return Result<SalesReturnDto>.Failure("فاتورة المبيعات الأصلية مطلوبة");

        var invoice = await _uow.SalesInvoices.FirstOrDefaultAsync(
            i => i.Id == request.SalesInvoiceId, ct, "Items");

        if (invoice == null) return Result<SalesReturnDto>.Failure("الفاتورة الأصلية غير موجودة");

        foreach (var item in request.Items)
        {
            var originalLine = invoice.Items.FirstOrDefault(it => it.Id == item.SalesInvoiceLineId);
            if (originalLine == null)
                return Result<SalesReturnDto>.Failure($"المنتج غير موجود في الفاتورة الأصلية");

            if (item.Quantity > originalLine.Quantity)
                return Result<SalesReturnDto>.Failure($"الكمية المرتجعة للمنتج أكبر من الكمية المباعة ({originalLine.Quantity})");
        }

        // 2. Transaction
        return await _uow.ExecuteTransactionAsync<Result<SalesReturnDto>>(async () =>
        {
            try
            {
                var returnNoResult = await _sequenceService.GetNextIntAsync("SalesReturn", ct);
                if (!returnNoResult.IsSuccess) return Result<SalesReturnDto>.Failure(returnNoResult.Error!);

                var salesReturn = SalesReturn.Create(
                    returnNoResult.Value,
                    request.SalesInvoiceId ?? 0,
                    request.CustomerId ?? 0,
                    request.WarehouseId,
                    request.CurrencyId ?? 1,
                    request.ReturnDate,
                    request.Notes,
                    userId
                );

                foreach (var item in request.Items)
                {
                    var line = SalesReturnLine.Create(
                        item.SalesInvoiceLineId,
                        item.Quantity,
                        item.Amount);
                    salesReturn.AddLine(line);
                }

                await _uow.SalesReturns.AddAsync(salesReturn, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Sales Return created as Draft: {ReturnNo} (ID: {Id})", salesReturn.ReturnNo, salesReturn.Id);

                return await GetByIdAsync(salesReturn.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain exception creating sales return: {Message}", ex.Message);
                return Result<SalesReturnDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sales return");
                return Result<SalesReturnDto>.Failure("حدث خطأ أثناء حفظ المرتجع");
            }
        }, ct);
    }

    public async Task<Result<SalesReturnDto>> PostAsync(int id, PostSalesReturnRequest request, int userId, CancellationToken ct)
    {
        // Overload with request params — forward to main PostAsync
        return await PostAsync(id, userId, ct);
    }

    public async Task<Result<SalesReturnDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var sr = await _uow.SalesReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Lines.SalesInvoiceLine.Product", "SalesInvoice", "Customer", "Customer");

        if (sr == null) return Result<SalesReturnDto>.Failure("مرتجع المبيعات غير موجود");
        if (sr.Status != InvoiceStatus.Draft) return Result<SalesReturnDto>.Failure("يمكن فقط ترحيل المرتجعات المسودة");

        return await _uow.ExecuteTransactionAsync<Result<SalesReturnDto>>(async () =>
        {
            try
            {
                // ---- Proportional calculation from original invoice ----
                if (sr.SalesInvoice != null)
                {
                    var invoice = sr.SalesInvoice;
                    var ratio = invoice.SubTotal > 0
                        ? sr.TotalAmount / invoice.SubTotal
                        : 0;
                    sr.SetProportionalAmounts(
                        invoice.DiscountAmount * ratio,
                        invoice.TaxAmount * ratio,
                        invoice.OtherCharges * ratio,
                        invoice.TaxId);
                }

                sr.Post();
                await _uow.SaveChangesAsync(ct);

                // Update Stock
                foreach (var item in sr.Lines)
                {
                    await _inventoryService.IncreaseStockAsync(
                        item.SalesInvoiceLine != null ? item.SalesInvoiceLine.ProductId : 0,
                        sr.WarehouseId,
                        item.Quantity,
                        unitCost: item.Amount / (item.Quantity > 0 ? item.Quantity : 1),
                        userId: userId,
                        ct: ct);
                }

                // ─── FIFO Return to Batch ────────────────────────────────────────
                // For each returned item, find a reference batch for cost metadata,
                // then create a new return batch via ReturnToBatchAsync.
                foreach (var item in sr.Lines)
                {
                    var productId = item.SalesInvoiceLine?.ProductId ?? 0;
                    if (productId <= 0 || item.Quantity <= 0) continue;

                    var breakdownResult = await _fifoAllocationService.GetBatchBreakdownAsync(
                        productId, sr.WarehouseId, ct);

                    if (!breakdownResult.IsSuccess || breakdownResult.Value == null || breakdownResult.Value.Count == 0)
                    {
                        _logger.LogWarning(
                            "No reference batch found for sales return Product {ProductId}, " +
                            "Warehouse {WarehouseId} — creating return batch with default cost",
                            productId, sr.WarehouseId);

                        // Create a return batch via AddPurchaseBatchesAsync (no reference batch available)
                        var unitCost = item.Quantity > 0 ? item.Amount / item.Quantity : 0;
                        var fallbackResult = await _fifoAllocationService.AddPurchaseBatchesAsync(
                            productId,
                            sr.WarehouseId,
                            item.Quantity,
                            unitCost,
                            batchNo: $"SR-{sr.Id}-{item.Id}",
                            expiryDate: null,
                            purchaseInvoiceId: null,
                            isOpeningBatch: false,
                            ct: ct);

                        if (!fallbackResult.IsSuccess)
                        {
                            _logger.LogWarning("Fallback batch creation failed for sales return item {ItemId}: {Error}",
                                item.Id, fallbackResult.Error);
                        }

                        continue;
                    }

                    // Use the most recent batch as the reference for cost/expiry metadata
                    var referenceBatch = breakdownResult.Value.OrderByDescending(b => b.ReceivedDate).First();
                    var returnResult = await _fifoAllocationService.ReturnToBatchAsync(
                        referenceBatch.BatchId,
                        item.Quantity,
                        item.Id,
                        userId,
                        ct);

                    if (!returnResult.IsSuccess)
                    {
                        _logger.LogWarning("ReturnToBatchAsync failed for sales return item {ItemId}: {Error}",
                            item.Id, returnResult.Error);
                    }
                }

                // ─── InventoryTransaction Audit Trail ────────────────────────────
                var srTxSeq = await _sequenceService.GetNextIntAsync("InventoryTransaction", ct);
                if (srTxSeq.IsSuccess)
                {
                    var srInvTx = InventoryTransaction.Create(
                        srTxSeq.Value.ToString("D6"),
                        InventoryTransactionType.SaleReturn,
                        sr.WarehouseId,
                        InventoryReferenceType.SalesReturn,
                        sr.Id,
                        $"مرتجع مبيعات - رقم {sr.ReturnNo}",
                        userId);
                    foreach (var item in sr.Lines)
                    {
                        var productId = item.SalesInvoiceLine?.ProductId ?? 0;
                        var productUnitId = item.SalesInvoiceLine?.ProductUnitId ?? 0;
                        if (productId <= 0 || productUnitId <= 0) continue;
                        srInvTx.AddLine(InventoryTransactionLine.Create(
                            srInvTx.Id,
                            productUnitId,
                            item.Quantity,
                            item.Amount / (item.Quantity > 0 ? item.Quantity : 1),
                            null, null, sr.WarehouseId));
                    }
                    await _uow.InventoryTransactions.AddAsync(srInvTx, ct);
                }

                // Compute actual totalCost from product costs (FIX: was sr.TotalAmount)
                decimal totalCost = 0;
                foreach (var item in sr.Lines)
                {
                    var productId = item.SalesInvoiceLine?.ProductId ?? 0;
                    if (productId > 0 && item.Quantity > 0)
                    {
                        var costResult = await _productCostService.GetAverageCostAsync(productId, ct);
                        if (costResult.IsSuccess)
                            totalCost += item.Quantity * costResult.Value;
                    }
                }

                // Create journal entry for the sales return with actual cost
                var entryResult = await _accountingService.CreateSalesReturnEntryAsync(
                    sr, totalCost, userId, ct);
                if (!entryResult.IsSuccess)
                {
                    _logger.LogWarning("Journal entry creation failed for sales return {Id}: {Error}",
                        sr.Id, entryResult.Error);
                    return Result<SalesReturnDto>.Failure(entryResult.Error!);
                }

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Sales Return posted: {ReturnNo} (ID: {Id})", sr.ReturnNo, sr.Id);
                return await GetByIdAsync(sr.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain exception posting sales return {Id}: {Message}", id, ex.Message);
                return Result<SalesReturnDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error posting sales return {Id}", id);
                return Result<SalesReturnDto>.Failure("حدث خطأ أثناء ترحيل المرتجع");
            }
        }, ct);
    }

    public async Task<Result<SalesReturnDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var sr = await _uow.SalesReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Lines.SalesInvoiceLine", "Customer");

        if (sr == null) return Result<SalesReturnDto>.Failure("مرتجع المبيعات غير موجود");
        if (sr.Status == InvoiceStatus.Cancelled)
            return Result<SalesReturnDto>.Failure("مرتجع المبيعات ملغى بالفعل", ErrorCodes.InvalidOperation);

        return await _uow.ExecuteTransactionAsync<Result<SalesReturnDto>>(async () =>
        {
            try
            {
                if (sr.Status == InvoiceStatus.Posted)
                {
                    // Reverse Stock
                    foreach (var item in sr.Lines)
                    {
                        await _inventoryService.DecreaseStockAsync(
                            item.SalesInvoiceLine != null ? item.SalesInvoiceLine.ProductId : 0,
                            sr.WarehouseId,
                            item.Quantity,
                            unitCost: item.Amount / (item.Quantity > 0 ? item.Quantity : 1),
                            userId: userId,
                            ct: ct);
                    }

                    // Compute actual totalCost from product costs (same as in PostAsync)
                    decimal totalCost = 0;
                    foreach (var item in sr.Lines)
                    {
                        var productId = item.SalesInvoiceLine?.ProductId ?? 0;
                        if (productId > 0 && item.Quantity > 0)
                        {
                            var costResult = await _productCostService.GetAverageCostAsync(productId, ct);
                            if (costResult.IsSuccess)
                                totalCost += item.Quantity * costResult.Value;
                        }
                    }

                    // ─── FIFO Batch Reversal Warning ────────────────────────────────
                    // ReturnToBatchAsync creates new immutable batch records during Post.
                    // These batches are consumed (deducted) here on cancel.
                    // Batch records are immutable by design — we use DeductFromBatchesAsync
                    // to consume the return batches that were created during post.
                    foreach (var item in sr.Lines)
                    {
                        var productId = item.SalesInvoiceLine?.ProductId ?? 0;
                        if (productId <= 0 || item.Quantity <= 0) continue;

                        var deductResult = await _fifoAllocationService.DeductFromBatchesAsync(
                            productId,
                            sr.WarehouseId,
                            item.Quantity,
                            SalesInvoiceLineId: item.SalesInvoiceLineId,
                            createdByUserId: userId,
                            ct: ct);

                        if (!deductResult.IsSuccess)
                        {
                            _logger.LogWarning("Batch deduction for sales return cancel {Id}, Product {ProductId}: {Error}",
                                sr.Id, productId, deductResult.Error);
                        }
                    }

                    // ─── InventoryTransaction Reversal Audit Trail ────────────────
                    var srCancelTxSeq = await _sequenceService.GetNextIntAsync("InventoryTransaction", ct);
                    if (srCancelTxSeq.IsSuccess)
                    {
                        var srCancelInvTx = InventoryTransaction.Create(
                            srCancelTxSeq.Value.ToString("D6"),
                            InventoryTransactionType.Sale,   // reversing the return
                            sr.WarehouseId,
                            InventoryReferenceType.SalesReturn,
                            sr.Id,
                            $"إلغاء مرتجع مبيعات - رقم {sr.ReturnNo}",
                            userId);
                        foreach (var item in sr.Lines)
                        {
                            var productId = item.SalesInvoiceLine?.ProductId ?? 0;
                            var productUnitId = item.SalesInvoiceLine?.ProductUnitId ?? 0;
                            if (productId <= 0 || productUnitId <= 0) continue;
                            srCancelInvTx.AddLine(InventoryTransactionLine.Create(
                                srCancelInvTx.Id,
                                productUnitId,
                                item.Quantity,
                                item.Amount / (item.Quantity > 0 ? item.Quantity : 1),
                                null, null, sr.WarehouseId));
                        }
                        await _uow.InventoryTransactions.AddAsync(srCancelInvTx, ct);
                    }

                    // Reverse the journal entry for the posted sales return
                    var reversalResult = await _accountingService.ReverseSalesReturnEntryAsync(
                        sr, totalCost, userId, ct);
                    if (!reversalResult.IsSuccess)
                    {
                        _logger.LogWarning("Journal entry reversal failed for sales return {Id}: {Error}",
                            sr.Id, reversalResult.Error);
                        return Result<SalesReturnDto>.Failure(reversalResult.Error!);
                    }
                }

                sr.Cancel();
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("Sales Return cancelled: {ReturnNo} (ID: {Id})", sr.ReturnNo, sr.Id);
                return await GetByIdAsync(sr.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "Domain exception cancelling sales return {Id}: {Message}", id, ex.Message);
                return Result<SalesReturnDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling sales return {Id}", id);
                return Result<SalesReturnDto>.Failure("حدث خطأ أثناء إلغاء المرتجع");
            }
        }, ct);
    }

    private static SalesReturnDto MapToDto(SalesReturn r)
    {
        return new SalesReturnDto(
            r.Id,
            r.ReturnNo.ToString(),
            r.WarehouseId,
            r.Warehouse?.Name ?? "غير معروف",
            r.CustomerId,
            r.Customer?.Name ?? "غير معروف",
            r.SalesInvoiceId,
            r.ReturnDate,
            0, // SubTotal removed
            r.ReturnedTaxAmount,
            r.ReturnedDiscountAmount,
            r.TotalAmount,
            r.ReturnedDiscountAmount,
            r.ReturnedTaxAmount,
            r.ReturnedChargeAmount,
            r.TaxId,
            r.CurrencyId,
            null, // ExchangeRate removed
            r.Notes,
            (byte)r.Status,
            null, // CashBoxId removed
            null, // CashBoxName removed
            0, // RefundAmount removed
            r.Lines.Select(it => new SalesReturnItemDto(
                it.Id,
                it.SalesInvoiceLine?.ProductId ?? 0,
                it.SalesInvoiceLine?.ProductUnitId ?? 0,
                it.SalesInvoiceLine?.Product?.Name ?? "غير معروف",
                it.Quantity,
                it.Amount,
                0, // DiscountAmount per line (not tracked separately)
                it.Amount,
                1 // Mode default (Retail)
            )).ToList()
        );
    }
}
