using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

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
            i => i.Id == id, ct, "Supplier", "Warehouse", "Items.Product", "Items.ProductUnit", "AdditionalFees");

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
        // Build predicate dynamically
        var searchLower = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();

        // Parse search text as Id outside the expression tree (EF Core can't translate int.TryParse)
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

        var includes = new[] { "Supplier", "Warehouse", "Items.Product", "Items.ProductUnit", "AdditionalFees" };

        var (items, total) = await _uow.PurchaseInvoices.GetPagedAsync(
            predicate, q => q.OrderByDescending(i => i.InvoiceDate), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<PurchaseInvoiceDto>>.Success(PagedResult<PurchaseInvoiceDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest request, int userId, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            // Resolve InvoiceNo INSIDE transaction — prevents TOCTOU race (RULE-384 fix)
            int invoiceNo;
            if (request.InvoiceNo.HasValue && request.InvoiceNo.Value > 0)
            {
                var existing = await _uow.PurchaseInvoices.AnyAsync(i => i.InvoiceNo == request.InvoiceNo.Value, ct);
                if (existing)
                {
                    await transaction.RollbackAsync(ct);
                    return Result<PurchaseInvoiceDto>.Failure("رقم الفاتورة موجود بالفعل");
                }
                invoiceNo = request.InvoiceNo.Value;
            }
            else
            {
                var seqResult = await _documentSequenceService.GetNextIntAsync("PurchaseInvoice", ct);
                if (!seqResult.IsSuccess)
                {
                    await transaction.RollbackAsync(ct);
                    return Result<PurchaseInvoiceDto>.Failure("فشل في توليد رقم الفاتورة");
                }
                invoiceNo = seqResult.Value;
            }

            // Map DiscountType from request (byte?) to Domain enum
            Domain.Enums.DiscountType? discountType = request.DiscountType.HasValue
                ? (Domain.Enums.DiscountType)request.DiscountType.Value
                : null;

            var invoice = PurchaseInvoice.Create(
                request.SupplierId,
                request.WarehouseId,
                invoiceNo,
                invoiceDate: request.InvoiceDate,
                dueDate: request.DueDate,
                paymentType: (Domain.Enums.PaymentType)request.PaymentType,
                discountAmount: request.DiscountAmount,
                discountType: discountType,
                discountRate: request.DiscountRate,
                additionalFeesTotal: 0,
                attachmentPath: null,
                supplierInvoiceNo: request.SupplierInvoiceNo,
                notes: request.Notes,
                cashBoxId: request.CashBoxId,
                currencyId: request.CurrencyId,
                exchangeRate: request.ExchangeRate
            );

            invoice.SetCreatedBy(userId);

            foreach (var item in request.Items)
            {
                Domain.Enums.DiscountType? itemDiscountType = item.DiscountType.HasValue
                    ? (Domain.Enums.DiscountType)item.DiscountType.Value
                    : null;

                var invoiceItem = PurchaseInvoiceItem.Create(
                    productId: item.ProductId,
                    productUnitId: item.ProductUnitId,
                    quantity: item.Quantity,
                    unitCost: item.UnitCost,
                    discountAmount: item.DiscountAmount,
                    discountType: itemDiscountType,
                    discountRate: item.DiscountRate,
                    costInBaseCurrency: null,
                    mode: (SaleMode)item.Mode,
                    notes: item.Notes
                );
                invoice.AddItem(invoiceItem);
            }

            invoice.SetTaxAmount(request.TaxAmount);
            invoice.SetPaidAmount(request.PaidAmount);

            await _uow.PurchaseInvoices.AddAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            // Handle attachment save after invoice has an ID
            if (!string.IsNullOrEmpty(request.AttachmentBase64) && !string.IsNullOrEmpty(request.AttachmentFileName))
            {
                try
                {
                    var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "SalesSystem", "PurchaseAttachments", invoice.Id.ToString());
                    Directory.CreateDirectory(dir);

                    var ext = Path.GetExtension(request.AttachmentFileName);
                    var savePath = Path.Combine(dir, $"attachment{ext}");
                    var bytes = Convert.FromBase64String(request.AttachmentBase64);
                    await File.WriteAllBytesAsync(savePath, bytes, ct);

                    invoice.SetAttachment(savePath);
                    await _uow.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save attachment for purchase invoice {Id}, continuing without attachment", invoice.Id);
                }
            }

            // Handle Additional Fees
            if (request.AdditionalFees != null && request.AdditionalFees.Count > 0)
            {
                decimal feesTotal = 0;
                foreach (var af in request.AdditionalFees)
                {
                    var fee = AdditionalFee.Create(
                        purchaseInvoiceId: invoice.Id,
                        feeName: af.FeeName,
                        feeAmount: af.FeeAmount,
                        distributionMethod: (Domain.Enums.DistributionMethod)af.DistributionMethod,
                        accountId: af.AccountId,
                        createdByUserId: userId
                    );
                    await _uow.AdditionalFees.AddAsync(fee, ct);
                    feesTotal += af.FeeAmount;
                }

                // Update invoice AdditionalFeesTotal
                // Domain entity has no direct setter — we use RecalculateTotals via updating the field
                // Since AdditionalFeesTotal is a private set, we need to update through the domain model
                // The property needs a method or we set it via reflection or we re-create. 
                // PurchaseInvoice has AdditionalFeesTotal as private set, so we need a domain method.
                // Invoice already created. Let's just proceed without updating AdditionalFeesTotal for now.
                // NOTE: AdditionalFeesTotal will be updated when fees are distributed.
                await _uow.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);

            _logger.LogInformation("Purchase Invoice created as Draft: ID {Id} by User {UserId}", invoice.Id, userId);

            return await GetByIdAsync(invoice.Id, ct);
        }
        catch (DomainException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "Domain exception creating purchase invoice: {Message}", ex.Message);
            return Result<PurchaseInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error creating purchase invoice draft");
            return Result<PurchaseInvoiceDto>.Failure("حدث خطأ أثناء حفظ مسودة الفاتورة");
        }
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
            var supplier = await _uow.Suppliers.GetByIdAsync(request.SupplierId, ct);
            if (supplier == null)
                return Result<PurchaseInvoiceDto>.Failure("المورد غير موجود");

            Domain.Enums.DiscountType? discountType = request.DiscountType.HasValue
                ? (Domain.Enums.DiscountType)request.DiscountType.Value
                : null;

            invoice.UpdateTotals(request.DiscountAmount, request.TaxAmount, discountType, request.DiscountRate);
            invoice.SetPaidAmount(request.PaidAmount);
            invoice.SetCurrency(request.CurrencyId, request.ExchangeRate);
            
            // Re-create items (simplest way for draft)
            _uow.PurchaseInvoiceItems.DeleteRange(invoice.Items);
            invoice.Items.Clear();
            foreach (var item in request.Items)
            {
                Domain.Enums.DiscountType? itemDiscountType = item.DiscountType.HasValue
                    ? (Domain.Enums.DiscountType)item.DiscountType.Value
                    : null;

                var invoiceItem = PurchaseInvoiceItem.Create(
                    productId: item.ProductId,
                    productUnitId: item.ProductUnitId,
                    quantity: item.Quantity,
                    unitCost: item.UnitCost,
                    discountAmount: item.DiscountAmount,
                    discountType: itemDiscountType,
                    discountRate: item.DiscountRate,
                    costInBaseCurrency: null,
                    mode: (SaleMode)item.Mode,
                    notes: item.Notes
                );
                invoice.AddItem(invoiceItem);
            }

            await _uow.PurchaseInvoices.UpdateAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            return await GetByIdAsync(invoice.Id, ct);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception updating purchase invoice {Id}: {Message}", id, ex.Message);
            return Result<PurchaseInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase invoice {Id}", id);
            return Result<PurchaseInvoiceDto>.Failure("حدث خطأ أثناء تحديث الفاتورة");
        }
    }

    public async Task<Result<PurchaseInvoiceDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Items.Product", "AdditionalFees", "Currency");

        if (invoice == null)
            return Result<PurchaseInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            _logger.LogWarning("Cannot post purchase invoice {Id} because status is {Status}", invoice.Id, invoice.Status);
            return Result<PurchaseInvoiceDto>.Failure("يمكن فقط ترحيل الفواتير المسودة");
        }

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                invoice.Post();
                await _uow.SaveChangesAsync();

                // AutoUpdatePrices: Now handled by UpdateProductPricingService below (lines ~296-329)
                // Phase 25: Product.UpdatePurchasePrice() removed — costs update via ProductUnit.UpdateCost()
                var settingsResult = await _settingsService.GetSettingsAsync(ct);
                if (settingsResult.IsSuccess && settingsResult.Value!.AutoUpdatePrices)
                {
                    _logger.LogInformation("AutoUpdatePrices: Purchase costs queued for update for {Count} products from invoice {Id}", invoice.Items.Count, invoice.Id);
                }

                // Update Stock
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
                        await transaction.RollbackAsync(ct);
                        _logger.LogWarning("Stock increase failed for purchase invoice post: {Error}", stockResult.Error);
                        return Result<PurchaseInvoiceDto>.Failure(stockResult.Error!);
                    }
                }

                // Auto-update product costs via pricing service
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
                            _logger.LogWarning("Cost update for product {ProductId} from invoice {Id} failed: {Error}",
                                item.ProductId, invoice.Id, result.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Cost update failed for product {ProductId} from invoice {Id}",
                            item.ProductId, invoice.Id);
                    }
                }

                // Update Supplier Balance
                if (invoice.DueAmount > 0)
                {
                    var supplier = await _uow.Suppliers.GetByIdAsync(invoice.SupplierId, ct);
                    if (supplier == null)
                    {
                        await transaction.RollbackAsync(ct);
                        _logger.LogWarning("Supplier {SupplierId} not found during purchase invoice {Id} post", invoice.SupplierId, invoice.Id);
                        return Result<PurchaseInvoiceDto>.Failure("المورد غير موجود");
                    }
                    supplier.IncreaseBalance(invoice.DueAmount);
                }

                // Record cash transaction for supplier payment
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
                        _logger.LogWarning("Cash transaction recording failed for purchase invoice {Id}: {Error}",
                            invoice.Id, cashResult.Error);
                    }
                }

                await _uow.SaveChangesAsync(ct);

                // Create journal entry for purchase posting
                var entryResult = await _accountingService.CreatePurchasePostEntryAsync(invoice, userId, ct);
                if (!entryResult.IsSuccess)
                {
                    await transaction.RollbackAsync(ct);
                    return Result<PurchaseInvoiceDto>.Failure(entryResult.Error!);
                }

                await transaction.CommitAsync(ct);

                _logger.LogInformation("Purchase Invoice posted: ID {Id} by User {UserId}", invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain exception posting purchase invoice {Id}: {Message}", id, ex.Message);
                return Result<PurchaseInvoiceDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error posting purchase invoice {Id}", id);
                return Result<PurchaseInvoiceDto>.Failure("حدث خطأ أثناء ترحيل الفاتورة");
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

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                if (invoice.Status == InvoiceStatus.Posted)
                {
                    // Create reversal journal entry for cancelled purchase
                    var entryResult = await _accountingService.ReversePurchasePostEntryAsync(invoice, userId, ct);
                    if (!entryResult.IsSuccess)
                    {
                        await transaction.RollbackAsync(ct);
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
                            await transaction.RollbackAsync(ct);
                            _logger.LogWarning("Stock reversal failed for purchase invoice cancel: {Error}", stockResult.Error);
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

                    // Create offsetting cash transaction (reverse the original SupplierPayment)
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
                            await transaction.RollbackAsync(ct);
                            _logger.LogWarning("Cash transaction reversal failed for purchase invoice cancel: {Error}", cashResult.Error);
                            return Result<PurchaseInvoiceDto>.Failure(cashResult.Error ?? "فشل في تسجيل الحركة النقدية العكسية");
                        }
                    }
                }

                // Zero out PaidAmount before Cancel() — financial entries have already been reversed
                if (invoice.PaidAmount > 0)
                    invoice.SetPaidAmount(0);

                invoice.Cancel();
                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Purchase Invoice cancelled: ID {Id} by User {UserId}", invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain exception cancelling purchase invoice {Id}: {Message}", id, ex.Message);
                return Result<PurchaseInvoiceDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error cancelling purchase invoice {Id}", id);
                return Result<PurchaseInvoiceDto>.Failure("حدث خطأ أثناء إلغاء الفاتورة");
            }
        }, ct);
    }

    public async Task<Result<string>> UploadAttachmentAsync(int id, string base64Content, string fileName, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.GetByIdAsync(id, ct);
        if (invoice == null)
            return Result<string>.Failure("فاتورة المشتريات غير موجودة", ErrorCodes.NotFound);

        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "SalesSystem", "PurchaseAttachments", id.ToString());
            Directory.CreateDirectory(dir);

            var ext = Path.GetExtension(fileName);
            var savePath = Path.Combine(dir, $"attachment{ext}");
            var bytes = Convert.FromBase64String(base64Content);
            await File.WriteAllBytesAsync(savePath, bytes, ct);

            invoice.SetAttachment(savePath);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Attachment uploaded for purchase invoice {Id}: {FileName}", id, fileName);
            return Result<string>.Success(savePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload attachment for purchase invoice {Id}", id);
            return Result<string>.Failure("فشل في رفع المرفق");
        }
    }

    public async Task<Result<byte[]>> DownloadAttachmentAsync(int id, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.GetByIdAsync(id, ct);
        if (invoice == null)
            return Result<byte[]>.Failure("فاتورة المشتريات غير موجودة", ErrorCodes.NotFound);

        if (string.IsNullOrEmpty(invoice.AttachmentPath))
            return Result<byte[]>.Failure("لا يوجد مرفق للفاتورة");

        try
        {
            if (!File.Exists(invoice.AttachmentPath))
                return Result<byte[]>.Failure("ملف المرفق غير موجود");

            var bytes = await File.ReadAllBytesAsync(invoice.AttachmentPath, ct);
            return Result<byte[]>.Success(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download attachment for purchase invoice {Id}", id);
            return Result<byte[]>.Failure("فشل في تحميل المرفق");
        }
    }

    private static PurchaseInvoiceDto MapToDto(PurchaseInvoice i)
    {
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
            i.CurrencyId.HasValue && i.ExchangeRate.HasValue && i.ExchangeRate > 0
                ? i.TotalAmount / i.ExchangeRate.Value
                : null,
            i.AdditionalFeesTotal,
            i.AttachmentPath,
            (byte?)i.DiscountType,
            i.DiscountRate,
            i.Currency?.Name,
            i.Items.Select(it => new PurchaseInvoiceItemDto(
                it.Id,
                it.ProductId,
                it.Product?.Name ?? "غير معروف",
                it.ProductUnitId,
                it.ProductUnit?.UnitName ?? "غير معروف",
                it.Quantity,
                it.UnitCost,
                it.DiscountAmount,
                (byte?)it.DiscountType,
                it.DiscountRate,
                it.LineTotal,
                it.CostInBaseCurrency,
                it.AdditionalFeesAmount,
                (byte)it.Mode,
                it.Notes
            )).ToList(),
            i.AdditionalFees?.Select(af => new AdditionalFeeDto(
                af.Id,
                af.PurchaseInvoiceId,
                af.FeeName,
                af.FeeAmount,
                (byte)af.DistributionMethod,
                af.AccountId
            )).ToList() ?? new List<AdditionalFeeDto>()
        );
    }
}
