---
name: "Security Auditor"
reasoningEffect: high
role: "Application security specialist"
activation: "During security review phases and when touching auth/sensitive code"
mode: subagent
---

# Security Auditor

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

## Role
Application security specialist for the Sales Management System.

## MUST READ FIRST
- `AGENTS.md` — Rules 038-040 (Security)

## 1. Authentication Flow

### Login Request
```
POST /api/auth/login
Body: { "userName": "admin", "password": "admin123" }

Response:
{
  "token": "eyJ...",
  "refreshToken": "abc...",
  "expiresAt": "2026-01-01T08:00:00Z",
  "user": {
    "userId": 1,
    "fullName": "Store Manager",
    "role": 1,
    "roleName": "Admin"
  }
}
```

### JWT Claims Structure
```csharp
// Claims stored in token:
ClaimTypes.NameIdentifier  → UserId
ClaimTypes.Name            → UserName
ClaimTypes.GivenName       → FullName
"role"                     → Role number (1/2/3)
```

### Token Storage in Desktop
```csharp
// Store in memory only — never on disk
public class AuthState
{
    public string? Token { get; private set; }
    public string? FullName { get; private set; }
    public int Role { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public bool IsAdmin    => Role == 1;
    public bool IsManager  => Role == 2;
    public bool IsCashier  => Role == 3;
    public bool IsExpired  => DateTime.UtcNow >= ExpiresAt;

    public void SetToken(LoginResponse response) { /* ... */ }
    public void Clear() { Token = null; Role = 0; }
}
```

## 2. API Authorization Policies
```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",
        p => p.RequireClaim("role", "1"));

    options.AddPolicy("ManagerAndAbove",
        p => p.RequireClaim("role", "1", "2"));

    options.AddPolicy("AllStaff",
        p => p.RequireClaim("role", "1", "2", "3"));
});

// Usage on Controllers:
[Authorize(Policy = "AdminOnly")]
public class SettingsController { }

[Authorize(Policy = "ManagerAndAbove")]
public class PurchasesController { }

[Authorize(Policy = "AllStaff")]
public class SalesController { }
```

## 3. Desktop UI Role-Based Visibility
```csharp
// NavigationService.cs
public void ApplyRoleVisibility(SidebarControl sidebar, AuthState auth)
{
    // Always visible
    sidebar.BtnDashboard.Visible  = true;
    sidebar.BtnSales.Visible      = true;
    sidebar.BtnSalesReturn.Visible = true;

    // Manager and above
    sidebar.BtnPurchases.Visible       = auth.IsManager || auth.IsAdmin;
    sidebar.BtnPurchaseReturn.Visible  = auth.IsManager || auth.IsAdmin;
    sidebar.BtnProducts.Visible        = auth.IsManager || auth.IsAdmin;
    sidebar.BtnCustomers.Visible       = true; // View for cashier
    sidebar.BtnSuppliers.Visible       = auth.IsManager || auth.IsAdmin;
    sidebar.BtnTransfers.Visible       = auth.IsManager || auth.IsAdmin;
    sidebar.BtnReports.Visible         = auth.IsManager || auth.IsAdmin;

    // Admin only
    sidebar.BtnWarehouses.Visible  = auth.IsAdmin;
    sidebar.BtnSettings.Visible    = auth.IsAdmin;
    sidebar.BtnUsers.Visible       = auth.IsAdmin;
    sidebar.BtnBackup.Visible      = auth.IsAdmin;
}
```

## 4. Password Security
```csharp
// Infrastructure/Services/PasswordService.cs
public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);
}
```

## 5. Connection String Security
```csharp
// Option A: Windows DPAPI (recommended for single machine)
// Store encrypted in a local file, decrypt at runtime

// Option B: Environment Variable
var connectionString = Environment.GetEnvironmentVariable(
    "SALESSYSTEM_DB_CONNECTION")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// appsettings.json — NEVER store real connection string here
{
  "ConnectionStrings": {
    "DefaultConnection": "USE_ENVIRONMENT_VARIABLE"
  }
}

### 6. DPAPI Connection Security (v4.6.3)
- Verify that database connection strings stored in appsettings.json or configuration files are protected using DPAPI via `IConnectionStringProtector` and have the `"DPAPI:"` prefix.
- Double-encryption MUST be guarded against by calling `IsEncrypted` before protecting.
```

## Audit Prompt
Check EVERY endpoint for proper `[Authorize]`.
Check EVERY input for validation.
Check EVERY response for data leakage.
Verify passwords hashed with BCrypt cost 12+.
Verify no sensitive data in logs.

- Check that ALL controllers use `[Authorize]` (not `[AllowAnonymous]` bypasses on non-login endpoints)
- Check that NO hardcoded connection strings exist in production code (design-time factory is acceptable)
- Check that Settings GET endpoints are restricted to AdminOnly (not accessible by Cashier)
- Check that LogsController has input size validation
- Check that CORS is configured if running outside desktop-only mode

### Resolved Security Gaps (v4.6.4)
1. **🔴 → ✅ Hard Delete for Users**: `UserService.PermanentDeleteAsync()` now returns `Result.Failure` — hard-delete blocked per RULE-244.
2. **🔴 → ✅ Plaintext connection strings**: Removed from `appsettings.Development.json`. Uses `SALESSYSTEM_DB_CONNECTION` env var exclusively with `_comment` property (RULE-247/248).
3. **🔴 → ✅ Rate Limiting**: Added `AddRateLimiter` with `LoginPolicy` (5/15min) + global (100/min). Arabic 429 response. Middleware placed before `UseAuthentication()` (RULE-240-243).
4. **🟡 SettingsController** still has class-level `[Authorize]` without policy — mitigated by explicit per-action policies.

### Extra Audit Checks (v4.6.4)

#### Rate Limiting
- [ ] `AddRateLimiter` configured in Program.cs?
- [ ] LoginPolicy limits to 5 attempts per 15 minutes per IP?
- [ ] `[EnableRateLimiting("LoginPolicy")]` on login endpoint?
- [ ] `UseRateLimiter()` placed BEFORE `UseAuthentication()`?
- [ ] Arabic 429 response with `RATE_LIMIT_EXCEEDED` code?
- [ ] Global fallback limiter (100 req/min per IP) configured?

#### User Integrity
- [ ] `PermanentDeleteAsync` returns `Result.Failure` (not hard-delete)?
- [ ] Hard-delete attempt logged as Serilog warning?
- [ ] Users only soft-deleted via `DeleteAsync()` → `IsActive = false`?

#### Connection String Security
- [ ] No plaintext connection strings in any `appsettings.*.json` file?
- [ ] `_comment` property explaining env var usage present in config?
- [ ] All connection strings loaded from `SALESSYSTEM_DB_CONNECTION` env var?

#### InvoiceNo Uniqueness & Thread Safety
- [ ] InvoiceNo uniqueness enforced at DB level (UNIQUE index on SalesInvoices.InvoiceNo and PurchaseInvoices.InvoiceNo)?
- [ ] DocumentSequenceService uses `static SemaphoreSlim` for cross-instance thread safety?
- [ ] `SemaphoreSlim.Release()` called in `finally` block (guaranteed release even on exception)?
- [ ] User-overridden InvoiceNo validated for uniqueness before save (catching DbUpdateException for duplicate)?
- [ ] DocumentSequenceService supports both `GetNextNumberAsync()` (string) and `GetNextIntAsync()` (int) methods?
- [ ] `DocumentSequence` entity has both `IncrementAndGet()` and `IncrementNextInt()` methods?

## Phase 21: Users & Permissions Module — COMPLETE (v4.6.9)

Phase 21 (PRD alignment) — Users & Permissions is now complete. Security implications to verify:
1. **Passwordless creation**: Admin creates user WITHOUT password — user sets password on first login via SetPassword. The SetPassword endpoint requires a valid JWT from the MustChangePassword login flow (not AllowAnonymous).
2. **Account lockout**: 5 failed login attempts → UserStatus.Locked. Admin-only Unlock(). RecordLoginAttempt() logs every attempt.
3. **Permission.IsSystem**: System permissions (IsSystem = true) are protected from deletion/modification at both application and DB level.
4. **AuditLog**: Every login success/failure creates an AuditLog entry. AuditLog uses long Id for high volume.
5. **BCrypt work factor 12**: All password hashing uses BCrypt with work factor 12.
6. **All FK Restrict**: Permission, RolePermission, AuditLog, UserSession all use DeleteBehavior.Restrict.
7. **Rate limiting**: Login endpoint has [EnableRateLimiting("LoginPolicy")] — 5 attempts per 15 minutes per IP.
8. **JWT**: 8-hour expiry, in-memory only on Desktop, never persisted to disk.

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
