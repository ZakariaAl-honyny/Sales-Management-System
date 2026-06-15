using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Currency : ActivatableEntity
{
    /// <summary>
    /// smallint PK — overrides base int Id for small lookup tables.
    /// </summary>
    public new short Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public decimal ExchangeRateToBase { get; private set; }
    public bool IsBaseCurrency { get; private set; }
    public string? FractionName { get; private set; }
    public int DecimalPlaces { get; private set; }
    public bool IsSystem { get; private set; }

    private Currency() { }

    public static Currency Create(
        string name,
        string code,
        string symbol,
        decimal exchangeRateToBase,
        bool isBaseCurrency = false,
        string? fractionName = null,
        int decimalPlaces = 2,
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
        if (symbol.Length > 20)
            throw new DomainException("رمز العملة (Symbol) لا يمكن أن يتجاوز 20 حرفاً.");
        if (exchangeRateToBase <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من صفر.");
        if (fractionName != null && fractionName.Length > 50)
            throw new DomainException("اسم الجزء الكسري لا يمكن أن يتجاوز 50 حرفاً.");
        if (decimalPlaces < 0 || decimalPlaces > 4)
            throw new DomainException("عدد المنازل العشرية يجب أن يكون بين 0 و 4.");

        return new Currency
        {
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Symbol = symbol.Trim(),
            ExchangeRateToBase = exchangeRateToBase,
            IsBaseCurrency = isBaseCurrency,
            FractionName = fractionName?.Trim(),
            DecimalPlaces = decimalPlaces,
            IsSystem = isSystem,
            IsActive = true
        };
    }

    public void Update(
        string name,
        string symbol,
        decimal exchangeRateToBase,
        string? fractionName = null,
        int decimalPlaces = 2)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم العملة مطلوب.");
        if (name.Length > 100)
            throw new DomainException("اسم العملة لا يمكن أن يتجاوز 100 حرف.");
        if (string.IsNullOrWhiteSpace(symbol))
            throw new DomainException("رمز العملة (Symbol) مطلوب.");
        if (symbol.Length > 20)
            throw new DomainException("رمز العملة (Symbol) لا يمكن أن يتجاوز 20 حرفاً.");
        if (exchangeRateToBase <= 0)
            throw new DomainException("سعر الصرف يجب أن يكون أكبر من صفر.");
        if (fractionName != null && fractionName.Length > 50)
            throw new DomainException("اسم الجزء الكسري لا يمكن أن يتجاوز 50 حرفاً.");
        if (decimalPlaces < 0 || decimalPlaces > 4)
            throw new DomainException("عدد المنازل العشرية يجب أن يكون بين 0 و 4.");

        Name = name.Trim();
        Symbol = symbol.Trim();
        ExchangeRateToBase = exchangeRateToBase;
        FractionName = fractionName?.Trim();
        DecimalPlaces = decimalPlaces;
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
