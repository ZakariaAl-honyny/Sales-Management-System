using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class ExchangeRateHistory : BaseEntity
{
    public int CurrencyId { get; private set; }
    public decimal OldRate { get; private set; }
    public decimal NewRate { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public string? RateType { get; private set; }
    public string? Notes { get; private set; }
    public int? ChangedByUserId { get; private set; }

    // Navigation
    public Currency Currency { get; private set; } = null!;

    private ExchangeRateHistory() { }

    public static ExchangeRateHistory Create(
        int currencyId,
        decimal oldRate,
        decimal newRate,
        DateOnly effectiveDate,
        string? rateType = null,
        string? notes = null,
        int? changedByUserId = null)
    {
        if (currencyId <= 0)
            throw new DomainException("معرف العملة مطلوب.");
        if (oldRate < 0)
            throw new DomainException("سعر الصرف القديم لا يمكن أن يكون سالباً.");
        if (newRate <= 0)
            throw new DomainException("سعر الصرف الجديد يجب أن يكون أكبر من صفر.");
        if (rateType != null && rateType.Length > 20)
            throw new DomainException("نوع السعر لا يمكن أن يتجاوز 20 حرفاً.");
        if (notes != null && notes.Length > 500)
            throw new DomainException("الملاحظات لا يمكن أن تتجاوز 500 حرف.");

        return new ExchangeRateHistory
        {
            CurrencyId = currencyId,
            OldRate = oldRate,
            NewRate = newRate,
            EffectiveDate = effectiveDate,
            RateType = rateType ?? "Daily",
            Notes = notes,
            ChangedByUserId = changedByUserId,
            IsActive = true
        };
    }
}
