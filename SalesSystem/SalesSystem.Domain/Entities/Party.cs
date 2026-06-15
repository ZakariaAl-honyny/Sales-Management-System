using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Shared contact data for both Customers and Suppliers.
/// Each Customer / Supplier has a 1:1 relationship with a Party record
/// via a PartyId FK.
/// Schema §1.1 — Parties table.
/// </summary>
public class Party : ActivatableEntity
{
    /// <summary>
    /// The display name of the party (e.g. customer name, supplier company name).
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional phone number.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Optional email address.
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Optional physical / postal address.
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// Optional tax identification number.
    /// </summary>
    public string? TaxNumber { get; private set; }

    /// <summary>
    /// Optional free-text notes.
    /// </summary>
    public string? Notes { get; private set; }

    private Party() { } // EF Core

    /// <summary>
    /// Factory method to create a new party.
    /// </summary>
    /// <param name="name">Display name (required, non-empty).</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional address.</param>
    /// <param name="taxNumber">Optional tax number.</param>
    /// <param name="notes">Optional free-text notes.</param>
    /// <param name="createdByUserId">ID of the user creating this party.</param>
    /// <returns>A new Party instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static Party Create(
        string name,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الطرف مطلوب.");

        var party = new Party
        {
            Name = name.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            Notes = notes?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        party.SetCreatedBy(createdByUserId);
        return party;
    }

    /// <summary>
    /// Updates all mutable fields of the party.
    /// </summary>
    /// <param name="name">New display name (required, non-empty).</param>
    /// <param name="phone">New phone number (null = keep current).</param>
    /// <param name="email">New email address (null = keep current).</param>
    /// <param name="address">New address (null = keep current).</param>
    /// <param name="taxNumber">New tax number (null = keep current).</param>
    /// <param name="notes">New notes (null = keep current).</param>
    /// <param name="updatedByUserId">ID of the user performing the update.</param>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public void Update(
        string name,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الطرف مطلوب.");

        Name = name.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        TaxNumber = taxNumber?.Trim();
        Notes = notes?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
