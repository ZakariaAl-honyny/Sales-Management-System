using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a user role (e.g., Admin, Manager, Cashier).
/// Replaces the UserRole enum with a database-driven role entity.
/// Roles are assigned to users via the UserRole join table.
/// Permissions are granted to roles via the RolePermission join table.
///
/// NOTE: Spec suggests SMALLINT Id, but our BaseEntity convention uses int.
///       The seed data will populate roles with Id values 1–5 matching
///       the legacy UserRole enum (Admin=1, Manager=2, Cashier=3, Observer=4, BranchManager=5).
/// </summary>
public class Role : ActivatableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // ─── Navigation ─────────────────────────────────
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    protected Role() { } // EF Core

    /// <summary>
    /// Creates a new role.
    /// </summary>
    public static Role Create(string name, string? description = null, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الدور مطلوب.");

        var role = new Role
        {
            Name = name.Trim(),
            Description = description?.Trim()
        };
        role.SetCreatedBy(createdByUserId);
        return role;
    }

    /// <summary>
    /// Updates the role's name and description.
    /// </summary>
    public void Update(string name, string? description = null, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الدور مطلوب.");
        Name = name.Trim();
        Description = description?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    public override void MarkAsDeleted()
    {
        // Cannot delete system roles (Id 1–5)
        if (Id <= 5)
            throw new DomainException("لا يمكن حذف دور نظام — الدور محمي.");
        base.MarkAsDeleted();
    }
}
