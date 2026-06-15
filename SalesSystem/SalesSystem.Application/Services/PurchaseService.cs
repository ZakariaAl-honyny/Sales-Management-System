using Microsoft.Extensions.Logging;
using SalesSystem.Application.Helpers;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

/// <summary>
/// خدمة فواتير الشراء — تدعم العملات المتعددة.
/// </summary>
public class PurchaseService : IPurchaseService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly IStoreSettingsService _settingsService;
    private readonly IUpdateProductPricingService _pricingService;
    private readonly ICashBoxService _cashBoxService;
    private readonly IAccountingIntegrationService _accountingService;
    private readonly IDocumentSequenceService _documentSequenceService;
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IStoreSettingsService settingsService,
        IUpdateProductPricingService pricingService,
        ICashBoxService cashBoxService,
        IAccountingIntegrationService accountingService,
        IDocumentSequenceService documentSequenceService,
        ILogger<PurchaseService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _settingsService = settingsService;
        _pricingService = pricingService;
        _cashBoxService = cashBoxService;
        _accountingService = accountingService;
        _documentSequenceService = documentSequenceService;
        _logger = logger;
    }

    public async Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct,
            "Supplier.Party", "Warehouse", "Items.Product", "Items.ProductUnit",
            "Currency", "Tax");

        if (invoice == null)
            return Result<PurchaseInvoiceDto>.Failure("فاتورة المشتريات غير موجودة", ErrorCodes.NotFound);

        return Result<PurchaseInvoiceDto>.Success(MapToDto(invoice));
    }

    public async Task<Result<PagedResult<PurchaseInvoiceDto>>> GetAllAsync(
        int? supplierId, 
        int? status, 
        string? search = null, 
        DateTime? from = null, 
        DateTime? to = null, 
        int page = 1, 
        int pageSize = 10, 
        bool includeInactive = false, 
        CancellationToken ct = default)
    {
        var searchLower = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();
        int? searchId = int.TryParse(searchLower, out var parsedId) ? parsedId : null;

        System.Linq.Expressions.Expression<System.Func<PurchaseInvoice, bool>> predicate = i =>
            (!supplierId.HasValue || i.SupplierId == supplierId.Value) &&
            (!status.HasValue || (int)i.Status == status.Value) &&
            (status.HasValue || includeInactive || i.Status != InvoiceStatus.Cancelled) &&
            (!from.HasValue || i.InvoiceDate >= from.Value) &&
            (!to.HasValue || i.InvoiceDate <= to.Value) &&
            (searchLower == null ||
             (searchId.HasValue && i.Id == searchId.Value) ||
             (i.Notes != null && i.Notes.ToLower().Contains(searchLower)) ||
             i.Items.Any(item =>
                 item.Product != null &&
                 item.Product.Name.ToLower().Contains(searchLower)));

        var includes = new[] { "Supplier.Party", "Warehouse", "Items.Product", "Items.ProductUnit" };

        var (items, total) = await _uow.PurchaseInvoices.GetPagedAsync(
            predicate, q => q.OrderByDescending(i => i.InvoiceDate), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<PurchaseInvoiceDto>>.Success(PagedResult<PurchaseInvoiceDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest request, int userId, CancellationToken ct)
    {
        return await _uow.ExecuteTransactionAsync<Result<PurchaseInvoiceDto>>(async () =>
        {
            try
            {
                // Resolve InvoiceNo inside transaction
                int invoiceNo;
                if (request.InvoiceNo.HasValue && request.InvoiceNo.Value > 0)
                {
                    var existing = await _uow.PurchaseInvoices.AnyAsync(i => i.InvoiceNo == request.InvoiceNo.Value, ct);
                    if (existing)
                    {
                        return Result<PurchaseInvoiceDto>.Failure("رقم الفاتورة موجود بالفعل");
                    }
                    invoiceNo = request.InvoiceNo.Value;
                }
                else
                {
                    var seqResult = await _documentSequenceService.GetNextIntAsync("PurchaseInvoice", ct);
                    if (!seqResult.IsSuccess)
                    {
                        return Result<PurchaseInvoiceDto>.Failure("فشل في توليد رقم الفاتورة");
                    }
                    invoiceNo = seqResult.Value;
                }

                var invoice = PurchaseInvoice.Create(
                    request.SupplierId,
                    (short)request.WarehouseId,
                    invoiceNo,
                    request.InvoiceDate,
                    (Domain.Enums.PaymentType)request.PaymentType,
                    request.DiscountAmount,
                    request.OtherCharges,
                    request.Notes,
                    taxId: null,
                    currencyId: (short)(request.CurrencyId ?? 1),
                    exchangeRate: request.ExchangeRate,
                    createdByUserId: userId
                );

                foreach (var item in request.Items)
                {
                    var invoiceItem = PurchaseInvoiceLine.Create(
                        item.ProductId,
                        item.ProductUnitId,
                        item.Quantity,
                        item.UnitPrice
                    );
                    invoice.AddItem(invoiceItem);
                }

                invoice.SetTaxAmount(request.TaxAmount);
                invoice.SetPaidAmount(request.PaidAmount);

                await _uow.PurchaseInvoices.AddAsync(invoice, ct);
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("تم إنشاء فاتورة شراء كمسودة: المعرف {Id} بواسطة المستخدم {UserId}", invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "خطأ في المجال أثناء إنشاء فاتورة الشراء: {Message}", ex.Message);
                return Result<PurchaseInvoiceDto>.Failure(ex.Message);
            }
        }, ct);
    }

    public async Task<Result<PurchaseInvoiceDto>> UpdateAsync(int id, UpdatePurchaseInvoiceRequest request, int userId, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Items");

        if (invoice == null)
            return Result<PurchaseInvoiceDto>.Failure("فاتورة المشتريات غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
            return Result<PurchaseInvoiceDto>.Failure("يمكن تعديل الفواتير المسودة فقط");

        try
        {
            if (request.CurrencyId.HasValue)
                invoice.SetCurrency(request.CurrencyId ?? 1, request.ExchangeRate);

            invoice.UpdateTotals(request.DiscountAmount, request.TaxAmount, request.OtherCharges);
            invoice.SetPaidAmount(request.PaidAmount);

            // Re-create items (simplest way for draft)
            _uow.PurchaseInvoiceLines.DeleteRange(invoice.Items);
            invoice.Items.Clear();
            foreach (var item in request.Items)
            {
                var invoiceItem = PurchaseInvoiceLine.Create(
                    item.ProductId,
                    item.ProductUnitId,
                    item.Quantity,
                    item.UnitPrice
                );
                invoice.AddItem(invoiceItem);
            }

            await _uow.PurchaseInvoices.UpdateAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            return await GetByIdAsync(invoice.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "خطأ في المجال أثناء تحديث فاتورة الشراء {Id}: {Message}", id, ex.Message);
            return Result<PurchaseInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء تحديث فاتورة الشراء {Id}", id);
            return Result<PurchaseInvoiceDto>.Failure("حدث خطأ أثناء تحديث الفاتورة");
        }
    }

    public async Task<Result<PurchaseInvoiceDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Items.Product", "Items.ProductUnit");

        if (invoice == null)
            return Result<PurchaseInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            _logger.LogWarning("لا يمكن ترحيل فاتورة الشراء {Id} لأن حالتها {Status}", invoice.Id, invoice.Status);
            return Result<PurchaseInvoiceDto>.Failure("يمكن فقط ترحيل الفواتير المسودة");
        }

        return await _uow.ExecuteTransactionAsync<Result<PurchaseInvoiceDto>>(async () =>
        {
            try
            {
                invoice.Post();
                await _uow.SaveChangesAsync();

                // ─── Compute Landed Costs ─────────────────────────────────────
                // توزيع المصاريف الإضافية (نقل، جمارك، إلخ) بشكل تناسبي على البنود
                var itemsList = invoice.Items.ToList();
                var allocationLines = itemsList.Select((item, index) => new AllocationLine
                {
                    Index = index,
                    LineTotal = item.LineTotal,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitPrice
                }).ToArray();

                var landedCosts = AdditionalChargeAllocator.Allocate(
                    allocationLines,
                    invoice.OtherCharges,
                    invoice.SubTotal);

                // ─── Update Stock ────────────────────────────────────────────────
                for (int i = 0; i < itemsList.Count; i++)
                {
                    var item = itemsList[i];
                    var landedUnitCost = landedCosts.GetValueOrDefault(i, item.UnitPrice);
                    var stockResult = await _inventoryService.IncreaseStockAsync(
                        item.ProductId,
                        invoice.WarehouseId,
                        item.Quantity,
                        unitCost: landedUnitCost,
                        userId: userId,
                        ct: ct);

                    if (!stockResult.IsSuccess)
                    {
                        _logger.LogWarning("فشل زيادة المخزون لفاتورة الشراء: {Error}", stockResult.Error);
                        return Result<PurchaseInvoiceDto>.Failure(stockResult.Error!);
                    }
                }

                // ─── Auto-update product costs via pricing service ───────────────
                for (int i = 0; i < itemsList.Count; i++)
                {
                    var item = itemsList[i];
                    if (item.Product == null) continue;

                    try
                    {
                        var landedUnitCost = landedCosts.GetValueOrDefault(i, item.UnitPrice);
                        var baseUnit = item.Product.GetBaseUnit();
                        var result = await _pricingService.UpdateFromPurchaseAsync(
                            new UpdatePricingRequest(
                                ProductUnitId: baseUnit.Id,
                                NewPurchaseCost: landedUnitCost,
                                NewQuantityPurchased: item.Quantity,
                                NewSalesPrice: null,
                                InvoiceId: invoice.Id,
                                ChangedBy: userId
                            ), ct);

                        if (!result.IsSuccess)
                        {
                            _logger.LogWarning("فشل تحديث تكلفة المنتج {ProductId} من الفاتورة {Id}: {Error}",
                                item.ProductId, invoice.Id, result.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "فشل تحديث التكلفة للمنتج {ProductId} من الفاتورة {Id}",
                            item.ProductId, invoice.Id);
                    }
                }

                // ─── Record cash transaction for supplier payment ───────────────
                if (invoice.PaidAmount > 0)
                {
                    // Cash transaction recorded via accounting integration
                }

                await _uow.SaveChangesAsync(ct);

                // ─── Create journal entry for purchase posting ─────────────────
                var entryResult = await _accountingService.CreatePurchasePostEntryAsync(invoice, userId, ct);
                if (!entryResult.IsSuccess)
                {
                    return Result<PurchaseInvoiceDto>.Failure(entryResult.Error!);
                }

                _logger.LogInformation("تم ترحيل فاتورة الشراء: المعرف {Id} بواسطة المستخدم {UserId}", invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "خطأ في المجال أثناء ترحيل فاتورة الشراء {Id}: {Message}", id, ex.Message);
                return Result<PurchaseInvoiceDto>.Failure(ex.Message);
            }
        }, ct);
    }

    public async Task<Result<PurchaseInvoiceDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Items.Product");

        if (invoice == null)
            return Result<PurchaseInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status == InvoiceStatus.Cancelled)
            return Result<PurchaseInvoiceDto>.Failure("الفاتورة ملغاة بالفعل", ErrorCodes.InvalidOperation);

        return await _uow.ExecuteTransactionAsync<Result<PurchaseInvoiceDto>>(async () =>
        {
            try
            {
                if (invoice.Status == InvoiceStatus.Posted)
                {
                    // Create reversal journal entry
                    var entryResult = await _accountingService.ReversePurchasePostEntryAsync(invoice, userId, ct);
                    if (!entryResult.IsSuccess)
                    {
                        return Result<PurchaseInvoiceDto>.Failure(entryResult.Error!);
                    }

                    // Reverse Stock
                    foreach (var item in invoice.Items)
                    {
                        var stockResult = await _inventoryService.DecreaseStockAsync(
                            item.ProductId,
                            invoice.WarehouseId,
                            item.Quantity,
                            unitCost: item.UnitPrice,
                            userId: userId,
                            ct: ct);

                        if (!stockResult.IsSuccess)
                        {
                            _logger.LogWarning("فشل عكس المخزون لإلغاء فاتورة الشراء: {Error}", stockResult.Error);
                            return Result<PurchaseInvoiceDto>.Failure(stockResult.Error!);
                        }
                    }

                    // Reverse cash transaction if applicable
                    if (invoice.PaidAmount > 0)
                    {
                        _logger.LogInformation("سيتم تسوية المبلغ المدفوع {PaidAmount} للفاتورة الملغاة {Id}", invoice.PaidAmount, invoice.Id);
                    }
                }

                // Zero out PaidAmount before Cancel()
                if (invoice.PaidAmount > 0)
                    invoice.SetPaidAmount(0);

                invoice.Cancel();
                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation("تم إلغاء فاتورة الشراء: المعرف {Id} بواسطة المستخدم {UserId}", invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                _logger.LogWarning(ex, "خطأ في المجال أثناء إلغاء فاتورة الشراء {Id}: {Message}", id, ex.Message);
                return Result<PurchaseInvoiceDto>.Failure(ex.Message);
            }
        }, ct);
    }

    private PurchaseInvoiceDto MapToDto(PurchaseInvoice i)
    {
        return new PurchaseInvoiceDto(
            i.Id,
            i.InvoiceNo,
            i.SupplierId,
            i.Supplier?.Party?.Name ?? "غير معروف",
            i.WarehouseId,
            i.Warehouse?.Name ?? "غير معروف",
            i.InvoiceDate,
            (byte)i.PaymentType,
            i.SubTotal,
            i.DiscountAmount,
            i.TaxAmount,
            i.OtherCharges,
            i.NetTotal,
            i.PaidAmount,
            i.RemainingAmount,
            i.Notes,
            (byte)i.Status,
            i.TaxId,
            i.Tax?.Name,
            (decimal?)i.Tax?.Rate,
            i.CurrencyId,
            i.ExchangeRate,
            i.Items.Select(it => new PurchaseInvoiceLineDto(
                it.Id,
                it.ProductId,
                it.Product?.Name ?? "غير معروف",
                it.ProductUnitId,
                it.ProductUnit?.Unit?.Name ?? "غير معروف",
                it.Quantity,
                it.UnitPrice,
                it.LineTotal
            )).ToList()
        );
    }
}
