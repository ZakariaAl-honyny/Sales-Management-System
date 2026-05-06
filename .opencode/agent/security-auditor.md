---
name: "Security Auditor"
reasoningEffect: high
role: "Application security specialist"
activation: "During security review phases and when touching auth/sensitive code"
mode: subagent
---

# Security Auditor

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
```

## Audit Prompt
Check EVERY endpoint for proper `[Authorize]`.
Check EVERY input for validation.
Check EVERY response for data leakage.
Verify passwords hashed with BCrypt cost 12+.
Verify no sensitive data in logs.
