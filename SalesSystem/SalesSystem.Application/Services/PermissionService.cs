using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Services;

/// <summary>
/// Service for managing permissions and role-permission assignments.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(IUnitOfWork uow, ILogger<PermissionService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PermissionDto>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var permissions = await _uow.Permissions.ToListAsync(ct: ct);

            var dtos = permissions
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .Select(p => new PermissionDto(
                    p.Id,
                    p.Name,
                    p.DisplayNameAr,
                    p.Category,
                    p.IsActive))
                .ToList();

            return Result<IReadOnlyList<PermissionDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all permissions");
            return Result<IReadOnlyList<PermissionDto>>.Failure("حدث خطأ أثناء جلب الصلاحيات.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Dictionary<UserRole, List<int>>>> GetRolePermissionsAsync(CancellationToken ct = default)
    {
        try
        {
            var rolePermissions = await _uow.RolePermissions.ToListAsync(
                includePaths: new[] { "Permission" });

            var result = new Dictionary<UserRole, List<int>>();

            foreach (UserRole role in Enum.GetValues<UserRole>())
            {
                if (role == 0) continue; // Skip undefined
                result[role] = rolePermissions
                    .Where(rp => rp.Role == role && rp.Permission.IsActive)
                    .Select(rp => rp.PermissionId)
                    .Distinct()
                    .ToList();
            }

            return Result<Dictionary<UserRole, List<int>>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get role permissions");
            return Result<Dictionary<UserRole, List<int>>>.Failure("حدث خطأ أثناء جلب صلاحيات الأدوار.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateRolePermissionsAsync(UserRole role, List<int> permissionIds, CancellationToken ct = default)
    {
        try
        {
            // 1. Get existing role permissions (read — outside transaction)
            var existing = await _uow.RolePermissions.ToListAsync(
                rp => rp.Role == role,
                ct: ct);

            // 2. Execute atomic write operations inside a transaction (RULE-317)
            await _uow.ExecuteTransactionAsync(async () =>
            {
                // Remove permissions that are no longer assigned
                var toRemove = existing.Where(rp => !permissionIds.Contains(rp.PermissionId)).ToList();
                if (toRemove.Any())
                    _uow.RolePermissions.DeleteRange(toRemove);

                // Add new permissions
                var existingIds = existing.Select(rp => rp.PermissionId).ToHashSet();
                var toAdd = permissionIds.Where(id => !existingIds.Contains(id)).ToList();

                foreach (var permissionId in toAdd)
                {
                    var rp = RolePermission.Create(role, permissionId);
                    await _uow.RolePermissions.AddAsync(rp, ct);
                }

                await _uow.SaveChangesAsync(ct);
            }, ct);

            _logger.LogInformation("Updated permissions for role {Role}: {Count} permissions assigned",
                role, permissionIds.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update role permissions for {Role}", role);
            return Result.Failure("حدث خطأ أثناء تحديث الصلاحيات.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<string>>> GetUserPermissionsAsync(int userId, CancellationToken ct = default)
    {
        try
        {
            var user = await _uow.Users.GetByIdAsync(userId, ct);
            if (user == null)
                return Result<List<string>>.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

            // Get all permission names for the user's role
            var rolePermissions = await _uow.RolePermissions.ToListAsync(
                rp => rp.Role == user.Role,
                includePaths: new[] { "Permission" });

            var permissionNames = rolePermissions
                .Where(rp => rp.Permission.IsActive)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToList();

            return Result<List<string>>.Success(permissionNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for user {UserId}", userId);
            return Result<List<string>>.Failure("حدث خطأ أثناء جلب صلاحيات المستخدم.");
        }
    }

    /// <inheritdoc />
    public async Task<bool> UserHasPermissionAsync(int userId, string permissionName, CancellationToken ct = default)
    {
        try
        {
            var permissionsResult = await GetUserPermissionsAsync(userId, ct);
            if (!permissionsResult.IsSuccess || permissionsResult.Value == null)
                return false;

            return permissionsResult.Value.Contains(permissionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check permission {Permission} for user {UserId}", permissionName, userId);
            return false;
        }
    }
}
