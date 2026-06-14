using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a product item in the inventory.
/// Maps to the "Products" table in the new 65-table schema.
/// - No prices stored directly (use ProductUnits → ProductPrices)
/// - Cost tracked via InventoryBatches (weighted average)
/// - Barcode is a varchar(50) primary barcode for quick lookup
/// - Images managed as a single ImagePath (ProductImages table removed)
/// </summary>
public class Product : ActivatableEntity
{
    /// <summary>
    /// Product display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Primary barcode for quick lookup. varchar(50) in DB — ASCII-only barcodes.
    /// </summary>
    public string? Barcode { get; private set; }

    /// <summary>
    /// Product description / notes.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// FK to ProductCategory — every product must belong to a category.
    /// </summary>
    public int CategoryId { get; private set; }

    /// <summary>
    /// FK to Tax. Optional tax rate override for this product.
    /// </summary>
    public int? TaxId { get; private set; }

    /// <summary>
    /// Quantity threshold that triggers a reorder alert.
    /// </summary>
    public decimal ReorderLevel { get; private set; }

    /// <summary>
    /// Indicates whether this product can expire (has an expiry date).
    /// When true, expiry tracking via InventoryBatches is enforced.
    /// </summary>
    public bool TrackExpiry { get; private set; }

    /// <summary>
    /// File path or URL to the primary product image.
    /// Single image — no separate ProductImages table.
    /// </summary>
    public string? ImagePath { get; private set; }

    /// <summary>
    /// Internal notes about the product.
    /// </summary>
    public string? Notes { get; private set; }

    // ─── Navigation Properties ────────────────────────────

    /// <summary>
    /// The category this product belongs to.
    /// </summary>
    public virtual ProductCategory? ProductCategory { get; private set; }

    /// <summary>
    /// The tax rate override for this product.
    /// </summary>
    public virtual Tax? Tax { get; private set; }

    /// <summary>
    /// Stock records per warehouse.
    /// </summary>
    public virtual ICollection<WarehouseStock> WarehouseStocks { get; private set; } = new List<WarehouseStock>();

    // ─── Dynamic UOM ──────────────────────────────────────
    private readonly List<ProductUnit> _units = new();
    public IReadOnlyCollection<ProductUnit> Units => _units.AsReadOnly();

    // ─── Inventory Batches ─────────────────────────────────
    private readonly List<InventoryBatch> _inventoryBatches = new();
    public IReadOnlyCollection<InventoryBatch> InventoryBatches => _inventoryBatches.AsReadOnly();

    private Product() { }

    // ─── Dynamic UOM Methods ─────────────────────────────

    /// <summary>
    /// Returns the base unit. ALWAYS exists for valid products.
    /// </summary>
    public ProductUnit GetBaseUnit()
    {
        return _units.FirstOrDefault(u => u.IsBaseUnit)
            ?? throw new DomainException(
                $"المنتج '{Name}' لا يحتوي على وحدة أساسية. " +
                $"يرجى تعريف وحدة صغرى أولاً (مثال: حبة) من شاشة إدارة المنتجات.");
    }

    /// <summary>
    /// Gets a unit by its ID.
    /// </summary>
    public ProductUnit GetUnitById(int unitId)
    {
        return _units.FirstOrDefault(u => u.Id == unitId)
            ?? throw new DomainException(
                $"الوحدة المحددة غير موجودة في المنتج '{Name}'");
    }

    /// <summary>
    /// Validates product has exactly ONE base unit before saving.
    /// </summary>
    public void ValidateUnits()
    {
        var baseUnits = _units.Where(u => u.IsBaseUnit).ToList();

        if (baseUnits.Count == 0)
            throw new DomainException(
                "يجب تعريف وحدة صغرى واحدة على الأقل.\n" +
                "مثال: أضف وحدة باسم 'حبة' واجعل معامل التحويل = 1");

        if (baseUnits.Count > 1)
            throw new DomainException(
                "لا يمكن تعريف أكثر من وحدة صغرى واحدة للمنتج الواحد.");

        var invalidDerived = _units
            .Where(u => !u.IsBaseUnit && u.Factor <= 1)
            .ToList();

        if (invalidDerived.Any())
            throw new DomainException(
                "الوحدات التالية لها معامل تحويل غير صحيح:\n" +
                $"{string.Join("\n", invalidDerived.Select(u => $"- {u.Unit?.Name ?? "?"}: يجب أن يكون أكبر من 1"))}\n" +
                "أدخل كم وحدة صغرى بداخل كل وحدة أكبر.");
    }

    /// <summary>
    /// Removes a unit from the product. Guards against removing the last active unit.
    /// </summary>
    public void RemoveUnit(ProductUnit unit)
    {
        if (_units.Count <= 1)
            throw new DomainException("يجب أن يكون للمنتج وحدة قياس واحدة على الأقل");
        _units.Remove(unit);
    }

    /// <summary>
    /// Adds a unit to the product.
    /// </summary>
    public void AddUnit(ProductUnit unit)
    {
        _units.Add(unit);
    }

    // ─── Inventory Batch Methods ─────────────────────────

    /// <summary>
    /// Adds an inventory batch to the product.
    /// </summary>
    public void AddInventoryBatch(InventoryBatch batch)
    {
        _inventoryBatches.Add(batch);
    }

    // ─── Factory ─────────────────────────────────────────

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="name">Product display name (required).</param>
    /// <param name="categoryId">FK to ProductCategory (required).</param>
    /// <param name="description">Optional product description.</param>
    /// <param name="barcode">Optional primary barcode for quick lookup.</param>
    /// <param name="taxId">Optional FK to Tax.</param>
    /// <param name="reorderLevel">Quantity threshold for reorder alert (default 0).</param>
    /// <param name="trackExpiry">Whether this product can expire (default false).</param>
    /// <param name="imagePath">Optional primary image path.</param>
    /// <param name="notes">Optional internal notes.</param>
    /// <param name="createdByUserId">User who created this record.</param>
    /// <returns>The newly created Product entity.</returns>
    public static Product Create(
        string name,
        int categoryId,
        string? description = null,
        string? barcode = null,
        int? taxId = null,
        decimal reorderLevel = 0,
        bool trackExpiry = false,
        string? imagePath = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب.");
        if (categoryId <= 0)
            throw new DomainException("التصنيف مطلوب.");
        if (reorderLevel < 0)
            throw new DomainException("مستوى إعادة الطلب لا يمكن أن يكون سالباً.");

        var product = new Product
        {
            Name = name,
            Description = description?.Trim(),
            Barcode = barcode?.Trim(),
            CategoryId = categoryId,
            TaxId = taxId,
            ReorderLevel = reorderLevel,
            TrackExpiry = trackExpiry,
            ImagePath = imagePath?.Trim(),
            Notes = notes?.Trim(),
            IsActive = true
        };
        product.SetCreatedBy(createdByUserId);
        return product;
    }

    /// <summary>
    /// Updates the product properties.
    /// </summary>
    public void Update(
        string name,
        int categoryId,
        string? description = null,
        string? barcode = null,
        int? taxId = null,
        decimal reorderLevel = 0,
        bool trackExpiry = false,
        string? imagePath = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب.");
        if (categoryId <= 0)
            throw new DomainException("التصنيف مطلوب.");
        if (reorderLevel < 0)
            throw new DomainException("مستوى إعادة الطلب لا يمكن أن يكون سالباً.");

        Name = name;
        Description = description?.Trim();
        Barcode = barcode?.Trim();
        CategoryId = categoryId;
        TaxId = taxId;
        ReorderLevel = reorderLevel;
        TrackExpiry = trackExpiry;
        ImagePath = imagePath?.Trim();
        Notes = notes?.Trim();

        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
