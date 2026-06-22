# Phase 18 — Accounting Foundation Implementation Plan
📋 Rules for AI Agent
This phase builds the financial backbone. Every other financial feature depends on it. Zero shortcuts. Zero assumptions.

---

## 🗺️ What We Are Building

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│                      ACCOUNTING FOUNDATION                                  │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │                Chart of Accounts (Accounts)                          │  │
│  │  ┌─────────────┐  ┌──────────────────┐  ┌────────────────────────┐  │  │
│  │  │ Level 1 (1) │  │ Level 2 (11,12…) │  │ Level 3 (1101,1102…)  │  │  │
│  │  │  1 digit    │──│  2 digits        │──│  4 digits (parent+2)   │  │  │
│  │  └─────────────┘  └──────────────────┘  └────────────────────────┘  │  │
│  │                                               │                     │  │
│  │                                               ▼                     │  │
│  │                                        ┌────────────────────────┐  │  │
│  │                                        │ Level 4 (11010001…)    │  │  │
│  │                                        │ 8 digits (parent+4)    │  │  │
│  │                                        └────────────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│       │                                                                     │
│       ▼                                                                     │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  Auto-Generation Services                                            │  │
│  │  ┌──────────────────────┐  ┌──────────────────┐  ┌────────────────┐  │  │
│  │  │ AccountCodeGenerator │  │ ColorCode Helper  │  │ OpeningBalance │  │  │
│  │  │ (SemaphoreSlim)      │  │ (Nature→Hex map)  │  │ Journal Entry  │  │  │
│  │  └──────────────────────┘  └──────────────────┘  └────────────────┘  │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│       │                                                                     │
│       ▼                                                                     │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  Journal Entries (JournalEntries)  ← 3-state: Draft→Posted→Cancelled │  │
│  │       │                                                               │  │
│  │       ▼                                                               │  │
│  │  Journal Entry Lines (JournalEntryLines)  ← Debit/Credit pairs       │  │
│  │       │                                                               │  │
│  │       ▼                                                               │  │
│  │  System Account Mappings (SystemAccountMappings)                      │  │
│  │  "Which account to hit for each operation"                            │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│       │                                                                     │
│       ▼                                                                     │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │  74 Seeded Accounts (5+8+24+37)                                      │  │
│  │  Level 1: 5 groups (1=Assets … 5=Expenses)                          │  │
│  │  Level 2: 8 main categories (11=CurrentAssets … 52=OperatingExp)    │  │
│  │  Level 3: 24 sub-categories (1101=Cash … 5203=Utilities)            │  │
│  │  Level 4: 37 detail leaf accounts (11010001=Cash on Hand …)         │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🗂️ Task 0 — Database Migration

### Task 0.1 — Create All Tables in Order

> See `docs/database-schema.md` Module [4.2] for the canonical AccountTypes table definition.

> See `docs/database-schema.md` for seed data patterns. The `AccountingSeeder` in `Infrastructure/Data/Seeders/` contains canonical seed logic.

> See `docs/database-schema.md` for seed data patterns. The `AccountsSeeder` in `Infrastructure/Data/Seeders/` contains canonical seed logic.

**Database Changes vs Previous Plan:**
- Remove `OpeningBalance` column from Accounts table
- Keep `IsLeaf` (bit) stored column; `AllowTransactions` is computed (`=> IsLeaf`)
- Add filtered unique index: `CREATE UNIQUE INDEX IX_Accounts_AccountCode ON Accounts(AccountCode) WHERE IsActive = 1`
- AccountCode length validation: Level 1 = 1 char, Level 2 = 2 chars, Level 3 = 4 chars, Level 4 = 8 chars

✅ **Task 0 Checklist**
- [ ] All 6 tables created without errors
- [ ] OpeningBalance column REMOVED from Accounts table
- [ ] Filtered unique index IX_Accounts_AccountCode added (WHERE IsActive = 1)
- [ ] All foreign keys valid
- [ ] 74 default accounts seeded (5 groups + 8 main + 24 sub + 37 detail)
- [ ] SystemAccountMappings has one global row (20+ mappings with updated account code references)
- [ ] CHK_DebitOrCredit constraint applied on JournalEntryLines
- [ ] CHK_NoNegativeValues constraint applied on JournalEntryLines

---

## 🏗️ Task 1 — Domain Layer

> See `docs/AGENTS.md` Section 3 for all enum values (AccountType, JournalEntryType, etc.).

### Task 1.1 — Account Code Strategy (NEW: Hierarchical Expanding Numbering)

Account codes follow a level-based hierarchical scheme:

| Level | Name | Digits | Format | Examples |
|-------|------|--------|--------|----------|
| 1 | Group | 1 | Single digit | `1` (Assets), `2` (Liabilities), `3` (Equity), `4` (Revenue), `5` (Expenses) |
| 2 | Main | 2 | Parent digit + 1 | `11` (Current Assets), `12` (Fixed Assets), `21` (Current Liabilities) |
| 3 | Sub | 4 | Parent code + 2-digit seq | `1101` (Cash), `1102` (Bank), `1103` (Accounts Receivable) |
| 4 | Detail | 8 | Parent code + 4-digit seq | `11010001` (Cash on Hand), `11010002` (Petty Cash) |

This eliminates the bottleneck of max 9 detail accounts per sub-account (old scheme). Now supports up to 9,999 detail accounts per sub.

**Code Generation Rules:**
- Level 1: Auto-numbered by checking `MAX(AccountCode)` across all Level 1 accounts, adding 1
- Level 2+: Get parent's `AccountCode`, query max child code with `LIKE parentCode + '%'`, increment numeric suffix
- Thread safety via `SemaphoreSlim` in `AccountCodeGeneratorService`

### Task 1.2 — Account Entity (Updated)

> See `docs/database-schema.md` Module 4.2 for the Account table definition and `docs/CONSTITUTION.md`/`AGENTS.md` for entity patterns (private set, Guard Clauses, domain methods).

**Updated `Account.Create()` signature:**

```csharp
public static Account Create(
    string accountCode,       // System-generated via AccountCodeGeneratorService
    string nameAr,            // Required
    string? nameEn = null,
    byte nature = 1,          // 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense
    bool isLeaf = true,       // false for parent accounts
    int? parentId = null,
    bool isSystem = false,
    short? categoryId = null,
    byte level = 1,           // 1, 2, 3, or 4
    string? description = null,
    string? colorCode = null,  // Auto-set by service from Nature; NOT user-supplied
    string? notes = null,
    int? createdByUserId = null)
```

**Key changes from previous implementation:**
- `openingBalance` param REMOVED from `Account.Create()` — opening balance is now a Journal Entry created by the service layer
- `ColorCode` is system-generated from `Nature` (not user-supplied)
- `Name` is culture-aware: checks `Thread.CurrentThread.CurrentUICulture` for display
- `IsLeaf` (bit) is the stored column; `AllowTransactions` is a computed property (`=> IsLeaf`)
- No `Explanation` field — `Description` covers that need

> See `docs/database-schema.md` Module 5.1 for the JournalEntry table definition and `docs/CONSTITUTION.md`/`AGENTS.md` for entity patterns (private set, Guard Clauses, domain methods).

### Task 1.4 — JournalEntryLine Entity

> See `docs/database-schema.md` Module 5.2 for the JournalEntryLine table definition and `docs/AGENTS.md`/`CONSTITUTION.md` for entity patterns.

### Task 1.5 — SystemAccountMappings Entity

> See `docs/database-schema.md` Module 6 for the SystemAccountMappings table definition and `docs/AGENTS.md`/`CONSTITUTION.md` for entity patterns.

✅ **Task 1 Checklist**
- [ ] All 4 entities created in Domain/Accounting/Entities/
- [ ] Account.Create() has updated signature (OpeningBalance removed)
- [ ] JournalEntry.IsBalanced() implemented
- [ ] JournalEntry.ValidateAndPost() throws if unbalanced
- [ ] Arabic error messages in all DomainExceptions
- [ ] IsSystemAccount prevents deletion/modification
- [ ] CreateDebit and CreateCredit are internal (only JournalEntry can create lines)
- [ ] Account.Create() param `openingBalance` removed (now handled at service layer via Journal Entry)
- [ ] ColorCode is NOT in the entity constructor (system-set by service)

---

## ⚙️ Task 2 — Infrastructure (EF Core Configuration)

### Task 2.1 — Account Configuration (Updated)

> See `docs/AGENTS.md` §2.16 (EF Core Conventions) and `docs/database-schema.md` Module 4.2 for the canonical AccountConfiguration pattern (DeleteBehavior.Restrict, HasPrecision(18,2), HasMaxLength, unique indexes).

**Config changes vs previous:**
- **REMOVED**: `.Property(a => a.OpeningBalance).HasPrecision(18, 2).HasDefaultValue(0m);`
- **KEPT**: All other config as-is
- **ADDED**: Filtered unique index `.HasFilter("[IsActive] = 1")` on AccountCode
- `IsLeaf` mapped as stored column; `AllowTransactions` is computed (`HasComputedColumnSql` is NOT needed — it's a C# computed property only)

### Task 2.2 — JournalEntry Configuration

> See `docs/database-schema.md` Module 5.1 for the JournalEntry table definition and `docs/AGENTS.md` §2.16 for EF Core conventions.

### Task 2.3 — JournalEntryLine Configuration

> See `docs/database-schema.md` Module 5.2 and `docs/AGENTS.md` §2.16 for JournalEntryLine configuration (decimal(18,2), Restrict delete, performance indexes).

✅ **Task 2 Checklist**
- [ ] All 3 configurations registered in AppDbContext.OnModelCreating()
- [ ] Decimal precision is (18,2) for all financial columns
- [ ] Unique indexes on EntryNumber and AccountCode (filtered: WHERE IsActive = 1)
- [ ] OpeningBalance config REMOVED from AccountConfiguration
- [ ] Restrict delete on ALL foreign keys (JournalEntry→Lines, Account→Account)
- [ ] Domain-level composition: JournalEntry owns Lines but deletion is prevented if posted
- [ ] Reversal via ReversedByEntryId FK (Restrict) preserves audit trail

---

## ⚙️ Task 3 — Application Layer (Services)

### Task 3.1 — AccountCodeGeneratorService (NEW)

A thread-safe service that generates hierarchical account codes:

```csharp
public class AccountCodeGeneratorService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<string> GenerateCodeAsync(int level, int? parentId, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            // Level 1: No parent → find max existing L1 code + 1 (digit)
            if (level == 1 && !parentId.HasValue)
            {
                var maxCode = await _repo.GetMaxLevel1CodeAsync(ct);
                return maxCode == null ? "1" : (int.Parse(maxCode) + 1).ToString();
            }

            // Level 2+: Get parent's AccountCode, query max child code
            var parent = await _repo.GetByIdAsync(parentId!.Value, ct);
            var parentCode = parent.AccountCode;
            var maxChildCode = await _repo.GetMaxChildCodeAsync(parentCode, ct);

            // Level 2 → parent code + 1 digit (e.g., parent "1" → "11", "12", "13")
            // Level 3 → parent code + 2 digits (e.g., parent "11" → "1101", "1102")
            // Level 4 → parent code + 4 digits (e.g., parent "1101" → "11010001")
            int digitCount = level switch { 2 => 1, 3 => 2, 4 => 4, _ => 2 };
            var newSuffix = maxChildCode == null
                ? 1
                : int.Parse(maxChildCode.Substring(parentCode.Length)) + 1;
            return parentCode + newSuffix.ToString().PadLeft(digitCount, '0');
        }
        finally { _semaphore.Release(); }
    }
}
```

### Task 3.2 — ColorCode Auto-Generation (NEW)

Static helper mapping Account Nature → hex color:

```csharp
public static class AccountColorHelper
{
    public static string GetColorCodeForNature(byte nature) => nature switch
    {
        1 => "#2B579A",  // Asset → Blue
        2 => "#D32F2F",  // Liability → Red
        3 => "#6A1B9A",  // Equity → Purple
        4 => "#2E7D32",  // Revenue → Green
        5 => "#795548",  // Expense → Brown
        _ => "#757575"   // Default → Gray
    };
}
```

ColorCode is NEVER user-supplied. It's auto-assigned at creation and never updated.

### Task 3.3 — AccountService.CreateAsync() — NEW FLOW

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Updated flow:**

```csharp
public async Task<Result<AccountDto>> CreateAsync(CreateAccountRequest request, int userId, CancellationToken ct)
{
    // 1. Validate parent exists and level constraint
    if (request.ParentId.HasValue)
    {
        var parent = await _uow.Accounts.GetByIdAsync(request.ParentId.Value, ct);
        if (parent == null)
            return Result<AccountDto>.Failure("الحساب الأب غير موجود", ErrorCodes.NotFound);
        if (request.Level <= parent.Level)
            return Result<AccountDto>.Failure("مستوى الحساب يجب أن يكون أعمق من الحساب الأب");
    }

    // 2. Auto-generate AccountCode
    var accountCode = await _accountCodeGenerator.GenerateCodeAsync(
        request.Level, request.ParentId, ct);

    // 3. Auto-set ColorCode from Nature
    var colorCode = AccountColorHelper.GetColorCodeForNature(request.Nature);

    // 4. Begin transaction
    await using var transaction = await _uow.BeginTransactionAsync(ct);
    try
    {
        // 5. Create Account entity (no OpeningBalance param)
        var account = Account.Create(
            accountCode: accountCode,
            nameAr: request.NameAr,
            nameEn: request.NameEn,
            nature: request.Nature,
            isLeaf: request.IsLeaf,
            parentId: request.ParentId,
            isSystem: request.IsSystem,
            categoryId: request.CategoryId,
            level: request.Level,
            description: request.Description,
            colorCode: colorCode,
            notes: request.Notes,
            createdByUserId: userId);

        await _uow.Accounts.AddAsync(account, ct);
        await _uow.SaveChangesAsync(ct);

        // 6. If OpeningBalance > 0, create Journal Entry
        if (request.OpeningBalance.HasValue && request.OpeningBalance.Value > 0)
        {
            var mappings = await _uow.SystemAccountMappings.GetFirstAsync(ct);
            var equityAccountId = mappings.OpeningBalanceEquityAccountId;

            var je = JournalEntry.Create(
                entryNumber: await _journalEntryNumberGenerator.GenerateAsync(ct),
                transactionDate: DateTime.UtcNow,
                description: "قيد افتتاحي - رصيد افتتاحي للحساب: " + request.NameAr,
                type: JournalEntryType.Opening,
                createdByUserId: userId);

            if (account.IsDebitNormal()) // Asset or Expense
            {
                je.AddDebitLine(account.Id, request.OpeningBalance.Value,
                    "رصيد افتتاحي: " + request.NameAr, account.AccountCode, request.NameAr);
                je.AddCreditLine(equityAccountId, request.OpeningBalance.Value,
                    "رصيد افتتاحي مقابل: " + request.NameAr,
                    "3102", "أرباح محتجزة"); // Fallback code/name
            }
            else // Liability, Equity, or Revenue
            {
                je.AddDebitLine(equityAccountId, request.OpeningBalance.Value,
                    "رصيد افتتاحي مقابل: " + request.NameAr,
                    "3102", "أرباح محتجزة");
                je.AddCreditLine(account.Id, request.OpeningBalance.Value,
                    "رصيد افتتاحي: " + request.NameAr, account.AccountCode, request.NameAr);
            }

            je.ValidateAndPost(); // Post immediately
            await _uow.JournalEntries.AddAsync(je, ct);
            await _uow.SaveChangesAsync(ct);
        }

        // 7. Commit
        await transaction.CommitAsync(ct);
        return Result<AccountDto>.Success(MapToDto(account));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);
        return Result<AccountDto>.Failure("حدث خطأ أثناء إنشاء الحساب");
    }
}
```

### Task 3.4 — EntryNumber Generator

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

### Task 3.5 — System Account Mappings Service

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

### Task 3.6 — Create Manual Journal Entry Command

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

✅ **Task 3 Checklist**
- [ ] AccountCodeGeneratorService creates hierarchical codes (1, 11, 1101, 11010001)
- [ ] AccountCodeGeneratorService uses SemaphoreSlim for thread safety
- [ ] ColorCode auto-generated from Nature (not user-supplied)
- [ ] AccountService.CreateAsync() transaction wraps both account + opening balance JE
- [ ] Opening Balance Journal Entry created with 2 lines (Dr/Cr based on Nature)
- [ ] JournalEntryNumberGenerator creates sequential daily numbers
- [ ] Validator rejects unbalanced entries BEFORE handler runs
- [ ] Handler validates all accounts exist before creating entry
- [ ] Entry is posted immediately upon creation (no draft journals)
- [ ] All services registered in DI container
- [ ] `CreateAccountRequest` DTO has NEW signature (no AccountCode, has OpeningBalance)

---

## 📊 Task 4 — Basic Financial Queries

### Task 4.1 — Get Account Balance Query

> See `docs/CONSTITUTION.md` for AsNoTracking patterns and `docs/AGENTS.md` for query service patterns.

### Task 4.2 — Get Account Statement Query

> See `docs/CONSTITUTION.md` for AsNoTracking patterns and `docs/AGENTS.md` for query service patterns.

✅ **Task 4 Checklist**
- [ ] All queries use AsNoTracking()
- [ ] Opening balance calculated correctly (before period)
- [ ] Running balance direction based on IsDebitNormal()
- [ ] Only IsPosted = true entries included in statements
- [ ] Results ordered by date then entry number

### Task 4.3 — Trial Balance Query Infrastructure

The Accounting Foundation provides the query infrastructure needed for Trial Balance reporting.
The full Trial Balance report UI + export will be developed in Phase 31 (Reporting Module).

Query infrastructure responsibilities:
- GetAccountBalanceQuery (Task 4.1) returns per-account debit/credit totals — reusable by Trial Balance
- Trial Balance needs: all active accounts, grouped by AccountType, showing:
  - Account Code (hierarchical: 1, 11, 1101, 11010001), Name
  - Total Debit, Total Credit
  - Net Balance (Debit Normal: TotalDebit - TotalCredit, Credit Normal: TotalCredit - TotalDebit)
  - Running Debit and Credit totals across all accounts (Debit total must equal Credit total)
- Filtering by date range and AccountType
- All queries use AsNoTracking and include only IsPosted = true entries

The Trial Balance query reuses GetAccountBalanceQuery per account, aggregated across all active accounts.

---

## 🔄 Task 6 — Annual Closing (إقفال سنوي)

### Task 6.1 — Fiscal Year Closing Workflow

The Annual Closing process zeros out all Revenue (Level-3 accounts under code 41) and Expense (Level-3 accounts under codes 51, 52) accounts and transfers the net income/loss to Retained Earnings (accounts under code 32). This is performed once per fiscal year.

**Steps:**
1. Verify ALL journal entries for the fiscal year are posted (IsPosted = true)
   - If any entry in the year is not posted, abort closing with DomainException
2. Calculate net income:
   - Total Revenue (under code 41) - Total Expense (under codes 51, 52)
   - If positive → Net Income (credit to Retained Earnings)
   - If negative → Net Loss (debit to Retained Earnings)
3. Create a closing JournalEntry:
   a. For each Revenue sub-account (e.g., 4101): Debit the balance to zero
      Example — Sales Revenue (4101) with balance 500,000:
        Debit  4101 (Sales Revenue)           500,000
        Credit 3201 (Retained Earnings)        500,000
   b. For each Expense sub-account (e.g., 5101): Credit the balance to zero
      Example — COGS (5101) with balance 300,000:
        Debit  3201 (Retained Earnings)        300,000
        Credit 5101 (COGS)                     300,000
   c. Net result: Retained Earnings (3201) reflects the difference
4. Post the closing entry (ValidateAndPost)
5. Mark fiscal year as closed in a dedicated FiscalYearClosure table
6. Prevent further posting of entries with TransactionDate in a closed fiscal year

**Data model for FiscalYearClosure:**

> See `docs/database-schema.md` Module [X.X] for the canonical FiscalYearClosures table definition.

**Checklist:**
- [ ] All entries must be posted before closing the year
- [ ] Revenue accounts zeroed out (debit to balance)
- [ ] Expense accounts zeroed out (credit to balance)
- [ ] Net income/loss transferred to Retained Earnings
- [ ] Closed fiscal year blocks new entries
- [ ] Closing entry is a regular JournalEntry (type = Manual) with full audit trail

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
| `Tests/Domain/AccountTests.cs` | **CREATE** — Remove OpeningBalance tests, add account code auto-generation tests |
| `Tests/Domain/JournalEntryTests.cs` | **EXPAND** (existing 9 tests → 25+) |
| `Tests/Domain/JournalEntryLineTests.cs` | **CREATE** |
| `Tests/Domain/FiscalYearClosureTests.cs` | **CREATE** |
| `Tests/Application/AccountServiceTests.cs` | **CREATE** — Add tests for OpeningBalance JE creation, transaction behavior |
| `Tests/Application/AccountCodeGeneratorServiceTests.cs` | **CREATE** — New: test hierarchical code generation |
| `Tests/Application/JournalEntryServiceTests.cs` | **CREATE** |
| `Tests/Application/SystemAccountMappingServiceTests.cs` | **CREATE** |
| `Tests/Application/TrialBalanceServiceTests.cs` | **CREATE** |
| `Tests/Api/AccountsControllerTests.cs` | **CREATE** |
| `Tests/Api/JournalEntriesControllerTests.cs` | **CREATE** |
| `Tests/Arch/AccountConfigurationTests.cs` | **CREATE** — Verify OpeningBalance config removed |
| `Tests/Arch/JournalEntryConfigurationTests.cs` | **CREATE** |
| `Tests/Arch/JournalEntryLineConfigurationTests.cs` | **CREATE** |

**Estimate:** ~5 hours (extra hour for new AccountCodeGeneratorService tests)

---

## Task 8 — Self-Explanation ◉ Tooltips for Accounting Concepts

**Goal**: Make every accounting term in the system self-explanatory via ◉ tooltips. Non-accountant users should understand each term without external help.

**Pattern**: Every accounting term in the UI gets a ◉ icon next to it. On hover/click, a tooltip shows a plain-Arabic explanation.

**Implementation**:
- Use WPF ToolTip with a custom style (blue background, question-mark icon)
- Create a reusable `InfoTooltip` UserControl: `<TextBlock Text="◉" ToolTip="{Binding}" Style="{StaticResource InfoTooltipStyle}"/>`
- Create `AccountingTermAttribute` to tag terms with their explanation ID
- Store explanations in a resource file or database table `AccountingTermExplanations`

**Concepts to explain with ◉ tooltips:**

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

> See `docs/ui-screens.md` for WPF UI patterns and `docs/AGENTS.md` for ViewModel patterns.

**Data structure for explanations**:

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for the table definition.

**Implementation Tasks:**
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

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

**Updated tests:**
- `Create_ValidParameters_SetsPropertiesCorrectly` — Verify accountCode, nameAr, nature, level
- `Create_NegativeParentAccountId` — Guard clause test
- `Create_AccountCodeTooLong` — Guard clause test (max 8 for Level 4)
- `Create_WithoutOpeningBalance_DoesNotRequireIt` — OpeningBalance is NOT a property
- `MarkAsDeleted_SystemAccount_Throws` — IsSystemAccount guard
- `IsDebitNormal_Asset_ReturnsTrue` — Debit-normal check
- `IsDebitNormal_Revenue_ReturnsFalse` — Credit-normal check
- `HasChildren_WithSubAccounts_ReturnsTrue` — Defense-in-depth (service uses DB AnyAsync as primary guard)

#### JournalEntry Entity (`JournalEntryTests.cs` — Expand existing)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### JournalEntryLine Entity (`JournalEntryLineTests.cs`)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### FiscalYearClosure Entity (`FiscalYearClosureTests.cs`)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

### 2. Service Tests (using `Mock<IUnitOfWork>`)

#### AccountCodeGeneratorServiceTests.cs (NEW)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

**Test cases:**
- `GenerateCodeAsync_Level1_FirstAccount_Returns1` — Empty table
- `GenerateCodeAsync_Level1_SecondAccount_Returns2` — One existing
- `GenerateCodeAsync_Level2_UnderParent1_Returns11` — First child of "1"
- `GenerateCodeAsync_Level3_UnderParent11_Returns1101` — First child of "11"
- `GenerateCodeAsync_Level4_UnderParent1101_Returns11010001` — First child of "1101"
- `GenerateCodeAsync_Level4_WithExisting_Returns11010002` — Increment sequence
- `GenerateCodeAsync_ThreadSafety_SemaphoreUsed` — Verify lock contention

#### AccountServiceTests.cs

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

**Updated tests:**
- `CreateAsync_WithOpeningBalance_CreatesJournalEntry` — Verify JE created with 2 lines
- `CreateAsync_WithOpeningBalance_TransactionCommits` — Account saved first, then JE
- `CreateAsync_WithOpeningBalance_AssetAccount_DebitNormal` — Dr NewAccount / Cr Equity
- `CreateAsync_WithOpeningBalance_RevenueAccount_CreditNormal` — Dr Equity / Cr NewAccount
- `CreateAsync_WithoutOpeningBalance_NoJournalEntry` — No JE when zero
- `CreateAsync_GeneratesAccountCode` — Verify code auto-generated
- `CreateAsync_SetsColorCodeFromNature` — Verify color auto-set

#### JournalEntryServiceTests.cs

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### TrialBalanceServiceTests.cs

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

### 3. FluentValidation Tests

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

### 4. API Controller Tests (Integration)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

### 5. Database Configuration Tests

> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions and `docs/database-schema.md` for table definitions.

**Updated tests:**
- `AccountConfiguration_DoesNotMapOpeningBalance` — Verify property removed
- `AccountConfiguration_HasFilteredUniqueIndex` — Verify IX_Accounts_AccountCode with IsActive filter

---

### 6. Phase 18-Specific Tests

#### JournalEntry 3-State Lifecycle

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### JournalEntryLine Debit/Credit Balance

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### Annual Closing

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### FiscalYear: IsDateInFiscalYear

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### Account Tree Hierarchy: Level Validation

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### Reversal Entry: ReversedByEntryId FK

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### AccountCodeGenerator Hierarchical Generation

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### ColorCode Auto-Assignment

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### Opening Balance Journal Entry Creation

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

**Test count target:** 85+ tests across all test categories (includes new AccountCodeGeneratorService + OpeningBalance JE tests).

**Estimate:** ~5 hours

---

## Task 9 — Simple Mode UX: Hide Debit/Credit for Non-Accountants

**Requirement**: Analysis Part 3 line 10: "الشاشة لا تعرض مدين ودائن للمستخدم العادي" — Screens must NOT show Debit/Credit columns to regular users. Instead:
1. Show simple transaction view (Amount, Description, Date)
2. Provide a "View Accounting Entry" button (line 14: "يوجد زر عرض القيد المحاسبي")
3. ⓘ explanations everywhere (line 18: "يوجد شرح ⓘ داخل الشاشة") — already handled in Task 8

**Implementation**:

> See `docs/AGENTS.md` for enum patterns and `docs/ui-screens.md` for WPF UI patterns.

- Add `UserPreferences.AccountingViewMode` to filter which columns display
- Cashier/Manager roles default to `Simple` mode
- Admin role defaults to `Accounting` mode
- A toggle button in journal entry views: "ⓘ عرض القيد المحاسبي" / "عرض بسيط"
- When in Simple mode: hide `Debit`/`Credit` columns, show single `Amount` column with `+`/`-` sign
- The toggle is per-user, persisted in UserPreferences

**XAML Pattern**:

> See `docs/ui-screens.md` for WPF UI patterns and `docs/AGENTS.md` for ViewModel patterns.

**Files**: UserPreferences entity (add field), JournalEntry ViewModel (add toggle), JournalEntry View (add button + column visibility toggle), UserService (persist preference).

**Estimate**: ~2 hours

---

## 📦 Final Summary

```text
┌─────────────────────────────────────────────────────────────────────────────────┐
│              PHASE 18 — ACCOUNTING FOUNDATION                                    │
│              Implementation Order (UPDATED)                                      │
├──────┬──────────────────────────────────────────────────────────┬────────────────┤
│ Task │ Deliverable                                              │ Must Pass      │
├──────┼──────────────────────────────────────────────────────────┼────────────────┤
│  0   │ SQL: 6 tables + seed data (74 accounts)                  │ Migration OK   │
│  1   │ Domain: Account (no OpeningBalance), JournalEntry,       │ No DB refs     │
│      │         Lines, SystemAccountMappings                     │ in Domain      │
│  2   │ EF Core: 3 configurations (OpeningBalance removed)      │ Precision      │
│      │                                                          │ (18,2)         │
│  3   │ Application: AccountCodeGenerator, ColorCodeHelper,      │ Validator      │
│      │              AccountService (JE for opening balance),    │ runs first     │
│      │              EntryNumberGenerator, JE Command+Handler    │                │
│  4   │ Queries: Balance + Statement + Trial Balance             │ AsNoTracking   │
│  5   │ Tests: 85+ unit tests (incl. new code gen + OB JE)      │ All green      │
│  6   │ Annual Closing: FiscalYearClosure + workflow             │ Balanced       │
│  7   │ Hierarchical Account Codes: 1/11/1101/11010001           │ Valid codes    │
│  8   │ Auto-Generation: Code, Color, Opening Balance JE         │ Atomic create  │
├──────┴──────────────────────────────────────────────────────────┴────────────────┤
│ NEW FOR THIS PHASE:                                                               │
│ • Hierarchical account codes (1→11→1101→11010001) instead of flat 4-digit codes  │
│ • AccountCodeGeneratorService (SemaphoreSlim) for thread-safe code generation     │
│ • ColorCode auto-assignment by Nature (Blue/Red/Purple/Green/Brown)               │
│ • Opening Balance creates Journal Entry (Dr Account / Cr Equity)                  │
│ • 74 seeded accounts (5+8+24+37) instead of 60                                    │
│ • OpeningBalance column REMOVED from Accounts table                                │
└──────────────────────────────────────────────────────────────────────────────────┘
```

**RULES — ZERO TOLERANCE:**
━━━━━━━━━━━━━━━━━━━━━━
- [ ] ✅ JournalEntry.ValidateAndPost() called BEFORE SaveChanges
- [ ] ✅ IsBalanced() must return true or SaveChanges never called
- [ ] ✅ Decimal precision is (18,2) — not (18,4)
- [ ] ✅ IsSystemAccount = true accounts cannot be edited or deleted
- [ ] ✅ JournalEntryLine.CreateDebit/Credit are internal — only JournalEntry creates lines
- [ ] ✅ All financial queries use AsNoTracking
- [ ] ✅ Only IsPosted = true entries appear in financial reports
- [ ] ✅ Arabic error messages in ALL DomainExceptions
- [ ] ✅ Account snapshots (Code + Name) stored in JournalEntryLine
- [ ] ✅ Account codes are hierarchical: 1-digit (L1), 2-digit (L2), 4-digit (L3), 8-digit (L4)
- [ ] ✅ OpeningBalance is NOT on Account entity — created as Journal Entry by service layer
- [ ] ✅ ColorCode is system-generated from Nature — NOT user-supplied
- [ ] ✅ AccountCodeGeneratorService uses SemaphoreSlim (thread-safe)
- [ ] ✅ Account creation with OpeningBalance wraps in transaction (account → JE → commit)
- [ ] ✅ Filtered unique index on AccountCode: WHERE IsActive = 1

---

## § Post-Review Fix Status (v4.6.8 — Applied 2026-06-06)

After the deep code review by 3 subagents, the following critical bugs were fixed:

### 🔴 CRITICAL (10 fixed)

| # | File | Issue | Fix |
|---|------|-------|-----|
| 1 | `Account.cs` | Missing 3 navigation properties (`ParentAccount`, `SubAccounts`, `JournalLines`) | Added all 3 |
| 2 | `Account.cs` | Missing `parentAccountId <= 0` guard | Added guard with Arabic message |
| 3 | `Account.cs` | `Update()` allowed changing `AccountType` | Removed `AccountType` param from `Update()` |
| 4 | `JournalEntry.cs` | Missing `ReversedByEntryId` / `BranchId` properties | Added both + `ReversedByEntry` nav |
| 5 | `JournalEntry.cs` | `AddDebitLine`/`AddCreditLine` missing `IsReversed` guard | Added guard |
| 6 | `JournalEntry.cs` | Arabic error used `الخصوم`/`الإيداعات` (wrong terms) | Fixed to `المدين`/`الدائن` |
| 7 | `JournalEntryLine.cs` | Missing `Account` navigation property | Added |
| 8 | `AnnualClosingService.cs:197` | `BeginTransactionAsync` with `EnableRetryOnFailure` (RULE-275 violation) | Removed explicit transaction — EF Core implicit transaction |
| 9 | `JournalEntryNumberGenerator.cs:23` | `CountAsync + 1` race condition (RULE-255 violation) | Replaced with `SemaphoreSlim` + `OrderByDescending(Id)` |
| 10 | `Account.cs` | No `createdByUserId <= 0` guard | Added |

### 🟠 MAJOR (9 fixed)

| # | File | Issue | Fix |
|---|------|-------|-----|
| 11 | `JournalEntry.cs` | `Description` was nullable (`string?`) | Made required non-nullable with guard |
| 12 | `JournalEntry.cs` | `UpdateTimestamp()` not called in `AddDebitLine`/`AddCreditLine` | Added |
| 13 | `JournalEntryLine.cs` | Missing `SortOrder` property | Added |
| 14 | `SystemAccountMappings.cs` | 6 missing guards on FK fields | Added all guards |
| 15 | `SystemAccountMappings.cs` | `GetPaymentAccountId` didn't handle credit path | Added `AccountsReceivableAccountId` path |
| 16 | `AccountsController.cs:91` | Domain entity returned directly (`SystemAccountMappings`) | Now returns `SystemAccountMappingsDto` |
| 17 | `AccountBalanceDto.cs:520` | `byte AccountType` instead of domain enum | Changed to `AccountType` |
| 18 | `AccountsController.cs` | Auth policies used `ManagerAndAbove` for reads | Changed GET endpoints to `AllStaff` |
| 19 | `AccountsController.cs` | `GetBalance` always returned 404 on failure | Added proper 404/400 dispatch |

### 🟡 MINOR (6 fixed)

| # | File | Issue | Fix |
|---|------|-------|-----|
| 20 | `Account.cs` | `AccountCode` length not validated (DB max 20) | Added guard |
| 21 | `FiscalYearClosure.cs` | Missing `Notes` field | Added |
| 22 | `AccountConfiguration.cs:23` | Self-referencing FK used bare `.WithMany()` | Changed to `.WithMany(x => x.SubAccounts)` |
| 23 | `JournalEntryLineConfiguration.cs` | FK to Account used bare `.WithMany()` | Changed to `.WithMany(x => x.JournalLines)` |
| 24 | `AccountTests.cs` | Missing edge case tests | Added `Create_NegativeParentAccountId` and `Create_AccountCodeTooLong` |
| 25 | `JournalEntryTests.cs` | Missing empty/whitespace description tests | Added `Create_EmptyDescription` and `Create_WhitespaceDescription` |

### ✅ Already Correct (verified by review)

- All FKs use `DeleteBehavior.Restrict` ✅
- All money fields use `decimal(18,2)` ✅
- Services return `Result<T>` (no raw exceptions) ✅
- Validators exist for both commands ✅
- `IsSystemAccount` protection in domain ✅
- Enum values match AGENTS.md Section 3 ✅
- Arabic error messages in all user-facing code ✅
- Double-entry balance rules enforced (`IsBalanced`, 0.001 tolerance) ✅
- `JournalEntryLine.CreateDebit`/`CreateCredit` are `internal` ✅

### 🔧 New Patterns Introduced

- **RULE-275 enforcement**: `AnnualClosingService` no longer uses `BeginTransactionAsync`
- **Description required**: `JournalEntry.Create` now requires non-nullable `description` parameter
- **AccountType immutability**: `Account.Update()` no longer accepts `AccountType` — type is set-only on creation
- **Controller auth**: Read endpoints use `AllStaff`; write/close endpoints use `ManagerAndAbove`
- **DTO purity**: `AccountBalanceDto` uses domain enum instead of `byte`; `SystemAccountService` returns DTO not entity

### 🆕 Post-Review Schema Alignment (Applied after initial review)

| # | File | Issue | Fix |
|---|------|-------|-----|
| 26 | `docs/plan.md` | Account code strategy used flat 4-digit codes (1000, 1100, 1110, 1111) | Changed to hierarchical: Level 1=1 digit, Level 2=2 digits, Level 3=4 digits, Level 4=8 digits |
| 27 | `docs/plan.md` | Seed count stated 60 accounts | Updated to 74 accounts (5+8+24+37) |
| 28 | `docs/plan.md` | `OpeningBalance` described as Account column | Removed — opening balance now creates Journal Entry in service layer |
| 29 | `docs/plan.md` | Account code examples used 4-digit flat format | All examples updated to hierarchical format (1, 11, 1101, 11010001) |
| 30 | `docs/plan.md` | ColorCode described as user-supplied | Changed to system-generated from Nature |
| 31 | `docs/plan.md` | DTO `CreateAccountRequest` had `AccountCode` field | Removed — code is system-generated; added `OpeningBalance` for JE creation |
| 32 | `docs/plan.md` | Missing `AccountCodeGeneratorService` section | Added — thread-safe hierarchical code generation with SemaphoreSlim |
| 33 | `docs/plan.md` | Missing ColorCode auto-generation section | Added — static helper mapping Nature → hex color |
| 34 | `docs/plan.md` | AccountService.CreateAsync() had no transaction for OB JE | Updated to wrap account creation + OB JE in BeginTransactionAsync |

---

## Appendix A — CreateAccountRequest DTO (Updated Signature)

```csharp
public record CreateAccountRequest(
    string NameAr,
    string? NameEn,
    byte Nature,                     // 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense
    bool IsLeaf,                     // true = detail (can transact), false = parent
    int? ParentId,                   // null for Level 1 accounts
    bool IsSystem,
    short? CategoryId,
    decimal? OpeningBalance,         // NEW — transient; creates Journal Entry if > 0
    string? Description,
    string? Notes
);
```

**Key changes:**
- `AccountCode` REMOVED (system-generated by AccountCodeGeneratorService)
- `OpeningBalance` ADDED (transient DTO property — NOT stored on Account entity)
- `ColorCode` is NOT in the request (system-assigned from Nature)

## Appendix B — UpdateAccountRequest DTO (Updated)

```csharp
public record UpdateAccountRequest(
    string NameAr,
    string? NameEn,
    // NO AccountCode — immutable after creation
    // NO ColorCode — system-managed
    string? Description,
    string? Notes
);
```

## Appendix C — AccountDto (Updated)

```csharp
public record AccountDto(
    int Id,
    string AccountCode,              // Hierarchical: "1", "11", "1101", "11010001"
    string NameAr,
    string? NameEn,
    string DisplayName,              // Culture-aware (checks CurrentUICulture)
    byte Nature,
    string NatureDisplay,            // "أصل", "خصم", etc.
    byte Level,
    bool IsLeaf,
    bool AllowTransactions,          // Computed => IsLeaf
    int? ParentId,
    string? ParentName,
    bool IsSystem,
    short? CategoryId,
    string? Description,
    string ColorCode,                // Now actual value, not null
    string? Notes,
    bool IsActive
);
```

## Appendix D — 74 Seeded Account Structure

### Level 1 — Groups (5 accounts, 1 digit)

| Code | Name (Ar) | Name (En) | Nature |
|------|-----------|-----------|--------|
| 1 | الأصول | Assets | Asset |
| 2 | الخصوم | Liabilities | Liability |
| 3 | حقوق الملكية | Equity | Equity |
| 4 | الإيرادات | Revenue | Revenue |
| 5 | المصروفات | Expenses | Expense |

### Level 2 — Main Categories (8 accounts, 2 digits)

| Code | Parent | Name (Ar) | Nature |
|------|--------|-----------|--------|
| 11 | 1 | الأصول المتداولة | Asset |
| 12 | 1 | الأصول الثابتة | Asset |
| 21 | 2 | الخصوم المتداولة | Liability |
| 31 | 3 | رأس المال | Equity |
| 32 | 3 | الأرباح والخسائر | Equity |
| 41 | 4 | الإيرادات التشغيلية | Revenue |
| 51 | 5 | تكاليف النشاط | Expense |
| 52 | 5 | مصروفات تشغيلية | Expense |

### Level 3 — Sub-Categories (24 accounts, 4 digits)

| Code | Parent | Name (Ar) | Nature |
|------|--------|-----------|--------|
| 1101 | 11 | النقدية | Asset |
| 1102 | 11 | البنوك | Asset |
| 1103 | 11 | العملاء | Asset |
| 1104 | 11 | المخزون | Asset |
| 1105 | 11 | أوراق قبض | Asset |
| 1106 | 11 | مصروفات مدفوعة مقدماً | Asset |
| 1107 | 11 | عهد الموظفين | Asset |
| 1201 | 12 | أراضي | Asset |
| 1202 | 12 | مباني | Asset |
| 1203 | 12 | معدات وأجهزة | Asset |
| 1204 | 12 | سيارات | Asset |
| 1205 | 12 | أثاث | Asset |
| 2101 | 21 | الموردون | Liability |
| 2102 | 21 | أوراق دفع | Liability |
| 2103 | 21 | الضرائب | Liability |
| 2104 | 21 | مستحقات موظفين | Liability |
| 3101 | 31 | رأس المال | Equity |
| 3201 | 32 | الأرباح المحتجزة | Equity |
| 4101 | 41 | إيرادات المبيعات | Revenue |
| 4102 | 41 | مردودات ومسموحات | Revenue |
| 5101 | 51 | تكلفة البضاعة المباعة | Expense |
| 5201 | 52 | رواتب وأجور | Expense |
| 5202 | 52 | إيجار | Expense |
| 5203 | 52 | مرافق (كهرباء - ماء) | Expense |

### Level 4 — Detail Accounts (37 accounts, 8 digits)

| Code | Parent | Name (Ar) | Nature |
|------|--------|-----------|--------|
| 11010001 | 1101 | صندوق النقدية | Asset |
| 11010002 | 1101 | نقدية في الطريق | Asset |
| 11020001 | 1102 | البنك الأهلي - جاري | Asset |
| 11020002 | 1102 | البنك التجاري - جاري | Asset |
| 11030001 | 1103 | عملاء نقدي | Asset |
| 11040001 | 1104 | بضاعة بالمخازن | Asset |
| 11040002 | 1104 | مواد خام | Asset |
| 11040003 | 1104 | بضاعة تحت التشغيل | Asset |
| 11070001 | 1107 | عهد - موظفين | Asset |
| 12020001 | 1202 | المبنى الرئيسي | Asset |
| 12020002 | 1202 | المستودع | Asset |
| 12030001 | 1203 | أجهزة حاسب آلي | Asset |
| 12030002 | 1203 | أجهزة طباعة | Asset |
| 12040001 | 1204 | سيارات نقل | Asset |
| 12050001 | 1205 | أثاث مكتبي | Asset |
| 21010001 | 2101 | موردون نقدي | Liability |
| 21030001 | 2103 | ضريبة المبيعات (خرج) | Liability |
| 21030002 | 2103 | ضريبة المشتريات (دخل) | Liability |
| 21040001 | 2104 | رواتب مستحقة | Liability |
| 21040002 | 2104 | مكافآت مستحقة | Liability |
| 31010001 | 3101 | رأس مال الشركة | Equity |
| 32010001 | 3201 | أرباح محتجزة سابقة | Equity |
| 32010002 | 3201 | أرباح العام الحالي | Equity |
| 41010001 | 4101 | مبيعات منتجات | Revenue |
| 41010002 | 4101 | مبيعات خدمات | Revenue |
| 41020001 | 4102 | مردودات مبيعات | Revenue |
| 51010001 | 5101 | COGS منتجات | Expense |
| 51010002 | 5101 | COGS خدمات | Expense |
| 52010001 | 5201 | رواتب إدارة | Expense |
| 52010002 | 5201 | رواتب عمال | Expense |
| 52020001 | 5202 | إيجار المقر الرئيسي | Expense |
| 52020002 | 5202 | إيجار المستودع | Expense |
| 52030001 | 5203 | فاتورة كهرباء | Expense |
| 52030002 | 5203 | فاتورة مياه | Expense |
| 52030003 | 5203 | اتصالات وإنترنت | Expense |
| 5204 | 52 | مصروفات تسويق | Expense |
| 5205 | 52 | مصروفات صيانة | Expense |

> Note: The last 2 in Level 3 (5204, 5205) are unexpanded Level 3 categories at this level of granularity. The 74 total = 5 (L1) + 8 (L2) + 24 (L3) + 37 (L4) = 74.

**SystemAccountMappings** (20+ mappings) reference the Level 4 accounts above:

| Mapping | Account Code | Account Name |
|---------|-------------|--------------|
| CashAccountId | 11010001 | صندوق النقدية |
| BankAccountId | 11020001 | البنك الأهلي - جاري |
| AccountsReceivableAccountId | 11030001 | عملاء نقدي |
| InventoryAssetAccountId | 11040001 | بضاعة بالمخازن |
| AccountsPayableAccountId | 21010001 | موردون نقدي |
| VatOutputAccountId | 21030001 | ضريبة المبيعات (خرج) |
| VatInputAccountId | 21030002 | ضريبة المشتريات (دخل) |
| CapitalAccountId | 31010001 | رأس مال الشركة |
| RetainedEarningsAccountId | 32010001 | أرباح محتجزة سابقة |
| SalesRevenueAccountId | 41010001 | مبيعات منتجات |
| SalesReturnsAccountId | 41020001 | مردودات مبيعات |
| COGSAccountId | 51010001 | COGS منتجات |
| SalariesExpenseAccountId | 52010001 | رواتب إدارة |
| RentExpenseAccountId | 52020001 | إيجار المقر الرئيسي |
| OpeningBalanceEquityAccountId | 32010001 | أرباح محتجزة سابقة |
| PurchaseReturnAccountId | 41020001 | مردودات مبيعات |
| DeliveryChargesRevenueAccountId | (TBD in Phase 28) | (سيتم إضافتها لاحقاً) |
