using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public decimal CreditLimit { get; private set; }
    public string? TaxNumber { get; private set; }

    // ─── Phase 23: New Fields ─────────────────────────────────────
    /// <summary>
    /// FK to Chart of Accounts. Links this customer to an Account for financial reporting.
    /// </summary>
    public int? AccountId { get; private set; }

    /// <summary>
    /// FK to CustomerGroup. Groups customers for reporting and filtering.
    /// </summary>
    public int? CustomerGroupId { get; private set; }

    // ─── Navigation Properties ─────────────────────────────────────
    /// <summary>
    /// Navigation property to the linked Account (Chart of Accounts).
    /// </summary>
    public virtual Account? Account { get; private set; }

    /// <summary>
    /// Navigation property to the CustomerGroup.
    /// </summary>
    public virtual CustomerGroup? CustomerGroup { get; private set; }

    private Customer() { }

    public static Customer Create(
        string name,
        decimal openingBalance = 0,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        decimal creditLimit = 0,
        int? createdByUserId = null,
        int? accountId = null,
        int? customerGroupId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم العميل مطلوب.");
        if (openingBalance < 0)
            throw new DomainException("الرصيد الافتتاحي لا يمكن أن يكون سالباً.");
        if (creditLimit < 0)
            throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");

        var customer = new Customer
        {
            Name = name,
            OpeningBalance = openingBalance,
            CurrentBalance = openingBalance,
            Phone = phone,
            Email = email,
            Address = address,
            TaxNumber = taxNumber,
            CreditLimit = creditLimit,
            AccountId = accountId,
            CustomerGroupId = customerGroupId
        };
        customer.SetCreatedBy(createdByUserId);
        return customer;
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
    /// Checks if adding the given amount would exceed the credit limit.
    /// Returns true if within limit, false if over limit.
    /// NOTE: This is a non-throwing check — the caller decides whether to block or warn.
    /// </summary>
    public bool CheckCreditLimit(decimal additionalAmount)
    {
        if (CreditLimit <= 0)
            return true; // No credit limit set — no enforcement

        return (CurrentBalance + additionalAmount) <= CreditLimit;
    }

    /// <summary>
    /// Links the customer to an Account in the Chart of Accounts.
    /// </summary>
    public void LinkToAccount(int accountId)
    {
        if (accountId <= 0)
            throw new DomainException("معرّف الحساب غير صالح.");

        AccountId = accountId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Assigns the customer to a CustomerGroup, or removes the assignment (null).
    /// </summary>
    public void SetCustomerGroup(int? customerGroupId)
    {
        CustomerGroupId = customerGroupId;
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
        int? accountId = null,
        int? customerGroupId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم العميل مطلوب.");
        if (creditLimit < 0)
            throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");

        Name = name;
        Phone = phone;
        Email = email;
        Address = address;
        TaxNumber = taxNumber;
        CreditLimit = creditLimit;
        AccountId = accountId;
        CustomerGroupId = customerGroupId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
