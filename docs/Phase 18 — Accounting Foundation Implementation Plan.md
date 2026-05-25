Phase 18 â€” Accounting Foundation Implementation Plan
ًں“‹ Rules for AI Agent
This phase builds the financial backbone. Every other financial feature depends on it. Zero shortcuts. Zero assumptions.

ًں—؛ï¸ڈ What We Are Building
text

â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”گ
â”‚                  ACCOUNTING FOUNDATION                          â”‚
â”‚                                                                 â”‚
â”‚  Chart of Accounts (Accounts)                                   â”‚
â”‚       â”‚                                                         â”‚
â”‚       â–¼                                                         â”‚
â”‚  Journal Entries (JournalEntries)                               â”‚
â”‚       â”‚                                                         â”‚
â”‚       â–¼                                                         â”‚
â”‚  Journal Entry Lines (JournalEntryLines)                        â”‚
â”‚       â”‚                                                         â”‚
â”‚       â–¼                                                         â”‚
â”‚  System Account Mappings (SystemAccountMappings)                â”‚
â”‚  "Which account to hit for each operation"                      â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”ک
ًں—‚ï¸ڈ Task 0 â€” Database Migration
Task 0.1 â€” Create All Tables in Order
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
(1, N'ط£طµظˆظ„',           'Assets'),
(2, N'ط®طµظˆظ…',           'Liabilities'),
(3, N'ط­ظ‚ظˆظ‚ ط§ظ„ظ…ظ„ظƒظٹط©',   'Equity'),
(4, N'ط¥ظٹط±ط§ط¯ط§طھ',        'Revenues'),
(5, N'ظ…طµط±ظˆظپط§طھ',        'Expenses');

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
('1000', N'ط§ظ„ط£طµظˆظ„',                        'Assets',               1, 1),
('1100', N'ط§ظ„ط£طµظˆظ„ ط§ظ„ظ…طھط¯ط§ظˆظ„ط©',               'Current Assets',       1, 1),
('1101', N'ط§ظ„طµظ†ط¯ظˆظ‚',                        'Cash Account',         1, 1),
('1102', N'ط§ظ„ط¨ظ†ظƒ',                          'Bank Account',         1, 1),
('1200', N'ط§ظ„ظ…ط®ط²ظˆظ†',                        'Inventory',            1, 1),
('1201', N'ط£طµظ„ ط§ظ„ظ…ط®ط²ظˆظ†',                    'Inventory Asset',      1, 1),
('1300', N'ط§ظ„ط°ظ…ظ… ط§ظ„ظ…ط¯ظٹظ†ط©',                  'Receivables',          1, 1),
('1301', N'ط°ظ…ظ… ط§ظ„ط¹ظ…ظ„ط§ط،',                    'Accounts Receivable',  1, 1);

-- LIABILITIES (2xxx)
INSERT INTO Accounts (AccountCode, NameAr, NameEn, AccountType, IsSystemAccount) VALUES
('2000', N'ط§ظ„ط®طµظˆظ…',                         'Liabilities',          2, 1),
('2100', N'ط§ظ„ط®طµظˆظ… ط§ظ„ظ…طھط¯ط§ظˆظ„ط©',               'Current Liabilities',  2, 1),
('2101', N'ط°ظ…ظ… ط§ظ„ظ…ظˆط±ط¯ظٹظ†',                   'Accounts Payable',     2, 1),
('2200', N'ط§ظ„ط¶ط±ط§ط¦ط¨ ط§ظ„ظ…ط³طھط­ظ‚ط©',               'Tax Liabilities',      2, 1),
('2201', N'ط¶ط±ظٹط¨ط© ط§ظ„ظ‚ظٹظ…ط© ط§ظ„ظ…ط¶ط§ظپط© - ظ…ط®ط±ط¬ط§طھ', 'VAT Output',           2, 1),
('2202', N'ط¶ط±ظٹط¨ط© ط§ظ„ظ‚ظٹظ…ط© ط§ظ„ظ…ط¶ط§ظپط© - ظ…ط¯ط®ظ„ط§طھ', 'VAT Input',            2, 1);

-- EQUITY (3xxx)
INSERT INTO Accounts (AccountCode, NameAr, NameEn, AccountType, IsSystemAccount) VALUES
('3000', N'ط­ظ‚ظˆظ‚ ط§ظ„ظ…ظ„ظƒظٹط©',                   'Equity',               3, 1),
('3101', N'ط±ط£ط³ ط§ظ„ظ…ط§ظ„',                      'Capital',              3, 1),
('3102', N'ط§ظ„ط£ط±ط¨ط§ط­ ط§ظ„ظ…ط­طھط¬ط²ط©',               'Retained Earnings',    3, 1);

-- REVENUES (4xxx)
INSERT INTO Accounts (AccountCode, NameAr, NameEn, AccountType, IsSystemAccount) VALUES
('4000', N'ط§ظ„ط¥ظٹط±ط§ط¯ط§طھ',                      'Revenues',             4, 1),
('4101', N'ط¥ظٹط±ط§ط¯ط§طھ ط§ظ„ظ…ط¨ظٹط¹ط§طھ',               'Sales Revenue',        4, 1),
('4102', N'ظ…ط±طھط¬ط¹ط§طھ ط§ظ„ظ…ط¨ظٹط¹ط§طھ',               'Sales Returns',        4, 1),
('4201', N'ط¥ظٹط±ط§ط¯ط§طھ ط£ط®ط±ظ‰',                   'Other Revenues',       4, 1);

-- EXPENSES (5xxx)
INSERT INTO Accounts (AccountCode, NameAr, NameEn, AccountType, IsSystemAccount) VALUES
('5000', N'ط§ظ„ظ…طµط±ظˆظپط§طھ',                       'Expenses',             5, 1),
('5101', N'طھظƒظ„ظپط© ط§ظ„ط¨ط¶ط§ط¹ط© ط§ظ„ظ…ط¨ط§ط¹ط©',           'Cost of Goods Sold',   5, 1),
('5201', N'ط§ظ„ظ…طµط±ظˆظپط§طھ ط§ظ„طھط´ط؛ظٹظ„ظٹط©',             'Operating Expenses',   5, 1),
('5202', N'ط§ظ„ط¥ظٹط¬ط§ط±',                         'Rent',                 5, 1),
('5203', N'ط§ظ„ظƒظ‡ط±ط¨ط§ط، ظˆط§ظ„ظ…ظٹط§ظ‡',                'Utilities',            5, 1),
('5204', N'ط±ظˆط§طھط¨ ط§ظ„ظ…ظˆط¸ظپظٹظ†',                  'Salaries',             5, 1),
('5205', N'ظ…طµط±ظˆظپط§طھ ط§ظ„ظ†ظ‚ظ„ ظˆط§ظ„طھظˆطµظٹظ„',          'Delivery Expenses',    5, 1),
('5301', N'ط®ط³ط§ط¦ط± ط§ظ„ط¨ط¶ط§ط¹ط© ط§ظ„طھط§ظ„ظپط©',           'Spoilage Loss',        5, 1),
('5302', N'ط®ط³ط§ط¦ط± ط§ظ„ظ…ط®ط²ظˆظ†',                   'Inventory Loss',       5, 1);

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
âœ… Task 0 Checklist
 All 6 tables created without errors
 All foreign keys valid
 Default accounts seeded (20+ accounts)
 SystemAccountMappings has one global row
 CHK_DebitOrCredit constraint applied
 CHK_NoNegativeValues constraint applied
ًںڈ—ï¸ڈ Task 1 â€” Domain Layer
Task 1.1 â€” Enums
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
Task 1.2 â€” Account Entity
csharp

// File: Domain/Accounting/Entities/Account.cs
namespace Domain.Accounting.Entities;

public class Account : BaseEntity
{
    // â”€â”€â”€ Properties â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public string AccountCode { get; private set; }
    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public AccountType AccountType { get; private set; }
    public int? ParentAccountId { get; private set; }
    public bool IsSystemAccount { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    // â”€â”€â”€ Navigation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public Account? ParentAccount { get; private set; }
    public IReadOnlyCollection<Account> SubAccounts => _subAccounts.AsReadOnly();
    private readonly List<Account> _subAccounts = new();

    public IReadOnlyCollection<JournalEntryLine> JournalLines => _journalLines.AsReadOnly();
    private readonly List<JournalEntryLine> _journalLines = new();

    private Account() { } // EF Core

    // â”€â”€â”€ Factory â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
            throw new DomainException("ظƒظˆط¯ ط§ظ„ط­ط³ط§ط¨ ظ…ط·ظ„ظˆط¨");

        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("ط§ط³ظ… ط§ظ„ط­ط³ط§ط¨ ط¨ط§ظ„ط¹ط±ط¨ظٹ ظ…ط·ظ„ظˆط¨");

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

    // â”€â”€â”€ Domain Methods â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void Update(string nameAr, string nameEn, string? notes)
    {
        if (IsSystemAccount)
            throw new DomainException(
                $"ط§ظ„ط­ط³ط§ط¨ '{NameAr}' ظ‡ظˆ ط­ط³ط§ط¨ ظ†ط¸ط§ظ… ظˆظ„ط§ ظٹظ…ظƒظ† طھط¹ط¯ظٹظ„ظ‡. " +
                $"طھظˆط§طµظ„ ظ…ط¹ ط§ظ„ظ…ط³ط¤ظˆظ„ ظ„ط¥ط¬ط±ط§ط، ط£ظٹ طھط؛ظٹظٹط±ط§طھ.");

        NameAr = nameAr.Trim();
        NameEn = nameEn.Trim();
        Notes  = notes;
    }

    public void Deactivate()
    {
        if (IsSystemAccount)
            throw new DomainException(
                $"ظ„ط§ ظٹظ…ظƒظ† طھط¹ط·ظٹظ„ ط§ظ„ط­ط³ط§ط¨ '{NameAr}' ظ„ط£ظ†ظ‡ ط­ط³ط§ط¨ ظ†ط¸ط§ظ… ط£ط³ط§ط³ظٹ.");

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
Task 1.3 â€” JournalEntry Entity
csharp

// File: Domain/Accounting/Entities/JournalEntry.cs
namespace Domain.Accounting.Entities;

public class JournalEntry : BaseEntity
{
    // â”€â”€â”€ Properties â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

    // â”€â”€â”€ Lines â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private readonly List<JournalEntryLine> _lines = new();
    public IReadOnlyCollection<JournalEntryLine> Lines => _lines.AsReadOnly();

    private JournalEntry() { } // EF Core

    // â”€â”€â”€ Factory â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
            throw new DomainException("ط±ظ‚ظ… ط§ظ„ظ‚ظٹط¯ ظ…ط·ظ„ظˆط¨");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("ط¨ظٹط§ظ† ط§ظ„ظ‚ظٹط¯ ظ…ط·ظ„ظˆط¨");

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

    // â”€â”€â”€ Domain Methods â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
        ValidateAmount(amount, "ط§ظ„ظ…ط¨ظ„ط؛ ط§ظ„ظ…ط¯ظٹظ†");

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
        ValidateAmount(amount, "ط§ظ„ظ…ط¨ظ„ط؛ ط§ظ„ط¯ط§ط¦ظ†");

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
                "ظ„ط§ ظٹظ…ظƒظ† طھط±ط­ظٹظ„ ظ‚ظٹط¯ ط¨ط¯ظˆظ† ط£ط³ط·ط±. ط£ط¶ظپ ط³ط·ط± ظ…ط¯ظٹظ† ظˆط¯ط§ط¦ظ† ط¹ظ„ظ‰ ط§ظ„ط£ظ‚ظ„.");

        if (!IsBalanced())
        {
            var totalDebit  = _lines.Sum(l => l.Debit);
            var totalCredit = _lines.Sum(l => l.Credit);
            throw new DomainException(
                $"ط§ظ„ظ‚ظٹط¯ ط؛ظٹط± ظ…طھظˆط§ط²ظ† ظˆظ„ط§ ظٹظ…ظƒظ† طھط±ط­ظٹظ„ظ‡.\n" +
                $"ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظ…ط¯ظٹظ†:  {totalDebit:N2}\n" +
                $"ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ط¯ط§ط¦ظ†: {totalCredit:N2}\n" +
                $"ط§ظ„ظپط±ظ‚: {Math.Abs(totalDebit - totalCredit):N2}\n" +
                $"ظٹط¬ط¨ ط£ظ† ظٹطھط³ط§ظˆظ‰ ط§ظ„ظ…ط¯ظٹظ† ظˆط§ظ„ط¯ط§ط¦ظ†.");
        }

        IsPosted  = true;
        PostedBy  = postedBy;
        PostedAt  = DateTime.UtcNow;
    }

    public decimal TotalDebit  => _lines.Sum(l => l.Debit);
    public decimal TotalCredit => _lines.Sum(l => l.Credit);

    // â”€â”€â”€ Private Guards â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void ValidateCanModify()
    {
        if (IsPosted)
            throw new DomainException(
                $"ط§ظ„ظ‚ظٹط¯ ط±ظ‚ظ… {EntryNumber} ظ…ط±ط­ظ‘ظ„ ظˆظ„ط§ ظٹظ…ظƒظ† طھط¹ط¯ظٹظ„ظ‡.\n" +
                $"ظ„ط¥طµظ„ط§ط­ ط£ظٹ ط®ط·ط£طŒ ط§ط³طھط®ط¯ظ… 'ظ‚ظٹط¯ ط¹ظƒط³ظٹ' ط£ظˆ طھظˆط§طµظ„ ظ…ط¹ ط§ظ„ظ…ط­ط§ط³ط¨.");

        if (IsReversed)
            throw new DomainException(
                $"ط§ظ„ظ‚ظٹط¯ ط±ظ‚ظ… {EntryNumber} طھظ… ط¹ظƒط³ظ‡ ظˆظ„ط§ ظٹظ…ظƒظ† طھط¹ط¯ظٹظ„ظ‡.");
    }

    private static void ValidateAmount(decimal amount, string fieldName)
    {
        if (amount < 0)
            throw new DomainException($"{fieldName} ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");

        if (amount == 0)
            throw new DomainException($"{fieldName} ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† طµظپط±ط§ظ‹");
    }
}
Task 1.4 â€” JournalEntryLine Entity
csharp

// File: Domain/Accounting/Entities/JournalEntryLine.cs
namespace Domain.Accounting.Entities;

public class JournalEntryLine : BaseEntity
{
    public int JournalEntryId { get; private set; }
    public int AccountId { get; private set; }

    // Snapshots â€” account data at time of entry
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

    // â”€â”€â”€ Factories â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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
Task 1.5 â€” SystemAccountMappings Entity
csharp

// File: Domain/Accounting/Entities/SystemAccountMappings.cs
namespace Domain.Accounting.Entities;

public class SystemAccountMappings : BaseEntity
{
    public int? BranchId { get; private set; }

    // â”€â”€â”€ Asset Accounts â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public int DefaultCashAccountId        { get; private set; }
    public int DefaultBankAccountId        { get; private set; }
    public int InventoryAssetAccountId     { get; private set; }
    public int AccountsReceivableAccountId { get; private set; }

    // â”€â”€â”€ Liability Accounts â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public int AccountsPayableAccountId { get; private set; }
    public int VatOutputAccountId       { get; private set; }
    public int VatInputAccountId        { get; private set; }

    // â”€â”€â”€ Equity Accounts â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public int CapitalAccountId { get; private set; }

    // â”€â”€â”€ Revenue Accounts â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public int SalesRevenueAccountId { get; private set; }
    public int SalesReturnAccountId  { get; private set; }

    // â”€â”€â”€ Expense Accounts â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public int CogsAccountId            { get; private set; }
    public int GeneralExpenseAccountId  { get; private set; }
    public int SpoilageLossAccountId    { get; private set; }

    // â”€â”€â”€ Navigation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
            "bank" or "ط´ط¨ظƒط©" or "ط¨ط·ط§ظ‚ط©" => DefaultBankAccountId,
            _ => DefaultCashAccountId
        };
    }
}
âœ… Task 1 Checklist
 All 4 entities created in Domain/Accounting/Entities/
 JournalEntry.IsBalanced() implemented
 JournalEntry.ValidateAndPost() throws if unbalanced
 Arabic error messages in all DomainExceptions
 IsSystemAccount prevents deletion/modification
 CreateDebit and CreateCredit are internal (only JournalEntry can create lines)
âڑ™ï¸ڈ Task 2 â€” Infrastructure (EF Core Configuration)
Task 2.1 â€” Account Configuration
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
Task 2.2 â€” JournalEntry Configuration
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
Task 2.3 â€” JournalEntryLine Configuration
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
âœ… Task 2 Checklist
 All 3 configurations registered in AppDbContext.OnModelCreating()
 Decimal precision is (18,2) for all financial columns
 Unique indexes on EntryNumber and AccountCode
 Cascade delete on JournalEntryLines when entry deleted
 Restrict delete on Account (cannot delete account that has transactions)
âڑ™ï¸ڈ Task 3 â€” Application Layer (Services)
Task 3.1 â€” EntryNumber Generator
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
Task 3.2 â€” System Account Mappings Service
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
                "ظ„ظ… ظٹطھظ… ط¥ط¹ط¯ط§ط¯ ط±ط¨ط· ط§ظ„ط­ط³ط§ط¨ط§طھ ط§ظ„ط§ظپطھط±ط§ط¶ظٹط©. " +
                "ظٹط±ط¬ظ‰ ط§ظ„ط°ظ‡ط§ط¨ ط¥ظ„ظ‰ ط§ظ„ط¥ط¹ط¯ط§ط¯ط§طھ â†گ ط¥ط¹ط¯ط§ط¯ط§طھ ط§ظ„ظ…ط­ط§ط³ط¨ط© ظˆطھط­ط¯ظٹط¯ ط§ظ„ط­ط³ط§ط¨ط§طھ ط§ظ„ط§ظپطھط±ط§ط¶ظٹط©.");

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
                $"ط§ظ„ط­ط³ط§ط¨ ط¨ط§ظ„ظƒظˆط¯ '{accountCode}' ط؛ظٹط± ظ…ظˆط¬ظˆط¯ ط£ظˆ ط؛ظٹط± ظ†ط´ط·.");
    }
}
Task 3.3 â€” Create Manual Journal Entry Command
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
            .WithMessage("ط¨ظٹط§ظ† ط§ظ„ظ‚ظٹط¯ ظ…ط·ظ„ظˆط¨")
            .MaximumLength(500);

        RuleFor(x => x.TransactionDate)
            .NotEmpty()
            .WithMessage("طھط§ط±ظٹط® ط§ظ„ظ‚ظٹط¯ ظ…ط·ظ„ظˆط¨")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1))
            .WithMessage("طھط§ط±ظٹط® ط§ظ„ظ‚ظٹط¯ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ظپظٹ ط§ظ„ظ…ط³طھظ‚ط¨ظ„");

        RuleFor(x => x.Lines)
            .NotEmpty()
            .WithMessage("ظٹط¬ط¨ ط¥ط¶ط§ظپط© ط£ط³ط·ط± ظ„ظ„ظ‚ظٹط¯")
            .Must(lines => lines.Count >= 2)
            .WithMessage("ط§ظ„ظ‚ظٹط¯ ظٹط¬ط¨ ط£ظ† ظٹط­طھظˆظٹ ط¹ظ„ظ‰ ط³ط·ط±ظٹظ† ط¹ظ„ظ‰ ط§ظ„ط£ظ‚ظ„");

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
                return $"ط§ظ„ظ‚ظٹط¯ ط؛ظٹط± ظ…طھظˆط§ط²ظ†. ط§ظ„ظ…ط¯ظٹظ†: {d:N2} â€” ط§ظ„ط¯ط§ط¦ظ†: {c:N2}";
            });

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId)
                .GreaterThan(0)
                .WithMessage("ط­ط³ط§ط¨ ط§ظ„ظ‚ظٹط¯ ط؛ظٹط± طµط§ظ„ط­");

            line.RuleFor(l => l)
                .Must(l =>
                    (l.Debit > 0 && l.Credit == 0) ||
                    (l.Credit > 0 && l.Debit == 0))
                .WithMessage("ظƒظ„ ط³ط·ط± ظٹط¬ط¨ ط£ظ† ظٹظƒظˆظ† ط¥ظ…ط§ ظ…ط¯ظٹظ†ط§ظ‹ ط£ظˆ ط¯ط§ط¦ظ†ط§ظ‹ ظˆظ„ظٹط³ ط§ظ„ط§ط«ظ†ظٹظ† ظ…ط¹ط§ظ‹");
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
        // â”€â”€â”€ 1. Generate entry number â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var entryNumber = await _numberGenerator.GenerateAsync(cancellationToken);

        // â”€â”€â”€ 2. Load accounts for all lines â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                $"ط§ظ„ط­ط³ط§ط¨ط§طھ ط§ظ„طھط§ظ„ظٹط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©: {string.Join(", ", missingIds)}");

        // â”€â”€â”€ 3. Create journal entry â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€ 4. Add lines â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€ 5. Validate and post â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        entry.ValidateAndPost(command.CreatedBy);

        // â”€â”€â”€ 6. Save â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        _context.JournalEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Journal entry {Number} created. Type: {Type}. Total: {Total:N2}",
            entryNumber, command.EntryType, entry.TotalDebit);

        return entry.Id;
    }
}
âœ… Task 3 Checklist
 JournalEntryNumberGenerator creates sequential daily numbers
 Validator rejects unbalanced entries BEFORE handler runs
 Handler validates all accounts exist before creating entry
 Entry is posted immediately upon creation (no draft journals)
 All services registered in DI container
ًں“ٹ Task 4 â€” Basic Financial Queries
Task 4.1 â€” Get Account Balance Query
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
Task 4.2 â€” Get Account Statement Query
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
âœ… Task 4 Checklist
 All queries use AsNoTracking()
 Opening balance calculated correctly (before period)
 Running balance direction based on IsDebitNormal()
 Only IsPosted = true entries included in statements
 Results ordered by date then entry number
ًں§ھ Task 5 â€” Unit Tests
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
        entry.AddDebitLine(CashId,  "1101", "ط§ظ„طµظ†ط¯ظˆظ‚",           1000);
        entry.AddCreditLine(SalesId,"4101", "ط¥ظٹط±ط§ط¯ط§طھ ط§ظ„ظ…ط¨ظٹط¹ط§طھ",  1000);

        Assert.True(entry.IsBalanced());
    }

    [Fact]
    public void IsBalanced_UnequalAmounts_ReturnsFalse()
    {
        var entry = CreateEntry();
        entry.AddDebitLine(CashId,  "1101", "ط§ظ„طµظ†ط¯ظˆظ‚",          1000);
        entry.AddCreditLine(SalesId,"4101", "ط¥ظٹط±ط§ط¯ط§طھ ط§ظ„ظ…ط¨ظٹط¹ط§طھ",  900);

        Assert.False(entry.IsBalanced());
    }

    [Fact]
    public void ValidateAndPost_UnbalancedEntry_ThrowsDomainException()
    {
        var entry = CreateEntry();
        entry.AddDebitLine(CashId,  "1101", "ط§ظ„طµظ†ط¯ظˆظ‚",          500);
        entry.AddCreditLine(SalesId,"4101", "ط¥ظٹط±ط§ط¯ط§طھ ط§ظ„ظ…ط¨ظٹط¹ط§طھ",  300);

        var ex = Assert.Throws<DomainException>(
            () => entry.ValidateAndPost(postedBy: 1));

        Assert.Contains("ط؛ظٹط± ظ…طھظˆط§ط²ظ†", ex.Message);
    }

    [Fact]
    public void ValidateAndPost_EmptyLines_ThrowsDomainException()
    {
        var entry = CreateEntry();

        var ex = Assert.Throws<DomainException>(
            () => entry.ValidateAndPost(postedBy: 1));

        Assert.Contains("ط¨ط¯ظˆظ† ط£ط³ط·ط±", ex.Message);
    }

    [Fact]
    public void ValidateAndPost_BalancedEntry_SetsIsPostedTrue()
    {
        var entry = CreateEntry();
        entry.AddDebitLine(CashId,  "1101", "ط§ظ„طµظ†ط¯ظˆظ‚",          1000);
        entry.AddCreditLine(SalesId,"4101", "ط¥ظٹط±ط§ط¯ط§طھ ط§ظ„ظ…ط¨ظٹط¹ط§طھ", 1000);

        entry.ValidateAndPost(postedBy: 1);

        Assert.True(entry.IsPosted);
        Assert.Equal(1, entry.PostedBy);
        Assert.NotNull(entry.PostedAt);
    }

    [Fact]
    public void AddDebitLine_AfterPosted_ThrowsDomainException()
    {
        var entry = CreateEntry();
        entry.AddDebitLine(CashId,  "1101", "ط§ظ„طµظ†ط¯ظˆظ‚",          1000);
        entry.AddCreditLine(SalesId,"4101", "ط¥ظٹط±ط§ط¯ط§طھ ط§ظ„ظ…ط¨ظٹط¹ط§طھ", 1000);
        entry.ValidateAndPost(postedBy: 1);

        var ex = Assert.Throws<DomainException>(
            () => entry.AddDebitLine(CashId, "1101", "ط§ظ„طµظ†ط¯ظˆظ‚", 500));

        Assert.Contains("ظ…ط±ط­ظ‘ظ„", ex.Message);
    }

    [Fact]
    public void MultiLineEntry_ComplexSalesJournal_IsBalanced()
    {
        // Sale: 1000 + VAT 150 = 1150 total, Cost = 700
        var entry = CreateEntry();

        // Cash in
        entry.AddDebitLine(1,  "1101", "ط§ظ„طµظ†ط¯ظˆظ‚",                1150);

        // Sales revenue + VAT
        entry.AddCreditLine(2, "4101", "ط¥ظٹط±ط§ط¯ط§طھ ط§ظ„ظ…ط¨ظٹط¹ط§طھ",       1000);
        entry.AddCreditLine(3, "2201", "ط¶ط±ظٹط¨ط© ط§ظ„ظ‚ظٹظ…ط© ط§ظ„ظ…ط¶ط§ظپط©",    150);

        // COGS
        entry.AddDebitLine(4,  "5101", "طھظƒظ„ظپط© ط§ظ„ط¨ط¶ط§ط¹ط© ط§ظ„ظ…ط¨ط§ط¹ط©",   700);
        entry.AddCreditLine(5, "1201", "ط£طµظ„ ط§ظ„ظ…ط®ط²ظˆظ†",             700);

        Assert.True(entry.IsBalanced());
        Assert.Equal(1850, entry.TotalDebit);
        Assert.Equal(1850, entry.TotalCredit);
    }

    [Fact]
    public void Account_IsDebitNormal_AssetReturnsTrue()
    {
        var account = Account.Create("1101", "ط§ظ„طµظ†ط¯ظˆظ‚", "Cash", AccountType.Asset);
        Assert.True(account.IsDebitNormal());
    }

    [Fact]
    public void Account_IsDebitNormal_RevenueReturnsFalse()
    {
        var account = Account.Create("4101", "ظ…ط¨ظٹط¹ط§طھ", "Sales", AccountType.Revenue);
        Assert.False(account.IsDebitNormal());
    }

    [Fact]
    public void Account_UpdateSystemAccount_ThrowsDomainException()
    {
        var account = Account.Create(
            "1101", "ط§ظ„طµظ†ط¯ظˆظ‚", "Cash",
            AccountType.Asset,
            isSystemAccount: true);

        var ex = Assert.Throws<DomainException>(
            () => account.Update("ط§ط³ظ… ط¬ط¯ظٹط¯", "New Name", null));

        Assert.Contains("ط­ط³ط§ط¨ ظ†ط¸ط§ظ…", ex.Message);
    }

    private static JournalEntry CreateEntry() =>
        JournalEntry.Create(
            "JE-20260520-0001",
            DateTime.Today,
            "ظ‚ظٹط¯ ط§ط®طھط¨ط§ط±",
            JournalEntryType.Manual,
            createdBy: 1);
}
âœ… Task 5 Checklist
 9 unit tests all passing
 Multi-line complex entry test confirms 1850 = 1850
 IsDebitNormal() tested for Asset (true) and Revenue (false)
 System account modification throws correct Arabic message
ًں“¦ Final Summary for Phase 17
text

â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”گ
â”‚              PHASE 17 â€” ACCOUNTING FOUNDATION                      â”‚
â”‚              Implementation Order                                  â”‚
â”œâ”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ Task â”‚ Deliverable                                 â”‚ Must Pass    â”‚
â”œâ”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  0   â”‚ SQL: 6 tables + seed data                   â”‚ Migration OK â”‚
â”‚  1   â”‚ Domain: Account, JournalEntry, Lines,       â”‚ No DB refs   â”‚
â”‚      â”‚         SystemAccountMappings               â”‚ in Domain    â”‚
â”‚  2   â”‚ EF Core: 3 configurations                   â”‚ Precision    â”‚
â”‚      â”‚                                             â”‚ (18,2)       â”‚
â”‚  3   â”‚ Application: Generator, Service,            â”‚ Validator    â”‚
â”‚      â”‚              Command + Validator + Handler  â”‚ runs first   â”‚
â”‚  4   â”‚ Queries: Balance + Statement                â”‚ AsNoTracking â”‚
â”‚  5   â”‚ Tests: 9 unit tests                         â”‚ All green    â”‚
â””â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”ک

RULES â€” ZERO TOLERANCE:
â”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پ
âœ… JournalEntry.ValidateAndPost() called BEFORE SaveChanges
âœ… IsBalanced() must return true or SaveChanges never called
âœ… Decimal precision is (18,2)
âœ… IsSystemAccount = true accounts cannot be edited or deleted
âœ… JournalEntryLine.CreateDebit/Credit are internal â€” only JournalEntry creates lines
âœ… All financial queries use AsNoTracking
âœ… Only IsPosted = true entries appear in financial reports
âœ… Arabic error messages in ALL DomainExceptions
âœ… Account snapshots (Code + Name) stored in JournalEntryLine
