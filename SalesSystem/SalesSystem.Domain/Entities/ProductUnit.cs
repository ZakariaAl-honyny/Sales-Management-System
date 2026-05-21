using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a unit of measure for a product (e.g., "حبة", "طبق", "كرتون").
/// Each product has one base unit (factor = 1) and optionally multiple derived units.
/// </summary>
public class ProductUnit : BaseEntity
{
    // ─── Properties ───────────────────────────────
    public int ProductId { get; private set; }
    public string UnitName { get; private set; } = string.Empty;

    /// <summary>
    /// How many BASE UNITS does this unit contain?
    /// Base unit itself = 1. Box of 12 = 12. Pallet of 360 = 360.
    /// </summary>
    public decimal BaseConversionFactor { get; private set; }

    public bool IsBaseUnit { get; private set; }
    public decimal SalesPrice { get; private set; }
    public decimal PurchaseCost { get; private set; }
    public decimal SupplierPrice { get; private set; }
    public decimal LastPurchasePrice { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation
    public Product Product { get; private set; } = null!;

    private readonly List<UnitBarcode> _barcodes = new();
    public IReadOnlyCollection<UnitBarcode> Barcodes => _barcodes.AsReadOnly();

    private ProductUnit() { } // EF Core

    // ─── Factory ──────────────────────────────────

    /// <summary>
    /// Creates the BASE unit for a product (e.g., "حبة", "قطعة").
    /// Factor is automatically set to 1.
    /// </summary>
    public static ProductUnit CreateBaseUnit(
        int productId,
        string unitName,
        decimal salesPrice = 0,
        decimal purchaseCost = 0)
    {
        if (string.IsNullOrWhiteSpace(unitName))
            throw new DomainException("اسم الوحدة لا يمكن أن يكون فارغاً");

        return new ProductUnit
        {
            ProductId = productId,
            UnitName = unitName.Trim(),
            BaseConversionFactor = 1,
            IsBaseUnit = true,
            SalesPrice = salesPrice,
            PurchaseCost = purchaseCost,
            SupplierPrice = 0,
            LastPurchasePrice = purchaseCost,
            SortOrder = 0,
            IsActive = true
        };
    }

    /// <summary>
    /// Creates a DERIVED unit (e.g., "طبق", "كرتون") with factor > 1.
    /// </summary>
    public static ProductUnit CreateDerivedUnit(
        int productId,
        string unitName,
        decimal baseConversionFactor,
        decimal salesPrice = 0,
        decimal purchaseCost = 0,
        int sortOrder = 1)
    {
        if (string.IsNullOrWhiteSpace(unitName))
            throw new DomainException("اسم الوحدة لا يمكن أن يكون فارغاً");

        if (baseConversionFactor <= 1)
            throw new DomainException(
                $"وحدة '{unitName}' يجب أن تحتوي على أكثر من وحدة صغرى واحدة. " +
                $"أدخل كم وحدة صغرى بداخلها (مثال: الكرتون يحتوي على 12 حبة، ادخل 12).");

        return new ProductUnit
        {
            ProductId = productId,
            UnitName = unitName.Trim(),
            BaseConversionFactor = baseConversionFactor,
            IsBaseUnit = false,
            SalesPrice = salesPrice,
            PurchaseCost = purchaseCost,
            SupplierPrice = 0,
            LastPurchasePrice = purchaseCost,
            SortOrder = sortOrder,
            IsActive = true
        };
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Converts quantity in THIS unit to base unit quantity.
    /// ALWAYS use this before touching stock calculations.
    /// </summary>
    public decimal ToBaseUnitQuantity(decimal quantity)
        => quantity * BaseConversionFactor;

    /// <summary>
    /// Updates purchase cost. Returns OLD cost for history logging.
    /// </summary>
    public decimal UpdatePurchaseCost(decimal newCost)
    {
        if (newCost < 0)
            throw new DomainException("التكلفة لا يمكن أن تكون سالبة");

        var oldCost = PurchaseCost;
        LastPurchasePrice = newCost;
        PurchaseCost = Math.Round(newCost, 4);
        return oldCost;
    }

    /// <summary>
    /// Updates sales price. Returns OLD price for history logging.
    /// </summary>
    public decimal UpdateSalesPrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new DomainException("سعر البيع لا يمكن أن يكون سالباً");

        var oldPrice = SalesPrice;
        SalesPrice = Math.Round(newPrice, 4);
        return oldPrice;
    }

    /// <summary>
    /// Updates supplier catalog price.
    /// </summary>
    public void UpdateSupplierPrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new DomainException("سعر المورد لا يمكن أن يكون سالباً");

        SupplierPrice = Math.Round(newPrice, 4);
    }

    /// <summary>
    /// Adds a barcode to this unit. If marked as default, unmarks others.
    /// </summary>
    public void AddBarcode(string barcodeValue, bool isDefault = false,
        string? supplierCode = null)
    {
        if (string.IsNullOrWhiteSpace(barcodeValue))
            throw new DomainException("قيمة الباركود لا يمكن أن تكون فارغة");

        // If this is default, unmark all existing
        if (isDefault)
        {
            foreach (var b in _barcodes)
                b.UnmarkDefault();
        }

        var barcode = UnitBarcode.Create(Id, barcodeValue, isDefault, supplierCode);
        _barcodes.Add(barcode);
    }

    /// <summary>
    /// Calculates cost for this unit based on base unit cost.
    /// e.g., if base unit costs 1 SAR and this unit = 12 pieces → cost = 12 SAR
    /// </summary>
    public decimal CalculateCostFromBaseUnitCost(decimal baseUnitCost)
        => baseUnitCost * BaseConversionFactor;

    /// <summary>
    /// Calculates sales price for this unit based on base unit price.
    /// </summary>
    public decimal CalculateSalesPriceFromBaseUnitPrice(decimal baseUnitPrice)
        => baseUnitPrice * BaseConversionFactor;
}