using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents a chart of accounts entry. System accounts are protected from modification/deletion.
/// </summary>
public class Account : BaseEntity
{
    public string AccountCode { get; private set; } = string.Empty;
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public AccountType AccountType { get; private set; }
    public int? ParentAccountId { get; private set; }
    public bool IsSystemAccount { get; private set; }
    public string? Notes { get; private set; }

    // ─── Navigation Properties ──────────────────────────
    public Account? ParentAccount { get; private set; }
    private readonly List<Account> _subAccounts = new();
    public IReadOnlyList<Account> SubAccounts => _subAccounts.AsReadOnly();
    private readonly List<JournalEntryLine> _journalLines = new();
    public IReadOnlyList<JournalEntryLine> JournalLines => _journalLines.AsReadOnly();

    private Account() { } // EF Core

    public static Account Create(
        string accountCode,
        string nameAr,
        string nameEn,
        AccountType accountType,
        int? parentAccountId = null,
        bool isSystemAccount = false,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(accountCode))
            throw new DomainException("رمز الحساب مطلوب");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("اسم الحساب بالعربية مطلوب");

        if (!Enum.IsDefined(typeof(AccountType), accountType))
            throw new DomainException("نوع الحساب غير صالح");

        if (parentAccountId.HasValue && parentAccountId.Value <= 0)
            throw new DomainException("رقم الحساب الأب غير صالح");

        if (createdByUserId <= 0)
            throw new DomainException("منشئ الحساب مطلوب");

        if (accountCode.Trim().Length > 20)
            throw new DomainException("رمز الحساب لا يمكن أن يتجاوز 20 حرف");

        var account = new Account
        {
            AccountCode = accountCode.Trim(),
            NameAr = nameAr.Trim(),
            NameEn = nameEn?.Trim() ?? string.Empty,
            AccountType = accountType,
            ParentAccountId = parentAccountId,
            IsSystemAccount = isSystemAccount,
            Notes = notes?.Trim(),
            IsActive = true
        };
        account.SetCreatedBy(createdByUserId);
        return account;
    }

    public void Update(
        string nameAr,
        string nameEn,
        int? parentAccountId = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (IsSystemAccount)
            throw new DomainException("لا يمكن تعديل حساب نظامي");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("اسم الحساب بالعربية مطلوب");

        NameAr = nameAr.Trim();
        NameEn = nameEn?.Trim() ?? string.Empty;
        ParentAccountId = parentAccountId;
        Notes = notes?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    public void Activate(int? updatedByUserId = null)
    {
        if (IsSystemAccount)
            throw new DomainException("لا يمكن تعديل حساب نظامي");

        IsActive = true;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    public void Deactivate(int? updatedByUserId = null)
    {
        if (IsSystemAccount)
            throw new DomainException("لا يمكن تعديل حساب نظامي");

        IsActive = false;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    public override void MarkAsDeleted()
    {
        if (IsSystemAccount)
            throw new DomainException("لا يمكن حذف حساب نظامي");

        base.MarkAsDeleted();
    }

    /// <summary>
    /// Returns true if this account type has a normal debit balance (Asset or Expense).
    /// </summary>
    public bool IsDebitNormal() =>
        AccountType == AccountType.Asset || AccountType == AccountType.Expense;
}
