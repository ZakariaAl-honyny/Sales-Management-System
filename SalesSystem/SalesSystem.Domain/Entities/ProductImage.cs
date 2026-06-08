using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents an image associated with a product.
/// Each product can have multiple images, with one designated as primary.
/// </summary>
public class ProductImage : BaseEntity
{
    /// <summary>
    /// FK to Product.
    /// </summary>
    public int ProductId { get; private set; }

    /// <summary>
    /// File path or URL to the image.
    /// </summary>
    public string ImagePath { get; private set; } = string.Empty;

    /// <summary>
    /// Indicates whether this is the primary (main display) image.
    /// Only one image per product can be primary.
    /// </summary>
    public bool IsPrimary { get; private set; }

    /// <summary>
    /// Display sort order (ascending).
    /// </summary>
    public int SortOrder { get; private set; }

    // ─── Navigation Properties ──────────────────────────

    public Product? Product { get; private set; }

    private ProductImage() { } // EF Core

    // ─── Factory ──────────────────────────────────

    /// <summary>
    /// Creates a new product image record.
    /// </summary>
    public static ProductImage Create(
        int productId,
        string imagePath,
        bool isPrimary = false,
        int sortOrder = 0,
        int? createdByUserId = null)
    {
        if (productId <= 0)
            throw new DomainException("معرف المنتج مطلوب.");
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new DomainException("مسار الصورة مطلوب.");
        if (sortOrder < 0)
            throw new DomainException("ترتيب العرض لا يمكن أن يكون سالباً.");

        var image = new ProductImage
        {
            ProductId = productId,
            ImagePath = imagePath.Trim(),
            IsPrimary = isPrimary,
            SortOrder = sortOrder,
            IsActive = true
        };
        image.SetCreatedBy(createdByUserId);
        return image;
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Marks this image as the primary image for the product.
    /// </summary>
    public void SetPrimary()
    {
        IsPrimary = true;
        UpdateTimestamp();
    }

    /// <summary>
    /// Unsets this image as the primary image.
    /// </summary>
    public void UnsetPrimary()
    {
        IsPrimary = false;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the sort order of this image.
    /// </summary>
    public void UpdateSortOrder(int newSortOrder)
    {
        if (newSortOrder < 0)
            throw new DomainException("ترتيب العرض لا يمكن أن يكون سالباً.");

        SortOrder = newSortOrder;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the image path.
    /// </summary>
    public void UpdateImagePath(string newImagePath)
    {
        if (string.IsNullOrWhiteSpace(newImagePath))
            throw new DomainException("مسار الصورة مطلوب.");

        ImagePath = newImagePath.Trim();
        UpdateTimestamp();
    }
}
