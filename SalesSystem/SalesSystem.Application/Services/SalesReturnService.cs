using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

public class SalesReturnService : ISalesReturnService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<SalesReturnService> _logger;

    public SalesReturnService(
        IUnitOfWork uow, 
        IInventoryService inventoryService, 
        IDocumentSequenceService sequenceService, 
        ILogger<SalesReturnService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<SalesReturnDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var sr = await _uow.SalesReturns.Query()
            .Include(r => r.Customer)
            .Include(r => r.Warehouse)
            .Include(r => r.Items)
                .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (sr == null)
            return Result<SalesReturnDto>.Failure("ظ…ط±طھط¬ط¹ ط§ظ„ظ…ط¨ظٹط¹ط§طھ ط؛ظٹط± ظ…ظˆط¬ظˆط¯", ErrorCodes.NotFound);

        return Result<SalesReturnDto>.Success(MapToDto(sr));
    }

    public async Task<Result<PagedResult<SalesReturnDto>>> GetAllAsync(int? customerId, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.SalesReturns.Query()
            .Include(r => r.Customer)
            .Include(r => r.Warehouse)
            .AsQueryable();

        if (customerId.HasValue) query = query.Where(r => r.CustomerId == customerId.Value);

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.ReturnDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<SalesReturnDto>>.Success(PagedResult<SalesReturnDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, int userId, CancellationToken ct)
    {
        // 1. Validation
        if (request.SalesInvoiceId.HasValue)
        {
            var invoice = await _uow.SalesInvoices.Query()
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == request.SalesInvoiceId.Value, ct);

            if (invoice == null) return Result<SalesReturnDto>.Failure("ط§ظ„ظپط§طھظˆط±ط© ط§ظ„ط£طµظ„ظٹط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©");

            foreach (var item in request.Items)
            {
                var originalLine = invoice.Items.FirstOrDefault(it => it.ProductId == item.ProductId);
                if (originalLine == null)
                    return Result<SalesReturnDto>.Failure($"ط§ظ„ظ…ظ†طھط¬ {item.ProductId} ط؛ظٹط± ظ…ظˆط¬ظˆط¯ ظپظٹ ط§ظ„ظپط§طھظˆط±ط© ط§ظ„ط£طµظ„ظٹط©");
                
                if (item.Quantity > originalLine.Quantity)
                    return Result<SalesReturnDto>.Failure($"ط§ظ„ظƒظ…ظٹط© ط§ظ„ظ…ط±طھط¬ط¹ط© ظ„ظ„ظ…ظ†طھط¬ {item.ProductId} ط£ظƒط¨ط± ظ…ظ† ط§ظ„ظƒظ…ظٹط© ط§ظ„ظ…ط¨ط§ط¹ط© ({originalLine.Quantity})");
            }
        }

        // 2. Transaction
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            var returnNoResult = await _sequenceService.GetNextNumberAsync("SR", ct);
            if (!returnNoResult.IsSuccess) return Result<SalesReturnDto>.Failure(returnNoResult.Error!);

            var salesReturn = SalesReturn.Create(
                returnNoResult.Value!,
                request.WarehouseId,
                request.CustomerId,
                request.SalesInvoiceId,
                request.ReturnDate,
                request.Notes,
                userId
            );

            foreach (var item in request.Items)
            {
                salesReturn.AddItem(item.ProductId, item.Quantity, item.UnitPrice, item.DiscountAmount, item.Notes);
            }

            await _uow.SalesReturns.AddAsync(salesReturn, ct);
            await _uow.SaveChangesAsync(ct);

            // 3. Stock & Balance
            foreach (var item in salesReturn.Items)
            {
                await _inventoryService.IncreaseStockAsync(
                    item.ProductId, 
                    salesReturn.WarehouseId, 
                    item.Quantity, 
                    MovementType.SaleReturnIn, 
                    "SalesReturn", 
                    salesReturn.Id, 
                    item.UnitPrice, 
                    userId, 
                    ct);
            }

            if (salesReturn.TotalAmount > 0 && salesReturn.CustomerId.HasValue)
            {
                var customer = await _uow.Customers.GetByIdAsync(salesReturn.CustomerId.Value, ct);
                if (customer != null)
                {
                    customer.DecreaseBalance(salesReturn.TotalAmount); // Reduce what they owe us
                }
            }

            await _uow.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Sales Return created: {ReturnNo} (ID: {Id})", salesReturn.ReturnNo, salesReturn.Id);

            return await GetByIdAsync(salesReturn.Id, ct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error creating sales return");
            return Result<SalesReturnDto>.Failure("ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، ط­ظپط¸ ط§ظ„ظ…ط±طھط¬ط¹");
        }
    }

        private static SalesReturnDto MapToDto(SalesReturn r)
    {
        return new SalesReturnDto(
            r.Id,
            r.ReturnNo,
            r.WarehouseId,
            r.Warehouse?.Name ?? "Unknown",
            r.CustomerId,
            r.Customer?.Name ?? "Unknown",
            r.SalesInvoiceId,
            r.ReturnDate,
            r.TotalAmount,
            r.Notes,
            (byte)r.Status,
            r.Items.Select(it => new SalesReturnItemDto(
                it.SalesReturnItemId,
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


