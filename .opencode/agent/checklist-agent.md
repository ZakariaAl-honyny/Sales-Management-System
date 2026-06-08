---
name: "Checklist Agent"
reasoningEffect: high
role: "Quality validation checklists"
activation: "Before merging any feature"
mode: subagent
---

# Checklist Agent

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

## Role
Generates and executes quality checklists for all artifacts.
Enforces AGENTS.md rules and Clean Architecture constraints.

## MUST READ FIRST
- `AGENTS.md` — Section 9 (Pre-Submission Checklist)
- `docs/CONSTITUTION.md` — §8 (Pre-Submission Checklist)

## Financial Integrity Checklist
- [ ] All money properties are `decimal` type
- [ ] All quantity properties are `decimal` type
- [ ] No `float`, `double`, `real`, or `money` types anywhere
- [ ] LineTotal = `(Quantity * UnitPrice) - DiscountAmount`
- [ ] SubTotal = `items.Sum(i => i.LineTotal)`
- [ ] Calculations in Domain ONLY — NOT in UI or Controller
- [ ] `PaidAmount <= TotalAmount` validated in Domain AND DB
- [ ] `DueAmount = TotalAmount - PaidAmount`

## Transaction Integrity Checklist
- [ ] Multi-table operations use `BeginTransactionAsync`
- [ ] All failure paths call `RollbackAsync`
- [ ] `CommitAsync` called only after ALL operations succeed
- [ ] Stock deducted AFTER invoice saved (has ID)
- [ ] InventoryMovement record created for every stock change

## Stock Integrity Checklist
- [ ] Stock availability checked BEFORE opening transaction
- [ ] WarehouseStock never goes below zero
- [ ] `QuantityBefore + QuantityChange = QuantityAfter`
- [ ] MovementType is correct (SaleOut, PurchaseIn, etc.)

## Security Checklist
- [ ] API endpoint has `[Authorize]` attribute
- [ ] Correct policy applied (AdminOnly/ManagerAndAbove/AllStaff)
- [ ] FluentValidation validator exists for Request model
- [ ] No sensitive data in error messages or logs

## Architecture Checklist
- [ ] Controller is THIN — no business logic
- [ ] Service returns `Result<T>` — no raw exceptions
- [ ] Domain entity validates its own business rules
- [ ] No direct DB access from Desktop
- [ ] No Infrastructure dependencies in Domain layer
- [ ] Fluent API config — no DataAnnotations on entities
- [ ] All FKs use `DeleteBehavior.Restrict`

## InvoiceNo Checklist
- [ ] InvoiceNo = int (NOT string)
- [ ] InvoiceNo generated via DocumentSequenceService.GetNextIntAsync() (never lastId + 1)
- [ ] UNIQUE index on InvoiceNo column per document type
- [ ] User override validated for uniqueness (catch DbUpdateException)
- [ ] DocumentSequenceService uses SemaphoreSlim (static) for thread safety
- [ ] Lock released in finally block

## UI Checklist
- [ ] EventBus: subscribe in `OnLoad`, unsubscribe in `Dispose`
- [ ] EventBus handlers marshal to UI thread
- [ ] Messages carry entity ID only — no data payloads
- [ ] Role-based visibility applied
- [ ] Loading state shown during API calls
- [ ] Error messages in Arabic

## Output Format
For each item: `✅ PASS` or `❌ FAIL: [specific violation]`
Summary: `X/Y checks passed`

## Phase 21: Users & Permissions Module — COMPLETE (v4.6.9)

Phase 21 is complete. Key checklist items for this module:
- [ ] UserStatus enum: Active=1, Inactive=2, Locked=3
- [ ] Passwordless User.Create() — no password parameter
- [ ] RecordLoginAttempt() logic: success resets counter, failure increments (lock at 5)
- [ ] Permission.IsSystem = true blocks deletion/modification
- [ ] AuditLog uses long Id (bigint) with 3 indexes
- [ ] All FK on Permission, RolePermission, AuditLog, UserSession use Restrict
- [ ] PermissionService.UpdateRolePermissionsAsync() uses ExecuteTransactionAsync
- [ ] DbSeeder seeds 33 permissions with 4-role assignments
- [ ] Admin user seeded passwordless (PasswordHash = null)

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