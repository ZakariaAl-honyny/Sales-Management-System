using SalesSystem.Contracts.Common;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for bitmask-based permission checking.
/// Permissions are stored as a BIGINT bitmask on both User and Role entities.
/// Super Admin = PermissionsMask == -1 (all bits set — bypasses all checks).
/// Permission check formula: <c>(User.PermissionsMask &amp; RequiredPermission) == RequiredPermission</c>.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Checks if a user has a specific permission using bitwise AND.
    /// Super admin (PermissionsMask == -1) always returns true.
    /// </summary>
    Task<bool> HasPermissionAsync(int userId, long requiredPermission, CancellationToken ct = default);

    /// <summary>
    /// Gets the user's PermissionsMask value.
    /// Returns 0 if the user does not exist.
    /// </summary>
    Task<long> GetPermissionsMaskAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Sets a role's PermissionsMask to the given value.
    /// </summary>
    Task<Result> SetRolePermissionsMaskAsync(short roleId, long mask, CancellationToken ct = default);

    /// <summary>
    /// Gets all roles with their PermissionsMask values.
    /// </summary>
    Task<Result<Dictionary<short, long>>> GetAllRoleMasksAsync(CancellationToken ct = default);

    /// <summary>
    /// Assigns a role to a user by copying the role's PermissionsMask to the user.
    /// </summary>
    Task<Result> AssignRoleToUserAsync(int userId, short roleId, CancellationToken ct = default);

    /// <summary>
    /// Gets the list of permission code strings for a user (for Desktop API).
    /// Converts the user's PermissionsMask into a list of codes by testing each bit.
    /// </summary>
    Task<Result<List<string>>> GetUserPermissionsAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has a specific permission by code name (e.g., "Sales.View").
    /// </summary>
    Task<bool> UserHasPermissionAsync(int userId, string permissionName, CancellationToken ct = default);
}
