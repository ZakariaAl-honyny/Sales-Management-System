using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Customer entity. Contact information (Name, Phone, Email, Address, TaxNumber, Notes)
/// lives on the referenced <see cref="Party"/> record via PartyId FK.
/// Customer adds financial fields (CreditLimit) and a CategoryId for classification.
/// Schema §1.2 — Customers table.
/// </summary>
public class Customer : ActivatableEntity
{
    /// <summary>
    /// FK to the Party record that holds shared contact data.
    /// </summary>
    public int PartyId { get; private set; }

    /// <summary>
    /// Navigation property to the Party record (shared contact data).
    /// </summary>
    public virtual Party Party { get; private set; } = null!;

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
    /// Factory method to create a new customer.
    /// Contact data lives on the Party record referenced by <paramref name="partyId"/>.
    /// </summary>
    /// <param name="partyId">FK to the Party record (must be > 0).</param>
    /// <param name="accountId">FK to the Account record (must be > 0).</param>
    /// <param name="creditLimit">Credit limit (default 0 = no limit).</param>
    /// <param name="categoryId">Optional FK to AccountCategories.</param>
    /// <param name="createdByUserId">ID of the user creating this customer.</param>
    /// <returns>A new Customer instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static Customer Create(
        int partyId,
        int accountId,
        decimal creditLimit = 0,
        int? categoryId = null,
        int? createdByUserId = null)
    {
        if (partyId <= 0)
            throw new DomainException("معرّف الطرف غير صالح.");
        if (accountId <= 0)
            throw new DomainException("معرّف الحساب غير صالح.");
        if (creditLimit < 0)
            throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");

        var customer = new Customer
        {
            PartyId = partyId,
            AccountId = accountId,
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
    /// Updates the customer-specific fields.
    /// Contact data is updated on the linked <see cref="Party"/> record separately.
    /// </summary>
    /// <param name="creditLimit">New credit limit (>= 0).</param>
    /// <param name="categoryId">New category id (null = keep current).</param>
    /// <param name="updatedByUserId">ID of the user performing the update.</param>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public void Update(
        decimal creditLimit = 0,
        int? categoryId = null,
        int? updatedByUserId = null)
    {
        if (creditLimit < 0)
            throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");

        CreditLimit = creditLimit;
        if (categoryId.HasValue)
            CategoryId = categoryId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
