using SalesSystem.Domain.Accounting.Entities;
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

    // ─── Phase 32: FK to Account (Chart of Accounts) ─────────────────
    /// <summary>
    /// FK to Chart of Accounts. Links this supplier to an Account for financial reporting.
    /// </summary>
    public int? AccountId { get; private set; }

    // ─── Navigation Properties ─────────────────────────────────────
    /// <summary>
    /// Navigation property to the linked Account (Chart of Accounts).
    /// </summary>
    public virtual Account? Account { get; private set; }

    private Supplier() { }

    public static Supplier Create(
        string name,
        decimal openingBalance = 0,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        decimal creditLimit = 0,
        int? createdByUserId = null,
        int? accountId = null)
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
            CreditLimit = creditLimit,
            AccountId = accountId
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

    /// <summary>
    /// Links the supplier to an Account in the Chart of Accounts.
    /// </summary>
    public void LinkToAccount(int accountId)
    {
        if (accountId <= 0)
            throw new DomainException("معرّف الحساب غير صالح.");

        AccountId = accountId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Removes the link to the Chart of Accounts.
    /// </summary>
    public void UnlinkAccount()
    {
        AccountId = null;
        UpdateTimestamp();
    }

    public void Update(
        string name,
        string? phone,
        string? email,
        string? address,
        string? taxNumber,
        decimal creditLimit,
        int? updatedByUserId,
        int? accountId = null)
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
        AccountId = accountId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}