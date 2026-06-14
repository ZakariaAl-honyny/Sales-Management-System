using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents an exchange rate between a currency and the base currency.
/// Maps to "CurrencyRates" table — smallint FK to Currencies(Id).
/// RateToBase is decimal(18,6) for precision.
/// </summary>
public class CurrencyRate : AuditableEntity
{
    /// <summary>
    /// FK to the currency this rate belongs to (smallint).
    /// </summary>
    public short CurrencyId { get; private set; }

    /// <summary>
    /// The exchange rate value against the base currency. decimal(18,6).
    /// </summary>
    public decimal RateToBase { get; private set; }

    /// <summary>
    /// The date from which this rate becomes effective.
    /// </summary>
    public DateTime EffectiveFrom { get; private set; }

    /// <summary>
    /// Optional end date for this rate's validity. Null = currently active.
    /// </summary>
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>
    /// Navigation property to the currency.
    /// </summary>
    public Currency Currency { get; private set; } = null!;

    private CurrencyRate() { }

    /// <summary>
    /// Factory method to create a new currency rate entry.
    /// </summary>
    /// <param name="currencyId">FK to the currency (smallint).</param>
    /// <param name="rateToBase">Exchange rate against base currency. Must be > 0.</param>
    /// <param name="effectiveFrom">Effective start date.</param>
    /// <param name="effectiveTo">Optional effective end date.</param>
    /// <param name="createdByUserId">Optional user ID who created this record.</param>
    /// <returns>A new CurrencyRate instance.</returns>
    public static CurrencyRate Create(
        short currencyId,
        decimal rateToBase,
        DateTime effectiveFrom,
        DateTime? effectiveTo = null,
        int? createdByUserId = null)
    {
        if (currencyId <= 0)
            throw new DomainException("معرف العملة مطلوب.");
        if (rateToBase <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من صفر.");
        if (effectiveFrom == default)
            throw new DomainException("تاريخ السريان مطلوب.");
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new DomainException("تاريخ الانتهاء يجب أن يكون بعد تاريخ البدء.");

        var rate = new CurrencyRate
        {
            CurrencyId = currencyId,
            RateToBase = rateToBase,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        };
        rate.SetCreatedBy(createdByUserId);
        return rate;
    }

    /// <summary>
    /// Updates the rate value and effective dates.
    /// </summary>
    public void Update(decimal rateToBase, DateTime effectiveFrom, DateTime? effectiveTo = null, int? updatedByUserId = null)
    {
        if (rateToBase <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من صفر.");
        if (effectiveFrom == default)
            throw new DomainException("تاريخ السريان مطلوب.");
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new DomainException("تاريخ الانتهاء يجب أن يكون بعد تاريخ البدء.");

        RateToBase = rateToBase;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
