using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a system permission (e.g., "Sales.View", "Products.Edit").
/// Each permission can be assigned to one or more roles via RolePermission.
/// System permissions (IsSystem = true) cannot be deleted or modified.
/// </summary>
public class Permission : ActivatableEntity
{
    public string Code { get; private set; } = string.Empty;               // "Sales.View" (was Name)
    public string DisplayName { get; private set; } = string.Empty;        // "عرض فواتير البيع" (was DisplayNameAr)
    public string Category { get; private set; } = string.Empty;           // "Sales", "Purchases", etc. (now non-nullable)
    public bool IsSystem { get; private set; }                             // System permissions are protected

    // Navigation
    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    protected Permission() { } // EF Core

    public static Permission Create(string code, string displayName, string category,
        bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("كود الصلاحية مطلوب.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("الاسم العربي للصلاحية مطلوب.");
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("تصنيف الصلاحية مطلوب.");

        return new Permission
        {
            Code = code.Trim(),
            DisplayName = displayName.Trim(),
            Category = category.Trim(),
            IsSystem = isSystem
        };
    }

    /// <summary>
    /// Returns true if this permission can be modified (non-system permissions only).
    /// System permissions (IsSystem = true) are protected and cannot be deleted or modified.
    /// </summary>
    public bool CanModify() => !IsSystem;

    /// <summary>
    /// Marks this permission as deleted.
    /// System permissions (IsSystem = true) cannot be deleted — they are protected.
    /// </summary>
    public override void MarkAsDeleted()
    {
        if (IsSystem)
            throw new DomainException("لا يمكن حذف صلاحية نظامية.");

        base.MarkAsDeleted();
    }
}
