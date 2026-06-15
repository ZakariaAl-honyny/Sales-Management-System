using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Supplier entity. Contact information (Name, Phone, Email, Address, TaxNumber, Notes)
/// lives on the referenced <see cref="Party"/> record via PartyId FK.
/// Schema §1.4 — Suppliers table.
/// </summary>
public class Supplier : ActivatableEntity
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
    /// FK to the Chart of Accounts Account that holds this supplier's balance.
    /// </summary>
    public int AccountId { get; private set; }

    /// <summary>
    /// Navigation property to the linked Account.
    /// The balance of this supplier lives on this Account.
    /// </summary>
    public virtual Account? Account { get; private set; }

    /// <summary>
    /// Optional FK to AccountCategories for supplier classification.
    /// </summary>
    public int? CategoryId { get; private set; }

    private Supplier() { } // EF Core

    /// <summary>
    /// Factory method to create a new supplier.
    /// Contact data lives on the Party record referenced by <paramref name="partyId"/>.
    /// </summary>
    /// <param name="partyId">FK to the Party record (must be > 0).</param>
    /// <param name="accountId">FK to the Account record (must be > 0).</param>
    /// <param name="categoryId">Optional FK to AccountCategories.</param>
    /// <param name="createdByUserId">ID of the user creating this supplier.</param>
    /// <returns>A new Supplier instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static Supplier Create(
        int partyId,
        int accountId,
        int? categoryId = null,
        int? createdByUserId = null)
    {
        if (partyId <= 0)
            throw new DomainException("معرّف الطرف غير صالح.");
        if (accountId <= 0)
            throw new DomainException("معرّف الحساب غير صالح.");

        var supplier = new Supplier
        {
            PartyId = partyId,
            AccountId = accountId,
            CategoryId = categoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        supplier.SetCreatedBy(createdByUserId);
        return supplier;
    }

    /// <summary>
    /// Updates the supplier-specific fields.
    /// Contact data is updated on the linked <see cref="Party"/> record separately.
    /// </summary>
    /// <param name="categoryId">New category id (null = keep current).</param>
    /// <param name="updatedByUserId">ID of the user performing the update.</param>
    public void Update(
        int? categoryId = null,
        int? updatedByUserId = null)
    {
        if (categoryId.HasValue)
            CategoryId = categoryId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
