using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Supplier entity. Contact information (Name, Phone, Email, Address, TaxNumber, Notes)
/// lives on the referenced <see cref="Party"/> record with which it shares its Id (PK = FK).
/// Supplier adds business-specific fields (PaymentTerms, Notes).
/// </summary>
public class Supplier : ActivatableEntity
{
    /// <summary>
    /// Id is BOTH the primary key AND the foreign key to Parties(Id).
    /// This enforces a 1:1 relationship: one Party → one Supplier.
    /// </summary>

    /// <summary>
    /// Navigation property to the Party record (shared contact data).
    /// </summary>
    public virtual Party Party { get; private set; } = null!;

    /// <summary>
    /// Payment terms for this supplier (e.g. "صافي 30 يوم", "نقداً").
    /// </summary>
    public string? PaymentTerms { get; private set; }

    /// <summary>
    /// Free-text notes for this supplier.
    /// </summary>
    public string? Notes { get; private set; }

    private Supplier() { } // EF Core

    /// <summary>
    /// Factory method to create a new supplier.
    /// The supplier's Id will be set to the same value as the referenced Party.Id
    /// (shared primary key pattern). Contact data lives on the Party record.
    /// </summary>
    /// <param name="partyId">FK to the Party record — also becomes this supplier's Id (must be > 0).</param>
    /// <param name="paymentTerms">Optional payment terms text.</param>
    /// <param name="notes">Optional free-text notes.</param>
    /// <param name="createdByUserId">ID of the user creating this supplier.</param>
    /// <returns>A new Supplier instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static Supplier Create(
        int partyId,
        string? paymentTerms = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (partyId <= 0)
            throw new DomainException("معرّف الطرف غير صالح.");

        var supplier = new Supplier
        {
            Id = partyId,
            PaymentTerms = paymentTerms?.Trim(),
            Notes = notes?.Trim(),
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
    /// <param name="paymentTerms">New payment terms (null = keep current).</param>
    /// <param name="notes">New notes (null = keep current).</param>
    /// <param name="updatedByUserId">ID of the user performing the update.</param>
    public void Update(
        string? paymentTerms = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (paymentTerms != null)
            PaymentTerms = paymentTerms.Trim();
        if (notes != null)
            Notes = notes.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
