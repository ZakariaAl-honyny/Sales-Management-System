using System.Linq.Expressions;
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
        {
            return Result.Success();
        }

        var stockResult = await GetStockAsync(productId, warehouseId, ct);
        if (!stockResult.IsSuccess) return stockResult;

        if (stockResult.Value < requiredQty)
        {
            _logger.LogWarning("Insufficient stock for Product {ProductId} in Warehouse {WarehouseId}. Available: {Available}, Required: {Required}", productId, warehouseId, stockResult.Value, requiredQty);
            return Result.Failure($"المخزون غير كافٍ للمنتج {productId}: المتوفر {stockResult.Value}، المطلوب {requiredQty}");
        }

        return Result.Success();
    }

    public async Task<Result> IncreaseStockAsync(int productId, int warehouseId, decimal quantity, MovementType movementType, string referenceType, int referenceId, decimal? unitCost, int? userId, CancellationToken ct)
    {
        var stock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
            ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, ct);

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
        var stock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
            ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, ct);

        if (stock == null)
        {
            _logger.LogWarning("Cannot decrease stock: record not found for Product {ProductId} in Warehouse {WarehouseId}", productId, warehouseId);
            return Result.Failure("سجل المخزون غير موجود");
        }

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
        var transfer = await _uow.StockTransfers.FirstOrDefaultAsync(
            t => t.Id == id, ct, "FromWarehouse", "ToWarehouse", "Items.Product");

        if (transfer == null)
            return Result<StockTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound);

        return Result<StockTransferDto>.Success(MapToDto(transfer));
    }

    public async Task<Result<PagedResult<StockTransferDto>>> GetAllTransfersAsync(int? fromWarehouseId, int? toWarehouseId, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        Expression<Func<StockTransfer, bool>> predicate = t =>
            (!fromWarehouseId.HasValue || t.FromWarehouseId == fromWarehouseId.Value) &&
            (!toWarehouseId.HasValue || t.ToWarehouseId == toWarehouseId.Value) &&
            (includeInactive || t.Status != InvoiceStatus.Cancelled);

        var totalItems = await _uow.StockTransfers.CountAsync(predicate, ct);
        var items = await _uow.StockTransfers.ToListAsync(
            predicate,
            q => q.OrderByDescending(t => t.TransferDate).Skip((page - 1) * pageSize).Take(pageSize),
            ct,
            false,
            "FromWarehouse", "ToWarehouse");

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<StockTransferDto>>.Success(PagedResult<StockTransferDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<StockTransferDto>> CreateTransferAsync(CreateStockTransferRequest request, int userId, CancellationToken ct)
    {
        if (request.FromWarehouseId == request.ToWarehouseId)
        {
            _logger.LogWarning("Stock transfer failed: Same source and destination warehouse {WarehouseId}", request.FromWarehouseId);
            return Result<StockTransferDto>.Failure("لا يمكن التحويل لنفس المخزن");
        }

        if (request.Items.Count == 0)
            return Result<StockTransferDto>.Failure("يجب إضافة أصناف للتحويل");

        // 2. Save Draft
        return await _uow.ExecuteAsync(async () =>
        {
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
                    transfer.AddItem(item.ProductId, item.Quantity, (SaleMode)item.Mode, item.Notes);
                }

                await _uow.StockTransfers.AddAsync(transfer, ct);
                await _uow.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                _logger.LogInformation("Stock Transfer Draft created: {TransferNo}", transfer.TransferNo);

                return await GetTransferByIdAsync(transfer.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain rule violation while creating stock transfer");
                return Result<StockTransferDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error creating stock transfer");
                return Result<StockTransferDto>.Failure("حدث خطأ أثناء حفظ التحويل");
            }
        }, ct);
    }

    public async Task<Result<IEnumerable<WarehouseStockDto>>> GetWarehouseStockAsync(int warehouseId, string? search, CancellationToken ct)
    {
        Expression<Func<WarehouseStock, bool>> predicate = s =>
            s.WarehouseId == warehouseId &&
            (string.IsNullOrWhiteSpace(search) || s.Product!.Name.Contains(search));

        // Phase 25: Product.Unit removed. Unit info is accessed via ProductUnits collection.
        // Product.Unit navigation property no longer exists; unit display name sourced from base unit.
        var items = await _uow.WarehouseStocks.ToListAsync(
            predicate, null, ct, false);

        var dtos = items.Select(s => new WarehouseStockDto(
            s.WarehouseId,
            null,
            s.ProductId,
            s.Product?.Name ?? "غير معروف",
            s.Product?.Units?.FirstOrDefault(u => u.IsBaseUnit)?.Unit?.Name,
            s.Quantity,
            s.ReorderLevel
        ));

        return Result<IEnumerable<WarehouseStockDto>>.Success(dtos);
    }

    public async Task<Result<PagedResult<WarehouseStockDto>>> GetWarehouseStocksAsync(int? warehouseId, int? productId, int page, int pageSize, CancellationToken ct)
    {
        Expression<Func<WarehouseStock, bool>> predicate = s =>
            (!warehouseId.HasValue || s.WarehouseId == warehouseId.Value) &&
            (!productId.HasValue || s.ProductId == productId.Value);

        var totalItems = await _uow.WarehouseStocks.CountAsync(predicate, ct);
        var items = await _uow.WarehouseStocks.ToListAsync(
            predicate,
            q => q.OrderBy(s => s.Warehouse!.Name).ThenBy(s => s.Product!.Name)
                  .Skip((page - 1) * pageSize).Take(pageSize),
            ct,
            false,
            "Product.Unit", "Warehouse");

        var dtos = items.Select(s => new WarehouseStockDto(
            s.WarehouseId,
            s.Warehouse?.Name,
            s.ProductId,
            s.Product?.Name ?? "غير معروف",
            s.Product?.Units?.FirstOrDefault(u => u.IsBaseUnit)?.Unit?.Name,
            s.Quantity,
            s.ReorderLevel
        )).ToList();

        return Result<PagedResult<WarehouseStockDto>>.Success(PagedResult<WarehouseStockDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<PagedResult<InventoryMovementDto>>> GetMovementsAsync(int? productId, int? warehouseId, int? movementType, int page, int pageSize, CancellationToken ct)
    {
        Expression<Func<InventoryMovement, bool>> predicate = m =>
            (!productId.HasValue || m.ProductId == productId.Value) &&
            (!warehouseId.HasValue || m.WarehouseId == warehouseId.Value) &&
            (!movementType.HasValue || (int)m.MovementType == movementType.Value);

        var totalItems = await _uow.InventoryMovements.CountAsync(predicate, ct);
        var items = await _uow.InventoryMovements.ToListAsync(
            predicate,
            q => q.OrderByDescending(m => m.MovementDate).Skip((page - 1) * pageSize).Take(pageSize),
            ct,
            false,
            "Product", "Warehouse");

        var dtos = items.Select(m => new InventoryMovementDto(
            m.Id,
            m.ProductId,
            m.Product?.Name ?? "غير معروف",
            m.WarehouseId,
            m.Warehouse?.Name ?? "غير معروف",
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

    public async Task<Result<StockTransferDto>> UpdateTransferAsync(int id, UpdateStockTransferRequest request, int userId, CancellationToken ct)
    {
        var transfer = await _uow.StockTransfers.FirstOrDefaultAsync(
            t => t.Id == id, ct, "Items.Product");

        if (transfer == null)
            return Result<StockTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound);

        if (transfer.Status != InvoiceStatus.Draft)
            return Result<StockTransferDto>.Failure("لا يمكن تعديل تحويل مرحل أو ملغى");

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                // Simple update logic - clear and re-add items for Drafts
                _uow.StockTransferItems.DeleteRange(transfer.Items);
                transfer.Items.Clear();

                foreach (var item in request.Items)
                {
                    transfer.AddItem(item.ProductId, item.Quantity, (SaleMode)item.Mode, item.Notes);
                }

                transfer.UpdateNotes(request.Notes);

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return await GetTransferByIdAsync(transfer.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain rule violation while updating stock transfer {Id}", id);
                return Result<StockTransferDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error updating stock transfer {Id}", id);
                return Result<StockTransferDto>.Failure("حدث خطأ أثناء تحديث التحويل");
            }
        }, ct);
    }

    public async Task<Result<StockTransferDto>> PostTransferAsync(int id, int userId, CancellationToken ct)
    {
        var transfer = await _uow.StockTransfers.FirstOrDefaultAsync(
            t => t.Id == id, ct, "Items.Product");

        if (transfer == null)
            return Result<StockTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound);

        if (transfer.Status != InvoiceStatus.Draft)
            return Result<StockTransferDto>.Failure("يمكن فقط ترحيل الحوالات المسودة");

        var settings = await _uow.StoreSettings.FirstOrDefaultAsync(s => true, ct);
        bool allowNegativeStock = settings?.AllowNegativeStock ?? false;

        // Phase 25: GetRetailQuantityEquivalent removed. Quantity is in base units.
        // 1. Validate Stock
        foreach (var item in transfer.Items)
        {
            var validation = await ValidateStockAsync(item.ProductId, transfer.FromWarehouseId, item.Quantity, allowNegativeStock, ct);
            if (!validation.IsSuccess) return Result<StockTransferDto>.Failure(validation.Error!);
        }

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                transfer.Post();

                foreach (var item in transfer.Items)
                {
                    // Phase 25: GetRetailQuantityEquivalent removed.
                    await DecreaseStockAsync(item.ProductId, transfer.FromWarehouseId, item.Quantity, MovementType.TransferOut, "StockTransfer", transfer.Id, null, userId, ct);
                    await IncreaseStockAsync(item.ProductId, transfer.ToWarehouseId, item.Quantity, MovementType.TransferIn, "StockTransfer", transfer.Id, null, userId, ct);
                }

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Stock Transfer Posted: {TransferNo}", transfer.TransferNo);
                return await GetTransferByIdAsync(transfer.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain rule violation while posting stock transfer {Id}", id);
                return Result<StockTransferDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error posting stock transfer {Id}", id);
                return Result<StockTransferDto>.Failure("حدث خطأ أثناء ترحيل التحويل");
            }
        }, ct);
    }

    public async Task<Result<StockTransferDto>> CancelTransferAsync(int id, int userId, CancellationToken ct)
    {
        var transfer = await _uow.StockTransfers.FirstOrDefaultAsync(
            t => t.Id == id, ct, "Items.Product");

        if (transfer == null)
            return Result<StockTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound);

        if (transfer.Status == InvoiceStatus.Cancelled)
            return Result<StockTransferDto>.Failure("التحويل ملغى بالفعل");

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                bool wasPosted = transfer.Status == InvoiceStatus.Posted;
                transfer.Cancel();

                if (wasPosted)
                {
                    // Reverse stock movements
                    foreach (var item in transfer.Items)
                    {
                        // Phase 25: GetRetailQuantityEquivalent removed.
                        await IncreaseStockAsync(item.ProductId, transfer.FromWarehouseId, item.Quantity, MovementType.TransferIn, "StockTransferCancel", transfer.Id, null, userId, ct);
                        await DecreaseStockAsync(item.ProductId, transfer.ToWarehouseId, item.Quantity, MovementType.TransferOut, "StockTransferCancel", transfer.Id, null, userId, ct);
                    }
                }

                await _uow.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Stock Transfer Cancelled: {TransferNo}", transfer.TransferNo);
                return await GetTransferByIdAsync(transfer.Id, ct);
            }
            catch (DomainException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Domain rule violation while cancelling stock transfer {Id}", id);
                return Result<StockTransferDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error cancelling stock transfer {Id}", id);
                return Result<StockTransferDto>.Failure("حدث خطأ أثناء إلغاء التحويل");
            }
        }, ct);
    }

    private static StockTransferDto MapToDto(StockTransfer t)
    {
        return new StockTransferDto(
            t.Id,
            t.TransferNo,
            t.FromWarehouseId,
            t.FromWarehouse?.Name ?? "غير معروف",
            t.ToWarehouseId,
            t.ToWarehouse?.Name ?? "غير معروف",
            t.TransferDate,
            t.Notes,
            (byte)t.Status,
            t.Items.Select(it => new StockTransferItemDto(
                it.Id,
                it.ProductId,
                it.Product?.Name ?? "غير معروف",
                it.Quantity,
                (byte)it.Mode,
                it.Notes
            )).ToList()
        );
    }
}


