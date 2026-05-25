using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class PurchaseReturnService : IPurchaseReturnService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<PurchaseReturnService> _logger;

    public PurchaseReturnService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        ILogger<PurchaseReturnService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<PurchaseReturnDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var pr = await _uow.PurchaseReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Supplier", "Warehouse", "Items.Product");

        if (pr == null)
            return Result<PurchaseReturnDto>.Failure("مرتجع المشتريات غير موجود", ErrorCodes.NotFound);

        return Result<PurchaseReturnDto>.Success(MapToDto(pr));
    }

    public async Task<Result<PagedResult<PurchaseReturnDto>>> GetAllAsync(int? supplierId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
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
            "Supplier", "Warehouse");

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<PurchaseReturnDto>>.Success(PagedResult<PurchaseReturnDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest request, int userId, CancellationToken ct)
    {
        // 1. Validation
        if (request.PurchaseInvoiceId.HasValue)
        {
            var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
                i => i.Id == request.PurchaseInvoiceId.Value, ct, "Items");

            if (invoice == null) return Result<PurchaseReturnDto>.Failure("الفاتورة الأصلية غير موجودة");

            foreach (var item in request.Items)
            {
                var originalLine = invoice.Items.FirstOrDefault(it => it.ProductId == item.ProductId);
                if (originalLine == null)
                    return Result<PurchaseReturnDto>.Failure($"المنتج {item.ProductId} غير موجود في الفاتورة الأصلية");

                if (item.Quantity > originalLine.Quantity)
                    return Result<PurchaseReturnDto>.Failure($"الكمية المرتجعة للمنتج {item.ProductId} أكبر من الكمية المشتراة ({originalLine.Quantity})");
            }
        }

        // 1b. Stock Validation BEFORE transaction
        var settings = await _uow.StoreSettings.FirstOrDefaultAsync(s => true, ct);
        bool allowNegativeStock = settings?.AllowNegativeStock ?? false;

        foreach (var item in request.Items)
        {
            // Load product for conversion factor
            var product = await _uow.Products.GetByIdAsync(item.ProductId, ct);
            if (product == null) return Result<PurchaseReturnDto>.Failure("المنتج غير موجود");
            
            var retailQty = product.GetRetailQuantityEquivalent(item.Quantity, (SaleMode)item.Mode);
            var stockValidation = await _inventoryService.ValidateStockAsync(item.ProductId, request.WarehouseId, retailQty, allowNegativeStock, ct);
            if (!stockValidation.IsSuccess)
                return Result<PurchaseReturnDto>.Failure(stockValidation.Error!);
        }

        // 2. Transaction
        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                var returnNoResult = await _sequenceService.GetNextNumberAsync("PR", ct);
                if (!returnNoResult.IsSuccess) return Result<PurchaseReturnDto>.Failure(returnNoResult.Error!);

                var purchaseReturn = PurchaseReturn.Create(
                    returnNoResult.Value!,
                    request.WarehouseId,
                    request.SupplierId,
                    request.PurchaseInvoiceId,
                    request.ReturnDate,
                    request.Notes,
                    userId
                );

                foreach (var item in request.Items)
                {
                    purchaseReturn.AddItem(item.ProductId, item.Quantity, item.UnitPrice, item.DiscountAmount, (SaleMode)item.Mode, item.Notes);
                }

                await _uow.PurchaseReturns.AddAsync(purchaseReturn, ct);
                await _uow.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                _logger.LogInformation("Purchase Return created as Draft: {ReturnNo} (ID: {Id})", purchaseReturn.ReturnNo, purchaseReturn.Id);

                return await GetByIdAsync(purchaseReturn.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain exception creating purchase return: {Message}", ex.Message);
                return Result<PurchaseReturnDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error creating purchase return");
                return Result<PurchaseReturnDto>.Failure("حدث خطأ أثناء حفظ المرتجع");
            }
        }, ct);
    }

    public async Task<Result<PurchaseReturnDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var pr = await _uow.PurchaseReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Items.Product");

        if (pr == null) return Result<PurchaseReturnDto>.Failure("مرتجع المشتريات غير موجود");
        if (pr.Status != InvoiceStatus.Draft) return Result<PurchaseReturnDto>.Failure("يمكن فقط ترحيل المرتجعات المسودة");

        var settings = await _uow.StoreSettings.FirstOrDefaultAsync(s => true, ct);
        bool allowNegativeStock = settings?.AllowNegativeStock ?? false;

        // Stock Validation BEFORE transaction
        foreach (var item in pr.Items)
        {
            var retailQty = item.Product!.GetRetailQuantityEquivalent(item.Quantity, item.Mode);
            var stockValidation = await _inventoryService.ValidateStockAsync(item.ProductId, pr.WarehouseId, retailQty, allowNegativeStock, ct);
            if (!stockValidation.IsSuccess)
                return Result<PurchaseReturnDto>.Failure(stockValidation.Error!);
        }

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                pr.Post();
                await _uow.SaveChangesAsync(ct);

                // Update Stock
                foreach (var item in pr.Items)
                {
                    var retailQty = item.Product!.GetRetailQuantityEquivalent(item.Quantity, item.Mode);
                    await _inventoryService.DecreaseStockAsync(
                        item.ProductId,
                        pr.WarehouseId,
                        retailQty,
                        MovementType.PurchaseReturnOut,
                        "PurchaseReturn",
                        pr.Id,
                        item.UnitCost,
                        userId,
                        ct);
                }

                // Update Supplier Balance
                if (pr.TotalAmount > 0)
                {
                    var supplier = await _uow.Suppliers.GetByIdAsync(pr.SupplierId, ct);
                    if (supplier != null)
                    {
                        supplier.DecreaseBalance(pr.TotalAmount);
                    }
                }

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Purchase Return posted: {ReturnNo} (ID: {Id})", pr.ReturnNo, pr.Id);
                return await GetByIdAsync(pr.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain exception posting purchase return {Id}: {Message}", id, ex.Message);
                return Result<PurchaseReturnDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error posting purchase return {Id}", id);
                return Result<PurchaseReturnDto>.Failure("حدث خطأ أثناء ترحيل المرتجع");
            }
        }, ct);
    }

    public async Task<Result<PurchaseReturnDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var pr = await _uow.PurchaseReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Items.Product");

        if (pr == null) return Result<PurchaseReturnDto>.Failure("مرتجع المشتريات غير موجود");
        if (pr.Status == InvoiceStatus.Cancelled)
            return Result<PurchaseReturnDto>.Failure("مرتجع المشتريات ملغى بالفعل", ErrorCodes.InvalidOperation);

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                if (pr.Status == InvoiceStatus.Posted)
                {
                    // Reverse Stock
                    foreach (var item in pr.Items)
                    {
                        var retailQty = item.Product!.GetRetailQuantityEquivalent(item.Quantity, item.Mode);
                        await _inventoryService.IncreaseStockAsync(
                            item.ProductId,
                            pr.WarehouseId,
                            retailQty,
                            MovementType.PurchaseIn, // Opposite of PurchaseReturnOut
                            "PurchaseReturnCancel",
                            pr.Id,
                            item.UnitCost,
                            userId,
                            ct);
                    }

                    // Reverse Supplier Balance
                    if (pr.TotalAmount > 0)
                    {
                        var supplier = await _uow.Suppliers.GetByIdAsync(pr.SupplierId, ct);
                        if (supplier != null)
                        {
                            supplier.IncreaseBalance(pr.TotalAmount);
                        }
                    }
                }

                pr.Cancel();
                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Purchase Return cancelled: {ReturnNo} (ID: {Id})", pr.ReturnNo, pr.Id);
                return await GetByIdAsync(pr.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain exception cancelling purchase return {Id}: {Message}", id, ex.Message);
                return Result<PurchaseReturnDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error cancelling purchase return {Id}", id);
                return Result<PurchaseReturnDto>.Failure("حدث خطأ أثناء إلغاء المرتجع");
            }
        }, ct);
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
            r.ReturnDate,
            r.SubTotal,
            0, // TaxAmount
            0, // DiscountAmount
            r.TotalAmount,
            r.Notes,
            (byte)r.Status,
            r.Items.Select(it => new PurchaseReturnItemDto(
                it.Id,
                it.ProductId,
                it.Product?.Name ?? "غير معروف",
                it.Quantity,
                it.UnitCost,
                it.DiscountAmount,
                it.LineTotal,
                (byte)it.Mode
            )).ToList()
        );
    }
}


