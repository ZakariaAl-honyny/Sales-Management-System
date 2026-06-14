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

> See `docs/database-schema.md` Module [4.2] for the canonical AccountTypes table definition.

> See `docs/database-schema.md` for seed data patterns. The `AccountingSeeder` in `Infrastructure/Data/Seeders/` contains canonical seed logic.

> See `docs/database-schema.md` for seed data patterns. The `AccountsSeeder` in `Infrastructure/Data/Seeders/` contains canonical seed logic.
✅ Task 0 Checklist
 All 6 tables created without errors
 All foreign keys valid
 Default accounts seeded (20+ accounts)
 SystemAccountMappings has one global row
 CHK_DebitOrCredit constraint applied
 CHK_NoNegativeValues constraint applied
🏗️ Task 1 — Domain Layer
> See `docs/AGENTS.md` Section 3 for all enum values (AccountType, JournalEntryType, etc.).

### Task 1.2 — Account Entity
> See `docs/database-schema.md` Module 4.2 for the Account table definition and `docs/CONSTITUTION.md`/`AGENTS.md` for entity patterns (private set, Guard Clauses, domain methods).
> See `docs/database-schema.md` Module 5.1 for the JournalEntry table definition and `docs/CONSTITUTION.md`/`AGENTS.md` for entity patterns (private set, Guard Clauses, domain methods).

### Task 1.4 — JournalEntryLine Entity
> See `docs/database-schema.md` Module 5.2 for the JournalEntryLine table definition and `docs/AGENTS.md`/`CONSTITUTION.md` for entity patterns.

### Task 1.5 — SystemAccountMappings Entity
> See `docs/database-schema.md` Module 6 for the SystemAccountMappings table definition and `docs/AGENTS.md`/`CONSTITUTION.md` for entity patterns.
✅ Task 1 Checklist
 All 4 entities created in Domain/Accounting/Entities/
 JournalEntry.IsBalanced() implemented
 JournalEntry.ValidateAndPost() throws if unbalanced
 Arabic error messages in all DomainExceptions
 IsSystemAccount prevents deletion/modification
 CreateDebit and CreateCredit are internal (only JournalEntry can create lines)
⚙️ Task 2 — Infrastructure (EF Core Configuration)
Task 2.1 — Account Configuration
> See `docs/AGENTS.md` §2.16 (EF Core Conventions) and `docs/database-schema.md` Module 4.2 for the canonical AccountConfiguration pattern (DeleteBehavior.Restrict, HasPrecision(18,2), HasMaxLength, unique indexes).

### Task 2.2 — JournalEntry Configuration
> See `docs/database-schema.md` Module 5.1 for the JournalEntry table definition and `docs/AGENTS.md` §2.16 for EF Core conventions.

### Task 2.3 — JournalEntryLine Configuration
> See `docs/database-schema.md` Module 5.2 and `docs/AGENTS.md` §2.16 for JournalEntryLine configuration (decimal(18,2), Restrict delete, performance indexes).
✅ Task 2 Checklist
 All 3 configurations registered in AppDbContext.OnModelCreating()
  Decimal precision is (18,2) for all financial columns
  Unique indexes on EntryNumber and AccountCode
  Restrict delete on ALL foreign keys (JournalEntry→Lines, Account→Account)
  Domain-level composition: JournalEntry owns Lines but deletion is prevented if posted
  Reversal via ReversedByEntryId FK (Restrict) preserves audit trail
⚙️ Task 3 — Application Layer (Services)
Task 3.1 — EntryNumber Generator

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.
Task 3.2 — System Account Mappings Service

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.
Task 3.3 — Create Manual Journal Entry Command

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.
✅ Task 3 Checklist
 JournalEntryNumberGenerator creates sequential daily numbers
 Validator rejects unbalanced entries BEFORE handler runs
 Handler validates all accounts exist before creating entry
 Entry is posted immediately upon creation (no draft journals)
 All services registered in DI container
📊 Task 4 — Basic Financial Queries
Task 4.1 — Get Account Balance Query

> See `docs/CONSTITUTION.md` for AsNoTracking patterns and `docs/AGENTS.md` for query service patterns.
Task 4.2 — Get Account Statement Query

> See `docs/CONSTITUTION.md` for AsNoTracking patterns and `docs/AGENTS.md` for query service patterns.
✅ Task 4 Checklist
 All queries use AsNoTracking()
 Opening balance calculated correctly (before period)
 Running balance direction based on IsDebitNormal()
 Only IsPosted = true entries included in statements
 Results ordered by date then entry number
🧪 Task 5 — Unit Tests

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.
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

> See `docs/database-schema.md` Module [X.X] for the canonical FiscalYearClosures table definition.

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

> See `docs/ui-screens.md` for WPF UI patterns and `docs/AGENTS.md` for ViewModel patterns.

**Data structure for explanations**:

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for the table definition.

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

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### JournalEntry Entity (`JournalEntryTests.cs` — Expand existing)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### JournalEntryLine Entity (`JournalEntryLineTests.cs`)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

#### FiscalYearClosure Entity (`FiscalYearClosureTests.cs`)

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

---

### 2. Service Tests (using `Mock<IUnitOfWork>`)

#### AccountServiceTests.cs

> See test patterns in `SalesSystem.Domain.Tests/` and `SalesSystem.Application.Tests/`. Test methodology follows xUnit + Moq + FluentAssertions as specified in `docs/MASTER-PLAN.md`.

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