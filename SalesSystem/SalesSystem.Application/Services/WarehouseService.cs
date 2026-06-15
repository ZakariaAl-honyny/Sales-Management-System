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

    public WarehouseService(IUnitOfWork uow, ILogger<WarehouseService> logger)
    {
        _uow = uow;
        _logger = logger;
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
        System.Linq.Expressions.Expression<System.Func<Warehouse, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search;
            predicate = w => w.Name.Contains(s);
        }

        var (items, total) = await _uow.Warehouses.GetPagedAsync(
            predicate, q => q.OrderBy(w => w.Name), page, pageSize, ct, includeInactive);

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<WarehouseDto>>.Success(PagedResult<WarehouseDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest request, CancellationToken ct)
    {
        var warehouse = Warehouse.Create(
            request.BranchId,
            request.Name,
            request.Phone,
            request.Address,
            request.Notes
        );

        await _uow.Warehouses.AddAsync(warehouse, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse created: {Name} (ID: {Id})", warehouse.Name, warehouse.Id);
        return Result<WarehouseDto>.Success(MapToDto(warehouse));
    }

    public async Task<Result<WarehouseDto>> UpdateAsync(int id, UpdateWarehouseRequest request, CancellationToken ct)
    {
        var warehouse = await _uow.Warehouses.FirstOrDefaultIgnoreFiltersAsync(w => w.Id == id, ct);
        if (warehouse == null)
            return Result<WarehouseDto>.Failure("المخزن غير موجود", ErrorCodes.NotFound);

        warehouse.Update(
            request.BranchId,
            request.Name,
            request.Phone,
            request.Address,
            request.Notes
        );

        if (request.IsActive != warehouse.IsActive)
        {
            if (request.IsActive) warehouse.Restore();
            else warehouse.MarkAsDeleted();
        }

        await _uow.Warehouses.UpdateAsync(warehouse, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse updated: {Name} (ID: {Id})", warehouse.Name, warehouse.Id);
        return Result<WarehouseDto>.Success(MapToDto(warehouse));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var warehouse = await _uow.Warehouses.GetByIdAsync(id, ct);
        if (warehouse == null)
            return Result.Failure("المخزن غير موجود", ErrorCodes.NotFound);

        await _uow.Warehouses.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Warehouse soft-deleted: {Id}", id);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        var warehouse = await _uow.Warehouses.FirstOrDefaultIgnoreFiltersAsync(w => w.Id == id, ct);
        if (warehouse == null)
            return Result.Failure("المخزن غير موجود", ErrorCodes.NotFound);

        if (await _uow.WarehouseStocks.AnyAsync(ws => ws.WarehouseId == id, ct))
            return Result.Failure("لا يمكن حذف المخزن نهائياً لأنه يحتوي على مخزون");

        if (await _uow.WarehouseTransfers.AnyAsync(st => st.FromWarehouseId == id || st.ToWarehouseId == id, ct))
            return Result.Failure("لا يمكن حذف المخزن نهائياً لأنه مرتبط بتحويلات مخزون");

        try
        {
            await _uow.Warehouses.HardDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Warehouse permanently deleted: {Id}", id);
            return Result.Success();
        }
        catch (Exception ex) when (IsDbUpdateException(ex))
        {
            _logger.LogError(ex, "Failed to permanently delete warehouse {Id} due to database constraint", id);
            return Result.Failure("لا يمكن حذف المخزن نهائياً. قد يكون مرتبطاً ببيانات أخرى في النظام.");
        }
    }

    private static WarehouseDto MapToDto(Warehouse w)
    {
        return new WarehouseDto(
            w.Id,
            w.Name,
            w.Phone,
            w.Address,
            w.Notes,
            w.IsActive
        );
    }

    private static bool IsDbUpdateException(Exception ex)
    {
        var typeName = ex.GetType().FullName ?? "";
        return typeName.Contains("DbUpdateException", StringComparison.Ordinal);
    }
}
