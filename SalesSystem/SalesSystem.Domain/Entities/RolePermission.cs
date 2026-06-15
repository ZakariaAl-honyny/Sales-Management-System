using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Join entity linking a Role to a Permission (many-to-many).
/// Determines which permissions are granted to each role.
/// Unique constraint: (RoleId, PermissionId).
/// </summary>
public class RolePermission : Entity
{
    public int RoleId { get; private set; }
    public Role Role { get; private set; } = null!;
    public int PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    protected RolePermission() { } // EF Core

    public static RolePermission Create(int roleId, int permissionId)
    {
        return new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId
        };
    }
}
