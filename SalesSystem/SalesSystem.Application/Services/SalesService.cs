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

public class SalesService : ISalesService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<SalesService> _logger;

    public SalesService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        ILogger<SalesService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<SalesInvoiceDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.Query()
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("فاتورة المبيعات غير موجودة", ErrorCodes.NotFound);

        return Result<SalesInvoiceDto>.Success(MapToDto(invoice));
    }

    public async Task<Result<SalesInvoiceDto>> GetByNumberAsync(string invoiceNo, CancellationToken ct = default)
    {
        var invoice = await _uow.SalesInvoices.Query()
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(i => i.InvoiceNo == invoiceNo, ct);

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("فاتورة المبيعات غير موجودة", ErrorCodes.NotFound);

        return Result<SalesInvoiceDto>.Success(MapToDto(invoice));
    }

    public async Task<Result<PagedResult<SalesInvoiceDto>>> GetAllAsync(
        int? customerId, 
        int? status, 
        string? search = null, 
        DateTime? from = null, 
        DateTime? to = null, 
        int page = 1, 
        int pageSize = 10, 
        bool includeInactive = false, 
        CancellationToken ct = default)
    {
        var query = _uow.SalesInvoices.Query()
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .Include(i => i.Items)
                .ThenInclude(item => item.Product)
            .AsQueryable();

        if (!includeInactive && !status.HasValue)
        {
            query = query.Where(i => i.Status != InvoiceStatus.Cancelled);
        }

        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);
        if (status.HasValue) query = query.Where(i => (int)i.Status == status.Value);

        if (from.HasValue) query = query.Where(i => i.InvoiceDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.InvoiceDate <= to.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(i => 
                i.InvoiceNo.ToLower().Contains(searchLower) ||
                (i.Customer != null && i.Customer.Name.ToLower().Contains(searchLower)) ||
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

        return Result<PagedResult<SalesInvoiceDto>>.Success(PagedResult<SalesInvoiceDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<SalesInvoiceDto>> CreateAsync(CreateSalesInvoiceRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var settings = await _uow.StoreSettings.Query().FirstOrDefaultAsync(ct);
            var invoicePrefix = settings?.InvoicePrefix ?? "INV";
            
            var invoiceNoResult = await _sequenceService.GetNextNumberAsync(invoicePrefix, ct);
            if (!invoiceNoResult.IsSuccess)
                return Result<SalesInvoiceDto>.Failure(invoiceNoResult.Error!);

            var invoice = SalesInvoice.Create(
                invoiceNoResult.Value!,
                request.WarehouseId,
                request.CustomerId,
                request.InvoiceDate,
                request.DueDate,
                (Domain.Enums.PaymentType)request.PaymentType,
                request.DiscountAmount,
                request.Notes
            );

            invoice.SetCreatedBy(userId);

            foreach (var item in request.Items)
            {
                var invoiceItem = SalesInvoiceItem.Create(
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountAmount,
                    (SaleMode)item.Mode,
                    item.Notes
                );
                invoice.AddItem(invoiceItem);
            }

            invoice.SetTaxAmount(request.TaxAmount);
            invoice.SetPaidAmount(request.PaidAmount);

            await _uow.SalesInvoices.AddAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Sales Invoice created as Draft: {InvoiceNo} (ID: {Id}) by User {UserId}", invoice.InvoiceNo, invoice.Id, userId);

            return await GetByIdAsync(invoice.Id, ct);
        }
        catch (DomainException ex)
        {
            return Result<SalesInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales invoice draft");
            return Result<SalesInvoiceDto>.Failure("حدث خطأ أثناء حفظ مسودة الفاتورة");
        }
    }

    public async Task<Result<SalesInvoiceDto>> UpdateAsync(int id, UpdateSalesInvoiceRequest request, int userId, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.Query()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("فاتورة المبيعات غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
            return Result<SalesInvoiceDto>.Failure("يمكن تعديل الفواتير المسودة فقط");

        try
        {
            if (request.CustomerId.HasValue)
            {
                var customer = await _uow.Customers.GetByIdAsync(request.CustomerId.Value, ct);
                if (customer == null)
                    return Result<SalesInvoiceDto>.Failure("العميل غير موجود");
            }

            invoice.UpdateTotals(request.DiscountAmount, request.TaxAmount);
            invoice.SetPaidAmount(request.PaidAmount);

            // Re-create items (simplest way for draft)
            _uow.SalesInvoiceItems.DeleteRange(invoice.Items);
            invoice.Items.Clear();
            foreach (var item in request.Items)
            {
                var invoiceItem = SalesInvoiceItem.Create(
                    item.ProductId,
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountAmount,
                    (SaleMode)item.Mode,
                    item.Notes
                );
                invoice.AddItem(invoiceItem);
            }

            await _uow.SalesInvoices.UpdateAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            return await GetByIdAsync(invoice.Id, ct);
        }
        catch (DomainException ex)
        {
            return Result<SalesInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sales invoice {Id}", id);
            return Result<SalesInvoiceDto>.Failure("حدث خطأ أثناء تحديث الفاتورة");
        }
    }

    public async Task<Result<SalesInvoiceDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.Query()
            .Include(i => i.Items)
                .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            _logger.LogWarning("Cannot post invoice {InvoiceNo} because status is {Status}", invoice.InvoiceNo, invoice.Status);
            return Result<SalesInvoiceDto>.Failure("يمكن فقط ترحيل الفواتير المسودة");
        }

        var settings = await _uow.StoreSettings.Query().FirstOrDefaultAsync(ct);
        bool allowNegativeStock = settings?.AllowNegativeStock ?? false;

        // 1. Validate Stock BEFORE Transaction
        foreach (var item in invoice.Items)
        {
            var retailQty = item.Product!.GetRetailQuantityEquivalent(item.Quantity, item.Mode);
            var stockValidation = await _inventoryService.ValidateStockAsync(item.ProductId, invoice.WarehouseId, retailQty, allowNegativeStock, ct);
            if (!stockValidation.IsSuccess)
                return Result<SalesInvoiceDto>.Failure(stockValidation.Error!);
        }

        // 2. Open Transaction
        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                invoice.Post();
                await _uow.SaveChangesAsync(ct);

                // 3. Deduct Stock
                foreach (var item in invoice.Items)
                {
                    var stockResult = await _inventoryService.DecreaseStockAsync(
                        item.ProductId,
                        invoice.WarehouseId,
                        item.Product!.GetRetailQuantityEquivalent(item.Quantity, item.Mode),
                        MovementType.SaleOut,
                        "SalesInvoice",
                        invoice.Id,
                        item.UnitPrice,
                        userId,
                        ct);

                    if (!stockResult.IsSuccess)
                    {
                        await transaction.RollbackAsync(ct);
                        return Result<SalesInvoiceDto>.Failure(stockResult.Error!);
                    }
                }

                // 4. Update Customer Balance
                if (invoice.DueAmount > 0)
                {
                    if (!invoice.CustomerId.HasValue)
                    {
                        await transaction.RollbackAsync(ct);
                        _logger.LogWarning("Cannot post credit invoice {InvoiceNo} without a customer", invoice.InvoiceNo);
                        return Result<SalesInvoiceDto>.Failure("يجب تحديد عميل للفواتير الآجلة");
                    }

                    var customer = await _uow.Customers.GetByIdAsync(invoice.CustomerId.Value, ct);
                    if (customer == null)
                    {
                        await transaction.RollbackAsync(ct);
                        return Result<SalesInvoiceDto>.Failure("العميل غير موجود");
                    }
                    customer.IncreaseBalance(invoice.DueAmount);
                }

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Sales Invoice posted: {InvoiceNo} (ID: {Id}) by User {UserId}", invoice.InvoiceNo, invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                return Result<SalesInvoiceDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error posting sales invoice {Id}", id);
                return Result<SalesInvoiceDto>.Failure("حدث خطأ أثناء ترحيل الفاتورة");
            }
        }, ct);
    }

    public async Task<Result<SalesInvoiceDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.Query()
            .Include(i => i.Items)
                .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

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
                        var stockResult = await _inventoryService.IncreaseStockAsync(
                            item.ProductId,
                            invoice.WarehouseId,
                            item.Product!.GetRetailQuantityEquivalent(item.Quantity, item.Mode),
                            MovementType.SaleReturnIn,
                            "SalesInvoiceCancel",
                            invoice.Id,
                            item.UnitPrice,
                            userId,
                            ct);

                        if (!stockResult.IsSuccess)
                        {
                            await transaction.RollbackAsync(ct);
                            return Result<SalesInvoiceDto>.Failure(stockResult.Error!);
                        }
                    }

                    // Reverse Customer Balance
                    if (invoice.DueAmount > 0 && invoice.CustomerId.HasValue)
                    {
                        var customer = await _uow.Customers.GetByIdAsync(invoice.CustomerId.Value, ct);
                        if (customer != null)
                        {
                            customer.DecreaseBalance(invoice.DueAmount);
                        }
                    }
                }

                invoice.Cancel();
                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Sales Invoice cancelled: {InvoiceNo} (ID: {Id}) by User {UserId}", invoice.InvoiceNo, invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                return Result<SalesInvoiceDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error cancelling sales invoice {Id}", id);
                return Result<SalesInvoiceDto>.Failure("حدث خطأ أثناء إلغاء الفاتورة");
            }
        }, ct);
    }

    private static SalesInvoiceDto MapToDto(SalesInvoice i)
    {
        return new SalesInvoiceDto(
            i.Id,
            i.InvoiceNo,
            i.CustomerId,
            i.Customer?.Name ?? "عميل نقدي",
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
            i.Notes,
            (byte)i.Status,
            i.Items.Select(it => new SalesInvoiceItemDto(
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


