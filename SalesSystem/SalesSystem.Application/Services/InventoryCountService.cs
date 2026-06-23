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
    private readonly IInventoryService _inventoryService;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly ILogger<InventoryCountService> _logger;

    public InventoryCountService(
        IUnitOfWork uow,
        IInventoryService inventoryService,
        IDocumentSequenceService sequenceService,
        ILogger<InventoryCountService> logger)
    {
        _uow = uow;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
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
            // Generate count number via DocumentSequenceService (thread-safe)
            var countNoResult = await _sequenceService.GetNextIntAsync("InventoryCount", ct);
            if (!countNoResult.IsSuccess)
                return Result<InventoryCountDto>.Failure("فشل في توليد رقم الجرد");
            var countNo = countNoResult.Value.ToString("D6");

            var count = InventoryCount.Create(countNo, request.WarehouseId, request.Notes, userId);

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
                request.ProductUnitId,
                request.ExpectedQuantity,
                request.ActualQuantity,
                request.Notes);

            count.AddLine(line);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Line added to inventory count {CountId}: ProductUnit {ProductUnitId}, Diff {Difference}",
                countId, request.ProductUnitId, line.Difference);
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
        // Load with Lines included for domain validation
        var count = await _uow.InventoryCounts.FirstOrDefaultAsync(
            c => c.Id == id, ct, "Lines");
        if (count == null)
            return Result.Failure("الجرد غير موجود", ErrorCodes.NotFound);

        return await _uow.ExecuteTransactionAsync<Result>(async () =>
        {
            try
            {
                count.Post();

                // Identify lines with non-zero differences (surplus or shortage)
                var allAdjustmentLines = count.Lines.Where(l => l.Difference != 0).ToList();
                if (allAdjustmentLines.Any())
                {
                    // Generate an adjustment document number
                    var adjustmentNoResult = await _sequenceService.GetNextIntAsync("InventoryCountAdjustment", ct);
                    if (!adjustmentNoResult.IsSuccess)
                        return Result.Failure("فشل في توليد رقم تسوية الجرد");
                    var adjustmentNo = adjustmentNoResult.Value.ToString("D6");

                    // Create the InventoryAdjustment document to record the count results
                    var adjustment = InventoryAdjustment.Create(
                        adjustmentNo: adjustmentNo,
                        warehouseId: count.WarehouseId,
                        adjustmentType: InventoryAdjustmentType.Correction,
                        reason: $"تسوية جرد رقم {count.CountNo}",
                        createdByUserId: userId);

                    foreach (var line in allAdjustmentLines)
                    {
                        var productUnit = await _uow.ProductUnits.GetByIdAsync(line.ProductUnitId, ct);
                        if (productUnit == null)
                        {
                            _logger.LogWarning("ProductUnit {ProductUnitId} not found for count line", line.ProductUnitId);
                            continue;
                        }

                        // Create adjustment line with expected/actual quantities
                        var adjLine = InventoryAdjustmentLine.Create(
                            inventoryAdjustmentId: adjustment.Id,
                            productUnitId: line.ProductUnitId,
                            expectedQuantity: line.ExpectedQuantity,
                            actualQuantity: line.ActualQuantity,
                            unitCost: 0m);
                        adjustment.AddLine(adjLine);

                        // Update stock: positive difference = surplus (increase), negative = shortage (decrease)
                        if (line.Difference > 0)
                        {
                            await _inventoryService.IncreaseStockAsync(
                                productUnit.ProductId, count.WarehouseId, line.Difference, null, userId, ct);
                        }
                        else
                        {
                            await _inventoryService.DecreaseStockAsync(
                                productUnit.ProductId, count.WarehouseId, Math.Abs(line.Difference), null, userId, ct);
                        }
                    }

                    await _uow.InventoryAdjustments.AddAsync(adjustment, ct);
                    _logger.LogInformation(
                        "Created inventory adjustment {AdjustmentNo} for count {CountId} — {LineCount} lines adjusted",
                        adjustmentNo, id, allAdjustmentLines.Count);
                }

                // ─── InventoryTransaction Audit Trail ────────────────────────────
                var countTxSeq = await _sequenceService.GetNextIntAsync("InventoryTransaction", ct);
                if (countTxSeq.IsSuccess)
                {
                    var countInvTx = InventoryTransaction.Create(
                        countTxSeq.Value.ToString("D6"),
                        InventoryTransactionType.Count,
                        count.WarehouseId,
                        InventoryReferenceType.Count,
                        count.Id,
                        $"جرد مخزون - رقم {count.CountNo}",
                        userId);
                    foreach (var line in allAdjustmentLines)
                    {
                        var productUnit = await _uow.ProductUnits.GetByIdAsync(line.ProductUnitId, ct);
                        if (productUnit == null) continue;
                        countInvTx.AddLine(InventoryTransactionLine.Create(
                            countInvTx.Id,
                            line.ProductUnitId,
                            Math.Abs(line.Difference),
                            0m,  // unit cost 0 for count adjustments
                            null, null, count.WarehouseId));
                    }
                    await _uow.InventoryTransactions.AddAsync(countInvTx, ct);
                }

                await _uow.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Inventory count {Id} posted by User {UserId} — {LineCount} adjustments created",
                    id, userId, allAdjustmentLines.Count);
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
        }, ct);
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct)
    {
        var count = await _uow.InventoryCounts.FirstOrDefaultAsync(
            c => c.Id == id, ct, "Lines");
        if (count == null)
            return Result.Failure("الجرد غير موجود", ErrorCodes.NotFound);

        return await _uow.ExecuteTransactionAsync<Result>(async () =>
        {
            try
            {
                // ─── Reverse Stock Changes (if Posted) ───────────────────────────
                if (count.Status == InventoryCountStatus.Posted)
                {
                    var linesNeedingRollback = count.Lines.Where(l => l.Difference != 0).ToList();
                    foreach (var line in linesNeedingRollback)
                    {
                        var productUnit = await _uow.ProductUnits.GetByIdAsync(line.ProductUnitId, ct);
                        if (productUnit == null)
                        {
                            _logger.LogWarning("ProductUnit {ProductUnitId} not found for count cancel line", line.ProductUnitId);
                            continue;
                        }

                        // Reverse: surplus → decrease, shortage → increase
                        if (line.Difference > 0)
                        {
                            await _inventoryService.DecreaseStockAsync(
                                productUnit.ProductId, count.WarehouseId, line.Difference, null, null, ct);
                        }
                        else
                        {
                            await _inventoryService.IncreaseStockAsync(
                                productUnit.ProductId, count.WarehouseId, Math.Abs(line.Difference), null, null, ct);
                        }
                    }

                    // ─── InventoryTransaction Reversal Audit Trail ────────────────
                    var countCancelTxSeq = await _sequenceService.GetNextIntAsync("InventoryTransaction", ct);
                    if (countCancelTxSeq.IsSuccess)
                    {
                        var countCancelInvTx = InventoryTransaction.Create(
                            countCancelTxSeq.Value.ToString("D6"),
                            InventoryTransactionType.Count,
                            count.WarehouseId,
                            InventoryReferenceType.Count,
                            count.Id,
                            $"إلغاء جرد مخزون - رقم {count.CountNo}",
                            null);
                        foreach (var line in linesNeedingRollback)
                        {
                            var productUnit = await _uow.ProductUnits.GetByIdAsync(line.ProductUnitId, ct);
                            if (productUnit == null) continue;
                            countCancelInvTx.AddLine(InventoryTransactionLine.Create(
                                countCancelInvTx.Id,
                                line.ProductUnitId,
                                Math.Abs(line.Difference),
                                0m,
                                null, null, count.WarehouseId));
                        }
                        await _uow.InventoryTransactions.AddAsync(countCancelInvTx, ct);
                    }
                }

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
        }, ct);
    }

    // ─── Private Helpers ─────────────────────────────────

    private static InventoryCountDto MapToDto(InventoryCount count)
    {
        return MapToDto(count, count.Lines?.ToList() ?? new List<InventoryCountLine>());
    }

    private static InventoryCountDto MapToDto(InventoryCount count, IReadOnlyCollection<InventoryCountLine>? lines)
    {
        return new InventoryCountDto(
            count.Id,
            count.CountNo,
            count.WarehouseId,
            null, // WarehouseName — not loaded
            (byte)count.Status,
            GetStatusName(count.Status),
            count.Notes,
            count.CreatedAt,
            count.CreatedByUserId,
            lines?.Select(l => new InventoryCountLineDto(
                l.Id,
                l.InventoryCountId,
                l.ProductUnitId,
                null, // ProductUnitName — not loaded
                l.ExpectedQuantity,
                l.ActualQuantity,
                l.Difference,
                l.Notes
            )).ToList()
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
