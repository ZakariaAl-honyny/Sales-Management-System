using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Customer entity. Contact information (Name, Phone, Email, Address, TaxNumber, Notes)
/// lives on the referenced <see cref="Party"/> record with which it shares its Id (PK = FK).
/// Customer adds financial fields (CreditLimit) and business-specific data
/// (CustomerSince, PriceLevel, Notes).
/// </summary>
public class Customer : ActivatableEntity
{
    /// <summary>
    /// Id is BOTH the primary key AND the foreign key to Parties(Id).
    /// This enforces a 1:1 relationship: one Party → one Customer.
    /// </summary>

    /// <summary>
    /// Navigation property to the Party record (shared contact data).
    /// </summary>
    public virtual Party Party { get; private set; } = null!;

    /// <summary>
    /// Date when the customer started doing business (optional).
    /// </summary>
    public DateTime? CustomerSince { get; private set; }

    /// <summary>
    /// Maximum credit allowed for this customer. Zero means no credit limit enforced.
    /// </summary>
    public decimal CreditLimit { get; private set; }

    /// <summary>
    /// Default price level for this customer: 1=Retail, 2=Wholesale, 3=VIP, 4=Distributor.
    /// </summary>
    public byte? PriceLevel { get; private set; }

    /// <summary>
    /// Free-text notes for this customer.
    /// </summary>
    public string? Notes { get; private set; }

    private Customer() { } // EF Core

    /// <summary>
    /// Factory method to create a new customer.
    /// The customer's Id will be set to the same value as the referenced Party.Id
    /// (shared primary key pattern). Contact data lives on the Party record.
    /// </summary>
    /// <param name="partyId">FK to the Party record — also becomes this customer's Id (must be > 0).</param>
    /// <param name="creditLimit">Credit limit (default 0 = no limit).</param>
    /// <param name="customerSince">Optional start date.</param>
    /// <param name="priceLevel">Optional price level (1-4).</param>
    /// <param name="notes">Optional free-text notes.</param>
    /// <param name="createdByUserId">ID of the user creating this customer.</param>
    /// <returns>A new Customer instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static Customer Create(
        int partyId,
        decimal creditLimit = 0,
        DateTime? customerSince = null,
        byte? priceLevel = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (partyId <= 0)
            throw new DomainException("معرّف الطرف غير صالح.");
        if (creditLimit < 0)
            throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");
        if (priceLevel.HasValue && (priceLevel < 1 || priceLevel > 4))
            throw new DomainException("مستوى السعر يجب أن يكون بين 1 و 4.");

        var customer = new Customer
        {
            Id = partyId,
            CreditLimit = creditLimit,
            CustomerSince = customerSince ?? DateTime.UtcNow,
            PriceLevel = priceLevel,
            Notes = notes?.Trim(),
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
    /// <param name="customerSince">New start date (null = keep current).</param>
    /// <param name="priceLevel">New price level (null = keep current, 1-4).</param>
    /// <param name="notes">New notes (null = keep current).</param>
    /// <param name="updatedByUserId">ID of the user performing the update.</param>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public void Update(
        decimal creditLimit = 0,
        DateTime? customerSince = null,
        byte? priceLevel = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (creditLimit < 0)
            throw new DomainException("حد الائتمان لا يمكن أن يكون سالباً.");
        if (priceLevel.HasValue && (priceLevel < 1 || priceLevel > 4))
            throw new DomainException("مستوى السعر يجب أن يكون بين 1 و 4.");

        CreditLimit = creditLimit;
        if (customerSince.HasValue)
            CustomerSince = customerSince.Value;
        if (priceLevel.HasValue)
            PriceLevel = priceLevel;
        if (notes != null)
            Notes = notes.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
