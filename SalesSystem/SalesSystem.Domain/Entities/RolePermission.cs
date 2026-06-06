using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Join entity linking a UserRole to a Permission.
/// Determines which permissions are granted to each role.
/// </summary>
public class RolePermission : BaseEntity
{
    public UserRole Role { get; private set; }
    public int PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    protected RolePermission() { } // EF Core

    public static RolePermission Create(UserRole role, int permissionId)
    {
        return new RolePermission
        {
            Role = role,
            PermissionId = permissionId
        };
    }
}
