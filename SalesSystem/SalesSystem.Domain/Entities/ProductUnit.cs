using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a unit of measure for a product (e.g., "حبة", "طبق", "كرتون").
/// Each product has one base unit (factor = 1) and optionally multiple derived units.
/// Unit names come from the referenced <see cref="Unit"/> entity (not embedded as a string).
/// </summary>
public class ProductUnit : BaseEntity
{
    // ─── Properties ───────────────────────────────
    public int ProductId { get; private set; }

    /// <summary>
    /// FK to the Units table — the canonical unit definition (name, symbol).
    /// </summary>
    public int UnitId { get; private set; }

    /// <summary>
    /// Navigation property to the canonical Unit.
    /// </summary>
    public Unit Unit { get; private set; } = null!;

    /// <summary>
    /// How many BASE UNITS does this unit contain?
    /// Base unit itself = 1. Box of 12 = 12. Pallet of 360 = 360.
    /// </summary>
    public decimal BaseConversionFactor { get; private set; }

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
        int unitId)
    {
        if (unitId <= 0)
            throw new DomainException("معرف الوحدة مطلوب.");

        return new ProductUnit
        {
            ProductId = productId,
            UnitId = unitId,
            BaseConversionFactor = 1,
            IsBaseUnit = true,
            IsActive = true
        };
    }

    /// <summary>
    /// Creates a DERIVED unit (e.g., "طبق", "كرتون") with factor > 1.
    /// </summary>
    public static ProductUnit CreateDerivedUnit(
        int productId,
        int unitId,
        decimal baseConversionFactor,
        int sortOrder = 1)
    {
        if (unitId <= 0)
            throw new DomainException("معرف الوحدة مطلوب.");

        if (baseConversionFactor <= 1)
            throw new DomainException(
                $"الوحدة يجب أن تحتوي على أكثر من وحدة صغرى واحدة. " +
                $"أدخل كم وحدة صغرى بداخلها (مثال: الكرتون يحتوي على 12 حبة، ادخل 12).");

        return new ProductUnit
        {
            ProductId = productId,
            UnitId = unitId,
            BaseConversionFactor = baseConversionFactor,
            IsBaseUnit = false,
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
    /// Updates the UnitId this ProductUnit points to.
    /// </summary>
    public void ChangeUnit(int newUnitId)
    {
        if (newUnitId <= 0)
            throw new DomainException("معرف الوحدة مطلوب.");
        UnitId = newUnitId;
        UpdateTimestamp();
    }
}
