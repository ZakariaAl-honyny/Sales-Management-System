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

public class InventoryCountService : IInventoryCountService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<InventoryCountService> _logger;

    public InventoryCountService(IUnitOfWork uow, ILogger<InventoryCountService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<InventoryCountDto>>> GetAllAsync(CancellationToken ct)
    {
        try
        {
            var counts = await _uow.InventoryCounts.ToListAsync(ct, "Warehouse", "Lines");
            var dtos = counts.Select(MapToDto).ToList();
            return Result<List<InventoryCountDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory counts");
            return Result<List<InventoryCountDto>>.Failure("حدث خطأ أثناء استرجاع قائمة الجرد");
        }
    }

    public async Task<Result<InventoryCountDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var count = await _uow.InventoryCounts.FirstOrDefaultAsync(
                c => c.Id == id, ct, "Warehouse", "Lines");
            if (count == null)
                return Result<InventoryCountDto>.Failure("الجرد غير موجود", ErrorCodes.NotFound);

            return Result<InventoryCountDto>.Success(MapToDto(count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory count {Id}", id);
            return Result<InventoryCountDto>.Failure("حدث خطأ أثناء استرجاع بيانات الجرد");
        }
    }

    public async Task<Result<InventoryCountDto>> CreateAsync(CreateInventoryCountRequest request, int userId, CancellationToken ct)
    {
        try
        {
            // Compute next count number (temporary — in production, use DocumentSequenceService)
            var countNo = await GetNextCountNumberAsync(ct);

            var count = InventoryCount.Create(countNo, (short)request.WarehouseId, request.CountDate, userId);
            if (!string.IsNullOrWhiteSpace(request.Notes))
                count.SetNotes(request.Notes);

            await _uow.InventoryCounts.AddAsync(count, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Inventory count created (No: {CountNo}, ID: {Id}) by User {UserId}", count.CountNo, count.Id, userId);
            return Result<InventoryCountDto>.Success(MapToDto(count));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating inventory count: {Message}", ex.Message);
            return Result<InventoryCountDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory count");
            return Result<InventoryCountDto>.Failure("حدث خطأ أثناء إنشاء الجرد");
        }
    }

    public async Task<Result<InventoryCountDto>> AddLineAsync(int countId, AddInventoryCountLineRequest request, CancellationToken ct)
    {
        try
        {
            var count = await _uow.InventoryCounts.GetByIdAsync(countId, ct);
            if (count == null)
                return Result<InventoryCountDto>.Failure("الجرد غير موجود", ErrorCodes.NotFound);

            var line = InventoryCountLine.Create(
                countId,
                request.ProductId,
                request.ProductUnitId,
                request.SystemQuantity,
                request.ActualQuantity);

            count.AddLine(line);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Line added to inventory count {CountId}: Product {ProductId}, Diff {Difference}", countId, request.ProductId, line.Difference);
            return Result<InventoryCountDto>.Success(MapToDto(count));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation adding line to inventory count {CountId}: {Message}", countId, ex.Message);
            return Result<InventoryCountDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding line to inventory count {Id}", countId);
            return Result<InventoryCountDto>.Failure("حدث خطأ أثناء إضافة بند الجرد");
        }
    }

    public async Task<Result> PostAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            // Load with Lines included for domain validation (Post checks _lines.Any())
            var count = await _uow.InventoryCounts.FirstOrDefaultAsync(
                c => c.Id == id, ct, "Lines");
            if (count == null)
                return Result.Failure("الجرد غير موجود", ErrorCodes.NotFound);

            count.Post(userId);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Inventory count {Id} posted by User {UserId}", id, userId);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation posting inventory count {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting inventory count {Id}", id);
            return Result.Failure("حدث خطأ أثناء ترحيل الجرد");
        }
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct)
    {
        try
        {
            var count = await _uow.InventoryCounts.GetByIdAsync(id, ct);
            if (count == null)
                return Result.Failure("الجرد غير موجود", ErrorCodes.NotFound);

            count.Cancel();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Inventory count {Id} cancelled", id);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation cancelling inventory count {Id}: {Message}", id, ex.Message);
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling inventory count {Id}", id);
            return Result.Failure("حدث خطأ أثناء إلغاء الجرد");
        }
    }

    // ─── Private Helpers ─────────────────────────────────

    /// <summary>
    /// Computes the next count number as (max existing CountNo) + 1.
    /// Temporary — in production, use IDocumentSequenceService.GetNextIntAsync().
    /// </summary>
    private async Task<int> GetNextCountNumberAsync(CancellationToken ct)
    {
        var allCounts = await _uow.InventoryCounts.ToListIgnoreFiltersAsync(ct);
        if (allCounts.Count == 0)
            return 1;
        return allCounts.Max(c => c.CountNo) + 1;
    }

    private static InventoryCountDto MapToDto(InventoryCount count)
    {
        return MapToDto(count, count.Lines?.ToList() ?? new List<InventoryCountLine>());
    }

    private static InventoryCountDto MapToDto(InventoryCount count, IReadOnlyCollection<InventoryCountLine>? lines)
    {
        return new InventoryCountDto(
            count.Id,
            count.CountNo,
            count.CountDate,
            count.WarehouseId,
            null, // WarehouseName — not loaded
            (byte)count.Status,
            GetStatusName(count.Status),
            count.Notes,
            count.PostedAt,
            count.PostedByUserId,
            lines?.Select(l => new InventoryCountLineDto(
                l.Id,
                l.InventoryCountId,
                l.ProductId,
                null, // ProductName — not loaded
                l.ProductUnitId,
                null, // ProductUnitName — not loaded
                l.SystemQuantity,
                l.ActualQuantity,
                l.Difference,
                false // Entity — no IsActive
            )).ToList(),
            false // DocumentEntity — no IsActive
        );
    }

    private static string? GetStatusName(InventoryCountStatus status) => status switch
    {
        InventoryCountStatus.Draft => "مسودة",
        InventoryCountStatus.Posted => "مرحّل",
        InventoryCountStatus.Cancelled => "ملغي",
        _ => null
    };
}
