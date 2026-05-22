using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class SalesReturnService : ISalesReturnService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<SalesReturnService> _logger;

    public SalesReturnService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        ILogger<SalesReturnService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<SalesReturnDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var sr = await _uow.SalesReturns.Query()
            .Include(r => r.Customer)
            .Include(r => r.Warehouse)
            .Include(r => r.Items)
                .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (sr == null)
            return Result<SalesReturnDto>.Failure("مرتجع المبيعات غير موجود", ErrorCodes.NotFound);

        return Result<SalesReturnDto>.Success(MapToDto(sr));
    }

    public async Task<Result<PagedResult<SalesReturnDto>>> GetAllAsync(int? customerId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _uow.SalesReturns.Query()
            .Include(r => r.Customer)
            .Include(r => r.Warehouse)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(r => r.Status != (InvoiceStatus)3); // 3 = Cancelled
        }

        if (customerId.HasValue) query = query.Where(r => r.CustomerId == customerId.Value);

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.ReturnDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<SalesReturnDto>>.Success(PagedResult<SalesReturnDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, int userId, CancellationToken ct)
    {
        // 1. Validation
        if (request.SalesInvoiceId.HasValue)
        {
            var invoice = await _uow.SalesInvoices.Query()
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == request.SalesInvoiceId.Value, ct);

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
                    request.WarehouseId,
                    request.CustomerId,
                    request.SalesInvoiceId,
                    request.ReturnDate,
                    request.Notes,
                    userId
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

    public async Task<Result<SalesReturnDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var sr = await _uow.SalesReturns.Query()
            .Include(r => r.Items)
                .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (sr == null) return Result<SalesReturnDto>.Failure("مرتجع المبيعات غير موجود");
        if (sr.Status != InvoiceStatus.Draft) return Result<SalesReturnDto>.Failure("يمكن فقط ترحيل المرتجعات المسودة");


        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                sr.Post();
                await _uow.SaveChangesAsync(ct);

                // Update Stock
                foreach (var item in sr.Items)
                {
                    var retailQty = item.Product!.GetRetailQuantityEquivalent(item.Quantity, item.Mode);
                    await _inventoryService.IncreaseStockAsync(
                        item.ProductId,
                        sr.WarehouseId,
                        retailQty,
                        MovementType.SaleReturnIn,
                        "SalesReturn",
                        sr.Id,
                        item.UnitPrice,
                        userId,
                        ct);
                }

                // Update Customer Balance
                if (sr.TotalAmount > 0 && sr.CustomerId.HasValue)
                {
                    var customer = await _uow.Customers.GetByIdAsync(sr.CustomerId.Value, ct);
                    if (customer != null)
                    {
                        customer.DecreaseBalance(sr.TotalAmount);
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
        var sr = await _uow.SalesReturns.Query()
            .Include(r => r.Items)
                .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (sr == null) return Result<SalesReturnDto>.Failure("مرتجع المبيعات غير موجود");
        if (sr.Status == InvoiceStatus.Cancelled) return await GetByIdAsync(id, ct);

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
                        var retailQty = item.Product!.GetRetailQuantityEquivalent(item.Quantity, item.Mode);
                        await _inventoryService.DecreaseStockAsync(
                            item.ProductId,
                            sr.WarehouseId,
                            retailQty,
                            MovementType.SaleOut, // Opposite of SaleReturnIn
                            "SalesReturnCancel",
                            sr.Id,
                            item.UnitPrice,
                            userId,
                            ct);
                    }

                    // Reverse Customer Balance
                    if (sr.TotalAmount > 0 && sr.CustomerId.HasValue)
                    {
                        var customer = await _uow.Customers.GetByIdAsync(sr.CustomerId.Value, ct);
                        if (customer != null)
                        {
                            customer.IncreaseBalance(sr.TotalAmount);
                        }
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
            r.Customer?.Name ?? "غير معروف",
            r.SalesInvoiceId,
            r.ReturnDate,
            r.SubTotal,
            0, // TaxAmount (not in entity yet)
            0, // DiscountAmount (not in entity yet)
            r.TotalAmount,
            r.Notes,
            (byte)r.Status,
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


