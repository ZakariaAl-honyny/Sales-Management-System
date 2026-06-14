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
    private readonly ILogger<PurchaseReturnService> _logger;

    public PurchaseReturnService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        ISystemSettingsRepository systemSettingsRepo,
        ILogger<PurchaseReturnService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _systemSettingsRepo = systemSettingsRepo;
        _logger = logger;
    }

    public async Task<Result<PurchaseReturnDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var pr = await _uow.PurchaseReturns.FirstOrDefaultAsync(
            r => r.Id == id, ct, "Supplier.Party", "Warehouse", "Items.Product", "Items.ProductUnit", "Currency");

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
            "Supplier.Party", "Warehouse", "Items.Product", "Items.ProductUnit");

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
                var originalLine = invoice.Items.FirstOrDefault(it => it.ProductId == item.ProductId);
                if (originalLine == null)
                    return Result<PurchaseReturnDto>.Failure($"المنتج {item.ProductId} غير موجود في الفاتورة الأصلية");

                if (item.Quantity > originalLine.Quantity)
                    return Result<PurchaseReturnDto>.Failure(
                        $"الكمية المرتجعة للمنتج {item.ProductId} أكبر من الكمية المشتراة ({originalLine.Quantity})");
            }
        }

        // ─── Stock validation before transaction ─────────────────────────────
        var allowNegativeStock = await _systemSettingsRepo.GetBoolAsync("AllowNegativeStock", false, ct);

        foreach (var item in request.Items)
        {
            var product = await _uow.Products.GetByIdAsync(item.ProductId, ct);
            if (product == null) return Result<PurchaseReturnDto>.Failure("المنتج غير موجود");

            var stockValidation = await _inventoryService.ValidateStockAsync(
                item.ProductId, request.WarehouseId, item.Quantity, allowNegativeStock, ct);
            if (!stockValidation.IsSuccess)
                return Result<PurchaseReturnDto>.Failure(stockValidation.Error!);
        }

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
                    (short)request.WarehouseId,
                    request.SupplierId,
                    request.PurchaseInvoiceId,
                    request.ReturnDate,
                    request.Notes,
                    request.CurrencyId is not null ? (short?)request.CurrencyId.Value : null,
                    request.ExchangeRate,
                    userId);

                foreach (var item in request.Items)
                {
                    purchaseReturn.AddItem(
                        item.ProductId,
                        item.ProductUnitId,
                        item.Quantity,
                        item.UnitCost);
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
            r => r.Id == id, ct, "Items.Product");

        if (pr == null)
            return Result<PurchaseReturnDto>.Failure("مرتجع المشتريات غير موجود");
        if (pr.Status != InvoiceStatus.Draft)
            return Result<PurchaseReturnDto>.Failure("يمكن فقط ترحيل المرتجعات المسودة");

        var allowNegativeStock = await _systemSettingsRepo.GetBoolAsync("AllowNegativeStock", false, ct);

        foreach (var item in pr.Items)
        {
            var stockValidation = await _inventoryService.ValidateStockAsync(
                item.ProductId, pr.WarehouseId, item.Quantity, allowNegativeStock, ct);
            if (!stockValidation.IsSuccess)
                return Result<PurchaseReturnDto>.Failure(stockValidation.Error!);
        }

        return await _uow.ExecuteTransactionAsync<Result<PurchaseReturnDto>>(async () =>
        {
            try
            {
                pr.Post();
                await _uow.SaveChangesAsync(ct);

                // Update Stock — decrease from warehouse
                foreach (var item in pr.Items)
                {
                    await _inventoryService.DecreaseStockAsync(
                        item.ProductId,
                        pr.WarehouseId,
                        item.Quantity,
                        unitCost: item.UnitCost,
                        userId: userId,
                        ct: ct);
                }

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("تم ترحيل مرتجع المشتريات: {ReturnNo} (المعرف {Id})", pr.ReturnNo, pr.Id);
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
            r => r.Id == id, ct, "Items.Product");

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
                    foreach (var item in pr.Items)
                    {
                        await _inventoryService.IncreaseStockAsync(
                            item.ProductId,
                            pr.WarehouseId,
                            item.Quantity,
                            unitCost: item.UnitCost,
                            userId: userId,
                            ct: ct);
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

    private static PurchaseReturnDto MapToDto(PurchaseReturn r)
    {
        return new PurchaseReturnDto(
            r.Id,
            r.ReturnNo,
            r.WarehouseId,
            r.Warehouse?.Name ?? "غير معروف",
            r.SupplierId,
            r.Supplier?.Party?.Name ?? "غير معروف",
            r.PurchaseInvoiceId,
            r.ReturnDate,
            r.SubTotal,
            r.TotalAmount,
            r.CurrencyId,
            r.ExchangeRate,
            r.Notes,
            (byte)r.Status,
            r.Items.Select(it => new PurchaseReturnItemDto(
                it.Id,
                it.ProductId,
                it.Product?.Name ?? "غير معروف",
                it.ProductUnitId,
                it.ProductUnit?.Unit?.Name,
                it.Quantity,
                it.UnitCost,
                it.LineTotal
            )).ToList()
        );
    }
}
