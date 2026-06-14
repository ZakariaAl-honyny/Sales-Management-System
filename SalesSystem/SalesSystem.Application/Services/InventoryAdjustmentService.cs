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
    private readonly ILogger<InventoryAdjustmentService> _logger;

    public InventoryAdjustmentService(IUnitOfWork uow, ILogger<InventoryAdjustmentService> logger)
    {
        _uow = uow;
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
            // Compute next adjustment number (temporary — in production, use DocumentSequenceService)
            var adjustmentNo = await GetNextAdjustmentNumberAsync(ct);

            var adjustment = InventoryAdjustment.Create(
                adjustmentNo,
                (short)request.WarehouseId,
                request.AdjustmentDate,
                (InventoryAdjustmentType)request.AdjustmentType,
                request.AccountId,
                userId);

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
                request.ProductUnitId,
                request.Quantity,
                request.UnitCost);

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

            adjustment.Post(userId);
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

    /// <summary>
    /// Computes the next adjustment number as (max existing AdjustmentNo) + 1.
    /// Temporary — in production, use IDocumentSequenceService.GetNextIntAsync().
    /// </summary>
    private async Task<int> GetNextAdjustmentNumberAsync(CancellationToken ct)
    {
        var allAdjustments = await _uow.InventoryAdjustments.ToListIgnoreFiltersAsync(ct);
        if (allAdjustments.Count == 0)
            return 1;
        return allAdjustments.Max(a => a.AdjustmentNo) + 1;
    }

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
            adjustment.AccountId,
            adjustment.Account?.NameAr ?? adjustment.Account?.NameEn,
            (byte)adjustment.Status,
            GetStatusName(adjustment.Status),
            adjustment.PostedAt,
            adjustment.Lines?.Select(l => new InventoryAdjustmentLineDto(
                l.Id,
                l.InventoryAdjustmentId,
                l.ProductId,
                null, // ProductName — not loaded
                l.ProductUnitId,
                null, // ProductUnitName — not loaded
                l.Quantity,
                l.UnitCost,
                l.LineTotal,
                false // Entity — no IsActive
            )).ToList(),
            false // DocumentEntity — no IsActive
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
