using SalesSystem.Domain.Accounting.Enums;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Accounting.Entities;

/// <summary>
/// Represents a chart of accounts entry matching schema §4.2.
/// Nature (tinyint): 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense.
/// Level: 1-10 hierarchy depth with CHECK constraint.
/// System accounts are protected from modification/deletion.
/// </summary>
public class Account : ActivatableEntity
{
    /// <summary>Unique account code (nvarchar(20), unique filtered on IsActive).</summary>
    public string AccountCode { get; private set; } = string.Empty;

    /// <summary>Primary Arabic name (stores schema Name column).</summary>
    public string NameAr { get; private set; } = string.Empty;

    /// <summary>English name — bilingual extension beyond base schema.</summary>
    public string NameEn { get; private set; } = string.Empty;

    /// <summary>
    /// Returns the Arabic name (NameAr) for display.
    /// Maps to the schema 'Name' column — EF Core maps NameAr to Name.
    /// </summary>
    public string Name => NameAr;

    /// <summary>Account nature: 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense.</summary>
    public byte Nature { get; private set; }

    /// <summary>Hierarchy level (1-10). CHECK constraint enforces range.</summary>
    public byte Level { get; private set; }

    /// <summary>True if this is a leaf (detail) account that allows journal entry transactions.</summary>
    public bool IsLeaf { get; private set; } = true;

    /// <summary>
    /// Convenience property: leaf accounts allow transactions.
    /// Maps to schema AllowTransactions column (bit).
    /// </summary>
    public bool AllowTransactions => IsLeaf;

    /// <summary>Self-referencing parent FK.</summary>
    public int? ParentId { get; private set; }

    /// <summary>True if this account is system-protected (cannot be deleted/modified).</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Optional classification category FK to AccountCategories (smallint).</summary>
    public short? CategoryId { get; private set; }

    /// <summary>Description of this account's purpose (nvarchar(500)).</summary>
    public string? Description { get; private set; }

    /// <summary>Hex color code for UI display (nvarchar(7), e.g. #2196F3).</summary>
    public string? ColorCode { get; private set; }

    /// <summary>Opening balance for this account (decimal(18,2)).</summary>
    public decimal OpeningBalance { get; private set; }

    /// <summary>Additional notes (nvarchar(300)).</summary>
    public string? Notes { get; private set; }

    // ─── Navigation Properties ──────────────────────────
    public Account? ParentAccount { get; private set; }
    private readonly List<Account> _subAccounts = new();
    public IReadOnlyList<Account> SubAccounts => _subAccounts.AsReadOnly();
    public AccountCategory? Category { get; private set; }
    private readonly List<JournalEntryLine> _journalLines = new();
    public IReadOnlyList<JournalEntryLine> JournalLines => _journalLines.AsReadOnly();

    private Account() { } // EF Core

    /// <summary>
    /// Creates a new account with the schema-matching signature.
    /// Level is required (1-10) — added after existing params to avoid breaking callers.
    /// Leaf accounts (level >= 4) should have AllowTransactions = true.
    /// </summary>
    public static Account Create(
        string accountCode,
        string nameAr,
        string? nameEn = null,
        byte nature = 1,
        bool isLeaf = true,
        int? parentId = null,
        bool isSystem = false,
        short? categoryId = null,
        byte level = 1,
        string? description = null,
        string? colorCode = null,
        decimal openingBalance = 0,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(accountCode))
            throw new DomainException("رمز الحساب مطلوب");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("اسم الحساب بالعربية مطلوب");

        if (nature < 1 || nature > 5)
            throw new DomainException("نوع الحساب غير صالح — القيم المسموحة: 1-5");

        if (level < 1 || level > 10)
            throw new DomainException("مستوى الحساب يجب أن يكون بين 1 و 10");

        if (parentId.HasValue && parentId.Value <= 0)
            throw new DomainException("رقم الحساب الأب غير صالح");

        if (accountCode.Trim().Length > 20)
            throw new DomainException("رمز الحساب لا يمكن أن يتجاوز 20 حرف");

        if (description?.Length > 500)
            throw new DomainException("الوصف لا يمكن أن يتجاوز 500 حرف");

        if (colorCode?.Length > 7)
            throw new DomainException("رمز اللون لا يمكن أن يتجاوز 7 أحرف");

        if (notes?.Length > 300)
            throw new DomainException("الملاحظات لا يمكن أن تتجاوز 300 حرف");

        if (openingBalance < 0)
            throw new DomainException("الرصيد الافتتاحي لا يمكن أن يكون سالباً");

        var account = new Account
        {
            AccountCode = accountCode.Trim(),
            NameAr = nameAr.Trim(),
            NameEn = nameEn?.Trim() ?? string.Empty,
            Nature = nature,
            Level = level,
            IsLeaf = isLeaf,
            ParentId = parentId,
            IsSystem = isSystem,
            CategoryId = categoryId,
            Description = description?.Trim(),
            ColorCode = colorCode?.Trim(),
            OpeningBalance = openingBalance,
            Notes = notes?.Trim(),
        };
        account.SetCreatedBy(createdByUserId);
        return account;
    }

    /// <summary>
    /// Updates account properties. System accounts are protected.
    /// </summary>
    public void Update(
        string nameAr,
        string? nameEn = null,
        byte nature = 1,
        bool isLeaf = true,
        int? parentId = null,
        short? categoryId = null,
        byte level = 1,
        string? description = null,
        string? colorCode = null,
        decimal openingBalance = 0,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (IsSystem)
            throw new DomainException("لا يمكن تعديل حساب نظامي");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("اسم الحساب بالعربية مطلوب");

        if (nature < 1 || nature > 5)
            throw new DomainException("نوع الحساب غير صالح — القيم المسموحة: 1-5");

        if (level < 1 || level > 10)
            throw new DomainException("مستوى الحساب يجب أن يكون بين 1 و 10");

        if (description?.Length > 500)
            throw new DomainException("الوصف لا يمكن أن يتجاوز 500 حرف");

        if (colorCode?.Length > 7)
            throw new DomainException("رمز اللون لا يمكن أن يتجاوز 7 أحرف");

        if (notes?.Length > 300)
            throw new DomainException("الملاحظات لا يمكن أن تتجاوز 300 حرف");

        if (openingBalance < 0)
            throw new DomainException("الرصيد الافتتاحي لا يمكن أن يكون سالباً");

        NameAr = nameAr.Trim();
        NameEn = nameEn?.Trim() ?? string.Empty;
        Nature = nature;
        Level = level;
        IsLeaf = isLeaf;
        ParentId = parentId;
        CategoryId = categoryId;
        Description = description?.Trim();
        ColorCode = colorCode?.Trim();
        OpeningBalance = openingBalance;
        Notes = notes?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <inheritdoc />
    public override void MarkAsDeleted()
    {
        if (IsSystem)
            throw new DomainException("لا يمكن حذف حساب نظامي");

        if (HasChildren())
            throw new DomainException("لا يمكن حذف حساب رئيسي لديه حسابات فرعية — احذف الحسابات الفرعية أولاً");

        base.MarkAsDeleted();
    }

    /// <summary>
    /// Activates this account. System accounts are protected.
    /// </summary>
    public void Activate(int? updatedByUserId = null)
    {
        if (IsSystem)
            throw new DomainException("لا يمكن تعديل حساب نظامي");

        IsActive = true;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Deactivates this account. System accounts are protected.
    /// </summary>
    public void Deactivate(int? updatedByUserId = null)
    {
        if (IsSystem)
            throw new DomainException("لا يمكن تعديل حساب نظامي");

        IsActive = false;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Returns true if this account has sub-accounts (children).
    /// </summary>
    public bool HasChildren() => _subAccounts.Count > 0;

    /// <summary>
    /// Returns true if this account type has a normal debit balance (Asset or Expense).
    /// </summary>
    public bool IsDebitNormal() =>
        Nature == 1 || Nature == 5; // Asset=1, Expense=5

    /// <summary>
    /// Returns the AccountType-equivalent for this account's Nature.
    /// Convenience helper for code that uses the AccountType enum.
    /// </summary>
    public AccountType GetAccountType() => (AccountType)Nature;
}
