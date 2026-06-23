using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Supplier entity with direct contact information.
/// The supplier's balance lives on the linked Chart of Accounts Account (via AccountId FK).
/// Schema §1.4 — Suppliers table.
/// </summary>
public class Supplier : ActivatableEntity
{
    /// <summary>
    /// Supplier name (required). This field holds the primary display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Supplier phone number (optional).
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Supplier email address (optional).
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Supplier physical address (optional).
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// Supplier tax number / VAT registration (optional).
    /// </summary>
    public string? TaxNumber { get; private set; }

    /// <summary>
    /// Free-text notes about the supplier (optional).
    /// </summary>
    public string? Notes { get; private set; }

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
    /// Factory method to create a new supplier with direct contact information.
    /// </summary>
    /// <param name="name">Supplier name (required).</param>
    /// <param name="accountId">FK to the Account record (must be &gt; 0).</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional physical address.</param>
    /// <param name="taxNumber">Optional tax number / VAT registration.</param>
    /// <param name="notes">Optional free-text notes.</param>
    /// <param name="categoryId">Optional FK to AccountCategories.</param>
    /// <param name="createdByUserId">ID of the user creating this supplier.</param>
    /// <returns>A new Supplier instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static Supplier Create(
        string name,
        int accountId,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        string? notes = null,
        int? categoryId = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المورد مطلوب.");
        if (accountId <= 0)
            throw new DomainException("معرّف الحساب غير صالح.");

        var supplier = new Supplier
        {
            Name = name.Trim(),
            AccountId = accountId,
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            Notes = notes?.Trim(),
            CategoryId = categoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        supplier.SetCreatedBy(createdByUserId);
        return supplier;
    }

    /// <summary>
    /// Updates the supplier fields including contact information.
    /// </summary>
    /// <param name="name">Supplier name (required).</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional physical address.</param>
    /// <param name="taxNumber">Optional tax number / VAT registration.</param>
    /// <param name="notes">Optional free-text notes.</param>
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
        int? categoryId = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المورد مطلوب.");

        Name = name.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        TaxNumber = taxNumber?.Trim();
        Notes = notes?.Trim();
        if (categoryId.HasValue)
            CategoryId = categoryId;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
