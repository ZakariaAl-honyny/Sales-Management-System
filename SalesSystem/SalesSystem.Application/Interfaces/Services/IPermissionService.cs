using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for managing permissions and role-permission assignments.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Returns all active permissions, ordered by Category then Name.
    /// </summary>
    Task<Result<IReadOnlyList<PermissionDto>>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a dictionary mapping each Role to the list of assigned permission IDs.
    /// </summary>
    Task<Result<Dictionary<byte, List<int>>>> GetRolePermissionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the permission set for a given role.
    /// Replaces all existing role permissions with the new set.
    /// </summary>
    Task<Result> UpdateRolePermissionsAsync(byte role, List<int> permissionIds, CancellationToken ct = default);

    /// <summary>
    /// Gets the list of permission names for a user based on their role.
    /// </summary>
    Task<Result<List<string>>> GetUserPermissionsAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has a specific permission (by name) based on their role.
    /// </summary>
    Task<bool> UserHasPermissionAsync(int userId, string permissionName, CancellationToken ct = default);
}
