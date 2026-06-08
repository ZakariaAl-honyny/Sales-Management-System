using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public int? CategoryId { get; private set; }
    public int? UnitId { get; private set; } // Legacy - Keep for now
    public int? WholesaleUnitId { get; private set; }
    public int? RetailUnitId { get; private set; }
    public decimal ConversionFactor { get; private set; } = 1m;
    public decimal MinStock { get; private set; }
    public decimal ReorderLevel { get; private set; }
    public string? Description { get; private set; }
    public string? ImagePath { get; private set; }

    // Navigation properties
    public virtual Category? Category { get; private set; }
    public virtual Unit? Unit { get; private set; } // Legacy
    public virtual Unit? WholesaleUnit { get; private set; }
    public virtual Unit? RetailUnit { get; private set; }
    public virtual ICollection<ProductBarcode> Barcodes { get; private set; } = new List<ProductBarcode>();
    public virtual ICollection<WarehouseStock> WarehouseStocks { get; private set; } = new List<WarehouseStock>();

    // ─── Dynamic UOM (Phase 1) ───────────────────────────────────────────
    private readonly List<ProductUnit> _units = new();
    public IReadOnlyCollection<ProductUnit> Units => _units.AsReadOnly();

    // ─── Phase 25 — Multi-Currency Pricing & Batch Tracking ──────────────
    // Prices are accessed via ProductUnit → ProductPrice (no direct Product → ProductPrice FK).
    // Use service-layer aggregation: product.Units.SelectMany(u => u.Prices)
    // to get all prices across all units.

    private readonly List<InventoryBatch> _inventoryBatches = new();
    public IReadOnlyCollection<InventoryBatch> InventoryBatches => _inventoryBatches.AsReadOnly();

    private readonly List<ProductImage> _images = new();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    private Product() { }

    // ── Centralized conversion methods (RULE-041) ───────────────────────────

    public decimal ConvertRetailToWholesaleBoxes(decimal retailQty)
        => ConversionFactor > 0 ? Math.Floor(retailQty / ConversionFactor) : 0;

    public decimal GetRemainingRetailAfterWholesale(decimal retailQty)
        => ConversionFactor > 0 ? retailQty % ConversionFactor : retailQty;

    public decimal ConvertWholesaleToRetail(decimal wholesaleQty)
        => wholesaleQty * ConversionFactor;

    public decimal GetRetailQuantityEquivalent(decimal inputQty, SaleMode mode)
        => mode == SaleMode.Wholesale ? inputQty * ConversionFactor : inputQty;

    public decimal ConvertToSmallestUnit(decimal quantity, UnitType unitType)
        => unitType == UnitType.Wholesale ? quantity * ConversionFactor : quantity;

    // ─── Dynamic UOM Methods (Phase 1) ──────────────────────────────────

    /// <summary>
    /// Returns the base unit (Piece/Egg/etc). ALWAYS exists for valid products.
    /// Throws if not found — product data is corrupted.
    /// </summary>
    public ProductUnit GetBaseUnit()
    {
        return _units.FirstOrDefault(u => u.IsBaseUnit)
            ?? throw new DomainException(
                $"المنتج '{Name}' لا يحتوي على وحدة أساسية. " +
                $"يرجى تعريف وحدة صغرى أولاً (مثال: حبة) من شاشة إدارة المنتجات.");
    }

    /// <summary>
    /// Gets a unit by its ID. Throws if not found.
    /// </summary>
    public ProductUnit GetUnitById(int unitId)
    {
        return _units.FirstOrDefault(u => u.Id == unitId)
            ?? throw new DomainException(
                $"الوحدة المحددة غير موجودة في المنتج '{Name}'");
    }

    /// <summary>
    /// Validates product has exactly ONE base unit before saving.
    /// Call this in the Command Handler before persisting.
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
                $"لا يمكن تعريف أكثر من وحدة صغرى واحدة للمنتج الواحد.\n" +
                $"الوحدات المعرّفة كأساسية: {string.Join(", ", baseUnits.Select(u => u.UnitName))}");

        var invalidDerived = _units
            .Where(u => !u.IsBaseUnit && u.BaseConversionFactor <= 1)
            .ToList();

        if (invalidDerived.Any())
            throw new DomainException(
                $"الوحدات التالية لها معامل تحويل غير صحيح:\n" +
                $"{string.Join("\n", invalidDerived.Select(u => $"- {u.UnitName}: يجب أن يكون أكبر من 1"))}\n" +
                $"أدخل كم وحدة صغرى بداخل كل وحدة أكبر.");
    }

    /// <summary>
    /// Removes a unit from the product. Guards against removing the last active unit.
    /// </summary>
    public void RemoveUnit(ProductUnit unit)
    {
        if (_units.Count(u => u.IsActive) <= 1 && unit.IsActive)
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

    // ─── Image Collection Methods ──────────────────────────────────────

    /// <summary>
    /// Adds an image to the product. If marked as primary, unmarks any existing primary.
    /// </summary>
    public void AddImage(ProductImage image)
    {
        if (image.IsPrimary)
        {
            foreach (var img in _images)
                img.UnsetPrimary();
        }
        _images.Add(image);
    }

    /// <summary>
    /// Removes an image from the product.
    /// </summary>
    public void RemoveImage(ProductImage image)
    {
        _images.Remove(image);
    }

    // ─── Inventory Batch Methods ───────────────────────────────────────

    /// <summary>
    /// Adds an inventory batch to the product.
    /// </summary>
    public void AddInventoryBatch(InventoryBatch batch)
    {
        _inventoryBatches.Add(batch);
    }

    // ─── Factory ───────────────────────────────────────────────────────

    public static Product Create(
        string name,
        decimal conversionFactor = 1,
        decimal minStock = 0,
        int? categoryId = null,
        int? retailUnitId = null,
        int? wholesaleUnitId = null,
        string? description = null,
        string? imagePath = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب.");
        if (conversionFactor <= 0)
            throw new DomainException("معامل التحويل يجب أن يكون أكبر من الصفر.");
        if (minStock < 0)
            throw new DomainException("الحد الأدنى للمخزون لا يمكن أن يكون سالباً.");

        var product = new Product
        {
            Name = name,
            ConversionFactor = conversionFactor,
            MinStock = minStock,
            CategoryId = categoryId,
            RetailUnitId = retailUnitId,
            WholesaleUnitId = wholesaleUnitId,
            Description = description,
            ImagePath = imagePath,
            // Sync legacy fields
            UnitId = retailUnitId
        };
        product.SetCreatedBy(createdByUserId);
        return product;
    }

    public void Update(
        string name,
        decimal conversionFactor,
        decimal minStock,
        int? categoryId,
        int? retailUnitId,
        int? wholesaleUnitId,
        string? description,
        int? updatedByUserId,
        string? imagePath = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب.");
        if (conversionFactor <= 0)
            throw new DomainException("معامل التحويل يجب أن يكون أكبر من الصفر.");
        if (minStock < 0)
            throw new DomainException("الحد الأدنى للمخزون لا يمكن أن يكون سالباً.");

        Name = name;
        ConversionFactor = conversionFactor;
        MinStock = minStock;
        CategoryId = categoryId;
        RetailUnitId = retailUnitId;
        WholesaleUnitId = wholesaleUnitId;
        Description = description;
        ImagePath = imagePath;
        UnitId = retailUnitId;

        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
