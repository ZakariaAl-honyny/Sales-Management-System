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

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(IUnitOfWork uow, IDocumentSequenceService sequenceService, ILogger<InventoryService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    #region Foundational Methods (T004)

    public async Task<Result<decimal>> GetStockAsync(int productId, int warehouseId, CancellationToken ct)
    {
        var stock = await _uow.WarehouseStocks.Query()
            .FirstOrDefaultAsync(ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, ct);

        if (stock == null)
            return Result<decimal>.Failure("ط¸â€‍ط¸â€¦ ط¸ظ¹ط·ع¾ط¸â€¦ ط·آ§ط¸â€‍ط·آ¹ط·آ«ط¸ث†ط·آ± ط·آ¹ط¸â€‍ط¸â€° ط·آ³ط·آ¬ط¸â€‍ ط¸â€¦ط·آ®ط·آ²ط¸ث†ط¸â€  ط¸â€‍ط¸â€،ط·آ°ط·آ§ ط·آ§ط¸â€‍ط¸â€¦ط¸â€ ط·ع¾ط·آ¬ ط¸ظ¾ط¸ظ¹ ط¸â€،ط·آ°ط·آ§ ط·آ§ط¸â€‍ط¸â€¦ط·آ³ط·ع¾ط¸ث†ط·آ¯ط·آ¹");

        return Result<decimal>.Success(stock.Quantity);
    }

    public async Task<Result> ValidateStockAsync(int productId, int warehouseId, decimal requiredQty, CancellationToken ct)
    {
        var stockResult = await GetStockAsync(productId, warehouseId, ct);
        if (!stockResult.IsSuccess) return stockResult;

        if (stockResult.Value < requiredQty)
            return Result.Failure($"ط·آ§ط¸â€‍ط¸â€¦ط·آ®ط·آ²ط¸ث†ط¸â€  ط·ط›ط¸ظ¹ط·آ± ط¸ئ’ط·آ§ط¸ظ¾ط¸ع† ط¸â€‍ط¸â€‍ط¸â€¦ط¸â€ ط·ع¾ط·آ¬ {productId}: ط·آ§ط¸â€‍ط¸â€¦ط·ع¾ط¸ث†ط¸ظ¾ط·آ± {stockResult.Value}ط·إ’ ط·آ§ط¸â€‍ط¸â€¦ط·آ·ط¸â€‍ط¸ث†ط·آ¨ {requiredQty}");

        return Result.Success();
    }

    public async Task<Result> IncreaseStockAsync(int productId, int warehouseId, decimal quantity, MovementType movementType, string referenceType, int referenceId, decimal? unitCost, int? userId, CancellationToken ct)
    {
        var stock = await _uow.WarehouseStocks.Query()
            .FirstOrDefaultAsync(ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, ct);

        if (stock == null)
        {
            stock = WarehouseStock.Create(warehouseId, productId, 0);
            await _uow.WarehouseStocks.AddAsync(stock, ct);
        }

        decimal qtyBefore = stock.Quantity;
        stock.IncreaseQuantity(quantity);
        decimal qtyAfter = stock.Quantity;

        var movement = InventoryMovement.Create(
            productId,
            warehouseId,
            movementType,
            quantity,
            qtyBefore,
            qtyAfter,
            referenceType,
            referenceId,
            unitCost,
            null,
            userId
        );

        await _uow.InventoryMovements.AddAsync(movement, ct);
        _logger.LogInformation("Stock Increased: Product {ProductId}, Warehouse {WarehouseId}, Qty +{Quantity}, Ref {RefType}:{RefId}", productId, warehouseId, quantity, referenceType, referenceId);
        
        return Result.Success();
    }

    public async Task<Result> DecreaseStockAsync(int productId, int warehouseId, decimal quantity, MovementType movementType, string referenceType, int referenceId, decimal? unitCost, int? userId, CancellationToken ct)
    {
        var stock = await _uow.WarehouseStocks.Query()
            .FirstOrDefaultAsync(ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, ct);

        if (stock == null)
            return Result.Failure("ط·آ³ط·آ¬ط¸â€‍ ط·آ§ط¸â€‍ط¸â€¦ط·آ®ط·آ²ط¸ث†ط¸â€  ط·ط›ط¸ظ¹ط·آ± ط¸â€¦ط¸ث†ط·آ¬ط¸ث†ط·آ¯");

        decimal qtyBefore = stock.Quantity;
        stock.DecreaseQuantity(quantity);
        decimal qtyAfter = stock.Quantity;

        var movement = InventoryMovement.Create(
            productId,
            warehouseId,
            movementType,
            -quantity,
            qtyBefore,
            qtyAfter,
            referenceType,
            referenceId,
            unitCost,
            null,
            userId
        );

        await _uow.InventoryMovements.AddAsync(movement, ct);
        _logger.LogInformation("Stock Decreased: Product {ProductId}, Warehouse {WarehouseId}, Qty -{Quantity}, Ref {RefType}:{RefId}", productId, warehouseId, quantity, referenceType, referenceId);

        return Result.Success();
    }

    #endregion

    #region Stock Transfer and Querying

    public async Task<Result<StockTransferDto>> GetTransferByIdAsync(int id, CancellationToken ct)
    {
        var transfer = await _uow.StockTransfers.Query()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .Include(t => t.Items)
                .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (transfer == null)
            return Result<StockTransferDto>.Failure("ط·آ§ط¸â€‍ط·ع¾ط·آ­ط¸ث†ط¸ظ¹ط¸â€‍ ط·ط›ط¸ظ¹ط·آ± ط¸â€¦ط¸ث†ط·آ¬ط¸ث†ط·آ¯", ErrorCodes.NotFound);

        return Result<StockTransferDto>.Success(MapToDto(transfer));
    }

    public async Task<Result<PagedResult<StockTransferDto>>> GetAllTransfersAsync(int? fromWarehouseId, int? toWarehouseId, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.StockTransfers.Query()
            .Include(t => t.FromWarehouse)
            .Include(t => t.ToWarehouse)
            .AsQueryable();

        if (fromWarehouseId.HasValue) query = query.Where(t => t.FromWarehouseId == fromWarehouseId.Value);
        if (toWarehouseId.HasValue) query = query.Where(t => t.ToWarehouseId == toWarehouseId.Value);

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.TransferDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<StockTransferDto>>.Success(PagedResult<StockTransferDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<StockTransferDto>> CreateTransferAsync(CreateStockTransferRequest request, int userId, CancellationToken ct)
    {
        if (request.FromWarehouseId == request.ToWarehouseId)
            return Result<StockTransferDto>.Failure("ط¸â€‍ط·آ§ ط¸ظ¹ط¸â€¦ط¸ئ’ط¸â€  ط·آ§ط¸â€‍ط·ع¾ط·آ­ط¸ث†ط¸ظ¹ط¸â€‍ ط¸â€‍ط¸â€ ط¸ظ¾ط·آ³ ط·آ§ط¸â€‍ط¸â€¦ط·آ®ط·آ²ط¸â€ ");

        if (request.Items.Count == 0)
            return Result<StockTransferDto>.Failure("ط¸ظ¹ط·آ¬ط·آ¨ ط·آ¥ط·آ¶ط·آ§ط¸ظ¾ط·آ© ط·آ£ط·آµط¸â€ ط·آ§ط¸ظ¾ ط¸â€‍ط¸â€‍ط·ع¾ط·آ­ط¸ث†ط¸ظ¹ط¸â€‍");

        // 1. Validation BEFORE transaction
        foreach (var item in request.Items)
        {
            var validation = await ValidateStockAsync(item.ProductId, request.FromWarehouseId, item.Quantity, ct);
            if (!validation.IsSuccess) return Result<StockTransferDto>.Failure(validation.Error!);
        }

        // 2. Start Transaction
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            var transferNoResult = await _sequenceService.GetNextNumberAsync("TRF", ct);
            if (!transferNoResult.IsSuccess) return Result<StockTransferDto>.Failure(transferNoResult.Error!);

            var transfer = StockTransfer.Create(
                transferNoResult.Value!,
                request.FromWarehouseId,
                request.ToWarehouseId,
                request.Notes,
                request.TransferDate
            );
            transfer.SetCreatedBy(userId);

            foreach (var item in request.Items)
            {
                transfer.AddItem(item.ProductId, item.Quantity, item.Notes);
            }

            await _uow.StockTransfers.AddAsync(transfer, ct);
            await _uow.SaveChangesAsync(ct);

            // 3. Update Stocks & Record Movements
            foreach (var item in transfer.Items)
            {
                await DecreaseStockAsync(item.ProductId, transfer.FromWarehouseId, item.Quantity, MovementType.TransferOut, "StockTransfer", transfer.Id, null, userId, ct);
                await IncreaseStockAsync(item.ProductId, transfer.ToWarehouseId, item.Quantity, MovementType.TransferIn, "StockTransfer", transfer.Id, null, userId, ct);
            }

            await _uow.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Stock Transfer created: {TransferNo}", transfer.TransferNo);

            return await GetTransferByIdAsync(transfer.Id, ct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Error creating stock transfer");
            return Result<StockTransferDto>.Failure("ط·آ­ط·آ¯ط·آ« ط·آ®ط·آ·ط·آ£ ط·آ£ط·آ«ط¸â€ ط·آ§ط·طŒ ط·آ­ط¸ظ¾ط·آ¸ ط·آ§ط¸â€‍ط·ع¾ط·آ­ط¸ث†ط¸ظ¹ط¸â€‍");
        }
    }

    public async Task<Result<IEnumerable<WarehouseStockDto>>> GetWarehouseStockAsync(int warehouseId, string? search, CancellationToken ct)
    {
        var query = _uow.WarehouseStocks.Query()
            .Include(s => s.Product)
                .ThenInclude(p => p!.Unit)
            .Where(s => s.WarehouseId == warehouseId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Product!.Name.Contains(search) || (s.Product.Code != null && s.Product.Code.Contains(search)));
        }

        var items = await query.ToListAsync(ct);
        var dtos = items.Select(s => new WarehouseStockDto(
            s.WarehouseId,
            null,
            s.ProductId,
            s.Product?.Name ?? "Unknown",
            s.Product?.Unit?.Name,
            s.Quantity,
            s.ReorderLevel
        ));

        return Result<IEnumerable<WarehouseStockDto>>.Success(dtos);
    }
    
    public async Task<Result<PagedResult<WarehouseStockDto>>> GetWarehouseStocksAsync(int? warehouseId, int? productId, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.WarehouseStocks.Query()
            .Include(s => s.Product)
                .ThenInclude(p => p!.Unit)
            .Include(s => s.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue) query = query.Where(s => s.WarehouseId == warehouseId.Value);
        if (productId.HasValue) query = query.Where(s => s.ProductId == productId.Value);

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.Warehouse!.Name)
            .ThenBy(s => s.Product!.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(s => new WarehouseStockDto(
            s.WarehouseId,
            s.Warehouse?.Name,
            s.ProductId,
            s.Product?.Name ?? "Unknown",
            s.Product?.Unit?.Name,
            s.Quantity,
            s.ReorderLevel
        )).ToList();

        return Result<PagedResult<WarehouseStockDto>>.Success(PagedResult<WarehouseStockDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<PagedResult<InventoryMovementDto>>> GetMovementsAsync(int? productId, int? warehouseId, int? movementType, int page, int pageSize, CancellationToken ct)
    {
        var query = _uow.InventoryMovements.Query()
            .Include(m => m.Product)
            .Include(m => m.Warehouse)
            .AsQueryable();

        if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
        if (warehouseId.HasValue) query = query.Where(m => m.WarehouseId == warehouseId.Value);
        if (movementType.HasValue) query = query.Where(m => (int)m.MovementType == movementType.Value);

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.MovementDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(m => new InventoryMovementDto(
            m.Id,
            m.ProductId,
            m.Product?.Name ?? "Unknown",
            m.WarehouseId,
            m.Warehouse?.Name ?? "Unknown",
            (byte)m.MovementType,
            m.QuantityChange,
            m.QuantityBefore,
            m.QuantityAfter,
            m.ReferenceType,
            m.ReferenceId,
            m.MovementDate,
            m.Notes
        )).ToList();

        return Result<PagedResult<InventoryMovementDto>>.Success(PagedResult<InventoryMovementDto>.Create(dtos, totalItems, page, pageSize));
    }

    #endregion

        private static StockTransferDto MapToDto(StockTransfer t)
    {
        return new StockTransferDto(
            t.Id,
            t.TransferNo,
            t.FromWarehouseId,
            t.FromWarehouse?.Name ?? "Unknown",
            t.ToWarehouseId,
            t.ToWarehouse?.Name ?? "Unknown",
            t.TransferDate,
            t.Notes,
            (byte)t.Status,
            t.Items.Select(it => new StockTransferItemDto(
                it.StockTransferItemId,
                it.ProductId,
                it.Product?.Code,
                it.Product?.Name ?? "Unknown",
                it.Quantity,
                it.Notes
            )).ToList()
        );
    }
}


