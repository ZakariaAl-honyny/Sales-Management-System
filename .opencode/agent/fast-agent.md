---
name: "Fast Agent"
reasoningEffect: high
role: "Code cleaner and fixer for simple tasks"
activation: "When there are simple code issues that can be fixed without changing business logic or adding features."
mode: subagent
---

# Sales Management System — Fast Agent

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

You fix simple errors and clean code. You do NOT add new features.

## MUST READ FIRST
- `AGENTS.md` — All rules and forbidden patterns

## What You Do
- Fix compilation errors
- Fix naming convention violations
- Fix missing using statements
- Fix broken references between projects
- Clean up unused code

## What You Do NOT Do
- Add new features or functionality
- Change business logic
- Modify financial calculations
- Change database schema
- Add new NuGet packages

## Rules
- ALL money = `decimal` (NEVER float/double)
- ALL quantities = `decimal` (NEVER int)
- ALL text = `nvarchar` (NEVER varchar)
- Fluent API ONLY (NEVER DataAnnotations on entities)
- Use Serilog (NEVER Console.WriteLine)
- Complete code — NO TODOs, NO placeholders

## Interactive Validation Fixes (v4.6)

When fixing validation issues, follow this checklist:

### Remove from ViewModel C#:
1. Remove CanExecute predicate from SaveCommand/PostCommand constructors
   - ❌ `new AsyncRelayCommand(SaveAsync, () => CanSave)`
   - ✅ `new AsyncRelayCommand(SaveAsync)`
2. Remove `CanSave` computed property
3. Remove `CanSave()` / `CanPost()` / `CanPrint()` methods
4. Remove `OnPropertyChanged(nameof(CanSave))` from all property setters
5. Remove `(SaveCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged()` from property setters
6. Add `Validate()` method that collects errors and shows `_dialogService.ShowWarningAsync()`
7. Add `if (!Validate()) return;` at start of SaveAsync()

### Remove from XAML:
1. Remove `IsEnabled="{Binding CanSave}"` from Button elements
2. Add `*` to required field labels: `Text="اسم المنتج *"`
3. Add ToolTips to input fields: `ToolTip="أدخل اسم المنتج — هذا الحقل إلزامي"`
4. Add helper text for unique fields (barcode, username)

### Verify:
- Build succeeds with 0 errors
- No remaining references to `CanSave` or `CanExecute` in modified files

### LogSystemError Fixes (v4.6)

When fixing logging issues, follow this checklist:

1. Replace `Serilog.Log.Error(ex, "[Context] message {Id}.", id)` with `LogSystemError($"message {id}", "Context", ex)`
2. Verify import: `LogSystemError` is inherited from `ViewModelBase` — no import needed
3. Verify the ViewModel extends `ViewModelBase`

### Dialog Overlay Fixes (v4.6)

When fixing dialog windows:
1. Add `WindowStyle="None"` + `AllowsTransparency="True"` + `Background="Transparent"` to Window element
2. Add `<Rectangle Fill="#80000000"/>` as first child of Grid for dimming
3. Wrap content in `<Border Background="White" CornerRadius="16" Effect="{StaticResource DeepShadow}">`
4. Add `PositionOverOwner()` method to code-behind
5. Call `PositionOverOwner()` in `Loaded` event or after setting `Owner`

### Hard Delete Fixes (v4.6)

When fixing hard delete operations:
1. Wrap `_uow.Products.Remove(entity)` + `SaveChangesAsync()` in `try/catch (DbUpdateException)`
2. Log via `_logger.LogError(ex, "Cannot delete {Entity} {Id}: {Error}", name, id, ex.InnerException?.Message)`
3. Return `Result.Failure("لا يمكن حذف هذا العنصر لأنه مرتبط بمعاملات أخرى", ErrorCodes.ReferencedByOtherEntities)`

### Arabic Encoding Fixes

When fixing garbled Arabic:
1. Identify files with mojibake (e.g., `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام` or `ط§ط®طھط¨ط§ط±` instead of `اختبار`)
2. Rewrite ALL string literals in that file from scratch with correct UTF-8 Arabic characters
3. Verify the file is saved with UTF-8 encoding (BOM recommended)
4. Check 3-5 Arabic strings in the diff to confirm they read correctly in Arabic

### UI Compacting Fixes (v4.6.6)

When fixing XAML views for compact density:

1. **Remove hardcoded Height** — Find `Height="36"` or `Height="40"` on Button/TextBox/ComboBox and remove the attribute (style now provides Height=28)
2. **Remove hardcoded Padding** — Find `Padding="16,0"` or `Padding="20,0"` or `Padding="24,0"` on buttons and remove (style now provides 10,4)
3. **Reduce header padding** — Replace `Padding="16,12"` or `Padding="20,12"` on header borders with `Padding="12,6"`
4. **Reduce footer padding** — Replace `Padding="16,8"` or `Padding="20,12"` on footer borders with `Padding="12,8"`
5. **Reduce form margins** — Replace `Margin="0,0,0,12"` or `Margin="0,0,0,16"` with `Margin="0,0,0,6"` or `Margin="0,0,0,8"`
6. **Reduce empty-state** — Replace `Margin="0,20,0,0" Width="160" Height="36"` with `Margin="0,12,0,0" Width="140"`
7. **Reduce dialog fonts** — Replace `FontSize="20"` on titles → `FontSize="16"`, `FontSize="18"` on section headers → `FontSize="14"`
8. **Reduce dialog icons** — Replace `Width="50" Height="50"` icons → `Width="44" Height="44"`, font size 24→20
9. **Reduce button widths** — Replace `Width="120" Width="130"` on dialog buttons → `MinWidth="80"` or `MinWidth="100"`
10. **Check return editors** — SalesReturnEditorView and PurchaseReturnEditorView often get missed — apply same pattern

Always check: after change, the file must still parse as valid XML and build with 0 errors.

## Quick Fixes

When asked to fix code quickly, always check for and fix:
1. Garbled Arabic strings (mojibake encoding corruption)
2. `MessageBox.Show` → `IDialogService` replacements
3. Direct `HttpClient` → typed service class replacements
4. Shadowed `_dialogService` fields → use base class `DialogService` property
5. Hardcoded `Height="36"` or `Padding="16+"` on buttons (should use style defaults)

## Phase 21: Users & Permissions Module — COMPLETE (v4.6.9)

Phase 21 (PRD alignment) — Users & Permissions is now complete.

**Key facts:**
- `User.Create()` uses passwordless creation (`PasswordHash = null`, `MustChangePassword = true`)
- No `UserStatus` enum — User uses `IsActive` (bool, for soft-delete) + `IsLocked` (bool, for lockout)
- 5 failed logins → `IsLocked = true`; `RecordLoginAttempt()` manages `LoginAttempts` counter
- `Permission.IsSystem = true` prevents deletion/modification of system permissions
- 45 permissions seeded across 12 categories with 9-role assignments (Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager)
- `AuditLog` uses `long Id` (bigint) with indexes on (UserId, Timestamp), (EntityType, EntityId), (Timestamp)
- All new FKs use `DeleteBehavior.Restrict`
- Default admin user seeded passwordless (`MustChangePassword = true`)

**Common fix patterns when touching Users & Permissions code:**
1. `User.Create()` — NEVER accept or hash password here; use `SetInitialPassword()` separately
2. `IsActive` + `IsLocked` — No `UserStatus` enum; toggle soft-delete via `IsActive`, lockout via `IsLocked`
3. `AuditLog` — Use `long` for PK, not `int`
4. `PermissionService.UpdateRolePermissionsAsync()` — Use `_uow.ExecuteTransactionAsync()` for atomic remove+add; use `byte roleId` (not `UserRole` enum)

## v4.6.9 — Phase 20 BUG-008 Quick Check
- CurrencyCode validation uses `code.Trim().Length != 3` (not `> 10`).

## v4.6.9 — Phase 19 Settings Module Remediations

When fixing Settings Module issues, check for these common patterns:

1. `SaveChangesAsync` in `SetBatchSystemSettingsAsync` — remove it, let service commit
2. `Tax.Update()` missing `UpdateTimestamp()` — add it
3. `SystemSetting.Create()` without Category/DataType guards — add them
4. `TaxConfiguration` index missing `AND [IsActive]` — add it
5. `SetStringAsync()` hardcoding `category: "Print"` — accept category parameter
6. Missing system settings in `DbSeeder` — add them (target: 29)
7. `defaultTaxRate: 15m` in seed — change to `0m`
8. Controller returning `BadRequest` for NotFound — differentiate with `ErrorCodes.NotFound`

---

## v4.10 — 65-Table Schema Quick Fixes

When touching code related to the restructured schema, apply these fixes automatically:

### New Entity Quick-Fix Patterns

1. **`ProductPrices` (multi-currency pricing)** — Replace any `product.RetailPrice` or `product.WholesalePrice` with `product.ProductPrices.FirstOrDefault(p => p.CurrencyId == sessionCurrencyId && p.EffectiveFrom <= today && (!p.EffectiveTo.HasValue || p.EffectiveTo >= today))?.Price`

2. **`InventoryBatches` (FIFO/FEFO)** — Replace any `product.CurrentQuantity` or simple stock deduction with `InventoryBatch` FIFO logic: consume from oldest batch first (`OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)` or `OrderBy(b => b.Id)` based on TrackExpiry).

3. **`Party` entity** — Customers and Suppliers now link to `PartyId` (shared contact data). Replace direct `Customer.Name`/`Customer.Phone`/`Customer.Address` with `customer.Party.Name`/`customer.Party.Phone`/`customer.Party.Address`.

4. **`Unit` independent table** — Replace any `ProductUnit.UnitName` with `productUnit.Unit.Name` and `productUnit.Unit.Symbol`. Units are no longer embedded strings.

5. **`InventoryTransaction`/`InventoryTransactionLine`** — Replace any old `InventoryMovement` reference with `InventoryTransaction` (header) + `InventoryTransactionLine` (detail) pattern.

6. **`WarehouseTransfer`/`WarehouseTransferLine`** — Replace any old `StockTransfer`/`StockTransferItem` with `WarehouseTransfer`/`WarehouseTransferLine`.

### Removed Entity Detection (Fix When Found)

| Old Entity | Replacement |
|------------|-------------|
| `InventoryMovement` | `InventoryTransaction` + `InventoryTransactionLine` |
| `StockTransfer` | `WarehouseTransfer` + `WarehouseTransferLine` |
| `InventoryOperation` | `InventoryTransactions` with appropriate TransactionType |
| `StockWriteOff` | `InventoryAdjustments` with `AdjustmentType = Damage` |
| `CustomerGroup` | REMOVED — not in V1, deferred to V2 |
| `SupplierType` | REMOVED — not in V1, deferred to V2 |
| `PurchaseOrder` | REMOVED — not in V1, deferred to V2 |
| `SalesQuotation` | REMOVED — not in V1, deferred to V2 |
| `Cheque` | REMOVED — not in V1, deferred to V2 |
| `DailyClosure` | REMOVED — not in V1, deferred to V2 |
| `ProductBarcode` | REPLACED by `UnitBarcode` (FK to ProductUnits) |
| `ProductCode` | REMOVED — use auto-increment `Id` only |

### int→smallint FK Type Changes

When you see `int` FK to these tables, change to `smallint`:
- `Roles(Id)` — used as FK in UserRoles, RolePermissions
- `Departments(Id)` — used as FK in Employees
- `Branches(Id)` — used as FK in UserBranches, Warehouses
- `Warehouses(Id)` — used as FK in many entities
- `Currencies(Id)` — used as FK in many entities
- `Taxes(Id)` — used as FK in Products, invoices
- `Units(Id)` — used as FK in ProductUnits

### AuditLog.Id int→bigint

When you see `AuditLog.Id` as `int`, change to `long` (bigint). Also verify:
- `SystemLogs.Id` is `long`
- `AuditLogs` has indexes on `(UserId, CreatedAt DESC)`, `(EntityName, EntityId)`, `(CreatedAt DESC)`
- `SystemLogs.Level` is `tinyint` NOT `nvarchar`

### BaseCurrency Immutability Guard

When you see code that changes `IsBaseCurrency`:
1. The base currency is set at system creation and NEVER changes
2. `Currency.SetAsBaseCurrency()` and `UnsetBaseCurrency()` are domain methods that call `UpdateTimestamp()`
3. The filtered unique index has `AND [IsActive] = 1` guard
4. User-facing UI should NEVER show a "set as base" toggle

### No Purchases Account (Perpetual Inventory)

When you see code referencing a "Purchases" account:
1. All inventory costs go DIRECTLY to Inventory Asset account (not Purchases clearing account)
2. Purchase invoice posts: Dr Inventory / Cr Cash (or AP)
3. NO intermediate Purchases account is used
4. COGS is computed from InventoryBatches at time of sale

---

## 📋 Phase Awareness (Phases 23-31)

The system is currently at **v4.6.9+ with Phases 18-24 completed and Phases 25-31 planned**:

| Phase | Status | Description |
|-------|--------|-------------|
| 23 — Customers Module | ✅ Completed | Customer groups, Account linking, CheckCreditLimit, CustomerType removed |
| 24 — Accounting Integration | ✅ Completed | Auto journal entries for all money ops, COGS (AverageCost), Payment reversals |
| 25 — Products Module | 📝 Planned | Multi-currency pricing (ProductPrices), FIFO batches (InventoryBatches), Unit independent table, Party entity, opening stock via InventoryBatches |
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

### CashBox Fix Patterns (v4.9)

When you see these patterns in CashBox code, fix them automatically:

1. **`CashBox.OpeningBalance` or `CashBox.CurrentBalance`** → These fields were REMOVED from the entity. Replace reference with `box.Account?.CurrentBalance` or remove entirely.
2. **`CashTransaction.BalanceBefore` or `CashTransaction.BalanceAfter`** → These were REPLACED with `RunningBalance`. Change property names.
3. **`CashBox.Deposit()` or `CashBox.Withdraw()`** → These methods were REMOVED. Create `CashTransaction` records directly via `CashTransaction.Create()`.
4. **`sourceBox.CurrentBalance >= amount` in CashTransfer** → This client-side check was REMOVED. Server validates via running balance.
5. **`new CashBox { OpeningBalance = ... }`** → The `OpeningBalance` property doesn't exist. Remove it.

### Features to Fix By Default

When you encounter any code related to these areas, apply fixes automatically:

1. CashBox with OpeningBalance/CurrentBalance → REMOVE both fields, add AccountId FK
2. CashTransaction with BalanceBefore/BalanceAfter → REPLACE with RunningBalance
3. CashTransaction.Create() internal → CHANGE to public
4. Deposit()/Withdraw() methods on CashBox → REMOVE
5. Client-side balance validation → REMOVE (server validates via Account)
6. Missing `AccountId` FK on CashBox → Add it and link to default cash account under "1110 — النقدية"
7. Missing `AccountId` FK on Warehouse → Add it and link to inventory account
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
