using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a user role (e.g., Admin, Manager, Cashier).
/// Replaces the UserRole enum with a database-driven role entity.
/// Roles are assigned to users via the UserRole join table.
/// Permissions are granted to roles via the RolePermission join table.
///
/// 9 system roles seeded: Admin(1), Manager(2), Accountant(3), Treasurer(4),
/// Cashier(5), WarehouseSupervisor(6), SalesEmployee(7), Observer(8), BranchManager(9).
/// </summary>
public class Role : ActivatableEntity
{
    /// <summary>
    /// Schema: smallint PK (short).
    /// </summary>
    public new short Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>
    /// Bitmask of all permissions granted to this role.
    /// Derived from the RolePermission join table values; used for fast lookups.
    /// 0 = no permissions, -1 = all permissions (super admin).
    /// </summary>
    public long PermissionsMask { get; private set; }

    // ─── Navigation ─────────────────────────────────
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    protected Role() { } // EF Core

    /// <summary>
    /// Creates a new role.
    /// </summary>
    public static Role Create(string name, string? description = null,
        long permissionsMask = 0, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الدور مطلوب.");

        var role = new Role
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            PermissionsMask = permissionsMask
        };
        role.SetCreatedBy(createdByUserId);
        return role;
    }

    /// <summary>
    /// Updates the role's name and description.
    /// </summary>
    public void Update(string name, string? description = null,
        long? permissionsMask = null, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الدور مطلوب.");
        Name = name.Trim();
        Description = description?.Trim();
        if (permissionsMask.HasValue)
            PermissionsMask = permissionsMask.Value;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Sets the permissions bitmask for this role.
    /// Use -1 to grant all permissions (super admin).
    /// </summary>
    public void SetPermissionsMask(long mask)
    {
        PermissionsMask = mask;
        UpdateTimestamp();
    }

    public override void MarkAsDeleted()
    {
        // Cannot delete system roles (Id 1–9)
        if (Id <= 9)
            throw new DomainException("لا يمكن حذف دور نظام — الدور محمي.");
        base.MarkAsDeleted();
    }
}
