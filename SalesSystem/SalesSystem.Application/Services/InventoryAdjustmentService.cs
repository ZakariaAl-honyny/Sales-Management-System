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
            var adjustments = await _uow.InventoryAdjustments.ToListAsync(ct, "Warehouse", "Account", "Lines");
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
                a => a.Id == id, ct, "Warehouse", "Account", "Lines");
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
            var adjustmentNo = seqResult.Value;

            var adjustment = InventoryAdjustment.Create(
                adjustmentNo,
                (short)request.WarehouseId,
                (InventoryAdjustmentType)request.AdjustmentType,
                request.AdjustmentDate,
                notes: null,
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
                request.ProductId,
                request.Quantity,
                request.UnitCost,
                batchId: null);

            adjustment.AddLine(line);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Line added to inventory adjustment {AdjustmentId}: Product {ProductId}, Qty {Quantity}, Cost {UnitCost}",
                adjustmentId, request.ProductId, request.Quantity, request.UnitCost);
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

            // Update stock levels based on adjustment type
            foreach (var line in adjustment.Lines)
            {
                switch (adjustment.AdjustmentType)
                {
                    case InventoryAdjustmentType.Opening:
                    case InventoryAdjustmentType.Increase:
                        await _inventoryService.IncreaseStockAsync(
                            line.ProductId, adjustment.WarehouseId, line.Quantity, line.UnitCost, userId, ct);
                        break;

                    case InventoryAdjustmentType.Shortage:
                    case InventoryAdjustmentType.Damage: // Damage also decreases stock
                        // Correction = set quantity to target level: compute delta, then adjust
                        var stockResult = await _inventoryService.GetStockAsync(
                            line.ProductId, adjustment.WarehouseId, ct);
                        if (stockResult.IsSuccess)
                        {
                            var currentQty = stockResult.Value;
                            if (line.Quantity > currentQty)
                                await _inventoryService.IncreaseStockAsync(
                                    line.ProductId, adjustment.WarehouseId,
                                    line.Quantity - currentQty, line.UnitCost, userId, ct);
                            else if (line.Quantity < currentQty)
                                await _inventoryService.DecreaseStockAsync(
                                    line.ProductId, adjustment.WarehouseId,
                                    currentQty - line.Quantity, line.UnitCost, userId, ct);
                        }
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
            adjustment.AdjustmentDate,
            adjustment.WarehouseId,
            adjustment.Warehouse?.Name,
            (byte)adjustment.AdjustmentType,
            GetAdjustmentTypeName(adjustment.AdjustmentType),
            (byte)adjustment.Status,
            GetStatusName(adjustment.Status),
            adjustment.PostedAt,
            adjustment.Lines?.Select(l => new InventoryAdjustmentLineDto(
                l.Id,
                l.InventoryAdjustmentId,
                l.ProductId,
                null, // ProductName — not loaded
                l.Quantity,
                l.UnitCost,
                l.TotalCost
            )).ToList()
        );
    }

    private static string? GetAdjustmentTypeName(InventoryAdjustmentType type) => type switch
    {
        InventoryAdjustmentType.Opening => "افتتاحي",
        InventoryAdjustmentType.Increase => "إضافة",
        InventoryAdjustmentType.Shortage => "عجز",
        InventoryAdjustmentType.Damage => "تلف",
        _ => null
    };

    private static string? GetStatusName(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "مسودة",
        InvoiceStatus.Posted => "مرحّل",
        InvoiceStatus.Cancelled => "ملغي",
        _ => null
    };
}
