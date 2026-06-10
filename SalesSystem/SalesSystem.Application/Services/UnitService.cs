using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class UnitService : IUnitService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UnitService> _logger;

    public UnitService(IUnitOfWork uow, ILogger<UnitService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<UnitDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var unit = await _uow.Units.GetByIdAsync(id, ct);
        if (unit == null)
            return Result<UnitDto>.Failure("الوحدة غير موجودة", ErrorCodes.NotFound);

        return Result<UnitDto>.Success(MapToDto(unit));
    }

    public async Task<Result<PagedResult<UnitDto>>> GetAllAsync(string? search, int page, int pageSize, bool includeInactive = false, CancellationToken ct = default)
    {
        System.Linq.Expressions.Expression<System.Func<Unit, bool>>? predicate = null;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search;
            predicate = u => u.Name.Contains(s) || (u.Symbol != null && u.Symbol.Contains(s));
        }

        var (items, total) = await _uow.Units.GetPagedAsync(
            predicate, q => q.OrderBy(u => u.Name), page, pageSize, ct, includeInactive);

        var dtos = items.Select(MapToDto).ToList();
        return Result<PagedResult<UnitDto>>.Success(PagedResult<UnitDto>.Create(dtos, total, page, pageSize));
    }

    public async Task<Result<UnitDto>> CreateAsync(CreateUnitRequest request, CancellationToken ct)
    {
        try
        {
            if (await _uow.Units.AnyAsync(u => u.Name == request.Name, ct))
                return Result<UnitDto>.Failure("اسم الوحدة مستخدم بالفعل", "DUPLICATE_UNIT_NAME");

            var unit = Unit.Create(request.Name, request.Symbol, null);

            await _uow.Units.AddAsync(unit, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Unit created: {UnitName} (ID: {UnitId})", unit.Name, unit.Id);

            return Result<UnitDto>.Success(MapToDto(unit));
        }
        catch (DomainException ex)
        {
            return Result<UnitDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating unit");
            return Result<UnitDto>.Failure("حدث خطأ أثناء إضافة الوحدة.");
        }
    }

    public async Task<Result<UnitDto>> UpdateAsync(int id, UpdateUnitRequest request, CancellationToken ct)
    {
        try
        {
            var unit = await _uow.Units.FirstOrDefaultIgnoreFiltersAsync(u => u.Id == id, ct);
            if (unit == null)
                return Result<UnitDto>.Failure("الوحدة غير موجودة", ErrorCodes.NotFound);

            if (await _uow.Units.AnyAsync(u => u.Name == request.Name && u.Id != id, ct))
                return Result<UnitDto>.Failure("اسم الوحدة مستخدم بالفعل", "DUPLICATE_UNIT_NAME");

            unit.Update(request.Name, request.Symbol, null);

            if (request.IsActive && !unit.IsActive) unit.Restore();
            else if (!request.IsActive && unit.IsActive) unit.MarkAsDeleted();

            await _uow.Units.UpdateAsync(unit, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Unit updated: {UnitName} (ID: {UnitId})", unit.Name, unit.Id);

            return Result<UnitDto>.Success(MapToDto(unit));
        }
        catch (DomainException ex)
        {
            return Result<UnitDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating unit {Id}", id);
            return Result<UnitDto>.Failure("حدث خطأ أثناء تحديث بيانات الوحدة.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var unit = await _uow.Units.GetByIdAsync(id, ct);
        if (unit == null)
            return Result.Failure("الوحدة غير موجودة", ErrorCodes.NotFound);

        if (await _uow.ProductUnits.AnyAsync(pu => pu.UnitId == id, ct))
            return Result.Failure("لا يمكن حذف الوحدة لأنها مرتبطة بمنتجات");

        await _uow.Units.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Unit soft-deleted: {UnitId}", id);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        var unit = await _uow.Units.FirstOrDefaultIgnoreFiltersAsync(u => u.Id == id, ct);
        if (unit == null)
            return Result.Failure("الوحدة غير موجودة", ErrorCodes.NotFound);

        if (await _uow.ProductUnits.AnyAsync(pu => pu.UnitId == id, ct))
            return Result.Failure("لا يمكن حذف الوحدة نهائياً لأنها مرتبطة بمنتجات");

        try
        {
            await _uow.Units.HardDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Unit permanently deleted: {UnitId}", id);
            return Result.Success();
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
        {
            _logger.LogError(ex, "Failed to permanently delete unit {UnitId} due to database constraint", id);
            return Result.Failure("لا يمكن حذف الوحدة نهائياً. قد تكون مرتبطة ببيانات أخرى في النظام.");
        }
    }

    private static UnitDto MapToDto(Unit u)
    {
        return new UnitDto(u.Id, u.Name, u.Symbol, u.IsActive);
    }
}
