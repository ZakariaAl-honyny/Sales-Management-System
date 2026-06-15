# Implementation Plan: Accounting Engine Automation (Phase 24)

**Branch**: `024-accounting-automation` | **Date**: 2026-06-13
**Input**: Database schema (Module 4), Phase 23 Customers, Phase 25–31 plans, Global Analysis
**Dependency**: Phase 18 (Accounting Foundation — JournalEntry, JournalEntryLine, Account, SystemAccountMappings)

---

## Summary

Currently, **no business operation creates journal entries automatically**. Sales post, purchase post, payments, and opening balances all affect inventory, customer/supplier balances, and cash — but the general ledger is never updated. This phase introduces a dedicated `AccountingIntegrationService` that creates balanced double-entry journal entries inside the **same transaction** as every money-affecting operation. The service is called from within existing business services (SalesService, PurchaseService, PaymentService, CustomerService, SupplierService) and uses the flexible key-value `SystemAccountMappings` to resolve which account to debit/credit for each economic event.

All entries use **per-entity account routing** (Customer.AccountId, Supplier.AccountId) instead of fixed AR/AP accounts. COGS is computed from **weighted average cost in InventoryBatches**, not PurchaseCost. Every method returns `Result<int>` — never throws.

---

## Entry Scenarios

### 1. Customer Opening Balance (OB > 0)
**Trigger**: `CustomerService.CreateAsync()` when `OpeningBalance > 0`.
**Timing**: After customer saved (has ID), inside `ExecuteTransactionAsync`.
**Entry**: Dr Customer.AccountId (AR sub-account under 1210), Cr OpeningBalanceEquity (MappingKey `"OpeningBalanceEquity"`).
**Condition**: Skip entirely if OB = 0. EntryType = OpeningBalance.

### 2. Supplier Opening Balance (OB > 0)
**Trigger**: `SupplierService.CreateAsync()` when `OpeningBalance > 0`.
**Timing**: After supplier saved (has ID), inside `ExecuteTransactionAsync`.
**Entry**: Dr OpeningBalanceEquity, Cr Supplier.AccountId (AP sub-account under 2100).
**Condition**: Skip if OB = 0. EntryType = OpeningBalance.

### 3. Sales Invoice Post (Credit Sale)
**Trigger**: `SalesService.PostAsync()` after stock deducted and customer balance updated.
**Entry — Revenue side**: Dr Customer.AccountId (AR sub-account, not fixed AR) for DueAmount, Cr SalesRevenue (`"SalesRevenue"`) for NetRevenue (SubTotal − Discount), Cr VATOutput (`"VATOutput"`) for TaxAmount.
**Entry — COGS side**: Dr COGS (`"COGS"`) for total weighted-average cost, Cr InventoryAsset (`"InventoryAsset"`) for same amount.
**Cash variant**: When PaymentType = Cash, debit CashBox.AccountId instead of Customer.AccountId, for the PaidAmount portion. Mixed splits between CashBox and AR.
**Validation**: NetRevenue MUST be ≥ 0 (if Discount > SubTotal, return failure — never clamp to zero).

### 4. Sales Invoice Cancel
**Trigger**: `SalesService.CancelAsync()` on a Posted invoice (Draft invoices have no journal entry).
**Entry**: Full reversal of the Post entry — every line's debit and credit are swapped. The original invoice's PaidAmount, DueAmount, NetRevenue, TaxAmount, and COGS are reused to build the reversal.
**Note**: COGS amount is recalculated from current InventoryBatches at cancel time (acceptable — cancellations are near-term).

### 5. Purchase Invoice Post (Credit Purchase)
**Trigger**: `PurchaseService.PostAsync()` after stock increased and supplier balance updated.
**Entry**: Dr InventoryAsset (`"InventoryAsset"`) for NetPurchaseCost (SubTotal − Discount), Dr VATInput (`"VATInput"`) for TaxAmount, Cr Supplier.AccountId (AP sub-account under 2100) for DueAmount.
**Cash variant**: When PaymentType = Cash, credit CashBox.AccountId for PaidAmount instead of Supplier.AccountId.

### 6. Purchase Invoice Cancel
**Trigger**: `PurchaseService.CancelAsync()` on a Posted invoice.
**Entry**: Full reversal of the Post entry (debit↔credit swap). Uses original values for NetPurchaseCost, TaxAmount, PaidAmount, DueAmount.

### 7. Customer Payment (Receipt)
**Trigger**: `PaymentService.CreateCustomerPaymentAsync()` after customer.DecreaseBalance.
**Entry**: Dr CashBox.AccountId (`"DefaultCashAccount"`) for amount, Cr Customer.AccountId (AR sub-account) for same amount.
**EntryType**: CustomerReceipt (10).

### 8. Supplier Payment
**Trigger**: `PaymentService.CreateSupplierPaymentAsync()` after supplier.DecreaseBalance.
**Entry**: Dr Supplier.AccountId (AP sub-account) for amount, Cr CashBox.AccountId for same amount.
**EntryType**: SupplierPayment (11).

### 9. Payment Update/Delete Reversal
**Trigger**: When a payment (CustomerReceipt or SupplierPayment) is updated or deleted.
**Pattern**: First create a **reversal entry** mirroring the original with Dr↔Cr swapped, using the same ReferenceType/ReferenceId. Then apply the new amount (or skip for delete). This ensures the general ledger is never left with orphan entries.

---

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| **Per-entity account routing** | Use `Customer.AccountId` / `Supplier.AccountId` instead of fixed AR/AP from SystemAccountMappings. Every customer/supplier has their own sub-account under 1210/2100. The only fixed mappings are revenue, COGS, inventory, VAT, cash, and OB equity. |
| **COGS = weighted average cost** | COGS is computed from the current `InventoryBatches.UnitCost` (weighted average via FIFO), NOT from `ProductUnit.PurchaseCost`. At sale time, sum (quantity × batch unit cost) across consumed batches. |
| **NetRevenue validation** | If `Discount > SubTotal`, return `Result.Failure("الخصم يتجاوز قيمة الفاتورة")`. Never clamp to zero — that produces unbalanced entries. |
| **Result<int> everywhere** | All 8+ AccountingIntegrationService methods return `Result<int>` (the journal entry ID). The caller checks `IsSuccess` and rolls back on failure. Never throw. |
| **ReferenceId as primary lookup** | Journal entries store `(ReferenceType, ReferenceId)` for reversal lookups. `ReferenceNumber` (string) is a fallback for manual search only — never the primary lookup key. |
| **Composite index** | A composite index on `JournalEntry(ReferenceType, ReferenceId)` with filter `WHERE ReferenceType IS NOT NULL AND ReferenceId IS NOT NULL` ensures fast reversal lookups. |
| **SystemAccountMappings key-value** | Look up accounts by MappingKey strings: `"SalesRevenue"`, `"COGS"`, `"InventoryAsset"`, `"AccountsReceivable"`, `"AccountsPayable"`, `"DefaultCashAccount"`, `"VATOutput"`, `"VATInput"`, `"OpeningBalanceEquity"`, `"SalesDiscount"`, `"PurchaseDiscount"`, `"PurchaseReturn"`. The service queries `SystemAccountMappings` by `MappingKey + BranchId` at runtime — no fixed properties needed. |
| **Fiscal year guard** | Before creating ANY entry, check that the transaction's fiscal year is not closed. Return `Result.Failure` with Arabic message if closed. |
| **UserId from JWT** | `CreatedByUserId` is extracted from JWT claims in the controller, passed through to the service, and assigned to the journal entry. Never accept client-supplied userId. |

---

## Journal Entry Patterns

Every journal entry follows these invariant rules:

1. **Balanced**: SUM(Debit) == SUM(Credit) across all lines. The service validates before persisting.
2. **Single-sided lines**: Each line has either Debit > 0 (Credit = 0) or Credit > 0 (Debit = 0). CHECK constraints `CHK_DebitOrCredit` and `CHK_NoNegativeValues` enforce this at DB level.
3. **EntryType identifies source**: Sales=2, Purchase=3, Receipt=4, Payment=5, OpeningBalance=9, CustomerReceipt=10, SupplierPayment=11.
4. **Reference tracking**: Every auto-generated entry sets `ReferenceType` (e.g., `"SalesInvoice"`, `"CustomerPayment"`) and `ReferenceId` (the source document's PK). This enables reversal lookups, audit trails, and account-ledger drill-down.
5. **Number generation**: Entry numbers use `JE-{yyyyMMdd}-{NNNN}` format via `JournalEntryNumberGenerator` (thread-safe daily sequence).
6. **Reversal entries**: On cancel/reversal, the new entry has `IsReversed = true` and sets `ReversedByEntryId` on the original entry (self-referencing FK with Restrict).
7. **Skip when zero**: If an amount is 0, that line is omitted. If all lines are zero (e.g., opening balance of 0), the entire entry is skipped.
8. **Caller owns the transaction**: `AccountingIntegrationService` never opens its own transaction. It creates and adds entities, then the caller (business service) commits via `ExecuteTransactionAsync`. If the integration fails, the caller rolls back everything.

---

## Tasks

### Task 1: Extend Enum — JournalEntryType
Add `CustomerReceipt = 10` and `SupplierPayment = 11` to the existing enum in Domain. These enable traceability for payment journal entries in reports and ledgers.

### Task 2: Service — AccountingIntegrationService Interface + Implementation
Create `IAccountingIntegrationService` with 8 core methods (CreateCustomerOpeningEntryAsync, CreateSupplierOpeningEntryAsync, CreateSalesInvoiceEntryAsync, ReverseSalesInvoiceEntryAsync, CreatePurchaseInvoiceEntryAsync, ReversePurchaseInvoiceEntryAsync, CreateCustomerPaymentEntryAsync, CreateSupplierPaymentEntryAsync) plus optional reversal helpers for payment update/delete. Each method:
- Loads `SystemAccountMappings` by MappingKey (key-value lookup)
- Loads `Customer.AccountId` / `Supplier.AccountId` for AR/AP routing
- Loads `CashBox.AccountId` for cash routing
- Computes COGS from `InventoryBatches` (weighted average per batch consumed)
- Validates fiscal year is open
- Calls private helper `CreateAndPostEntryAsync` to build, validate balance, and persist the JournalEntry + lines
- Returns `Result<int>` (the entry ID)

### Task 3: Integrate into CustomerService
Inject `IAccountingIntegrationService`. In `CreateAsync()`: after customer saved, if `OpeningBalance > 0`, call `CreateCustomerOpeningEntryAsync`. Wrap customer create + JE in `ExecuteTransactionAsync`.

### Task 4: Integrate into SupplierService
Same pattern as CustomerService. Call `CreateSupplierOpeningEntryAsync` when OB > 0.

### Task 5: Integrate into SalesService
In `PostAsync()`: after stock deduction and customer balance update, call `CreateSalesInvoiceEntryAsync`. In `CancelAsync()`: if invoice was Posted, call `ReverseSalesInvoiceEntryAsync`. Both inside existing transaction.

### Task 6: Integrate into PurchaseService
In `PostAsync()`: after stock increase and pricing update, call `CreatePurchaseInvoiceEntryAsync`. In `CancelAsync()`: if Posted, call `ReversePurchaseInvoiceEntryAsync`.

### Task 7: Integrate into PaymentService
In `CreateCustomerPaymentAsync()`: call `CreateCustomerPaymentEntryAsync`. In `CreateSupplierPaymentAsync()`: call `CreateSupplierPaymentEntryAsync`. For update/delete: first reverse the original entry, then apply the new one.

### Task 8: DB Migration — Composite Index
Add a composite index on `JournalEntry(ReferenceType, ReferenceId)` using a filtered condition `WHERE ReferenceType IS NOT NULL AND ReferenceId IS NOT NULL`. No new tables or columns needed (SystemAccountMappings already uses key-value design — no OpeningBalanceEquityAccountId field needed).

### Task 9: Tests
Cover every scenario from the testing matrix: opening balance (zero and non-zero), sales post (cash/credit/mixed, with/without tax), sales cancel (Posted vs Draft), purchase post (cash/credit), purchase cancel, customer/supplier payment, payment reversal, fiscal year closed, mapping not found. Verify balance invariants, reference tracking, and transaction rollback on failure.

---

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| **COGS mismatch on cancel** | If product costs changed between post and cancel, reversed COGS may differ from original. Low materiality (±small amount) because cancellations are near-term. Accept as-is. | Document in code comment. |
| **Fiscal year boundary** | Invoice posted in FY2026, cancelled in FY2027. The guard blocks the reversal. | Enhance guard to allow reversals of entries originally created in a now-closed year if the reversal creates an entry in the open year. Deferred to Phase 30. |
| **Missing SystemAccountMappings** | If a required MappingKey is not configured, the integration returns failure and the entire transaction rolls back. | Seed all 12+ mapping keys in `AccountingSeeder` with valid account IDs. The system should not start without mappings configured. |
| **Performance** | Every invoice post now makes an additional DB round-trip to create JE + lines, inside a transaction. | Negligible for single-invoice operations (< 50ms). For bulk import, batch the JE creation. |
| **Transaction scope** | Current services use `BeginTransactionAsync` (incompatible with retrying execution strategy). | Migrate to `ExecuteTransactionAsync` pattern (RULE-275/276) in the same PR. The `IUnitOfWork` already has this method. |
