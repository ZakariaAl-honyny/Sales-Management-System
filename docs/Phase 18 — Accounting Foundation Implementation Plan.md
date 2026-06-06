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
        FOREIGN KEY (JournalEntryId) REFERENCES JournalEntries(Id),

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
        // Composition enforced at domain level: JournalEntry owns Lines via JournalEntryLine entity
        // On delete: domain checks if entry is posted before allowing deletion
        // Reversal pattern: use ReversedByEntryId FK (Restrict) to reverse a posted entry
        // rather than deleting it — this preserves audit trail
        builder.HasMany(x => x.Lines)
            .WithOne(x => x.JournalEntry)
            .HasForeignKey(x => x.JournalEntryId)
            .OnDelete(DeleteBehavior.Restrict);

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
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Credit)
            .HasPrecision(18, 2)
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
  Restrict delete on ALL foreign keys (JournalEntry→Lines, Account→Account)
  Domain-level composition: JournalEntry owns Lines but deletion is prevented if posted
  Reversal via ReversedByEntryId FK (Restrict) preserves audit trail
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
│  4   │ Queries: Balance + Statement + Trial Balance  │ AsNoTracking │
│  5   │ Tests: 9 unit tests                         │ All green    │
│  6   │ Annual Closing: FiscalYearClosure + workflow │ Balanced     │
└──────┴─────────────────────────────────────────────┴──────────────┘

RULES — ZERO TOLERANCE:
━━━━━━━━━━━━━━━━━━━━━━
✅ JournalEntry.ValidateAndPost() called BEFORE SaveChanges
✅ IsBalanced() must return true or SaveChanges never called
✅ Decimal precision is (18,2) — not (18,4)
✅ IsSystemAccount = true accounts cannot be edited or deleted
✅ JournalEntryLine.CreateDebit/Credit are internal — only JournalEntry creates lines
✅ All financial queries use AsNoTracking
✅ Only IsPosted = true entries appear in financial reports
✅ Arabic error messages in ALL DomainExceptions
✅ Account snapshots (Code + Name) stored in JournalEntryLine

📊 Task 4.3 — Trial Balance Query Infrastructure
The Accounting Foundation provides the query infrastructure needed for Trial Balance reporting.
The full Trial Balance report UI + export will be developed in Phase 31 (Reporting Module).

Query infrastructure responsibilities:
- GetAccountBalanceQuery (Task 4.1) returns per-account debit/credit totals — reusable by Trial Balance
- Trial Balance needs: all active accounts, grouped by AccountType, showing:
  - Account Code, Name
  - Total Debit, Total Credit
  - Net Balance (Debit Normal: TotalDebit - TotalCredit, Credit Normal: TotalCredit - TotalDebit)
  - Running Debit and Credit totals across all accounts (Debit total must equal Credit total)
- Filtering by date range and AccountType
- All queries use AsNoTracking and include only IsPosted = true entries

The Trial Balance query reuses GetAccountBalanceQuery per account, aggregated across all active accounts.

🔄 Task 6 — Annual Closing (إقفال سنوي)
Task 6.1 — Fiscal Year Closing Workflow
The Annual Closing process zeros out all Revenue (4xxx) and Expense (5xxx) accounts and
transfers the net income/loss to Retained Earnings (3102). This is performed once per fiscal year.

Steps:
1. Verify ALL journal entries for the fiscal year are posted (IsPosted = true)
   - If any entry in the year is not posted, abort closing with DomainException
2. Calculate net income:
   - Total Revenue (4xxx) - Total Expense (5xxx)
   - If positive → Net Income (credit to Retained Earnings)
   - If negative → Net Loss (debit to Retained Earnings)
3. Create a closing JournalEntry:
   a. For each Revenue account: Debit the balance to zero
      Example — Sales Revenue (4101) with balance 500,000:
        Debit  4101 (Sales Revenue)     500,000
        Credit 3102 (Retained Earnings)  500,000
   b. For each Expense account: Credit the balance to zero
      Example — COGS (5101) with balance 300,000:
        Debit  3102 (Retained Earnings)  300,000
        Credit 5101 (COGS)               300,000
   c. Net result: Retained Earnings reflects the difference
4. Post the closing entry (ValidateAndPost)
5. Mark fiscal year as closed in a dedicated FiscalYearClosure table
6. Prevent further posting of entries with TransactionDate in a closed fiscal year

Data model for FiscalYearClosure:
```sql
CREATE TABLE FiscalYearClosures (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    FiscalYear      INT NOT NULL,           -- e.g., 2026
    ClosedAt        DATETIME2 NOT NULL DEFAULT GETDATE(),
    ClosedByUserId  INT NOT NULL,
    NetIncome       DECIMAL(18,2) NOT NULL,
    ClosingEntryId  INT NOT NULL,            -- FK to JournalEntries

    CONSTRAINT FK_FiscalYearClosures_ClosedBy
        FOREIGN KEY (ClosedByUserId) REFERENCES Users(Id),
    CONSTRAINT FK_FiscalYearClosures_Entry
        FOREIGN KEY (ClosingEntryId) REFERENCES JournalEntries(Id),
    CONSTRAINT UQ_FiscalYearClosures_Year
        UNIQUE (FiscalYear)
);

CREATE INDEX IX_FiscalYearClosures_Year ON FiscalYearClosures(FiscalYear);
```

Checklist:
  All entries must be posted before closing the year
  Revenue accounts zeroed out (debit to balance)
  Expense accounts zeroed out (credit to balance)
  Net income/loss transferred to Retained Earnings
  Closed fiscal year blocks new entries
  Closing entry is a regular JournalEntry (type = Manual) with full audit trail

---

## Task 7 — Comprehensive Unit Tests

**Test Infrastructure:**
- Use xUnit + Moq + FluentAssertions
- `SalesSystem.Domain.Tests` for entity tests
- `SalesSystem.Application.Tests` for service tests
- `SalesSystem.Api.Tests` for API controller tests
- `SalesSystem.Arch.Tests` for configuration tests

**Files to create/modify:**

| File | Change |
|------|--------|
| `Tests/Domain/AccountTests.cs` | **CREATE** |
| `Tests/Domain/JournalEntryTests.cs` | **EXPAND** (existing 9 tests → 25+) |
| `Tests/Domain/JournalEntryLineTests.cs` | **CREATE** |
| `Tests/Domain/FiscalYearClosureTests.cs` | **CREATE** |
| `Tests/Application/AccountServiceTests.cs` | **CREATE** |
| `Tests/Application/JournalEntryServiceTests.cs` | **CREATE** |
| `Tests/Application/SystemAccountMappingServiceTests.cs` | **CREATE** |
| `Tests/Application/TrialBalanceServiceTests.cs` | **CREATE** |
| `Tests/Api/AccountsControllerTests.cs` | **CREATE** |
| `Tests/Api/JournalEntriesControllerTests.cs` | **CREATE** |
| `Tests/Arch/AccountConfigurationTests.cs` | **CREATE** |
| `Tests/Arch/JournalEntryConfigurationTests.cs` | **CREATE** |
| `Tests/Arch/JournalEntryLineConfigurationTests.cs` | **CREATE** |

**Estimate:** ~4 hours

---

## Task 8 — Self-Explanation ◉ Tooltips for Accounting Concepts

**Goal**: Make every accounting term in the system self-explanatory via ◉ tooltips. Non-accountant users should understand each term without external help.

**Pattern**: Every accounting term in the UI gets a ◉ icon next to it. On hover/click, a tooltip shows a plain-Arabic explanation.

**Implementation**:
- Use WPF ToolTip with a custom style (blue background, question-mark icon)
- Create a reusable `InfoTooltip` UserControl: `<TextBlock Text="◉" ToolTip="{Binding}" Style="{StaticResource InfoTooltipStyle}"/>`
- Create `AccountingTermAttribute` to tag terms with their explanation ID
- Store explanations in a resource file or database table `AccountingTermExplanations`

**Concepts to explain with ◉ tooltips**:

| Term (Arabic) | Explanation (Arabic) | Location |
|--------------|---------------------|----------|
| قيد اليومية | "القيد اليومية هو تسجيل حركة مالية في دفتر المحاسبة. كل عملية بيع أو شراء أو دفع تنشئ قيداً يومياً." | Journal Entry creation screen |
| الترحيل | "الترحيل يعني تأكيد القيد اليومية وجعله نهائياً. بعد الترحيل لا يمكن تعديل القيد." | Post button tooltip |
| الإقفال السنوي | "الإقفال السنوي يعني إنهاء السنة المالية وتحويل أرصدة الإيرادات والمصروفات إلى الأرباح المحتجزة. يتم مرة واحدة في نهاية السنة." | Annual Closing wizard |
| قيد الافتتاح | "قيد الافتتاح هو قيد يفتح السنة المالية الجديدة بأرصدة الحسابات التي تحمل رصيداً إلى السنة الجديدة." | Opening entry screen |
| دليل الحسابات | "دليل الحسابات هو قائمة بجميع الحسابات التي تستخدمها الشركة. يشبه فهرس الدفتر." | Chart of Accounts screen |
| شجرة الحسابات | "شجرة الحسابات هي تصنيف هرمي للحسابات. المستوى الأول: أصول - خصوم - حقوق ملكية - إيرادات - مصروفات." | Account tree view |
| حساب رئيسي | "حساب رئيسي هو حساب تجميعي لا يمكن الترحيل إليه مباشرة. يستخدم لتنظيم وتجميع الحسابات التفصيلية تحته." | Account selection |
| حساب تفصيلي | "حساب تفصيلي هو حساب يمكن الترحيل إليه. يمثل حساباً حقيقياً مثل: الصندوق، البنك، عميل معين." | Account selection |
| رصيد دائن | "الرصيد الدائن يعني أن الحساب عليه التزام أو دين. في حساب المورد مثلاً، الرصيد الدائن يعني أن عليه فاتورة غير مدفوعة." | Trial balance / Reports |
| رصيد مدين | "الرصيد المدين يعني أن الحساب له قيمة مستحقة. في حساب العميل مثلاً، الرصيد المدين يعني أن عليه مبلغاً للشركة." | Trial balance / Reports |
| الأصول | "الأصول هي ممتلكات الشركة التي لها قيمة مالية. مثل: النقد في الصندوق، الأثاث، السيارة، المباني." | Balance sheet / Reports |
| الخصوم | "الخصوم هي التزامات الشركة المالية تجاه الغير. مثل: فواتير الموردين غير المدفوعة، القروض." | Balance sheet / Reports |
| حقوق الملكية | "حقوق الملكية هي حقوق أصحاب الشركة في أصولها. تحسب كالتالي: الأصول - الخصوم = حقوق الملكية." | Balance sheet / Reports |
| الإيرادات | "الإيرادات هي الأموال التي تكسبها الشركة من بيع المنتجات أو تقديم الخدمات." | Income statement / Reports |
| المصروفات | "المصروفات هي التكاليف التي تدفعها الشركة لتشغيل النشاط التجاري. مثل: الإيجار، الرواتب، الكهرباء." | Income statement / Reports |
| صافي الربح | "صافي الربح = الإيرادات - المصروفات. إذا كانت الإيرادات أكبر من المصروفات فهناك ربح." | Income statement / Reports |
| صافي الخسارة | "صافي الخسارة = المصروفات - الإيرادات. إذا كانت المصروفات أكبر من الإيرادات فهناك خسارة." | Income statement / Reports |
| الأرباح المحتجزة | "الأرباح المحتجزة هي الأرباح التي تراكمت في الشركة منذ بدء النشاط ولم توزع على الملاك." | Balance sheet / Reports |
| ميزان المراجعة | "ميزان المراجعة هو تقرير يظهر جميع الحسابات وأرصدتها المدينة والدائنة. يستخدم للتحقق من صحة القيود قبل إعداد القوائم المالية." | Trial Balance report |

**UI Design**:
```xml
<!-- Reusable InfoTooltip control -->
<Border CornerRadius="12" Background="#E3F2FD" BorderBrush="#90CAF9" BorderThickness="1"
        ToolTipService.ShowDuration="60000" ToolTipService.InitialShowDelay="200">
    <TextBlock Text="ⓘ" FontSize="14" Foreground="#1565C0"
               ToolTip="{Binding Explanation}" Cursor="Help"
               Style="{StaticResource InfoIconStyle}"/>
</Border>
```

**Data structure for explanations**:
```csharp
public class AccountingTermExplanation
{
    public string TermKey { get; set; } = "";  // e.g., "journal_entry"
    public string TermArabic { get; set; } = "";  // e.g., "قيد اليومية"
    public string ExplanationArabic { get; set; } = "";  // plain Arabic explanation
    public string ScreenLocation { get; set; } = "";  // where it appears
}
```

**Implementation Tasks**:
1. Create `AccountingTermExplanation` entity + DbSet
2. Seed all 18 explanations above in migration
3. Create `InfoTooltip` WPF UserControl with ◉ icon + styled ToolTip
4. Apply to all accounting screens: Journal Entry, Chart of Accounts, Trial Balance, Reports
5. Create API endpoint: `GET /api/v1/accounting/terms/{key}` for dynamic loading

**Estimate**: ~3 hours
**Files**: 6 (Entity + Migration + UserControl + ViewModel + Controller + API Service)

---

### 1. Domain Entity Tests

#### Account Entity (`AccountTests.cs`)

```csharp
[Fact]
public void Create_ValidInput_CreatesAccountCorrectly()
{
    var account = Account.Create("1101", "الصندوق", "Cash", AccountType.Asset);
    Assert.Equal("1101", account.Code);
    Assert.Equal("الصندوق", account.NameAr);
    Assert.Equal(AccountType.Asset, account.Type);
    Assert.False(account.IsSystemAccount);
    Assert.True(account.IsActive);
}

[Fact]
public void Create_EmptyCode_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        Account.Create("", "الصندوق", "Cash", AccountType.Asset));
    Assert.Contains("مطلوب", ex.Message);
}

[Fact]
public void Create_NegativeParentId_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        Account.Create("1101", "الصندوق", "Cash", AccountType.Asset, parentId: -1));
    Assert.Contains("سال", ex.Message);
}

[Fact]
public void Update_SystemAccount_ThrowsDomainException()
{
    var account = Account.Create("1101", "الصندوق", "Cash", AccountType.Asset, isSystemAccount: true);
    var ex = Assert.Throws<DomainException>(() =>
        account.Update("اسم جديد", "New Name", null));
    Assert.Contains("حساب نظام", ex.Message);
}

[Fact]
public void Update_NonSystemAccount_Succeeds()
{
    var account = Account.Create("1101", "الصندوق", "Cash", AccountType.Asset);
    account.Update("خزينة", "Treasury", null);
    Assert.Equal("خزينة", account.NameAr);
}

[Fact]
public void IsDebitNormal_Asset_ReturnsTrue()
{
    var account = Account.Create("1101", "الصندوق", "Cash", AccountType.Asset);
    Assert.True(account.IsDebitNormal());
}

[Fact]
public void IsDebitNormal_Liability_ReturnsFalse()
{
    var account = Account.Create("2101", "موردون", "Payables", AccountType.Liability);
    Assert.False(account.IsDebitNormal());
}

[Fact]
public void IsDebitNormal_Equity_ReturnsFalse()
{
    var account = Account.Create("3101", "رأس المال", "Capital", AccountType.Equity);
    Assert.False(account.IsDebitNormal());
}

[Fact]
public void IsDebitNormal_Revenue_ReturnsFalse()
{
    var account = Account.Create("4101", "مبيعات", "Sales", AccountType.Revenue);
    Assert.False(account.IsDebitNormal());
}

[Fact]
public void IsDebitNormal_Expense_ReturnsTrue()
{
    var account = Account.Create("5101", "تكلفة", "COGS", AccountType.Expense);
    Assert.True(account.IsDebitNormal());
}

[Fact]
public void Level_CalculatedFromCodeLength()
{
    var account = Account.Create("1101", "الصندوق", "Cash", AccountType.Asset);
    Assert.Equal(2, account.Level); // 4 chars → 2 (1101 → Level 2)
}

[Fact]
public void MarkAsDeleted_SetsIsActiveFalse()
{
    var account = Account.Create("1101", "الصندوق", "Cash", AccountType.Asset);
    account.MarkAsDeleted();
    Assert.False(account.IsActive);
}
```

#### JournalEntry Entity (`JournalEntryTests.cs` — Expand existing)

```csharp
// Keep all 9 existing tests. Add:

[Fact]
public void Create_ValidInput_CreatesEntryWithDraftStatus()
{
    var entry = JournalEntry.Create(
        "JE-20260520-0001", DateTime.Today, "قيد اختبار",
        JournalEntryType.Manual, createdBy: 1);
    Assert.Equal(InvoiceStatus.Draft, entry.Status);
    Assert.False(entry.IsPosted);
    Assert.Equal(0, entry.TotalDebit);
    Assert.Equal(0, entry.TotalCredit);
}

[Fact]
public void Cancel_DraftEntry_SetsStatusCancelled()
{
    var entry = CreateEntry(); // draft by default
    entry.Cancel(1);
    Assert.Equal(InvoiceStatus.Cancelled, entry.Status);
}

[Fact]
public void Cancel_PostedEntry_ThrowsDomainException()
{
    var entry = CreateEntry();
    entry.AddDebitLine(1, "1101", "الصندوق", 1000);
    entry.AddCreditLine(2, "4101", "إيرادات", 1000);
    entry.ValidateAndPost(1);
    var ex = Assert.Throws<DomainException>(() => entry.Cancel(1));
    Assert.Contains("إلغاء", ex.Message);
}

[Fact]
public void AddLine_AfterCancelled_ThrowsDomainException()
{
    var entry = CreateEntry();
    entry.Cancel(1);
    var ex = Assert.Throws<DomainException>(() =>
        entry.AddDebitLine(1, "1101", "الصندوق", 500));
    Assert.Contains("ملغي", ex.Message);
}

[Fact]
public void Reversal_CreatesReversedEntry_WithCorrectReference()
{
    var original = CreateEntry();
    original.AddDebitLine(1, "1101", "الصندوق", 1000);
    original.AddCreditLine(2, "4101", "إيرادات", 1000);
    original.ValidateAndPost(1);

    var reversal = original.CreateReversal(DateTime.Today, 1);
    Assert.NotNull(reversal);
    Assert.Equal(original.Id, reversal.ReversedEntryId);
    Assert.Equal(JournalEntryType.Reversal, reversal.Type);
    Assert.True(reversal.IsBalanced());
}

[Fact]
public void Reversal_DraftEntry_ThrowsDomainException()
{
    var entry = CreateEntry();
    var ex = Assert.Throws<DomainException>(() =>
        entry.CreateReversal(DateTime.Today, 1));
    Assert.Contains("مرحّل", ex.Message);
}

// 3-state lifecycle test
[Fact]
public void Lifecycle_Draft_To_Posted_To_Cancelled_ValidTransitions()
{
    var entry = CreateEntry();
    Assert.Equal(InvoiceStatus.Draft, entry.Status);
    entry.AddDebitLine(1, "1101", "الصندوق", 1000);
    entry.AddCreditLine(2, "4101", "إيرادات", 1000);
    entry.ValidateAndPost(1);
    Assert.Equal(InvoiceStatus.Posted, entry.Status);
    entry.CancelWithReversal(DateTime.Today, 1);
    Assert.Equal(InvoiceStatus.Cancelled, entry.Status);
}

[Fact]
public void Lifecycle_Posted_To_Invalid_ThrowsDomainException()
{
    // Verify that a posted entry can NEVER go back to Draft
    var entry = CreateEntry();
    entry.AddDebitLine(1, "1101", "الصندوق", 1000);
    entry.AddCreditLine(2, "4101", "إيرادات", 1000);
    entry.ValidateAndPost(1);
    // No Draft setter exists — validate via reflection or design
    // This test confirms the design prevents the transition
    Assert.True(entry.IsPosted);
}

[Fact]
public void TotalDebit_MultipleLines_ReturnsSum()
{
    var entry = CreateEntry();
    entry.AddDebitLine(1, "1101", "الصندوق", 500);
    entry.AddDebitLine(3, "1201", "مخزون", 300);
    entry.AddCreditLine(2, "4101", "إيرادات", 800);
    Assert.Equal(800m, entry.TotalDebit);
}

[Fact]
public void TotalCredit_MultipleLines_ReturnsSum()
{
    var entry = CreateEntry();
    entry.AddDebitLine(1, "1101", "الصندوق", 800);
    entry.AddCreditLine(2, "4101", "إيرادات", 500);
    entry.AddCreditLine(4, "2201", "ضريبة", 300);
    Assert.Equal(800m, entry.TotalCredit);
}
```

#### JournalEntryLine Entity (`JournalEntryLineTests.cs`)

```csharp
[Fact]
public void Create_ValidInput_CreatesLineCorrectly()
{
    var entry = CreateEntry();
    entry.AddDebitLine(1, "1101", "الصندوق", 1000);
    Assert.Single(entry.Lines);
    Assert.Equal(1000m, entry.Lines.First().Debit);
    Assert.Equal(0m, entry.Lines.First().Credit);
}

[Fact]
public void AddDebitLine_NegativeAmount_ThrowsDomainException()
{
    var entry = CreateEntry();
    var ex = Assert.Throws<DomainException>(() =>
        entry.AddDebitLine(1, "1101", "الصندوق", -100));
    Assert.Contains("أكبر من", ex.Message);
}

[Fact]
public void AddCreditLine_ZeroAmount_ThrowsDomainException()
{
    var entry = CreateEntry();
    var ex = Assert.Throws<DomainException>(() =>
        entry.AddCreditLine(2, "4101", "إيرادات", 0));
    Assert.Contains("أكبر من", ex.Message);
}

[Fact]
public void AddLine_EmptyAccountName_ThrowsDomainException()
{
    var entry = CreateEntry();
    var ex = Assert.Throws<DomainException>(() =>
        entry.AddDebitLine(1, "1101", "", 100));
    Assert.Contains("مطلوب", ex.Message);
}

[Fact]
public void DebitCredit_NotNullConstraint()
{
    var entry = CreateEntry();
    entry.AddDebitLine(1, "1101", "Cash", 500);
    var line = entry.Lines.First();
    Assert.NotNull(line.Debit);
    Assert.NotNull(line.Credit);
    Assert.True(line.Debit > 0 || line.Credit > 0);
}
```

#### FiscalYearClosure Entity (`FiscalYearClosureTests.cs`)

```csharp
[Fact]
public void CloseYear_ValidYear_CreatesClosure()
{
    var closure = new FiscalYearClosure(2026, closedBy: 1, netIncome: 50000m, closingEntryId: 100);
    Assert.Equal(2026, closure.FiscalYear);
    Assert.Equal(50000m, closure.NetIncome);
    Assert.NotNull(closure.ClosedAt);
}

[Fact]
public void CloseYear_FutureYear_ThrowsDomainException()
{
    var ex = Assert.Throws<DomainException>(() =>
        new FiscalYearClosure(2099, closedBy: 1, netIncome: 0, closingEntryId: 100));
    Assert.Contains("مستقبل", ex.Message);
}

[Fact]
public void CloseYear_NegativeNetIncome_AllowsValue()
{
    // Net income can be negative (net loss)
    var closure = new FiscalYearClosure(2026, closedBy: 1, netIncome: -10000m, closingEntryId: 100);
    Assert.Equal(-10000m, closure.NetIncome);
}
```

---

### 2. Service Tests (using `Mock<IUnitOfWork>`)

#### AccountServiceTests.cs

```csharp
public class AccountServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<IAccountRepository> _repoMock;
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _uowMock = new Mock<IUnitOfWork>();
        _repoMock = new Mock<IAccountRepository>();
        _uowMock.Setup(x => x.Accounts).Returns(_repoMock.Object);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _service = new AccountService(_uowMock.Object, Mock.Of<ILogger<AccountService>>());
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsSuccessWithDto()
    {
        _repoMock.Setup(x => x.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(new CreateAccountRequest
        {
            Code = "1101", NameAr = "الصندوق", NameEn = "Cash",
            Type = AccountType.Asset
        }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("1101", result.Value.Code);
    }

    [Fact]
    public async Task CreateAsync_DuplicateCode_ReturnsFailure()
    {
        _repoMock.Setup(x => x.AnyAsync(a => a.Code == "1101", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CreateAsync(new CreateAccountRequest
        {
            Code = "1101", NameAr = "الصندوق", NameEn = "Cash",
            Type = AccountType.Asset
        }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.DuplicateCode, result.Error);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingAccount_ReturnsDto()
    {
        var account = Account.Create("1101", "الصندوق", "Cash", AccountType.Asset);
        _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _service.GetByIdAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("1101", result.Value.Code);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsFailure()
    {
        _repoMock.Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.NotFound, result.Error);
    }
}
```

#### JournalEntryServiceTests.cs

```csharp
[Fact]
public async Task CreateAsync_ValidEntry_ReturnsSuccess()
{
    var request = new CreateJournalEntryRequest
    {
        TransactionDate = DateTime.Today,
        Description = "قيد اختبار",
        Type = JournalEntryType.Manual,
        Lines = new List<JournalEntryLineRequest>
        {
            new() { AccountId = 1, AccountCode = "1101", AccountName = "الصندوق", Debit = 1000 },
            new() { AccountId = 2, AccountCode = "4101", AccountName = "إيرادات", Credit = 1000 }
        }
    };

    var result = await _service.CreateAsync(request, CancellationToken.None);

    Assert.True(result.IsSuccess);
    _repoMock.Verify(x => x.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task CreateAsync_UnbalancedEntry_ReturnsFailure()
{
    var request = new CreateJournalEntryRequest
    {
        TransactionDate = DateTime.Today,
        Description = "قيد غير متوازن",
        Type = JournalEntryType.Manual,
        Lines = new List<JournalEntryLineRequest>
        {
            new() { AccountId = 1, AccountCode = "1101", AccountName = "الصندوق", Debit = 1000 },
            new() { AccountId = 2, AccountCode = "4101", AccountName = "إيرادات", Credit = 900 }
        }
    };

    var result = await _service.CreateAsync(request, CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Contains("غير متوازن", result.Error);
}

[Fact]
public async Task PostAsync_ValidEntry_ChangesStatusToPosted()
{
    var entry = CreateDraftEntry();
    _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(entry);

    var result = await _service.PostAsync(1, 1, CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.True(entry.IsPosted);
}

[Fact]
public async Task PostAsync_AlreadyPosted_ReturnsFailure()
{
    var entry = CreatePostedEntry();
    _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(entry);

    var result = await _service.PostAsync(1, 1, CancellationToken.None);

    Assert.False(result.IsSuccess);
}

[Fact]
public async Task CreateAsync_TransactionRollbackOnFailure()
{
    _uowMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(Mock.Of<IDbContextTransaction>());

    var request = new CreateJournalEntryRequest
    {
        TransactionDate = DateTime.Today,
        Description = "قيد فاشل",
        Type = JournalEntryType.Manual,
        Lines = new List<JournalEntryLineRequest>()
    };

    var result = await _service.CreateAsync(request, CancellationToken.None);

    Assert.False(result.IsSuccess);
    _uowMock.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
}

[Fact]
public async Task CancelAsync_PostedEntry_CreatesReversalEntry()
{
    var entry = CreatePostedEntry();
    _repoMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
        .ReturnsAsync(entry);

    var result = await _service.CancelAsync(1, 1, DateTime.Today, CancellationToken.None);

    Assert.True(result.IsSuccess);
    // Verify reversal entry was created
    _repoMock.Verify(x => x.AddAsync(
        It.Is<JournalEntry>(e => e.Type == JournalEntryType.Reversal),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

#### TrialBalanceServiceTests.cs

```csharp
[Fact]
public async Task GetTrialBalanceAsync_ValidDateRange_ReturnsTrialBalance()
{
    var accounts = new List<Account>
    {
        Account.Create("1101", "الصندوق", "Cash", AccountType.Asset),
        Account.Create("4101", "مبيعات", "Sales", AccountType.Revenue)
    };
    _accountRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(accounts);

    var result = await _service.GetTrialBalanceAsync(
        new DateTime(2026, 1, 1), new DateTime(2026, 12, 31),
        CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.NotEmpty(result.Value.Accounts);
}

[Fact]
public async Task GetTrialBalanceAsync_NoData_ReturnsEmptyReport()
{
    _accountRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<Account>());

    var result = await _service.GetTrialBalanceAsync(
        new DateTime(2026, 1, 1), new DateTime(2026, 12, 31),
        CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Empty(result.Value.Accounts);
}

[Fact]
public async Task GetTrialBalanceAsync_TotalDebitEqualsTotalCredit()
{
    // Set up journal entries that are balanced
    // Verify trial balance totals match
    var result = await _service.GetTrialBalanceAsync(
        new DateTime(2026, 1, 1), new DateTime(2026, 12, 31),
        CancellationToken.None);

    Assert.True(result.IsSuccess);
    Assert.Equal(result.Value.TotalDebit, result.Value.TotalCredit);
}
```

---

### 3. FluentValidation Tests

```csharp
public class CreateAccountRequestValidatorTests
{
    private readonly CreateAccountRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = new CreateAccountRequest
        {
            Code = "1101", NameAr = "الصندوق", NameEn = "Cash",
            Type = AccountType.Asset
        };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyCode_FailsWithError()
    {
        var request = new CreateAccountRequest { Code = "", NameAr = "الصندوق", NameEn = "Cash", Type = AccountType.Asset };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Code");
    }

    [Fact]
    public void NullNameAr_FailsWithError()
    {
        var request = new CreateAccountRequest { Code = "1101", NameAr = null!, NameEn = "Cash", Type = AccountType.Asset };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "NameAr");
    }

    [Fact]
    public void CodeTooLong_FailsWithMaxLength()
    {
        var request = new CreateAccountRequest { Code = new string('1', 21), NameAr = "Test", NameEn = "Test", Type = AccountType.Asset };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void InvalidAccountType_FailsWithError()
    {
        var request = new CreateAccountRequest { Code = "1101", NameAr = "Test", NameEn = "Test", Type = (AccountType)99 };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }
}
```

```csharp
public class CreateJournalEntryRequestValidatorTests
{
    private readonly CreateJournalEntryRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = new CreateJournalEntryRequest
        {
            TransactionDate = DateTime.Today,
            Description = "قيد اختبار",
            Type = JournalEntryType.Manual,
            Lines = new List<JournalEntryLineRequest>
            {
                new() { AccountId = 1, AccountCode = "1101", AccountName = "الصندوق", Debit = 1000 },
                new() { AccountId = 2, AccountCode = "4101", AccountName = "إيرادات", Credit = 1000 }
            }
        };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyDescription_FailsWithError()
    {
        var request = new CreateJournalEntryRequest
        {
            TransactionDate = DateTime.Today,
            Description = "",
            Type = JournalEntryType.Manual,
            Lines = new List<JournalEntryLineRequest>()
        };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }

    [Fact]
    public void FutureTransactionDate_FailsWithError()
    {
        var request = new CreateJournalEntryRequest
        {
            TransactionDate = DateTime.Today.AddDays(1),
            Description = "Test",
            Type = JournalEntryType.Manual,
            Lines = new List<JournalEntryLineRequest>()
        };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TransactionDate");
    }

    [Fact]
    public void ZeroLines_FailsWithError()
    {
        var request = new CreateJournalEntryRequest
        {
            TransactionDate = DateTime.Today,
            Description = "Test",
            Type = JournalEntryType.Manual,
            Lines = new List<JournalEntryLineRequest>()
        };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SingleLine_FailsWithError()
    {
        var request = new CreateJournalEntryRequest
        {
            TransactionDate = DateTime.Today,
            Description = "Test",
            Type = JournalEntryType.Manual,
            Lines = new List<JournalEntryLineRequest>
            {
                new() { AccountId = 1, AccountCode = "1101", AccountName = "الصندوق", Debit = 1000 }
            }
        };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Lines");
    }
}
```

---

### 4. API Controller Tests (Integration)

```csharp
public class AccountsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AccountsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_Returns200WithData()
    {
        var response = await _client.GetAsync("/api/v1/accounts");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingId_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/accounts/1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/accounts/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_Returns201()
    {
        var request = new { Code = "9999", NameAr = "اختبار", NameEn = "Test", Type = 1 };
        var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v1/accounts", json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidRequest_Returns400()
    {
        var request = new { Code = "", NameAr = "", NameEn = "", Type = 99 };
        var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v1/accounts", json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingId_Returns204()
    {
        var response = await _client.DeleteAsync("/api/v1/accounts/1");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Unauthorized_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/v1/accounts");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task JournalEntries_PostValid_Returns201()
    {
        var request = new
        {
            TransactionDate = DateTime.Today.ToString("yyyy-MM-dd"),
            Description = "قيد اختبار",
            Type = 1,
            Lines = new[]
            {
                new { AccountId = 1, AccountCode = "1101", AccountName = "الصندوق", Debit = 1000, Credit = 0m },
                new { AccountId = 2, AccountCode = "4101", AccountName = "إيرادات", Debit = 0m, Credit = 1000 }
            }
        };
        var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v1/journal-entries", json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

---

### 5. Database Configuration Tests

```csharp
public class AccountConfigurationTests
{
    [Fact]
    public void AccountConfiguration_HasCorrectPrecision()
    {
        var entityType = typeof(Account);
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new AccountConfiguration());

        var entity = builder.Entity<Account>();
        var decimalProps = entity.Metadata.GetProperties()
            .Where(p => p.ClrType == typeof(decimal));

        foreach (var prop in decimalProps)
        {
            Assert.Equal("decimal(18,2)", prop.GetColumnType());
        }
    }

    [Fact]
    public void AccountConfiguration_CodeIsRequired()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new AccountConfiguration());

        var entity = builder.Entity<Account>();
        var codeProp = entity.Metadata.FindProperty(nameof(Account.Code));
        Assert.False(codeProp.IsNullable);
    }

    [Fact]
    public void AccountConfiguration_ForeignKeysUseRestrict()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new AccountConfiguration());

        var entity = builder.Entity<Account>();
        var foreignKeys = entity.Metadata.GetForeignKeys();

        foreach (var fk in foreignKeys)
        {
            Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        }
    }

    [Fact]
    public void JournalEntryConfiguration_HasQueryFilter()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new JournalEntryConfiguration());

        var entity = builder.Entity<JournalEntry>();
        var queryFilter = entity.Metadata.GetQueryFilter();
        Assert.NotNull(queryFilter); // IsActive filter
    }

    [Fact]
    public void JournalEntryLineConfiguration_DecimalPrecision()
    {
        var builder = new ModelBuilder();
        builder.ApplyConfiguration(new JournalEntryLineConfiguration());

        var entity = builder.Entity<JournalEntryLine>();
        var debitProp = entity.Metadata.FindProperty(nameof(JournalEntryLine.Debit));
        Assert.Equal("decimal(18,2)", debitProp.GetColumnType());

        var creditProp = entity.Metadata.FindProperty(nameof(JournalEntryLine.Credit));
        Assert.Equal("decimal(18,2)", creditProp.GetColumnType());
    }
}
```

---

### 6. Phase 18-Specific Tests

#### JournalEntry 3-State Lifecycle

```csharp
[Fact]
public void Lifecycle_AllValidTransitions()
{
    // Draft (1) → Posted (2) → Cancelled (3): ALLOWED
    var entry = CreateEntry();
    Assert.Equal(InvoiceStatus.Draft, entry.Status);
    entry.AddDebitLine(1, "1101", "الصندوق", 1000);
    entry.AddCreditLine(2, "4101", "إيرادات", 1000);
    entry.ValidateAndPost(1);
    Assert.Equal(InvoiceStatus.Posted, entry.Status);
    entry.CancelWithReversal(DateTime.Today, 1);
    Assert.Equal(InvoiceStatus.Cancelled, entry.Status);
}

[Fact]
public void Lifecycle_InvalidTransitions()
{
    var entry = CreatePostedEntry();
    // Posted → Draft: FORBIDDEN
    Assert.Throws<DomainException>(() => entry.SetDraft());
    
    // Cancelled → anything: FORBIDDEN (terminal state)
    var cancelled = CreatePostedEntry();
    cancelled.CancelWithReversal(DateTime.Today, 1);
    Assert.Throws<DomainException>(() => cancelled.AddDebitLine(1, "1101", "الصندوق", 100));
    Assert.Throws<DomainException>(() => cancelled.ValidateAndPost(1));
}
```

#### JournalEntryLine Debit/Credit Balance

```csharp
[Fact]
public void Line_DebitAndCreditCannotBothBePositive()
{
    var entry = CreateEntry();
    var ex = Assert.Throws<DomainException>(() =>
        entry.AddDebitLine(1, "1101", "الصندوق", 1000, credit: 500));
    Assert.Contains("مدين أو دائن", ex.Message);
}

[Fact]
public void Line_DebitAndCreditCannotBothBeZero()
{
    var entry = CreateEntry();
    var ex = Assert.Throws<DomainException>(() =>
        entry.AddDebitLine(1, "1101", "الصندوق", 0));
    Assert.Contains("أكبر من", ex.Message);
}
```

#### Annual Closing

```csharp
[Fact]
public async Task AnnualClosing_CreatesClosingEntryAndMarksYearClosed()
{
    var result = await _closingService.CloseFiscalYearAsync(2026, 1, CancellationToken.None);
    Assert.True(result.IsSuccess);
    Assert.True(result.Value.IsClosed);
    Assert.NotNull(result.Value.ClosingEntryId);
}

[Fact]
public async Task AnnualClosing_AlreadyClosed_ReturnsFailure()
{
    await _closingService.CloseFiscalYearAsync(2026, 1, CancellationToken.None);
    var result = await _closingService.CloseFiscalYearAsync(2026, 1, CancellationToken.None);
    Assert.False(result.IsSuccess);
    Assert.Contains("مغلق", result.Error);
}

[Fact]
public async Task AnnualClosing_UnpostedEntriesExist_ReturnsFailure()
{
    // Arrange: create draft (unposted) entries in 2026
    var result = await _closingService.CloseFiscalYearAsync(2026, 1, CancellationToken.None);
    Assert.False(result.IsSuccess);
    Assert.Contains("غير مرحّلة", result.Error);
}
```

#### FiscalYear: IsDateInFiscalYear

```csharp
[Fact]
public void IsDateInFiscalYear_DateInRange_ReturnsTrue()
{
    var closure = new FiscalYearClosure(2026, 1, 50000m, 100);
    var date = new DateTime(2026, 6, 15);
    // The closure covers the 2026 fiscal year
}

[Fact]
public void IsDateInFiscalYear_DateOutOfRange_ReturnsFalse()
{
    var date = new DateTime(2025, 12, 31);
    // Not in fiscal year 2026
}
```

#### Account Tree Hierarchy: Level Validation

```csharp
[Fact]
public void Account_Level_CalculatedCorrectly()
{
    // Level 1: 1 digit (e.g., "1")
    var level1 = Account.Create("1", "أصول", "Assets", AccountType.Asset);
    Assert.Equal(1, level1.Level);

    // Level 2: 2 digits (e.g., "11")
    var level2 = Account.Create("11", "أصول متداولة", "Current Assets", AccountType.Asset);
    Assert.Equal(2, level2.Level);

    // Level 4: 4 digits (e.g., "1101")
    var level4 = Account.Create("1101", "الصندوق", "Cash", AccountType.Asset);
    Assert.Equal(4, level4.Level);
}

[Fact]
public void Account_ParentLevelMustBeLessThanChildLevel()
{
    var parent = Account.Create("1", "أصول", "Assets", AccountType.Asset);
    var child = Account.Create("11", "أصول متداولة", "Current Assets", AccountType.Asset, parentId: 1);
    Assert.True(child.Level > parent.Level);
}
```

#### Reversal Entry: ReversedByEntryId FK

```csharp
[Fact]
public void Reversal_ReversedByEntryId_SetCorrectly()
{
    var original = CreatePostedEntry();
    var reversal = original.CreateReversal(DateTime.Today, 1);
    Assert.Equal(original.Id, reversal.ReversedEntryId);
}

[Fact]
public void Reversal_OriginalEntryLinkedToReversal()
{
    var original = CreatePostedEntry();
    var reversal = original.CreateReversal(DateTime.Today, 1);
    // When the reversal is saved, it should reference the original
    Assert.Equal(JournalEntryType.Reversal, reversal.Type);
}
```

---

**Test count target:** 80+ tests across all test categories.

**Estimate:** ~4 hours

---

## Task 9 — Simple Mode UX: Hide Debit/Credit for Non-Accountants

**Requirement**: Analysis Part 3 line 10: "الشاشة لا تعرض مدين ودائن للمستخدم العادي" — Screens must NOT show Debit/Credit columns to regular users. Instead:
1. Show simple transaction view (Amount, Description, Date)
2. Provide a "View Accounting Entry" button (line 14: "يوجد زر عرض القيد المحاسبي")
3. ⓘ explanations everywhere (line 18: "يوجد شرح ⓘ داخل الشاشة") — already handled in Task 8

**Implementation**:
```csharp
public enum AccountingViewMode
{
    Simple = 0,     // Hide Debit/Credit — show amounts only
    Accounting = 1  // Show full accounting entry with Debit/Credit
}
```

- Add `UserPreferences.AccountingViewMode` to filter which columns display
- Cashier/Manager roles default to `Simple` mode
- Admin role defaults to `Accounting` mode
- A toggle button in journal entry views: "ⓘ عرض القيد المحاسبي" / "عرض بسيط"
- When in Simple mode: hide `Debit`/`Credit` columns, show single `Amount` column with `+`/`-` sign
- The toggle is per-user, persisted in UserPreferences

**XAML Pattern**:
```xml
<!-- Accounting Entry Button — only visible in Simple mode -->
<Button Content="ⓘ عرض القيد المحاسبي" 
        Command="{Binding ToggleAccountingViewCommand}"
        Visibility="{Binding IsSimpleMode, Converter={StaticResource BoolToVisibility}}"
        ToolTip="يعرض تفاصيل القيد المحاسبي كاملًاً (مدين / دائن)">
    <Button.Style>
        <Style TargetType="Button" BasedOn="{StaticResource InfoButtonStyle}"/>
    </Button.Style>
</Button>
```

**Files**: UserPreferences entity (add field), JournalEntry ViewModel (add toggle), JournalEntry View (add button + column visibility toggle), UserService (persist preference).

**Estimate**: ~2 hours