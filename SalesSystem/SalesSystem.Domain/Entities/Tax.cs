using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Tax : ActivatableEntity
{
    /// <summary>
    /// smallint PK — overrides base int Id for small lookup tables.
    /// </summary>
    public new short Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public byte TaxType { get; private set; }
    public bool IsDefault { get; private set; }

    private Tax() { }

    public static Tax Create(string name, string code, decimal rate, byte taxType = 1, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الضريبة مطلوب.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("رمز الضريبة مطلوب.");
        if (rate < 0)
            throw new DomainException("نسبة الضريبة لا يمكن أن تكون سالبة.");
        if (rate > 100)
            throw new DomainException("نسبة الضريبة لا يمكن أن تتجاوز 100%.");
        if (taxType < 1 || taxType > 3)
            throw new DomainException("نوع الضريبة غير صحيح (1=قياسية, 2=صفرية, 3=معفاة).");

        return new Tax
        {
            Name = name.Trim(),
            Code = code.Trim(),
            Rate = rate,
            TaxType = taxType,
            IsDefault = isDefault,
            IsActive = true
        };
    }

    public void Update(string name, string code, decimal rate, byte taxType = 1, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الضريبة مطلوب.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("رمز الضريبة مطلوب.");
        if (rate < 0)
            throw new DomainException("نسبة الضريبة لا يمكن أن تكون سالبة.");
        if (rate > 100)
            throw new DomainException("نسبة الضريبة لا يمكن أن تتجاوز 100%.");
        if (taxType < 1 || taxType > 3)
            throw new DomainException("نوع الضريبة غير صحيح (1=قياسية, 2=صفرية, 3=معفاة).");
        Name = name.Trim();
        Code = code.Trim();
        Rate = rate;
        TaxType = taxType;
        IsDefault = isDefault;
        UpdateTimestamp();
    }

    public void SetDefault()
    {
        IsDefault = true;
        UpdateTimestamp();
    }

    public void ClearDefault()
    {
        IsDefault = false;
        UpdateTimestamp();
    }

    public override void MarkAsDeleted()
    {
        IsActive = false;
        UpdateTimestamp();
    }
}
