using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Application.Printing;
using SalesSystem.Application.Printing.Contracts;
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
    private readonly ICashBoxService _cashBoxService;
    private readonly IPrintDataService _printDataService;
    private readonly IPrintService _printService;
    private readonly ILogger<SalesService> _logger;

    public SalesService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        ICashBoxService cashBoxService,
        IPrintDataService printDataService,
        IPrintService printService,
        ILogger<SalesService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _cashBoxService = cashBoxService;
        _printDataService = printDataService;
        _printService = printService;
        _logger = logger;
    }

    public async Task<Result<SalesInvoiceDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Customer", "Warehouse", "Items.Product");

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
        // Build predicate dynamically for search conditions
        var searchLower = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();

        // Parse search text as Id outside the expression tree (EF Core can't translate int.TryParse)
        int? searchId = int.TryParse(searchLower, out var parsedId) ? parsedId : null;

        System.Linq.Expressions.Expression<System.Func<SalesInvoice, bool>> predicate = i =>
            (includeInactive || i.Status != InvoiceStatus.Cancelled) &&
            (!customerId.HasValue || i.CustomerId == customerId.Value) &&
            (!status.HasValue || (int)i.Status == status.Value) &&
            (!from.HasValue || i.InvoiceDate >= from.Value) &&
            (!to.HasValue || i.InvoiceDate <= to.Value) &&
            (searchLower == null ||
             (searchId.HasValue && i.Id == searchId.Value) ||
             (i.Customer != null && i.Customer.Name.ToLower().Contains(searchLower)) ||
             (i.Notes != null && i.Notes.ToLower().Contains(searchLower)) ||
             i.Items.Any(item =>
                 item.Product != null && (
                     item.Product.Name.ToLower().Contains(searchLower) ||
                     (item.Product.Barcode ?? "").ToLower().Contains(searchLower))));

        var includes = new[] { "Customer", "Warehouse", "Items.Product" };

        var (items, total) = await _uow.SalesInvoices.GetPagedAsync(
            predicate, q => q.OrderByDescending(i => i.InvoiceDate), page, pageSize, ct, includeInactive, includes);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<SalesInvoiceDto>>.Success(PagedResult<SalesInvoiceDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<SalesInvoiceDto>> CreateAsync(CreateSalesInvoiceRequest request, int userId, CancellationToken ct)
    {
        // Compute default InvoiceNo if not provided: last Id + 1
        var invoiceNo = request.InvoiceNo ?? 0;
        if (invoiceNo <= 0)
        {
            var lastInvoices = await _uow.SalesInvoices.ToListAsync(
                predicate: null,
                queryConfig: q => q.OrderByDescending(i => i.Id).Take(1),
                ct: ct);
            var lastId = lastInvoices.FirstOrDefault()?.Id ?? 0;
            invoiceNo = lastId + 1;
        }

        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            var invoice = SalesInvoice.Create(
                request.WarehouseId,
                invoiceNo,
                request.CustomerId,
                request.InvoiceDate,
                request.DueDate,
                (Domain.Enums.PaymentType)request.PaymentType,
                request.DiscountAmount,
                request.Notes,
                cashBoxId: request.CashBoxId
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

            await transaction.CommitAsync(ct);

            _logger.LogInformation("Sales Invoice created as Draft: ID {Id} by User {UserId}", invoice.Id, userId);

            return await GetByIdAsync(invoice.Id, ct);
        }
        catch (DomainException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "Validation error creating sales invoice draft");
            return Result<SalesInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error creating sales invoice draft");
            return Result<SalesInvoiceDto>.Failure("حدث خطأ أثناء حفظ مسودة الفاتورة");
        }
    }

    public async Task<Result<SalesInvoiceDto>> UpdateAsync(int id, UpdateSalesInvoiceRequest request, int userId, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Items");

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
        var invoice = await _uow.SalesInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Items.Product");

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
        {
            _logger.LogWarning("Cannot post invoice {Id} because status is {Status}", invoice.Id, invoice.Status);
            return Result<SalesInvoiceDto>.Failure("يمكن فقط ترحيل الفواتير المسودة");
        }

        var settings = await _uow.StoreSettings.FirstOrDefaultAsync(s => true, ct);
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
                        _logger.LogWarning("Stock decrease failed for sales invoice post: {Error}", stockResult.Error);
                        return Result<SalesInvoiceDto>.Failure(stockResult.Error!);
                    }
                }

                // 4. Update Customer Balance
                if (invoice.DueAmount > 0)
                {
                    if (!invoice.CustomerId.HasValue)
                    {
                        await transaction.RollbackAsync(ct);
                        _logger.LogWarning("Cannot post credit invoice {Id} without a customer", invoice.Id);
                        return Result<SalesInvoiceDto>.Failure("يجب تحديد عميل للفواتير الآجلة");
                    }

                    var customer = await _uow.Customers.GetByIdAsync(invoice.CustomerId.Value, ct);
                    if (customer == null)
                    {
                        await transaction.RollbackAsync(ct);
                        _logger.LogWarning("Customer {CustomerId} not found for credit sales invoice {Id} post", invoice.CustomerId, invoice.Id);
                        return Result<SalesInvoiceDto>.Failure("العميل غير موجود");
                    }
                    customer.IncreaseBalance(invoice.DueAmount);
                }

                // 5. Record cash transaction if payment is linked to a cash box
                if (invoice.CashBoxId.HasValue && invoice.PaidAmount > 0)
                {
                    var cashResult = await _cashBoxService.RecordInvoicePaymentAsync(
                        invoice.CashBoxId.Value,
                        invoice.PaidAmount,
                        CashTransactionType.SalesIncome,
                        "SalesInvoice",
                        invoice.Id,
                        userId,
                        ct);

                    if (!cashResult.IsSuccess)
                    {
                        _logger.LogWarning("Cash transaction recording failed for invoice {Id}: {Error}",
                            invoice.Id, cashResult.Error);
                    }
                }

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Sales Invoice posted: ID {Id} by User {UserId}", invoice.Id, userId);

                var postedResult = await GetByIdAsync(invoice.Id, ct);

                // Fire-and-forget auto-print if enabled — failure MUST NOT roll back the post
                if (postedResult.IsSuccess && postedResult.Value != null)
                {
                    var capturedInvoiceId = invoice.Id;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var settingsResult = await _printDataService.GetPrintSettingsAsync(CancellationToken.None);
                            if (settingsResult.IsSuccess && settingsResult.Value != null && settingsResult.Value.AutoPrintOnPost)
                            {
                                var printDataResult = await _printDataService.GetSalesInvoicePrintDataAsync(capturedInvoiceId, CancellationToken.None);
                                if (printDataResult.IsSuccess && printDataResult.Value != null)
                                {
                                    var printResult = await _printService.PrintThermalAsync(printDataResult.Value);
                                    if (!printResult.IsSuccess)
                                    {
                                        _logger.LogWarning("Auto-print failed for invoice {InvoiceId}: {Error}", capturedInvoiceId, printResult.ErrorMessage);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Auto-print error for invoice {InvoiceId}", capturedInvoiceId);
                        }
                    }, CancellationToken.None);
                }

                return postedResult;
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain exception posting sales invoice {Id}: {Message}", id, ex.Message);
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
        var invoice = await _uow.SalesInvoices.FirstOrDefaultAsync(
            i => i.Id == id, ct, "Items.Product");

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("الفاتورة غير موجودة", ErrorCodes.NotFound);

        if (invoice.Status == InvoiceStatus.Cancelled)
            return Result<SalesInvoiceDto>.Failure("الفاتورة ملغاة بالفعل", ErrorCodes.InvalidOperation);

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
                            _logger.LogWarning("Stock increase reversal failed for sales invoice cancel: {Error}", stockResult.Error);
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

                    // Create offsetting cash transaction if invoice had cash box
                    if (invoice.CashBoxId.HasValue && invoice.PaidAmount > 0)
                    {
                        var cashResult = await _cashBoxService.RecordInvoicePaymentAsync(
                            invoice.CashBoxId.Value,
                            invoice.PaidAmount,
                            CashTransactionType.RefundOut,
                            "SalesInvoiceCancel",
                            invoice.Id,
                            userId,
                            ct);

                        if (!cashResult.IsSuccess)
                        {
                            _logger.LogWarning("Cash transaction recording failed during cancellation of invoice {Id}: {Error}",
                                invoice.Id, cashResult.Error);
                        }
                    }
                }

                // Zero out PaidAmount before Cancel() — financial entries have already been reversed
                if (invoice.PaidAmount > 0)
                    invoice.SetPaidAmount(0);

                invoice.Cancel();
                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Sales Invoice cancelled: ID {Id} by User {UserId}", invoice.Id, userId);

                return await GetByIdAsync(invoice.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain exception cancelling sales invoice {Id}: {Message}", id, ex.Message);
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
