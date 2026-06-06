using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Tax : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public decimal Rate { get; private set; }
    public bool IsDefault { get; private set; }

    private Tax() { }

    public static Tax Create(string name, decimal rate, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الضريبة مطلوب.");
        if (rate < 0)
            throw new DomainException("نسبة الضريبة لا يمكن أن تكون سالبة.");
        if (rate > 100)
            throw new DomainException("نسبة الضريبة لا يمكن أن تتجاوز 100%.");

        return new Tax
        {
            Name = name.Trim(),
            Rate = rate,
            IsDefault = isDefault,
            IsActive = true
        };
    }

    public void Update(string name, decimal rate, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الضريبة مطلوب.");
        if (rate < 0)
            throw new DomainException("نسبة الضريبة لا يمكن أن تكون سالبة.");
        if (rate > 100)
            throw new DomainException("نسبة الضريبة لا يمكن أن تتجاوز 100%.");
        Name = name.Trim();
        Rate = rate;
        IsDefault = isDefault;
    }

    public void MarkAsDeleted() => IsActive = false;
}
