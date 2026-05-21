using Microsoft.EntityFrameworkCore;
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
        var query = _uow.Units.Query();
        
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.Name.Contains(search) ||
                                    (u.Symbol != null && u.Symbol.Contains(search)));
        }

        var totalItems = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result<PagedResult<UnitDto>>.Success(PagedResult<UnitDto>.Create(dtos, totalItems, page, pageSize));
    }

    public async Task<Result<UnitDto>> CreateAsync(CreateUnitRequest request, CancellationToken ct)
    {
        try
        {
            if (await _uow.Units.Query().AnyAsync(u => u.Name == request.Name, ct))
                return Result<UnitDto>.Failure("اسم الوحدة مستخدم بالفعل", ErrorCodes.DuplicateCode);

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
            var unit = await _uow.Units.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);
            if (unit == null)
                return Result<UnitDto>.Failure("الوحدة غير موجودة", ErrorCodes.NotFound);

            if (await _uow.Units.Query().AnyAsync(u => u.Name == request.Name && u.Id != id, ct))
                return Result<UnitDto>.Failure("اسم الوحدة مستخدم بالفعل", ErrorCodes.DuplicateCode);

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

        if (await _uow.Products.Query().AnyAsync(p => p.UnitId == id, ct))
            return Result.Failure("لا يمكن حذف الوحدة لأنها مرتبطة بمنتجات");

        await _uow.Units.SoftDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Unit soft-deleted: {UnitId}", id);
        return Result.Success();
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        var unit = await _uow.Units.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);
        if (unit == null)
            return Result.Failure("الوحدة غير موجودة", ErrorCodes.NotFound);

        if (await _uow.Products.Query().AnyAsync(p => p.UnitId == id || p.RetailUnitId == id || p.WholesaleUnitId == id, ct))
            return Result.Failure("لا يمكن حذف الوحدة نهائياً لأنها مرتبطة بمنتجات");

        await _uow.Units.HardDeleteAsync(id, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Unit permanently deleted: {UnitId}", id);
        return Result.Success();
    }

    private static UnitDto MapToDto(Unit u)
    {
        return new UnitDto(u.Id, u.Name, u.Symbol, u.IsActive);
    }
}
