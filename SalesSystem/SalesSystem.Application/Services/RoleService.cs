using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Entities;

namespace SalesSystem.Application.Services;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RoleService> _logger;

    public RoleService(IUnitOfWork uow, ILogger<RoleService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<RoleDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var role = await _uow.Roles.GetByIdAsync(id, ct);
        if (role == null)
            return Result<RoleDto>.Failure("الدور غير موجود", ErrorCodes.NotFound);

        return Result<RoleDto>.Success(MapToDto(role));
    }

    public async Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        List<Role> roles;
        if (includeInactive)
        {
            roles = await _uow.Roles.ToListIgnoreFiltersAsync(ct);
        }
        else
        {
            roles = await _uow.Roles.ToListAsync(ct);
        }

        var dtos = roles.Select(MapToDto).OrderByDescending(x => x.Id).ToList();
        return Result<IReadOnlyList<RoleDto>>.Success(dtos);
    }

    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Result<RoleDto>.Failure("اسم الدور مطلوب");

            var duplicateExists = await _uow.Roles.AnyIgnoreFiltersAsync(
                r => r.Name.ToLower() == request.Name.Trim().ToLower(), ct);
            if (duplicateExists)
                return Result<RoleDto>.Failure("اسم الدور موجود بالفعل", ErrorCodes.DuplicateEntry);

            var role = Role.Create(request.Name, request.Description);

            await _uow.Roles.AddAsync(role, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Role created: {RoleName} (ID: {RoleId})", role.Name, role.Id);

            return Result<RoleDto>.Success(MapToDto(role));
        }
        catch (DomainException ex)
        {
            return Result<RoleDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating role");
            return Result<RoleDto>.Failure("حدث خطأ أثناء إضافة الدور.");
        }
    }

    public async Task<Result<RoleDto>> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken ct)
    {
        try
        {
            var role = await _uow.Roles.FirstOrDefaultIgnoreFiltersAsync(r => r.Id == id, ct);
            if (role == null)
                return Result<RoleDto>.Failure("الدور غير موجود", ErrorCodes.NotFound);

            if (await _uow.Roles.AnyIgnoreFiltersAsync(r => r.Name == request.Name && r.Id != id, ct))
                return Result<RoleDto>.Failure("اسم الدور مستخدم بالفعل", ErrorCodes.DuplicateEntry);

            role.Update(request.Name, request.Description);

            if (request.IsActive && !role.IsActive)
                role.Restore();
            else if (!request.IsActive && role.IsActive)
                role.MarkAsDeleted();

            await _uow.Roles.UpdateAsync(role, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Role updated: {RoleName} (ID: {RoleId})", role.Name, role.Id);

            return Result<RoleDto>.Success(MapToDto(role));
        }
        catch (DomainException ex)
        {
            return Result<RoleDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating role {Id}", id);
            return Result<RoleDto>.Failure("حدث خطأ أثناء تحديث بيانات الدور.");
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        try
        {
            var role = await _uow.Roles.GetByIdAsync(id, ct);
            if (role == null)
                return Result.Failure("الدور غير موجود", ErrorCodes.NotFound);

            if (id <= 5)
                return Result.Failure("لا يمكن حذف دور نظام — الدور محمي.", ErrorCodes.InvalidOperation);

            // Check if any users are assigned to this role
            var hasUsers = await _uow.UserRoles.AnyAsync(ur => ur.RoleId == id, ct);
            if (hasUsers)
                return Result.Failure("لا يمكن حذف الدور لأنه مرتبط بمستخدمين. قم بإزالة تعيين المستخدمين أولاً.");

            await _uow.Roles.SoftDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Role soft-deleted: {RoleId}", id);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft-deleting role {Id}", id);
            return Result.Failure("حدث خطأ أثناء حذف الدور.");
        }
    }

    public async Task<Result> PermanentDeleteAsync(int id, CancellationToken ct)
    {
        var role = await _uow.Roles.FirstOrDefaultIgnoreFiltersAsync(r => r.Id == id, ct);
        if (role == null)
            return Result.Failure("الدور غير موجود", ErrorCodes.NotFound);

        if (id <= 5)
            return Result.Failure("لا يمكن حذف دور نظام — الدور محمي.", ErrorCodes.InvalidOperation);

        var hasUsers = await _uow.UserRoles.AnyAsync(ur => ur.RoleId == id, ct);
        if (hasUsers)
            return Result.Failure("لا يمكن حذف الدور لأنه مرتبط بمستخدمين.", ErrorCodes.InvalidOperation);

        try
        {
            await _uow.Roles.HardDeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Role permanently deleted: {RoleId}", id);
            return Result.Success();
        }
        catch (Exception ex) when (ex.GetType().Name.Contains("DbUpdate") || ex.GetType().Name.Contains("Sql"))
        {
            _logger.LogError(ex, "Failed to permanently delete role {RoleId} due to database constraint", id);
            return Result.Failure("لا يمكن حذف الدور نهائياً. قد يكون مرتبطاً ببيانات أخرى في النظام.");
        }
    }

    private static RoleDto MapToDto(Role r)
    {
        return new RoleDto(r.Id, r.Name, r.Description, r.IsActive);
    }
}
