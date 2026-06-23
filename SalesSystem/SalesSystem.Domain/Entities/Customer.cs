using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Customer entity with direct contact information.
/// The customer's balance lives on the linked Chart of Accounts Account (via AccountId FK).
/// Schema §1.2 — Customers table.
/// </summary>
public class Customer : ActivatableEntity
{
    /// <summary>
    /// Customer name (required). This field holds the primary display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Customer phone number (optional).
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Customer email address (optional).
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Customer physical address (optional).
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// Customer tax number / VAT registration (optional).
    /// </summary>
    public string? TaxNumber { get; private set; }

    /// <summary>
    /// Free-text notes about the customer (optional).
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// FK to the Chart of Accounts Account that holds this customer's balance.
    /// </summary>
    public int AccountId { get; private set; }

    /// <summary>
    /// Navigation property to the linked Account.
    /// The balance of this customer lives on this Account.
    /// </summary>
    public virtual Account? Account { get; private set; }

    /// <summary>
    /// Optional FK to AccountCategories for customer classification.
    /// </summary>
    public int? CategoryId { get; private set; }

    /// <summary>
    /// Maximum credit allowed for this customer. Zero means no credit limit enforced.
    /// </summary>
    public decimal CreditLimit { get; private set; }

    private Customer() { } // EF Core

    /// <summary>
    /// Factory method to create a new customer with direct contact information.
    /// </summary>
    /// <param name="name">Customer name (required).</param>
    /// <param name="accountId">FK to the Account record (must be &gt; 0).</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional physical address.</param>
    /// <param name="taxNumber">Optional tax number / VAT registration.</param>
    /// <param name="notes">Optional free-text notes.</param>
    /// <param name="creditLimit">Credit limit (default 0 = no limit).</param>
    /// <param name="categoryId">Optional FK to AccountCategories.</param>
    /// <param name="createdByUserId">ID of the user creating this customer.</param>
    /// <returns>A new Customer instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static Customer Create(
        string name,
        int accountId,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        string? notes = null,
        decimal creditLimit = 0,
        int? categoryId = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم العميل مطلوب.");
        if (accountId <= 0)
            throw new DomainException("معرّف الحساب غير صالح.");
        if (creditLimit < 0)
            throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");

        var customer = new Customer
        {
            Name = name.Trim(),
            AccountId = accountId,
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            Notes = notes?.Trim(),
            CreditLimit = creditLimit,
            CategoryId = categoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        customer.SetCreatedBy(createdByUserId);
        return customer;
    }

    /// <summary>
    /// Checks whether adding an additional amount would exceed the credit limit.
    /// Non-throwing — the caller decides whether to block.
    /// Balance is read from the linked Account, not stored on Customer.
    /// </summary>
    /// <param name="additionalAmount">The additional amount to check against the limit.</param>
    /// <returns>True if the transaction is within the credit limit (or no limit is set).</returns>
    public bool CheckCreditLimit(decimal additionalAmount)
    {
        if (CreditLimit <= 0)
            return true;

        // Note: CurrentBalance is read from the linked Account (via Chart of Accounts).
        // This method uses CreditLimit as a soft check. The caller provides the current balance.
        return additionalAmount <= CreditLimit;
    }

    /// <summary>
    /// Updates the customer fields including contact information.
    /// </summary>
    /// <param name="name">Customer name (required).</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional physical address.</param>
    /// <param name="taxNumber">Optional tax number / VAT registration.</param>
    /// <param name="notes">Optional free-text notes.</param>
    /// <param name="creditLimit">New credit limit (&gt;= 0).</param>
    /// <param name="categoryId">New category id (null = keep current).</param>
    /// <param name="updatedByUserId">ID of the user performing the update.</param>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public void Update(
        string name,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        string? notes = null,
        decimal creditLimit = 0,
        int? categoryId = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم العميل مطلوب.");
        if (creditLimit < 0)
            throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");

        Name = name.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        TaxNumber = taxNumber?.Trim();
        Notes = notes?.Trim();
        CreditLimit = creditLimit;
        if (categoryId.HasValue)
            CategoryId = categoryId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
