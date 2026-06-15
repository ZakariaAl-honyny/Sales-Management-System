# Phase 29 — Receipts & Payments Module: Comprehensive Implementation Plan

> **Version**: 1.0 — Full rewrite after 3 subagent reviews (Code Reviewer, Backend Architect, Database Engineer)
> **Scope**: Complete CashBox Enhancement, Customer/Supplier Payment Enhancement, Daily Closure Enhancement, Cheque Management

---

## Table of Contents
1. [Architecture — 3 Sub-Modules](#1-architecture--3-sub-modules)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [Design Catalog](#4-design-catalog)
5. [Gap Analysis](#5-gap-analysis)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Architecture — 3 Sub-Modules

The Receipts & Payments Module extends and enhances 3 existing sub-modules:

| # | Sub-Module | Scope | Type |
|---|------------|-------|------|
| 🏦 | **CashBox Management** | Extend CashBox entity with AccountId FK + CurrencyId FK. Add Cheque entity. Add auto-journal entry generation. Enhance CashTransaction with PaymentMethod. | Enhancement |
| 💳 | **Customer/Supplier Payments** | Multi-invoice payment distribution. Enhanced payment methods (Cash/Cheque/Transfer/Credit Card). Receipt/Payment voucher printing. Balance clearing automation. | Enhancement |
| 🔒 | **Daily Closure** | Enhanced closure with verification workflow. Actual cash count vs system balance comparison. Forced closure if no discrepancies. | Enhancement |

### Data Flow

```text
Desktop (Payment Screen)
  → (HttpClient) → API (Payments/Receipts/CashBox)
    → Application (PaymentService, CashBoxService, ChequeService)
      → Domain (CashBox, CustomerPayment, SupplierPayment, Cheque, DailyClosure)
        → Infrastructure (EF Core + IUnitOfWork)
          → SQL Server

Transactions:
  Step 1: Validate balances BEFORE transaction
  Step 2: BeginTransactionAsync
  Step 3: Create payment entity (CustomerPayment / SupplierPayment)
  Step 4: Record CashTransaction (CashBox.Deposit/Withdraw)
  Step 5: Update Customer/Supplier balance
  Step 6: Create JournalEntry (debit CashBox account, credit Customer/Supplier account)
  Step 7: If Cheque — create Cheque entry with status "UnderCollection"
  Step 8: Commit transaction
```

---

## 2. Full Inventory — What Already Exists

### 2.1 Domain Entities ✅ (6 entities exist)

| Entity | File | Status |
|--------|------|--------|
| `CashBox` | `Domain/Entities/CashBox.cs` | ✅ Exists (131 lines) |
| `CashTransaction` | `Domain/Entities/CashTransaction.cs` | ✅ Exists (60 lines) |
| `CustomerPayment` | `Domain/Entities/CustomerPayment.cs` | ✅ Exists (68 lines) |
| `SupplierPayment` | `Domain/Entities/SupplierPayment.cs` | ✅ Exists (68 lines) |
| `DailyClosure` | `Domain/Entities/DailyClosure.cs` | ✅ Exists (60 lines) |
| — | `Cheque` entity | **❌ NEW — must create** |

### 2.2 Domain Enums ✅ (match AGENTS.md Section 3)

| Enum | File | Status |
|------|------|--------|
| `CashTransactionType` | `Domain/Enums/CashTransactionType.cs` | ✅ Values: OpeningBalance=1, SalesIncome=2, Expense=3, TransferOut=4, TransferIn=5, RefundOut=6, SupplierPayment=7, CustomerPayment=8 |
| `DeleteStrategy` | `Domain/Enums/DeleteStrategy.cs` | ✅ Cancel=0, Deactivate=1, Permanent=2 |

**⚠️ NEW enum needed**: `PaymentMethod` (enhanced): Cash=1, Cheque=2, BankTransfer=3, CreditCard=4
**⚠️ NEW enum needed**: `ChequeStatus`: UnderCollection=1, Deposited=2, Cleared=3, Bounced=4, Cancelled=5

### 2.3 Application Services ✅ (2 services exist)

| Interface | Implementation | Status |
|-----------|---------------|--------|
| `ICashBoxService` | `CashBoxService.cs` | ✅ 20 methods, 409 lines |
| `IPaymentService` | `PaymentService.cs` | ✅ 20 methods, 420 lines |
| — | `IChequeService` / `ChequeService` | **❌ NEW — must create** |

### 2.4 Infrastructure Layer ✅ (partially)

| Component | Status |
|-----------|--------|
| `CashBoxConfiguration` | ✅ Exists (Fluent API, Restrict FK) |
| `CashTransactionConfiguration` | ❌ Needs check — likely exists or needs creation |
| `DailyClosureConfiguration` | ❌ Needs check |
| `CustomerPaymentConfiguration` | ❌ Needs check |
| `SupplierPaymentConfiguration` | ❌ Needs check |
| `ChequeConfiguration` | **❌ NEW — must create** |

### 2.5 API Controllers ✅ (2 controllers exist)

| Controller | Route | Status |
|------------|-------|--------|
| `CashBoxesController` | `/api/v1/cash-boxes` | ✅ Likely exists |
| `CustomerPaymentsController` | `/api/v1/customer-payments` | ✅ 6 endpoints, 111 lines |
| `SupplierPaymentsController` | `/api/v1/supplier-payments` | ✅ 6 endpoints, 111 lines |
| — `ChequesController` | `/api/v1/cheques` | **❌ NEW — must create** |
| — `ReceiptVoucherController` | `/api/v1/receipts` | **❌ NEW — must create** |

### 2.6 Contracts Layer ✅ (partially)

| DTO | File | Status |
|-----|------|--------|
| `CashBoxDto` | `Responses/CashBoxDto.cs` | ✅ 9 fields |
| `CashTransactionDto` | `Responses/CashTransactionDto.cs` | ✅ 11 fields + `TransactionTypeName` |
| `DailyClosureDto` | `Responses/DailyClosureDto.cs` | ✅ 8 fields |
| `CustomerPaymentDto` | `DTOs/AllDtos.cs` | ✅ 9 fields |
| `SupplierPaymentDto` | `DTOs/AllDtos.cs` | ✅ 9 fields |
| `CustomerPaymentResponse` | `Responses/PaymentResponses.cs` | ✅ 8 fields |
| `SupplierPaymentResponse` | `Responses/PaymentResponses.cs` | ✅ 8 fields |
| — `ChequeDto` | **❌ NEW** | ❌ |
| — `ReceiptVoucherDto` | **❌ NEW** | ❌ |

| Request | File | Status |
|---------|------|--------|
| `CreateCashBoxRequest` | `Requests/CreateCashBoxRequest.cs` | ✅ 5 fields — **needs AccountId + CurrencyId** |
| `AddCashTransactionRequest` | `Requests/AddCashTransactionRequest.cs` | ✅ Amount + Notes |
| `CashTransferRequest` | `Requests/CashTransferRequest.cs` | ✅ Source/Dest/Amount/Notes |
| `CreateCustomerPaymentRequest` | `Requests/CreatePaymentRequest.cs` | ✅ 6 fields — **needs CashBoxId, multi-invoice distribution** |
| `CreateSupplierPaymentRequest` | `Requests/CreatePaymentRequest.cs` | ✅ 6 fields — **needs CashBoxId, multi-invoice distribution** |
| `UpdateCustomerPaymentRequest` | `Requests/UpdateRequests.cs` | ✅ 5 fields |
| `UpdateSupplierPaymentRequest` | `Requests/UpdateRequests.cs` | ✅ 5 fields |

### 2.7 Desktop Layer ✅ (partially)

| ViewModel | File | Status |
|-----------|------|--------|
| CashBox List/Editor VMs | `ViewModels/CashBoxes/` | ✅ Exists |
| `CustomerPaymentsListViewModel` | `ViewModels/Payments/` | ✅ Exists |
| `CustomerPaymentEditorViewModel` | `ViewModels/Payments/` | ✅ Exists — **needs CashBoxId + multi-invoice** |
| `SupplierPaymentsListViewModel` | `ViewModels/Payments/` | ✅ Exists |
| `SupplierPaymentEditorViewModel` | `ViewModels/Payments/` | ✅ Exists — **needs CashBoxId + multi-invoice** |
| — Cheque List/Editor VMs | **❌ NEW** | ❌ |
| — ReceiptVoucher Print VM | **❌ NEW** | ❌ |

| XAML View | File | Status |
|-----------|------|--------|
| CashBox Views | `Views/CashBoxes/` | ✅ Exists |
| CustomerPayment List/Editor Views | `Views/Payments/` | ✅ Exists |
| SupplierPayment List/Editor Views | `Views/Payments/` | ✅ Exists |
| — Cheque Views | **❌ NEW** | ❌ |
| — ReceiptVoucher View | **❌ NEW** | ❌ |

### 2.8 IUnitOfWork Repository Methods

| Repository | Status |
|-----------|--------|
| `_uow.CashBoxes` | ✅ Exists |
| `_uow.CashTransactions` | ✅ Exists |
| `_uow.CustomerPayments` | ✅ Exists |
| `_uow.SupplierPayments` | ✅ Exists |
| `_uow.DailyClosures` | ✅ Exists |
| `_uow.Cheques` | **❌ NEW — must add** |

---

## 3. BLOCKER Resolution — Critical Fixes

These issues must be resolved **before** any Phase 29 implementation begins.

### 3.1 Blocker 1: CashBox Missing AccountId FK

**Problem**: `CashBox` entity has no `AccountId` FK to link to the Accounts table. This means:
- Auto-journal entries from cash transactions cannot reference the correct account
- CashBox is operationally isolated from the chart of accounts
- RULE-079 (invoice payment references CashBoxId) works, but RULE-077/RULE-078 (accounting linkage) is broken

**Root cause**: The system does not yet have a proper `Account` entity (planned for Phase 28 — Ledger/Chart of Accounts). However, per Analysis Part 3 (lines 365-396), `CashBox` MUST have `AccountId` to be linked to the chart of accounts.

**Fix**: Add `int? AccountId` nullable FK to `CashBox`. Nullable because:
- Existing CashBoxes have no Account reference
- Accounts module (Phase 28) may not be completed yet
- Once Accounts exist, CashBox must link to an Account

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for table definitions.

**Files changed**: `CashBox.cs`, `CashBoxConfiguration.cs`, `CashBoxDto.cs`, `CreateCashBoxRequest.cs`, `CashBoxService.cs`, migration

### 3.2 Blocker 2: CashBox Missing CurrencyId FK

**Problem**: `CashBox` stores `CurrencyCode` as a string (e.g., "SAR") instead of `CurrencyId` FK linking to a `Currency` table. This means:
- Per Analysis Part 1 (lines 344-414), each CashBox should be tied to a specific currency
- Multi-currency cash flow cannot be properly tracked
- Exchange rate conversions cannot reference the correct currency pair

**Root cause**: Currency entity does not yet exist in the system (planned for Phase 30 — Multi-Currency Module).

**Fix**: Add `int? CurrencyId` nullable FK to `CashBox`. Keep `CurrencyCode` as denormalized string for quick display.

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for table definitions.

**Files changed**: `CashBox.cs`, `CashBoxConfiguration.cs`, `CashBoxDto.cs`, `CreateCashBoxRequest.cs`, `CashBoxService.cs`, migration

### 3.3 Blocker 3: Account Entity Dependency

**Problem**: Auto-journal entry generation requires an `Account` entity to exist. The system currently has no `Account` entity or `JournalEntry` entity — these are planned for Phase 28 (Ledger/Chart of Accounts).

**Risk**: If Phase 28 is not complete, Phase 29 must defer auto-journal entry generation or implement a simplified version.

**Decision**: **Defer auto-journal entry generation** to Phase 30 (Accounting Integration). Phase 29 will:
1. Add `AccountId` FK to CashBox (forward-looking — nullable)
2. Add `JournalEntryId` to `CashTransaction` (forward-looking — nullable int?)
3. Implement the `IAutoJournalService` interface with a **stub** that logs the intended entry
4. Full auto-journal generation implemented when Phase 28 (Accounts) + Phase 30 (Accounting Integration) are complete

> See `docs/AGENTS.md` for domain entity patterns and `docs/CONSTITUTION.md` for the Result<T> pattern and service stub patterns.

**Files changed**: `CashTransaction.cs`, `CashTransactionConfiguration.cs`, `CashTransactionDto.cs`, create `IAutoJournalService.cs` + stub

---

## 4. Design Catalog

### 4.1 CashBox (Extended)

| Field | Type | Default | Required | Current | New/Modify |
|-------|------|---------|----------|---------|------------|
| `Id` | `int PK` | Auto | ✅ | ✅ | — |
| `BoxName` | `nvarchar(100)` | — | ✅ | `BoxName` | — |
| `OpeningBalance` | `decimal(18,2)` | `0` | ✅ | ✅ | — |
| `CurrentBalance` | `decimal(18,2)` | `0` | ✅ | ✅ | — |
| `BranchId` | `int?` | `null` | ❌ | ✅ | — |
| `CurrencyCode` | `nvarchar(10)` | `"SAR"` | ❌ | ✅ | Keep (denormalized) |
| `AssignedUserId` | `int?` | `null` | ❌ | ✅ | — |
| `Notes` | `nvarchar(500)?` | `null` | ❌ | ✅ | — |
| `AccountId` | `int?` | `null` | ❌ | **❌ MISSING** | **🔴 BLOCKER 1 — ADD** |
| `CurrencyId` | `int?` | `null` | ❌ | **❌ MISSING** | **🔴 BLOCKER 2 — ADD** |
| `IsDefault` | `bit` | `false` | ❌ | **❌ MISSING** | **➕ ADD** |
| `AllowNegativeBalance` | `bit` | `false` | ❌ | **❌ MISSING** | **➕ ADD** |
| `IsActive` | `bit` | `true` | ❌ | ✅ | — |

**Domain methods** (all exist): `Create()`, `Deposit()`, `Withdraw()`, `ValidateUserAccess()`, `UpdateName()`
**⚠️ NEW domain methods**: `SetDefault(bool isDefault)` — manages IsDefault flag. `ValidateSufficientBalance(decimal amount)` — throws `DomainException` if `amount > CurrentBalance` (RULE-080).

**🔴 AccountId FK — New Property (CRITICAL)**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for table definitions.

> **Note**: Required for auto-journal posting. Links the cash box to its Chart of Accounts entry.

**Fluent API** (add to `CashBoxConfiguration.cs`):
> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions.

### 4.2 CashTransaction (Extended)

| Field | Type | Default | Required | Current | New/Modify |
|-------|------|---------|----------|---------|------------|
| `Id` | `int PK` | Auto | ✅ | ✅ | — |
| `CashBoxId` | `int FK` | — | ✅ | ✅ | — |
| `TransactionDate` | `datetime2` | `UtcNow` | ✅ | Uses `CreatedAt` | **➕ ADD** separate `TransactionDate` |
| `TransactionType` | `tinyint` (enum) | — | ✅ | ✅ | — |
| `Amount` | `decimal(18,2)` | — | ✅ | ✅ | — |
| `BalanceBefore` | `decimal(18,2)` | — | ✅ | ✅ | — |
| `BalanceAfter` | `decimal(18,2)` | — | ✅ | ✅ | — |
| `PaymentMethod` | `tinyint` | `1` (Cash) | ✅ | **❌ MISSING** | **➕ ADD** — Cash=1, Cheque=2, BankTransfer=3, CreditCard=4 |
| `ReferenceType` | `nvarchar(50)?` | `null` | ❌ | ✅ | — |
| `ReferenceId` | `int?` | `null` | ❌ | ✅ | — |
| `JournalEntryId` | `int?` | `null` | ❌ | **❌ MISSING** | **➕ ADD** (forward-looking) |
| `Notes` | `nvarchar(500)?` | `null` | ❌ | ✅ | — |
| `CreatedAt` | `datetime2` | `UtcNow` | ✅ | ✅ | — |
| `CreatedByUserId` | `int?` | `null` | ❌ | ✅ | — |

**⚠️ NEW**: `CashTransaction.Create()` updated to accept `PaymentMethod` parameter.

**⚠️ Balance Validation (RULE-080)**:
Before creating expense, transfer-out, or refund `CashTransaction` entries, call `cashBox.ValidateSufficientBalance(amount)` to enforce `CashBox.CurrentBalance` NEVER goes negative. This is a pre-condition check before the domain `Withdraw()` method is invoked.

**🔴 IMMUTABILITY — RULE-082**:
> CashTransaction entries are **IMMUTABLE** once created:
> - ❌ No editing allowed — a transaction represents a completed financial event
> - ❌ No deletion allowed — removing a transaction would break the audit trail
> - ✅ To correct an error: create an **offsetting entry** (reverse transaction with opposite sign)
> - This is enforced by RULE-082: "Cash transactions are immutable once created — no editing, cancellation via offsetting entry"

### 4.3 CustomerPayment (Extended)

| Field | Type | Default | Required | Current | New/Modify |
|-------|------|---------|----------|---------|------------|
| `Id` | `int PK` | Auto | ✅ | ✅ | — |
| `PaymentNo` | `nvarchar(50)` | — | ✅ | ✅ | — |
| `CustomerId` | `int FK` | — | ✅ | ✅ | — |
| `CashBoxId` | `int FK` | — | ✅ | **❌ MISSING** | **➕ ADD** — link to CashBox |
| `PaymentDate` | `datetime2` | `UtcNow` | ✅ | ✅ | — |
| `Amount` | `decimal(18,2)` | — | ✅ | ✅ | — |
| `PaymentMethod` | `tinyint` | `1` | ✅ | `byte PaymentMethod` | Change to use `PaymentMethod` enum instead of raw byte |
| `ReferenceNo` | `nvarchar(100)?` | `null` | ❌ | ✅ | — |
| `ChequeId` | `int?` | `null` | ❌ | **❌ MISSING** | **➕ ADD** — link to Cheque when PaymentMethod=Cheque |
| `Notes` | `nvarchar(500)?` | `null` | ❌ | ✅ | — |
| `IsActive` | `bit` | `true` | ❌ | ❌ | ✅ Via BaseEntity |
| `CreatedByUserId` | `int?` | `null` | ❌ | ✅ | — |

**Multi-invoice distribution** (NEW related table):

| Table | Field | Type |
|-------|-------|------|
| `PaymentInvoiceAllocation` | `Id` | `int PK` |
| | `PaymentType` | `nvarchar(20)` — "Customer" or "Supplier" |
| | `PaymentId` | `int` — FK to CustomerPayment or SupplierPayment |
| | `InvoiceType` | `nvarchar(20)` — "SalesInvoice" or "PurchaseInvoice" |
| | `InvoiceId` | `int` — FK to SalesInvoice or PurchaseInvoice |
| | `AllocatedAmount` | `decimal(18,2)` |
| | `CreatedAt` | `datetime2` |

### 4.4 SupplierPayment (Extended)

Same fields as CustomerPayment, but with `SupplierId` FK and `PurchaseInvoiceId` FK:

| Field | Type | Current | New/Modify |
|-------|------|---------|------------|
| `PurchaseInvoiceId` | `int?` | ✅ | Keep (single invoice reference) |
| `CashBoxId` | `int FK` | **❌ MISSING** | **➕ ADD** |
| `ChequeId` | `int?` | **❌ MISSING** | **➕ ADD** |
| `PaymentMethod` | `tinyint` | `byte PaymentMethod` | Change to `PaymentMethod` enum |

### 4.5 Cheque (NEW Entity)

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for the canonical table definition.

**ChequeStatus enum**:
> See `docs/AGENTS.md` §3 for canonical enum definitions.

### 4.6 PaymentMethod Enum (NEW — Enhanced)

> See `docs/AGENTS.md` §3 for canonical enum definitions.

**⚠️ IMPORTANT**: The existing `PaymentType` enum (`Cash=1, Credit=2, Mixed=3`) is used for **invoice payment type** (cash vs credit sale). This is DIFFERENT from `PaymentMethod` (how the payment is tendered). Do NOT conflate them.

### 4.7 DailyClosure (Extended)

| Field | Type | Current | New/Modify |
|-------|------|---------|------------|
| `Id` | `int PK` | ✅ | — |
| `CashBoxId` | `int FK` | ✅ | — |
| `ClosureDate` | `date` | ✅ | — |
| `OpeningBalance` | `decimal(18,2)` | ✅ | — |
| `TotalIncome` | `decimal(18,2)` | ✅ | — |
| `TotalExpense` | `decimal(18,2)` | ✅ | — |
| `ClosingBalance` | `decimal(18,2)` | ✅ | — |
| `ActualCashCount` | `decimal(18,2)?` | **❌ MISSING** | **➕ ADD** — actual cash counted |
| `Difference` | `decimal(18,2)?` | **❌ MISSING** | **➕ ADD** — computed: Actual - Closing |
| `DifferenceReason` | `nvarchar(500)?` | **❌ MISSING** | **➕ ADD** — reason for difference |
| `IsVerified` | `bit` | **❌ MISSING** | **➕ ADD** — verified by supervisor |
| `VerifiedByUserId` | `int?` | **❌ MISSING** | **➕ ADD** — who verified |
| `Status` | `tinyint` | **❌ MISSING** | **➕ ADD** — Open=1, Closed=2, Verified=3, Discrepancy=4 |
| `ClosedByUserId` | `int` | ✅ | — |

**DailyClosureStatus enum**:
> See `docs/AGENTS.md` §3 for canonical enum definitions.

**🔴 New Fields — ActualCashCount & Difference**:
> See `docs/AGENTS.md` for domain entity patterns (private set, domain methods).

**Fluent API** (add to `DailyClosureConfiguration.cs`):
> See `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions.

> **Both use `HasPrecision(18, 2)` per RULE-001.

### 4.8 PaymentInvoiceAllocation (NEW Entity)

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for table definitions.

**Constraint**: Sum of `AllocatedAmount` for a payment must equal the payment's total `Amount`.

---

## 5. Gap Analysis

### 5.1 CashBox Entity

| Feature | Status | Action |
|---------|--------|--------|
| `AccountId` FK | ❌ MISSING | **BLOCKER 1** — Add nullable FK (forward-looking) |
| `CurrencyId` FK | ❌ MISSING | **BLOCKER 2** — Add nullable FK (forward-looking) |
| `IsDefault` flag | ❌ MISSING | Add boolean field |
| `AllowNegativeBalance` | ❌ MISSING | Add boolean field |
| `AccountId` on `CreateCashBoxRequest` | ❌ MISSING | Add optional field |
| `CurrencyId` on `CreateCashBoxRequest` | ❌ MISSING | Add optional field |

### 5.2 CashTransaction Entity

| Feature | Status | Action |
|---------|--------|--------|
| `TransactionDate` field | ❌ MISSING | Uses `CreatedAt` — add separate `TransactionDate` |
| `PaymentMethod` field | ❌ MISSING | Add `PaymentMethod` enum field |
| `JournalEntryId` field | ❌ MISSING | Add nullable FK (forward-looking for Phase 30) |
| `PaymentMethod` on `CashTransaction.Create()` | ❌ MISSING | Update factory method |

### 5.3 CustomerPayment / SupplierPayment

| Feature | Status | Action |
|---------|--------|--------|
| `CashBoxId` FK | ❌ MISSING | Add to both entities |
| `ChequeId` FK | ❌ MISSING | Add nullable FK |
| Multi-invoice distribution | ❌ MISSING | Create `PaymentInvoiceAllocation` table |
| `PaymentMethod` enum (enhanced) | ❌ Using raw `byte` | Change to `PaymentMethod` enum |
| `CashBoxId` on request DTOs | ❌ MISSING | Add to `CreateCustomerPaymentRequest`, `CreateSupplierPaymentRequest` |
| `PaymentMethod` values for Cheque/Transfer/Credit | ❌ MISSING | Extend enum values |
| Voucher printing | ❌ MISSING | Add print DTO + print service methods |
| Balance clearing automation | ❌ MISSING | Add service method to auto-assign payment to oldest invoices |

### 5.4 Cheque (NEW Entity)

| Feature | Status | Action |
|---------|--------|--------|
| Cheque entity | ❌ NONE | **Create from scratch** |
| Cheque domain methods | ❌ NONE | Create: Create, Deposit, MarkAsCleared, MarkAsBounced, Cancel |
| Cheque configuration | ❌ NONE | Create Fluent API config |
| Cheque service | ❌ NONE | Create with CRUD + lifecycle methods |
| Cheque controller | ❌ NONE | Create 6+ endpoints |
| Cheque DTOs | ❌ NONE | Create |
| Cheque Desktop ViewModels + Views | ❌ NONE | Create List + Editor screens |

### 5.5 DailyClosure

| Feature | Status | Action |
|--------|--------|--------|
| `ActualCashCount` field | ❌ MISSING | Add decimal field |
| `Difference` computed field | ❌ MISSING | Add computed field |
| `IsVerified` / `VerifiedByUserId` | ❌ MISSING | Add verification workflow |
| `Status` field | ❌ MISSING | Add DailyClosureStatus enum |
| Verification workflow | ❌ MISSING | Add VerifyClosureAsync method |
| Discrepancy handling | ❌ MISSING | Allow supervisor to approve/reject discrepancies |

### 5.6 PaymentInvoiceAllocation (NEW)

| Feature | Status | Action |
|---------|--------|--------|
| Entity | ❌ NONE | Create |
| Configuration | ❌ NONE | Create Fluent API |
| Service methods | ❌ NONE | Create — `AllocatePaymentAsync`, `GetAllocationsAsync` |
| Validation | ❌ NONE | Sum of allocations = payment amount |
| Desktop UI | ❌ NONE | Add allocation grid to payment editor |

### 5.7 Voucher Printing

| Feature | Status | Action |
|---------|--------|--------|
| Receipt Voucher print DTO | ❌ NONE | Create `ReceiptVoucherPrintDto` |
| Payment Voucher print DTO | ❌ NONE | Create `PaymentVoucherPrintDto` |
| Voucher QuestPDF document | ❌ NONE | Create `ReceiptVoucherDocument.cs`, `PaymentVoucherDocument.cs` |
| Print endpoint | ❌ NONE | Add to `PrintController` |
| Desktop preview | ❌ NONE | Add preview + print button in payment editor |

---

## 6. Architectural Decisions

### 6.1 CashBox-Account Link Strategy

**Problem**: CashBox needs to be linked to an Account for auto-journal entries, but the `Account` entity does not exist yet (Phase 28 — Ledger/Chart of Accounts).

**Options**:
1. **Wait for Phase 28** — Defer CashBox enhancements entirely
2. **Add nullable FK now** — Add `AccountId` as nullable FK that will reference `Accounts` table when created
3. **Use string account reference** — Store account code as string temporarily

**Decision**: **Option 2 — Add nullable FK now (forward-looking)**. 
- Add `int? AccountId` to `CashBox` entity with no FK constraint initially (accounts table doesn't exist)
- When Phase 28 creates the `Accounts` table, a migration will add the FK constraint
- This unblocks Phase 29 development without waiting for Phase 28

### 6.2 CashBox-Currency Link Strategy

**Problem**: Same as AccountId — `Currency` entity does not exist (Phase 30 — Multi-Currency).

**Decision**: **Add nullable FK now**.
- Add `int? CurrencyId` to `CashBox` entity with no FK constraint initially
- Keep `CurrencyCode` as denormalized string for display
- When Phase 30 creates the `Currencies` table, a migration will add the FK constraint

### 6.3 Cheque Lifecycle

```text
Issued/Received → UnderCollection → Deposited → Cleared
                                      ↓
                                  Bounced → UnderCollection (re-deposit) or Cancelled
```

**States**:
- `UnderCollection` (1) — Cheque received/issued, not yet deposited
- `Deposited` (2) — Cheque deposited to bank
- `Cleared` (3) — Cheque cleared by bank (final success state)
- `Bounced` (4) — Cheque returned unpaid (e.g., insufficient funds)
- `Cancelled` (5) — Cheque cancelled manually

**Business rules**:
- From Bounced: can move to UnderCollection (re-deposit) or Cancelled
- From Deposited: can move to Cleared or Bounced
- From UnderCollection: can move to Deposited or Cancelled
- Cleared and Cancelled are terminal states

**Auto-reversal on bounce**: When a cheque bounces:
1. Create reversal `CashTransaction` (opposite sign)
2. Reverse the customer/supplier balance impact
3. Update `Cheque.Status = Bounced`
4. Create `CashTransaction` of type `RefundOut` or appropriate type
5. Create reversal journal entry (when Phase 30 is active)

### 6.4 Multi-Invoice Payment Distribution Algorithm

When a payment is made without specifying a single invoice:
1. Collect all **open** invoices for the customer/supplier (debt > 0)
2. Sort by oldest due date first (FIFO aging)
3. Allocate payment amount to invoices in order until payment is fully distributed
4. Create `PaymentInvoiceAllocation` records for each invoice
5. Update each invoice's `PaidAmount` 

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

### 6.5 Auto Journal Entry — Deferred

Per Blocker 3 decision, auto-journal entry generation is deferred to Phase 30 (Accounting Integration). Phase 29 will:
1. Add `JournalEntryId` to `CashTransaction` (forward-looking)
2. Create `IAutoJournalService` stub with logging only
3. Full implementation when Phase 28 (Accounts) + Phase 30 (Accounting Integration) complete

**Intended journal entry structure** (for reference):
```text
// Customer Payment (receipt)
DR: CashBox.AccountId (increase asset)
CR: Customer.AccountId (decrease receivable)

// Supplier Payment (payment)
DR: Supplier.AccountId (decrease payable)
CR: CashBox.AccountId (decrease asset)
```

### 6.6 PaymentMethod vs PaymentType — Clarification

**Current confusion**: System has `PaymentType` enum (`Cash=1, Credit=2, Mixed=3`) used on invoices. CustomerPayment and SupplierPayment use `byte PaymentMethod`.

**Decision**: 
- **Keep** `PaymentType` for invoices (indicates cash sale vs credit sale)
- **Enhance** `PaymentMethod` for payments (indicates HOW payment is tendered): Cash, Cheque, BankTransfer, CreditCard
- CustomerPayment/SupplierPayment use `PaymentMethod` not `PaymentType`
- Create a new `PaymentMethod` enum in Domain/Enums/PaymentMethod.cs

### 6.7 CurrencyCode — Keep as Denormalized String

`CashBox.CurrencyCode` is kept as a denormalized string for quick display purposes, even after `CurrencyId` FK is added. This avoids joins in list queries while maintaining referential integrity through the FK.

---

## 7. Non-V1 Items (Deferred)

| Feature | Reason | Target Phase |
|---------|--------|--------------|
| Bank Reconciliation (matching bank statements) | Requires external bank integration + statement import | Phase 31 |
| Recurring Payments (scheduled payments) | Complex scheduling engine, not critical for V1 | Phase 32 |
| Online Payment Gateway (Mada, Visa, PayPal) | Requires third-party payment processor integration + PCI compliance | Phase 33 |
| Full Journal Entry auto-generation | Requires Account entity (Phase 28) + Journal Entry module (Phase 30) | Phase 30 |
| Cheque image attachment upload | Requires file storage service | Phase 34 |
| SMS/Email payment notifications | Requires notification service | Phase 32 |
| Multi-currency cashbox operations | Requires Currency entity (Phase 30) | Phase 30 |
| End-of-day email report with closure summary | Requires reporting module enhancement | Phase 31 |
| CashBox budgeting / spending limits | Advanced feature, not in V1 scope | Phase 33 |
| Petty cash fund replenishment workflow | Small business niche feature | Phase 32 |

---

## 8. Implementation Tasks

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), Arabic ToolTips (RULE-185-190), and UI Compact styles (RULE-262-274).

### Task 1 — BLOCKERS 1+2: Add AccountId & CurrencyId FK to CashBox

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/CashBox.cs` | Add `int? AccountId` + `Account? Account` nav property, `int? CurrencyId`, `bool IsDefault`, `bool AllowNegativeBalance` properties + `SetDefault()` method + `ValidateSufficientBalance()` guard method |
| `Infrastructure/Data/Configurations/CashBoxConfiguration.cs` | Add FK config for AccountId (`.HasOne(cb => cb.Account).WithMany().HasForeignKey(cb => cb.AccountId).OnDelete(DeleteBehavior.Restrict)`), CurrencyId. Add `IsDefault` with default `false`. Add `AllowNegativeBalance` with default `false` |
| `Contracts/Responses/CashBoxDto.cs` | Add `int? AccountId`, `int? CurrencyId`, `bool IsDefault`, `bool AllowNegativeBalance` |
| `Contracts/Requests/CreateCashBoxRequest.cs` | Add `int? AccountId`, `int? CurrencyId`, `bool IsDefault` |
| `Application/Services/CashBoxService.cs` | Update `CreateAsync()` to pass `AccountId`, `CurrencyId`, `IsDefault`. Update `MapToDto()` |
| `Domain/Entities/CashBox.cs` — `Create()` method | Add `accountId`, `currencyId`, `isDefault`, `allowNegativeBalance` params |
| `Infrastructure/Data/Migrations/` | New migration: `ALTER TABLE CashBoxes ADD AccountId int NULL, CurrencyId int NULL, IsDefault bit NOT NULL DEFAULT 0, AllowNegativeBalance bit NOT NULL DEFAULT 0` |

**Logging**: `Log.Information("Cash box created: {BoxName} with Account {AccountId}, Currency {CurrencyId}", ...)` (RULE-035)

**Estimate**: ~1 hour

---

### Task 2 — Enhance CashTransaction: TransactionDate + PaymentMethod + JournalEntryId

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/CashTransaction.cs` | Add `DateTime TransactionDate`, `PaymentMethod PaymentMethod`, `int? JournalEntryId`. Update `Create()` to accept `paymentMethod` and `transactionDate` |
| `Domain/Enums/PaymentMethod.cs` | **NEW** — Create enum: `Cash=1, Cheque=2, BankTransfer=3, CreditCard=4` |
| `Infrastructure/Data/Configurations/CashTransactionConfiguration.cs` | Add config for TransactionDate, PaymentMethod, JournalEntryId. `HasPrecision(18,2)` on Amount fields |
| `Contracts/Responses/CashTransactionDto.cs` | Add `DateTime TransactionDate`, `byte PaymentMethod`, `int? JournalEntryId` + computed `PaymentMethodName` |
| `Application/Services/CashBoxService.cs` | Update `Deposit()`/`Withdraw()` internal calls to pass PaymentMethod. Update `RecordInvoicePaymentAsync()` to accept PaymentMethod param |
| `Application/Interfaces/Services/ICashBoxService.cs` | Update `RecordInvoicePaymentAsync()` signature to include `PaymentMethod paymentMethod` |
| `Contracts/Requests/AddCashTransactionRequest.cs` | Add `byte PaymentMethod = 1` (default Cash) |
| `Infrastructure/Data/Migrations/` | New migration: ALTER TABLE |

**Domain method update**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods).

**Logging**: `Log.Information("CashTransaction {Id}: {Type} of {Amount} via {PaymentMethod} in box {CashBoxId}", ...)` (RULE-035)

**Estimate**: ~1.5 hours

---

### Task 3 — Cheque Entity + Domain + Configuration (Full Build — NEW)

#### 3.1 Domain Layer

**File**: `Domain/Entities/Cheque.cs`

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` for the canonical table definition.

**File**: `Domain/Enums/ChequeStatus.cs` — `UnderCollection=1, Deposited=2, Cleared=3, Bounced=4, Cancelled=5`

#### 3.2 Infrastructure Layer

**File**: `Infrastructure/Data/Configurations/ChequeConfiguration.cs`
- All FKs use `DeleteBehavior.Restrict` (RULE-214)
- `ChequeNumber` has index (not unique — multiple cheques can have same number from different banks)
- `Amount` has `HasPrecision(18, 2)`
- `ChequeNumber`, `BankName` have `HasMaxLength(100)`

#### 3.3 Application Layer

**File**: `Application/Interfaces/Services/IChequeService.cs`
> See `docs/AGENTS.md` for service layer patterns and `docs/CONSTITUTION.md` for the Result<T> pattern.

**File**: `Application/Services/ChequeService.cs`
- Uses `IUnitOfWork` (RULE-024)
- Returns `Result<T>` (RULE-006)
- All lifecycle methods have status transition validation
- Bounce reversal: creates reversal `CashTransaction` + balance reversal
- **Logging**: `Log.Information("Cheque {Id} {Status}: {ChequeNumber}, {Amount}", ...)` (RULE-035)
- **Error handling**: `LogSystemError()` pattern (RULE-199), `DbUpdateException` catch (RULE-200)

#### 3.4 Add to IUnitOfWork

**File**: `Application/Interfaces/IUnitOfWork.cs`
> See `Application/Interfaces/IUnitOfWork.cs` for the canonical IUnitOfWork interface.

**Estimate**: ~4 hours

---

### Task 4 — Multi-Invoice Payment Distribution (PaymentInvoiceAllocation)

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/PaymentInvoiceAllocation.cs` | **NEW** — Create entity with PaymentType, PaymentId, InvoiceType, InvoiceId, AllocatedAmount |
| `Infrastructure/Data/Configurations/PaymentInvoiceAllocationConfiguration.cs` | **NEW** — Fluent API config, Restrict FKs |
| `Application/Services/PaymentService.cs` | Add `DistributePaymentAsync()` method. Update `CreateCustomerPaymentAsync()` and `CreateSupplierPaymentAsync()` to accept optional invoice allocations list |
| `Application/Interfaces/Services/IPaymentService.cs` | Add `DistributePaymentAsync()` to interface |
| `Contracts/DTOs/AllDtos.cs` | Add `PaymentInvoiceAllocationDto` |
| `Contracts/Requests/CreatePaymentRequest.cs` | Add optional `List<InvoiceAllocationRequest>? Allocations` to both Customer and Supplier payment requests |
| `Contracts/Requests/MiscRequests.cs` | Add `InvoiceAllocationRequest` record: `string InvoiceType, int InvoiceId, decimal AllocatedAmount` |
| `Application/Interfaces/IUnitOfWork.cs` | Add `IRepository<PaymentInvoiceAllocation> PaymentInvoiceAllocations` |
| `Infrastructure/Data/Migrations/` | New migration: CREATE TABLE `PaymentInvoiceAllocations` |

**Algorithm**:
> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Logging**: `Log.Information("Payment {PaymentId} distributed across {Count} invoices for {Type}", paymentId, allocations.Count, paymentType)`

**Estimate**: ~3 hours

---

### Task 5 — Add CashBoxId + ChequeId + Enhanced PaymentMethod to CustomerPayment & SupplierPayment

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/CustomerPayment.cs` | Add `int? CashBoxId`, `int? ChequeId`. Change `byte PaymentMethod` to `PaymentMethod PaymentMethod` |
| `Domain/Entities/SupplierPayment.cs` | Same changes |
| `Domain/Entities/CustomerPayment.cs` — `Create()` | Add `cashBoxId`, `chequeId`, `PaymentMethod paymentMethod` params |
| `Domain/Entities/SupplierPayment.cs` — `Create()` | Same |
| `Infrastructure/Data/Configurations/CustomerPaymentConfiguration.cs` | Add FK configs for CashBoxId, ChequeId (Restrict), change PaymentMethod config |
| `Infrastructure/Data/Configurations/SupplierPaymentConfiguration.cs` | Same |
| `Contracts/DTOs/AllDtos.cs` | Update `CustomerPaymentDto`, `SupplierPaymentDto` — add `CashBoxId`, `string? CashBoxName`, `ChequeId`, change `PaymentMethod` type |
| `Contracts/Requests/CreatePaymentRequest.cs` | Add `int? CashBoxId`, `int? ChequeId`, `PaymentMethod PaymentMethod = PaymentMethod.Cash` to both request types |
| `Contracts/Requests/UpdateRequests.cs` | Add `int? CashBoxId` to both update request types |
| `Application/Services/PaymentService.cs` | Update `CreateCustomerPaymentAsync()`, `CreateSupplierPaymentAsync()` to handle CashBoxId + ChequeId. After creating payment, record CashTransaction via `_cashBoxService.RecordInvoicePaymentAsync()` |
| `Application/Services/PaymentService.cs` — DI | Inject `ICashBoxService` |
| `Infrastructure/Data/Migrations/` | New migration: ALTER TABLE |

**Logging**: `Log.Information("Customer payment {PaymentNo}: CashBox {CashBoxId}, Method {PaymentMethod}, Cheque {ChequeId}", ...)`

**Estimate**: ~2.5 hours

---

### Task 6 — Enhanced Daily Closure with Verification Workflow

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/DailyClosure.cs` | Add `decimal ActualCashCount` + `decimal Difference` (computed: `ActualCashCount - ClosingBalance`), `string? DifferenceReason`, `bool IsVerified`, `int? VerifiedByUserId`, `DailyClosureStatus Status`. Both money fields use `HasPrecision(18, 2)`. Update `Create()` to accept new fields |
| `Domain/Enums/DailyClosureStatus.cs` | **NEW** — `Open=1, Closed=2, Verified=3, Discrepancy=4` |
| `Infrastructure/Data/Configurations/DailyClosureConfiguration.cs` | Add config for new fields: `ActualCashCount.HasPrecision(18, 2)`. `Difference` is computed (no DB column — C# only). `DifferenceReason.HasMaxLength(500)` |
| `Application/Services/CashBoxService.cs` | Update `PerformDailyClosureAsync()` to accept `actualCashCount`, compare with system balance. If mismatch, set Status=Discrepancy. Add `VerifyClosureAsync(int closureId, int userId, bool approve, string? reason)` method |
| `Application/Interfaces/Services/ICashBoxService.cs` | Add `VerifyClosureAsync()` method |
| `Contracts/Responses/DailyClosureDto.cs` | Add new fields: `ActualCashCount`, `Difference`, `IsVerified`, `Status` |
| `Contracts/Requests/MiscRequests.cs` | Add `VerifyClosureRequest(int ClosureId, bool Approve, string? Reason)` |
| `Infrastructure/Data/Migrations/` | New migration: ALTER TABLE |

**Closure workflow**:
> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

**Logging**: 
- `Log.Information("Daily closure for CashBox {Id}: Opening={Op}, Income={Inc}, Expense={Exp}, Closing={Cls}", ...)`
- `Log.Warning("Discrepancy detected: System={Sys}, Actual={Act}, Diff={Diff}", ...)` (RULE-183)
- `Log.Information("Daily closure {Id} verified by User {UserId}, Approved={Approve}", ...)`

**Estimate**: ~2 hours

---

### Task 7 — Receipt/Payment Voucher Printing

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `ReceiptVoucherPrintDto` and `PaymentVoucherPrintDto` records with: VoucherNumber, Date, Customer/Supplier Name, Amount (words + digits), PaymentMethod, CashBoxName, ChequeNumber, Notes, UserSignature fields |
| `Application/Printing/IReceiptVoucherPrintService.cs` | **NEW** — Interface for voucher printing: `PrintReceiptVoucherAsync(int paymentId)`, `PrintPaymentVoucherAsync(int paymentId)` |
| `Infrastructure/Printing/ReceiptVoucherDocument.cs` | **NEW** — QuestPDF A5 document for receipt voucher with: store header, "إيصال قبض" title, customer name, amount table, payment method, signature lines, store stamp |
| `Infrastructure/Printing/PaymentVoucherDocument.cs` | **NEW** — QuestPDF A5 document for payment voucher |
| `Application/Printing/ReceiptVoucherPrintService.cs` | **NEW** — Builds print DTO from payment + cashbox data, generates document |
| `Application/Interfaces/Services/IPaymentService.cs` | Add `GetReceiptVoucherDataAsync(int paymentId)` and `GetPaymentVoucherDataAsync(int paymentId)` |
| `Application/Services/PaymentService.cs` | Implement methods to build print DTOs |
| `Api/Controllers/PrintController.cs` | Add endpoints: `GET /api/v1/print/customer-payment/{id}/voucher`, `GET /api/v1/print/supplier-payment/{id}/voucher` |
| `Desktop/Services/Api/IPrintApiService.cs` | Add methods for voucher preview + print |
| `Desktop/Services/Api/PrintApiService.cs` | Implement HTTP methods for voucher print |
| Desktop payment editor Views | Add "طباعة إيصال قبض" button in CustomerPaymentEditorView / "طباعة إيصال صرف" in SupplierPaymentEditorView |

**Voucher document structure** (A5 portrait):
```
┌─────────────────────────────────┐
│        [Store Logo/Name]         │
│       إيصال قبض / إيصال صرف      │
│                                  │
│  رقم الإيصال: CP-2026-000001    │
│  التاريخ: 05/06/2026            │
│                                  │
│  العميل/المورد: أحمد محمد       │
│  المبلغ: 5,000.00 ريال          │
│  المبلغ كتابة: خمسة آلاف ريال   │
│                                  │
│  طريقة الدفع: شيك / نقدي        │
│  الصندوق: الصندوق الرئيسي       │
│  رقم الشيك: 123456 - بنك الراجحي│
│                                  │
│  البيان: سداد فاتورة رقم 100    │
│                                  │
│  ─────────────────────────────   │
│  المستلم: ________   المدفوع: __ │
│  التوقيع: ________   التوقيع: __ │
│                                  │
│  [Store Stamp]                   │
└─────────────────────────────────┘
```

**Estimate**: ~4 hours

---

### Task 8 — IAutoJournalService Stub (Forward-Looking)

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/IAutoJournalService.cs` | **NEW** — Interface: `Task<Result> CreateJournalEntryForCashTransactionAsync(CashTransaction transaction, CancellationToken ct)` |
| `Application/Services/AutoJournalServiceStub.cs` | **NEW** — Stub implementation that logs intended entry, returns Success |
| `Application/Services/CashBoxService.cs` | Inject `IAutoJournalService`, call after every Deposit/Withdraw that has a reference |
| `Application/Services/PaymentService.cs` | Inject `IAutoJournalService`, call after payment creation |
| DI Registration (Program.cs) | Register stub: `services.AddScoped<IAutoJournalService, AutoJournalServiceStub>()` |

**Estimate**: ~30 minutes

---

### Task 9 — Enhanced FluentValidation

**Files**:

| File | Change |
|------|--------|
| `Api/Validators/CustomerPayment/CreateCustomerPaymentRequestValidator.cs` | **NEW** — Validate: Amount > 0, CustomerId > 0, CashBoxId optional, PaymentMethod valid (1-4), allocations sum = Amount |
| `Api/Validators/SupplierPayment/CreateSupplierPaymentRequestValidator.cs` | **NEW** — Same as above |
| `Api/Validators/CashBox/CreateCashBoxRequestValidator.cs` | **NEW** — BoxName required max 100, OpeningBalance >= 0 |
| `Api/Validators/Cheque/CreateChequeRequestValidator.cs` | **NEW** — ChequeNumber required, BankName required, Amount > 0, ChequeDate not in distant future |
| `Api/Validators/Cheque/DepositChequeRequestValidator.cs` | **NEW** — DepositDate required |
| `Api/Validators/Cheque/BounceChequeRequestValidator.cs` | **NEW** — Reason required |
| `Api/Validators/DailyClosure/VerifyClosureRequestValidator.cs` | **NEW** — Approve required, Reason required if not approved |

**Estimate**: ~1.5 hours

---

### Task 10 — Enhanced Desktop Screens

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Payments/CustomerPaymentEditorViewModel.cs` | Add CashBoxId dropdown, ChequeId field (visible when PaymentMethod=Cheque), multi-invoice allocation DataGrid. Add payment method RadioButtons. Add print button. |
| `Views/Payments/CustomerPaymentEditorView.xaml` | Add CashBoxId ComboBox, Cheque fields section (shown/hidden by PaymentMethod), allocation DataGrid, print button. Compact UI (RULE-262-274). Arabic ToolTips (RULE-185-190) |
| `ViewModels/Payments/SupplierPaymentEditorViewModel.cs` | Same enhancements |
| `Views/Payments/SupplierPaymentEditorView.xaml` | Same enhancements |
| `ViewModels/Payments/CustomerPaymentsListViewModel.cs` | Add PaymentMethod column, CashBoxName column, ChequeNumber column. Filter by PaymentMethod. |
| `Views/Payments/CustomerPaymentsListView.xaml` | Add columns, filter combobox. Compact widths. |
| `ViewModels/Payments/SupplierPaymentsListViewModel.cs` | Same | 
| `Views/Payments/SupplierPaymentsListView.xaml` | Same |
| `ViewModels/CashBoxes/CashBoxEditorViewModel.cs` | Add AccountId, CurrencyId, IsDefault fields (when entities exist) |
| `Views/CashBoxes/CashBoxEditorView.xaml` | Add fields with ToolTips |
| `ViewModels/Cheque/ChequesListViewModel.cs` | **NEW** — List with status filter, newest-first sort |
| `Views/Cheque/ChequesListView.xaml` | **NEW** — DataGrid with status colors, lifecycle action buttons |
| `ViewModels/Cheque/ChequeEditorViewModel.cs` | **NEW** — Editor with INotifyDataErrorInfo (RULE-228) |
| `Views/Cheque/ChequeEditorView.xaml` | **NEW** — Editor form, Save always enabled (RULE-059), compact styles |
| `Services/Api/CustomerPaymentApiService.cs` | Add methods for voucher print, allocation |
| `Services/Api/SupplierPaymentApiService.cs` | Same |
| `Services/Api/ChequeApiService.cs` | **NEW** — HTTP client with content-type guard (RULE-184) |
| `Messaging/Messages/AppMessages.cs` | Add `ChequeChangedMessage`, `PaymentChangedMessage` |
| `App.xaml.cs` | DI registrations for all new Cheque VMs + services, navigation entries |
| `MainWindow.xaml` | Add "الشيكات" navigation item, "إيصالات القبض" and "إيصالات الصرف" sub-items |

**ViewModel patterns** (RULE-141):
- All async commands wrapped in `ExecuteAsync()`
- Error messages via `LogSystemError()` (RULE-199) — NEVER `ex.Message` in user dialogs (RULE-171)
- Dialog titles screen-specific: `"خطأ في حفظ الشيك"`, `"خطأ في توزيع الدفعة"` (RULE-173)
- All user messages via `IDialogService` — NO `MessageBox.Show` (RULE-174)
- Async suffix on all dialog calls: `ShowErrorAsync` (RULE-175)

**UI Compact** (RULE-262-274):
- Button/TextBox heights: via style (28px default)
- Padding: `10,4` via style
- Header: `Padding="12,6"`, Footer: `Padding="12,8"`
- Section margins: `Margin="0,0,0,6"` between fields
- Dialog title font: `FontSize="16"`, section headers: `FontSize="14"`

**Arabic ToolTips** (RULE-185-190):
- CashBox dropdown: `"اختيار الصندوق الذي سيتم القبض/الصرف منه"`
- Cheque number field: `"رقم الشيك كما هو مدون على الشيك"`
- Payment method radio: `"اختيار طريقة الدفع — نقدي، شيك، تحويل بنكي، أو بطاقة ائتمان"`
- Allocate button: `"توزيع المبلغ على الفواتير المحددة"`
- Print voucher: `"طباعة إيصال قبض/صرف معتمد"`
- Deposit cheque: `"إيداع الشيك في البنك — تسجيل تاريخ الإيداع"`
- Bounce cheque: `"تسجيل ارتجاع الشيك — سيتم عكس الحركة المالية"`
- Verify closure: `"اعتماد الإغلاق اليومي — توثيق المراجعة"`

**Estimate**: ~8 hours

---

### Task 11 — API Endpoints Updates

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/CashBoxesController.cs` | Ensure existing endpoints. Add: `PUT /api/v1/cash-boxes/{id}/set-default` |
| `Api/Controllers/CustomerPaymentsController.cs` | Update `Create` endpoint to accept `CreateCustomerPaymentRequest` with new fields (CashBoxId, ChequeId, Allocations). Add `POST /api/v1/customer-payments/{id}/distribute` |
| `Api/Controllers/SupplierPaymentsController.cs` | Same updates |
| `Api/Controllers/ChequesController.cs` | **NEW**:

| Method | Endpoint | Policy |
|--------|----------|--------|
| GET | `/api/v1/cheques` | `ManagerAndAbove` |
| GET | `/api/v1/cheques/{id}` | `ManagerAndAbove` |
| POST | `/api/v1/cheques` | `ManagerAndAbove` |
| POST | `/api/v1/cheques/{id}/deposit` | `ManagerAndAbove` |
| POST | `/api/v1/cheques/{id}/clear` | `ManagerAndAbove` |
| POST | `/api/v1/cheques/{id}/bounce` | `ManagerAndAbove` |
| POST | `/api/v1/cheques/{id}/cancel` | `ManagerAndAbove` |

**Controller purity** (RULE-203): No controller injects `DbContext` or `IUnitOfWork` — only service interfaces.

**Estimate**: ~3 hours

---

### Task 12 — Reports Endpoints

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/ReportsController.cs` | Add report endpoints for:
  - `GET /api/v1/reports/cash-box-summary?from=&to=` — summary of all cash boxes
  - `GET /api/v1/reports/cheque-report?from=&to=&status=` — cheque aging/report
  - `GET /api/v1/reports/payment-summary?type=customer|supplier&from=&to=` — payment summary
  - `GET /api/v1/reports/daily-closure-report?cashBoxId=&from=&to=` — closure history |
| `Application/Services/ReportService.cs` | Add methods for cash box summary, cheque report, payment summary |
| `Contracts/DTOs/AllDtos.cs` | Add `CashBoxSummaryReportDto`, `ChequeReportDto`, `PaymentSummaryDto` |

**Report DTOs**:
> See `SalesSystem.Contracts/` for canonical DTO definitions.

**Estimate**: ~3 hours

---

### Task 17 — Self-Explanation ◉ Tooltips (Receipts & Payments Module)

**Objective**: Add ⓘ (InfoTooltip) controls next to key terms in the Receipts & Payments module UI. Each tooltip provides an Arabic explanation of the term using the same `InfoTooltip` UserControl pattern from Phase 18.

**Pattern**: `◉` icon with styled ToolTip bound to `HelpText` property.

**Where to add**: CashBox editor, Customer/Supplier Payment editors, Cheque editor, Daily Closure view — next to section headers, labels, and DataGrid column headers.

**ⓘ Terms table (Receipts & Payments)**:

| Term | Explanation (Arabic) |
|------|---------------------|
| سند قبض | "سند القبض هو مستند يثبت استلام الشركة مبلغاً مالياً من عميل أو جهة أخرى." |
| سند صرف | "سند الصرف هو مستند يثبت قيام الشركة بدفع مبلغ مالي لمورد أو لجهة أخرى." |
| الصندوق | "الصندوق هو المكان الفعلي الذي توجد فيه النقود. يمكن أن يكون صندوق نقدي أو حساب بنكي." |
| الإغلاق اليومي | "الإغلاق اليومي هو عملية مطابقة الرصيد النظري للنظام مع الرصيد الفعلي في الصندوق في نهاية اليوم." |
| الرصيد النظري | "الرصيد النظري = رصيد بداية اليوم + المقبوضات - المدفوعات. يحسب تلقائياً من العمليات." |
| الرصيد الفعلي | "الرصيد الفعلي هو المبلغ الموجود فعلياً في الصندوق بعد العد اليدوي. قد يختلف عن الرصيد النظري." |
| شيك | "الشيك هو أمر écrit لسحب مبلغ من البنك. يمر بعدة حالات: معلق، مودع، مقبوض، مرتجع." |
| توزيع المبلغ | "توزيع المبلغ يعني تقسيم قيمة سند القبض أو الصرف على عدة فواتير. مثلاً: دفع 500 ريال تغطي 3 فواتير مختلفة." |
| التحصيل | "التحصيل هو عملية استلام نقدية من العملاء مقابل فواتير سابقة." |
| الدفع | "الدفع هو عملية صرف نقدية للموردين مقابل فواتير سابقة." |

**Files**:

| File | Change |
|------|--------|
| `DesktopPWF/Controls/InfoTooltip.xaml` + `.cs` | ✅ Already exists (from Phase 18) — no changes needed |
| `DesktopPWF/Views/CashBoxes/CashBoxEditorView.xaml` | Add `ⓘ` tooltip next to: الصندوق |
| `DesktopPWF/Views/Payments/CustomerPaymentEditorView.xaml` | Add `ⓘ` tooltips next to: سند قبض, توزيع المبلغ, التحصيل |
| `DesktopPWF/Views/Payments/SupplierPaymentEditorView.xaml` | Add `ⓘ` tooltips next to: سند صرف, توزيع المبلغ, الدفع |
| `DesktopPWF/Views/Cheques/ChequeEditorView.xaml` | Add `ⓘ` tooltip next to: شيك |
| `DesktopPWF/Views/CashBoxes/DailyClosureView.xaml` | Add `ⓘ` tooltips next to: الإغلاق اليومي, الرصيد النظري, الرصيد الفعلي |

**XAML usage pattern**:
> See `DesktopPWF/Controls/InfoTooltip.xaml` for the canonical InfoTooltip pattern.

**Estimate**: ~1 hour

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | Cheque.Amount, PaymentInvoiceAllocation.AllocatedAmount, DailyClosure.ActualCashCount, all existing money fields | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields in this phase | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | Payment creation + CashTransaction + balance update + journal entry stub — wrapped in BeginTransactionAsync | ✅ |
| **RULE-005** | Stock deducted AFTER invoice saved | N/A — no stock operations in receipts/payments | ✅ N/A |
| **RULE-006** | ALL services return `Result<T>` | ChequeService, enhanced PaymentService, enhanced CashBoxService, AutoJournalServiceStub | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | ChequeNumber, BankName, BranchName, DrawerName, PayeeName, Notes, PaymentInvoiceAllocation fields | ✅ |
| **RULE-016** | BaseEntity audit fields | Cheque, PaymentInvoiceAllocation inherit BaseEntity | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | ChequeService, PaymentService, CashBoxService | ✅ |
| **RULE-035** | Serilog for logging | All services: Log.Information on CRUD + status changes + closures | ✅ |
| **RULE-036** | Log critical operations | Payment creation, cheque lifecycle events, daily closure, discrepancies, balance reversals | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — no secrets logged | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | All new + updated endpoints | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | Cheque entity: Create(), Deposit(), MarkAsCleared(), MarkAsBounced(), Cancel(). CashBox: existing methods. | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | All new validators: CreateCustomerPaymentRequest, CreateSupplierPaymentRequest, CreateCashBoxRequest, CreateChequeRequest, DepositChequeRequest, BounceChequeRequest, VerifyClosureRequest | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | Cheque cancellation via Cancel() domain method (soft). Payment deletion reverses balance (soft delete). | ✅ |
| **RULE-052** | Guard Clauses on all entities | Cheque.Create: ChequeNumber required, BankName required, Amount > 0, ChequeDate not future. PaymentInvoiceAllocation: AllocatedAmount > 0, PaymentType valid. | ✅ |
| **RULE-053** | DomainException in Arabic | All new guard clause messages in Arabic | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all new ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | ChequeEditorViewModel, enhanced Customer/Supplier PaymentEditorViewModels | ✅ |
| **RULE-059** | Save always enabled, validate on click | All editor ViewModels — no CanExecute blocking | ✅ |
| **RULE-077** | CashBox has OpeningBalance, CurrentBalance | Already implemented — verified in existing CashBox entity | ✅ |
| **RULE-078** | CashTransaction records ALL types | Already implemented — 8 transaction types, PaymentMethod added | ✅ |
| **RULE-079** | Every invoice payment references CashBoxId | **BLOCKER** — CashBoxId added to CustomerPayment and SupplierPayment in Task 5 | ✅ |
| **RULE-080** | CashBox.CurrentBalance NEVER negative | CashBox.Withdraw() already validates `CurrentBalance >= amount` | ✅ |
| **RULE-081** | Transfer requires TWO transactions (Out + In) | CashBoxService.TransferAsync() already creates both TransferOut and TransferIn | ✅ |
| **RULE-082** | Cash transactions immutable | CashTransaction.Create() is `internal` — only called from CashBox.Deposit()/Withdraw(). No public Update() method. | ✅ |
| **RULE-083** | DailyClosure computes: Opening+Income-Expense=Closing | DailyClosure.Create() validates `computedClosing == closingBalance` | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | All ViewModels in Tasks 10, 12 | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern everywhere | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | Cheque editor, enhanced payment editors open via `OpenScreen()` | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern in all VMs | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في حفظ الشيك"`, `"خطأ في توزيع الدفعة"`, `"خطأ في الإغلاق اليومي"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync`, `ShowConfirmationAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable, JSON parse crashes | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Cheque bounce, closure discrepancy, validation errors, insufficient balance | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | ChequeApiService, enhanced payment API services | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, inputs across all new/enhanced XAML views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "إيداع الشيك في البنك" ✅, not just "إيداع" ❌ | ✅ |
| **RULE-187** | Action buttons explain consequences | Bounce: "تسجيل ارتجاع الشيك — سيتم عكس الحركة المالية والرصيد" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "الشيكات — إدارة الشيكات الواردة والصادرة" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ إضافة شيك جديد — تسجيل شيك وارد أو صادر" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use LogSystemError() — never direct Serilog.Log.Error | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException → Result.Failure | Cheque permanent delete (if implemented) catches FK violation | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | ChequeService, enhanced PaymentService, CashBoxService | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | ChequesController, enhanced Customer/Supplier Payments controllers — service only | ✅ |
| **RULE-210** | CHECK constraints at DB level | `CHK_DailyClosures_Difference` (computed), `CHK_Cheques_Amount_Positive` | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | Cheque FK to Customer/Supplier/User, CashTransaction FK to CashBox, PaymentInvoiceAllocation FKs — ALL Restrict | ✅ |
| **RULE-220** | Newest-first sorting on lists | ChequesListViewModel: OrderByDescending(Id). Payment lists: OrderByDescending(PaymentDate) | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | ChequeEditorViewModel, enhanced Customer/Supplier PaymentEditorViewModels | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | All editor ViewModels | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation in all editors | ✅ |
| **RULE-240** | Login endpoint rate limited | Not affected by this phase | ✅ N/A |
| **RULE-246** | Users soft-deleted only | Not affected | ✅ N/A |
| **RULE-254** | InvoiceNo as int, NOT string | Not affected | ✅ N/A |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | All new XAML: compact 28px via styles | ✅ |
| **RULE-263** | No hardcoded Padding="16+" on buttons | All new XAML: 10,4 via styles | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | All new XAML views | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Between form fields | ✅ |
| **RULE-266** | Dialog titles FontSize=16 max | All dialog windows | ✅ |
| **RULE-267** | Section headers FontSize=14 max | All section headers | ✅ |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | All empty-state views | ✅ |
| **RULE-269** | MainWindow sidebar Width=200 | Already set | ✅ N/A |
| **RULE-270** | Dialog icons: 44×44 max | All dialog windows | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | All screen windows | ✅ |
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | All dialogs | ✅ |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | All new XAML uses styles only | ✅ |

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Account entity doesn't exist yet (Blocker 1)** | **HIGH** — CashBox-Account link can't be enforced | Add nullable FK now, defer FK constraint to Phase 28 migration |
| **Currency entity doesn't exist yet (Blocker 2)** | **HIGH** — CashBox-Currency link can't be enforced | Same strategy as Account — nullable FK now, constraint later |
| **Cheque bounce auto-reversal complex** | Medium | Implement reversal as separate transaction in same scope — test thoroughly |
| **Multi-invoice allocation validation errors** | Medium | Validation: sum of allocations must equal payment amount. UI must warn on mismatch. |
| **PaymentMethod vs PaymentType confusion** | Low | Clear documentation in code comments. PaymentType = invoice type (cash/credit). PaymentMethod = how tendered (cash/cheque/transfer/card). |
| **Daily closure discrepancy resolution** | Medium | Supervisor verification workflow handles discrepancies. Discrepancy without approval = forced difference reason required. |
| **Migration conflicts with existing CashBox records** | Low | All new CashBox fields are nullable with defaults — no breaking changes |
| **Voucher printing template not approved** | Low | Template designed in consultation with user requirements (Analysis Part 1 sections 143-241: سندات القبض والصرف) |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| Cheque entity not needed | `DROP TABLE Cheques;` Remove all Cheque files |
| PaymentInvoiceAllocation not needed | `DROP TABLE PaymentInvoiceAllocations;` Remove entity, revert PaymentService |
| CashBox AccountId/CurrencyId cause issues | `ALTER TABLE CashBoxes DROP COLUMN AccountId, CurrencyId, IsDefault, AllowNegativeBalance;` Keep fields in entity as nullable with no FK — no data loss |
| Enhanced Daily Closure not needed | `ALTER TABLE DailyClosures DROP COLUMN ActualCashCount, Difference, DifferenceReason, IsVerified, VerifiedByUserId, Status;` |
| Enhanced PaymentMethod causing issues | Keep byte field, map PaymentMethod enum to byte at boundary |
| Voucher printing not needed | Remove print endpoints, revert PaymentService print methods — no data impact |
| Multi-invoice allocation not needed | Remove allocation grid from UI — payments still work with single invoice |
| AutoJournalServiceStub causing issues | Remove DI registration — logging stub has no side effects |
| All Phase 29 changes need revert | `git revert` migration commit + remove all new files + revert entity changes |

---

## 12. Unit Test Tasks

### T17 — Domain Entity Tests (Receipts & Payments)

| ID | Test | Expected |
|----|------|----------|
| T17.01 | `CashBox.Create()` with valid args → OpeningBalance, CurrentBalance, AccountId set | No exception |
| T17.02 | `CashBox.Create()` with negative OpeningBalance → `DomainException("الرصيد الافتتاحي لا يمكن أن يكون سالباً")` | Arabic message |
| T17.03 | `CashBox.Create()` with empty name → `DomainException("اسم الصندوق مطلوب")` | Arabic message |
| T17.04 | `CashBox.ValidateSufficientBalance(amount)` when CurrentBalance >= amount → no exception | Passes |
| T17.05 | `CashBox.ValidateSufficientBalance(amount)` when CurrentBalance < amount → `DomainException("الرصيد غير كافٍ")` | Arabic message |
| T17.06 | `CashTransaction.Create()` with valid args → TransactionType, Amount, CashBoxId set | No exception |
| T17.07 | `CashTransaction.Create()` with amount ≤ 0 → `DomainException("المبلغ يجب أن يكون أكبر من صفر")` | Arabic message |
| T17.08 | `CashTransaction` property setters not publicly accessible → immutability enforced | No public setters |
| T17.09 | `DailyClosure.Create()` → OpeningBalance, TotalIncome, TotalExpense, ClosingBalance set | No exception |
| T17.10 | `DailyClosure.Difference` = `ActualCashCount - ClosingBalance` | Correct decimal math |
| T17.11 | `DailyClosure` with Difference ≠ 0 → Status = Discrepancy | Property check |
| T17.12 | `Cheque.Create()` → Pending status, Amount, ChequeNumber, BankName set | No exception |
| T17.13 | `Cheque.Deposit()` → status Deposited, DepositDate set | Property check |
| T17.14 | `Cheque.Clear()` → status Cleared, ClearanceDate set | Property check |
| T17.15 | `Cheque.Bounce()` → status Bounced, BounceReason set | Property check |
| T17.16 | `Cheque.Deposit()` when already Bounced → `DomainException` | Arabic message |
| T17.17 | `CustomerPayment.Create()` with PaymentType.Cash → PaymentMethod set, ReferenceNumber optional | No exception |
| T17.18 | `SupplierPayment.Create()` with WithholdingTax → TaxAmount calculated correctly | TaxAmount = Amount × TaxRate% |
| T17.19 | `CustomerPayment.PaidAmount > TotalAmount` → `DomainException("المبلغ المدفوع أكبر من الإجمالي")` | Arabic message |
| T17.20 | `CashTransactionType` enum values match Section 3 (1–8) | All 8 values verified |
| T17.21 | `DailyClosure` status values: Open=1, Closed=2, Verified=3, Discrepancy=4 | All 4 values verified |

### T18 — Service Tests (Receipts & Payments, Mock IUnitOfWork)

| ID | Test | Expected |
|----|------|----------|
| T18.01 | `CashBoxService.CreateAsync()` with valid request → `Result<CashBoxDto>.Success` | IsSuccess = true |
| T18.02 | `CashBoxService.CreateAsync()` duplicate name → `Result.Failure("اسم الصندوق موجود مسبقاً")` | IsSuccess = false |
| T18.03 | `CashTransactionService.CreateIncomeAsync()` → CashTransaction created, CurrentBalance increased | Balance after = before + amount |
| T18.04 | `CashTransactionService.CreateExpenseAsync()` → CashTransaction created, CurrentBalance decreased | Balance after = before - amount |
| T18.05 | `CashTransactionService.CreateExpenseAsync()` insufficient balance → `Result.Failure("الرصيد غير كافٍ")` | IsSuccess = false |
| T18.06 | `CashBoxService.TransferAsync()` → TransferOut for source + TransferIn for destination | Two transactions created |
| T18.07 | `CashBoxService.TransferAsync()` source balance insufficient → `Result.Failure` | IsSuccess = false, rollback |
| T18.08 | `DailyClosureService.PerformClosureAsync()` → computes ClosingBalance = OpeningBalance + Income - Expense | Correct math |
| T18.09 | `DailyClosureService.VerifyClosureAsync()` → Status = Verified, IsVerified = true | Property check |
| T18.10 | `ChequeService.CreateAsync()` → `Result<ChequeDto>.Success`, status Pending | IsSuccess = true |
| T18.11 | `ChequeService.DepositAsync()` → status Deposited, DepositDate = UtcNow | Property check |
| T18.12 | `ChequeService.ClearAsync()` → status Cleared, CashBalance updated | Balance increased by cheque amount |
| T18.13 | `ChequeService.BounceAsync()` → status Bounced, BounceReason stored | Property check |
| T18.14 | `PaymentService.CreateCustomerPaymentAsync()` → CashTransaction.CustomerPayment created | TransactionType = CustomerPayment (8) |
| T18.15 | `PaymentService.CreateSupplierPaymentAsync()` → CashTransaction.SupplierPayment created | TransactionType = SupplierPayment (7) |
| T18.16 | `PaymentService.CreateSupplierPaymentAsync()` with WithholdingTax → TaxAmount deducted | NetAmount = Amount - TaxAmount |
| T18.17 | Transaction rollback on any failure → no CashTransaction, no balance change | Verify via mocks |
| T18.18 | `PaymentInvoiceAllocation` — payment distributed across multiple invoices correctly | Sum of allocations = PaymentAmount |
| T18.19 | Balance never negative after any transaction → Guard clause enforced | Verify all edge cases |

### T19 — FluentValidation Tests (Receipts & Payments)

| ID | Test | Expected |
|----|------|----------|
| T19.01 | `CreateCashBoxRequest` all valid → passes validation | IsValid = true |
| T19.02 | `CreateCashBoxRequest` empty Name → `"اسم الصندوق مطلوب"` | Specific error |
| T19.03 | `CreateCashTransactionRequest` empty CashBoxId → `"يجب اختيار الصندوق"` | Specific error |
| T19.04 | `CreateCashTransactionRequest` amount ≤ 0 → `"المبلغ يجب أن يكون أكبر من صفر"` | Specific error |
| T19.05 | `CreateCashTransactionRequest` empty TransactionType → validation error | Specific error |
| T19.06 | `CreateChequeRequest` empty ChequeNumber → `"رقم الشيك مطلوب"` | Specific error |
| T19.07 | `CreateChequeRequest` amount ≤ 0 → `"المبلغ يجب أن يكون أكبر من صفر"` | Specific error |
| T19.08 | `CreateCustomerPaymentRequest` empty CustomerId → `"يجب اختيار العميل"` | Specific error |
| T19.09 | `CreateSupplierPaymentRequest` empty SupplierId → `"يجب اختيار المورد"` | Specific error |
| T19.10 | `PerformClosureRequest` ActualCashCount < 0 → `"العد الفعلي لا يمكن أن يكون سالباً"` | Specific error |

### T20 — Database Configuration Tests (Receipts & Payments)

| ID | Test | Expected |
|----|------|----------|
| T20.01 | `CashBoxConfiguration` → `Name` has `.HasMaxLength(100)` | MaxLength = 100 |
| T20.02 | `CashBoxConfiguration` → `OpeningBalance` has `.HasPrecision(18, 2)` | Precision = (18,2) |
| T20.03 | `CashBoxConfiguration` → `CurrentBalance` has `.HasPrecision(18, 2)` | Precision = (18,2) |
| T20.04 | `CashBoxConfiguration` → FK `AccountId` is `DeleteBehavior.Restrict` | Restrict |
| T20.05 | `CashTransactionConfiguration` → `Amount` has `.HasPrecision(18, 2)` | Precision = (18,2) |
| T20.06 | `CashTransactionConfiguration` → `TransactionType` is `.IsRequired()` | Required |
| T20.07 | `CashTransactionConfiguration` → FK `CashBoxId` is `DeleteBehavior.Restrict` | Restrict |
| T20.08 | `DailyClosureConfiguration` → `OpeningBalance`/`ClosingBalance` have `.HasPrecision(18, 2)` | Precision = (18,2) |
| T20.09 | `DailyClosureConfiguration` → `Difference` has `.HasPrecision(18, 2)` | Precision = (18,2) |
| T20.10 | `ChequeConfiguration` → `Amount` has `.HasPrecision(18, 2)` | Precision = (18,2) |
| T20.11 | `ChequeConfiguration` → FK `CashBoxId` is `DeleteBehavior.Restrict` | Restrict |
| T20.12 | `CustomerPaymentConfiguration` → FK `CashBoxId` is `DeleteBehavior.Restrict` | Restrict |
| T20.13 | `SupplierPaymentConfiguration` → FK `CashBoxId` is `DeleteBehavior.Restrict` | Restrict |
| T20.14 | All Payment entities have `CreatedAt` with `.HasDefaultValueSql("GETUTCDATE()")` | Default value |

---

> **End of Phase 29 — Receipts & Payments Module Implementation Plan**
