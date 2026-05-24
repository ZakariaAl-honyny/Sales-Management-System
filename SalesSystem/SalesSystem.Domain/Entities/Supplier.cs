using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Supplier : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public decimal CreditLimit { get; private set; }
    public string? TaxNumber { get; private set; }

    private Supplier() { }

    public static Supplier Create(
        string name,
        decimal openingBalance = 0,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        decimal creditLimit = 0,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المورد مطلوب.");
        if (openingBalance < 0)
            throw new DomainException("الرصيد الافتتاحي لا يمكن أن يكون سالباً.");
        if (creditLimit < 0)
            throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");

        var supplier = new Supplier
        {
            Name = name,
            OpeningBalance = openingBalance,
            CurrentBalance = openingBalance,
            Phone = phone,
            Email = email,
            Address = address,
            TaxNumber = taxNumber,
            CreditLimit = creditLimit
        };
        supplier.SetCreatedBy(createdByUserId);
        return supplier;
    }

    public void IncreaseBalance(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        CurrentBalance += amount;
    }

    public void DecreaseBalance(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");
        CurrentBalance -= amount;
    }

    public void Update(
        string name,
        string? phone,
        string? email,
        string? address,
        string? taxNumber,
        decimal creditLimit,
        int? updatedByUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المورد مطلوب.");
        if (creditLimit < 0)
            throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");
        Name = name;
        Phone = phone;
        Email = email;
        Address = address;
        TaxNumber = taxNumber;
        CreditLimit = creditLimit;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}