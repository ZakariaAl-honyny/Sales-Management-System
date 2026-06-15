using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class WarehouseTransferService : IWarehouseTransferService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _documentSequence;
    private readonly ILogger<WarehouseTransferService> _logger;

    public WarehouseTransferService(
        IUnitOfWork uow,
        IDocumentSequenceService documentSequence,
        ILogger<WarehouseTransferService> logger)
    {
        _uow = uow;
        _documentSequence = documentSequence;
        _logger = logger;
    }

    public async Task<Result<WarehouseTransferDto>> CreateAsync(CreateWarehouseTransferRequest request, int userId, CancellationToken ct)
    {
        try
        {
            var transferNo = await GetNextTransferNumberAsync(ct);
            if (transferNo <= 0)
                return Result<WarehouseTransferDto>.Failure("فشل في توليد رقم التحويل");

            var transfer = WarehouseTransfer.Create(
                transferNo,
                request.FromWarehouseId,
                request.ToWarehouseId,
                request.TransferDate,
                request.Notes,
                userId);

            if (request.Lines != null)
            {
                foreach (var lineReq in request.Lines)
                {
                    var line = WarehouseTransferLine.Create(
                        transfer.Id,
                        lineReq.ProductId,
                        lineReq.BatchId ?? 0,
                        lineReq.Quantity,
                        lineReq.UnitCost);
                    transfer.AddLine(line);
                }
            }

            if (!transfer.Lines.Any())
                return Result<WarehouseTransferDto>.Failure("يجب إضافة صنف واحد على الأقل للتحويل");

            await _uow.WarehouseTransfers.AddAsync(transfer, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("WarehouseTransfer created (No: {TransferNo}, ID: {Id}) by User {UserId}",
                transfer.TransferNo, transfer.Id, userId);

            var created = await LoadTransferWithIncludesAsync(transfer.Id, ct);
            return Result<WarehouseTransferDto>.Success(MapToDto(created!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation creating warehouse transfer: {Message}", ex.Message);
            return Result<WarehouseTransferDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating warehouse transfer");
            return Result<WarehouseTransferDto>.Failure("حدث خطأ أثناء إنشاء التحويل");
        }
    }

    public async Task<Result<WarehouseTransferDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        try
        {
            var transfer = await LoadTransferWithIncludesAsync(id, ct);
            if (transfer == null)
                return Result<WarehouseTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound);

            return Result<WarehouseTransferDto>.Success(MapToDto(transfer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warehouse transfer {Id}", id);
            return Result<WarehouseTransferDto>.Failure("حدث خطأ أثناء استرجاع بيانات التحويل");
        }
    }

    public async Task<Result<PagedResult<WarehouseTransferDto>>> GetAllAsync(
        int? fromWarehouseId, int? toWarehouseId, int page, int pageSize, CancellationToken ct)
    {
        try
        {
            System.Linq.Expressions.Expression<Func<WarehouseTransfer, bool>>? predicate = null;

            if (fromWarehouseId.HasValue || toWarehouseId.HasValue)
            {
                predicate = t =>
                    (!fromWarehouseId.HasValue || t.FromWarehouseId == fromWarehouseId.Value) &&
                    (!toWarehouseId.HasValue || t.ToWarehouseId == toWarehouseId.Value);
            }

            var (items, totalCount) = await _uow.WarehouseTransfers.GetPagedAsync(
                predicate,
                orderConfig: q => q.OrderByDescending(t => t.TransferDate).ThenByDescending(t => t.Id),
                page,
                pageSize,
                ct,
                includePaths: new[] { "FromWarehouse", "ToWarehouse", "Lines", "Lines.Product", "Lines.Batch" });

            var dtos = items.Select(MapToDto).ToList();
            var result = PagedResult<WarehouseTransferDto>.Create(dtos, totalCount, page, pageSize);

            return Result<PagedResult<WarehouseTransferDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving warehouse transfers list");
            return Result<PagedResult<WarehouseTransferDto>>.Failure("حدث خطأ أثناء استرجاع قائمة التحويلات");
        }
    }

    public async Task<Result<WarehouseTransferDto>> PostAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var transfer = await LoadTransferWithIncludesAsync(id, ct);
            if (transfer == null)
                return Result<WarehouseTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound);

            if (transfer.Status != InvoiceStatus.Draft)
                return Result<WarehouseTransferDto>.Failure("فقط التحويلات المسودة يمكن ترحيلها");

            // Validate stock availability before posting
            foreach (var line in transfer.Lines)
            {
                var sourceStock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
                    ws => ws.ProductId == line.ProductId && ws.WarehouseId == transfer.FromWarehouseId, ct);

                var availableQty = sourceStock?.Quantity ?? 0;
                if (availableQty < line.Quantity)
                {
                    var productName = line.Product?.Name ?? $"منتج {line.ProductId}";
                    return Result<WarehouseTransferDto>.Failure(
                        $"الكمية غير كافية للمنتج '{productName}' في المستودع المصدر. " +
                        $"المتاح: {availableQty}, المطلوب: {line.Quantity}");
                }
            }

            await _uow.ExecuteTransactionAsync(async () =>
            {
                // Post the transfer
                transfer.Post();

                // Execute stock movements
                foreach (var line in transfer.Lines)
                {
                    // 1. Deduct from source warehouse
                    var sourceStock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
                        ws => ws.ProductId == line.ProductId && ws.WarehouseId == transfer.FromWarehouseId, ct);

                    if (sourceStock != null)
                    {
                        sourceStock.DecreaseQuantity(line.Quantity);
                    }

                    // 2. Add to destination warehouse
                    var destStock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
                        ws => ws.ProductId == line.ProductId && ws.WarehouseId == transfer.ToWarehouseId, ct);

                    if (destStock == null)
                    {
                        destStock = WarehouseStock.Create(
                            transfer.ToWarehouseId,
                            line.ProductId,
                            line.Quantity,
                            userId);
                        await _uow.WarehouseStocks.AddAsync(destStock, ct);
                    }
                    else
                    {
                        destStock.IncreaseQuantity(line.Quantity);
                    }

                }

                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation("WarehouseTransfer {Id} posted by User {UserId}", id, userId);

            var posted = await LoadTransferWithIncludesAsync(id, ct);
            return Result<WarehouseTransferDto>.Success(MapToDto(posted!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation posting warehouse transfer {Id}: {Message}", id, ex.Message);
            return Result<WarehouseTransferDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting warehouse transfer {Id}", id);
            return Result<WarehouseTransferDto>.Failure("حدث خطأ أثناء ترحيل التحويل");
        }
    }

    public async Task<Result<WarehouseTransferDto>> CancelAsync(int id, int userId, CancellationToken ct)
    {
        try
        {
            var transfer = await LoadTransferWithIncludesAsync(id, ct);
            if (transfer == null)
                return Result<WarehouseTransferDto>.Failure("التحويل غير موجود", ErrorCodes.NotFound);

            if (transfer.Status == InvoiceStatus.Cancelled)
                return Result<WarehouseTransferDto>.Failure("التحويل ملغي بالفعل");

            var wasPosted = transfer.Status == InvoiceStatus.Posted;

            await _uow.ExecuteTransactionAsync(async () =>
            {
                transfer.Cancel();

                // If posted, reverse the stock movements
                if (wasPosted)
                {
                    foreach (var line in transfer.Lines)
                    {
                        // 1. Add back to source warehouse
                        var sourceStock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
                            ws => ws.ProductId == line.ProductId && ws.WarehouseId == transfer.FromWarehouseId, ct);

                        if (sourceStock != null)
                        {
                            sourceStock.IncreaseQuantity(line.Quantity);
                        }

                        // 2. Deduct from destination warehouse
                        var destStock = await _uow.WarehouseStocks.FirstOrDefaultAsync(
                            ws => ws.ProductId == line.ProductId && ws.WarehouseId == transfer.ToWarehouseId, ct);

                        if (destStock != null)
                        {
                            destStock.DecreaseQuantity(line.Quantity);
                        }
                    }
                }

                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation("WarehouseTransfer {Id} cancelled by User {UserId}", id, userId);

            var cancelled = await LoadTransferWithIncludesAsync(id, ct);
            return Result<WarehouseTransferDto>.Success(MapToDto(cancelled!));
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation cancelling warehouse transfer {Id}: {Message}", id, ex.Message);
            return Result<WarehouseTransferDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling warehouse transfer {Id}", id);
            return Result<WarehouseTransferDto>.Failure("حدث خطأ أثناء إلغاء التحويل");
        }
    }

    // ─── Private Helpers ─────────────────────────────────

    private async Task<int> GetNextTransferNumberAsync(CancellationToken ct)
    {
        var seqResult = await _documentSequence.GetNextIntAsync("WarehouseTransfer", ct);
        return seqResult.IsSuccess ? seqResult.Value : 0;
    }

    private async Task<WarehouseTransfer?> LoadTransferWithIncludesAsync(int id, CancellationToken ct)
    {
        return await _uow.WarehouseTransfers.FirstOrDefaultAsync(
            t => t.Id == id, ct,
            "FromWarehouse", "ToWarehouse",
            "Lines", "Lines.Product", "Lines.Batch");
    }

    private static WarehouseTransferDto MapToDto(WarehouseTransfer transfer)
    {
        return new WarehouseTransferDto(
            transfer.Id,
            transfer.TransferNo,
            transfer.FromWarehouseId,
            transfer.FromWarehouse?.Name,
            transfer.ToWarehouseId,
            transfer.ToWarehouse?.Name,
            transfer.TransferDate,
            transfer.Notes,
            (byte)transfer.Status,
            transfer.Lines.Select(l => new WarehouseTransferLineDto(
                l.Id,
                l.ProductId,
                l.Product?.Name,
                l.BatchId,
                l.Batch?.BatchNo,
                l.Quantity,
                l.UnitCost,
                l.TotalCost
            )).ToList()
        );
    }
}
