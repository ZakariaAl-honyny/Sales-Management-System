using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class WarehouseService : IWarehouseService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WarehouseService> _logger;
    private readonly IDocumentSequenceService _sequenceService;

    public WarehouseService(IUnitOfWork uow, ILogger<WarehouseService> logger, IDocumentSequenceService sequenceService)
    {
        _uow = uow;
        _logger = logger;
        _sequenceService = sequenceService;
    }

    public async Task<Result<WarehouseDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var warehouse = await _uow.Warehouses.GetByIdAsync(id, ct);
        if (warehouse == null)
            return Result<WarehouseDto>.Failure("المخزن غير موجود", ErrorCodes.NotFound);

        return Result<WarehouseDto>.Success(MapToDto(warehouse));
    }

    public async Task<Result<PagedResult<WarehouseDto>>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = _uow.Warehouses.Query();
        
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(w => w.Name.Contains(search) ||
                                    (w.Code != null && w.Code.Contains(search)));
        }

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderBy(w => w.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<WarehouseDto>>.Success(PagedResult<WarehouseDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest request, CancellationToken ct)
    {
        string? code = request.Code;
        if (string.IsNullOrWhiteSpace(code))
        {
            var codeResult = await _sequenceService.GetNextNumberAsync("WH", ct);
            if (!codeResult.IsSuccess)
                return Result<WarehouseDto>.Failure(codeResult.Error ?? "حدث خطأ أثناء توليد الكود");
            code = codeResult.Value;
        }

        if (await _uow.Warehouses.Query().AnyAsync(w => w.Code == code, ct))
            return Result<WarehouseDto>.Failure("كود المخزن مستخدم بالفعل", ErrorCodes.DuplicateCode);

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                if (request.IsDefault)
                {
                    await UnsetOtherDefaultsAsync(ct);
                }

                var warehouse = Warehouse.Create(
                    name: request.Name,
                    code: code,
                    location: request.Location,
                    isDefault: request.IsDefault,
                    createdByUserId: null
                );

                await _uow.Warehouses.AddAsync(warehouse, ct);
                await _uow.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                _logger.LogInformation("Warehouse created: {WarehouseName} (ID: {WarehouseId}, Default: {IsDefault})",
                    warehouse.Name, warehouse.Id, warehouse.IsDefault);

                return Result<WarehouseDto>.Success(MapToDto(warehouse));
            }
            catch (DomainException ex)
            {
                return Result<WarehouseDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error creating warehouse");
                return Result<WarehouseDto>.Failure("حدث خطأ أثناء إضافة المستودع");
            }
        }, ct);
    }

    public async Task<Result<WarehouseDto>> UpdateAsync(int id, UpdateWarehouseRequest request, CancellationToken ct)
    {
        var warehouse = await _uow.Warehouses.Query().IgnoreQueryFilters().FirstOrDefaultAsync(w => w.Id == id, ct);
        if (warehouse == null)
            return Result<WarehouseDto>.Failure("المخزن غير موجود", ErrorCodes.NotFound);

        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != warehouse.Code)
        {
            if (await _uow.Warehouses.Query().AnyAsync(w => w.Code == request.Code && w.Id != id, ct))
                return Result<WarehouseDto>.Failure("كود المخزن مستخدم بالفعل", ErrorCodes.DuplicateCode);
        }

        return await _uow.ExecuteAsync(async () =>
        {
            await using var transaction = await _uow.BeginTransactionAsync(ct);
            try
            {
                if (request.IsDefault && !warehouse.IsDefault)
                {
                    await UnsetOtherDefaultsAsync(ct);
                }

                warehouse.Update(
                    name: request.Name,
                    code: request.Code,
                    location: request.Location,
                    isDefault: request.IsDefault,
                    updatedByUserId: null
                );

                if (request.IsActive != warehouse.IsActive)
                {
                    if (request.IsActive) warehouse.Restore();
                    else warehouse.MarkAsDeleted();
                }

                await _uow.Warehouses.UpdateAsync(warehouse, ct);
                await _uow.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);

                _logger.LogInformation("Warehouse updated: {WarehouseName} (ID: {WarehouseId}, Default: {IsDefault})",
                    warehouse.Name, warehouse.Id, warehouse.IsDefault);

                return Result<WarehouseDto>.Success(MapToDto(warehouse));
            }
            catch (DomainException ex)
            {
                return Result<WarehouseDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error updating warehouse {Id}", id);
                return Result<WarehouseDto>.Failure("حدث خطأ أثناء تحديث المستودع");
            }
        }, ct);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var warehouse = await _uow.Warehouses.GetByIdAsync(id, ct);
        if (warehouse == null)
            return Result.Failure("المخزن غير موجود", ErrorCodes.NotFound);

        if (warehouse.IsDefault)
            return Result.Failure("لا يمكن حذف المخزن الافتراضي");

        await _uow.Warehouses.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse soft-deleted: {WarehouseId}", id);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        var warehouse = await _uow.Warehouses.Query().IgnoreQueryFilters().FirstOrDefaultAsync(w => w.Id == id, ct);
        if (warehouse == null)
            return Result.Failure("المخزن غير موجود", ErrorCodes.NotFound);

        if (warehouse.IsDefault)
            return Result.Failure("لا يمكن حذف المخزن الافتراضي نهائياً");

        if (await _uow.WarehouseStocks.Query().AnyAsync(ws => ws.WarehouseId == id, ct))
            return Result.Failure("لا يمكن حذف المخزن نهائياً لأنه يحتوي على مخزون");

        if (await _uow.StockTransfers.Query().AnyAsync(st => st.FromWarehouseId == id || st.ToWarehouseId == id, ct))
            return Result.Failure("لا يمكن حذف المخزن نهائياً لأنه مرتبط بتحويلات مخزون");

        await _uow.Warehouses.HardDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse permanently deleted: {WarehouseId}", id);
        return Result.Success();
    }

    private async Task UnsetOtherDefaultsAsync(CancellationToken ct)
    {
        var defaults = await _uow.Warehouses.Query()
            .Where(w => w.IsDefault)
            .ToListAsync(ct);

        foreach (var w in defaults)
        {
            w.Update(
                name: w.Name,
                code: w.Code,
                location: w.Location,
                isDefault: false,
                updatedByUserId: null
            );
            await _uow.Warehouses.UpdateAsync(w, ct);
        }
    }

    private static WarehouseDto MapToDto(Warehouse w)
    {
        return new WarehouseDto(
            w.Id,
            w.Code,
            w.Name,
            w.Location,
            w.IsDefault,
            w.IsActive
        );
    }
}
