using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a bank account linked to the chart of accounts.
/// Each bank is mapped to an Account (FK) for automatic journal entries.
/// Inherits <see cref="BaseEntity"/> for audit and soft-delete support.
/// </summary>
public class Bank : ActivatableEntity
{
    /// <summary>
    /// FK to the linked chart-of-accounts account.
    /// If null at creation time, the service layer auto-creates a sub-account under "1120 — البنوك".
    /// </summary>
    public int? AccountId { get; private set; }

    /// <summary>
    /// The linked chart-of-accounts account navigation property.
    /// </summary>
    public Account? Account { get; private set; }

    /// <summary>
    /// Bank name (e.g. "البنك الأهلي").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Account holder name as registered with the bank.
    /// </summary>
    public string? AccountName { get; private set; }

    /// <summary>
    /// Bank account number assigned by the bank.
    /// </summary>
    public string? AccountNumber { get; private set; }

    /// <summary>
    /// International Bank Account Number (IBAN).
    /// </summary>
    public string? IBAN { get; private set; }

    /// <summary>
    /// Bank branch name (e.g. "فرع الحمراء").
    /// </summary>
    public string? BranchName { get; private set; }

    /// <summary>
    /// Contact phone number for the bank or relationship manager.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Optional notes or description.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// FK to the currency this bank account operates in (required).
    /// </summary>
    public short CurrencyId { get; private set; }

    /// <summary>
    /// The currency navigation property.
    /// </summary>
    public Currency Currency { get; private set; } = null!;

    /// <summary>
    /// Private constructor required by EF Core.
    /// </summary>
    private Bank() { }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Sets the Chart of Accounts AccountId for this bank.
    /// Called by the service layer after auto-creating a sub-account under "1120 — البنوك".
    /// Only allowed when the current AccountId is null (not yet set).
    /// </summary>
    public void SetAccountId(int accountId)
    {
        if (accountId <= 0)
            throw new DomainException("معرف الحساب غير صالح");
        if (AccountId.HasValue)
            throw new DomainException("لا يمكن تغيير الحساب المحاسبي للبنك بعد تعيينه");

        AccountId = accountId;
        UpdateTimestamp();
    }

    /// <summary>
    /// Factory method to create a new bank record.
    /// </summary>
    /// <param name="accountId">FK to the chart-of-accounts account (optional — service auto-creates if null).</param>
    /// <param name="name">Bank name (required).</param>
    /// <param name="currencyId">FK to the currency (required).</param>
    /// <param name="accountName">Optional account holder name.</param>
    /// <param name="accountNumber">Optional bank account number.</param>
    /// <param name="iban">Optional IBAN.</param>
    /// <param name="branchName">Optional bank branch name.</param>
    /// <param name="phone">Optional contact phone.</param>
    /// <param name="notes">Optional notes.</param>
    /// <param name="createdByUserId">ID of the creating user.</param>
    /// <returns>A new Bank instance.</returns>
    public static Bank Create(
        int? accountId,
        string name,
        short currencyId,
        string? accountName = null,
        string? accountNumber = null,
        string? iban = null,
        string? branchName = null,
        string? phone = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم البنك مطلوب.");

        if (currencyId <= 0)
            throw new DomainException("عملة الحساب البنكي مطلوبة.");

        var bank = new Bank
        {
            AccountId = accountId,
            Name = name.Trim(),
            CurrencyId = currencyId,
            AccountName = accountName?.Trim(),
            AccountNumber = accountNumber?.Trim(),
            IBAN = iban?.Trim(),
            BranchName = branchName?.Trim(),
            Phone = phone?.Trim(),
            Notes = notes?.Trim()
        };
        bank.SetCreatedBy(createdByUserId);
        return bank;
    }

    /// <summary>
    /// Updates the bank properties.
    /// </summary>
    /// <param name="name">Bank name (required).</param>
    /// <param name="currencyId">FK to the currency (required).</param>
    /// <param name="accountName">Optional account holder name.</param>
    /// <param name="accountNumber">Optional bank account number.</param>
    /// <param name="iban">Optional IBAN.</param>
    /// <param name="branchName">Optional bank branch name.</param>
    /// <param name="phone">Optional contact phone.</param>
    /// <param name="notes">Optional notes.</param>
    /// <param name="updatedByUserId">ID of the updating user.</param>
    public void Update(
        string name,
        short currencyId,
        string? accountName = null,
        string? accountNumber = null,
        string? iban = null,
        string? branchName = null,
        string? phone = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم البنك مطلوب.");

        if (currencyId <= 0)
            throw new DomainException("عملة الحساب البنكي مطلوبة.");

        Name = name.Trim();
        CurrencyId = currencyId;
        AccountName = accountName?.Trim();
        AccountNumber = accountNumber?.Trim();
        IBAN = iban?.Trim();
        BranchName = branchName?.Trim();
        Phone = phone?.Trim();
        Notes = notes?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
