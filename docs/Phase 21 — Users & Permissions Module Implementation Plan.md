# Phase 21 — Users & Permissions Module: Implementation Plan

> **v4.0** — Bitmask permissions (`BIGINT`), `[Flags]` enum, Super Admin = -1. Dropped `Roles`/`Permissions`/`RolePermissions`/`UserPermissions` tables.  
> **Scope**: User Management + Permissions (Bitmask) + Audit & Security + Admin UI.

---

## 1. Architecture — 3 Sub-Modules

### 1.1 User Management
- `User` entity: `UserName`, `PasswordHash`, `IsActive`, `IsLocked`, `MustChangePassword`, `LastLoginAt`, `LoginAttempts`, `PermissionsMask BIGINT`
- **No `IsSystemAdmin` field** — replaced by `PermissionsMask = -1` (Super Admin)
- Default password "12345678" (BCrypt, work factor 12), `MustChangePassword = true`
- Lockout after 5 failures → `IsLocked = true`

### 1.2 Permissions System (Bitmask + Role Templates)
- **Dropped**: `Permissions`, `RolePermissions`, `UserPermissions` tables
- **Kept**: `Roles` table — stores named role templates with predefined `PermissionsMask`
- **Single active column**: `Users.PermissionsMask BIGINT NOT NULL DEFAULT 0`
- `[Flags] Permission : long` enum (powers of 2, up to 64 bits)
- Super Admin: `PermissionsMask = -1` (all bits 1 → passes all `&` checks)
- Check: `(User.PermissionsMask & RequiredPermission) == RequiredPermission`
- **Role assignment flow**: Admin picks a role → `User.PermissionsMask = Role.PermissionsMask` → can be further customized per-user
- **8 seeded default roles** (`IsSystem = true` — protected from delete/rename)
- **Custom roles**: Admin can add new roles beyond the 8 defaults (`IsSystem = false` — editable/deletable)
- Future: `AllowMask` / `DenyMask` for per-user fine-tuning — `(RoleMask | AllowMask) & ~DenyMask`

### 1.3 Audit & Security
- `AuditLog` table (`long` PK) + `UserSession` table — both exist ✅
- Login history, account lockout, session tracking, audit browser UI all ✅

### 1.4 Permission Codes (33 codes, powers of 2)

| Permission | Bit Value |
|---|---|
| Sales.View / .Create / .EditDraft / .Post / .Cancel / .ViewProfit / .EditPrice | 1 – 64 |
| Purchase.View / .Create / .EditDraft / .Post / .Cancel | 128 – 2048 |
| Inventory.View / .Transfer / .Adjust | 4096 – 16384 |
| Customer.View / .Create / .Edit | 32768 – 131072 |
| Supplier.View / .Create / .Edit | 262144 – 1048576 |
| Product.View / .Create / .Edit | 2097152 – 8388608 |
| Reports.View | 16777216 |
| Accounting.Manage / .Journal | 33554432 – 67108864 |
| Settings.Manage / UserManagement | 134217728 – 268435456 |
| Backup.Manage / Cashbox.Close / DeleteRecord | 536870912 – 2147483648 |
| AuditLog.View | 4294967296 |

---

## 2. Full Inventory — What Already Exists

### 2.1 Domain
- `User.cs` — all fields + domain methods: `Create`, `CreateWithPassword`, `Update`, `ChangePassword`, `ResetPassword`, `RecordLoginAttempt`, `SetInitialPassword` ✅
- `AuditLog.cs` — `long` PK, `UserId`, `Action`, `EntityType`, `EntityId`, `OldValues`, `NewValues`, `ChangedColumns`, `IpAddress` ✅
- `UserSession.cs` — `UserId`, `SessionToken`, `DeviceName`, `IpAddress`, `UserAgent`, `LastActivityAt`, `ExpiresAt`, `IsRevoked` ✅

### 2.2 Application
- `AuthService` — BCrypt verify, JWT, login tracking, lockout, `MustChangePassword` ✅
- `UserService` — CRUD, reset password, soft-delete, guard hard-delete, **saves `PermissionsMask` on create/update** ✅
- `AuditLogService` — write + paginated query + filters ✅
- `PermissionService` — **Bitmask-only**: `HasPermission(long mask, Permission req)` = `(mask & req) == req`. Returns all when `mask == -1`. No DB joins. ✅
- `SessionManagementService` — session CRUD, revoke ✅
- All return `Result<T>` ✅

### 2.3 API
| Controller | Endpoints |
|---|---|
| `AuthController` | `POST login` (rate-limited, `[AllowAnonymous]`), `POST change-password` |
| `UsersController` | `GET/POST/PUT/DELETE /api/v1/users`, `GET current`, `POST reset-password` |
| `AuditLogsController` | `GET /api/v1/audit-logs` (paginated, filterable) |
| `PermissionsController` | `GET /api/v1/permissions` — returns `Permission` enum values (name + bitValue) for Desktop UI checkboxes |
| `SessionsController` | `GET /api/v1/sessions`, `POST revoke` |

### 2.4 Desktop
- `UsersListView` + `UserEditorView` — full CRUD ✅
- `PasswordChangeView` — first-login forced change ✅
- `AuditLogListView` — paginated, filterable ✅
- `PermissionManagementView` — checkboxes map to `[Flags]` enum bits, saved as `PermissionsMask` ✅
- `SessionService` — in-memory token/user/role, `HasPermission()`, `CanAccess()` ✅
- StatusBar — avatar + name + role + change password link + logout ✅
- `Permission.cs` — `[Flags] enum Permission : long` with `GetPermissionsForRole()`, `HasPermission()` ✅

---

## 3. Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Permission model | **Bitmask (`BIGINT`)** | Ultra-fast, no JOINs, single column. Super Admin = -1 bypasses all checks. |
| Roles | **`Roles` DB table with `PermissionsMask`** | Stores named templates. 8 seeded defaults (`IsSystem=true`). Admin can add custom roles. Assigning a role copies mask to user. |
| No join tables | **No `RolePermissions`/`UserPermissions`** | Role assignment = copy mask to user. User's mask is self-contained — no runtime joins needed for permission checks. |
| Super Admin | **`PermissionsMask = -1`** | Two's complement: all 64 bits = 1, passes every `(mask & req) == req` check automatically |
| User creation password | **Default "12345678"** (always BCrypt-hashed) | No anonymous SetPassword endpoint; no null PasswordHash guards needed |
| Audit storage | **SQL Server `AuditLog` table** with retention cleanup | Sufficient for < 1M rows; archiving deferred to V2 |
| Password flow | `MustChangePassword = true` → mandatory change screen on first login | Familiar pattern, no tokens needed |
| 2FA / LDAP / SSO | **Deferred to V2** | Overkill for local retail system |
| Avatar | **Deferred to V2** | Profile fields on Employee/Party, not on User |

---

## 4. Gap Analysis

| Item | Status | Action |
|---|---|---|
| User entity fields | ✅ Exist | No change |
| Login + lockout | ✅ Implemented | No change |
| Audit log + session tracking | ✅ Implemented | No change |
| Password change screen | ✅ Exists | No change |
| Permission management UI | ✅ Exists (checkboxes → mask) | No change |
| `PermissionsMask` column on Users | ✅ Added via migration | Active system — replaces all legacy RBAC tables |
| Legacy RBAC tables | ⚠️ Must be removed | **DROP** `Roles`, `Permissions`, `RolePermissions`, `UserPermissions` |

---

## 5. Implementation Tasks

| # | Task | Status | Files |
|---|---|---|---|
| 1 | User entity + migration — add `PermissionsMask BIGINT NOT NULL DEFAULT 0`, remove `IsSystemAdmin` | ✅ | `User.cs`, `UserConfiguration.cs`, Migration |
| 2 | `Role` entity + EF config + migration — `Id`, `Name`, `PermissionsMask`, `IsSystem`, `IsActive` | ⚠️ TODO | `Role.cs`, `RoleConfiguration.cs`, Migration |
| 2b | Seed 8 default roles with correct `PermissionsMask` values + `IsSystem = true` | ⚠️ TODO | `DbSeeder.cs` |
| 2c | `RolesController` — `GET/POST/PUT/DELETE /api/v1/roles` (guards: no delete/rename for `IsSystem = true`) | ⚠️ TODO | `RolesController.cs` |
| 2d | `RoleApiService` in Desktop + `RolesListView` + `RoleEditorView` | ⚠️ TODO | Desktop layer |
| 3 | AuditLog entity + config + migration | ✅ | `AuditLog.cs`, `AuditLogConfiguration.cs` |
| 4 | UserSession entity + config + migration | ✅ | `UserSession.cs`, `UserSessionConfiguration.cs` |
| 5 | AuditLogService (write + query + paginated) | ✅ | `IAuditLogService.cs`, `AuditLogService.cs` |
| 6 | PermissionService — Bitmask-only: `HasPermission(mask, req)`, `IsSuperAdmin(mask)` | ⚠️ Update | `IPermissionService.cs`, `PermissionService.cs` |
| 7 | AuthService (default password, lockout, MustChangePassword, audit) | ✅ | `AuthService.cs` |
| 8 | API endpoints (change-password, reset-password, current-user, audit-logs, permissions enum, sessions) | ✅ | All controllers + `SessionManagementService` |
| 9-10 | UserEditor enhancements (avatar/phone/email) | ⏳ DEFERRED | `UserEditorViewModel.cs`, `UserEditorView.xaml` |
| 11 | PasswordChangeView + ViewModel | ✅ | `PasswordChangeViewModel.cs`, `PasswordChangeView.xaml` |
| 12 | AuditLogBrowser ViewModel + View | ✅ | `AuditLogListViewModel.cs`, `AuditLogListView.xaml` |
| 13 | PermissionManagement ViewModel + View | ✅ | `PermissionManagementViewModel.cs`, `PermissionManagementView.xaml` |
| 14 | Current User indicator + permission filtering in MainWindow | ✅ | `MainWindow.xaml`, `MainViewModel.cs` |
| 15 | Validators + DI registration | ✅ | `UserRequestValidators.cs`, `AuthRequestValidators.cs`, `Program.cs`, `App.xaml.cs` |

---

## 6. Non-V1 Items (Deferred)

2FA/TOTP, LDAP/AD, IP restriction, user groups, role inheritance, bulk import/export, SSO, session dashboard, password expiry, audit partitioning, permission audit trail, self-service password reset.

---

## 7. Rollback Plan

| Scenario | Action |
|---|---|
| PermissionsMask migration issue | Restore `Permissions`/`Roles` tables from backup; revert `UserConfiguration.cs` |
| Permission module issues | Remove `PermissionsController`, `PermissionService`. Desktop `[Flags]` enum unaffected. |
| AuditLog/Session table issues | `DROP TABLE` — Serilog + in-memory session still work |
| Password change bug | Revert `AuthController.cs` to previous version |
