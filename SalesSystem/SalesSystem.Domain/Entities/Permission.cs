using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a system permission (e.g., "Sales.View", "Products.Edit").
/// Each permission can be assigned to one or more roles via RolePermission.
/// System permissions (IsSystem = true) cannot be deleted or modified.
/// </summary>
public class Permission : BaseEntity
{
    public string Name { get; private set; } = string.Empty;               // "Sales.View"
    public string DisplayNameAr { get; private set; } = string.Empty;      // "عرض فواتير البيع"
    public string? Category { get; private set; }                          // "Sales", "Purchase", etc.
    public bool IsSystem { get; private set; }                             // System permissions are protected

    // Navigation
    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    protected Permission() { } // EF Core

    public static Permission Create(string name, string displayNameAr, string? category = null, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الصلاحية مطلوب.");
        if (string.IsNullOrWhiteSpace(displayNameAr))
            throw new DomainException("الاسم العربي للصلاحية مطلوب.");

        return new Permission
        {
            Name = name.Trim(),
            DisplayNameAr = displayNameAr.Trim(),
            Category = category?.Trim(),
            IsSystem = isSystem
        };
    }
}
