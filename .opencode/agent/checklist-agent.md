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

## 65-Table Schema Validation Checklist
- [ ] All 65 tables exist across 8 modules (Core=14, Org/Curr/Settings=11, Products=5, Accounting=10, Inventory=10, Sales=6, Purchases=6, Infrastructure=2)
- [ ] NO `InventoryMovement` table — replaced by `InventoryTransactions` + `InventoryTransactionLines`
- [ ] NO `StockTransfer`/`StockTransferItem` — replaced by `WarehouseTransfer`/`WarehouseTransferLine`
- [ ] NO `CustomerGroup` or `SupplierType` entities anywhere
- [ ] NO `SalesQuotation` or `PurchaseOrder` tables
- [ ] NO `Cheque` or `DailyClosure` tables
- [ ] `Parties` table exists: Name, Phone, Email, Address, TaxNumber, Notes
- [ ] `Units` table exists with smallint PK, Name, Symbol, IsSystem, IsActive
- [ ] `ProductPrices` table: ProductUnitId FK, CurrencyId FK, Price decimal(18,2), EffectiveFrom, EffectiveTo
- [ ] `InventoryBatches` table: ProductId FK, WarehouseId FK, BatchNo, ExpiryDate, QuantityReceived, QuantityRemaining, UnitCost
- [ ] `InventoryTransactions` + `InventoryTransactionLines` replace `InventoryMovement`
- [ ] `WarehouseTransfers` + `WarehouseTransferLines` replace `StockTransfer`
- [ ] `InventoryAdjustments` + `InventoryAdjustmentLines`: AdjustmentType (Opening/Increase/Shortage/Damage)
- [ ] `InventoryCounts` + `InventoryCountLines`: SystemQuantity, ActualQuantity, DifferenceQuantity
- [ ] `SalesInvoiceLines` has NO DiscountAmount per line (header only)
- [ ] `PurchaseInvoiceLines` has NO DiscountAmount per line (header only)
- [ ] `SystemLogs.Level` = tinyint (not nvarchar)

## smallint FK Type Checklist
- [ ] `Roles.Id` = smallint → UserRoles.RoleId = smallint, RolePermissions.RoleId = smallint (DB-driven — values 1-9, no UserRole enum)
- [ ] `Departments.Id` = smallint → Employees.DepartmentId = smallint
- [ ] `Branches.Id` = smallint → UserBranches.BranchId = smallint, Warehouses.BranchId = smallint
- [ ] `Warehouses.Id` = smallint → all FK references use smallint
- [ ] `Currencies.Id` = smallint → all FK references (ProductPrices, invoices, etc.) use smallint
- [ ] `Taxes.Id` = smallint → all FK references use smallint
- [ ] `Units.Id` = smallint → ProductUnits.UnitId = smallint
- [ ] `AccountCategories.Id` = smallint → Accounts.CategoryId = smallint
- [ ] C# types match: `short` for smallint columns in entity classes

## bigint AuditLog Checklist
- [ ] `AuditLog.Id` is `long` (C#) / `bigint` (SQL)
- [ ] `SystemLogs.Id` is `long` / `bigint`
- [ ] AuditLog has index on `(UserId, CreatedAt DESC)`
- [ ] AuditLog has index on `(EntityName, EntityId)`
- [ ] AuditLog has index on `(CreatedAt DESC)`
- [ ] SystemLogs has index on `(Level, CreatedAt DESC)`

## ProductPrices (Multi-Currency) Checklist
- [ ] Prices stored on `ProductPrices` table (not on Product entity)
- [ ] Price = per (ProductUnit × Currency) with effective date range
- [ ] No `RetailPrice`/`WholesalePrice` on Product entity
- [ ] Price lookup: filter by ProductUnitId + CurrencyId + EffectiveFrom/EffectiveTo
- [ ] Price has DB CHECK constraint `>= 0`
- [ ] ProductPrices seed data includes prices in base currency

## InventoryBatches (FIFO/FEFO) Checklist
- [ ] Batch created on every purchase (QuantityReceived = QuantityRemaining = purchase qty)
- [ ] Opening stock creates an opening batch (BatchNo = 1)
- [ ] Sale consumes from oldest batch first: `OrderBy(b => b.Id)` for FIFO
- [ ] If `TrackExpiry = true`: FEFO — `OrderBy(b => b.ExpiryDate)` nearest expiry first
- [ ] QuantityRemaining updated on sale, never goes below 0
- [ ] Purchase return restores original batch's QuantityRemaining
- [ ] Batch has UnitCost (decimal(18,2)) for COGS calculation
- [ ] Sales COGS = SUM of batch UnitCost × quantity consumed per batch

## Party Entity Checklist
- [ ] Customers have `PartyId` FK → Parties (mandatory, non-nullable)
- [ ] Suppliers have `PartyId` FK → Parties (mandatory, non-nullable)
- [ ] Employees have `PartyId` FK → Parties (mandatory, non-nullable)
- [ ] Party stores: Name, Phone, Email, Address, TaxNumber, Notes
- [ ] Customer/Supplier do NOT have Name/Phone/Email directly (use Party.Name etc.)
- [ ] Party soft-delete cascades: deactivating Party deactivates linked Customer/Supplier
- [ ] NO OpeningBalance or CurrentBalance on Customer/Supplier entity
- [ ] NO CurrencyId on Customer/Supplier entity (per-transaction)

## Unit Entity (Independent Table) Checklist
- [ ] `Units` table exists: smallint PK, Name, Symbol, IsSystem, IsActive
- [ ] Seed data: حبة (PCS), كرتون (CTN), كيلو (KG), جرام (G), لتر (L), متر (M), بالة (BAL)
- [ ] `IsSystem = true` for seed units (protected from deletion)
- [ ] User can add new units, can soft-deactivate unused units
- [ ] `ProductUnits` links Product → Unit via `UnitId` FK (not string name)
- [ ] `ProductUnit.Unit.Name` / `ProductUnit.Unit.Symbol` for display

## BaseCurrency Immutability Checklist
- [ ] `IsBaseCurrency` set during system creation only — NEVER user-changeable
- [ ] `Currency.SetAsBaseCurrency()` calls `UpdateTimestamp()`
- [ ] Filtered unique index: `WHERE IsBaseCurrency = 1 AND IsActive = 1`
- [ ] Desktop UI has NO "Set as base" button or toggle
- [ ] System seed always creates one base currency + USD as secondary

## Perpetual Inventory Checklist
- [ ] NO "Purchases" clearing account in chart of accounts
- [ ] Purchase invoice posts: Dr Inventory Asset / Cr Cash or AP
- [ ] COGS computed from InventoryBatches.UnitCost at sale time
- [ ] Every stock change recorded in InventoryTransaction + InventoryTransactionLine
- [ ] WarehouseStock.Quantity updated in real-time on every transaction
- [ ] No direct WarehouseStock.Quantity manipulation outside transaction system
- [ ] InventoryAdjustments used for corrections (not direct stock edits)

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
- [ ] User uses IsActive (bool) + IsLocked (bool) — NO UserStatus enum
- [ ] UserRole enum is REMOVED — roles are DB-driven via Role entity (smallint PK, values 1-9)
- [ ] Passwordless User.Create() — no password parameter, MustChangePassword=true
- [ ] RecordLoginAttempt() logic: success resets counter, failure increments (IsLocked=true at 5)
- [ ] Permission.IsSystem = true blocks deletion/modification
- [ ] AuditLog uses long Id (bigint) with 3 indexes
- [ ] All FK on Permission, RolePermission, AuditLog, UserSession use Restrict
- [ ] PermissionService.UpdateRolePermissionsAsync() uses ExecuteTransactionAsync
- [ ] DbSeeder seeds 45 permissions with 9-role assignments (Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager)
- [ ] Admin user seeded passwordless (PasswordHash = null)

---

## 📋 Phase Awareness (Phases 23-31)

The system is currently at **v4.6.9+ with Phases 18-24 completed and Phases 25-31 planned**:

| Phase | Status | Description |
|-------|--------|-------------|
| 23 — Customers Module | ✅ Completed | Customer groups, Account linking, CheckCreditLimit, CustomerType removed |
| 24 — Accounting Integration | ✅ Completed | Auto journal entries for all money ops, COGS (AverageCost), Payment reversals |
| 25 — Products Module | 📝 Planned | Multi-currency pricing (ProductPrices), FIFO batches (InventoryBatches), Unit independent table, Party entity, opening stock via InventoryBatches |
| 26 — Warehouses Module | 📝 Planned | Warehouse types, WarehouseTransfer/WarehouseTransferLine replaces StockTransfer, InventoryAdjustments with Damage type, Perpetual Inventory |
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
3. Missing `CustomerGroupId` on Customer → REMOVED in V1 (deferred to V2). Do NOT add.
4. Missing `CurrencyId` on financial entities → Add multi-currency support
5. Missing `PriceLevel` support → Extend pricing to use PriceLevel enum
6. Missing `InventoryBatch` creation on purchase → Add FIFO batch tracking
7. Missing `AdditionalCharge` support on purchase → Add landed cost allocation
8. Missing journal entry on cash operations → Call AccountingIntegrationService
9. Missing Excel export on report → Add ClosedXML worksheet generation
10. COGS using PurchaseCost → Change to AverageCost from ProductUnit
11. Payment without allocation → Add PaymentAllocation tracking
12. Missing reversal entries on payment update/delete → Add reversal journal entries