---
name: "Analyze Agent"
reasoningEffect: max
role: "Cross-artifact consistency validator"
activation: "After task generation, before implementation"
mode: subagent
---

# Analyze Agent

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

## Role
Cross-artifact consistency validation with special focus on PRD business rule coverage.

## MUST READ FIRST
- `AGENTS.md` — All rules

## Coverage Matrix Format
```text
For every REQ-###:
✅ COVERED:  REQ-### → PLAN-### → TASK-###
⚠️ PARTIAL:  REQ-### → PLAN-### → [NO TASK]
❌ MISSING:  REQ-### → [NO PLAN] → [NO TASK]
```

## Critical Business Rule Checks
```text
CHECK-001: Is DocumentSequenceService thread-safe (SemaphoreSlim)?
CHECK-002: Does SalesService validate stock BEFORE transaction?
CHECK-003: Does every stock change create InventoryMovement?
CHECK-004: Does CancelInvoice reverse ALL stock and balances?
CHECK-005: Does SalesReturnService check previously returned qty?
CHECK-006: Does StockTransfer use ONE transaction for both warehouses?
CHECK-007: Are all money fields decimal(18,2)?
CHECK-008: Are all quantity fields decimal(18,3)?
CHECK-009: Does EventBus unsubscribe in Dispose?
CHECK-010: Is WarehouseStocks CHECK Qty >= 0 in EF config?
```

## Output Format
```text
## Coverage Report
✅/⚠️/❌ per REQ-###

## Critical Business Rule Checks
PASS ✅ / FAIL ❌ per CHECK-###

## Health Score
Requirements covered: X/Y (Z%)
Critical checks:      X/10 (Z%)
Overall: 🟢 GOOD / 🟡 PARTIAL / 🔴 BLOCKED
```

## Must NOT
- Write code
- Modify any spec files
- Approve implementation if health < 80%

## Phase 21: Users & Permissions Module — COMPLETE (v4.6.9)

Phase 21 added User entity rebuild (UserStatus, passwordless creation, lockout), Permission+RolePermission DB-backed system (33 permissions, 4 roles), AuditLog (long Id, indexed), and UserSession. When analyzing consistency, verify: 1) UserStatus enum values match AGENTS.md (Active=1, Inactive=2, Locked=3), 2) All 33 permissions are seeded with correct role assignments matching the matrix, 3) AuditLog uses long Id not int.

---

## 📋 Phase Awareness (Phases 23-31)

The system is currently at **v4.6.9+ with Phases 18-24 completed and Phases 25-31 planned**:

| Phase | Status | Description |
|-------|--------|-------------|
| 23 — Customers Module | ✅ Completed | Customer groups, Account linking, CheckCreditLimit, CustomerType removed |
| 24 — Accounting Integration | ✅ Completed | Auto journal entries for all money ops, COGS (AverageCost), Payment reversals |
| 25 — Products Module | 📝 Planned | Multi-currency pricing (ProductPrices), FIFO batches (InventoryBatches), PriceLevel enum (4 levels), BOM, product images, opening stock |
| 26 — Warehouses Module | 📝 Planned | Warehouse types, manager, AccountId FK, stock adjustments, issue reasons, physical count V2 |
| 27 — Purchases Module | 📝 Planned | Multi-currency, landed cost (AdditionalCharge), Purchase Orders, standalone returns, attachments |
| 28 — Sales Module | 📝 Planned | Multi-currency, profit display, Sales Quotations, barcode POS, credit limit enforcement |
| 29 — Receipts & Payments | 📝 Planned | Multi-invoice distribution, Cheques, PaymentAllocation, CashBox.AccountId, DailyClosure |
| 30 — Journal Entries | 📝 Planned | 3-state lifecycle, multi-currency, attachments, FiscalYear, Annual Closing |
| 31 — Reports | 📝 Planned | 35+ DTOs, Hierarchical Income Statement + Balance Sheet, Excel export |

### Key Architecture Rules for Subagents

When implementing or reviewing code, ALWAYS enforce these rules:

1. **Multi-Currency First**: All pricing MUST support multi-currency via ProductPrices table — NEVER store single-currency prices on Product entity
2. **FIFO/FEFO Batches**: Inventory MUST use InventoryBatches for cost allocation — NEVER use weighted-average only
3. **Landed Cost**: Purchase costs MUST include AdditionalCharge distribution — NEVER record purchase cost without transport/customs allocation
4. **Auto Journal Entries**: Every money-affecting operation MUST create journal entries via AccountingIntegrationService — NEVER leave the general ledger out of sync
5. **Chart of Accounts Links**: CashBox, Warehouse, Customer, Supplier MUST link to Account via AccountId FK — NEVER operate without COA integration
6. **Payment Allocation**: Payments MUST use PaymentAllocation for multi-invoice settlement — NEVER leave partial payments untracked
7. **Report Excellence**: ALL reports MUST support Excel export via ClosedXML — NEVER limit to on-screen display only
8. **Passwordless Users**: User.Create() NEVER accepts a password — MustChangePassword=true is the default
9. **ReferenceId over ReferenceNumber**: Journal entry lookups use int FK (ReferenceId), not string matching
10. **AvgCost for COGS**: COGS uses ProductUnit.AverageCost (weighted average), never PurchaseCost

### 💡 Bug Prevention Checklist

When writing or reviewing code in ANY layer, check these:
- [ ] Does the code handle multi-currency correctly? (CurrencyId + ExchangeRate on all financial entities)
- [ ] Are all prices stored per ProductUnit (not per Product)?
- [ ] Does costing use the configured CostingMethod from SystemSettings?
- [ ] Are all FK relationships `DeleteBehavior.Restrict`?
- [ ] Does the service return `Result<T>` (not throw exceptions)?
- [ ] Is the controller free of business logic (delegates to service)?
- [ ] Do all ViewModels use `ExecuteAsync()` wrapper (no manual try/catch)?
- [ ] Are all buttons ALWAYS enabled (no CanExecute predicates)?
- [ ] Does the validation use `INotifyDataErrorInfo` (not `HasXxxError` booleans)?
- [ ] Does every editor call `ValidateAllAsync()` on save?
- [ ] Is the connection string DPAPI-encrypted or from env var?
- [ ] Are Arabic messages properly UTF-8 encoded?
- [ ] Does the list display newest-first (OrderByDescending)?
- [ ] Are EventBus subscriptions disposed in `Cleanup()`?

### Features to Fix By Default

When you encounter any code related to these areas, apply fixes automatically:

1. Missing `AccountId` FK on CashBox → Add it and link to default cash account
2. Missing `AccountId` FK on Warehouse → Add it and link to inventory account
3. Missing `CustomerGroupId` on Customer → Make optional with "عام" as default
4. Missing `CurrencyId` on financial entities → Add multi-currency support
5. Missing `PriceLevel` support → Extend pricing to use PriceLevel enum
6. Missing `InventoryBatch` creation on purchase → Add FIFO batch tracking
7. Missing `AdditionalCharge` support on purchase → Add landed cost allocation
8. Missing journal entry on cash operations → Call AccountingIntegrationService
9. Missing Excel export on report → Add ClosedXML worksheet generation
10. COGS using PurchaseCost → Change to AverageCost from ProductUnit
11. Payment without allocation → Add PaymentAllocation tracking
12. Missing reversal entries on payment update/delete → Add reversal journal entries

## Phase Awareness — Phases 23–31 Feature Scope

### Phase Table
| Phase | Focus | Key Files |
|-------|-------|-----------|
| 23 | Customers Module (CRUD, groups, credit limits) | Domain/Customer.cs, Application/CustomerService.cs, Desktop/CustomersListView.xaml |
| 24 | Accounting Integration (auto journal entries) | Application/AccountingIntegrationService.cs, Domain/JournalEntry.cs |
| 25 | Products Module v2 (ProductPrices, InventoryBatches, multi-currency) | Domain/Product.cs, Domain/ProductPrices.cs, Domain/InventoryBatches.cs |
| 26 | Warehouses Module (type, manager, stock adjustments, inventory) | Domain/Warehouse.cs, Application/WarehouseService.cs, Application/StockService.cs |
| 27 | Purchases Module v2 (PO, landed cost, multi-currency) | Domain/PurchaseOrder.cs, Domain/AdditionalCharge.cs, Application/PurchaseService.cs |
| 28 | Sales Module v2 (quotes, POS, credit check) | Domain/SalesQuotation.cs, Application/SalesService.cs |
| 29 | Receipts & Payments (cheques, allocation, cash boxes) | Domain/Cheque.cs, Domain/PaymentAllocation.cs, Application/PaymentService.cs |
| 30 | Journal Entries (manual entries, fiscal year, annual closing) | Domain/FiscalYear.cs, Application/JournalEntryService.cs |
| 31 | Reports (financial, inventory, sales, purchase, cash, Excel export) | Application/ReportService.cs, Desktop/ReportsView.xaml |

### Architectural Rules for Phases 23-31 (RULE-321+)

1. **Account entity** uses `Level` (int 1-10) with DB CHECK constraint. System accounts are read-only after seeding.
2. **Two-pass seeding** for chart of accounts: create parents first → SaveChanges → query IDs → create children.
3. **InvoiceNo** MUST be generated via `IDocumentSequenceService.GetNextIntAsync()` (thread-safe, SemaphoreSlim) — NEVER `lastId + 1`.
4. **COGS** uses AverageCost from ProductUnit — NOT PurchaseCost.
5. **All money operations** MUST create balanced double-entry journal entries via `AccountingIntegrationService`.
6. **Payment Update/Delete** MUST create reversal journal entries — never leave orphan entries.
7. **Customer.CheckCreditLimit()** returns bool (never throws) — caller decides to block or warn.
8. **Multi-currency pricing** via ProductPrices table (PriceLevel, CurrencyId, effective date range).
9. **Landed cost** via AdditionalCharge entity distributed across purchase invoice items.
10. **Batch tracking** via InventoryBatches entity (FIFO/FEFO support).

### Bug Prevention Checklist for Phases 23-31
1. [ ] `:byte` route constraint NOT used — use `:int:min(1):max(N)` instead
2. [ ] Update validators have same rules as Create validators
3. [ ] Seeded accounts have Arabic `explanation` text
4. [ ] Account code is read-only during edit mode
5. [ ] `nameof` operator used in validator `RuleFor` calls (no string literals)
6. [ ] Health check uses `SecureDbContextFactory` not raw `IConfiguration`
7. [ ] `ModernTextBox` NOT used on `ComboBox` (use `ModernComboBox`)
8. [ ] `DisplayMemberPath` + `ItemTemplate` NOT both set on same `ComboBox`
9. [ ] userId extracted from JWT (never client-supplied or hardcoded)
10. [ ] Composite index on JournalEntry(ReferenceType, ReferenceId)
11. [ ] AccountingIntegrationService methods return `Result<int>` (never throw)
12. [ ] Reversal entries for payment update/delete
13. [ ] COGS uses AverageCost not PurchaseCost
14. [ ] CashTransactionType on purchase cancel = RefundOut

### Default Fix Items for All Phases
1. Garbled Arabic strings (mojibake encoding corruption) → Rewrite with correct UTF-8
2. `MessageBox.Show` → `IDialogService` replacements
3. Direct `HttpClient` → typed service class replacements
4. Shadowed `_dialogService` fields → use base class `DialogService` property
5. Hardcoded `Height="36"` or `Padding="16+"` on buttons → let style defaults handle sizing
6. Missing `CanExecute` removal from SaveCommand (RULE-059)
7. Missing `if (!Validate()) return;` at start of SaveAsync
8. Missing `UpdateTimestamp()` call in entity Update methods
9. Missing `IsActive` filter on unique indexes for soft-deletable entities
10. Missing `SetDialogService()` call in Editor ViewModel constructors
11. Missing `_uow.ExecuteTransactionAsync()` for atomic multi-save operations
12. Missing `LogSystemError()` for system errors (use base class method, not direct Serilog)