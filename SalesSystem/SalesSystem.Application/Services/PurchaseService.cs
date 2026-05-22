using Microsoft.EntityFrameworkCore;
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
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        IStoreSettingsService settingsService,
        ILogger<PurchaseService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.Query()
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice == null)
            return Result<PurchaseInvoiceDto>.Failure("فاتورة المشتريات غير موجودة", ErrorCodes.NotFound);

        return Result<PurchaseInvoiceDto>.Success(MapToDto(invoice));
    }

    public async Task<Result<PurchaseInvoiceDto>> GetByNumberAsync(string invoiceNo, CancellationToken ct = default)
    {
        var invoice = await _uow.PurchaseInvoices.Query()
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.InvoiceNo == invoiceNo, ct);

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
        var query = _uow.PurchaseInvoices.Query()
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .AsQueryable();

        if (supplierId.HasValue) query = query.Where(i => i.SupplierId == supplierId.Value);
        if (status.HasValue) query = query.Where(i => (int)i.Status == status.Value);
        else if (!includeInactive) query = query.Where(i => i.Status != InvoiceStatus.Cancelled);

        if (from.HasValue) query = query.Where(i => i.InvoiceDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.InvoiceDate <= to.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(i => 
                i.InvoiceNo.ToLower().Contains(searchLower) ||
                (i.Supplier != null && i.Supplier.Name.ToLower().Contains(searchLower)) ||
                (i.SupplierInvoiceNo != null && i.SupplierInvoiceNo.ToLower().Contains(searchLower)) ||
                (i.Notes != null && i.Notes.ToLower().Contains(searchLower)) ||
                i.Items.Any(item => 
                    item.Product.Name.ToLower().Contains(searchLower) ||
                    item.Product.Barcode.ToLower().Contains(searchLower))
            );
        }

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<PurchaseInvoiceDto>>.Success(PagedResult<PurchaseInvoiceDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var invoiceNoResult = await _sequenceService.GetNextNumberAsync("PUR", ct);
            if (!invoiceNoResult.IsSuccess)
                return Result<PurchaseInvoiceDto>.Failure(invoiceNoResult.Error!);

            var invoice = PurchaseInvoice.Create(
                invoiceNoResult.Value!,
                request.WarehouseId,
                request.SupplierId,
                request.InvoiceDate,
                request.DueDate,
                (Domain.Enums.PaymentType)request.PaymentType,
                request.DiscountAmount,
                request.SupplierInvoiceNo,
                request.Notes
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

            _logger.LogInformation("Purchase Invoice created as Draft: {InvoiceNo} (ID: {Id}) by User {UserId}", invoice.InvoiceNo, invoice.Id, userId);

            return await GetByIdAsync(invoice.Id, ct);
        }
        catch (DomainException ex)
        {
            return Result<PurchaseInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase invoice draft");
            return Result<PurchaseInvoiceDto>.Failure("حدث خطأ أثناء حفظ مسودة الفاتورة");
        }
    }

    public async Task<Result<PurchaseInvoiceDto>> UpdateAsync(int id, UpdatePurchaseInvoiceRequest request, int userId, CancellationToken ct)
    {
        var invoice = await _uow.PurchaseInvoices.Query()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

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
            // In a real scenario, we might need more Update methods on the entity
            // For now, we'll assume we can update these fields via reflection or adding methods to Domain
            
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
        var invoice = await _uow.PurchaseInvoices.Query()
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

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
                        return Result<PurchaseInvoiceDto>.Failure(stockResult.Error!);
                    }
                }

                // Update Supplier Balance
                if (invoice.DueAmount > 0)
                {
                    var supplier = await _uow.Suppliers.GetByIdAsync(invoice.SupplierId, ct);
                    if (supplier == null)
                    {
                        await transaction.RollbackAsync(ct);
                        return Result<PurchaseInvoiceDto>.Failure("المورد غير موجود");
                    }
                    supplier.IncreaseBalance(invoice.DueAmount);
                }

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Purchase Invoice posted: {InvoiceNo} (ID: {Id}) by User {UserId}", invoice.InvoiceNo, invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
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
        var invoice = await _uow.PurchaseInvoices.Query()
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice == null)
            return Result<PurchaseInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status == InvoiceStatus.Cancelled)
            return await GetByIdAsync(id, ct);

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
                }

                invoice.Cancel();
                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Purchase Invoice cancelled: {InvoiceNo} (ID: {Id}) by User {UserId}", invoice.InvoiceNo, invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
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


