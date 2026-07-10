using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a bank account linked to the chart of accounts.
/// Each bank is mapped to an Account (FK) for automatic journal entries.
/// Inherits <see cref="ActivatableEntity"/> for audit and soft-delete support.
/// Schema: §4.4 Banks — Id, AccountId (int not null FK), Name, AccountNumber (nullable),
/// IBAN (nullable), IsActive, CreatedByUserId, UpdatedByUserId, CreatedAt, UpdatedAt.
/// </summary>
public class Bank : ActivatableEntity
{
    /// <summary>
    /// FK to the linked chart-of-accounts account (required).
    /// The service layer resolves the account before entity creation.
    /// </summary>
    public int AccountId { get; private set; }

    /// <summary>
    /// The linked chart-of-accounts account navigation property.
    /// </summary>
    public Account? Account { get; private set; }

    /// <summary>
    /// Bank name (e.g. "البنك الأهلي").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Bank account number assigned by the bank.
    /// </summary>
    public string? AccountNumber { get; private set; }

    /// <summary>
    /// International Bank Account Number (IBAN).
    /// </summary>
    public string? IBAN { get; private set; }

    /// <summary>
    /// Private constructor required by EF Core.
    /// </summary>
    private Bank() { }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Factory method to create a new bank record.
    /// AccountId is required — the service layer resolves auto-creation before calling this factory.
    /// </summary>
    /// <param name="name">Bank name (required).</param>
    /// <param name="accountId">FK to the chart-of-accounts account (required).</param>
    /// <param name="currencyId">FK to the Currencies table (required).</param>
    /// <param name="accountNumber">Optional bank account number.</param>
    /// <param name="iban">Optional IBAN.</param>
    /// <param name="createdByUserId">ID of the creating user.</param>
    /// <returns>A new Bank instance.</returns>
    public static Bank Create(
        string name,
        int accountId,
        string? accountNumber = null,
        string? iban = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم البنك مطلوب.");

        if (accountId <= 0)
            throw new DomainException("الحساب المحاسبي مطلوب.");

        var bank = new Bank
        {
            Name = name.Trim(),
            AccountId = accountId,
            AccountNumber = accountNumber?.Trim(),
            IBAN = iban?.Trim(),
        };
        bank.SetCreatedBy(createdByUserId);
        return bank;
    }

    /// <summary>
    /// Updates the bank properties.
    /// </summary>
    /// <param name="name">Bank name (required).</param>
    /// <param name="currencyId">FK to the Currencies table (required).</param>
    /// <param name="accountNumber">Optional bank account number.</param>
    /// <param name="iban">Optional IBAN.</param>
    /// <param name="updatedByUserId">ID of the updating user.</param>
    public void Update(
        string name,
        string? accountNumber = null,
        string? iban = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم البنك مطلوب.");

        Name = name.Trim();
        AccountNumber = accountNumber?.Trim();
        IBAN = iban?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
