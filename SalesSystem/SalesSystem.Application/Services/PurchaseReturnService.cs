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

public class PurchaseReturnService : IPurchaseReturnService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<PurchaseReturnService> _logger;

    public PurchaseReturnService(
        IUnitOfWork uow, 
        IInventoryService inventoryService, 
        IDocumentSequenceService sequenceService, 
        ILogger<PurchaseReturnService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<PurchaseReturnDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var pr = await _uow.PurchaseReturns.Query()
            .Include(r => r.Supplier)
            .Include(r => r.Warehouse)
            .Include(r => r.Items)
                .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (pr == null)
            return Result<PurchaseReturnDto>.Failure("ظ…ط±طھط¬ط¹ ط§ظ„ظ…ط´طھط±ظٹط§طھ ط؛ظٹط± ظ…ظˆط¬ظˆط¯", ErrorCodes.NotFound);

        return Result<PurchaseReturnDto>.Success(MapToDto(pr));
    }

    public async Task<Result<PagedResult<PurchaseReturnDto>>> GetAllAsync(int? supplierId, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.PurchaseReturns.Query()
            .Include(r => r.Supplier)
            .Include(r => r.Warehouse)
            .AsQueryable();

        if (supplierId.HasValue) query = query.Where(r => r.SupplierId == supplierId.Value);

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.ReturnDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<PurchaseReturnDto>>.Success(PagedResult<PurchaseReturnDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest request, int userId, CancellationToken ct)
    {
        // 1. Validation
        if (request.PurchaseInvoiceId.HasValue)
        {
            var invoice = await _uow.PurchaseInvoices.Query()
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == request.PurchaseInvoiceId.Value, ct);

            if (invoice == null) return Result<PurchaseReturnDto>.Failure("ط§ظ„ظپط§طھظˆط±ط© ط§ظ„ط£طµظ„ظٹط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©");

            foreach (var item in request.Items)
            {
                var originalLine = invoice.Items.FirstOrDefault(it => it.ProductId == item.ProductId);
                if (originalLine == null)
                    return Result<PurchaseReturnDto>.Failure($"ط§ظ„ظ…ظ†طھط¬ {item.ProductId} ط؛ظٹط± ظ…ظˆط¬ظˆط¯ ظپظٹ ط§ظ„ظپط§طھظˆط±ط© ط§ظ„ط£طµظ„ظٹط©");
                
                if (item.Quantity > originalLine.Quantity)
                    return Result<PurchaseReturnDto>.Failure($"ط§ظ„ظƒظ…ظٹط© ط§ظ„ظ…ط±طھط¬ط¹ط© ظ„ظ„ظ…ظ†طھط¬ {item.ProductId} ط£ظƒط¨ط± ظ…ظ† ط§ظ„ظƒظ…ظٹط© ط§ظ„ظ…ط´طھط±ط§ط© ({originalLine.Quantity})");
            }
        }

        // 1b. Stock Validation BEFORE transaction
        foreach (var item in request.Items)
        {
            var stockValidation = await _inventoryService.ValidateStockAsync(item.ProductId, request.WarehouseId, item.Quantity, ct);
            if (!stockValidation.IsSuccess)
                return Result<PurchaseReturnDto>.Failure(stockValidation.Error!);
        }

        // 2. Transaction
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            var returnNoResult = await _sequenceService.GetNextNumberAsync("PR", ct);
            if (!returnNoResult.IsSuccess) return Result<PurchaseReturnDto>.Failure(returnNoResult.Error!);

            var purchaseReturn = PurchaseReturn.Create(
                returnNoResult.Value!,
                request.WarehouseId,
                request.SupplierId,
                request.PurchaseInvoiceId,
                request.ReturnDate,
                request.Notes,
                userId
            );

            foreach (var item in request.Items)
            {
                purchaseReturn.AddItem(item.ProductId, item.Quantity, item.UnitPrice, item.DiscountAmount, item.Notes);
            }

            await _uow.PurchaseReturns.AddAsync(purchaseReturn, ct);
            await _uow.SaveChangesAsync(ct);

            // 3. Stock & Balance
            foreach (var item in purchaseReturn.Items)
            {
                await _inventoryService.DecreaseStockAsync(
                    item.ProductId, 
                    purchaseReturn.WarehouseId, 
                    item.Quantity, 
                    MovementType.PurchaseReturnOut, 
                    "PurchaseReturn", 
                    purchaseReturn.Id, 
                    item.UnitCost, 
                    userId, 
                    ct);
            }

            if (purchaseReturn.TotalAmount > 0)
            {
                var supplier = await _uow.Suppliers.GetByIdAsync(purchaseReturn.SupplierId, ct);
                if (supplier != null)
                {
                    supplier.DecreaseBalance(purchaseReturn.TotalAmount); // Reduce what we owe them
                }
            }

            await _uow.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Purchase Return created: {ReturnNo} (ID: {Id})", purchaseReturn.ReturnNo, purchaseReturn.Id);

            return await GetByIdAsync(purchaseReturn.Id, ct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error creating purchase return");
            return Result<PurchaseReturnDto>.Failure("ط­ط¯ط« ط®ط·ط£ ط£ط«ظ†ط§ط، ط­ظپط¸ ط§ظ„ظ…ط±طھط¬ط¹");
        }
    }

        private static PurchaseReturnDto MapToDto(PurchaseReturn r)
    {
        return new PurchaseReturnDto(
            r.Id,
            r.ReturnNo,
            r.WarehouseId,
            r.Warehouse?.Name ?? "Unknown",
            r.SupplierId,
            r.Supplier?.Name ?? "Unknown",
            r.PurchaseInvoiceId,
            r.ReturnDate,
            r.TotalAmount,
            r.Notes,
            (byte)r.Status,
            r.Items.Select(it => new PurchaseReturnItemDto(
                it.PurchaseReturnItemId,
                it.ProductId,
                it.Product?.Code,
                it.Product?.Name ?? "Unknown",
                it.Quantity,
                it.UnitCost,
                it.DiscountAmount,
                it.LineTotal
            )).ToList()
        );
    }
}


