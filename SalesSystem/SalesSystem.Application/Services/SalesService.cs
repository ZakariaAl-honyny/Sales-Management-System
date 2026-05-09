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
            return Result<SalesInvoiceDto>.Failure("ظپط§طھظˆط±ط© ط§ظ„ظ…ط¨ظٹط¹ط§طھ ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©", ErrorCodes.NotFound);

        return Result<SalesInvoiceDto>.Success(MapToDto(invoice));
    }

    public async Task<Result<PagedResult<SalesInvoiceDto>>> GetAllAsync(int? customerId, int? status, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.SalesInvoices.Query()
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .AsQueryable();

        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);
        if (status.HasValue) query = query.Where(i => (int)i.Status == status.Value);

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
            var invoiceNoResult = await _sequenceService.GetNextNumberAsync("INV", ct);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales invoice draft");
            return Result<SalesInvoiceDto>.Failure("ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، ط­ظپط¸ ظ…ط³ظˆط¯ط© ط§ظ„ظپط§طھظˆط±ط©");
        }
    }

    public async Task<Result<SalesInvoiceDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.Query()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("ط§ظ„ظپط§طھظˆط±ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©", ErrorCodes.NotFound);

        if (invoice.Status != InvoiceStatus.Draft)
            return Result<SalesInvoiceDto>.Failure("ظٹظ…ظƒظ† ظپظ‚ط· طھط±ط­ظٹظ„ ط§ظ„ظپظˆط§طھظٹط± ط§ظ„ظ…ط³ظˆط¯ط©");

        // 1. Validate Stock BEFORE Transaction
        foreach (var item in invoice.Items)
        {
            var stockValidation = await _inventoryService.ValidateStockAsync(item.ProductId, invoice.WarehouseId, item.Quantity, ct);
            if (!stockValidation.IsSuccess)
                return Result<SalesInvoiceDto>.Failure(stockValidation.Error!);
        }

        // 2. Open Transaction
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
                    item.Quantity, 
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
                    return Result<SalesInvoiceDto>.Failure("ظٹط¬ط¨ طھط­ط¯ظٹط¯ ط¹ظ…ظٹظ„ ظ„ظ„ظپظˆط§طھظٹط± ط§ظ„ط¢ط¬ظ„ط©");
                }

                var customer = await _uow.Customers.GetByIdAsync(invoice.CustomerId.Value, ct);
                if (customer == null)
                {
                    await transaction.RollbackAsync(ct);
                    return Result<SalesInvoiceDto>.Failure("ط§ظ„ط¹ظ…ظٹظ„ ط؛ظٹط± ظ…ظˆط¬ظˆط¯");
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
            return Result<SalesInvoiceDto>.Failure("ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، طھط±ط­ظٹظ„ ط§ظ„ظپط§طھظˆط±ط©");
        }
    }

    public async Task<Result<SalesInvoiceDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var invoice = await _uow.SalesInvoices.Query()
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (invoice == null)
            return Result<SalesInvoiceDto>.Failure("ط§ظ„ظپط§طھظˆط±ط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©", ErrorCodes.NotFound);

        if (invoice.Status == InvoiceStatus.Cancelled)
            return await GetByIdAsync(id, ct);

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
                        item.Quantity, 
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
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error cancelling sales invoice {Id}", id);
            return Result<SalesInvoiceDto>.Failure("ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، ط¥ظ„ط؛ط§ط، ط§ظ„ظپط§طھظˆط±ط©");
        }
    }

        private static SalesInvoiceDto MapToDto(SalesInvoice i)
    {
        return new SalesInvoiceDto(
            i.Id,
            i.InvoiceNo,
            i.CustomerId,
            i.Customer?.Name ?? "عميل نقدي",
            i.WarehouseId,
            i.Warehouse?.Name ?? "Unknown",
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
                it.SalesInvoiceItemId,
                it.ProductId,
                it.Product?.Code,
                it.Product?.Name ?? "Unknown",
                it.Quantity,
                it.UnitPrice,
                it.DiscountAmount,
                it.LineTotal
            )).ToList()
        );
    }
}


