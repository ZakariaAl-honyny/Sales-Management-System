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
    private readonly ILogger<SalesReturnService> _logger;

    public SalesReturnService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        ICashBoxService cashBoxService,
        ILogger<SalesReturnService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _cashBoxService = cashBoxService;
        _logger = logger;
    }

    public async Task<Result<SalesReturnDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var sr = await _uow.SalesReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Customer", "Warehouse", "Items.Product");

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
        if (request.SalesInvoiceId.HasValue)
        {
            var invoice = await _uow.SalesInvoices.FirstOrDefaultAsync(
                i => i.Id == request.SalesInvoiceId.Value, ct, "Items");

            if (invoice == null) return Result<SalesReturnDto>.Failure("الفاتورة الأصلية غير موجودة");

            foreach (var item in request.Items)
            {
                var originalLine = invoice.Items.FirstOrDefault(it => it.ProductId == item.ProductId);
                if (originalLine == null)
                    return Result<SalesReturnDto>.Failure($"المنتج {item.ProductId} غير موجود في الفاتورة الأصلية");

                if (item.Quantity > originalLine.Quantity)
                    return Result<SalesReturnDto>.Failure($"الكمية المرتجعة للمنتج {item.ProductId} أكبر من الكمية المباعة ({originalLine.Quantity})");
            }
        }

        // 2. Transaction
        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                var returnNoResult = await _sequenceService.GetNextNumberAsync("SR", ct);
                if (!returnNoResult.IsSuccess) return Result<SalesReturnDto>.Failure(returnNoResult.Error!);

                var salesReturn = SalesReturn.Create(
                    returnNoResult.Value!,
                    (short)request.WarehouseId,
                    request.CustomerId,
                    request.SalesInvoiceId,
                    request.ReturnDate,
                    request.Notes,
                    userId: userId,
                    cashBoxId: request.CashBoxId,
                    refundAmount: request.RefundAmount ?? 0
                );

                foreach (var item in request.Items)
                {
                    salesReturn.AddItem(item.ProductId, item.Quantity, item.UnitPrice, item.DiscountAmount, (SaleMode)item.Mode, item.Notes);
                }

                await _uow.SalesReturns.AddAsync(salesReturn, ct);
                await _uow.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                _logger.LogInformation("Sales Return created as Draft: {ReturnNo} (ID: {Id})", salesReturn.ReturnNo, salesReturn.Id);

                return await GetByIdAsync(salesReturn.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain exception creating sales return: {Message}", ex.Message);
                return Result<SalesReturnDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error creating sales return");
                return Result<SalesReturnDto>.Failure("حدث خطأ أثناء حفظ المرتجع");
            }
        }, ct);
    }

    public async Task<Result<SalesReturnDto>> PostAsync(int id, PostSalesReturnRequest request, int userId, CancellationToken ct)
    {
        return await PostAsync(id, userId, ct);
    }

    public async Task<Result<SalesReturnDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var sr = await _uow.SalesReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Items.Product");

        if (sr == null) return Result<SalesReturnDto>.Failure("مرتجع المبيعات غير موجود");
        if (sr.Status != InvoiceStatus.Draft) return Result<SalesReturnDto>.Failure("يمكن فقط ترحيل المرتجعات المسودة");


        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                sr.Post();
                await _uow.SaveChangesAsync(ct);

                // Phase 25: GetRetailQuantityEquivalent removed. Quantity is in base units.
                // Update Stock
                foreach (var item in sr.Items)
                {
                    await _inventoryService.IncreaseStockAsync(
                        item.ProductId,
                        sr.WarehouseId,
                        item.Quantity,
                        unitCost: item.UnitPrice,
                        userId: userId,
                        ct: ct);
                }

                // Create payment voucher (سند صرف) for refund if CashBoxId is set
                // TODO: Use proper currencyId and accountId when available
                if (sr.CashBoxId.HasValue && sr.RefundAmount > 0)
                {
                    var cashResult = await _cashBoxService.RecordInvoicePaymentAsync(
                        sr.CashBoxId.Value,
                        currencyId: 1, // Default SAR
                        sr.RefundAmount,
                        accountId: 1, // Default cash account
                        notes: "مردود مبيعات",
                        sourceDocumentId: sr.Id,
                        sourceDocumentType: "SalesReturn",
                        userId: userId,
                        ct: ct);

                    if (!cashResult.IsSuccess)
                    {
                        _logger.LogWarning("Payment voucher recording failed for sales return {Id}: {Error}",
                            sr.Id, cashResult.Error);
                    }
                }

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Sales Return posted: {ReturnNo} (ID: {Id})", sr.ReturnNo, sr.Id);
                return await GetByIdAsync(sr.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain exception posting sales return {Id}: {Message}", id, ex.Message);
                return Result<SalesReturnDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error posting sales return {Id}", id);
                return Result<SalesReturnDto>.Failure("حدث خطأ أثناء ترحيل المرتجع");
            }
        }, ct);
    }

    public async Task<Result<SalesReturnDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var sr = await _uow.SalesReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Items.Product");

        if (sr == null) return Result<SalesReturnDto>.Failure("مرتجع المبيعات غير موجود");
        if (sr.Status == InvoiceStatus.Cancelled)
            return Result<SalesReturnDto>.Failure("مرتجع المبيعات ملغى بالفعل", ErrorCodes.InvalidOperation);

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                if (sr.Status == InvoiceStatus.Posted)
                {
                    // Reverse Stock
                    foreach (var item in sr.Items)
                    {
                        // Phase 25: GetRetailQuantityEquivalent removed.
                        await _inventoryService.DecreaseStockAsync(
                            item.ProductId,
                            sr.WarehouseId,
                            item.Quantity,
                            unitCost: item.UnitPrice,
                            userId: userId,
                            ct: ct);
                    }

                }

                sr.Cancel();
                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Sales Return cancelled: {ReturnNo} (ID: {Id})", sr.ReturnNo, sr.Id);
                return await GetByIdAsync(sr.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain exception cancelling sales return {Id}: {Message}", id, ex.Message);
                return Result<SalesReturnDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error cancelling sales return {Id}", id);
                return Result<SalesReturnDto>.Failure("حدث خطأ أثناء إلغاء المرتجع");
            }
        }, ct);
    }

    private static SalesReturnDto MapToDto(SalesReturn r)
    {
        return new SalesReturnDto(
            r.Id,
            r.ReturnNo,
            r.WarehouseId,
            r.Warehouse?.Name ?? "غير معروف",
            r.CustomerId,
            r.Customer?.Party?.Name ?? "غير معروف",
            r.SalesInvoiceId,
            r.ReturnDate,
            r.SubTotal,
            0, // TaxAmount (not in entity yet)
            0, // DiscountAmount (not in entity yet)
            r.TotalAmount,
            r.CurrencyId,
            r.ExchangeRate,
            r.Notes,
            (byte)r.Status,
            r.CashBoxId,
            null, // CashBoxName — not loaded via navigation
            r.RefundAmount,
            r.Items.Select(it => new SalesReturnItemDto(
                it.Id,
                it.ProductId,
                it.Product?.Name ?? "غير معروف",
                it.Quantity,
                it.UnitPrice,
                it.DiscountAmount,
                it.LineTotal,
                (byte)it.Mode
            )).ToList()
        );
    }
}
