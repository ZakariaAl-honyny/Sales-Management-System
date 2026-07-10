using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Single-row table storing company-wide settings (name, contact, logo, default currency).
/// Id is always 1 — enforced at the database level.
/// Schema: No CreatedByUserId/UpdatedByUserId/IsActive — only CreatedAt/UpdatedAt for audit.
/// </summary>
public class CompanySettings : Entity
{
    /// <summary>
    /// Schema: tinyint PK (byte).
    /// </summary>
    public new byte Id { get; private set; }

    /// <summary>
    /// Company legal name (required, max 200 characters).
    /// </summary>
    public string CompanyName { get; private set; } = string.Empty;

    /// <summary>
    /// Primary phone number.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Primary email address.
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Company physical address.
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// Tax registration number.
    /// </summary>
    public string? TaxNumber { get; private set; }

    /// <summary>
    /// File path to the company logo image (used in print documents).
    /// </summary>
    public string? LogoPath { get; private set; }

    /// <summary>
    /// Audit timestamps — schema has only CreatedAt/UpdatedAt, no CreatedByUserId/UpdatedByUserId.
    /// </summary>
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    private CompanySettings() { } // EF Core

    /// <summary>
    /// Factory method to create the single CompanySettings row.
    /// </summary>
    /// <param name="companyName">Company legal name (required).</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional physical address.</param>
    /// <param name="taxNumber">Optional tax registration number.</param>
    /// <param name="logoPath">Optional logo file path.</param>
    /// <param name="createdByUserId">Optional ID of the creating user.</param>
    /// <returns>A new CompanySettings instance with Id = 1 (enforced at DB level).</returns>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public static CompanySettings Create(
        string companyName,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        string? logoPath = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new DomainException("اسم الشركة مطلوب.");
        if (companyName.Trim().Length > 200)
            throw new DomainException("اسم الشركة لا يمكن أن يتجاوز 200 حرف.");
        if (phone != null && phone.Trim().Length > 30)
            throw new DomainException("رقم الهاتف لا يمكن أن يتجاوز 30 حرفاً.");
        if (email != null && email.Trim().Length > 100)
            throw new DomainException("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف.");
        if (address != null && address.Trim().Length > 300)
            throw new DomainException("العنوان لا يمكن أن يتجاوز 300 حرف.");
        if (taxNumber != null && taxNumber.Trim().Length > 50)
            throw new DomainException("الرقم الضريبي لا يمكن أن يتجاوز 50 حرفاً.");
        if (logoPath != null && logoPath.Trim().Length > 500)
            throw new DomainException("مسار الشعار لا يمكن أن يتجاوز 500 حرف.");

        var settings = new CompanySettings
        {
            CompanyName = companyName.Trim(),
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            TaxNumber = taxNumber?.Trim(),
            LogoPath = logoPath?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        return settings;
    }

    /// <summary>
    /// Updates the company settings fields.
    /// </summary>
    /// <param name="companyName">Company legal name (required).</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="email">Optional email address.</param>
    /// <param name="address">Optional physical address.</param>
    /// <param name="taxNumber">Optional tax registration number.</param>
    /// <param name="logoPath">Optional logo file path.</param>
    /// <param name="updatedByUserId">Optional ID of the updating user.</param>
    /// <exception cref="DomainException">If any guard clause fails.</exception>
    public void Update(
        string companyName,
        string? phone = null,
        string? email = null,
        string? address = null,
        string? taxNumber = null,
        string? logoPath = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new DomainException("اسم الشركة مطلوب.");
        if (companyName.Trim().Length > 200)
            throw new DomainException("اسم الشركة لا يمكن أن يتجاوز 200 حرف.");
        if (phone != null && phone.Trim().Length > 30)
            throw new DomainException("رقم الهاتف لا يمكن أن يتجاوز 30 حرفاً.");
        if (email != null && email.Trim().Length > 100)
            throw new DomainException("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف.");
        if (address != null && address.Trim().Length > 300)
            throw new DomainException("العنوان لا يمكن أن يتجاوز 300 حرف.");
        if (taxNumber != null && taxNumber.Trim().Length > 50)
            throw new DomainException("الرقم الضريبي لا يمكن أن يتجاوز 50 حرفاً.");
        if (logoPath != null && logoPath.Trim().Length > 500)
            throw new DomainException("مسار الشعار لا يمكن أن يتجاوز 500 حرف.");

        CompanyName = companyName.Trim();
        Phone = phone?.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
        TaxNumber = taxNumber?.Trim();
        LogoPath = logoPath?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
