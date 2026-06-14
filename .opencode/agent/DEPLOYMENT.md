# Deployment Guide
# Sales Management System — v1.0

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

---

## 1. API as Windows Service

```csharp
// SalesSystem.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

// Run as Windows Service
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "SalesSystem API";
});

// ... rest of configuration

// Install command (run as Administrator):
// sc create "SalesSystemAPI"
//   binpath="C:\SalesSystem\Api\SalesSystem.Api.exe"
//   start=auto
// sc start "SalesSystemAPI"
```

## 2. Inno Setup Script

```pascal
; SalesSystem_Setup.iss

[Setup]
AppName=Sales Management System
AppVersion=1.0.0
AppPublisher=Your Company Name
DefaultDirName={autopf}\SalesSystem
DefaultGroupName=Sales Management System
OutputBaseFilename=SalesSystem_Setup_v1.0.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[Files]
; API (Windows Service)
Source: "publish\api\*"; DestDir: "{app}\Api"; Flags: recursesubdirs

; Desktop Application
Source: "publish\desktop\*"; DestDir: "{app}\Desktop"; Flags: recursesubdirs

; SQL Setup Script
Source: "setup\CreateDatabase.sql"; DestDir: "{app}\Setup"

[Icons]
Name: "{group}\Sales System"; Filename: "{app}\Desktop\SalesSystem.Desktop.exe"
Name: "{commondesktop}\Sales System"; Filename: "{app}\Desktop\SalesSystem.Desktop.exe"

[Run]
; Install and start the API Windows Service
Filename: "sc.exe"; Parameters: "create ""SalesSystemAPI"" binpath=""{app}\Api\SalesSystem.Api.exe"" start=auto"; Flags: runhidden
Filename: "sc.exe"; Parameters: "start SalesSystemAPI"; Flags: runhidden

[UninstallRun]
Filename: "sc.exe"; Parameters: "stop SalesSystemAPI"; Flags: runhidden
Filename: "sc.exe"; Parameters: "delete SalesSystemAPI"; Flags: runhidden
```

## 3. Connection String Security

```csharp
// Set during installation via Inno Setup Pascal script:
// [Code] section sets environment variable with connection string

// Read in API:
var connectionString =
    Environment.GetEnvironmentVariable("SALESSYSTEM_CONNECTION")
    ?? configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException(
        "Database connection string not configured");
```

```json
// appsettings.json — NEVER store real connection string here
{
  "ConnectionStrings": {
    "DefaultConnection": "USE_ENVIRONMENT_VARIABLE"
  }
}

## v4.10 — Schema Restructuring Migration (65 Tables)

### Removed Tables (17 — must drop or migrate)
1. `InventoryMovements` → migrate to `InventoryTransactions` + `InventoryTransactionLines`
2. `StockTransfers` / `StockTransferItems` → migrate to `WarehouseTransfers` / `WarehouseTransferLines`
3. `InventoryOperations` → map to `InventoryTransactions` with appropriate TransactionType
4. `StockWriteOffs` → map to `InventoryAdjustments` with AdjustmentType = Damage
5. `ProductBarcodes` → migrate to `UnitBarcodes` (if exists)
6. `CashTransactions` → migrate to `ReceiptVouchers` / `PaymentVouchers`
7. `CustomerGroups` → drop (deferred to V2)
8. `SupplierTypes` → drop (deferred to V2)
9. `SalesQuotations` → drop (deferred to V2)
10. `PurchaseOrders` → drop (deferred to V2)
11. `Cheques` → drop (deferred to V2)
12. `DailyClosures` → drop (deferred to V2)
13. `CustomerPayments` → migrate to `CustomerReceipts`
14. `SupplierPayments` → keep as `SupplierPayments` (renamed columns)
15. `InvoiceSettings` / `GeneralSettings` → merge into `SystemSettings`
16. Old `ProductPrices` (if existed with different schema) → replace with new multi-currency schema
17. `CustomerLedger` / `SupplierLedger` → remove (balance via Account)

### Added Tables (8 — must create EF migrations)
1. `Parties` — shared contact data (Name, Phone, Email, Address, TaxNumber, Notes)
2. `Units` — independent lookup table (smallint PK, Name, Symbol, IsSystem, IsActive)
3. `ProductPrices` — multi-currency pricing per (ProductUnitId, CurrencyId)
4. `InventoryBatches` — FIFO/FEFO batch tracking (BatchNo, QuantityReceived, QuantityRemaining, UnitCost)
5. `InventoryTransactions` — header for ALL inventory movements (TransactionType, WarehouseId, ReferenceType, ReferenceId)
6. `InventoryTransactionLines` — detail lines for each transaction (ProductId, ProductUnitId, BatchId, Quantity, UnitCost, TotalCost)
7. `WarehouseTransfers` — header for warehouse-to-warehouse transfers (FromWarehouseId, ToWarehouseId)
8. `WarehouseTransferLines` — detail lines with BatchId, Quantity, UnitCost

### Seed Data Updates
1. **Units**: Seed 7 default units (حبة/PCS, كرتون/CTN, كيلو/KG, جرام/G, لتر/L, متر/M, بالة/BAL) with `IsSystem = true`
2. **Currencies**: Seed local currency + USD with `IsSystem = true`; set IsBaseCurrency on local currency
3. **Parties**: Auto-create default "نقدي" Party for default cash customer/supplier
4. **SystemSettings**: Update CostingMethod to use InventoryBatches-based costing
5. **SystemAccountMappings**: Add PurchaseReturnAccountId mapping (1632)

### FK Type Changes (int → smallint)
- `Roles.Id` → smallint (was int): update UserRoles.RoleId, RolePermissions.RoleId
- `Departments.Id` → smallint: update Employees.DepartmentId
- `Branches.Id` → smallint: update UserBranches.BranchId, Warehouses.BranchId
- `Warehouses.Id` → smallint: update ALL FK references across 20+ tables
- `Currencies.Id` → smallint: update ALL FK references across 15+ tables
- `Taxes.Id` → smallint: update Products.TaxId, invoice TaxId columns
- `Units.Id` → smallint: update ProductUnits.UnitId
- `AccountCategories.Id` → smallint: update Accounts.CategoryId

**Migration strategy**: Create new smallint FK columns alongside old int columns → populate from lookup → drop old int columns → rename new columns.

### AuditLog/SystemLog Type Changes
- `AuditLogs.Id` → bigint (was int)
- `SystemLogs.Id` → bigint (was int)
- `SystemLogs.Level` → tinyint (was nvarchar)

### EF Migration Commands
```powershell
# From Infrastructure project directory:
dotnet ef migrations add Phase25_ProductsModule_v2 --context SalesDbContext
dotnet ef migrations add Phase26_WarehousesModule --context SalesDbContext
dotnet ef migrations add Phase27_InventoryTransaction --context SalesDbContext
dotnet ef migrations add Phase28_Party_Units_Upgrade --context SalesDbContext

# Apply all pending:
dotnet ef database update --context SalesDbContext
```

## Phase 21: Users & Permissions Module — COMPLETE (v4.6.9)

Phase 21 (PRD alignment) — Users & Permissions is now complete. Deployment impact:
- **EF Migration**: `Phase21_UsersAndPermissions` must be applied — creates Permissions, RolePermissions, AuditLogs, UserSessions tables, alters Users table (adds Status, Phone, Email, Avatar, DefaultCashBoxId, LoginAttempts, LastLoginAt, MustChangePassword columns)
- **Existing users**: Migration sets Status = Active for all existing users, preserves existing password hashes
- **Default admin**: Seeded passwordless — first login requires password set (MustChangePassword = true)
- **New dependencies**: AuditLogService, PermissionService registered in DI
- **API changes**: 10 new endpoints (see CHANGELOG.md)
- **Desktop UI**: 5 new/updated screens, StatusBar changes, permission-based nav filtering
- **Rate limiting**: Applies to login endpoint — 5 attempts/15 min per IP

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