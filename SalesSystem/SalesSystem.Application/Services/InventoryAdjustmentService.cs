using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class InventoryAdjustmentService : IInventoryAdjustmentService
{
    private readonly IUnitOfWork _uow;
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<InventoryAdjustmentService> _logger;

    public InventoryAdjustmentService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        ILogger<InventoryAdjustmentService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _logger = logger;
    }

    public async Task<Result<List<InventoryAdjustmentDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var adjustments = await _uow.InventoryAdjustments.ToListAsync(ct, "Warehouse", "Lines");
            var dtos = adjustments.Select(MapToDto).ToList();
            return Result<List<InventoryAdjustmentDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory adjustments");
            return Result<List<InventoryAdjustmentDto>>.Failure("حدث خطأ أثناء استرجاع قائمة التسويات");
        }
    }

    public async Task<Result<InventoryAdjustmentDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var adjustment = await _uow.InventoryAdjustments.FirstOrDefaultAsync(
                a => a.Id == id, ct, "Warehouse", "Lines");
            if (adjustment == null)
                return Result<InventoryAdjustmentDto>.Failure("التسوية غير موجودة", ErrorCodes.NotFound);

            return Result<InventoryAdjustmentDto>.Success(MapToDto(adjustment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory adjustment {Id}", id);
            return Result<InventoryAdjustmentDto>.Failure("حدث خطأ أثناء استرجاع بيانات التسوية");
        }
    }

    public async Task<Result<InventoryAdjustmentDto>> CreateAsync(CreateInventoryAdjustmentRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Generate adjustment number via DocumentSequenceService (thread-safe)
            var seqResult = await _sequenceService.GetNextIntAsync("InventoryAdjustment", ct);
            if (!seqResult.IsSuccess)
                return Result<InventoryAdjustmentDto>.Failure(seqResult.Error!);
            var adjustmentNo = seqResult.Value.ToString("D6");

            var adjustment = InventoryAdjustment.Create(
                adjustmentNo,
                request.WarehouseId,
                (InventoryAdjustmentType)request.AdjustmentType,
                reason: request.Reason,
                createdByUserId: userId);

            await _uow.InventoryAdjustments.AddAsync(adjustment, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Inventory adjustment created (No: {AdjustmentNo}, ID: {Id}) by User {UserId}",
                adjustment.AdjustmentNo, adjustment.Id, userId);
            return Result<InventoryAdjustmentDto>.Success(MapToDto(adjustment));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating inventory adjustment: {Message}", ex.Message);
            return Result<InventoryAdjustmentDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory adjustment");
            return Result<InventoryAdjustmentDto>.Failure("حدث خطأ أثناء إنشاء التسوية");
        }
    }

    public async Task<Result<InventoryAdjustmentDto>> AddLineAsync(int adjustmentId, AddInventoryAdjustmentLineRequest request, CancellationToken ct)
    {
        try
        {
            var adjustment = await _uow.InventoryAdjustments.FirstOrDefaultAsync(
                a => a.Id == adjustmentId, ct, "Lines");
            if (adjustment == null)
                return Result<InventoryAdjustmentDto>.Failure("التسوية غير موجودة", ErrorCodes.NotFound);

            var line = InventoryAdjustmentLine.Create(
                adjustmentId,
                request.ProductUnitId,
                request.ExpectedQuantity,
                request.ActualQuantity,
                request.UnitCost);

            adjustment.AddLine(line);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Line added to inventory adjustment {AdjustmentId}: ProductUnit {ProductUnitId}, Expected {Expected}, Actual {Actual}",
                adjustmentId, request.ProductUnitId, request.ExpectedQuantity, request.ActualQuantity);
            return Result<InventoryAdjustmentDto>.Success(MapToDto(adjustment));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation adding line to inventory adjustment {AdjustmentId}: {Message}", adjustmentId, ex.Message);
            return Result<InventoryAdjustmentDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding line to inventory adjustment {Id}", adjustmentId);
            return Result<InventoryAdjustmentDto>.Failure("حدث خطأ أثناء إضافة بند التسوية");
        }
    }

    public async Task<Result> PostAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var adjustment = await _uow.InventoryAdjustments.FirstOrDefaultAsync(
                a => a.Id == id, ct, "Lines");
            if (adjustment == null)
                return Result.Failure("التسوية غير موجودة", ErrorCodes.NotFound);

            adjustment.Post();

            // Update stock levels based on adjustment type and line differences
            foreach (var line in adjustment.Lines)
            {
                var productUnit = await _uow.ProductUnits.GetByIdAsync(line.ProductUnitId, ct);
                if (productUnit == null)
                {
                    _logger.LogWarning("ProductUnit {ProductUnitId} not found for adjustment line", line.ProductUnitId);
                    continue;
                }

                var productId = productUnit.ProductId;
                var diff = line.ActualQuantity - line.ExpectedQuantity;

                switch (adjustment.AdjustmentType)
                {
                    case InventoryAdjustmentType.Addition:
                        // Increase stock by ActualQuantity (treated as addition from zero)
                        if (line.ActualQuantity > 0)
                            await _inventoryService.IncreaseStockAsync(
                                productId, adjustment.WarehouseId, line.ActualQuantity, line.UnitCost, userId, ct);
                        break;

                    case InventoryAdjustmentType.Deduction:
                        // Decrease stock by ActualQuantity (treated as removal)
                        if (line.ActualQuantity > 0)
                            await _inventoryService.DecreaseStockAsync(
                                productId, adjustment.WarehouseId, line.ActualQuantity, line.UnitCost, userId, ct);
                        break;

                    case InventoryAdjustmentType.Correction:
                        // Adjust stock to match ActualQuantity
                        if (diff > 0)
                            await _inventoryService.IncreaseStockAsync(
                                productId, adjustment.WarehouseId, Math.Abs(diff), line.UnitCost, userId, ct);
                        else if (diff < 0)
                            await _inventoryService.DecreaseStockAsync(
                                productId, adjustment.WarehouseId, Math.Abs(diff), line.UnitCost, userId, ct);
                        break;
                }
            }

            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Inventory adjustment {Id} posted by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation posting inventory adjustment {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting inventory adjustment {Id}", id);
            return Result.Failure("حدث خطأ أثناء ترحيل التسوية");
        }
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct)
    {
        try
        {
            var adjustment = await _uow.InventoryAdjustments.GetByIdAsync(id, ct);
            if (adjustment == null)
                return Result.Failure("التسوية غير موجودة", ErrorCodes.NotFound);

            adjustment.Cancel();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Inventory adjustment {Id} cancelled", id);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation cancelling inventory adjustment {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling inventory adjustment {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء التسوية");
        }
    }

    // ─── Private Helpers ─────────────────────────────────

    private static InventoryAdjustmentDto MapToDto(InventoryAdjustment adjustment)
    {
        return new InventoryAdjustmentDto(
            adjustment.Id,
            adjustment.AdjustmentNo,
            adjustment.WarehouseId,
            adjustment.Warehouse?.Name,
            (byte)adjustment.AdjustmentType,
            GetAdjustmentTypeName(adjustment.AdjustmentType),
            adjustment.Reason,
            (byte)adjustment.Status,
            GetStatusName(adjustment.Status),
            adjustment.CreatedAt,
            adjustment.CreatedByUserId,
            adjustment.PostedAt,
            adjustment.CancelledAt,
            adjustment.Lines?.Select(l => new InventoryAdjustmentLineDto(
                l.Id,
                l.InventoryAdjustmentId,
                l.ProductUnitId,
                null,
                l.ExpectedQuantity,
                l.ActualQuantity,
                l.UnitCost
            )).ToList()
        );
    }

    private static string? GetAdjustmentTypeName(InventoryAdjustmentType type) => type switch
    {
        InventoryAdjustmentType.Addition => "إضافة",
        InventoryAdjustmentType.Deduction => "خصم",
        InventoryAdjustmentType.Correction => "تصحيح",
        _ => null
    };

    private static string? GetStatusName(InventoryCountStatus status) => status switch
    {
        InventoryCountStatus.Draft => "مسودة",
        InventoryCountStatus.Posted => "مرحّل",
        InventoryCountStatus.Cancelled => "ملغي",
        _ => null
    };
}
