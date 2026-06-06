using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Currency : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public decimal ExchangeRateToBase { get; private set; }
    public bool IsBaseCurrency { get; private set; }
    public string? FractionName { get; private set; }
    public bool IsSystem { get; private set; }

    private Currency() { }

    public static Currency Create(
        string name,
        string code,
        string symbol,
        decimal exchangeRateToBase,
        bool isBaseCurrency = false,
        string? fractionName = null,
        bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم العملة مطلوب.");
        if (name.Length > 100)
            throw new DomainException("اسم العملة لا يمكن أن يتجاوز 100 حرف.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("رمز العملة مطلوب.");
        if (code.Trim().Length != 3)
            throw new DomainException("رمز العملة يجب أن يكون 3 أحرف.");
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("رمز العملة (Symbol) مطلوب.");
        if (symbol.Length > 10)
            throw new DomainException("رمز العملة (Symbol) لا يمكن أن يتجاوز 10 أحرف.");
        if (exchangeRateToBase <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من صفر.");
        if (fractionName != null && fractionName.Length > 20)
            throw new DomainException("اسم الجزء الكسري لا يمكن أن يتجاوز 20 حرفاً.");

        return new Currency
        {
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Symbol = symbol.Trim(),
            ExchangeRateToBase = exchangeRateToBase,
            IsBaseCurrency = isBaseCurrency,
            FractionName = fractionName?.Trim(),
            IsSystem = isSystem,
            IsActive = true
        };
    }

    public void Update(
        string name,
        string symbol,
        decimal exchangeRateToBase,
        bool isBaseCurrency,
        string? fractionName = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم العملة مطلوب.");
        if (name.Length > 100)
            throw new DomainException("اسم العملة لا يمكن أن يتجاوز 100 حرف.");
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("رمز العملة (Symbol) مطلوب.");
        if (symbol.Length > 10)
            throw new DomainException("رمز العملة (Symbol) لا يمكن أن يتجاوز 10 أحرف.");
        if (exchangeRateToBase <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من صفر.");
        if (fractionName != null && fractionName.Length > 20)
            throw new DomainException("اسم الجزء الكسري لا يمكن أن يتجاوز 20 حرفاً.");

        Name = name.Trim();
        Symbol = symbol.Trim();
        ExchangeRateToBase = exchangeRateToBase;
        IsBaseCurrency = isBaseCurrency;
        FractionName = fractionName?.Trim();
        UpdateTimestamp();
    }

    public void SetAsBaseCurrency()
    {
        IsBaseCurrency = true;
        UpdateTimestamp();
    }

    public void UnsetBaseCurrency()
    {
        IsBaseCurrency = false;
        UpdateTimestamp();
    }

    public void UpdateExchangeRate(decimal newRate)
    {
        if (newRate <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من صفر.");
        ExchangeRateToBase = newRate;
        UpdateTimestamp();
    }

    public override void MarkAsDeleted()
    {
        if (IsSystem)
            throw new DomainException("لا يمكن حذف عملة النظام — العملة محمية");
        IsActive = false;
        UpdateTimestamp();
    }
}
