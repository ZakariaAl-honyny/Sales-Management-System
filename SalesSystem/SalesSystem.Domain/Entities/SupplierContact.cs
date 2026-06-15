using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Contact person for a <see cref="Supplier"/>.
/// </summary>
public class SupplierContact : ActivatableEntity
{
    /// <summary>
    /// FK to the supplier this contact belongs to.
    /// </summary>
    public int SupplierId { get; private set; }

    /// <summary>
    /// Navigation property to the supplier.
    /// </summary>
    public virtual Supplier Supplier { get; private set; } = null!;

    /// <summary>
    /// Contact person's full name (required).
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
    /// Job position / title (e.g. "مدير مشتريات").
    /// </summary>
    public string? Position { get; private set; }

    /// <summary>
    /// Free-text notes for this contact.
    /// </summary>
    public string? Notes { get; private set; }

    private SupplierContact() { } // EF Core

    /// <summary>
    /// Factory method to create a new supplier contact.
    /// </summary>
    /// <param name="supplierId">FK to the supplier (must be > 0).</param>
    /// <param name="name">Contact person's full name (required, non-empty).</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="position">Optional job position.</param>
    /// <param name="notes">Optional free-text notes.</param>
    /// <param name="createdByUserId">ID of the user creating this contact.</param>
    /// <returns>A new SupplierContact instance.</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static SupplierContact Create(
        int supplierId,
        string name,
        string? phone = null,
        string? email = null,
        string? position = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (supplierId <= 0)
            throw new DomainException("معرّف المورد غير صالح.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم جهة الاتصال مطلوب.");

        var contact = new SupplierContact
        {
            SupplierId = supplierId,
            Name = name.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Position = position?.Trim(),
            Notes = notes?.Trim(),
            IsActive = true
        };
        contact.SetCreatedBy(createdByUserId);
        return contact;
    }

    /// <summary>
    /// Updates the mutable fields of this contact.
    /// </summary>
    /// <param name="name">New full name (required, non-empty).</param>
    /// <param name="phone">New phone number, pass null to keep current.</param>
    /// <param name="email">New email address, pass null to keep current.</param>
    /// <param name="position">New position, pass null to keep current.</param>
    /// <param name="notes">New notes, pass null to keep current.</param>
    /// <param name="updatedByUserId">ID of the user performing the update.</param>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public void Update(
        string name,
        string? phone = null,
        string? email = null,
        string? position = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم جهة الاتصال مطلوب.");

        Name = name.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Position = position?.Trim();
        Notes = notes?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
