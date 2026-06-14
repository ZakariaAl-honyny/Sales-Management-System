using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Unified party (customer/supplier) entity that consolidates common fields
/// (Name, Phone, Email, Address, TaxNumber, Notes).
/// Specific customer/supplier behaviour is distinguished by the PartyType enum.
/// Each Customer/Supplier record has a 1:1 relationship with a Party row
/// via a shared primary key (Customer.Id = Party.Id, Supplier.Id = Party.Id).
/// </summary>
public class Party : ActivatableEntity
{
    /// <summary>
    /// Determines whether this party is a customer or a supplier.
    /// </summary>
    public PartyType PartyType { get; private set; }

    /// <summary>
    /// The display name of the party (e.g. customer name, supplier company name).
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional name in Arabic.
    /// </summary>
    public string? NameAr { get; private set; }

    /// <summary>
    /// Optional phone number (landline).
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Optional mobile number.
    /// </summary>
    public string? Mobile { get; private set; }

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
    /// FK to the Chart of Accounts Account that holds this party's balance.
    /// </summary>
    public int AccountId { get; private set; }

    /// <summary>
    /// Nav property to the linked Account.
    /// The balance of this party lives on this Account.
    /// </summary>
    public virtual Account Account { get; private set; } = null!;

    private Party() { } // EF Core

    /// <summary>
    /// Factory method to create a new party.
    /// </summary>
    /// <param name="name">Display name (required, non-empty).</param>
    /// <param name="partyType">Customer or Supplier.</param>
    /// <param name="accountId">FK to the Chart of Accounts Account (must be > 0).</param>
    /// <param name="nameAr">Optional Arabic name.</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="mobile">Optional mobile number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional address.</param>
    /// <param name="taxNumber">Optional tax number.</param>
    /// <param name="createdByUserId">ID of the user creating this party.</param>
    /// <returns>A new Party instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static Party Create(
        string name,
        PartyType partyType,
        int accountId,
        string? nameAr = null,
        string? phone = null,
        string? mobile = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الطرف مطلوب.");
        if (accountId <= 0)
            throw new DomainException("معرّف الحساب غير صالح.");
        if (!Enum.IsDefined(typeof(PartyType), partyType))
            throw new DomainException("نوع الطرف غير صالح.");

        var party = new Party
        {
            Name = name.Trim(),
            PartyType = partyType,
            AccountId = accountId,
            NameAr = nameAr?.Trim(),
            Phone = phone?.Trim(),
            Mobile = mobile?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            TaxNumber = taxNumber?.Trim(),
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
    /// <param name="accountId">New account ID (> 0) — must be provided.</param>
    /// <param name="nameAr">New Arabic name (null = keep current).</param>
    /// <param name="phone">New phone number (null = keep current).</param>
    /// <param name="mobile">New mobile number (null = keep current).</param>
    /// <param name="email">New email address (null = keep current).</param>
    /// <param name="address">New address (null = keep current).</param>
    /// <param name="taxNumber">New tax number (null = keep current).</param>
    /// <param name="updatedByUserId">ID of the user performing the update.</param>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public void Update(
        string name,
        int accountId,
        string? nameAr = null,
        string? phone = null,
        string? mobile = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الطرف مطلوب.");
        if (accountId <= 0)
            throw new DomainException("معرّف الحساب غير صالح.");

        Name = name.Trim();
        AccountId = accountId;
        NameAr = nameAr?.Trim();
        Phone = phone?.Trim();
        Mobile = mobile?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        TaxNumber = taxNumber?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
