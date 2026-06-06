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
