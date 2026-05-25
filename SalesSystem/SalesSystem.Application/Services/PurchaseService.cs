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
    private readonly IDocumentSequenceService _sequenceService;
    private readonly IStoreSettingsService _settingsService;
    private readonly IUpdateProductPricingService _pricingService;
    private readonly ICashBoxService _cashBoxService;
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        IStoreSettingsService settingsService,
        IUpdateProductPricingService pricingService,
        ICashBoxService cashBoxService,
        ILogger<PurchaseService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _settingsService = settingsService;
        _pricingService = pricingService;
        _cashBoxService = cashBoxService;
        _logger = logger;
    }

    public async Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Supplier", "Warehouse", "Items.Product");

        if (invoice == null)
            return Result<PurchaseInvoiceDto>.Failure("فاتورة المشتريات غير موجودة", ErrorCodes.NotFound);

        return Result<PurchaseInvoiceDto>.Success(MapToDto(invoice));
    }

    public async Task<Result<PurchaseInvoiceDto>> GetByNumberAsync(string invoiceNo, CancellationToken ct = default)
    {
        var invoice = await _uow.PurchaseInvoices.FirstOrDefaultAsync(
            i => i.InvoiceNo == invoiceNo, ct, "Supplier", "Warehouse", "Items.Product");

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

        System.Linq.Expressions.Expression<System.Func<PurchaseInvoice, bool>> predicate = i =>
            (!supplierId.HasValue || i.SupplierId == supplierId.Value) &&
            (!status.HasValue || (int)i.Status == status.Value) &&
            (status.HasValue || includeInactive || i.Status != InvoiceStatus.Cancelled) &&
            (!from.HasValue || i.InvoiceDate >= from.Value) &&
            (!to.HasValue || i.InvoiceDate <= to.Value) &&
            (searchLower == null ||
             i.InvoiceNo.ToLower().Contains(searchLower) ||
             (i.Supplier != null && i.Supplier.Name.ToLower().Contains(searchLower)) ||
             (i.SupplierInvoiceNo != null && i.SupplierInvoiceNo.ToLower().Contains(searchLower)) ||
             (i.Notes != null && i.Notes.ToLower().Contains(searchLower)) ||
             i.Items.Any(item =>
                 item.Product != null && (
                     item.Product.Name.ToLower().Contains(searchLower) ||
                     (item.Product.Barcode ?? "").ToLower().Contains(searchLower))));

        var includes = new[] { "Supplier", "Warehouse", "Items.Product" };

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
            var invoiceNoResult = await _sequenceService.GetNextNumberAsync("PUR", ct);
            if (!invoiceNoResult.IsSuccess)
                return Result<PurchaseInvoiceDto>.Failure(invoiceNoResult.Error!);

            var invoice = PurchaseInvoice.Create(
                invoiceNoResult.Value!,
                request.SupplierId,
                request.WarehouseId,
                request.InvoiceDate,
                request.DueDate,
                (Domain.Enums.PaymentType)request.PaymentType,
                request.DiscountAmount,
                request.SupplierInvoiceNo,
                request.Notes,
                cashBoxId: request.CashBoxId
            );

            invoice.SetCreatedBy(userId);

            foreach (var item in request.Items)
            {
                var invoiceItem = PurchaseInvoiceItem.Create(
                    item.ProductId,
                    item.Quantity,
                    item.UnitCost,
                    item.DiscountAmount,
                    (SaleMode)item.Mode,
                    item.Notes
                );
                invoice.AddItem(invoiceItem);
            }

            invoice.SetTaxAmount(request.TaxAmount);
            invoice.SetPaidAmount(request.PaidAmount);

            await _uow.PurchaseInvoices.AddAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            _logger.LogInformation("Purchase Invoice created as Draft: {InvoiceNo} (ID: {Id}) by User {UserId}", invoice.InvoiceNo, invoice.Id, userId);

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

            invoice.UpdateTotals(request.DiscountAmount, request.TaxAmount);
            invoice.SetPaidAmount(request.PaidAmount);
            
            // Re-create items (simplest way for draft)
            _uow.PurchaseInvoiceItems.DeleteRange(invoice.Items);
            invoice.Items.Clear();
            foreach (var item in request.Items)
            {
                var invoiceItem = PurchaseInvoiceItem.Create(
                    item.ProductId,
                    item.Quantity,
                    item.UnitCost,
                    item.DiscountAmount,
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
            i => i.Id == id, ct, "Items.Product");

        if (invoice == null)
            return Result<PurchaseInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            _logger.LogWarning("Cannot post purchase invoice {InvoiceNo} because status is {Status}", invoice.InvoiceNo, invoice.Status);
            return Result<PurchaseInvoiceDto>.Failure("يمكن فقط ترحيل الفواتير المسودة");
        }

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                invoice.Post();
                await _uow.SaveChangesAsync();

                // AutoUpdatePrices: Update product purchase prices if enabled
                var settingsResult = await _settingsService.GetSettingsAsync(ct);
                if (settingsResult.IsSuccess && settingsResult.Value!.AutoUpdatePrices)
                {
                    foreach (var item in invoice.Items)
                    {
                        if (item.Product != null)
                        {
                            var retailUnitCost = item.Product.GetRetailQuantityEquivalent(1, item.Mode) > 0
                                ? item.UnitCost / item.Product.GetRetailQuantityEquivalent(1, item.Mode)
                                : item.UnitCost;
                            item.Product.UpdatePurchasePrice(retailUnitCost, userId);
                        }
                    }
                    await _uow.SaveChangesAsync(ct);
                    _logger.LogInformation("AutoUpdatePrices: Updated purchase prices for {Count} products from invoice {InvoiceNo}", invoice.Items.Count, invoice.InvoiceNo);
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
                            _logger.LogWarning("Cost update for product {ProductId} from invoice {InvoiceNo} failed: {Error}",
                                item.ProductId, invoice.InvoiceNo, result.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Cost update failed for product {ProductId} from invoice {InvoiceNo}",
                            item.ProductId, invoice.InvoiceNo);
                    }
                }

                // Update Supplier Balance
                if (invoice.DueAmount > 0)
                {
                    var supplier = await _uow.Suppliers.GetByIdAsync(invoice.SupplierId, ct);
                    if (supplier == null)
                    {
                        await transaction.RollbackAsync(ct);
                        _logger.LogWarning("Supplier {SupplierId} not found during purchase invoice {InvoiceNo} post", invoice.SupplierId, invoice.InvoiceNo);
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
                        _logger.LogWarning("Cash transaction recording failed for purchase invoice {InvoiceNo} (ID: {Id}): {Error}",
                            invoice.InvoiceNo, invoice.Id, cashResult.Error);
                    }
                }

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Purchase Invoice posted: {InvoiceNo} (ID: {Id}) by User {UserId}", invoice.InvoiceNo, invoice.Id, userId);

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

                    // Create offsetting cash transaction
                    if (invoice.CashBoxId.HasValue && invoice.PaidAmount > 0)
                    {
                        var cashResult = await _cashBoxService.RecordInvoicePaymentAsync(
                            invoice.CashBoxId.Value,
                            invoice.PaidAmount,
                            CashTransactionType.SupplierPayment,
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

                _logger.LogInformation("Purchase Invoice cancelled: {InvoiceNo} (ID: {Id}) by User {UserId}", invoice.InvoiceNo, invoice.Id, userId);

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
            i.Items.Select(it => new PurchaseInvoiceItemDto(
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
