using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// خدمة مرتجعات المشتريات — تدعم الربط بالفاتورة والمرتجعات المستقلة والعملات.
/// </summary>
public class PurchaseReturnService : IPurchaseReturnService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ISystemSettingsRepository _systemSettingsRepo;
    private readonly IAccountingIntegrationService _accountingService;
    private readonly IFifoAllocationService _fifoAllocationService;
    private readonly ILogger<PurchaseReturnService> _logger;

    public PurchaseReturnService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        ISystemSettingsRepository systemSettingsRepo,
        IAccountingIntegrationService accountingService,
        IFifoAllocationService fifoAllocationService,
        ILogger<PurchaseReturnService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _systemSettingsRepo = systemSettingsRepo;
        _accountingService = accountingService;
        _fifoAllocationService = fifoAllocationService;
        _logger = logger;
    }

    public async Task<Result<PurchaseReturnDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var pr = await _uow.PurchaseReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Supplier", "Warehouse", "Items.Product", "Items.ProductUnit", "Currency");

        if (pr == null)
            return Result<PurchaseReturnDto>.Failure("مرتجع المشتريات غير موجود", ErrorCodes.NotFound);

        return Result<PurchaseReturnDto>.Success(MapToDto(pr));
    }

    public async Task<Result<PagedResult<PurchaseReturnDto>>> GetAllAsync(
        int? supplierId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        Expression<Func<PurchaseReturn, bool>> predicate = r =>
            (includeInactive || r.Status != InvoiceStatus.Cancelled) &&
            (!supplierId.HasValue || r.SupplierId == supplierId.Value);

        var totalItems = await _uow.PurchaseReturns.CountAsync(predicate, ct);
        var items = await _uow.PurchaseReturns.ToListAsync(
            predicate,
            q => q.OrderByDescending(r => r.ReturnDate).Skip((page - 1) * pageSize).Take(pageSize),
            ct,
            false,
            "Supplier", "Warehouse", "Lines.PurchaseInvoiceLine.Product");

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<PurchaseReturnDto>>.Success(PagedResult<PurchaseReturnDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest request, int userId, CancellationToken ct)
    {
        // ─── Validate linked invoice ─────────────────────────────────────────
        if (request.PurchaseInvoiceId.HasValue)
        {
            var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
                i => i.Id == request.PurchaseInvoiceId.Value, ct, "Items");

            if (invoice == null)
                return Result<PurchaseReturnDto>.Failure("الفاتورة الأصلية غير موجودة");

            foreach (var item in request.Items)
            {
                var originalLine = invoice.Items.FirstOrDefault(it => it.Id == item.PurchaseInvoiceLineId);
                if (originalLine == null)
                    return Result<PurchaseReturnDto>.Failure($"المنتج غير موجود في الفاتورة الأصلية");

                if (item.Quantity > originalLine.Quantity)
                    return Result<PurchaseReturnDto>.Failure(
                        $"الكمية المرتجعة للمنتج أكبر من الكمية المشتراة ({originalLine.Quantity})");
            }
        }

        // ─── Stock validation before transaction ─────────────────────────────
        var allowNegativeStock = await _systemSettingsRepo.GetBoolAsync("AllowNegativeStock", false, ct);

        // ─── Transaction ─────────────────────────────────────────────────────
        return await _uow.ExecuteTransactionAsync<Result<PurchaseReturnDto>>(async () =>
        {
            try
            {
                var returnNoResult = await _sequenceService.GetNextIntAsync("PurchaseReturn", ct);
                if (!returnNoResult.IsSuccess)
                    return Result<PurchaseReturnDto>.Failure(returnNoResult.Error!);

                var purchaseReturn = PurchaseReturn.Create(
                    returnNoResult.Value,
                    request.SupplierId,
                    (short)request.WarehouseId,
                    (short)(request.CurrencyId ?? 1),
                    request.ReturnDate.HasValue
                        ? DateOnly.FromDateTime(request.ReturnDate.Value)
                        : DateOnly.FromDateTime(DateTime.UtcNow),
                    request.PurchaseInvoiceId,
                    request.Notes,
                    userId);

                foreach (var item in request.Items)
                {
                    var line = PurchaseReturnLine.Create(
                        item.ProductId,
                        item.ProductUnitId,
                        item.Quantity,
                        item.Amount,
                        item.PurchaseInvoiceLineId);
                    purchaseReturn.AddLine(line);
                }

                await _uow.PurchaseReturns.AddAsync(purchaseReturn, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("تم إنشاء مرتجع مشتريات كمسودة: {ReturnNo} (المعرف {Id})",
                    purchaseReturn.ReturnNo, purchaseReturn.Id);

                return await GetByIdAsync(purchaseReturn.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "خطأ في المجال أثناء إنشاء مرتجع المشتريات: {Message}", ex.Message);
                return Result<PurchaseReturnDto>.Failure(ex.Message);
            }
        }, ct);
    }

    public async Task<Result<PurchaseReturnDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var pr = await _uow.PurchaseReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Lines.PurchaseInvoiceLine.Product", "PurchaseInvoice", "Supplier");

        if (pr == null)
            return Result<PurchaseReturnDto>.Failure("مرتجع المشتريات غير موجود");
        if (pr.Status != InvoiceStatus.Draft)
            return Result<PurchaseReturnDto>.Failure("يمكن فقط ترحيل المرتجعات المسودة");

        var allowNegativeStock = await _systemSettingsRepo.GetBoolAsync("AllowNegativeStock", false, ct);

        foreach (var item in pr.Lines)
        {
            var productId = item.PurchaseInvoiceLine?.ProductId ?? 0;
            if (productId <= 0) continue;
            var stockValidation = await _inventoryService.ValidateStockAsync(
                productId, pr.WarehouseId, item.Quantity, allowNegativeStock, ct);
            if (!stockValidation.IsSuccess)
                return Result<PurchaseReturnDto>.Failure(stockValidation.Error!);
        }

        // ---- Proportional calculation from original invoice ----
        if (pr.PurchaseInvoiceId.HasValue && pr.PurchaseInvoice != null)
        {
            var invoice = pr.PurchaseInvoice;
            var ratio = invoice.SubTotal > 0
                ? pr.TotalAmount / invoice.SubTotal
                : 0;
            pr.SetProportionalAmounts(
                invoice.DiscountAmount * ratio,
                invoice.TaxAmount * ratio,
                invoice.OtherCharges * ratio,
                invoice.TaxId);
        }

        return await _uow.ExecuteTransactionAsync<Result<PurchaseReturnDto>>(async () =>
        {
            try
            {
                pr.Post();
                await _uow.SaveChangesAsync(ct);

                // Update Stock — decrease from warehouse
                foreach (var item in pr.Lines)
                {
                    var productId = item.PurchaseInvoiceLine?.ProductId ?? 0;
                    if (productId <= 0) continue;
                    await _inventoryService.DecreaseStockAsync(
                        productId,
                        pr.WarehouseId,
                        item.Quantity,
                        unitCost: item.Amount / (item.Quantity > 0 ? item.Quantity : 1),
                        userId: userId,
                        ct: ct);
                }

                await _uow.SaveChangesAsync(ct);

                // ─── FIFO Batch Deduction ────────────────────────────────────────
                // Deduct from earliest batches for each returned item
                foreach (var item in pr.Lines)
                {
                    var productId = item.PurchaseInvoiceLine?.ProductId ?? 0;
                    if (productId <= 0 || item.Quantity <= 0) continue;

                    var fifoResult = await _fifoAllocationService.DeductFromBatchesAsync(
                        productId,
                        pr.WarehouseId,
                        item.Quantity,
                        SalesInvoiceLineId: null,   // purchase return — not linked to a sales line
                        createdByUserId: userId,
                        ct: ct);

                    if (!fifoResult.IsSuccess)
                    {
                        _logger.LogWarning("FIFO batch deduction failed for purchase return {Id}, Product {ProductId}: {Error}",
                            pr.Id, productId, fifoResult.Error);
                        return Result<PurchaseReturnDto>.Failure(fifoResult.Error!);
                    }
                }

                // ─── InventoryTransaction Audit Trail ────────────────────────────
                var prTxSeq = await _sequenceService.GetNextIntAsync("InventoryTransaction", ct);
                if (prTxSeq.IsSuccess)
                {
                    var prInvTx = InventoryTransaction.Create(
                        prTxSeq.Value.ToString("D6"),
                        InventoryTransactionType.PurchaseReturn,
                        pr.WarehouseId,
                        InventoryReferenceType.PurchaseReturn,
                        pr.Id,
                        $"مرتجع شراء - رقم {pr.ReturnNo}",
                        userId);
                    foreach (var item in pr.Lines)
                    {
                        var productId = item.PurchaseInvoiceLine?.ProductId ?? 0;
                        if (productId <= 0) continue;
                        prInvTx.AddLine(InventoryTransactionLine.Create(
                            prInvTx.Id,
                            item.ProductUnitId,
                            item.Quantity,
                            item.Amount / (item.Quantity > 0 ? item.Quantity : 1),
                            null, null, pr.WarehouseId));
                    }
                    await _uow.InventoryTransactions.AddAsync(prInvTx, ct);
                }

                // Create accounting entry for the purchase return
                var entryResult = await _accountingService.CreatePurchaseReturnEntryAsync(pr, userId, ct);
                if (!entryResult.IsSuccess)
                {
                    _logger.LogWarning("فشل إنشاء القيد المحاسبي لمردود المشتريات {Id}: {Error}", id, entryResult.Error);
                    return Result<PurchaseReturnDto>.Failure(entryResult.Error!);
                }

                _logger.LogInformation("تم ترحيل مرتجع المشتريات: {ReturnNo} (المعرف {Id}) — القيد المحاسبي {EntryId}",
                    pr.ReturnNo, pr.Id, entryResult.Value);
                return await GetByIdAsync(pr.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "خطأ في المجال أثناء ترحيل مرتجع المشتريات {Id}: {Message}", id, ex.Message);
                return Result<PurchaseReturnDto>.Failure(ex.Message);
            }
        }, ct);
    }

    public async Task<Result<PurchaseReturnDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var pr = await _uow.PurchaseReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Lines.PurchaseInvoiceLine.Product", "Supplier");

        if (pr == null)
            return Result<PurchaseReturnDto>.Failure("مرتجع المشتريات غير موجود");
        if (pr.Status == InvoiceStatus.Cancelled)
            return Result<PurchaseReturnDto>.Failure("مرتجع المشتريات ملغي بالفعل", ErrorCodes.InvalidOperation);

        return await _uow.ExecuteTransactionAsync<Result<PurchaseReturnDto>>(async () =>
        {
            try
            {
                if (pr.Status == InvoiceStatus.Posted)
                {
                    // Reverse Stock — increase back
                    foreach (var item in pr.Lines)
                    {
                        var productId = item.PurchaseInvoiceLine?.ProductId ?? 0;
                        if (productId <= 0) continue;
                        await _inventoryService.IncreaseStockAsync(
                            productId,
                            pr.WarehouseId,
                            item.Quantity,
                            unitCost: item.Amount / (item.Quantity > 0 ? item.Quantity : 1),
                            userId: userId,
                            ct: ct);
                    }

                    // ─── FIFO Batch Restoration ───────────────────────────────────
                    // Restore batches since goods are coming back
                    foreach (var item in pr.Lines)
                    {
                        var productId = item.PurchaseInvoiceLine?.ProductId ?? 0;
                        if (productId <= 0 || item.Quantity <= 0) continue;

                        var batchResult = await _fifoAllocationService.AddPurchaseBatchesAsync(
                            productId,
                            pr.WarehouseId,
                            item.Quantity,
                            item.Amount / (item.Quantity > 0 ? item.Quantity : 1),
                            batchNo: $"CNL-PR-{pr.Id}-{item.Id}",
                            expiryDate: null,
                            purchaseInvoiceId: null,
                            isOpeningBatch: false,
                            ct: ct);

                        if (!batchResult.IsSuccess)
                        {
                            _logger.LogWarning("Batch restoration failed for purchase return cancel {Id}, Product {ProductId}: {Error}",
                                pr.Id, productId, batchResult.Error);
                            return Result<PurchaseReturnDto>.Failure(batchResult.Error!);
                        }
                    }

                    // ─── InventoryTransaction Reversal Audit Trail ────────────────
                    var prCancelTxSeq = await _sequenceService.GetNextIntAsync("InventoryTransaction", ct);
                    if (prCancelTxSeq.IsSuccess)
                    {
                        var prCancelInvTx = InventoryTransaction.Create(
                            prCancelTxSeq.Value.ToString("D6"),
                            InventoryTransactionType.Purchase,   // reversing the return
                            pr.WarehouseId,
                            InventoryReferenceType.PurchaseReturn,
                            pr.Id,
                            $"إلغاء مرتجع شراء - رقم {pr.ReturnNo}",
                            userId);
                        foreach (var item in pr.Lines)
                        {
                            var productId = item.PurchaseInvoiceLine?.ProductId ?? 0;
                            if (productId <= 0) continue;
                            prCancelInvTx.AddLine(InventoryTransactionLine.Create(
                                prCancelInvTx.Id,
                                item.ProductUnitId,
                                item.Quantity,
                                item.Amount / (item.Quantity > 0 ? item.Quantity : 1),
                                null, null, pr.WarehouseId));
                        }
                        await _uow.InventoryTransactions.AddAsync(prCancelInvTx, ct);
                    }

                    // Reverse the accounting entry for the posted purchase return
                    var entryResult = await _accountingService.ReversePurchaseReturnEntryAsync(pr, userId, ct);
                    if (!entryResult.IsSuccess)
                    {
                        _logger.LogWarning("فشل إنشاء قيد عكس المحاسبة لمردود المشتريات {Id}: {Error}", id, entryResult.Error);
                        return Result<PurchaseReturnDto>.Failure(entryResult.Error!);
                    }
                }

                pr.Cancel();
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("تم إلغاء مرتجع المشتريات: {ReturnNo} (المعرف {Id})", pr.ReturnNo, pr.Id);
                return await GetByIdAsync(pr.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "خطأ في المجال أثناء إلغاء مرتجع المشتريات {Id}: {Message}", id, ex.Message);
                return Result<PurchaseReturnDto>.Failure(ex.Message);
            }
        }, ct);
    }

    public async Task<Result<Dictionary<int, decimal>>> GetReturnedQuantitiesByInvoiceAsync(int invoiceId, CancellationToken ct)
    {
        try
        {
            // Query all POSTED purchase returns linked to this invoice
            var returns = await _uow.PurchaseReturns.ToListAsync(
                r => r.PurchaseInvoiceId == invoiceId && r.Status == InvoiceStatus.Posted,
                null,
                ct,
                false,
                "Items");

            // Aggregate quantities returned per product
            var result = new Dictionary<int, decimal>();
            foreach (var pr in returns)
            {
                foreach (var item in pr.Lines)
                {
                    var productId = item.PurchaseInvoiceLine?.ProductId ?? 0;
                    if (productId <= 0) continue;
                    if (result.ContainsKey(productId))
                        result[productId] += item.Quantity;
                    else
                        result[productId] = item.Quantity;
                }
            }

            return Result<Dictionary<int, decimal>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting returned quantities for invoice {InvoiceId}", invoiceId);
            return Result<Dictionary<int, decimal>>.Failure("حدث خطأ أثناء جلب كميات المرتجعات");
        }
    }

    private static PurchaseReturnDto MapToDto(PurchaseReturn r)
    {
        return new PurchaseReturnDto(
            r.Id,
            r.ReturnNo,
            r.WarehouseId,
            r.Warehouse?.Name ?? "غير معروف",
            r.SupplierId,
            r.Supplier?.Name ?? "غير معروف",
            r.PurchaseInvoiceId,
            r.PurchaseInvoiceId.HasValue, // LinkToInvoice = true when linked to an invoice
            r.ReturnDate,
            0, // SubTotal removed
            r.TotalAmount,
            r.ReturnedDiscountAmount,
            r.ReturnedTaxAmount,
            r.ReturnedChargeAmount,
            r.TaxId,
            r.CurrencyId,
            null, // ExchangeRate removed
            r.Notes,
            (byte)r.Status,
            r.Lines.Select(it => new PurchaseReturnItemDto(
                it.Id,
                it.ProductId,
                it.Product?.Name ?? "غير معروف",
                it.ProductUnitId,
                it.ProductUnit?.Unit?.Name ?? null,
                it.Quantity,
                it.Amount,
                it.Amount,
                it.PurchaseInvoiceLineId
            )).ToList()
        );
    }
}
