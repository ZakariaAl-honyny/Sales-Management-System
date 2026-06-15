using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a unit of measure for a product (e.g., "حبة", "طبق", "كرتون").
/// Each product has one base unit (factor = 1) and optionally multiple derived units.
/// Unit names come from the referenced <see cref="Unit"/> entity (not embedded as a string).
/// Maps to "ProductUnits" table in the new schema — pure junction table (ProductId, UnitId, Factor, IsBaseUnit).
/// No prices or barcodes stored here — pricing via ProductPrices table, barcode via Product.Barcode.
/// </summary>
public class ProductUnit : AuditableEntity
{
    // ─── Properties ───────────────────────────────
    public int ProductId { get; private set; }

    /// <summary>
    /// FK to the Units table — the canonical unit definition (name, symbol).
    /// smallint in DB.
    /// </summary>
    public short UnitId { get; private set; }

    /// <summary>
    /// Navigation property to the canonical Unit.
    /// </summary>
    public Unit Unit { get; private set; } = null!;

    /// <summary>
    /// How many BASE UNITS does this unit contain?
    /// Base unit itself = 1. Box of 12 = 12. Pallet of 360 = 360.
    /// decimal(18,3) in DB.
    /// </summary>
    public decimal Factor { get; private set; }

    public bool IsBaseUnit { get; private set; }

    // Navigation
    public Product Product { get; private set; } = null!;

    private ProductUnit() { } // EF Core

    // ─── Factory ──────────────────────────────────

    /// <summary>
    /// Creates the BASE unit for a product (e.g., "حبة", "قطعة").
    /// Factor is automatically set to 1.
    /// </summary>
    public static ProductUnit CreateBaseUnit(
        int productId,
        short unitId)
    {
        if (unitId <= 0)
            throw new DomainException("معرف الوحدة مطلوب.");

        return new ProductUnit
        {
            ProductId = productId,
            UnitId = unitId,
            Factor = 1,
            IsBaseUnit = true
        };
    }

    /// <summary>
    /// Creates a DERIVED unit (e.g., "طبق", "كرتون") with factor > 1.
    /// </summary>
    public static ProductUnit CreateDerivedUnit(
        int productId,
        short unitId,
        decimal factor,
        int sortOrder = 1)
    {
        if (unitId <= 0)
            throw new DomainException("معرف الوحدة مطلوب.");

        if (factor <= 1)
            throw new DomainException(
                $"الوحدة يجب أن تحتوي على أكثر من وحدة صغرى واحدة. " +
                $"أدخل كم وحدة صغرى بداخلها (مثال: الكرتون يحتوي على 12 حبة، ادخل 12).");

        return new ProductUnit
        {
            ProductId = productId,
            UnitId = unitId,
            Factor = factor,
            IsBaseUnit = false
        };
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Converts quantity in THIS unit to base unit quantity.
    /// ALWAYS use this before touching stock calculations.
    /// </summary>
    public decimal ToBaseUnitQuantity(decimal quantity)
        => quantity * Factor;

    /// <summary>
    /// Converts a base unit quantity to THIS unit's quantity.
    /// </summary>
    public decimal FromBaseUnitQuantity(decimal baseQuantity)
        => Factor > 0 ? baseQuantity / Factor : 0;

    /// <summary>
    /// Updates the UnitId this ProductUnit points to.
    /// </summary>
    public void ChangeUnit(short newUnitId)
    {
        if (newUnitId <= 0)
            throw new DomainException("معرف الوحدة مطلوب.");
        UnitId = newUnitId;
        UpdateTimestamp();
    }
}
