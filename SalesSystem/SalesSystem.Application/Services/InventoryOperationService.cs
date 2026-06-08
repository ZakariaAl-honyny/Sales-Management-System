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

public class InventoryOperationService : IInventoryOperationService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<InventoryOperationService> _logger;

    public InventoryOperationService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        ILogger<InventoryOperationService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<InventoryOperationDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var operation = await _uow.InventoryOperations.FirstOrDefaultAsync(
            o => o.Id == id, ct, "Items.Product", "Warehouse");

        if (operation == null)
            return Result<InventoryOperationDto>.Failure("العملية غير موجودة", ErrorCodes.NotFound);

        return Result<InventoryOperationDto>.Success(MapToDto(operation));
    }

    public async Task<Result<PagedResult<InventoryOperationDto>>> GetAllAsync(
        int? warehouseId, byte? operationType, int page, int pageSize, CancellationToken ct)
    {
        Expression<Func<InventoryOperation, bool>> predicate = o =>
            (!warehouseId.HasValue || o.WarehouseId == warehouseId.Value) &&
            (!operationType.HasValue || (int)o.OperationType == operationType.Value);

        var totalItems = await _uow.InventoryOperations.CountAsync(predicate, ct);

        var items = await _uow.InventoryOperations.ToListAsync(
            predicate,
            q => q.OrderByDescending(o => o.Id)
                  .Skip((page - 1) * pageSize)
                  .Take(pageSize),
            ct,
            false,
            "Items.Product", "Warehouse");

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<InventoryOperationDto>>.Success(
            PagedResult<InventoryOperationDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<InventoryOperationDto>> CreateAsync(
        CreateInventoryOperationRequest request, int userId, CancellationToken ct)
    {
        // ─── Validation ───────────────────────────────────────────────
        if (request.Items.Count == 0)
            return Result<InventoryOperationDto>.Failure("يجب إضافة صنف واحد على الأقل");

        if (request.OperationType < 1 || request.OperationType > 3)
            return Result<InventoryOperationDto>.Failure("نوع العملية غير صالح");

        if (request.OperationType == (byte)InventoryOperationType.Adjustment && !request.AdjustmentType.HasValue)
            return Result<InventoryOperationDto>.Failure("نوع التسوية مطلوب لعمليات التسوية المخزنية");

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                return Result<InventoryOperationDto>.Failure("الكمية يجب أن تكون أكبر من الصفر لجميع الأصناف");
        }

        // ─── Generate OperationNo ─────────────────────────────────────
        var prefix = GetOperationPrefix((InventoryOperationType)request.OperationType);
        var seqResult = await _sequenceService.GetNextIntAsync($"InvOp_{prefix}", ct);
        if (!seqResult.IsSuccess)
            return Result<InventoryOperationDto>.Failure(seqResult.Error ?? "فشل في توليد رقم العملية");

        var operationNo = $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{seqResult.Value:D4}";

        // ─── Create Operation ─────────────────────────────────────────
        var operation = InventoryOperation.Create(
            operationNo,
            request.WarehouseId,
            (InventoryOperationType)request.OperationType,
            request.OperationDate,
            request.ReferenceNo,
            request.Notes,
            request.AdjustmentType.HasValue
                ? (AdjustmentType)request.AdjustmentType.Value
                : null,
            userId);

        foreach (var item in request.Items)
        {
            operation.AddItem(
                item.ProductId,
                item.Quantity,
                item.UnitCost,
                item.StockIssueReason.HasValue
                    ? (StockIssueReason)item.StockIssueReason.Value
                    : null,
                item.Notes);
        }

        // ─── Save ─────────────────────────────────────────────────────
        return await _uow.ExecuteTransactionAsync(async () =>
        {
            await _uow.InventoryOperations.AddAsync(operation, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "InventoryOperation {OperationNo} (ID: {Id}, Type: {Type}) created by user {UserId}",
                operation.OperationNo, operation.Id, prefix, userId);

            return Result<InventoryOperationDto>.Success(MapToDto(operation));
        }, ct);
    }

    public async Task<Result<InventoryOperationDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        var operation = await _uow.InventoryOperations.FirstOrDefaultAsync(
            o => o.Id == id, ct, "Items.Product", "Warehouse");

        if (operation == null)
            return Result<InventoryOperationDto>.Failure("العملية غير موجودة", ErrorCodes.NotFound);

        if (operation.Status != InvoiceStatus.Draft)
            return Result<InventoryOperationDto>.Failure("يمكن فقط ترحيل العمليات المسودة");

        // ─── Validate stock for StockIssue ──────────────────────────────
        if (operation.OperationType == InventoryOperationType.StockIssue)
        {
            var settings = await _uow.StoreSettings.FirstOrDefaultAsync(s => true, ct);
            var allowNegativeStock = settings?.AllowNegativeStock ?? false;

            foreach (var item in operation.Items)
            {
                var validation = await _inventoryService.ValidateStockAsync(
                    item.ProductId, operation.WarehouseId, item.Quantity, allowNegativeStock, ct);

                if (!validation.IsSuccess)
                    return Result<InventoryOperationDto>.Failure(validation.Error!, validation.ErrorCode);
            }
        }

        // ─── Post & Apply Stock Changes ────────────────────────────────
        return await _uow.ExecuteTransactionAsync(async () =>
        {
            // 1. Set status to Posted
            operation.Post();

            // 2. Apply stock changes based on operation type
            foreach (var item in operation.Items)
            {
                switch (operation.OperationType)
                {
                    case InventoryOperationType.StockReceipt:
                        await _inventoryService.IncreaseStockAsync(
                            item.ProductId, operation.WarehouseId, item.Quantity,
                            MovementType.Adjustment, "InventoryOperation", operation.Id,
                            item.UnitCost, userId, ct);
                        break;

                    case InventoryOperationType.StockIssue:
                        await _inventoryService.DecreaseStockAsync(
                            item.ProductId, operation.WarehouseId, item.Quantity,
                            MovementType.Adjustment, "InventoryOperation", operation.Id,
                            item.UnitCost, userId, ct);
                        break;

                    case InventoryOperationType.Adjustment:
                        if (operation.AdjustmentType == Domain.Enums.AdjustmentType.Surplus)
                        {
                            await _inventoryService.IncreaseStockAsync(
                                item.ProductId, operation.WarehouseId, item.Quantity,
                                MovementType.Adjustment, "InventoryOperation", operation.Id,
                                item.UnitCost, userId, ct);
                        }
                        else if (operation.AdjustmentType == Domain.Enums.AdjustmentType.Shortage)
                        {
                            await _inventoryService.DecreaseStockAsync(
                                item.ProductId, operation.WarehouseId, item.Quantity,
                                MovementType.Adjustment, "InventoryOperation", operation.Id,
                                item.UnitCost, userId, ct);
                        }
                        break;
                }
            }

            // 3. Persist all changes
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation(
                "InventoryOperation {OperationNo} (ID: {Id}, Type: {Type}) posted by user {UserId}",
                operation.OperationNo, operation.Id, operation.OperationType, userId);

            // 4. Reload with full graph for DTO
            var reloaded = await _uow.InventoryOperations.FirstOrDefaultAsync(
                o => o.Id == id, ct, "Items.Product", "Warehouse");

            return Result<InventoryOperationDto>.Success(MapToDto(reloaded!));
        }, ct);
    }

    public async Task<Result<InventoryOperationDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        var operation = await _uow.InventoryOperations.FirstOrDefaultAsync(
            o => o.Id == id, ct, "Items.Product", "Warehouse");

        if (operation == null)
            return Result<InventoryOperationDto>.Failure("العملية غير موجودة", ErrorCodes.NotFound);

        if (operation.Status == InvoiceStatus.Cancelled)
            return Result<InventoryOperationDto>.Failure("العملية ملغاة بالفعل");

        var wasPosted = operation.Status == InvoiceStatus.Posted;

        // ─── Cancel & Reverse Stock Changes ──────────────────────────
        return await _uow.ExecuteTransactionAsync(async () =>
        {
            // 1. Set status to Cancelled
            operation.Cancel();

            // 2. If it was posted, reverse the stock changes
            if (wasPosted)
            {
                foreach (var item in operation.Items)
                {
                    switch (operation.OperationType)
                    {
                        case InventoryOperationType.StockReceipt:
                            // Reverse receipt → decrease stock
                            await _inventoryService.DecreaseStockAsync(
                                item.ProductId, operation.WarehouseId, item.Quantity,
                                MovementType.Adjustment, "InventoryOperationCancel", operation.Id,
                                item.UnitCost, userId, ct);
                            break;

                        case InventoryOperationType.StockIssue:
                            // Reverse issue → increase stock
                            await _inventoryService.IncreaseStockAsync(
                                item.ProductId, operation.WarehouseId, item.Quantity,
                                MovementType.Adjustment, "InventoryOperationCancel", operation.Id,
                                item.UnitCost, userId, ct);
                            break;

                        case InventoryOperationType.Adjustment:
                            if (operation.AdjustmentType == Domain.Enums.AdjustmentType.Surplus)
                            {
                                // Reverse surplus → decrease stock
                                await _inventoryService.DecreaseStockAsync(
                                    item.ProductId, operation.WarehouseId, item.Quantity,
                                    MovementType.Adjustment, "InventoryOperationCancel", operation.Id,
                                    item.UnitCost, userId, ct);
                            }
                            else if (operation.AdjustmentType == Domain.Enums.AdjustmentType.Shortage)
                            {
                                // Reverse shortage → increase stock
                                await _inventoryService.IncreaseStockAsync(
                                    item.ProductId, operation.WarehouseId, item.Quantity,
                                    MovementType.Adjustment, "InventoryOperationCancel", operation.Id,
                                    item.UnitCost, userId, ct);
                            }
                            break;
                    }
                }
            }

            // 3. Persist all changes
            await _uow.SaveChangesAsync(ct);

            _logger.LogWarning(
                "InventoryOperation {OperationNo} (ID: {Id}, Type: {Type}) cancelled by user {UserId}. WasPosted: {WasPosted}",
                operation.OperationNo, operation.Id, operation.OperationType, userId, wasPosted);

            var reloaded = await _uow.InventoryOperations.FirstOrDefaultAsync(
                o => o.Id == id, ct, "Items.Product", "Warehouse");

            return Result<InventoryOperationDto>.Success(MapToDto(reloaded!));
        }, ct);
    }

    // ─── Private Helpers ───────────────────────────────────────────────

    private static string GetOperationPrefix(InventoryOperationType type)
    {
        return type switch
        {
            InventoryOperationType.StockIssue => "ISS",
            InventoryOperationType.StockReceipt => "REC",
            InventoryOperationType.Adjustment => "ADJ",
            _ => "OPR"
        };
    }

    private static InventoryOperationDto MapToDto(InventoryOperation o)
    {
        return new InventoryOperationDto(
            o.Id,
            o.OperationNo,
            o.WarehouseId,
            o.Warehouse?.Name ?? "غير معروف",
            (byte)o.OperationType,
            o.OperationDate,
            o.ReferenceNo,
            o.Notes,
            o.AdjustmentType.HasValue ? (byte)o.AdjustmentType.Value : null,
            (byte)o.Status,
            o.Items.Select(i => new InventoryOperationItemDto(
                i.Id,
                i.ProductId,
                i.Product?.Name ?? "غير معروف",
                i.Quantity,
                i.UnitCost,
                i.StockIssueReason.HasValue ? (byte)i.StockIssueReason.Value : null,
                i.Notes
            )).ToList()
        );
    }
}
