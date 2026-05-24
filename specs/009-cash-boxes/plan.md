# Implementation Plan: Cash Boxes (v4.3)

**Branch**: `009-cash-boxes` | **Date**: 2026-05-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/009-cash-boxes/spec.md`

---

## Summary

Implement a cash box subsystem where all financial transactions (invoice payments, expenses, transfers, sales returns) are recorded as immutable `CashTransaction` entries. `CashBox.CurrentBalance` is always computed from the running sum — never stored. Transfers between boxes are atomic dual-entry operations. End-of-day `DailyClosure` snapshots are immutable and unique per box per date.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS
**Primary Dependencies**: Entity Framework Core 10, FluentValidation 11, Serilog 8
**Storage**: SQL Server 2019+ — new tables: `CashBoxes`, `CashTransactions`, `DailyClosures`; extended: `SalesInvoices`, `PurchaseInvoices` (add `CashBoxId` FK)
**Testing**: xUnit + Moq + FluentAssertions
**Target Platform**: Windows x64 (API as Windows Service + WPF Desktop)
**Project Type**: Desktop App + REST API (Clean Architecture — 6 existing projects)
**Performance Goals**: Balance inquiry and transaction history for 10,000 entries resolves in < 1 second
**Constraints**: All amounts = `decimal(18,2)`. Negative amounts forbidden. `CurrentBalance` never stored. All multi-table ops in `BeginTransactionAsync`
**Scale/Scope**: 1–10 cash boxes per store, ~50–500 transactions per box per day

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal-Only Financial Precision | ✅ PASS | All `Amount`, `OpeningBalance`, `ClosingBalance` fields → `decimal(18,2)` |
| II | Domain-Computed Financial Formulas | ✅ PASS | `CurrentBalance` computed in Domain via `CashBox.ComputeBalance(transactions)` — never stored |
| III | Transactional Integrity | ✅ PASS | Cash transfers: both `TransferOut` + `TransferIn` inside single `BeginTransactionAsync`. Invoice posting: `CashTransaction` created inside the existing invoice transaction |
| IV | Invoice Lifecycle State Machine | ✅ PASS | `CashTransaction` created on `Draft → Posted`. Offsetting entry created on `Posted → Cancelled` |
| V | Stock Integrity | ✅ N/A | Cash boxes do not affect stock |
| VI | Result Pattern | ✅ PASS | All service methods return `Result<T>` |
| VII | Clean Architecture Boundaries | ✅ PASS | `CashBox` domain entity has zero EF Core dependencies; `ICashBoxService` in Application layer |
| VIII | Security | ✅ PASS | All new endpoints carry `[Authorize]` |
| IX | Four-Layer Validation | ✅ PASS | Domain guard (balance ≥ 0); Application pre-check (balance before transfer); FluentValidation (amount > 0); DB CHECK (Amount > 0) |
| X | Logging | ✅ PASS | All cash transactions logged via Serilog at `Information` level |
| XI | EF Core Conventions | ✅ PASS | Fluent API only; `DeleteBehavior.Restrict` on all FKs; `nvarchar` for name fields |
| XII | Audit Trail | ✅ PASS | `CashTransaction.CreatedByUserId` FK → Users |
| XIII | Delete Strategy | ✅ PASS | `CashTransaction` — immutable, no delete. `CashBox` — soft delete (`IsActive = false`) only |
| XIV | Defensive Programming | ✅ PASS | `CashBox` constructor: name not empty, openingBalance ≥ 0. `CashTransaction` constructor: amount > 0 |
| XV | WPF Interactive Dialogs | ✅ PASS | All dialogs via `IDialogService` |
| XVI | Toast Notifications | ✅ PASS | Transfer success, expense recorded → toast |
| XVII | Real-Time UI Validation | ✅ PASS | `ViewModelBase` implements `INotifyDataErrorInfo` |

**Gate Result**: ✅ ALL CLEAR — no violations.

---

## Project Structure

### Documentation (this feature)

```text
specs/009-cash-boxes/
├── plan.md              ← This file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── quickstart.md        ← Phase 1 output
├── contracts/           ← Phase 1 output
│   ├── api-contracts.md
│   └── ui-contracts.md
└── tasks.md             ← Phase 2 output (/speckit-tasks)
```

### Source Code (affected paths)

```text
SalesSystem/
├── SalesSystem.Domain/
│   └── Entities/
│       ├── CashBox.cs                  ← NEW
│       ├── CashTransaction.cs          ← NEW
│       └── DailyClosure.cs             ← NEW
│
├── SalesSystem.Contracts/
│   ├── Requests/
│   │   ├── CreateCashBoxRequest.cs     ← NEW
│   │   ├── AddCashTransactionRequest.cs ← NEW
│   │   └── CashTransferRequest.cs      ← NEW
│   └── Responses/
│       ├── CashBoxDto.cs               ← NEW
│       ├── CashTransactionDto.cs       ← NEW
│       └── DailyClosureDto.cs          ← NEW
│
├── SalesSystem.Application/
│   ├── Interfaces/Services/
│   │   └── ICashBoxService.cs          ← NEW
│   └── Services/
│       └── CashBoxService.cs           ← NEW
│
├── SalesSystem.Infrastructure/
│   ├── Data/
│   │   ├── SalesDbContext.cs           ← EXTEND (add DbSets)
│   │   └── Configurations/
│   │       ├── CashBoxConfiguration.cs ← NEW
│   │       ├── CashTransactionConfiguration.cs ← NEW
│   │       └── DailyClosureConfiguration.cs    ← NEW
│   └── Migrations/                     ← NEW migration
│
├── SalesSystem.Api/
│   ├── Controllers/
│   │   └── CashBoxesController.cs      ← NEW
│   └── Validators/
│       ├── CreateCashBoxRequestValidator.cs     ← NEW
│       ├── AddCashTransactionRequestValidator.cs ← NEW
│       └── CashTransferRequestValidator.cs      ← NEW
│
└── SalesSystem.DesktopPWF/
    ├── Services/Api/
    │   └── CashBoxApiService.cs        ← NEW
    ├── Views/CashBoxes/
    │   ├── CashBoxesListView.xaml      ← NEW
    │   ├── CashBoxEditorView.xaml      ← NEW
    │   └── CashBoxTransactionsView.xaml ← NEW
    └── ViewModels/CashBoxes/
        ├── CashBoxesListViewModel.cs   ← NEW
        ├── CashBoxEditorViewModel.cs   ← NEW
        └── CashBoxTransactionsViewModel.cs ← NEW
```

---

## Complexity Tracking

No constitution violations — standard architecture patterns throughout.
