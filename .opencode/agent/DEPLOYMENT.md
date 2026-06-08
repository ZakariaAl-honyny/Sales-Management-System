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