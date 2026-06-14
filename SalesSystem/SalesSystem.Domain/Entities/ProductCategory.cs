using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a product category with hierarchical self-referencing support.
/// Map to "ProductCategories" table in the database schema.
/// </summary>
public class ProductCategory : ActivatableEntity
{
    /// <summary>
    /// Category display name (Arabic, e.g., "مشروبات", "منظفات").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Category display name (English, optional).
    /// </summary>
    public string? NameEn { get; private set; }

    /// <summary>
    /// Optional description of the category.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Display sort order (ascending).
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// FK to the parent category. Null for root-level categories.
    /// </summary>
    public int? ParentId { get; private set; }

    // ─── Navigation Properties ────────────────────────────

    /// <summary>
    /// Parent category (self-referencing).
    /// </summary>
    public virtual ProductCategory? Parent { get; private set; }

    /// <summary>
    /// Child categories in the hierarchy.
    /// </summary>
    private readonly List<ProductCategory> _children = new();
    public IReadOnlyCollection<ProductCategory> Children => _children.AsReadOnly();

    /// <summary>
    /// Products belonging to this category.
    /// </summary>
    private readonly List<Product> _products = new();
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private ProductCategory() { } // EF Core

    // ─── Factory ─────────────────────────────────────────

    /// <summary>
    /// Creates a new product category.
    /// </summary>
    /// <param name="name">Category name (Arabic, required).</param>
    /// <param name="nameEn">Optional category name in English.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="sortOrder">Display sort order (default 0).</param>
    /// <param name="parentId">Optional FK to parent category. Null for root level.</param>
    /// <param name="createdByUserId">User who created this record.</param>
    /// <returns>The newly created ProductCategory entity.</returns>
    public static ProductCategory Create(
        string name,
        string? nameEn = null,
        string? description = null,
        int sortOrder = 0,
        int? parentId = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم التصنيف مطلوب.");

        if (parentId.HasValue && parentId.Value <= 0)
            throw new DomainException("معرف التصنيف الأب غير صحيح.");
        if (sortOrder < 0)
            throw new DomainException("ترتيب العرض لا يمكن أن يكون سالباً.");

        var category = new ProductCategory
        {
            Name = name.Trim(),
            NameEn = nameEn?.Trim(),
            Description = description?.Trim(),
            SortOrder = sortOrder,
            ParentId = parentId,
            IsActive = true
        };
        category.SetCreatedBy(createdByUserId);
        return category;
    }

    // ─── Domain Methods ──────────────────────────────────

    /// <summary>
    /// Updates the category properties. Throws if name is empty.
    /// </summary>
    public void Update(
        string name,
        string? nameEn = null,
        string? description = null,
        int sortOrder = 0,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم التصنيف مطلوب.");
        if (sortOrder < 0)
            throw new DomainException("ترتيب العرض لا يمكن أن يكون سالباً.");

        Name = name.Trim();
        NameEn = nameEn?.Trim();
        Description = description?.Trim();
        SortOrder = sortOrder;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Returns true if this category has any child categories.
    /// </summary>
    public bool HasChildren() => _children.Count > 0;

    /// <summary>
    /// Returns true if this category has any products assigned.
    /// </summary>
    public bool HasProducts() => _products.Count > 0;

    /// <summary>
    /// Soft-deletes the category. Throws if it has children or products.
    /// </summary>
    public override void MarkAsDeleted()
    {
        if (HasChildren())
            throw new DomainException("لا يمكن حذف تصنيف رئيسي — لديه تصنيفات فرعية.");

        if (HasProducts())
            throw new DomainException("لا يمكن حذف التصنيف — لديه منتجات مرتبطة.");

        IsActive = false;
        UpdateTimestamp();
    }
}
