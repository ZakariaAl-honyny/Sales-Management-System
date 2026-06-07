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
    public string? Explanation { get; private set; }

    // ─── New Fields (v4.7 — Chart of Accounts Expansion) ──────────
    /// <summary>
    /// Hierarchical level: 1=Group, 2=Main, 3=Sub, 4=Detail (up to 10).
    /// </summary>
    public int Level { get; private set; }

    /// <summary>
    /// Help text for reports — explains the account's purpose and usage.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Hex color code for visual distinction in reports/UI (e.g. "#2196F3").
    /// </summary>
    public string? ColorCode { get; private set; }

    /// <summary>
    /// False for parent (group/main/sub) accounts — only detail accounts allow direct journal entry transactions.
    /// </summary>
    public bool AllowTransactions { get; private set; }

    /// <summary>
    /// Initial balance for opening entries.
    /// </summary>
    public decimal? OpeningBalance { get; private set; }

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
        int level,
        int? parentAccountId = null,
        bool isSystemAccount = false,
        string? description = null,
        string? colorCode = null,
        bool allowTransactions = false,
        decimal? openingBalance = null,
        string? explanation = null,
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

        if (accountCode.Trim().Length > 20)
            throw new DomainException("رمز الحساب لا يمكن أن يتجاوز 20 حرف");

        if (level < 1 || level > 10)
            throw new DomainException("مستوى الحساب يجب أن يكون بين 1 و 10");

        if (level >= 4 && !allowTransactions)
            throw new DomainException("الحساب التفصيلي يجب أن يسمح بالحركات");

        var account = new Account
        {
            AccountCode = accountCode.Trim(),
            NameAr = nameAr.Trim(),
            NameEn = nameEn?.Trim() ?? string.Empty,
            AccountType = accountType,
            Level = level,
            ParentAccountId = parentAccountId,
            IsSystemAccount = isSystemAccount,
            Description = description?.Trim(),
            ColorCode = colorCode?.Trim(),
            AllowTransactions = allowTransactions,
            OpeningBalance = openingBalance,
            Notes = notes?.Trim(),
            Explanation = explanation?.Trim(),
            IsActive = true
        };
        account.SetCreatedBy(createdByUserId);
        return account;
    }

    public void Update(
        string nameAr,
        string nameEn,
        AccountType accountType,
        int level,
        int? parentAccountId = null,
        string? description = null,
        string? colorCode = null,
        bool allowTransactions = false,
        string? explanation = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (IsSystemAccount)
            throw new DomainException("لا يمكن تعديل حساب نظامي");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("اسم الحساب بالعربية مطلوب");

        if (!Enum.IsDefined(typeof(AccountType), accountType))
            throw new DomainException("نوع الحساب غير صالح");

        if (level < 1 || level > 10)
            throw new DomainException("مستوى الحساب يجب أن يكون بين 1 و 10");

        if (level >= 4 && !allowTransactions)
            throw new DomainException("الحساب التفصيلي يجب أن يسمح بالحركات");

        NameAr = nameAr.Trim();
        NameEn = nameEn?.Trim() ?? string.Empty;
        AccountType = accountType;
        Level = level;
        ParentAccountId = parentAccountId;
        Description = description?.Trim();
        ColorCode = colorCode?.Trim();
        AllowTransactions = allowTransactions;
        Notes = notes?.Trim();
        Explanation = explanation?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    public void SetExplanation(string? explanation)
    {
        if (explanation?.Length > 500)
            throw new DomainException("الشرح لا يمكن أن يتجاوز 500 حرف");

        Explanation = explanation?.Trim();
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

        if (HasChildren())
            throw new DomainException("لا يمكن حذف حساب رئيسي لديه حسابات فرعية — احذف الحسابات الفرعية أولاً");

        base.MarkAsDeleted();
    }

    /// <summary>
    /// Returns true if this account has sub-accounts (children).
    /// </summary>
    public bool HasChildren() => _subAccounts.Count > 0;

    /// <summary>
    /// Returns true if this account type has a normal debit balance (Asset or Expense).
    /// </summary>
    public bool IsDebitNormal() =>
        AccountType == AccountType.Asset || AccountType == AccountType.Expense;
}
