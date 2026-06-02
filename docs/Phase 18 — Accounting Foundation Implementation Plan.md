Phase 18 — Accounting Foundation Implementation Plan
📋 Rules for AI Agent
This phase builds the financial backbone. Every other financial feature depends on it. Zero shortcuts. Zero assumptions.

🗺️ What We Are Building
text

┌─────────────────────────────────────────────────────────────────┐
│                  ACCOUNTING FOUNDATION                          │
│                                                                 │
│  Chart of Accounts (Accounts)                                   │
│       │                                                         │
│       ▼                                                         │
│  Journal Entries (JournalEntries)                               │
│       │                                                         │
│       ▼                                                         │
│  Journal Entry Lines (JournalEntryLines)                        │
│       │                                                         │
│       ▼                                                         │
│  System Account Mappings (SystemAccountMappings)                │
│  "Which account to hit for each operation"                      │
└─────────────────────────────────────────────────────────────────┘
🗂️ Task 0 — Database Migration
Task 0.1 — Create All Tables in Order
SQL

-- =============================================
-- Phase 17: Accounting Foundation
-- Run in this exact order
-- =============================================

-- 1. Account Types Lookup
CREATE TABLE AccountTypes (
    Id          INT PRIMARY KEY,
    NameAr      NVARCHAR(100) NOT NULL,
    NameEn      NVARCHAR(100) NOT NULL
);

INSERT INTO AccountTypes VALUES
(1, N'أصول',           'Assets'),
(2, N'خصوم',           'Liabilities'),
(3, N'حقوق الملكية',   'Equity'),
(4, N'إيرادات',        'Revenues'),
(5, N'مصروفات',        'Expenses');

-- =============================================
-- 2. Chart of Accounts
-- =============================================
CREATE TABLE Accounts (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    AccountCode     NVARCHAR(20)  NOT NULL UNIQUE,
    NameAr          NVARCHAR(200) NOT NULL,
    NameEn          NVARCHAR(200) NOT NULL,
    AccountType     INT           NOT NULL,     -- 1=Asset 2=Liability 3=Equity 4=Revenue 5=Expense
    ParentAccountId INT           NULL,         -- For sub-accounts (NULL = root)
    IsSystemAccount BIT           NOT NULL DEFAULT 0, -- Cannot be deleted by user
    IsActive        BIT           NOT NULL DEFAULT 1,
    Notes           NVARCHAR(500) NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Accounts_Parent
        FOREIGN KEY (ParentAccountId) REFERENCES Accounts(Id),

    CONSTRAINT FK_Accounts_Type
        FOREIGN KEY (AccountType) REFERENCES AccountTypes(Id)
);

CREATE INDEX IX_Accounts_Code     ON Accounts(AccountCode);
CREATE INDEX IX_Accounts_Type     ON Accounts(AccountType);
CREATE INDEX IX_Accounts_Parent   ON Accounts(ParentAccountId);

-- =============================================
-- 3. Journal Entry Header
-- =============================================
CREATE TABLE JournalEntries (
    Id              INT           PRIMARY KEY IDENTITY(1,1),
    EntryNumber     NVARCHAR(50)  NOT NULL UNIQUE,  -- AUTO: JE-20260520-001
    TransactionDate DATETIME2     NOT NULL,
    Description     NVARCHAR(500) NOT NULL,
    EntryType       INT           NOT NULL,
    -- 1=Sales  2=SalesReturn  3=Purchase  4=PurchaseReturn
    -- 5=Expense 6=StockWriteOff 7=Transfer 8=Manual 9=OpeningBalance

    ReferenceType   NVARCHAR(50)  NULL,  -- 'SalesInvoice' 'PurchaseInvoice' etc.
    ReferenceId     INT           NULL,  -- FK to source document
    ReferenceNumber NVARCHAR(50)  NULL,  -- Human-readable reference

    IsPosted        BIT           NOT NULL DEFAULT 0,
    IsReversed      BIT           NOT NULL DEFAULT 0,
    ReversedByEntryId INT         NULL,

    BranchId        INT           NULL,
    CreatedBy       INT           NOT NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT GETDATE(),
    PostedBy        INT           NULL,
    PostedAt        DATETIME2     NULL,

    CONSTRAINT FK_JournalEntries_ReversedBy
        FOREIGN KEY (ReversedByEntryId) REFERENCES JournalEntries(Id)
);

CREATE INDEX IX_JournalEntries_Date          ON JournalEntries(TransactionDate);
CREATE INDEX IX_JournalEntries_Reference     ON JournalEntries(ReferenceType, ReferenceId);
CREATE INDEX IX_JournalEntries_Type          ON JournalEntries(EntryType);
CREATE INDEX IX_JournalEntries_IsPosted      ON JournalEntries(IsPosted);

-- =============================================
-- 4. Journal Entry Lines (The Core Table)
-- =============================================
CREATE TABLE JournalEntryLines (
    Id              INT           PRIMARY KEY IDENTITY(1,1),
    JournalEntryId  INT           NOT NULL,
    AccountId       INT           NOT NULL,
    AccountCode     NVARCHAR(20)  NOT NULL,   -- Snapshot at time of entry
    AccountNameAr   NVARCHAR(200) NOT NULL,   -- Snapshot at time of entry
    Debit           DECIMAL(18,2) NOT NULL DEFAULT 0,
    Credit          DECIMAL(18,2) NOT NULL DEFAULT 0,
    Description     NVARCHAR(500) NULL,       -- Line-level note
    SortOrder       INT           NOT NULL DEFAULT 0,

    CONSTRAINT FK_JournalEntryLines_Entry
        FOREIGN KEY (JournalEntryId) REFERENCES JournalEntries(Id)
            ON DELETE CASCADE,

    CONSTRAINT FK_JournalEntryLines_Account
        FOREIGN KEY (AccountId) REFERENCES Accounts(Id),

    CONSTRAINT CHK_DebitOrCredit
        CHECK (
            (Debit > 0 AND Credit = 0) OR
            (Credit > 0 AND Debit = 0) OR
            (Debit = 0 AND Credit = 0)  -- Allowed for zero-value lines
        ),

    CONSTRAINT CHK_NoNegativeValues
        CHECK (Debit >= 0 AND Credit >= 0)
);

CREATE INDEX IX_JournalEntryLines_EntryId   ON JournalEntryLines(JournalEntryId);
CREATE INDEX IX_JournalEntryLines_AccountId ON JournalEntryLines(AccountId);

-- =============================================
-- 5. System Account Mappings
-- =============================================
CREATE TABLE SystemAccountMappings (
    Id                          INT PRIMARY KEY IDENTITY(1,1),
    BranchId                    INT NULL,   -- NULL = global default

    -- Asset Accounts
    DefaultCashAccountId        INT NOT NULL,
    DefaultBankAccountId        INT NOT NULL,
    InventoryAssetAccountId     INT NOT NULL,
    AccountsReceivableAccountId INT NOT NULL,

    -- Liability Accounts
    AccountsPayableAccountId    INT NOT NULL,
    VatOutputAccountId          INT NOT NULL,
    VatInputAccountId           INT NOT NULL,

    -- Equity Accounts
    CapitalAccountId            INT NOT NULL,

    -- Revenue Accounts
    SalesRevenueAccountId       INT NOT NULL,
    SalesReturnAccountId        INT NOT NULL,

    -- Expense Accounts
    CogsAccountId               INT NOT NULL,
    GeneralExpenseAccountId     INT NOT NULL,
    SpoilageLossAccountId       INT NOT NULL,

    UpdatedBy   INT       NULL,
    UpdatedAt   DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Mappings_Cash    FOREIGN KEY (DefaultCashAccountId)        REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_Bank    FOREIGN KEY (DefaultBankAccountId)        REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_Inv     FOREIGN KEY (InventoryAssetAccountId)     REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_AR      FOREIGN KEY (AccountsReceivableAccountId) REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_AP      FOREIGN KEY (AccountsPayableAccountId)    REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_VATOut  FOREIGN KEY (VatOutputAccountId)          REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_VATIn   FOREIGN KEY (VatInputAccountId)           REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_Capital FOREIGN KEY (CapitalAccountId)            REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_Sales   FOREIGN KEY (SalesRevenueAccountId)       REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_Return  FOREIGN KEY (SalesReturnAccountId)        REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_COGS    FOREIGN KEY (CogsAccountId)               REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_Exp     FOREIGN KEY (GeneralExpenseAccountId)     REFERENCES Accounts(Id),
    CONSTRAINT FK_Mappings_Spoil   FOREIGN KEY (SpoilageLossAccountId)       REFERENCES Accounts(Id)
);

-- =============================================
-- 6. Seed Default Chart of Accounts
-- =============================================

-- ASSETS (1xxx)
INSERT INTO Accounts (AccountCode, NameAr, NameEn, AccountType, IsSystemAccount) VALUES
('1000', N'الأصول',                        'Assets',               1, 1),
('1100', N'الأصول المتداولة',               'Current Assets',       1, 1),
('1101', N'الصندوق',                        'Cash Account',         1, 1),
('1102', N'البنك',                          'Bank Account',         1, 1),
('1200', N'المخزون',                        'Inventory',            1, 1),
('1201', N'أصل المخزون',                    'Inventory Asset',      1, 1),
('1300', N'الذمم المدينة',                  'Receivables',          1, 1),
('1301', N'ذمم العملاء',                    'Accounts Receivable',  1, 1);

-- LIABILITIES (2xxx)
INSERT INTO Accounts (AccountCode, NameAr, NameEn, AccountType, IsSystemAccount) VALUES
('2000', N'الخصوم',                         'Liabilities',          2, 1),
('2100', N'الخصوم المتداولة',               'Current Liabilities',  2, 1),
('2101', N'ذمم الموردين',                   'Accounts Payable',     2, 1),
('2200', N'الضرائب المستحقة',               'Tax Liabilities',      2, 1),
('2201', N'ضريبة القيمة المضافة - مخرجات', 'VAT Output',           2, 1),
('2202', N'ضريبة القيمة المضافة - مدخلات', 'VAT Input',            2, 1);

-- EQUITY (3xxx)
INSERT INTO Accounts (AccountCode, NameAr, NameEn, AccountType, IsSystemAccount) VALUES
('3000', N'حقوق الملكية',                   'Equity',               3, 1),
('3101', N'رأس المال',                      'Capital',              3, 1),
('3102', N'الأرباح المحتجزة',               'Retained Earnings',    3, 1);

-- REVENUES (4xxx)
INSERT INTO Accounts (AccountCode, NameAr, NameEn, AccountType, IsSystemAccount) VALUES
('4000', N'الإيرادات',                      'Revenues',             4, 1),
('4101', N'إيرادات المبيعات',               'Sales Revenue',        4, 1),
('4102', N'مرتجعات المبيعات',               'Sales Returns',        4, 1),
('4201', N'إيرادات أخرى',                   'Other Revenues',       4, 1);

-- EXPENSES (5xxx)
INSERT INTO Accounts (AccountCode, NameAr, NameEn, AccountType, IsSystemAccount) VALUES
('5000', N'المصروفات',                       'Expenses',             5, 1),
('5101', N'تكلفة البضاعة المباعة',           'Cost of Goods Sold',   5, 1),
('5201', N'المصروفات التشغيلية',             'Operating Expenses',   5, 1),
('5202', N'الإيجار',                         'Rent',                 5, 1),
('5203', N'الكهرباء والمياه',                'Utilities',            5, 1),
('5204', N'رواتب الموظفين',                  'Salaries',             5, 1),
('5205', N'مصروفات النقل والتوصيل',          'Delivery Expenses',    5, 1),
('5301', N'خسائر البضاعة التالفة',           'Spoilage Loss',        5, 1),
('5302', N'خسائر المخزون',                   'Inventory Loss',       5, 1);

-- =============================================
-- 7. Seed System Account Mappings
-- =============================================
INSERT INTO SystemAccountMappings (
    BranchId,
    DefaultCashAccountId,
    DefaultBankAccountId,
    InventoryAssetAccountId,
    AccountsReceivableAccountId,
    AccountsPayableAccountId,
    VatOutputAccountId,
    VatInputAccountId,
    CapitalAccountId,
    SalesRevenueAccountId,
    SalesReturnAccountId,
    CogsAccountId,
    GeneralExpenseAccountId,
    SpoilageLossAccountId
)
SELECT
    NULL,  -- Global
    (SELECT Id FROM Accounts WHERE AccountCode = '1101'),
    (SELECT Id FROM Accounts WHERE AccountCode = '1102'),
    (SELECT Id FROM Accounts WHERE AccountCode = '1201'),
    (SELECT Id FROM Accounts WHERE AccountCode = '1301'),
    (SELECT Id FROM Accounts WHERE AccountCode = '2101'),
    (SELECT Id FROM Accounts WHERE AccountCode = '2201'),
    (SELECT Id FROM Accounts WHERE AccountCode = '2202'),
    (SELECT Id FROM Accounts WHERE AccountCode = '3101'),
    (SELECT Id FROM Accounts WHERE AccountCode = '4101'),
    (SELECT Id FROM Accounts WHERE AccountCode = '4102'),
    (SELECT Id FROM Accounts WHERE AccountCode = '5101'),
    (SELECT Id FROM Accounts WHERE AccountCode = '5201'),
    (SELECT Id FROM Accounts WHERE AccountCode = '5301');
✅ Task 0 Checklist
 All 6 tables created without errors
 All foreign keys valid
 Default accounts seeded (20+ accounts)
 SystemAccountMappings has one global row
 CHK_DebitOrCredit constraint applied
 CHK_NoNegativeValues constraint applied
🏗️ Task 1 — Domain Layer
Task 1.1 — Enums
csharp

// File: Domain/Accounting/Enums/AccountType.cs
namespace Domain.Accounting.Enums;

public enum AccountType
{
    Asset     = 1,
    Liability = 2,
    Equity    = 3,
    Revenue   = 4,
    Expense   = 5
}

// File: Domain/Accounting/Enums/JournalEntryType.cs
namespace Domain.Accounting.Enums;

public enum JournalEntryType
{
    Sales           = 1,
    SalesReturn     = 2,
    Purchase        = 3,
    PurchaseReturn  = 4,
    Expense         = 5,
    StockWriteOff   = 6,
    Transfer        = 7,
    Manual          = 8,
    OpeningBalance  = 9
}
Task 1.2 — Account Entity
csharp

// File: Domain/Accounting/Entities/Account.cs
namespace Domain.Accounting.Entities;

public class Account : BaseEntity
{
    // ─── Properties ───────────────────────────────
    public string AccountCode { get; private set; }
    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public AccountType AccountType { get; private set; }
    public int? ParentAccountId { get; private set; }
    public bool IsSystemAccount { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    // ─── Navigation ───────────────────────────────
    public Account? ParentAccount { get; private set; }
    public IReadOnlyCollection<Account> SubAccounts => _subAccounts.AsReadOnly();
    private readonly List<Account> _subAccounts = new();

    public IReadOnlyCollection<JournalEntryLine> JournalLines => _journalLines.AsReadOnly();
    private readonly List<JournalEntryLine> _journalLines = new();

    private Account() { } // EF Core

    // ─── Factory ──────────────────────────────────
    public static Account Create(
        string accountCode,
        string nameAr,
        string nameEn,
        AccountType accountType,
        int? parentAccountId = null,
        bool isSystemAccount = false,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(accountCode))
            throw new DomainException("كود الحساب مطلوب");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("اسم الحساب بالعربي مطلوب");

        return new Account
        {
            AccountCode     = accountCode.Trim(),
            NameAr          = nameAr.Trim(),
            NameEn          = nameEn.Trim(),
            AccountType     = accountType,
            ParentAccountId = parentAccountId,
            IsSystemAccount = isSystemAccount,
            IsActive        = true,
            Notes           = notes
        };
    }

    // ─── Domain Methods ───────────────────────────

    public void Update(string nameAr, string nameEn, string? notes)
    {
        if (IsSystemAccount)
            throw new DomainException(
                $"الحساب '{NameAr}' هو حساب نظام ولا يمكن تعديله. " +
                $"تواصل مع المسؤول لإجراء أي تغييرات.");

        NameAr = nameAr.Trim();
        NameEn = nameEn.Trim();
        Notes  = notes;
    }

    public void Deactivate()
    {
        if (IsSystemAccount)
            throw new DomainException(
                $"لا يمكن تعطيل الحساب '{NameAr}' لأنه حساب نظام أساسي.");

        IsActive = false;
    }

    public void Activate() => IsActive = true;

    /// <summary>
    /// Returns true if this account increases with Debit.
    /// Assets and Expenses are Debit-normal.
    /// Liabilities, Equity, and Revenues are Credit-normal.
    /// </summary>
    public bool IsDebitNormal()
        => AccountType == AccountType.Asset ||
           AccountType == AccountType.Expense;

    public string GetDisplayName()
        => $"{AccountCode} - {NameAr}";
}
Task 1.3 — JournalEntry Entity
csharp

// File: Domain/Accounting/Entities/JournalEntry.cs
namespace Domain.Accounting.Entities;

public class JournalEntry : BaseEntity
{
    // ─── Properties ───────────────────────────────
    public string EntryNumber { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public string Description { get; private set; }
    public JournalEntryType EntryType { get; private set; }

    public string? ReferenceType { get; private set; }
    public int? ReferenceId { get; private set; }
    public string? ReferenceNumber { get; private set; }

    public bool IsPosted { get; private set; }
    public bool IsReversed { get; private set; }
    public int? ReversedByEntryId { get; private set; }

    public int? BranchId { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int? PostedBy { get; private set; }
    public DateTime? PostedAt { get; private set; }

    // ─── Lines ────────────────────────────────────
    private readonly List<JournalEntryLine> _lines = new();
    public IReadOnlyCollection<JournalEntryLine> Lines => _lines.AsReadOnly();

    private JournalEntry() { } // EF Core

    // ─── Factory ──────────────────────────────────
    public static JournalEntry Create(
        string entryNumber,
        DateTime transactionDate,
        string description,
        JournalEntryType entryType,
        int createdBy,
        string? referenceType = null,
        int? referenceId = null,
        string? referenceNumber = null,
        int? branchId = null)
    {
        if (string.IsNullOrWhiteSpace(entryNumber))
            throw new DomainException("رقم القيد مطلوب");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("بيان القيد مطلوب");

        return new JournalEntry
        {
            EntryNumber     = entryNumber,
            TransactionDate = transactionDate,
            Description     = description,
            EntryType       = entryType,
            CreatedBy       = createdBy,
            CreatedAt       = DateTime.UtcNow,
            ReferenceType   = referenceType,
            ReferenceId     = referenceId,
            ReferenceNumber = referenceNumber,
            BranchId        = branchId,
            IsPosted        = false,
            IsReversed      = false
        };
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Adds a debit line to the entry.
    /// </summary>
    public void AddDebitLine(
        int accountId,
        string accountCode,
        string accountNameAr,
        decimal amount,
        string? description = null)
    {
        ValidateCanModify();
        ValidateAmount(amount, "المبلغ المدين");

        _lines.Add(JournalEntryLine.CreateDebit(
            accountId, accountCode, accountNameAr, amount, description));
    }

    /// <summary>
    /// Adds a credit line to the entry.
    /// </summary>
    public void AddCreditLine(
        int accountId,
        string accountCode,
        string accountNameAr,
        decimal amount,
        string? description = null)
    {
        ValidateCanModify();
        ValidateAmount(amount, "المبلغ الدائن");

        _lines.Add(JournalEntryLine.CreateCredit(
            accountId, accountCode, accountNameAr, amount, description));
    }

    /// <summary>
    /// The most important method in the accounting engine.
    /// A Journal Entry is valid ONLY if Total Debit == Total Credit.
    /// </summary>
    public bool IsBalanced()
    {
        var totalDebit  = _lines.Sum(l => l.Debit);
        var totalCredit = _lines.Sum(l => l.Credit);

        // Use tolerance for floating point comparison
        return Math.Abs(totalDebit - totalCredit) < 0.001m;
    }

    /// <summary>
    /// Call this BEFORE saving to database.
    /// Throws DomainException if not balanced.
    /// </summary>
    public void ValidateAndPost(int postedBy)
    {
        if (!_lines.Any())
            throw new DomainException(
                "لا يمكن ترحيل قيد بدون أسطر. أضف سطر مدين ودائن على الأقل.");

        if (!IsBalanced())
        {
            var totalDebit  = _lines.Sum(l => l.Debit);
            var totalCredit = _lines.Sum(l => l.Credit);
            throw new DomainException(
                $"القيد غير متوازن ولا يمكن ترحيله.\n" +
                $"إجمالي المدين:  {totalDebit:N2}\n" +
                $"إجمالي الدائن: {totalCredit:N2}\n" +
                $"الفرق: {Math.Abs(totalDebit - totalCredit):N2}\n" +
                $"يجب أن يتساوى المدين والدائن.");
        }

        IsPosted  = true;
        PostedBy  = postedBy;
        PostedAt  = DateTime.UtcNow;
    }

    public decimal TotalDebit  => _lines.Sum(l => l.Debit);
    public decimal TotalCredit => _lines.Sum(l => l.Credit);

    // ─── Private Guards ───────────────────────────

    private void ValidateCanModify()
    {
        if (IsPosted)
            throw new DomainException(
                $"القيد رقم {EntryNumber} مرحّل ولا يمكن تعديله.\n" +
                $"لإصلاح أي خطأ، استخدم 'قيد عكسي' أو تواصل مع المحاسب.");

        if (IsReversed)
            throw new DomainException(
                $"القيد رقم {EntryNumber} تم عكسه ولا يمكن تعديله.");
    }

    private static void ValidateAmount(decimal amount, string fieldName)
    {
        if (amount < 0)
            throw new DomainException($"{fieldName} لا يمكن أن يكون سالباً");

        if (amount == 0)
            throw new DomainException($"{fieldName} لا يمكن أن يكون صفراً");
    }
}
Task 1.4 — JournalEntryLine Entity
csharp

// File: Domain/Accounting/Entities/JournalEntryLine.cs
namespace Domain.Accounting.Entities;

public class JournalEntryLine : BaseEntity
{
    public int JournalEntryId { get; private set; }
    public int AccountId { get; private set; }

    // Snapshots — account data at time of entry
    public string AccountCode { get; private set; }
    public string AccountNameAr { get; private set; }

    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }

    // Navigation
    public Account Account { get; private set; }
    public JournalEntry JournalEntry { get; private set; }

    private JournalEntryLine() { } // EF Core

    // ─── Factories ────────────────────────────────

    internal static JournalEntryLine CreateDebit(
        int accountId,
        string accountCode,
        string accountNameAr,
        decimal amount,
        string? description = null)
    {
        return new JournalEntryLine
        {
            AccountId     = accountId,
            AccountCode   = accountCode,
            AccountNameAr = accountNameAr,
            Debit         = amount,
            Credit        = 0,
            Description   = description
        };
    }

    internal static JournalEntryLine CreateCredit(
        int accountId,
        string accountCode,
        string accountNameAr,
        decimal amount,
        string? description = null)
    {
        return new JournalEntryLine
        {
            AccountId     = accountId,
            AccountCode   = accountCode,
            AccountNameAr = accountNameAr,
            Debit         = 0,
            Credit        = amount,
            Description   = description
        };
    }

    public bool IsDebit  => Debit > 0;
    public bool IsCredit => Credit > 0;
    public decimal Amount => Debit > 0 ? Debit : Credit;
}
Task 1.5 — SystemAccountMappings Entity
csharp

// File: Domain/Accounting/Entities/SystemAccountMappings.cs
namespace Domain.Accounting.Entities;

public class SystemAccountMappings : BaseEntity
{
    public int? BranchId { get; private set; }

    // ─── Asset Accounts ───────────────────────────
    public int DefaultCashAccountId        { get; private set; }
    public int DefaultBankAccountId        { get; private set; }
    public int InventoryAssetAccountId     { get; private set; }
    public int AccountsReceivableAccountId { get; private set; }

    // ─── Liability Accounts ───────────────────────
    public int AccountsPayableAccountId { get; private set; }
    public int VatOutputAccountId       { get; private set; }
    public int VatInputAccountId        { get; private set; }

    // ─── Equity Accounts ──────────────────────────
    public int CapitalAccountId { get; private set; }

    // ─── Revenue Accounts ─────────────────────────
    public int SalesRevenueAccountId { get; private set; }
    public int SalesReturnAccountId  { get; private set; }

    // ─── Expense Accounts ─────────────────────────
    public int CogsAccountId            { get; private set; }
    public int GeneralExpenseAccountId  { get; private set; }
    public int SpoilageLossAccountId    { get; private set; }

    // ─── Navigation ───────────────────────────────
    public Account DefaultCashAccount        { get; private set; }
    public Account DefaultBankAccount        { get; private set; }
    public Account InventoryAssetAccount     { get; private set; }
    public Account AccountsReceivableAccount { get; private set; }
    public Account AccountsPayableAccount    { get; private set; }
    public Account VatOutputAccount          { get; private set; }
    public Account VatInputAccount           { get; private set; }
    public Account CapitalAccount            { get; private set; }
    public Account SalesRevenueAccount       { get; private set; }
    public Account SalesReturnAccount        { get; private set; }
    public Account CogsAccount               { get; private set; }
    public Account GeneralExpenseAccount     { get; private set; }
    public Account SpoilageLossAccount       { get; private set; }

    private SystemAccountMappings() { }

    /// <summary>
    /// Returns the cash account ID based on payment method.
    /// </summary>
    public int GetPaymentAccountId(string paymentMethod)
    {
        return paymentMethod?.ToLower() switch
        {
            "bank" or "شبكة" or "بطاقة" => DefaultBankAccountId,
            _ => DefaultCashAccountId
        };
    }
}
✅ Task 1 Checklist
 All 4 entities created in Domain/Accounting/Entities/
 JournalEntry.IsBalanced() implemented
 JournalEntry.ValidateAndPost() throws if unbalanced
 Arabic error messages in all DomainExceptions
 IsSystemAccount prevents deletion/modification
 CreateDebit and CreateCredit are internal (only JournalEntry can create lines)
⚙️ Task 2 — Infrastructure (EF Core Configuration)
Task 2.1 — Account Configuration
csharp

// File: Infrastructure/Persistence/Configurations/AccountConfiguration.cs

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.NameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.NameEn)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AccountType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        // Unique index on AccountCode
        builder.HasIndex(x => x.AccountCode)
            .IsUnique()
            .HasDatabaseName("UQ_Accounts_Code");

        // Self-referencing relationship for sub-accounts
        builder.HasOne(x => x.ParentAccount)
            .WithMany(x => x.SubAccounts)
            .HasForeignKey(x => x.ParentAccountId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
Task 2.2 — JournalEntry Configuration
csharp

// File: Infrastructure/Persistence/Configurations/JournalEntryConfiguration.cs

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntryNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.EntryType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(50);

        builder.Property(x => x.ReferenceNumber)
            .HasMaxLength(50);

        // Unique entry number
        builder.HasIndex(x => x.EntryNumber)
            .IsUnique()
            .HasDatabaseName("UQ_JournalEntries_Number");

        // Composite index for date + type queries
        builder.HasIndex(x => new { x.TransactionDate, x.EntryType })
            .HasDatabaseName("IX_JournalEntries_Date_Type");

        // Reference lookup index
        builder.HasIndex(x => new { x.ReferenceType, x.ReferenceId })
            .HasDatabaseName("IX_JournalEntries_Reference");

        // Lines relationship
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.JournalEntry)
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-reference for reversals
        builder.HasOne<JournalEntry>()
            .WithMany()
            .HasForeignKey(x => x.ReversedByEntryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
Task 2.3 — JournalEntryLine Configuration
csharp

// File: Infrastructure/Persistence/Configurations/JournalEntryLineConfiguration.cs

public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines");
        builder.HasKey(x => x.Id);

        // Precision for financial values
        builder.Property(x => x.Debit)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.Credit)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(x => x.AccountCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.AccountNameAr)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        // Account relationship
        builder.HasOne(x => x.Account)
            .WithMany(x => x.JournalLines)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Performance indexes
        builder.HasIndex(x => x.JournalEntryId)
            .HasDatabaseName("IX_JournalEntryLines_EntryId");

        builder.HasIndex(x => x.AccountId)
            .HasDatabaseName("IX_JournalEntryLines_AccountId");
    }
}
✅ Task 2 Checklist
 All 3 configurations registered in AppDbContext.OnModelCreating()
 Decimal precision is (18,2) for all financial columns
 Unique indexes on EntryNumber and AccountCode
 Cascade delete on JournalEntryLines when entry deleted
 Restrict delete on Account (cannot delete account that has transactions)
⚙️ Task 3 — Application Layer (Services)
Task 3.1 — EntryNumber Generator
csharp

// File: Application/Accounting/Services/JournalEntryNumberGenerator.cs

public interface IJournalEntryNumberGenerator
{
    Task<string> GenerateAsync(CancellationToken ct = default);
}

public class JournalEntryNumberGenerator : IJournalEntryNumberGenerator
{
    private readonly AppDbContext _context;

    public JournalEntryNumberGenerator(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var prefix = $"JE-{today:yyyyMMdd}";

        // Count today's entries for sequential number
        var todayCount = await _context.JournalEntries
            .Where(e => e.EntryNumber.StartsWith(prefix))
            .CountAsync(ct);

        return $"{prefix}-{(todayCount + 1):D4}";
        // Result: "JE-20260520-0001"
    }
}
Task 3.2 — System Account Mappings Service
csharp

// File: Application/Accounting/Services/ISystemAccountService.cs

public interface ISystemAccountService
{
    Task<SystemAccountMappings> GetMappingsAsync(
        int? branchId = null,
        CancellationToken ct = default);

    Task<Account> GetAccountByCodeAsync(
        string accountCode,
        CancellationToken ct = default);
}

public class SystemAccountService : ISystemAccountService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SystemAccountService> _logger;

    public SystemAccountService(AppDbContext context, ILogger<SystemAccountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SystemAccountMappings> GetMappingsAsync(
        int? branchId = null,
        CancellationToken ct = default)
    {
        // Try branch-specific mapping first, then fall back to global
        var mapping = await _context.SystemAccountMappings
            .Include(m => m.DefaultCashAccount)
            .Include(m => m.DefaultBankAccount)
            .Include(m => m.InventoryAssetAccount)
            .Include(m => m.AccountsReceivableAccount)
            .Include(m => m.AccountsPayableAccount)
            .Include(m => m.VatOutputAccount)
            .Include(m => m.VatInputAccount)
            .Include(m => m.SalesRevenueAccount)
            .Include(m => m.SalesReturnAccount)
            .Include(m => m.CogsAccount)
            .Include(m => m.GeneralExpenseAccount)
            .Include(m => m.SpoilageLossAccount)
            .Where(m => m.BranchId == branchId || m.BranchId == null)
            .OrderByDescending(m => m.BranchId)  // Branch-specific first
            .FirstOrDefaultAsync(ct);

        if (mapping == null)
            throw new InvalidOperationException(
                "لم يتم إعداد ربط الحسابات الافتراضية. " +
                "يرجى الذهاب إلى الإعدادات ← إعدادات المحاسبة وتحديد الحسابات الافتراضية.");

        return mapping;
    }

    public async Task<Account> GetAccountByCodeAsync(
        string accountCode,
        CancellationToken ct = default)
    {
        return await _context.Accounts
            .FirstOrDefaultAsync(a =>
                a.AccountCode == accountCode && a.IsActive, ct)
            ?? throw new DomainException(
                $"الحساب بالكود '{accountCode}' غير موجود أو غير نشط.");
    }
}
Task 3.3 — Create Manual Journal Entry Command
csharp

// File: Application/Accounting/Commands/CreateJournalEntry/CreateJournalEntryCommand.cs

public record CreateJournalEntryCommand : IRequest<int>
{
    public DateTime TransactionDate { get; init; }
    public string Description { get; init; } = string.Empty;
    public JournalEntryType EntryType { get; init; } = JournalEntryType.Manual;
    public string? ReferenceType { get; init; }
    public int? ReferenceId { get; init; }
    public string? ReferenceNumber { get; init; }
    public int? BranchId { get; init; }
    public int CreatedBy { get; init; }
    public List<JournalEntryLineRequest> Lines { get; init; } = new();
}

public record JournalEntryLineRequest(
    int AccountId,
    decimal Debit,
    decimal Credit,
    string? Description
);
csharp

// File: Application/Accounting/Commands/CreateJournalEntry/CreateJournalEntryCommandValidator.cs

public class CreateJournalEntryCommandValidator
    : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("بيان القيد مطلوب")
            .MaximumLength(500);

        RuleFor(x => x.TransactionDate)
            .NotEmpty()
            .WithMessage("تاريخ القيد مطلوب")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1))
            .WithMessage("تاريخ القيد لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("يجب إضافة أسطر للقيد")
            .Must(lines => lines.Count >= 2)
            .WithMessage("القيد يجب أن يحتوي على سطرين على الأقل");

        RuleFor(x => x.Lines)
            .Must(lines =>
            {
                var totalDebit  = lines.Sum(l => l.Debit);
                var totalCredit = lines.Sum(l => l.Credit);
                return Math.Abs(totalDebit - totalCredit) < 0.001m;
            })
            .WithMessage(x =>
            {
                var d = x.Lines.Sum(l => l.Debit);
                var c = x.Lines.Sum(l => l.Credit);
                return $"القيد غير متوازن. المدين: {d:N2} — الدائن: {c:N2}";
            });

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId)
                .GreaterThan(0)
                .WithMessage("حساب القيد غير صالح");

            line.RuleFor(l => l)
                .Must(l =>
                    (l.Debit > 0 && l.Credit == 0) ||
                    (l.Credit > 0 && l.Debit == 0))
                .WithMessage("كل سطر يجب أن يكون إما مديناً أو دائناً وليس الاثنين معاً");
        });
    }
}
csharp

// File: Application/Accounting/Commands/CreateJournalEntry/CreateJournalEntryCommandHandler.cs

public class CreateJournalEntryCommandHandler
    : IRequestHandler<CreateJournalEntryCommand, int>
{
    private readonly AppDbContext _context;
    private readonly IJournalEntryNumberGenerator _numberGenerator;
    private readonly ILogger<CreateJournalEntryCommandHandler> _logger;

    public CreateJournalEntryCommandHandler(
        AppDbContext context,
        IJournalEntryNumberGenerator numberGenerator,
        ILogger<CreateJournalEntryCommandHandler> logger)
    {
        _context = context;
        _numberGenerator = numberGenerator;
        _logger = logger;
    }

    public async Task<int> Handle(
        CreateJournalEntryCommand command,
        CancellationToken cancellationToken)
    {
        // ─── 1. Generate entry number ───────────────────
        var entryNumber = await _numberGenerator.GenerateAsync(cancellationToken);

        // ─── 2. Load accounts for all lines ────────────
        var accountIds = command.Lines.Select(l => l.AccountId).Distinct().ToList();

        var accounts = await _context.Accounts
            .Where(a => accountIds.Contains(a.Id) && a.IsActive)
            .ToListAsync(cancellationToken);

        // Validate all accounts exist
        var missingIds = accountIds
            .Except(accounts.Select(a => a.Id))
            .ToList();

        if (missingIds.Any())
            throw new DomainException(
                $"الحسابات التالية غير موجودة: {string.Join(", ", missingIds)}");

        // ─── 3. Create journal entry ────────────────────
        var entry = JournalEntry.Create(
            entryNumber,
            command.TransactionDate,
            command.Description,
            command.EntryType,
            command.CreatedBy,
            command.ReferenceType,
            command.ReferenceId,
            command.ReferenceNumber,
            command.BranchId);

        // ─── 4. Add lines ───────────────────────────────
        foreach (var lineRequest in command.Lines)
        {
            var account = accounts.First(a => a.Id == lineRequest.AccountId);

            if (lineRequest.Debit > 0)
                entry.AddDebitLine(
                    account.Id,
                    account.AccountCode,
                    account.NameAr,
                    lineRequest.Debit,
                    lineRequest.Description);
            else
                entry.AddCreditLine(
                    account.Id,
                    account.AccountCode,
                    account.NameAr,
                    lineRequest.Credit,
                    lineRequest.Description);
        }

        // ─── 5. Validate and post ───────────────────────
        entry.ValidateAndPost(command.CreatedBy);

        // ─── 6. Save ────────────────────────────────────
        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Journal entry {Number} created. Type: {Type}. Total: {Total:N2}",
            entryNumber, command.EntryType, entry.TotalDebit);

        return entry.Id;
    }
}
✅ Task 3 Checklist
 JournalEntryNumberGenerator creates sequential daily numbers
 Validator rejects unbalanced entries BEFORE handler runs
 Handler validates all accounts exist before creating entry
 Entry is posted immediately upon creation (no draft journals)
 All services registered in DI container
📊 Task 4 — Basic Financial Queries
Task 4.1 — Get Account Balance Query
csharp

// File: Application/Accounting/Queries/GetAccountBalance/GetAccountBalanceQuery.cs

public record GetAccountBalanceQuery(
    int AccountId,
    DateTime? AsOfDate = null
) : IRequest<AccountBalanceDto>;

public record AccountBalanceDto(
    int AccountId,
    string AccountCode,
    string AccountNameAr,
    AccountType AccountType,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Balance,
    bool IsDebitNormal
);

public class GetAccountBalanceHandler
    : IRequestHandler<GetAccountBalanceQuery, AccountBalanceDto>
{
    private readonly AppDbContext _context;

    public GetAccountBalanceHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AccountBalanceDto> Handle(
        GetAccountBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);

        var query = _context.JournalEntryLines
            .AsNoTracking()
            .Where(l =>
                l.AccountId == request.AccountId &&
                l.JournalEntry.IsPosted);

        if (request.AsOfDate.HasValue)
            query = query.Where(l =>
                l.JournalEntry.TransactionDate <= request.AsOfDate.Value);

        var totals = await query
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                TotalDebit  = g.Sum(l => l.Debit),
                TotalCredit = g.Sum(l => l.Credit)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalDebit  = totals?.TotalDebit  ?? 0;
        var totalCredit = totals?.TotalCredit ?? 0;
        var isDebitNormal = account.IsDebitNormal();

        // Balance direction depends on account type
        var balance = isDebitNormal
            ? totalDebit - totalCredit   // Assets/Expenses: Debit increases balance
            : totalCredit - totalDebit;  // Liabilities/Equity/Revenue: Credit increases balance

        return new AccountBalanceDto(
            account.Id,
            account.AccountCode,
            account.NameAr,
            account.AccountType,
            totalDebit,
            totalCredit,
            balance,
            isDebitNormal);
    }
}
Task 4.2 — Get Account Statement Query
csharp

// File: Application/Accounting/Queries/GetAccountStatement/GetAccountStatementQuery.cs

public record GetAccountStatementQuery(
    int AccountId,
    DateTime StartDate,
    DateTime EndDate
) : IRequest<AccountStatementDto>;

public record AccountStatementDto(
    string AccountCode,
    string AccountNameAr,
    decimal OpeningBalance,
    List<AccountStatementLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance
);

public record AccountStatementLineDto(
    DateTime Date,
    string EntryNumber,
    string Description,
    string ReferenceNumber,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance
);

public class GetAccountStatementHandler
    : IRequestHandler<GetAccountStatementQuery, AccountStatementDto>
{
    private readonly AppDbContext _context;

    public GetAccountStatementHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AccountStatementDto> Handle(
        GetAccountStatementQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new NotFoundException("Account", request.AccountId);

        // Opening balance = all transactions BEFORE start date
        var openingQuery = await _context.JournalEntryLines
            .AsNoTracking()
            .Where(l =>
                l.AccountId == request.AccountId &&
                l.JournalEntry.IsPosted &&
                l.JournalEntry.TransactionDate < request.StartDate)
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                Debit  = g.Sum(l => l.Debit),
                Credit = g.Sum(l => l.Credit)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var openingBalance = account.IsDebitNormal()
            ? (openingQuery?.Debit ?? 0) - (openingQuery?.Credit ?? 0)
            : (openingQuery?.Credit ?? 0) - (openingQuery?.Debit ?? 0);

        // Period transactions
        var lines = await _context.JournalEntryLines
            .AsNoTracking()
            .Include(l => l.JournalEntry)
            .Where(l =>
                l.AccountId == request.AccountId &&
                l.JournalEntry.IsPosted &&
                l.JournalEntry.TransactionDate >= request.StartDate &&
                l.JournalEntry.TransactionDate <= request.EndDate)
            .OrderBy(l => l.JournalEntry.TransactionDate)
            .ThenBy(l => l.JournalEntry.EntryNumber)
            .ToListAsync(cancellationToken);

        // Calculate running balance
        var runningBalance = openingBalance;
        var statementLines = new List<AccountStatementLineDto>();

        foreach (var line in lines)
        {
            if (account.IsDebitNormal())
                runningBalance += line.Debit - line.Credit;
            else
                runningBalance += line.Credit - line.Debit;

            statementLines.Add(new AccountStatementLineDto(
                line.JournalEntry.TransactionDate,
                line.JournalEntry.EntryNumber,
                line.Description ?? line.JournalEntry.Description,
                line.JournalEntry.ReferenceNumber ?? string.Empty,
                line.Debit,
                line.Credit,
                runningBalance));
        }

        return new AccountStatementDto(
            account.AccountCode,
            account.NameAr,
            openingBalance,
            statementLines,
            lines.Sum(l => l.Debit),
            lines.Sum(l => l.Credit),
            runningBalance);
    }
}
✅ Task 4 Checklist
 All queries use AsNoTracking()
 Opening balance calculated correctly (before period)
 Running balance direction based on IsDebitNormal()
 Only IsPosted = true entries included in statements
 Results ordered by date then entry number
🧪 Task 5 — Unit Tests
csharp

// File: Tests/Domain/JournalEntryTests.cs

public class JournalEntryTests
{
    // Sample account data
    private static readonly int CashId   = 1;
    private static readonly int SalesId  = 2;

    [Fact]
    public void IsBalanced_EqualDebitsAndCredits_ReturnsTrue()
    {
        var entry = CreateEntry();
        entry.AddDebitLine(CashId,  "1101", "الصندوق",           1000);
        entry.AddCreditLine(SalesId,"4101", "إيرادات المبيعات",  1000);

        Assert.True(entry.IsBalanced());
    }

    [Fact]
    public void IsBalanced_UnequalAmounts_ReturnsFalse()
    {
        var entry = CreateEntry();
        entry.AddDebitLine(CashId,  "1101", "الصندوق",          1000);
        entry.AddCreditLine(SalesId,"4101", "إيرادات المبيعات",  900);

        Assert.False(entry.IsBalanced());
    }

    [Fact]
    public void ValidateAndPost_UnbalancedEntry_ThrowsDomainException()
    {
        var entry = CreateEntry();
        entry.AddDebitLine(CashId,  "1101", "الصندوق",          500);
        entry.AddCreditLine(SalesId,"4101", "إيرادات المبيعات",  300);

        var ex = Assert.Throws<DomainException>(
            () => entry.ValidateAndPost(postedBy: 1));

        Assert.Contains("غير متوازن", ex.Message);
    }

    [Fact]
    public void ValidateAndPost_EmptyLines_ThrowsDomainException()
    {
        var entry = CreateEntry();

        var ex = Assert.Throws<DomainException>(
            () => entry.ValidateAndPost(postedBy: 1));

        Assert.Contains("بدون أسطر", ex.Message);
    }

    [Fact]
    public void ValidateAndPost_BalancedEntry_SetsIsPostedTrue()
    {
        var entry = CreateEntry();
        entry.AddDebitLine(CashId,  "1101", "الصندوق",          1000);
        entry.AddCreditLine(SalesId,"4101", "إيرادات المبيعات", 1000);

        entry.ValidateAndPost(postedBy: 1);

        Assert.True(entry.IsPosted);
        Assert.Equal(1, entry.PostedBy);
        Assert.NotNull(entry.PostedAt);
    }

    [Fact]
    public void AddDebitLine_AfterPosted_ThrowsDomainException()
    {
        var entry = CreateEntry();
        entry.AddDebitLine(CashId,  "1101", "الصندوق",          1000);
        entry.AddCreditLine(SalesId,"4101", "إيرادات المبيعات", 1000);
        entry.ValidateAndPost(postedBy: 1);

        var ex = Assert.Throws<DomainException>(
            () => entry.AddDebitLine(CashId, "1101", "الصندوق", 500));

        Assert.Contains("مرحّل", ex.Message);
    }

    [Fact]
    public void MultiLineEntry_ComplexSalesJournal_IsBalanced()
    {
        // Sale: 1000 + VAT 150 = 1150 total, Cost = 700
        var entry = CreateEntry();

        // Cash in
        entry.AddDebitLine(1,  "1101", "الصندوق",                1150);

        // Sales revenue + VAT
        entry.AddCreditLine(2, "4101", "إيرادات المبيعات",       1000);
        entry.AddCreditLine(3, "2201", "ضريبة القيمة المضافة",    150);

        // COGS
        entry.AddDebitLine(4,  "5101", "تكلفة البضاعة المباعة",   700);
        entry.AddCreditLine(5, "1201", "أصل المخزون",             700);

        Assert.True(entry.IsBalanced());
        Assert.Equal(1850, entry.TotalDebit);
        Assert.Equal(1850, entry.TotalCredit);
    }

    [Fact]
    public void Account_IsDebitNormal_AssetReturnsTrue()
    {
        var account = Account.Create("1101", "الصندوق", "Cash", AccountType.Asset);
        Assert.True(account.IsDebitNormal());
    }

    [Fact]
    public void Account_IsDebitNormal_RevenueReturnsFalse()
    {
        var account = Account.Create("4101", "مبيعات", "Sales", AccountType.Revenue);
        Assert.False(account.IsDebitNormal());
    }

    [Fact]
    public void Account_UpdateSystemAccount_ThrowsDomainException()
    {
        var account = Account.Create(
            "1101", "الصندوق", "Cash",
            AccountType.Asset,
            isSystemAccount: true);

        var ex = Assert.Throws<DomainException>(
            () => account.Update("اسم جديد", "New Name", null));

        Assert.Contains("حساب نظام", ex.Message);
    }

    private static JournalEntry CreateEntry() =>
        JournalEntry.Create(
            "JE-20260520-0001",
            DateTime.Today,
            "قيد اختبار",
            JournalEntryType.Manual,
            createdBy: 1);
}
✅ Task 5 Checklist
 9 unit tests all passing
 Multi-line complex entry test confirms 1850 = 1850
 IsDebitNormal() tested for Asset (true) and Revenue (false)
 System account modification throws correct Arabic message
📦 Final Summary for Phase 17
text

┌────────────────────────────────────────────────────────────────────┐
│              PHASE 17 — ACCOUNTING FOUNDATION                      │
│              Implementation Order                                  │
├──────┬─────────────────────────────────────────────┬──────────────┤
│ Task │ Deliverable                                 │ Must Pass    │
├──────┼─────────────────────────────────────────────┼──────────────┤
│  0   │ SQL: 6 tables + seed data                   │ Migration OK │
│  1   │ Domain: Account, JournalEntry, Lines,       │ No DB refs   │
│      │         SystemAccountMappings               │ in Domain    │
│  2   │ EF Core: 3 configurations                   │ Precision    │
│      │                                             │ (18,2)       │
│  3   │ Application: Generator, Service,            │ Validator    │
│      │              Command + Validator + Handler  │ runs first   │
│  4   │ Queries: Balance + Statement                │ AsNoTracking │
│  5   │ Tests: 9 unit tests                         │ All green    │
└──────┴─────────────────────────────────────────────┴──────────────┘

RULES — ZERO TOLERANCE:
━━━━━━━━━━━━━━━━━━━━━━
✅ JournalEntry.ValidateAndPost() called BEFORE SaveChanges
✅ IsBalanced() must return true or SaveChanges never called
✅ Decimal precision is (18,2) — not (18,2)
✅ IsSystemAccount = true accounts cannot be edited or deleted
✅ JournalEntryLine.CreateDebit/Credit are internal — only JournalEntry creates lines
✅ All financial queries use AsNoTracking
✅ Only IsPosted = true entries appear in financial reports
✅ Arabic error messages in ALL DomainExceptions
✅ Account snapshots (Code + Name) stored in JournalEntryLine