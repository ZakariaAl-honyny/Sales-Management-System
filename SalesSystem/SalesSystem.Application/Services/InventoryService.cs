using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Repositories;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

/// <summary>
/// Inventory management service using the new v4.10+ schema:
/// InventoryTransactions (document-based) and WarehouseTransfers replace old InventoryMovement, StockTransfer, etc.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ISystemSettingsRepository _systemSettingsRepo;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        IUnitOfWork uow,
        IDocumentSequenceService sequenceService,
        ISystemSettingsRepository systemSettingsRepo,
        ILogger<InventoryService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _systemSettingsRepo = systemSettingsRepo;
        _logger = logger;
    }

    #region Foundational Stock Methods

    public async Task<Result<decimal>> GetStockAsync(int productId, int warehouseId, CancellationToken ct)
    {
        var stock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
            ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, ct);

        if (stock == null)
        {
            _logger.LogWarning("Stock record not found for Product {ProductId} in Warehouse {WarehouseId}", productId, warehouseId);
            return Result<decimal>.Failure("لم يتم العثور على سجل مخزون لهذا المنتج في هذا المستودع");
        }

        return Result<decimal>.Success(stock.Quantity);
    }

    public async Task<Result> ValidateStockAsync(int productId, int warehouseId, decimal requiredQty, bool allowNegativeStock = false, CancellationToken ct = default)
    {
        if (allowNegativeStock)
            return Result.Success();

        var stockResult = await GetStockAsync(productId, warehouseId, ct);
        if (!stockResult.IsSuccess) return stockResult;

        if (stockResult.Value < requiredQty)
        {
            _logger.LogWarning("Insufficient stock for Product {ProductId} in Warehouse {WarehouseId}. Available: {Available}, Required: {Required}",
                productId, warehouseId, stockResult.Value, requiredQty);
            return Result.Failure($"المخزون غير كافٍ للمنتج {productId}: المتوفر {stockResult.Value}، المطلوب {requiredQty}");
        }

        return Result.Success();
    }

    public async Task<Result> IncreaseStockAsync(
        int productId, int warehouseId, decimal quantity,
        decimal? unitCost = null, int? userId = null,
        CancellationToken ct = default)
    {
        var stock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
            ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, ct);

        if (stock == null)
        {
            stock = WarehouseStock.Create((short)warehouseId, productId, 0, unitCost ?? 0, userId);
            await _uow.WarehouseStocks.AddAsync(stock, ct);
        }
        else
        {
            stock.IncreaseQuantity(quantity);
            if (unitCost.HasValue && unitCost.Value > 0)
                stock.UpdateAvgCost(quantity, unitCost.Value);
        }

        _logger.LogInformation("Stock Increased: Product {ProductId}, Warehouse {WarehouseId}, Qty +{Quantity}",
            productId, warehouseId, quantity);
        return Result.Success();
    }

    public async Task<Result> DecreaseStockAsync(
        int productId, int warehouseId, decimal quantity,
        decimal? unitCost = null, int? userId = null,
        CancellationToken ct = default)
    {
        var stock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
            ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, ct);

        if (stock == null)
        {
            _logger.LogWarning("Cannot decrease stock: record not found for Product {ProductId} in Warehouse {WarehouseId}",
                productId, warehouseId);
            return Result.Failure("سجل المخزون غير موجود");
        }

        stock.DecreaseQuantity(quantity);
        _logger.LogInformation("Stock Decreased: Product {ProductId}, Warehouse {WarehouseId}, Qty -{Quantity}",
            productId, warehouseId, quantity);
        return Result.Success();
    }

    #endregion

    #region Warehouse Stock Query

    public async Task<Result<IEnumerable<WarehouseStockDto>>> GetWarehouseStockAsync(
        int warehouseId, string? search, CancellationToken ct)
    {
        Expression<Func<WarehouseStock, bool>> predicate = s =>
            s.WarehouseId == warehouseId &&
            (string.IsNullOrWhiteSpace(search) || s.Product!.Name.Contains(search));

        var items = await _uow.WarehouseStocks.ToListAsync(predicate, null, ct, false, "Product");

        var dtos = items.Select(s => new WarehouseStockDto(
            s.WarehouseId,
            s.Warehouse?.Name,
            s.ProductId,
            s.Product?.Name ?? "غير معروف",
            null,
            s.Quantity,
            s.AvgCost
        ));

        return Result<IEnumerable<WarehouseStockDto>>.Success(dtos);
    }

    public async Task<Result<PagedResult<WarehouseStockDto>>> GetWarehouseStocksAsync(
        int? warehouseId, int? productId, int page, int pageSize, CancellationToken ct)
    {
        Expression<Func<WarehouseStock, bool>> predicate = s =>
            (!warehouseId.HasValue || s.WarehouseId == warehouseId.Value) &&
            (!productId.HasValue || s.ProductId == productId.Value);

        var totalItems = await _uow.WarehouseStocks.CountAsync(predicate, ct);
        var items = await _uow.WarehouseStocks.ToListAsync(
            predicate,
            q => q.OrderBy(s => s.Warehouse!.Name).ThenBy(s => s.Product!.Name)
                  .Skip((page - 1) * pageSize).Take(pageSize),
            ct, false, "Warehouse", "Product");

        var dtos = items.Select(s => new WarehouseStockDto(
            s.WarehouseId,
            s.Warehouse?.Name,
            s.ProductId,
            s.Product?.Name ?? "غير معروف",
            null,
            s.Quantity,
            s.AvgCost
        )).ToList();

        return Result<PagedResult<WarehouseStockDto>>.Success(
            PagedResult<WarehouseStockDto>.Create(dtos, totalItems, page, pageSize));
    }

    #endregion

    #region Inventory Transaction

    public async Task<Result<InventoryTransactionDto>> CreateTransactionAsync(
        CreateInventoryTransactionRequest request, int userId, CancellationToken ct)
    {
        if (request.Lines == null || request.Lines.Count == 0)
            return Result<InventoryTransactionDto>.Failure("يجب إضافة أصناف للمعاملة");

        var transactionType = (InventoryTransactionType)request.TransactionType;

        var tx = InventoryTransaction.Create(
            request.TransactionNo,
            transactionType,
            request.WarehouseId,
            request.TransactionDate,
            request.ReferenceType.HasValue ? (InventoryReferenceType?)request.ReferenceType.Value : null,
            request.ReferenceId,
            request.Notes,
            userId);

        foreach (var lineReq in request.Lines)
        {
            var line = InventoryTransactionLine.Create(
                tx.Id,
                lineReq.ProductId,
                lineReq.ProductUnitId,
                lineReq.Quantity,
                lineReq.UnitCost,
                lineReq.BatchId);
            tx.AddLine(line);
        }

        await _uow.InventoryTransactions.AddAsync(tx, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Inventory Transaction created: #{No}, Type: {Type}",
            tx.TransactionNo, tx.TransactionType);

        return await GetTransactionByIdAsync(tx.Id, ct);
    }

    public async Task<Result<InventoryTransactionDto>> PostTransactionAsync(
        int transactionId, int userId, CancellationToken ct)
    {
        var tx = await _uow.InventoryTransactions.FirstOrDefaultAsync(
            t => t.Id == transactionId, ct, "Lines");

        if (tx == null)
            return Result<InventoryTransactionDto>.Failure("المعاملة غير موجودة", ErrorCodes.NotFound);

        if (tx.Status != InvoiceStatus.Draft)
            return Result<InventoryTransactionDto>.Failure("فقط المعاملات المسودة يمكن ترحيلها");

        // Validate stock for outgoing transactions
        var allowNegativeStock = await _systemSettingsRepo.GetBoolAsync("AllowNegativeStock", false, ct);
        foreach (var line in tx.Lines)
        {
            if (IsOutgoingTransaction(tx.TransactionType))
            {
                var validation = await ValidateStockAsync(line.ProductId, tx.WarehouseId, line.Quantity, allowNegativeStock, ct);
                if (!validation.IsSuccess) return Result<InventoryTransactionDto>.Failure(validation.Error!);
            }
        }

        tx.Post();

        // Update stock levels
        foreach (var line in tx.Lines)
        {
            if (IsOutgoingTransaction(tx.TransactionType))
            {
                await DecreaseStockAsync(line.ProductId, tx.WarehouseId, line.Quantity, line.UnitCost, userId, ct);
            }
            else
            {
                await IncreaseStockAsync(line.ProductId, tx.WarehouseId, line.Quantity, line.UnitCost, userId, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Inventory Transaction Posted: #{No}", tx.TransactionNo);
        return await GetTransactionByIdAsync(tx.Id, ct);
    }

    public async Task<Result<InventoryTransactionDto>> CancelTransactionAsync(
        int transactionId, int userId, CancellationToken ct)
    {
        var tx = await _uow.InventoryTransactions.FirstOrDefaultAsync(
            t => t.Id == transactionId, ct, "Lines");

        if (tx == null)
            return Result<InventoryTransactionDto>.Failure("المعاملة غير موجودة", ErrorCodes.NotFound);

        if (tx.Status == InvoiceStatus.Cancelled)
            return Result<InventoryTransactionDto>.Failure("المعاملة ملغاة بالفعل");

        bool wasPosted = tx.Status == InvoiceStatus.Posted;
        tx.Cancel();

        // Reverse stock if it was posted
        if (wasPosted)
        {
            foreach (var line in tx.Lines)
            {
                if (IsOutgoingTransaction(tx.TransactionType))
                {
                    await IncreaseStockAsync(line.ProductId, tx.WarehouseId, line.Quantity, line.UnitCost, userId, ct);
                }
                else
                {
                    await DecreaseStockAsync(line.ProductId, tx.WarehouseId, line.Quantity, line.UnitCost, userId, ct);
                }
            }
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Inventory Transaction Cancelled: #{No}", tx.TransactionNo);
        return await GetTransactionByIdAsync(tx.Id, ct);
    }

    public async Task<Result<InventoryTransactionDto>> GetTransactionByIdAsync(int id, CancellationToken ct)
    {
        var tx = await _uow.InventoryTransactions.FirstOrDefaultAsync(
            t => t.Id == id, ct, "Warehouse", "Lines");

        if (tx == null)
            return Result<InventoryTransactionDto>.Failure("المعاملة غير موجودة", ErrorCodes.NotFound);

        return Result<InventoryTransactionDto>.Success(MapTransactionToDto(tx));
    }

    public async Task<Result<PagedResult<InventoryTransactionDto>>> GetAllTransactionsAsync(
        int? warehouseId, int? transactionType, int page, int pageSize, CancellationToken ct)
    {
        Expression<Func<InventoryTransaction, bool>> predicate = t =>
            (!warehouseId.HasValue || t.WarehouseId == warehouseId.Value) &&
            (!transactionType.HasValue || (int)t.TransactionType == transactionType.Value);

        var totalItems = await _uow.InventoryTransactions.CountAsync(predicate, ct);
        var items = await _uow.InventoryTransactions.ToListAsync(
            predicate,
            q => q.OrderByDescending(t => t.TransactionDate).Skip((page - 1) * pageSize).Take(pageSize),
            ct, false, "Warehouse");

        var dtos = items.Select(MapTransactionToDto).ToList();

        return Result<PagedResult<InventoryTransactionDto>>.Success(
            PagedResult<InventoryTransactionDto>.Create(dtos, totalItems, page, pageSize));
    }

    #endregion

    #region Warehouse Transfer

    public async Task<Result<WarehouseTransferDto>> CreateTransferAsync(
        CreateWarehouseTransferRequest request, int userId, CancellationToken ct)
    {
        if (request.Lines == null || request.Lines.Count == 0)
            return Result<WarehouseTransferDto>.Failure("يجب إضافة أصناف للتحويل");

        var transfer = WarehouseTransfer.Create(
            request.TransferNo,
            request.SourceWarehouseId,
            request.DestinationWarehouseId,
            request.TransferDate,
            request.Notes,
            userId);

        foreach (var lineReq in request.Lines)
        {
            var line = WarehouseTransferLine.Create(
                transfer.Id,
                lineReq.ProductId,
                lineReq.ProductUnitId,
                lineReq.Quantity,
                lineReq.UnitCost,
                lineReq.BatchId);
            transfer.AddLine(line);
        }

        await _uow.WarehouseTransfers.AddAsync(transfer, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse Transfer Draft created: #{No}", transfer.TransferNo);
        return await GetTransferByIdAsync(transfer.Id, ct);
    }

    public async Task<Result<WarehouseTransferDto>> PostTransferAsync(
        int transferId, int userId, CancellationToken ct)
    {
        var transfer = await _uow.WarehouseTransfers.FirstOrDefaultAsync(
            t => t.Id == transferId, ct, "Lines");

        if (transfer == null)
            return Result<WarehouseTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound);

        if (transfer.Status != InvoiceStatus.Draft)
            return Result<WarehouseTransferDto>.Failure("فقط التحويلات المسودة يمكن ترحيلها");

        // Validate stock in source warehouse
        var allowNegativeStock = await _systemSettingsRepo.GetBoolAsync("AllowNegativeStock", false, ct);
        foreach (var line in transfer.Lines)
        {
            var validation = await ValidateStockAsync(line.ProductId, transfer.SourceWarehouseId, line.Quantity, allowNegativeStock, ct);
            if (!validation.IsSuccess) return Result<WarehouseTransferDto>.Failure(validation.Error!);
        }

        transfer.Post();

        // Decrease from source, increase to destination
        foreach (var line in transfer.Lines)
        {
            await DecreaseStockAsync(line.ProductId, transfer.SourceWarehouseId, line.Quantity, line.UnitCost, userId, ct);
            await IncreaseStockAsync(line.ProductId, transfer.DestinationWarehouseId, line.Quantity, line.UnitCost, userId, ct);
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse Transfer Posted: #{No}", transfer.TransferNo);
        return await GetTransferByIdAsync(transfer.Id, ct);
    }

    public async Task<Result<WarehouseTransferDto>> CancelTransferAsync(
        int transferId, int userId, CancellationToken ct)
    {
        var transfer = await _uow.WarehouseTransfers.FirstOrDefaultAsync(
            t => t.Id == transferId, ct, "Lines");

        if (transfer == null)
            return Result<WarehouseTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound);

        if (transfer.Status == InvoiceStatus.Cancelled)
            return Result<WarehouseTransferDto>.Failure("التحويل ملغي بالفعل");

        bool wasPosted = transfer.Status == InvoiceStatus.Posted;
        transfer.Cancel();

        if (wasPosted)
        {
            // Reverse: increase back to source, decrease from destination
            foreach (var line in transfer.Lines)
            {
                await IncreaseStockAsync(line.ProductId, transfer.SourceWarehouseId, line.Quantity, line.UnitCost, userId, ct);
                await DecreaseStockAsync(line.ProductId, transfer.DestinationWarehouseId, line.Quantity, line.UnitCost, userId, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse Transfer Cancelled: #{No}", transfer.TransferNo);
        return await GetTransferByIdAsync(transfer.Id, ct);
    }

    public async Task<Result<WarehouseTransferDto>> GetTransferByIdAsync(int id, CancellationToken ct)
    {
        var transfer = await _uow.WarehouseTransfers.FirstOrDefaultAsync(
            t => t.Id == id, ct, "SourceWarehouse", "DestinationWarehouse", "Lines");

        if (transfer == null)
            return Result<WarehouseTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound);

        return Result<WarehouseTransferDto>.Success(MapTransferToDto(transfer));
    }

    public async Task<Result<PagedResult<WarehouseTransferDto>>> GetAllTransfersAsync(
        int? sourceWarehouseId, int? destinationWarehouseId, int page, int pageSize, CancellationToken ct)
    {
        Expression<Func<WarehouseTransfer, bool>> predicate = t =>
            (!sourceWarehouseId.HasValue || t.SourceWarehouseId == sourceWarehouseId.Value) &&
            (!destinationWarehouseId.HasValue || t.DestinationWarehouseId == destinationWarehouseId.Value);

        var totalItems = await _uow.WarehouseTransfers.CountAsync(predicate, ct);
        var items = await _uow.WarehouseTransfers.ToListAsync(
            predicate,
            q => q.OrderByDescending(t => t.TransferDate).Skip((page - 1) * pageSize).Take(pageSize),
            ct, false, "SourceWarehouse", "DestinationWarehouse");

        var dtos = items.Select(MapTransferToDto).ToList();

        return Result<PagedResult<WarehouseTransferDto>>.Success(
            PagedResult<WarehouseTransferDto>.Create(dtos, totalItems, page, pageSize));
    }

    #endregion

    #region Movement History

    public async Task<Result<PagedResult<InventoryTransactionDto>>> GetMovementsAsync(
        int? productId, int? warehouseId, int? transactionType, int page, int pageSize, CancellationToken ct)
    {
        // Reuse GetAllTransactionsAsync with same signature
        return await GetAllTransactionsAsync(warehouseId, transactionType, page, pageSize, ct);
    }

    #endregion

    #region Mapping Helpers

    private static InventoryTransactionDto MapTransactionToDto(InventoryTransaction tx)
    {
        return new InventoryTransactionDto(
            tx.Id,
            tx.TransactionNo,
            tx.TransactionDate,
            (byte)tx.TransactionType,
            tx.WarehouseId,
            tx.Warehouse?.Name ?? "غير معروف",
            tx.ReferenceId,
            tx.ReferenceType.HasValue ? (byte?)tx.ReferenceType.Value : null,
            tx.Notes,
            (byte)tx.Status,
            tx.Lines.Select(l => new InventoryTransactionLineDto(
                l.Id,
                l.ProductId,
                null,
                l.ProductUnitId,
                null,
                l.Quantity,
                l.UnitCost,
                l.TotalCost,
                l.BatchId
            )).ToList()
        );
    }

    private static WarehouseTransferDto MapTransferToDto(WarehouseTransfer t)
    {
        return new WarehouseTransferDto(
            t.Id,
            t.TransferNo,
            t.SourceWarehouseId,
            t.SourceWarehouse?.Name ?? "غير معروف",
            t.DestinationWarehouseId,
            t.DestinationWarehouse?.Name ?? "غير معروف",
            t.TransferDate,
            t.Notes,
            (byte)t.Status,
            t.Lines.Select(l => new WarehouseTransferLineDto(
                l.Id,
                l.ProductId,
                null,
                l.ProductUnitId,
                null,
                l.Quantity,
                l.UnitCost,
                l.TotalCost,
                l.BatchId
            )).ToList()
        );
    }

    private static bool IsOutgoingTransaction(InventoryTransactionType type) => type switch
    {
        InventoryTransactionType.Sale => true,
        InventoryTransactionType.PurchaseReturn => true,
        InventoryTransactionType.TransferOut => true,
        InventoryTransactionType.Damage => true,
        InventoryTransactionType.Adjustment => true,
        InventoryTransactionType.InternalIssue => true,
        _ => false
    };

    #endregion
}
