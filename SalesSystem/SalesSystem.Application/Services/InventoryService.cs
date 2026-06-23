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
/// Inventory management service using the v4.10+ schema:
/// InventoryTransactions (immutable, no Status lifecycle) and WarehouseTransfers (Draft/Posted/Cancelled).
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
            stock = WarehouseStock.Create((short)warehouseId, productId, 0, userId);
            await _uow.WarehouseStocks.AddAsync(stock, ct);
        }
        else
        {
            stock.IncreaseQuantity(quantity);
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
            0m // AvgCost no longer stored on WarehouseStock
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
            0m
        )).ToList();

        return Result<PagedResult<WarehouseStockDto>>.Success(
            PagedResult<WarehouseStockDto>.Create(dtos, totalItems, page, pageSize));
    }

    #endregion

    #region Inventory Transaction (immutable — written once, no Status)

    public async Task<Result<InventoryTransactionDto>> CreateTransactionAsync(
        CreateInventoryTransactionRequest request, int userId, CancellationToken ct)
    {
        if (request.Lines == null || request.Lines.Count == 0)
            return Result<InventoryTransactionDto>.Failure("يجب إضافة أصناف للمعاملة");

        var movementType = (InventoryTransactionType)request.MovementType;

        // Auto-generate TransactionNo if not provided
        var transactionNo = request.TransactionNo;
        if (string.IsNullOrWhiteSpace(transactionNo))
        {
            var seqResult = await _sequenceService.GetNextIntAsync("InventoryTransaction", ct);
            if (!seqResult.IsSuccess)
                return Result<InventoryTransactionDto>.Failure("فشل في توليد رقم المعاملة");
            transactionNo = seqResult.Value.ToString("D6");
        }

        var tx = InventoryTransaction.Create(
            transactionNo,
            movementType,
            request.WarehouseId,
            request.ReferenceType.HasValue ? (InventoryReferenceType?)request.ReferenceType.Value : null,
            request.ReferenceId,
            request.Notes,
            userId);

        // Process lines
        foreach (var lineReq in request.Lines)
        {
            var line = InventoryTransactionLine.Create(
                tx.Id,
                lineReq.ProductUnitId,
                lineReq.Quantity,
                lineReq.UnitCost,
                lineReq.BatchNo,
                lineReq.ExpiryDate,
                lineReq.WarehouseId);
            tx.AddLine(line);

            // Update stock immediately (transaction is immutable — no Draft/Posted lifecycle)
            var warehouseId = lineReq.WarehouseId ?? request.WarehouseId;
            var productUnit = await _uow.ProductUnits.GetByIdAsync(lineReq.ProductUnitId, ct);
            if (productUnit == null)
                return Result<InventoryTransactionDto>.Failure($"وحدة المنتج {lineReq.ProductUnitId} غير موجودة");

            if (IsOutgoingMovement(movementType))
            {
                await DecreaseStockAsync(productUnit.ProductId, warehouseId, lineReq.Quantity, lineReq.UnitCost, userId, ct);
            }
            else
            {
                await IncreaseStockAsync(productUnit.ProductId, warehouseId, lineReq.Quantity, lineReq.UnitCost, userId, ct);
            }
        }

        await _uow.InventoryTransactions.AddAsync(tx, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Inventory Transaction created: #{No}, Type: {Type}",
            tx.TransactionNo, tx.MovementType);

        return await GetTransactionByIdAsync(tx.Id, ct);
    }

    /// <summary>
    /// InventoryTransactions are immutable — creation IS posting. This is a no-op returning the existing transaction.
    /// </summary>
    public async Task<Result<InventoryTransactionDto>> PostTransactionAsync(
        int transactionId, int userId, CancellationToken ct)
    {
        return await GetTransactionByIdAsync(transactionId, ct);
    }

    /// <summary>
    /// InventoryTransactions are immutable and cannot be cancelled.
    /// </summary>
    public async Task<Result<InventoryTransactionDto>> CancelTransactionAsync(
        int transactionId, int userId, CancellationToken ct)
    {
        return Result<InventoryTransactionDto>.Failure("المعاملة غير قابلة للإلغاء — المعاملات غير قابلة للتعديل");
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
        int? warehouseId, int? movementType, int page, int pageSize, CancellationToken ct)
    {
        Expression<Func<InventoryTransaction, bool>> predicate = t =>
            (!warehouseId.HasValue || t.WarehouseId == warehouseId.Value) &&
            (!movementType.HasValue || (int)t.MovementType == movementType.Value);

        var totalItems = await _uow.InventoryTransactions.CountAsync(predicate, ct);
        var items = await _uow.InventoryTransactions.ToListAsync(
            predicate,
            q => q.OrderByDescending(t => t.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize),
            ct, false, "Warehouse");

        var dtos = items.Select(MapTransactionToDto).ToList();

        return Result<PagedResult<InventoryTransactionDto>>.Success(
            PagedResult<InventoryTransactionDto>.Create(dtos, totalItems, page, pageSize));
    }

    #endregion

    #region Warehouse Transfer (has Draft/Posted/Cancelled lifecycle)

    public async Task<Result<WarehouseTransferDto>> CreateTransferAsync(
        CreateWarehouseTransferRequest request, int userId, CancellationToken ct)
    {
        if (request.Lines == null || request.Lines.Count == 0)
            return Result<WarehouseTransferDto>.Failure("يجب إضافة أصناف للتحويل");

        // Auto-generate TransferNo if not provided
        var transferNo = request.TransferNo;
        if (string.IsNullOrWhiteSpace(transferNo))
        {
            var seqResult = await _sequenceService.GetNextIntAsync("WarehouseTransfer", ct);
            if (!seqResult.IsSuccess)
                return Result<WarehouseTransferDto>.Failure("فشل في توليد رقم التحويل");
            transferNo = seqResult.Value.ToString("D6");
        }

        var transfer = WarehouseTransfer.Create(
            transferNo,
            request.SourceWarehouseId,
            request.DestinationWarehouseId,
            request.Notes,
            userId);

        foreach (var lineReq in request.Lines)
        {
            var line = WarehouseTransferLine.Create(
                transfer.Id,
                lineReq.ProductUnitId,
                lineReq.Quantity,
                lineReq.BatchNo);
            transfer.AddLine(line);
        }

        return await _uow.ExecuteTransactionAsync<Result<WarehouseTransferDto>>(async () =>
        {
            await _uow.WarehouseTransfers.AddAsync(transfer, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Warehouse Transfer Draft created: #{No}", transfer.TransferNo);
            return await GetTransferByIdAsync(transfer.Id, ct);
        }, ct);
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
            var productUnit = await _uow.ProductUnits.GetByIdAsync(line.ProductUnitId, ct);
            if (productUnit == null)
                return Result<WarehouseTransferDto>.Failure($"وحدة المنتج {line.ProductUnitId} غير موجودة");

            var validation = await ValidateStockAsync(productUnit.ProductId, transfer.SourceWarehouseId, line.Quantity, allowNegativeStock, ct);
            if (!validation.IsSuccess) return Result<WarehouseTransferDto>.Failure(validation.Error!);
        }

        return await _uow.ExecuteTransactionAsync<Result<WarehouseTransferDto>>(async () =>
        {
            transfer.Post();

            // Decrease from source, increase to destination
            foreach (var line in transfer.Lines)
            {
                var productUnit = await _uow.ProductUnits.GetByIdAsync(line.ProductUnitId, ct);
                if (productUnit == null) continue;

                await DecreaseStockAsync(productUnit.ProductId, transfer.SourceWarehouseId, line.Quantity, null, userId, ct);
                await IncreaseStockAsync(productUnit.ProductId, transfer.DestinationWarehouseId, line.Quantity, null, userId, ct);
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Warehouse Transfer Posted: #{No}", transfer.TransferNo);
            return await GetTransferByIdAsync(transfer.Id, ct);
        }, ct);
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

        var wasPosted = transfer.Status == InvoiceStatus.Posted;

        return await _uow.ExecuteTransactionAsync<Result<WarehouseTransferDto>>(async () =>
        {
            transfer.Cancel();

            if (wasPosted)
            {
                // Reverse: increase back to source, decrease from destination
                foreach (var line in transfer.Lines)
                {
                    var productUnit = await _uow.ProductUnits.GetByIdAsync(line.ProductUnitId, ct);
                    if (productUnit == null) continue;

                    await IncreaseStockAsync(productUnit.ProductId, transfer.SourceWarehouseId, line.Quantity, null, userId, ct);
                    await DecreaseStockAsync(productUnit.ProductId, transfer.DestinationWarehouseId, line.Quantity, null, userId, ct);
                }
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Warehouse Transfer Cancelled: #{No}", transfer.TransferNo);
            return await GetTransferByIdAsync(transfer.Id, ct);
        }, ct);
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
            q => q.OrderByDescending(t => t.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize),
            ct, false, "SourceWarehouse", "DestinationWarehouse");

        var dtos = items.Select(MapTransferToDto).ToList();

        return Result<PagedResult<WarehouseTransferDto>>.Success(
            PagedResult<WarehouseTransferDto>.Create(dtos, totalItems, page, pageSize));
    }

    #endregion

    #region Movement History

    public async Task<Result<PagedResult<InventoryTransactionDto>>> GetMovementsAsync(
        int? productId, int? warehouseId, int? movementType, int page, int pageSize, CancellationToken ct)
    {
        // Reuse GetAllTransactionsAsync
        return await GetAllTransactionsAsync(warehouseId, movementType, page, pageSize, ct);
    }

    #endregion

    #region Mapping Helpers

    private static InventoryTransactionDto MapTransactionToDto(InventoryTransaction tx)
    {
        return new InventoryTransactionDto(
            tx.Id,
            tx.TransactionNo,
            (byte)tx.MovementType,
            tx.WarehouseId,
            tx.Warehouse?.Name ?? "غير معروف",
            tx.ReferenceId,
            tx.ReferenceType.HasValue ? (byte?)tx.ReferenceType.Value : null,
            tx.Notes,
            tx.CreatedAt,
            tx.CreatedByUserId,
            tx.Lines.Select(l => new InventoryTransactionLineDto(
                l.Id,
                l.InventoryTransactionId,
                l.ProductUnitId,
                null,
                l.Quantity,
                l.UnitCost,
                l.BatchNo,
                l.ExpiryDate,
                l.WarehouseId
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
            t.Notes,
            (byte)t.Status,
            t.CreatedAt,
            t.CreatedByUserId,
            t.Lines.Select(l => new WarehouseTransferLineDto(
                l.Id,
                l.WarehouseTransferId,
                l.ProductUnitId,
                null,
                l.Quantity,
                l.BatchNo
            )).ToList()
        );
    }

    private static bool IsOutgoingMovement(InventoryTransactionType type) => type switch
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
