# Implementation Tasks: Accounting Foundation (Phase 18)

**Feature**: `018-accounting-foundation`
**Date**: 2026-05-25
**Model Context**: *Optimized for execution by smaller models. Every task explicitly defines file paths and strict actions.*

---

## Pre-requisites
- [x] All occurrences of `(18,4)` have been changed to `(18,2)`.
- [x] Read `specs/018-accounting-foundation/plan.md`.
- [x] Read `specs/018-accounting-foundation/data-model.md`.

---

## Phase 1: Domain Enums

**Purpose**: Define the static types for the accounting module.

- [X] T001 [P] [US1] Create file `SalesSystem.Domain/Accounting/Enums/AccountType.cs`. Add enum `AccountType` with: `Asset = 1`, `Liability = 2`, `Equity = 3`, `Revenue = 4`, `Expense = 5`.
- [X] T002 [P] [US1] Create file `SalesSystem.Domain/Accounting/Enums/JournalEntryType.cs`. Add enum `JournalEntryType` with: `Sales = 1`, `SalesReturn = 2`, `Purchase = 3`, `PurchaseReturn = 4`, `Expense = 5`, `StockWriteOff = 6`, `Transfer = 7`, `Manual = 8`, `OpeningBalance = 9`.

**Checkpoint**: Enums compile without errors.

---

## Phase 2: Domain Entities (The Core Engine)

**Purpose**: Implement the rich domain model with strict validation rules.

- [X] T003 [US1] Create file `SalesSystem.Domain/Accounting/Entities/Account.cs` inheriting from `BaseEntity`. Add properties: `AccountCode` (string), `NameAr` (string), `NameEn` (string), `AccountType` (enum), `ParentAccountId` (int?), `IsSystemAccount` (bool), `IsActive` (bool), `Notes` (string?). Add factory method `Create()` and `Update()`/`Deactivate()` methods that throw `DomainException` (in Arabic) if `IsSystemAccount == true`. Add method `bool IsDebitNormal() => AccountType == AccountType.Asset || AccountType == AccountType.Expense;`.
- [X] T004 [US1] Create file `SalesSystem.Domain/Accounting/Entities/SystemAccountMappings.cs` inheriting from `BaseEntity`. Add `BranchId` (int?) and `int` properties for all default accounts: `DefaultCashAccountId`, `DefaultBankAccountId`, `InventoryAssetAccountId`, `AccountsReceivableAccountId`, `AccountsPayableAccountId`, `VatOutputAccountId`, `VatInputAccountId`, `CapitalAccountId`, `SalesRevenueAccountId`, `SalesReturnAccountId`, `CogsAccountId`, `GeneralExpenseAccountId`, `SpoilageLossAccountId`. Add `GetPaymentAccountId(string paymentMethod)` returning Cash or Bank.
- [X] T005 [US1] Create file `SalesSystem.Domain/Accounting/Entities/JournalEntryLine.cs` inheriting from `BaseEntity`. Add properties: `JournalEntryId`, `AccountId`, `AccountCode`, `AccountNameAr`, `Debit` (decimal), `Credit` (decimal), `Description`. Make constructors private. Add `internal static` methods `CreateDebit()` and `CreateCredit()`.
- [X] T006 [US1] Create file `SalesSystem.Domain/Accounting/Entities/JournalEntry.cs` inheriting from `BaseEntity`. Add properties: `EntryNumber`, `TransactionDate`, `Description`, `EntryType`, `ReferenceType`, `ReferenceId`, `ReferenceNumber`, `IsPosted`, `IsReversed`, `CreatedBy`, `CreatedAt`, `PostedBy`, `PostedAt`. Add `private readonly List<JournalEntryLine> _lines`. Add methods `AddDebitLine()` and `AddCreditLine()`.
- [X] T007 [US1] In `JournalEntry.cs`, implement `bool IsBalanced()` which returns `Math.Abs(TotalDebit - TotalCredit) < 0.001m`. Implement `ValidateAndPost(int postedBy)` which throws a `DomainException` with Arabic details if `_lines` is empty or if `!IsBalanced()`. If balanced, sets `IsPosted = true` and `PostedBy = postedBy`.

**Checkpoint**: All Domain Entities are fully implemented with encapsulation (`private set`).

---

## Phase 3: Infrastructure (EF Core Configurations)

**Purpose**: Map the Domain to SQL Server securely.

- [X] T008 [P] [US1] Create file `SalesSystem.Infrastructure/Persistence/Configurations/AccountConfiguration.cs`. Map `Account`. Set `AccountCode` to `IsRequired().HasMaxLength(20)`. Add unique index `HasIndex(x => x.AccountCode).IsUnique()`. Configure self-referencing relationship for `ParentAccountId` with `DeleteBehavior.Restrict`.
- [X] T009 [P] [US1] Create file `SalesSystem.Infrastructure/Persistence/Configurations/JournalEntryLineConfiguration.cs`. Map `JournalEntryLine`. Crucially set `.HasPrecision(18,2)` for `Debit` and `Credit`. Configure relationship to `Account` with `DeleteBehavior.Restrict`. Add indexes for `JournalEntryId` and `AccountId`.
- [X] T010 [P] [US1] Create file `SalesSystem.Infrastructure/Persistence/Configurations/JournalEntryConfiguration.cs`. Map `JournalEntry`. Set `EntryNumber` max length 50 + Unique Index. Configure relationship to `Lines` with `DeleteBehavior.Cascade`.
- [X] T011 [US1] Open `SalesSystem.Infrastructure/Persistence/AppDbContext.cs`. Add `DbSet` properties for `Accounts`, `JournalEntries`, `JournalEntryLines`, and `SystemAccountMappings`. In `OnModelCreating`, add `builder.ApplyConfigurationsFromAssembly(...)` if not already present.

**Checkpoint**: EF Core is fully aware of the accounting tables and precision rules.

---

## Phase 4: Application Services

**Purpose**: Create helper services for journal numbering and mapping lookups.

- [X] T012 [P] [US1] Create file `SalesSystem.Application/Accounting/Services/JournalEntryNumberGenerator.cs`. Implement interface `IJournalEntryNumberGenerator` with `Task<Result<string>> GenerateAsync(CancellationToken ct)`. Logic: Return `$"JE-{DateTime.Today:yyyyMMdd}-{count+1:D4}"`. Count is fetched from `IUnitOfWork.JournalEntries`.
- [X] T013 [P] [US1] Create file `SalesSystem.Application/Accounting/Services/SystemAccountService.cs`. Implement interface `ISystemAccountService`. Add `GetMappingsAsync(int? branchId)` which queries `SystemAccountMappings` falling back to global `BranchId == null`. Return `Result<SystemAccountMappings>` on success or Arabic error.

**Checkpoint**: Services compile and are ready for DI.

---

## Phase 5-6: Service Layer (Write + Read Operations) — Adapted from CQRS

**Purpose**: Implement the Service Layer pattern with direct interfaces (no MediatR — per RULE-147/148).

- [X] T014 [US1] Create file `SalesSystem.Contracts/Requests/AccountingRequests.cs`. Define `CreateJournalEntryRequest` and `JournalEntryLineRequest` records with all required fields (TransactionDate, Description, EntryType, ReferenceType/Id/Number, CreatedBy, Lines).
- [X] T015 [US1] Create file `SalesSystem.Application/Accounting/Validators/CreateJournalEntryRequestValidator.cs`. Inherit `AbstractValidator<CreateJournalEntryRequest>`. Add rules: `Lines` must have at least 1 item, `Math.Abs(lines.Sum(l => l.Debit) - lines.Sum(l => l.Credit)) < 0.001m` (Custom Rule with Arabic message).
- [X] T016 [US1] Create file `SalesSystem.Application/Accounting/Services/JournalEntryService.cs` implementing `IJournalEntryService`. Methods: `CreateJournalEntryAsync`, `GetAccountBalanceAsync`, `GetAccountLedgerAsync`. Inject `IUnitOfWork` and `IJournalEntryNumberGenerator`. Use `Result<T>` return types, Arabic messages, Serilog logging.
- [X] T017 [P] [US2] Create `IJournalEntryService.GetAccountBalanceAsync(accountId, asOfDate?)` using `IUnitOfWork` to sum Debit/Credit from posted lines. Calculate balance using `Account.IsDebitNormal()`. Return `Result<AccountBalanceDto>`.
- [X] T018 [P] [US2] Create `IJournalEntryService.GetAccountLedgerAsync(accountId, startDate, endDate)` using `IUnitOfWork`. Calculate Opening Balance from posted entries BEFORE startDate. Fetch period lines with running balance. Return `Result<AccountLedgerDto>`.

---

## Phase 7: Testing & Migration

**Purpose**: Final verification.

- [X] T019 [US3] Create file `SalesSystem.Domain.Tests/Accounting/JournalEntryTests.cs`. Write xUnit tests to verify: 1) `IsBalanced()` returns true when debit=credit. 2) `ValidateAndPost()` throws `DomainException` when unbalanced. 3) Modifying an account with `IsSystemAccount == true` throws `DomainException`.
- [X] T020 [US3] Create a new Entity Framework Core Migration named `AddAccountingFoundation` via .NET CLI.
- [X] T021 [US3] Create file `SalesSystem.Infrastructure/Data/Seeders/AccountingSeeder.cs`. Write logic to seed `AccountTypes`, default `Accounts` (Cash, Bank, Sales Revenue, COGS, VAT, etc. with `IsSystemAccount = true`), and a global `SystemAccountMappings` row. Call this seeder from `DbSeeder.cs`.
