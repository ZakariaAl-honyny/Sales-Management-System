using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;
using System.Text;

namespace SalesSystem.Application.Services;

/// <summary>
/// خدمة فواتير الشراء — تدعم العملات المتعددة والخصم المتنوع والمرفقات والمصاريف الإضافية.
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
    private readonly IFeeDistributionService _feeDistributionService;
    private readonly ILogger<PurchaseService> _logger;

    private static readonly string AttachmentDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "SalesSystem", "PurchaseAttachments");

    public PurchaseService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IStoreSettingsService settingsService,
        IUpdateProductPricingService pricingService,
        ICashBoxService cashBoxService,
        IAccountingIntegrationService accountingService,
        IDocumentSequenceService documentSequenceService,
        IFeeDistributionService feeDistributionService,
        ILogger<PurchaseService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _settingsService = settingsService;
        _pricingService = pricingService;
        _cashBoxService = cashBoxService;
        _accountingService = accountingService;
        _documentSequenceService = documentSequenceService;
        _feeDistributionService = feeDistributionService;
        _logger = logger;
    }

    public async Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct,
            "Supplier", "Warehouse", "Items.Product", "Items.ProductUnit",
            "AdditionalFees", "Currency", "Tax");

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
             (i.Supplier != null && i.Supplier.Name.ToLower().Contains(searchLower)) ||
             (i.SupplierInvoiceNo != null && i.SupplierInvoiceNo.ToLower().Contains(searchLower)) ||
             (i.Notes != null && i.Notes.ToLower().Contains(searchLower)) ||
             i.Items.Any(item =>
                 item.Product != null &&
                 item.Product.Name.ToLower().Contains(searchLower)));

        var includes = new[] { "Supplier", "Warehouse", "Items.Product", "Items.ProductUnit" };

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
                    request.WarehouseId,
                    invoiceNo,
                    request.InvoiceDate,
                    request.DueDate,
                    (Domain.Enums.PaymentType)request.PaymentType,
                    request.DiscountAmount,
                    request.SupplierInvoiceNo,
                    request.Notes,
                    cashBoxId: request.CashBoxId,
                    taxId: null,
                    currencyId: request.CurrencyId,
                    exchangeRate: request.ExchangeRate,
                    createdByUserId: userId
                );

                // Set discount type/rate if provided
                if (request.DiscountType.HasValue)
                {
                    invoice.SetDiscount(
                        request.DiscountAmount,
                        (Domain.Enums.DiscountType)request.DiscountType.Value,
                        request.DiscountRate);
                }

                foreach (var item in request.Items)
                {
                    var invoiceItem = PurchaseInvoiceItem.Create(
                        item.ProductId,
                        item.ProductUnitId,
                        item.Quantity,
                        item.UnitCost,
                        item.DiscountAmount,
                        (Domain.Enums.DiscountType)(item.DiscountType ?? 0),
                        item.DiscountRate,
                        (SaleMode)item.Mode,
                        item.Notes
                    );
                    invoice.AddItem(invoiceItem);
                }

                invoice.SetTaxAmount(request.TaxAmount);
                invoice.SetPaidAmount(request.PaidAmount);

                // Handle attachment if provided
                if (!string.IsNullOrEmpty(request.AttachmentBase64))
                {
                    var attachmentPath = await SaveAttachmentAsync(invoice.Id, request.AttachmentBase64, request.AttachmentFileName, ct);
                    if (attachmentPath != null)
                        invoice.SetAttachment(attachmentPath);
                }

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
            // Update currency and discount fields
            if (request.CurrencyId.HasValue)
                invoice.SetCurrency(request.CurrencyId, request.ExchangeRate);
            if (request.DiscountType.HasValue)
                invoice.SetDiscount(request.DiscountAmount, (Domain.Enums.DiscountType)request.DiscountType.Value, request.DiscountRate);
            else
                invoice.UpdateTotals(request.DiscountAmount, request.TaxAmount);

            invoice.SetPaidAmount(request.PaidAmount);

            // Handle attachment update
            if (!string.IsNullOrEmpty(request.AttachmentBase64))
            {
                var attachmentPath = await SaveAttachmentAsync(id, request.AttachmentBase64, request.AttachmentFileName, ct);
                if (attachmentPath != null)
                    invoice.SetAttachment(attachmentPath);
            }
            
            // Re-create items (simplest way for draft)
            _uow.PurchaseInvoiceItems.DeleteRange(invoice.Items);
            invoice.Items.Clear();
            foreach (var item in request.Items)
            {
                var invoiceItem = PurchaseInvoiceItem.Create(
                    item.ProductId,
                    item.ProductUnitId,
                    item.Quantity,
                    item.UnitCost,
                    item.DiscountAmount,
                    (Domain.Enums.DiscountType)(item.DiscountType ?? 0),
                    item.DiscountRate,
                    (SaleMode)item.Mode,
                    item.Notes
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
            i => i.Id == id, ct, "Items.Product", "Items.ProductUnit", "AdditionalFees");

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

                // ─── Distribute Additional Fees ──────────────────────────────────
                if (invoice.AdditionalFees.Any())
                {
                    var itemList = invoice.Items.ToList();
                    var allAllocations = new List<AdditionalFeeAllocation>();

                    foreach (var fee in invoice.AdditionalFees.Where(f => f.IsActive))
                    {
                        var distResult = await _feeDistributionService.DistributeFeeAsync(fee, itemList, ct);
                        if (!distResult.IsSuccess)
                            return Result<PurchaseInvoiceDto>.Failure(distResult.Error!);
                        allAllocations.AddRange(distResult.Value!);
                    }

                    // Save allocations and update invoice AdditionalFeesTotal
                    foreach (var allocation in allAllocations)
                    {
                        // Fix PurchaseInvoiceItemId after items are persisted
                        var targetItem = invoice.Items.FirstOrDefault(i =>
                            i.Id == allocation.PurchaseInvoiceItemId ||
                            (i.ProductId == itemList.ElementAtOrDefault(allocation.PurchaseInvoiceItemId > 0 ? 0 : 0)?.ProductId));
                        if (targetItem != null && allocation.PurchaseInvoiceItemId == 0)
                        {
                            // allocation will be fixed by EF Core navigation fixup
                        }
                        await _uow.AdditionalFeeAllocations.AddAsync(allocation, ct);
                    }

                    var totalResult = await _feeDistributionService.CalculateTotalAllocatedFeesAsync(itemList);
                    var totalAllocated = totalResult.IsSuccess ? totalResult.Value : 0;
                    invoice.SetAdditionalFeesTotal(totalAllocated);
                    await _uow.SaveChangesAsync(ct);
                }

                // ─── Update Stock ────────────────────────────────────────────────
                foreach (var item in invoice.Items)
                {
                    var retailQty = item.Product!.GetRetailQuantityEquivalent(item.Quantity, item.Mode);
                    var stockResult = await _inventoryService.IncreaseStockAsync(
                        item.ProductId,
                        invoice.WarehouseId,
                        retailQty,
                        MovementType.PurchaseIn,
                        "PurchaseInvoice",
                        invoice.Id,
                        item.UnitCost,
                        userId,
                        ct);

                    if (!stockResult.IsSuccess)
                    {
                        _logger.LogWarning("فشل زيادة المخزون لفاتورة الشراء: {Error}", stockResult.Error);
                        return Result<PurchaseInvoiceDto>.Failure(stockResult.Error!);
                    }
                }

                // ─── Auto-update product costs via pricing service ───────────────
                foreach (var item in invoice.Items)
                {
                    if (item.Product == null) continue;

                    try
                    {
                        var baseUnit = item.Product.GetBaseUnit();
                        var retailQty = item.Product.GetRetailQuantityEquivalent(item.Quantity, item.Mode);
                        var retailUnitCost = item.Product.GetRetailQuantityEquivalent(1, item.Mode) > 0
                            ? item.UnitCost / item.Product.GetRetailQuantityEquivalent(1, item.Mode)
                            : item.UnitCost;

                        var result = await _pricingService.UpdateFromPurchaseAsync(
                            new UpdatePricingRequest(
                                ProductUnitId: baseUnit.Id,
                                NewPurchaseCost: retailUnitCost,
                                NewQuantityPurchased: retailQty,
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

                // ─── Update Supplier Balance ────────────────────────────────────
                if (invoice.DueAmount > 0)
                {
                    var supplier = await _uow.Suppliers.GetByIdAsync(invoice.SupplierId, ct);
                    if (supplier == null)
                    {
                        _logger.LogWarning("المورد {SupplierId} غير موجود أثناء ترحيل الفاتورة {Id}", invoice.SupplierId, invoice.Id);
                        return Result<PurchaseInvoiceDto>.Failure("المورد غير موجود");
                    }
                    supplier.IncreaseBalance(invoice.DueAmount);
                }

                // ─── Record cash transaction for supplier payment ───────────────
                if (invoice.CashBoxId.HasValue && invoice.PaidAmount > 0)
                {
                    var cashResult = await _cashBoxService.RecordInvoicePaymentAsync(
                        invoice.CashBoxId.Value,
                        invoice.PaidAmount,
                        CashTransactionType.SupplierPayment,
                        "PurchaseInvoice",
                        invoice.Id,
                        userId,
                        ct);

                    if (!cashResult.IsSuccess)
                    {
                        _logger.LogWarning("فشل تسجيل الحركة النقدية لفاتورة الشراء {Id}: {Error}",
                            invoice.Id, cashResult.Error);
                    }
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
                        var retailQty = item.Product!.GetRetailQuantityEquivalent(item.Quantity, item.Mode);
                        var stockResult = await _inventoryService.DecreaseStockAsync(
                            item.ProductId,
                            invoice.WarehouseId,
                            retailQty,
                            MovementType.PurchaseReturnOut,
                            "PurchaseInvoiceCancel",
                            invoice.Id,
                            item.UnitCost,
                            userId,
                            ct);

                        if (!stockResult.IsSuccess)
                        {
                            _logger.LogWarning("فشل عكس المخزون لإلغاء فاتورة الشراء: {Error}", stockResult.Error);
                            return Result<PurchaseInvoiceDto>.Failure(stockResult.Error!);
                        }
                    }

                    // Reverse Supplier Balance
                    if (invoice.DueAmount > 0)
                    {
                        var supplier = await _uow.Suppliers.GetByIdAsync(invoice.SupplierId, ct);
                        if (supplier != null)
                        {
                            supplier.DecreaseBalance(invoice.DueAmount);
                        }
                    }

                    // Reverse cash transaction
                    if (invoice.CashBoxId.HasValue && invoice.PaidAmount > 0)
                    {
                        var cashResult = await _cashBoxService.RecordInvoicePaymentAsync(
                            invoice.CashBoxId.Value,
                            invoice.PaidAmount,
                            CashTransactionType.RefundOut,
                            "PurchaseInvoiceCancel",
                            invoice.Id,
                            userId,
                            ct);

                        if (!cashResult.IsSuccess)
                        {
                            _logger.LogWarning("فشل عكس الحركة النقدية لإلغاء فاتورة الشراء: {Error}", cashResult.Error);
                            return Result<PurchaseInvoiceDto>.Failure(cashResult.Error ?? "فشل في تسجيل الحركة النقدية العكسية");
                        }
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

    public async Task<Result<string>> UploadAttachmentAsync(int id, string base64Content, string? fileName, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.GetByIdAsync(id, ct);
        if (invoice == null)
            return Result<string>.Failure("فاتورة الشراء غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
            return Result<string>.Failure("يمكن إرفاق ملفات فقط للفواتير المسودة");

        try
        {
            var path = await SaveAttachmentAsync(id, base64Content, fileName, ct);
            if (path == null)
                return Result<string>.Failure("فشل في حفظ المرفق");

            invoice.SetAttachment(path);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم رفع المرفق للفاتورة {InvoiceId}: {Path}", id, path);

            return Result<string>.Success(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء رفع المرفق للفاتورة {InvoiceId}", id);
            return Result<string>.Failure("حدث خطأ أثناء رفع المرفق");
        }
    }

    public async Task<Result> DeleteAttachmentAsync(int id, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.GetByIdAsync(id, ct);
        if (invoice == null)
            return Result.Failure("فاتورة الشراء غير موجودة", ErrorCodes.NotFound);

        if (string.IsNullOrEmpty(invoice.AttachmentPath))
            return Result.Failure("لا يوجد مرفق محذوف");

        try
        {
            // Delete file from disk
            if (File.Exists(invoice.AttachmentPath))
                File.Delete(invoice.AttachmentPath);

            invoice.SetAttachment(null);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("تم حذف المرفق للفاتورة {InvoiceId}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطأ أثناء حذف المرفق للفاتورة {InvoiceId}", id);
            return Result.Failure("حدث خطأ أثناء حذف المرفق");
        }
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    private async Task<string?> SaveAttachmentAsync(int invoiceId, string base64Content, string? fileName, CancellationToken ct)
    {
        try
        {
            var dir = Path.Combine(AttachmentDir, invoiceId.ToString());
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var ext = string.IsNullOrEmpty(fileName) ? ".pdf" : Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext)) ext = ".pdf";

            var safeFileName = $"attachment_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var filePath = Path.Combine(dir, safeFileName);

            var bytes = Convert.FromBase64String(base64Content);
            await File.WriteAllBytesAsync(filePath, bytes, ct);

            return filePath;
        }
        catch (FormatException)
        {
            _logger.LogWarning("محتوى Base64 غير صالح للمرفق في الفاتورة {InvoiceId}", invoiceId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل حفظ المرفق للفاتورة {InvoiceId}", invoiceId);
            return null;
        }
    }

    private PurchaseInvoiceDto MapToDto(PurchaseInvoice i)
    {
        // Build additional fees DTOs
        var additionalFees = i.AdditionalFees?
            .Where(f => f.IsActive)
            .Select(f => new AdditionalFeeDto(
                f.Id,
                f.FeeName,
                f.FeeAmount,
                (byte)f.DistributionMethod,
                f.AccountId,
                null
            )).ToList() ?? new List<AdditionalFeeDto>();

        return new PurchaseInvoiceDto(
            i.Id,
            i.InvoiceNo,
            i.SupplierId,
            i.Supplier?.Name ?? "غير معروف",
            i.WarehouseId,
            i.Warehouse?.Name ?? "غير معروف",
            i.InvoiceDate,
            i.DueDate,
            (byte)i.PaymentType,
            i.SubTotal,
            i.DiscountAmount,
            i.TaxAmount,
            i.TotalAmount,
            i.PaidAmount,
            i.DueAmount,
            i.SupplierInvoiceNo,
            i.Notes,
            (byte)i.Status,
            i.TaxId,
            i.Tax?.Name,
            (decimal?)i.Tax?.Rate,
            i.CurrencyId,
            i.ExchangeRate,
            i.CostInBaseCurrency,
            i.AdditionalFeesTotal,
            i.AttachmentPath,
            (byte?)i.DiscountType,
            i.DiscountRate,
            additionalFees,
            i.Items.Select(it => new PurchaseInvoiceItemDto(
                it.Id,
                it.ProductId,
                it.Product?.Name ?? "غير معروف",
                it.ProductUnitId,
                it.ProductUnit?.UnitName ?? "غير معروف",
                it.Quantity,
                it.UnitCost,
                it.DiscountAmount,
                it.LineTotal,
                (byte?)it.DiscountType,
                it.DiscountRate,
                it.CostInBaseCurrency,
                it.AdditionalFeesAmount,
                (byte)it.Mode
            )).ToList()
        );
    }
}
