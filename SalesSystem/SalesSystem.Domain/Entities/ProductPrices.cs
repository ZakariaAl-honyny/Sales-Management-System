using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a price entry for a specific product unit, currency, and price level.
/// Supports multi-currency pricing with effective date ranges.
/// </summary>
public class ProductPrice : BaseEntity
{
    /// <summary>
    /// FK to ProductUnit — identifies which unit this price applies to.
    /// </summary>
    public int ProductUnitId { get; private set; }

    /// <summary>
    /// FK to Currency — the currency of this price entry.
    /// </summary>
    public int CurrencyId { get; private set; }



    /// <summary>
    /// The actual price amount. Stored as decimal(18,2).
    /// </summary>
    public decimal Price { get; private set; }

    /// <summary>
    /// The date from which this price is effective.
    /// </summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>
    /// The date after which this price is no longer effective. Null means no expiry.
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    // ─── Navigation Properties ──────────────────────────

    public ProductUnit ProductUnit { get; private set; } = null!;

    public Currency Currency { get; private set; } = null!;

    private ProductPrice() { } // EF Core

    // ─── Factory ──────────────────────────────────

    /// <summary>
    /// Creates a new product price entry.
    /// </summary>
    public static ProductPrice Create(
        int productUnitId,
        int currencyId,
        decimal price,
        DateTime effectiveFrom,
        DateTime? effectiveTo = null,
        int? createdByUserId = null)
    {
        if (productUnitId <= 0)
            throw new DomainException("معرف وحدة المنتج مطلوب.");
        if (currencyId <= 0)
            throw new DomainException("معرف العملة مطلوب.");
        if (price <= 0)
            throw new DomainException("السعر يجب أن يكون أكبر من الصفر.");
        if (effectiveFrom == default)
            throw new DomainException("تاريخ بدء السعر مطلوب.");
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new DomainException("تاريخ انتهاء السعر يجب أن يكون بعد تاريخ البداية.");

        var productPrice = new ProductPrice
        {
            ProductUnitId = productUnitId,
            CurrencyId = currencyId,
            Price = Math.Round(price, 2),
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            IsActive = true
        };
        productPrice.SetCreatedBy(createdByUserId);
        return productPrice;
    }

    // ─── Domain Methods ───────────────────────────

    public void UpdatePrice(
        decimal newPrice,
        DateTime? effectiveFrom = null,
        DateTime? effectiveTo = null,
        int? updatedByUserId = null)
    {
        if (newPrice <= 0)
            throw new DomainException("السعر يجب أن يكون أكبر من الصفر.");
        if (effectiveFrom.HasValue && effectiveFrom.Value == default)
            throw new DomainException("تاريخ بدء السعر مطلوب.");
        if (effectiveTo.HasValue && effectiveFrom.HasValue && effectiveTo.Value <= effectiveFrom.Value)
            throw new DomainException("تاريخ انتهاء السعر يجب أن يكون بعد تاريخ البداية.");
        if (effectiveTo.HasValue && !effectiveFrom.HasValue && effectiveTo.Value <= EffectiveFrom)
            throw new DomainException("تاريخ انتهاء السعر يجب أن يكون بعد تاريخ البداية.");

        Price = Math.Round(newPrice, 2);
        if (effectiveFrom.HasValue)
            EffectiveFrom = effectiveFrom.Value;
        if (effectiveTo.HasValue)
            EffectiveTo = effectiveTo;

        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Checks if this price is currently effective based on the given date.
    /// </summary>
    public bool IsEffectiveOn(DateTime date)
        => date >= EffectiveFrom && (!EffectiveTo.HasValue || date <= EffectiveTo.Value);

    /// <summary>
    /// Extends the effective period. Only updates if the new date is later.
    /// </summary>
    public void ExtendEffectiveTo(DateTime newEffectiveTo)
    {
        if (newEffectiveTo <= EffectiveFrom)
            throw new DomainException("تاريخ الانتهاء الجديد يجب أن يكون بعد تاريخ البداية.");

        if (!EffectiveTo.HasValue || newEffectiveTo > EffectiveTo.Value)
            EffectiveTo = newEffectiveTo;
    }
}
