# Phase 21 — Users & Permissions Module Implementation Plan

> **Version**: 2.1  
> **Scope**: Complete Users, Roles, Permissions, and Security system with 9 new entities  
> **Dependencies**: Phase 20 (Currencies), Phase 22 (Chart of Accounts) — no blocking dependencies  
> **Design Principle**: Passwordless user creation with forced initial password set, BCrypt hashing, 4-role model, 33 DB-backed permission codes

---

## Summary

Phase 21 builds the complete security foundation for the system. It introduces 9 tables (Departments, Employees, Roles, Users, UserRoles, Permissions, RolePermissions, UserBranches, UserSessions) and replaces the existing hardcoded 3-role enum + `[Flags]` Permission enum with a dynamic DB-driven model supporting 4 roles and 33 granular permission codes. The design follows a party-model pattern where every person in the system is a `Party` (storing shared contact data), with `Employees` and `Users` layered on top. Authentication uses BCrypt (work factor 12), account lockout after 5 failed attempts, session tracking, and audit logging for all security events.

---

## 1. Key Entities

### 1.1 Departments (`smallint` PK)
Lightweight organizational unit lookup. Has Name, Description, IsActive, audit fields. Departments are `smallint` because they are low-volume reference data (< 100 entries).

### 1.2 Parties (shared base for all people)
Stores common contact data: Name, Phone, Email, Address, TaxNumber, Notes. Used by Customers, Suppliers, and Employees. This avoids duplicating address/phone data across person-type entities. Inherits `ActivatableEntity` (IsActive + audit fields).

### 1.3 Employees (`int` PK, PartyId FK)
Links to Parties for shared contact data. Has DepartmentId (optional FK → Departments), AccountId (optional FK → Accounts, for payroll), EmployeeNo (int, sequential), HireDate (date), Salary (decimal(18,2)), Notes. An employee may or may not have a system User account.

### 1.4 Roles (`smallint` PK)
Role definitions: Name, Description, IsActive. Four roles are seeded:
- **Admin** (1) — full system access
- **Accountant** (2) — financial operations
- **Cashier** (3) — sales/cash operations only
- **Observer** (4) — read-only access

### 1.5 Users (`int` PK, EmployeeId FK → Employees)
The login identity. Key design decisions:
- **EmployeeId** is nullable — a user can exist without being an employee (e.g., external auditor), though seeded admin links to a default employee record.
- **UserName** is unique `nvarchar(50)`.
- **PasswordHash** is `nvarchar(256)` — stores BCrypt hash.
- **MustChangePassword** (bit) — `true` by default on new user creation, forces password change on first login.
- **LoginAttempts** (`smallint`) — tracks consecutive failed logins.
- **IsLocked** (bit) — set to `true` when `LoginAttempts >= 5`. Cleared by admin or auto-unlock timer.
- **LastLoginAt** (`datetime2` nullable) — updated on every successful login.

**Critical design decision**: Users are created WITHOUT a password by default. `PasswordHash` is set to an empty hash placeholder, and `MustChangePassword` is `true`. The first login attempt returns a special `RequiresPasswordSetup` error. The user then completes the login with new password + confirm fields in a single request. This avoids the security vulnerability of having an `[AllowAnonymous]` set-password endpoint.

### 1.6 UserRoles (junction table, Entity base — no audit)
Maps Users to Roles. Unique constraint on `(UserId, RoleId)`. A user can have multiple roles (e.g., Cashier + Observer for specific screens).

### 1.7 Permissions (standalone entity, `int` PK)
Not role-based — permissions are independent codes organized by category:
- **Code**: `nvarchar(100)` unique, dot-notation format (e.g., `Sales.View`, `Purchase.Post`).
- **DisplayName**: Arabic display name for admin UI.
- **Category**: Grouping label (Sales, Purchases, Inventory, Customers, Suppliers, Products, Reports, Accounting, System, Operations, Audit).

33 seeded permission codes follow a `{Module}.{Action}` pattern with actions: View, Create, EditDraft, Post, Cancel, ViewProfit, EditPrice, Transfer, Adjust, Manage, Close, Delete.

### 1.8 RolePermissions (junction table, Entity base)
Maps Roles to Permissions. Unique constraint on `(RoleId, PermissionId)`. Defines which permissions each role has. The seeded matrix is explicitly defined in the seed data (see §4).

### 1.9 UserBranches (junction table, Entity base)
Maps Users to Branches (`smallint` FK). Restricts a user's data visibility to specific branches. If no UserBranch records exist, the user sees all branches (global access).

### 1.10 UserSessions (audited entity)
Tracks active user sessions for security monitoring:
- **SessionToken**: `nvarchar(200)` — the JWT token hash or session identifier.
- **DeviceName**: user-friendly device description.
- **IpAddress**: login source IP.
- **UserAgent**: browser/OS info.
- **LastActivityAt**: updated on each API call.
- **ExpiresAt**: session expiry.
- **IsRevoked**: manual session invalidation (admin force-logout).

Indexed on `(UserId, IsRevoked)` for efficient active-session lookup.

---

## 2. Business Rules

### 2.1 User Creation (Passwordless)
- `User.Create()` does NOT accept a password. `PasswordHash` is set to a BCrypt placeholder hash of a randomly generated 12-char string (prevents blank-hash attacks).
- `MustChangePassword` is always `true` on creation.
- The admin does not know the user's initial password — the user sets it on first login.

### 2.2 First Login Flow
1. User enters username only → Login endpoint checks `MustChangePassword` → returns `RequiresPasswordSetup` error (HTTP 400 with specific error code).
2. Desktop shows password-set dialog (New Password + Confirm).
3. Single API call sends `UserName` + `NewPassword` + `ConfirmPassword`.
4. Server validates strength (min 8 chars, at least one letter + one digit), hashes with BCrypt (work factor 12), sets `PasswordHash`, clears `MustChangePassword`, logs `AuditLog` entry.
5. Server returns JWT on success.

### 2.3 Normal Login Flow
1. Validate `UserName` exists and `IsActive = true`.
2. If `IsLocked = true`, return `AccountLocked` error with lockout time info.
3. Verify password with `BCrypt.Verify()`.
4. On failure: increment `LoginAttempts`. If `LoginAttempts >= 5`, set `IsLocked = true`. Log `LoginFailed` audit entry. Return `InvalidCredentials` error.
5. On success: reset `LoginAttempts = 0`, update `LastLoginAt`, create `UserSession`, log `LoginSuccess` audit entry, return JWT with claims (UserId, UserName, Roles, Permissions).

### 2.4 Password Change (Authenticated)
- `ChangePasswordAsync()` requires CurrentPassword verification + NewPassword + ConfirmPassword.
- Validates current password via BCrypt, validates new password strength, updates hash, updates `PasswordChangedAt`.
- Invalidates all existing sessions for the user (optional, configurable).

### 2.5 Account Lockout
- Locked after 5 consecutive failed login attempts.
- Admin can manually unlock via `User.Unlock()` domain method.
- Optional auto-unlock after configurable timeout (default: 30 minutes, checked on each login attempt).
- Audit entry created on every lock/unlock event.

### 2.6 Permission Check Pattern
- API endpoints use a custom `[RequirePermission("Sales.View")]` attribute (or policy-based equivalent).
- The attribute reads the JWT claims (which include the user's permission codes at login time) and validates against the required permission.
- Desktop checks permissions via the API's `CurrentUserDto.Permissions` list — buttons/menus are conditionally visible/hidden based on permission codes (not hardcoded role checks).
- If the JWT permissions are stale (e.g., admin changed permissions while user was logged in), the API re-fetches from DB on each authorized call to ensure up-to-date enforcement.

### 2.7 Session Management
- Each login creates a `UserSession` record with a unique session token (JWT ID `jti` claim).
- `IsRevoked = false` by default.
- Admin can revoke sessions from the user management UI — forces logout on next request.
- Expired sessions are automatically ignored.
- Session cleanup runs periodically (background job or on app startup) to delete expired sessions older than 30 days.

### 2.8 Audit Logging
- Every login success/failure creates an `AuditLog` entry:
  - `LoginSuccess` — includes UserId, IP, Device.
  - `LoginFailed` — includes attempt count.
  - `LoginBlocked_Locked` — when blocked due to lockout.
  - `PasswordChanged`, `UserCreated`, `UserLocked`, `UserUnlocked`.
- Audit entries are viewed in a dedicated Desktop screen with filters (User, Date range, Event type).

---

## 3. Security Design

### 3.1 Authentication Flow
```
┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
│ Desktop  │────→│ API      │────→│ AuthSvc  │────→│ DB       │
│ HttpClient│    │ Controller│    │ Service  │    │ (Users)  │
└──────────┘     └──────────┘     └──────────┘     └──────────┘
     │                │                │
     │  POST /login   │                │
     │───────────────→│                │
     │                │ LoginAsync()   │
     │                │───────────────→│
     │                │                │──→ Check UserName
     │                │                │──→ Check IsLocked
     │                │                │──→ BCrypt.Verify
     │                │                │──→ Update Attempts
     │                │                │──→ Create Session
     │                │                │──→ Generate JWT
     │  JWT + UserDto │                │
     │←───────────────│                │
```

### 3.2 JWT Token Structure
Claims:
- `sub`: UserId (int)
- `unique_name`: UserName
- `role`: List of role names (e.g., "Admin", "Cashier")
- `permissions`: Comma-separated permission codes
- `jti`: Session token (GUID, stored in UserSessions)
- `exp`: Expiration (configurable, default 24 hours)

### 3.3 API Permission Enforcement
- Controllers use `[Authorize]` at class level for all endpoints (except `/api/v1/auth/login`).
- Granular permission enforcement via custom `[RequirePermission("code")]` attribute or authorization policies.
- Rate limiting on login endpoint: 5 attempts per 15 minutes per IP (using .NET 10 built-in rate limiter).
- All permission checks are server-enforced — Desktop UI hiding is cosmetic only.

### 3.4 Password Policy
- Minimum 8 characters.
- At least one letter and one digit.
- BCrypt hash, work factor 12.
- Password history NOT in V1 (deferred to V2).

---

## 4. Seed Data

### 4.1 Default Employee
- Party: "مدير النظام" (System Admin) — passed to Parties table.
- Employee: EmployeeNo=1, DepartmentId=null (system-level), HireDate=today.
- AccountId auto-created or null.

### 4.2 Default User
- UserName: "admin"
- PasswordHash: Placeholder (BCrypt of random string — never "Admin@123").
- MustChangePassword: true
- LoginAttempts: 0, IsLocked: false
- Links to the default employee via EmployeeId.

### 4.3 Four Roles
| Id | Name | Description |
|----|------|-------------|
| 1 | Admin | مدير النظام — full access |
| 2 | Accountant | محاسب — financial operations |
| 3 | Cashier | كاشير — sales/cash operations |
| 4 | Observer | مراقب — read-only view |

### 4.4 33 Permissions (9 Categories)

| Category | Permissions | Admin | Accountant | Cashier | Observer |
|----------|------------|:-----:|:----------:|:-------:|:--------:|
| **Sales** (7) | View, Create, EditDraft, Post, Cancel, ViewProfit, EditPrice | ✅ All | ✅ All | ✅ View, Create, EditPrice | ✅ View only |
| **Purchases** (5) | View, Create, EditDraft, Post, Cancel | ✅ All | ✅ All | ❌ | ✅ View only |
| **Inventory** (3) | View, Transfer, Adjust | ✅ All | ✅ View, Transfer | ✅ View | ✅ View |
| **Customers** (3) | View, Create, Edit | ✅ All | ✅ All | ✅ View | ✅ View |
| **Suppliers** (3) | View, Create, Edit | ✅ All | ✅ All | ❌ | ✅ View |
| **Products** (3) | View, Create, Edit | ✅ All | ✅ All | ❌ | ✅ View |
| **Reports** (1) | View | ✅ | ✅ | ❌ | ✅ |
| **Accounting** (2) | Manage, Journal | ✅ | ✅ | ❌ | ❌ |
| **System** (2) | Settings.Manage, UserManagement | ✅ | ❌ | ❌ | ❌ |
| **Operations** (3) | Backup.Manage, Cashbox.Close, DeleteRecord | ✅ | ❌ | ❌ | ❌ |
| **Audit** (1) | AuditLog.View | ✅ | ✅ | ❌ | ✅ |

### 4.5 Default UserRoles
Admin user gets RoleId=1 (Admin).

### 4.6 Default RolePermissions
33 records matching the matrix above — Admin gets all 33, Accountant gets 25, Cashier gets 7, Observer gets 11.

---

## 5. Implementation Tasks

### T1: Domain Entities (5 files)
- `Department.cs` — smallint PK, Name, Description.
- `Employee.cs` — PartyId FK, DepartmentId FK, EmployeeNo, HireDate, Salary.
- `Role.cs` — smallint PK, Name, Description.
- `User.cs` — EmployeeId FK, UserName, PasswordHash, MustChangePassword, LoginAttempts, IsLocked, LastLoginAt. Domain methods: `Create()` (passwordless), `SetInitialPassword()`, `ChangePassword()`, `RecordLoginAttempt()`, `Unlock()`, `MarkAsDeleted()` (soft via IsActive).
- `Permission.cs` — Code, DisplayName, Category, IsActive. Domain guard: system permissions (`IsSystem = true`) cannot be deleted.
- `UserRole.cs`, `RolePermission.cs`, `UserBranch.cs`, `UserSession.cs` — junction/session entities.

### T2: EF Core Configurations (9 files)
Fluent API for all 9 entities. Key points:
- `UserConfiguration`: `UserName` unique index, `PasswordHash` max 256, `LoginAttempts` default 0.
- `RoleConfiguration`: `smallint` PK convention.
- `PermissionConfiguration`: `Code` unique index, `Category` max 100.
- `UserRoleConfiguration`: `UNIQUE(UserId, RoleId)`.
- `RolePermissionConfiguration`: `UNIQUE(RoleId, PermissionId)`.
- `UserSessionConfiguration`: Index on `(UserId, IsRevoked)`.
- ALL FKs: `DeleteBehavior.Restrict`.
- `User`: Query filter `u => u.IsActive` (standard soft delete — NOT a separate `Status` enum, avoiding the dual-state issue identified in reviews. Lock state is handled by `IsLocked` bit field).

### T3: Database Migration
- `AddUsersAndPermissions` migration adding all 9 tables.
- `UpdateTablesWithCreatedByUserId` migration: update existing tables (Parties, Customers, etc.) to use new User FK references.

### T4: Domain Services (4 files)
- `UserService`: CRUD (soft delete only — `PermanentDeleteAsync` returns `Result.Failure`), `UnlockAsync`, `GetByUserNameAsync`.
- `AuthService`: `LoginAsync`, `SetInitialPasswordAsync`, `ChangePasswordAsync`, `RefreshTokenAsync`, `RevokeSessionAsync`.
- `PermissionService`: `GetAllPermissionsAsync`, `GetRolePermissionsAsync`, `UpdateRolePermissionsAsync` (atomic via `IUnitOfWork.ExecuteTransactionAsync`), `GetUserPermissionsAsync`.
- `SessionService`: `CreateSessionAsync`, `ValidateSessionAsync`, `RevokeSessionAsync`, `CleanupExpiredSessionsAsync`.

### T5: API Controllers (4 files)
- `AuthController`: POST `/login`, POST `/set-password`, POST `/change-password`, POST `/refresh-token`, POST `/logout`. Rate-limited login (5/15min). No `[AllowAnonymous]` on set-password — integrated into login flow instead.
- `UsersController`: CRUD endpoints, POST `{id}/unlock`. Admin-only for most operations.
- `PermissionsController`: GET permissions list, GET role-permissions, PUT role-permissions.
- `SessionsController`: GET active sessions for user, POST `{id}/revoke`.

### T6: FluentValidators (5 files)
- `LoginRequestValidator`, `SetPasswordRequestValidator`, `ChangePasswordRequestValidator`.
- `CreateUserRequestValidator`, `UpdateUserRequestValidator`.

### T7: Desktop — Login & Password Change
- `LoginView` — enhanced to handle `MustChangePassword` flow: two-stage UI (username-only → password-set dialog).
- `LoginViewModel` — handle `RequiresPasswordSetup` response, show password fields.
- `ChangePasswordView` — current password + new password + confirm, strength indicator.

### T8: Desktop — User Management
- `UsersListView` — DataGrid with search, filter by role, status badges (Active/Inactive/Locked).
- `UsersListViewModel` — CRUD with `IDialogService` for delete confirmation, `IToastNotificationService` for success. Newest-first sorting.
- `UserEditorView` — form with UserName, employee lookup, roles checkboxes, IsActive toggle, Unlock button.
- `UserEditorViewModel` — INotifyDataErrorInfo validation, `ValidateAllAsync()`.

### T9: Desktop — Permission Management (Admin Only)
- `PermissionsView` — DataGrid listing all 33 permissions with columns: Code, DisplayName, Category.
- `RolePermissionsView` — Matrix grid: rows = Roles, columns = Permissions, cells = checkboxes.
- `RolePermissionsViewModel` — Loads role-permission matrix, saves atomically via API.

### T10: Desktop — Session Management (Admin Only)
- `UserSessionsView` — DataGrid per user showing active sessions with DeviceName, IP, LastActivityAt, ExpiresAt.
- Revoke button with confirmation dialog.

### T11: Desktop — Audit Log Browser
- `AuditLogView` — filterable DataGrid (date range, user, event type).
- Read-only view — no delete/modify.
- Export to Excel via ClosedXML.

### T12: Desktop — Current User Display
- MainWindow status bar shows current user name and role badge.
- `GetCurrentUserAsync()` on login returns `CurrentUserDto` with UserName, Roles list, Permissions list.
- `SessionService.CurrentUser` property available app-wide.

### T13: API Authorization Middleware
- `PermissionAuthorizationHandler` — custom `AuthorizationHandler` that checks `RequirePermissionAttribute` against the user's permission claims.
- Registers policies: `AdminOnly`, `ManagerAndAbove`, `AllStaff`, and granular `RequirePermission("Sales.View")`.

### T14: DbSeeder Update
- Seed: 1 Department, 1 Party, 1 Employee, 1 User (admin, passwordless), 4 Roles, 33 Permissions, 4 sets of RolePermissions, 1 UserRole.
- Use two-pass approach: seed roles and permissions first → `SaveChangesAsync` → query IDs → seed RolePermissions and UserRole.

### T15: Data Protection & Rate Limiting
- Connection string encryption via DPAPI (already exists — verify compatibility).
- Rate limiter middleware before `UseAuthentication()`.
- JWT secret from environment variable.

---

## 6. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Forgot password recovery not in V1 | Users locked out with no self-recovery | Admin unlock via Users screen; password reset by admin (sets MustChangePassword=true) |
| Session token in JWT payload | Token cannot be revoked until expiry | Keep session expiration short (24h default); store `jti` in UserSessions for server-side revocation checks |
| Performance: permission check on every API call | Latency on high-traffic endpoints | Cache permissions in memory with 5-minute sliding expiry; invalidate on RolePermissions update |
| Migration from 3-role enum to DB roles | Existing hardcoded role checks break | Dual-run period: DB-backed permissions + existing enum checks; remove enum in V2 migration |
| Employee vs User confusion | Over-engineered for small shops | EmployeeId is nullable — small shops can create Users without Employee records |
| Passwordless-first login UX friction | First-time users confused | Desktop shows clear step-by-step wizard dialog explaining "تعيين كلمة المرور لأول مرة" |
| Audit log table growth | Performance degradation over time | Keep high-volume audit entries; implement archiving/deletion of records older than 1 year (V2) |
