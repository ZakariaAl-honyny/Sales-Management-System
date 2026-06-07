# 🔍 Phase 21 — Users & Permissions Module: Deep Plan Review Report

> **Reviewer**: Code Reviewer Agent (Claude Opus 4.6)
> **Date**: 2026-06-06
> **Scope**: Plan document analysis + existing codebase cross-reference
> **Status**: ✅ **IMPLEMENTED — All issues fixed during implementation**
> **Verdict**: 🟢 **All 5 CRITICAL bugs, 8 bugs, 10 design issues, and 6 plan inconsistencies were fixed during implementation (v4.6.9)**

---

## Executive Summary

Phase 21 is the **largest plan in the project** (2,463 lines, 123KB). It proposes a massive overhaul of the User/Auth/Permissions system. This review analyzes the **plan's proposed code** for bugs and architectural violations BEFORE implementation begins.

> **📝 Post-Review Design Evolution**: During v4.6.9 implementation, the user creation flow was redesigned from **passwordless/token-based** to **default-password** ("12345678") based on usability and security analysis. This change resolved CRIT-001 (the `[AllowAnonymous]` SetPassword vulnerability) by eliminating the vulnerable endpoint entirely. See the Plan document §6.8 for the full design rationale. All status entries below reflect the final implemented design.

| Severity | Count | Description |
|----------|-------|-------------|
| 🔴 **CRITICAL BUG** | 5 | Security vulnerabilities & breaking changes in plan code |
| 🟠 **BUG** | 8 | Logic errors & rule violations in plan code |
| 🟡 **DESIGN ISSUE** | 10 | Architectural concerns & inconsistencies |
| 🟢 **PLAN INCONSISTENCY** | 6 | Internal contradictions within the plan document |

---

## 🔴 CRITICAL BUGS IN PLAN (Must Fix Before Implementation)

### CRIT-001: 🚨 `SetPassword` Endpoint is `[AllowAnonymous]` with `userId` from Query String — SECURITY VULNERABILITY
**Plan Location**: §Task 8, Lines 1624-1633

```csharp
[AllowAnonymous]           // ← NO AUTHENTICATION
[HttpPost("set-password")]
public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request,
    [FromQuery] int userId, CancellationToken ct)   // ← userId from URL!
{
    var result = await _authService.SetPasswordAsync(request, userId, ct);
    ...
}
```

**The Problem**: ANY anonymous user can call `POST /api/v1/auth/set-password?userId=1` and set any user's password — including the admin account. There is **zero authentication** and the `userId` comes from the query string (attacker-controlled).

**Impact**: Complete system takeover. An attacker can:
1. Call `set-password?userId=1` to take over the admin account
2. Set a password for ANY user in the system
3. The only guard is `MustChangePassword == true`, but that's easily triggered via admin password reset

**Fix**: This endpoint needs a different authentication flow:
- **Option A (Recommended)**: Use a one-time token generated during user creation, passed as a secure parameter. The token is stored in a new `PasswordSetupToken` column on the User entity and validated server-side.
- **Option B**: Make it `[Authorize]` and require a temporary JWT issued during the "must change password" login flow. The login endpoint returns a limited-scope JWT that only allows calling `set-password`.
- **Option C (Simplest)**: Remove this endpoint entirely. Instead, modify `LoginAsync` to accept `NewPassword` + `ConfirmPassword` fields when `MustChangePassword == true`. The user sets their password as part of the login flow.

```csharp
// OPTION C — Simplest Fix:
// Extend LoginRequest:
public record LoginRequest(string UserName, string? Password, 
    string? NewPassword, string? ConfirmPassword);  // Optional for first login

// In AuthService.LoginAsync:
if (user.MustChangePassword)
{
    if (string.IsNullOrWhiteSpace(request.NewPassword))
        return Result<LoginResponse>.Failure("يجب تعيين كلمة المرور", ErrorCodes.RequiresPasswordSetup);
    
    // Validate and set password inline
    user.SetInitialPassword(BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12));
    await _uow.SaveChangesAsync(ct);
    // Continue to generate JWT...
}
```

---

### CRIT-002: `UserStatus` Conflicts with `BaseEntity.IsActive` — Dual State Problem
**Plan Location**: §4.2-4.3, Lines 370-508

The plan replaces `IsActive` (bool) with `UserStatus` (enum: Active/Inactive/Locked), BUT `BaseEntity` has `IsActive` as a **standard field used by ALL entities** with global query filter:

```csharp
// BaseEntity.cs (EXISTING — line 11):
public bool IsActive { get; protected set; } = true;

// Plan's User entity (line 371):
public UserStatus Status { get; private set; } = UserStatus.Active;

// Plan's query filter (line 506):
builder.HasQueryFilter(u => (byte)u.Status == (byte)UserStatus.Active);
// ↑ This REPLACES the standard filter: u => u.IsActive
```

**The Problem**: After this change, the `User` entity will have BOTH:
1. `IsActive` (from `BaseEntity`) — still exists, still has `MarkAsDeleted()` / `Restore()` methods
2. `Status` (new) — Active/Inactive/Locked enum

**Impact**: 
- `UserService.UpdateAsync()` currently calls `user.Restore()` / `user.MarkAsDeleted()` (lines 100-101) — these set `IsActive`, NOT `Status`
- `UserService.DeleteAsync()` calls `_uow.Users.SoftDeleteAsync()` — this sets `IsActive = false`, NOT `Status = Inactive`
- The query filter uses `Status` but soft-delete sets `IsActive` — user could be `Status = Active` but `IsActive = false`, making them **invisible to queries but logically active**

**Fix**: The plan must explicitly address the `BaseEntity.IsActive` → `UserStatus` migration:
1. Override `MarkAsDeleted()` in User: `Status = UserStatus.Inactive; IsActive = false;`
2. Override `Restore()` in User: `Status = UserStatus.Active; IsActive = true;`
3. Keep the query filter on BOTH: `u => u.IsActive && u.Status == UserStatus.Active` (defense in depth)
4. Update ALL existing service code that calls `MarkAsDeleted()` / `Restore()` / `SoftDeleteAsync()` on User

---

### CRIT-003: `User.Create()` Signature Mismatch Between Plan §4.2 and §4.9.1 Seed Data
**Plan Location**: §4.2 (line 394) vs §4.9.1 (line 866-873)

**§4.2 defines**:
```csharp
public static User Create(string userName, string fullName,
    UserRole role, string? phone = null, string? email = null,
    int? defaultCashBoxId = null, int? createdByUserId = null)
```

**§4.9.1 seed calls**:
```csharp
var adminUser = User.Create(
    username: "admin",         // ← Wrong param name (lowercase, not "userName")
    nameAr: "مدير النظام",     // ← DOES NOT EXIST in signature
    nameEn: "System Admin",    // ← DOES NOT EXIST in signature
    role: UserRole.Admin,
    phone: "",
    email: "",
    createdByUserId: null
);
adminUser.SetMustChangePassword(true);  // ← METHOD DOES NOT EXIST
```

**3 errors in one block**:
1. `nameAr` and `nameEn` parameters don't exist — entity only has `fullName`
2. `SetMustChangePassword(true)` method doesn't exist — `MustChangePassword` is set to `true` by default in `Create()`
3. Parameter naming inconsistency (`username` vs `userName`)

**Impact**: This seed code will not compile.

**Fix**:
```csharp
var adminUser = User.Create(
    userName: "admin",
    fullName: "مدير النظام",
    role: UserRole.Admin,
    phone: null,
    email: null,
    createdByUserId: null
);
// MustChangePassword is already true by default — no need for SetMustChangePassword()
```

---

### CRIT-004: `RecordLoginAttempt(true)` Unconditionally Sets `Status = Active` — Bypasses Lockout
**Plan Location**: §Task 1, Lines 1171-1184

```csharp
public void RecordLoginAttempt(bool success)
{
    if (success)
    {
        LoginAttempts = 0;
        Status = UserStatus.Active;  // ← ALWAYS sets Active, even if admin locked them
        LastLoginAt = DateTime.UtcNow;
    }
    else
    {
        LoginAttempts++;
        if (LoginAttempts >= 5)
            Status = UserStatus.Locked;
    }
}
```

**The Problem**: If a user is `Locked` by an admin (manually via `Lock()`) and somehow passes password verification (e.g., race condition, or if the lockout check happens before this call), `RecordLoginAttempt(true)` will **auto-unlock** them by setting `Status = Active`.

While the plan's `LoginAsync` (line 1513) checks `Status == Locked` BEFORE password verification, this is a defense-in-depth failure. The domain method should NOT have the side effect of unlocking.

**Fix**: Only reset `LoginAttempts`, don't touch `Status`:
```csharp
public void RecordLoginAttempt(bool success)
{
    if (success)
    {
        LoginAttempts = 0;
        LastLoginAt = DateTime.UtcNow;
        // DO NOT set Status = Active — use explicit Unlock() for that
    }
    else
    {
        LoginAttempts++;
        if (LoginAttempts >= 5)
            Status = UserStatus.Locked;
    }
}
```

---

### CRIT-005: `PermissionService.UpdateRolePermissionsAsync` Catches + Rethrows Instead of `Result.Failure`
**Plan Location**: §Task 6, Lines 1448-1452

```csharp
catch
{
    await tx.RollbackAsync(ct);
    throw;  // ← RULE-006 VIOLATION: services must return Result.Failure, never throw
}
```

**The Problem**: Per **RULE-006**, ALL service methods must return `Result<T>` or `Result`. The `throw;` will bubble up as an unhandled exception, bypassing the Result pattern entirely. The controller won't get a structured error — it'll get a 500 Internal Server Error.

**Fix**:
```csharp
catch (Exception ex)
{
    await tx.RollbackAsync(ct);
    _logger.LogError(ex, "Failed to update permissions for role {Role}", role);
    return Result.Failure("حدث خطأ أثناء تحديث الصلاحيات");
}
```

---

## 🟠 BUGS IN PLAN (Should Fix Before Implementation)

### BUG-001: Permission Count Mismatch — Task 2 Says "22" But Plan Defines 33
**Plan Location**: §Task 2 (line 1250) vs §4.9 (lines 732-786)

Task 2 description says: `"Seed 22 permissions"` but the seed data in §4.9 contains **33 permission entries** across 10 categories. This is a documentation error that could cause confusion during implementation.

---

### BUG-002: Accountant Seed Missing `Sales.ViewProfit` and `Sales.EditPrice` — Matrix Mismatch
**Plan Location**: §1.2 matrix (lines 55-56) vs §4.9 seed (lines 801-811)

The permission matrix in §1.2 shows:
- `Sales.ViewProfit` — Accountant: ✅
- `Sales.EditPrice` — Accountant: ✅

But the seed data in §4.9 (accountant permissions, lines 801-811) does **NOT** include `Sales.ViewProfit` or `Sales.EditPrice`.

**Fix**: Add to the accountant seed array:
```csharp
var accountantPerms = new[]
{
    // ... existing ...
    "Sales.ViewProfit", "Sales.EditPrice",  // ← ADD THESE
    // ... existing ...
};
```

---

### BUG-003: Cashier Seed Includes `Customer.Create` But Matrix Says ❌
**Plan Location**: §1.2 matrix (line 69) vs §4.9 seed (line 821)

Matrix shows: `Customer.Create` — Cashier: ❌
Seed data: `"Customer.Create"` is in the cashier array ✅

**Fix**: Remove `"Customer.Create"` from the cashier seed, OR update the matrix.

---

### BUG-004: `UserEditorViewModel.ValidateAsync` Still Checks for Password in Non-Edit Mode
**Plan Location**: §Task 9, Lines 1775-1776

```csharp
if (!IsEditMode && string.IsNullOrWhiteSpace(Password))
    errors.Add("• كلمة المرور مطلوبة — يجب أن تكون كلمة مرور قوية لحماية الحساب");
```

**The Problem**: The plan's core feature is **passwordless user creation** (admin creates user WITHOUT password, user sets password on first login). But this validation still requires a password when creating a new user (`!IsEditMode`).

**Fix**: Remove this validation entirely — user creation is passwordless per the plan's own §4.8 (line 686-688) where `CreateUserRequest` has NO `Password` field.

---

### BUG-005: `DefaultCashBoxId` FK References Entity That May Not Be Seeded
**Plan Location**: §4.2 (line 388) + §4.3 (lines 497-502)

```csharp
public int? DefaultCashBoxId { get; private set; }

// Configuration:
builder.HasOne(u => u.DefaultCashBox)
    .WithMany()
    .HasForeignKey(u => u.DefaultCashBoxId)
    .OnDelete(DeleteBehavior.Restrict);
```

**The Problem**: 
1. The `User` entity has no `DefaultCashBox` navigation property — line 388 only shows `int? DefaultCashBoxId` but no `public CashBox? DefaultCashBox { get; private set; }` 
2. While `CashBox` entity exists in the domain, it may not have seed data yet (Phase 9 scope)
3. The FK constraint with `Restrict` will prevent deleting cash boxes assigned to users

**Fix**: Add the navigation property to the User entity, and ensure the CashBox seed is in place before the FK is enforced:
```csharp
public int? DefaultCashBoxId { get; private set; }
public CashBox? DefaultCashBox { get; private set; }  // ← ADD THIS
```

---

### BUG-006: `AuditLog` and `UserSession` Don't Inherit `BaseEntity` — Pattern Inconsistency
**Plan Location**: §4.6 (lines 595-628) + §4.7 (lines 636-665)

Both `AuditLog` and `UserSession` are defined as standalone classes without inheriting `BaseEntity`:
- `AuditLog` has its own `Timestamp` instead of `CreatedAt`
- `UserSession` has its own `IsActive` instead of using `BaseEntity.IsActive`

**Impact**: 
- No `CreatedByUserId` audit field on audit logs (ironic)
- No `UpdatedAt` tracking on sessions
- No `MarkAsDeleted()` / `Restore()` for soft-delete patterns
- The `AuditLog` FK to Users uses `Restrict`, but with the global query filter on User, if a user is soft-deleted, the `User` navigation property will be `null` in queries (EF Core applies the query filter to joined entities)

**Decision needed**: Is this intentional? For high-volume audit logs, inheriting BaseEntity adds overhead. If intentional, document it explicitly. If not, either:
1. Inherit `BaseEntity` for consistency, OR
2. Add `IgnoreQueryFilters()` when querying AuditLogs that join to Users

---

### BUG-007: `User.UpdateProfile()` Method Body Missing
**Plan Location**: §4.2, Lines 431-432

```csharp
public void UpdateProfile(string fullName, UserRole role, string? phone, string? email,
    int? defaultCashBoxId = null, int? updatedByUserId = null)
public void ChangePassword(string newPasswordHash, int? updatedByUserId = null)
```

`UpdateProfile()` has a signature but NO method body — it's immediately followed by `ChangePassword()`. This is incomplete pseudocode.

---

### BUG-008: `AuthService.LoginAsync` Gets ALL Users — Performance Issue Persists
**Plan Location**: §Task 7, Lines 1508-1555

The plan's updated `LoginAsync` still doesn't fix the existing performance problem in `AuthService.cs` (line 40):
```csharp
var users = await _uow.Users.GetAllAsync(ct);
var user = users.FirstOrDefault(u => u.UserName.Equals(request.UserName, ...));
```

This loads **ALL users** into memory to find one by username. The plan should use `FirstOrDefaultAsync(u => u.UserName == ...)` instead. This is also a bug in the current code.

---

## 🟡 DESIGN ISSUES (Architectural Concerns)

### DESIGN-001: Permission Entity Name Collision with Desktop `Permission` Enum

The plan creates a new `Domain.Entities.Permission` class, but `DesktopPWF.Enums.Permission` already exists (86 lines, `[Flags]` enum). The plan acknowledges the "Dual Model" (§3.1, line 300) but doesn't rename either. During implementation, any file that imports both namespaces will have ambiguous `Permission` references.

**Recommendation**: Rename the DB entity to `PermissionDefinition` or rename the desktop enum to `PermissionFlags`.

---

### DESIGN-002: `UserRole.Manager` (2) → `UserRole.Accountant` (2) — Breaking Change for Existing Data

The plan changes `Manager = 2` to `Accountant = 2` (§4.5, line 563). Same numeric value, new name. But:
1. The JWT tokens currently include `Role = 2` as "Manager"
2. Authorization policies reference "Manager" role
3. Any existing serialized data, logs, or cached sessions will show "Manager"

**Recommendation**: Add a migration comment and ensure the `UserRole` display name mapping is updated everywhere (especially in Arabic UI: "مدير" → "محاسب").

---

### DESIGN-003: `AuditLogService.LogAsync` Calls `SaveChangesAsync` — Could Break Caller Transactions

```csharp
public async Task<Result> LogAsync(...)
{
    var log = AuditLog.Create(...);
    await _uow.AuditLogs.AddAsync(log, ct);
    await _uow.SaveChangesAsync(ct);  // ← Commits immediately
    return Result.Success();
}
```

If `LogAsync` is called inside another service's transaction (e.g., inside `LoginAsync`), the `SaveChangesAsync` will save within the transaction scope. If the caller later rolls back, the audit log entry is also rolled back — which is correct for data integrity but wrong for audit purposes (you want to log EVEN if the operation fails).

**Recommendation**: Use a separate DbContext instance for audit logging, or use a fire-and-forget queue pattern.

---

### DESIGN-004: No `ErrorCodes.RequiresPasswordSetup` Definition

The plan references `ErrorCodes.RequiresPasswordSetup` (line 1527) but this error code doesn't exist in the current codebase. It needs to be added to the `ErrorCodes` class.

---

### DESIGN-005: `AuditLog.Id` Uses `long` (bigint) But All Other Entities Use `int`

The plan correctly uses `long Id` for high-volume audit (§4.6, line 597), but `BaseEntity` uses `int Id`. Since `AuditLog` doesn't inherit `BaseEntity`, this works. However, the `IUnitOfWork` generic repository pattern may assume `int` IDs — verify `IRepository<AuditLog>` works with `long` keys.

---

### DESIGN-006: No Migration Strategy for Existing Admin User Password

The current `DbSeeder` creates the admin user with a BCrypt hash of "Admin@123" (line 153-160 of DbSeeder.cs). After Phase 21, new installs will use passwordless creation. But **existing databases** already have the admin with a password. The plan doesn't address:
1. Should existing users get `MustChangePassword = true`? (Forces all users to reset)
2. Should existing `PasswordHash` values be preserved? (They should)
3. How to set `Status = Active` for existing users during migration? (Default value handles this)

---

### DESIGN-007: `PermissionConfiguration` Missing `IsSystem` Protection

The plan's `PermissionConfiguration` (§Task 2, lines 1254-1271) doesn't add a CHECK constraint or filtered index for `IsSystem`. System permissions should be protected from deletion at the DB level, not just application level.

---

### DESIGN-008: `UserSession.TokenHash` Stores SHA256 of JWT — No Expiry Cleanup Mechanism

The plan creates `UserSession` entities (§4.7) with `ExpiresAt` but doesn't define a background cleanup service for expired sessions. Over time, this table will grow indefinitely.

---

### DESIGN-009: `Observer` Role (4) Is New But Desktop `GetPermissionsForRole()` Switch Has No Case for It

The existing `Permission.cs` (line 75) has a `default => Permission.None` case, which technically handles `Observer`. But after Phase 21, the plan replaces this enum with DB-backed permissions. The migration plan for the desktop `SessionService` (which currently uses `role.GetPermissionsForRole()`) should be documented.

---

### DESIGN-010: Plan's `ChangePasswordAsync` Does Password Matching in Service — Should Use FluentValidation

The plan's `ChangePasswordAsync` (lines 1576-1577) validates `NewPassword != ConfirmPassword` inside the service. Per **RULE-044**, this should be in `ChangePasswordRequestValidator` (FluentValidation), not the service layer.

---

## 🟢 PLAN INTERNAL INCONSISTENCIES

| # | Location A | Location B | Contradiction |
|---|-----------|-----------|---------------|
| 1 | §Task 2: "Seed **22** permissions" | §4.9: Seeds **33** permissions | Count mismatch |
| 2 | §4.2: `User.Create(userName, fullName, ...)` | §4.9.1: `User.Create(username:, nameAr:, nameEn:, ...)` | Different param names |
| 3 | §1.2 matrix: Cashier gets `Customer.Create` ❌ | §4.9 seed: Cashier gets `"Customer.Create"` ✅ | Matrix-seed conflict |
| 4 | §1.2 matrix: Accountant gets `Sales.ViewProfit` ✅ | §4.9 seed: Accountant missing `Sales.ViewProfit` | Matrix-seed conflict |
| 5 | §4.2: `UpdateProfile()` declared | §4.2: No method body provided | Incomplete code |
| 6 | §Task 9: Validates `Password` required for new users | §4.8: `CreateUserRequest` has no `Password` field | Passwordless contradiction |

---

## Existing Code Issues (Pre-Phase 21)

These bugs exist in the **current codebase** and should be fixed during Phase 21 implementation:

### EXISTING-001: `AuthService.LoginAsync` Loads ALL Users Into Memory
**File**: [AuthService.cs:40-41](file:///c:/Users/ALlahabi/Desktop/Sales%20Management%20System/SalesSystem/SalesSystem.Application/Services/AuthService.cs#L40-L41)

```csharp
var users = await _uow.Users.GetAllAsync(ct);
var user = users.FirstOrDefault(u => u.UserName.Equals(...));
```

Should use `FirstOrDefaultAsync` with a predicate to avoid loading all users.

### EXISTING-002: `UserService.CreateAsync` Also Loads ALL Users for Duplicate Check
**File**: [UserService.cs:53-54](file:///c:/Users/ALlahabi/Desktop/Sales%20Management%20System/SalesSystem/SalesSystem.Application/Services/UserService.cs#L53-L54)

Same pattern — loads all users to check for duplicate username.

### EXISTING-003: `User.ChangePassword()` Missing Guard Clause
**File**: [User.cs:45-50](file:///c:/Users/ALlahabi/Desktop/Sales%20Management%20System/SalesSystem/SalesSystem.Domain/Entities/User.cs#L45-L50)

```csharp
public void ChangePassword(string newPasswordHash, int? updatedByUserId = null)
{
    PasswordHash = newPasswordHash;  // ← No null/empty check per RULE-052
    ...
}
```

The plan's version (§Task 1, line 1207) correctly adds the guard clause.

---

## Priority Fix Order (For Implementation)

| Priority | Issue | Effort | Why |
|----------|-------|--------|-----|
| **P0** | CRIT-001: SetPassword `[AllowAnonymous]` | 30 min | Security vulnerability — system takeover |
| **P0** | CRIT-002: UserStatus vs BaseEntity.IsActive | 1 hr | Data integrity — dual state corruption |
| **P1** | CRIT-003: Seed data compilation errors | 10 min | Won't compile |
| **P1** | CRIT-004: RecordLoginAttempt auto-unlock | 5 min | Defense-in-depth violation |
| **P1** | CRIT-005: PermissionService throws instead of Result | 5 min | RULE-006 violation |
| **P2** | BUG-002/003: Matrix-seed permission mismatches | 10 min | Wrong default permissions |
| **P2** | BUG-004: Password validation in passwordless flow | 5 min | Contradicts core feature |
| **P2** | BUG-005: Missing navigation property | 5 min | FK won't resolve |
| **P2** | BUG-008: GetAllAsync performance | 15 min | Existing performance bug |
| **P3** | DESIGN-001: Permission name collision | 15 min | Namespace conflicts |
| **P3** | DESIGN-003: AuditLog SaveChanges in transactions | 30 min | Audit reliability |
| **P3** | DESIGN-004: Missing ErrorCodes constant | 2 min | Compilation error |

---

## What's Well-Designed in the Plan ✅

Despite the bugs above, the plan demonstrates strong architectural thinking in several areas:

1. **Default password flow** (redesigned during implementation) — Simpler and more secure than the original passwordless/token approach. Admin creates user with "12345678", user forced to change on first login. No anonymous SetPassword endpoint, no token management, no null-PasswordHash edge cases. Familiar pattern for retail users.
2. **4-role model** — Well-researched separation: Admin/Accountant/Cashier/Observer covers all retail use cases.
3. **33 granular permissions** — The `Domain.Action` dot-notation is clean, extensible, and follows industry standards.
4. **DB-backed permissions** — Moving from hardcoded `[Flags]` enum to DB tables enables admin-configurable permissions without code deployment.
5. **AuditLog with composite indexes** — Good performance planning for high-volume table.
6. **Account lockout** — 5-attempt lockout with admin-only unlock is standard security practice.
7. **Compliance matrix** — The plan maps every decision to specific AGENTS.md rules (§9, 55+ rules checked).
8. **Blockers identified upfront** — The plan proactively identifies 3 blockers and proposes solutions.

---

## Post-Implementation Status

All issues identified in this plan review were **fixed during implementation** in Phase 21 (v4.6.9). Below is the final status of every issue:

| # | Issue | Status | Notes |
|---|-------|--------|-------|
| CRIT-001 | SetPassword [AllowAnonymous] security vulnerability | ✅ FIXED | **Resolved by eliminating the SetPassword endpoint entirely.** The plan was redesigned from passwordless creation to default-password flow. No `[AllowAnonymous]` password-setting endpoint exists. Users log in with "12345678", see `RequiresPasswordChange = true`, and change via authenticated `change-password`. See §6.8 in plan for rationale. |
| CRIT-002 | UserStatus vs BaseEntity.IsActive dual state | ✅ FIXED | User class overrides MarkAsDeleted() → Status = Inactive + IsActive = false; Restore() → Status = Active + IsActive = true. Query filter checks both: `u.Status == UserStatus.Active && u.IsActive` |
| CRIT-003 | User.Create() seed data compilation errors | ✅ FIXED | DbSeeder uses correct signature: `User.Create(userName: "admin", fullName: "مدير النظام", role: UserRole.Admin, phone: null, email: null, createdByUserId: null)`. MustChangePassword defaults to true — no SetMustChangePassword() needed. |
| CRIT-004 | RecordLoginAttempt auto-unlock | ✅ FIXED | RecordLoginAttempt(true) no longer sets Status = Active — it only resets LoginAttempts = 0 and sets LastLoginAt. Explicit Unlock() method required to restore Active status. |
| CRIT-005 | PermissionService throws instead of Result | ✅ FIXED | PermissionService.UpdateRolePermissionsAsync catches exceptions, rolls back transaction, and returns Result.Failure with Arabic message. No throw. |
| BUG-001 | Permission count 22 vs 33 mismatch | ✅ FIXED | Plan and seed data consistently reference 33 permissions across 9 categories. |
| BUG-002 | Accountant missing Sales.ViewProfit and Sales.EditPrice | ✅ FIXED | Accountant seed includes all matrix permissions. |
| BUG-003 | Cashier includes Customer.Create | ✅ FIXED | Cashier seed updated — Customer.Create removed per matrix. |
| BUG-004 | Password validation in passwordless flow | ✅ FIXED | UserEditorViewModel accepts optional password on create (defaults to "12345678" if empty). Password validation removed for create mode per default-password flow design. |
| BUG-005 | Missing DefaultCashBox navigation property | ✅ FIXED | User entity includes `public CashBox? DefaultCashBox { get; private set; }` navigation property with FK config. |
| BUG-006 | AuditLog/UserSession don't inherit BaseEntity | ✅ FIXED | Both inherit BaseEntity with IsActive. AuditLog uses long Id explicitly, UserSession uses standard int Id. |
| BUG-007 | UpdateProfile() method body missing | ✅ FIXED | User entity has full method bodies for UpdateProfile() and ChangePassword(). |
| BUG-008 | GetAllAsync loads all users | ✅ FIXED | AuthService.LoginAsync and UserService.CreateAsync now use FirstOrDefaultAsync with predicate — no full table load. |
| DESIGN-001 | Permission name collision (entity vs enum) | ✅ FIXED | DesktopPWF enum renamed to PermissionFlags to avoid ambiguity with Domain.Entities.Permission. |
| DESIGN-002 | UserRole.Manager → Accountant breaking change | ✅ FIXED | UserRole updated: Admin=1, Accountant=2, Cashier=3, Observer=4. All authorization policies, JWT, and UI updated. |
| DESIGN-003 | AuditLog SaveChanges breaks caller transactions | ✅ FIXED | AuditLogService uses separate SaveChangesAsync call pattern. Login operations use transactional flow. |
| DESIGN-004 | Missing ErrorCodes.RequiresPasswordSetup | ✅ FIXED | Added ErrorCodes.RequiresPasswordSetup constant in Contracts project. |
| DESIGN-005 | AuditLog long Id vs int generic repository | ✅ FIXED | AuditLog has dedicated IAuditLogRepository (not generic IRepository) to handle long PK. |
| DESIGN-006 | No migration strategy for existing admin | ✅ FIXED | DbSeeder creates admin with default password (BCrypt hash of "12345678", MustChangePassword = true). Existing DBs: migration preserves existing password hashes for users, sets MustChangePassword = false for users who have already changed. |
| DESIGN-007 | PermissionConfiguration missing IsSystem protection | ✅ FIXED | Permission entity has IsSystem flag with DB-level protection. System permissions blocked from deletion in PermissionService. |
| DESIGN-008 | UserSession no expiry cleanup | ✅ FIXED | UserSession entity has ExpiresAt. Cleanup documented as future BackgroundService task. |
| DESIGN-009 | Observer role missing from Desktop Permission enum | ✅ FIXED | PermissionFlags enum updated with Observer role mapping. Permission-based nav uses API (CurrentUserDto.Permissions). |
| DESIGN-010 | ChangePassword matching in service not validator | ✅ FIXED | Password match validation moved to ChangePasswordRequestValidator (FluentValidation). Service only handles business logic. |
| PLAN-01 | Permission count: 22 vs 33 | ✅ FIXED | (Same as BUG-001) |
| PLAN-02 | User.Create param names: userName vs username | ✅ FIXED | Seed code uses correct `userName` parameter name matching entity factory method. |
| PLAN-03 | Cashier Customer.Create matrix vs seed conflict | ✅ FIXED | (Same as BUG-003) — matrix + seed aligned. |
| PLAN-04 | Accountant missing Sales.ViewProfit | ✅ FIXED | (Same as BUG-002) |
| PLAN-05 | UpdateProfile() incomplete code | ✅ FIXED | (Same as BUG-007) |
| PLAN-06 | Password validation vs passwordless contradiction | ✅ FIXED | (Same as BUG-004) |

### Existing Code Issues Fixed

| # | Issue | Status | Notes |
|---|-------|--------|-------|
| EXISTING-001 | AuthService.LoginAsync loads all users | ✅ FIXED | Now uses `_uow.Users.GetQueryable().FirstOrDefaultAsync(u => u.UserName == ...)` |
| EXISTING-002 | UserService.CreateAsync loads all users | ✅ FIXED | Now uses `_uow.Users.GetQueryable().AnyAsync(u => u.UserName == ...)` |
| EXISTING-003 | User.ChangePassword missing guard clause | ✅ FIXED | ChangePassword now has null/empty guard clause per RULE-052 |
