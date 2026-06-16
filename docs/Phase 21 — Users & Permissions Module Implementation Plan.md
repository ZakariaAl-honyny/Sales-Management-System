# Phase 21 — Users & Permissions Module: Comprehensive Implementation Plan

> **Version**: 2.1 — Updated from passwordless creation to default-password flow (v4.6.9 design refinement). Admin creates user with default password "12345678", user forced to change on first login. No token-based SetPassword flow.
> **Scope**: Complete Users & Permissions system with 3 sub-modules — User Management (Enhanced), Permissions System (New), Audit & Security (New)

---

## Table of Contents
1. [Architecture — 3 Sub-Modules](#1-architecture--3-sub-modules)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Fixes](#3-blocker-resolution--critical-fixes)
4. [Design Catalog](#4-design-catalog)
5. [Gap Analysis](#5-gap-analysis)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Items (Deferred)](#7-non-v1-items-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Architecture — 3 Sub-Modules

The Users & Permissions module is divided into **3 sub-modules** based on the analysis checklist and current system gaps:

### 1.1 User Management (Enhanced)

| Component | Current State | Target State |
|-----------|---------------|--------------|
| User entity | `UserName`, `PasswordHash`, `FullName`, `Role`, `IsActive` | + `Phone`, `Email`, `AvatarPath`, `LastLoginAt`, `LoginAttempts`, `IsLocked` |
| User CRUD | Basic — username/password/role | Full profile — avatar upload, contact info, change password screen |
| Login | Password only | + Track login attempts, lockout after 5 failures, last login display |
| Password | BCrypt hash (work factor 12) | Same + dedicated password change screen + password strength validation |

### 1.2 Permissions System (New)

| Component | Current State | Target State |
|-----------|---------------|--------------|
| Roles | 3 fixed (Admin=1, Manager=2, Cashier=3) | **9 roles** (Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager) per AGENTS.md Section 6 |
| UI Permissions | `Permission` flags enum in DesktopPWF (14 flags via `[Flags]`) | Move to DB-backed `Permission` entity + `RolePermission` join table (Roles+Permissions model per analysis recommendation) |
| API Authorization | Policy-based: `AdminOnly`, `ManagerAndAbove`, `AllStaff` | Dynamic permission check from DB using exact permission codes |
| Admin UI | No permission configuration screen | Admin grid with checkboxes showing Roles × Permissions matrix |

**Permission entity** — Using exact codes from Analysis Part 5 (lines 3981-4022):

| # | Permission Code | Display Name (Arabic) | Category | Admin | Accountant | Cashier | Observer |
|---|-----------------|----------------------|----------|:-----:|:----------:|:-------:|:--------:|
| **Sales** | | | | | | | |
| 1 | `Sales.View` | عرض فواتير البيع | Sales | ✅ | ✅ | ✅ | ✅ |
| 2 | `Sales.Create` | إنشاء فاتورة بيع | Sales | ✅ | ✅ | ✅ | ❌ |
| 3 | `Sales.EditDraft` | تعديل مسودة بيع | Sales | ✅ | ✅ | ❌ | ❌ |
| 4 | `Sales.Post` | ترحيل فاتورة بيع | Sales | ✅ | ✅ | ❌ | ❌ |
| 5 | `Sales.Cancel` | إلغاء فاتورة بيع | Sales | ✅ | ✅ | ❌ | ❌ |
| 6 | `Sales.ViewProfit` | عرض الربح في البيع | Sales | ✅ | ✅ | ❌ | ❌ |
| 7 | `Sales.EditPrice` | تعديل سعر البيع | Sales | ✅ | ✅ | ✅ | ❌ |
| **Purchases** | | | | | | | |
| 8 | `Purchase.View` | عرض فواتير الشراء | Purchase | ✅ | ✅ | ❌ | ✅ |
| 9 | `Purchase.Create` | إنشاء فاتورة شراء | Purchase | ✅ | ✅ | ❌ | ❌ |
| 10 | `Purchase.EditDraft` | تعديل مسودة شراء | Purchase | ✅ | ✅ | ❌ | ❌ |
| 11 | `Purchase.Post` | ترحيل فاتورة شراء | Purchase | ✅ | ✅ | ❌ | ❌ |
| 12 | `Purchase.Cancel` | إلغاء فاتورة شراء | Purchase | ✅ | ✅ | ❌ | ❌ |
| **Inventory** | | | | | | | |
| 13 | `Inventory.View` | عرض المخزون | Inventory | ✅ | ✅ | ✅ | ✅ |
| 14 | `Inventory.Transfer` | نقل مخزني | Inventory | ✅ | ✅ | ❌ | ❌ |
| 15 | `Inventory.Adjust` | تسوية مخزنية | Inventory | ✅ | ✅ | ❌ | ❌ |
| **Customers** | | | | | | | |
| 16 | `Customer.View` | عرض العملاء | Customers | ✅ | ✅ | ✅ | ✅ |
| 17 | `Customer.Create` | إضافة عميل | Customers | ✅ | ✅ | ❌ | ❌ |
| 18 | `Customer.Edit` | تعديل عميل | Customers | ✅ | ✅ | ❌ | ❌ |
| **Suppliers** | | | | | | | |
| 19 | `Supplier.View` | عرض الموردين | Suppliers | ✅ | ✅ | ❌ | ✅ |
| 20 | `Supplier.Create` | إضافة مورد | Suppliers | ✅ | ✅ | ❌ | ❌ |
| 21 | `Supplier.Edit` | تعديل مورد | Suppliers | ✅ | ✅ | ❌ | ❌ |
| **Products** | | | | | | | |
| 22 | `Product.View` | عرض المنتجات | Products | ✅ | ✅ | ❌ | ✅ |
| 23 | `Product.Create` | إضافة منتج | Products | ✅ | ✅ | ❌ | ❌ |
| 24 | `Product.Edit` | تعديل منتج | Products | ✅ | ✅ | ❌ | ❌ |
| **Reports** | | | | | | | |
| 25 | `Reports.View` | عرض التقارير | Reports | ✅ | ✅ | ❌ | ✅ |
| **Accounting** | | | | | | | |
| 26 | `Accounting.Manage` | إدارة الحسابات | Accounting | ✅ | ✅ | ❌ | ❌ |
| 27 | `Accounting.Journal` | قيد يومية | Accounting | ✅ | ✅ | ❌ | ❌ |
| **System** | | | | | | | |
| 28 | `Settings.Manage` | إدارة الإعدادات | System | ✅ | ❌ | ❌ | ❌ |
| 29 | `UserManagement` | إدارة المستخدمين | System | ✅ | ❌ | ❌ | ❌ |
| **Operations** | | | | | | | |
| 30 | `Backup.Manage` | النسخ الاحتياطي | Operations | ✅ | ❌ | ❌ | ❌ |
| 31 | `Cashbox.Close` | إغلاق الصندوق | Operations | ✅ | ❌ | ❌ | ❌ |
| 32 | `DeleteRecord` | حذف سجل | Operations | ✅ | ❌ | ❌ | ❌ |
| **Audit** | | | | | | | |
| 33 | `AuditLog.View` | عرض سجل الحركات | Audit | ✅ | ✅ | ❌ | ✅ |

### 1.3 Audit & Security (New)

| Component | Current State | Target State |
|-----------|---------------|--------------|
| Audit log | Only Serilog file logging | `AuditLog` DB table + UI browser with filter/search |
| Login history | Not tracked | `AuditLog` entries for login success/failure + `User.LoginAttempts` + `User.LastLoginAt` |
| Account lockout | Not implemented | Lock after 5 failed attempts — unlock by admin or auto after 30 min |
| Session tracking | In-memory `SessionService` only | `UserSession` table — track active sessions + expiry |
| Current user indicator | Basic text in StatusBar | Avatar image + name + role + log out button |
| Password change | Via `UpdateUserRequest` only | Dedicated "تغيير كلمة المرور" screen with current password validation |

---

## 2. Full Inventory — What Already Exists

### 2.1 Domain Layer

**Entity**: `SalesSystem.Domain.Entities.User`

| Field | Type | Status | Notes |
|-------|------|--------|-------|
| `Id` | `int PK` | ✅ | Inherited from `ActivatableEntity` |
| `UserName` | `string(50)` | ✅ | Unique index |
| `PasswordHash` | `string(256)` | ✅ | BCrypt hash of default password "12345678" on create |
| `EmployeeId` | `int?` | ✅ | Optional link to employee record (new) |
| `IsLocked` | `bool` | ✅ | True = account locked after 5 failed attempts |
| `MustChangePassword` | `bool` | ✅ | Default true — forces password change on first login |
| `LastLoginAt` | `DateTime?` | ✅ | Updated on each successful login |
| `LoginAttempts` | `smallint` | ✅ | Schema: smallint — C#: short, default 0 |
| `IsActive` | `bool` | ✅ | Inherited from `ActivatableEntity` — soft delete support |
| `CreatedByUserId` | `int?` | ✅ | FK to Users table |
| `CreatedAt` | `DateTime` | ✅ | Inherited from ActivatableEntity |

**IMPORTANT**: The User entity does NOT have Phone, Email, AvatarPath, FullName, Role (enum), or UserStatus enum. Profile fields are on the linked Parties/Employees table. Roles are assigned via the many-to-many `UserRole` join entity (not an enum on User).

**Factory methods**:
- `User.Create(userName, employeeId?, createdByUserId?)` — ✅ Passwordless creation, `MustChangePassword = true`
- `User.CreateWithPassword(userName, passwordHash, employeeId?, createdByUserId?, mustChangePassword?)` — ✅ For seeds/admin
- `User.Update(employeeId?, updatedByUserId?)` — ✅ Updates profile, NOT role/password
- `User.ChangePassword(newPasswordHash, updatedByUserId?)` — ✅ Sets hash + resets login attempts
- `User.ResetPassword(newPasswordHash)` — ✅ Admin reset, sets `MustChangePassword = true`
- `User.RecordLoginAttempt(bool success)` — ✅ Lockout at 5 failures via `IsLocked`
- `User.SetInitialPassword(passwordHash)` — ✅ For passwordless flow, guard: `MustChangePassword == true`

**Configuration**: `Infrastructure/Data/Configurations/UserConfiguration.cs` — ✅ Exists with:
- `.ToTable("Users")`, `.HasKey(u => u.Id)`
- `.Property(u => u.UserName).HasMaxLength(50).IsRequired()` + Unique Index
- `.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired()`
- `.Property(u => u.EmployeeId).IsRequired(false)`
- `.Property(u => u.LoginAttempts).HasColumnType("smallint")` — matches schema
- `.Property(u => u.IsLocked).HasDefaultValue(false)`
- `.Property(u => u.MustChangePassword).HasDefaultValue(true)`
- `.HasQueryFilter(u => u.IsActive)`
- `.Ignore(u => u.UserRoles)` — mapped via separate configuration

### 2.2 Application Layer

**Services**:

| Service | Interface | Status | Methods |
|---------|-----------|--------|---------|
| `AuthService` | `IAuthService` | ✅ | `LoginAsync(LoginRequest, CancellationToken)` → `Result<LoginResponse>` |
| `UserService` | `IUserService` | ✅ | `GetByIdAsync`, `GetAllAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `PermanentDeleteAsync` (guarded) |

**AuthService features**:
- BCrypt password verification (work factor 12 per RULE-039)
- JWT token generation via `IJwtTokenGenerator`
- Checks IsActive before login
- Username case-insensitive matching
- Serilog: `Log.Information` on login, `Log.Warning` on failed attempt

**UserService features**:
- `CreateAsync`: checks duplicate username, hashes "12345678" as default password (BCrypt, work factor 12), creates user via `User.CreateWithPassword(mustChangePassword: true)`, assigns role via `UserRole` join entity. `CreateUserRequest.Password` field is IGNORED — password is always default "12345678" (user must change on first login).
- `UpdateAsync`: updates employee link, lock state, optional password, role assignment (via UserRole join — full replace). No FullName/IsActive update.
- `DeleteAsync`: soft delete via `Users.SoftDeleteAsync()` with guard against deactivating last Admin role user.
- `PermanentDeleteAsync`: **GUARDED** — returns `Result.Failure("لا يمكن حذف المستخدمين بشكل نهائي")` per RULE-244
- `ResetPasswordAsync`: hashes "12345678" as new password, calls `user.ResetPassword()` sets `MustChangePassword = true`, logs audit event via `IAuditLogService`.
- `GetCurrentUserAsync`: loads user + permissions via `IPermissionService.GetUserPermissionsAsync()`, returns `CurrentUserDto` with permission list.
- All methods return `Result<T>` per RULE-006
- Uses `IUnitOfWork` per RULE-024
- Injects `IPermissionService` for permission queries and `IAuditLogService` for audit logging

### 2.3 API Layer

**Controller**: `SalesSystem.Api.Controllers.UsersController`

| Method | Endpoint | Policy | Status |
|--------|----------|--------|--------|
| GET | `/api/v1/users` | `AdminOnly` | ✅ Exists |
| GET | `/api/v1/users/{id:int}` | `AdminOnly` | ✅ Exists |
| POST | `/api/v1/users` | `AdminOnly` | ✅ Exists |
| PUT | `/api/v1/users/{id:int}` | `AdminOnly` | ✅ Exists |
| DELETE | `/api/v1/users/{id:int}` | `AdminOnly` | ✅ Exists (soft) |
| DELETE | `/api/v1/users/permanent/{id:int}` | `AdminOnly` | ✅ Exists (guarded) |

**Controller**: `SalesSystem.Api.Controllers.AuthController`

| Method | Endpoint | Policy | Status |
|--------|----------|--------|--------|
| POST | `/api/v1/auth/login` | `[AllowAnonymous]` + `[EnableRateLimiting("LoginPolicy")]` | ✅ Exists |

### 2.4 Contracts Layer

**DTOs** (in `SalesSystem.Contracts.DTOs.AllDtos.cs`):
> See `SalesSystem.Contracts/` for canonical DTO definitions.

**Requests** (in `SalesSystem.Contracts.Requests.UserRequests.cs`):
> See `SalesSystem.Contracts/` for canonical Request definitions.

**Responses** (in `SalesSystem.Contracts.Responses.LoginResponse.cs`):
> See `SalesSystem.Contracts/` for canonical Response definitions.

**Validators** (in `SalesSystem.Api.Validators.UserRequestValidators.cs`):
- `CreateUserRequestValidator` — ✅ Exists
- `UpdateUserRequestValidator` — ✅ Exists

### 2.5 Desktop Layer — Views

**Files**:

| File | Status | Content |
|------|--------|---------|
| `Views/Users/UsersListView.xaml` | ✅ Exists (288 lines) | DataGrid + Search + Toolbar + Empty State |
| `Views/Users/UsersListView.xaml.cs` | ✅ Exists (20 lines) | Instantiate VM, register Loaded handler |
| `Views/Users/UserEditorView.xaml` | ✅ Exists (29 lines code-behind) | Window with CloseRequested + FocusFirstInvalid |
| `Views/Users/UserEditorView.xaml` | XAML ✅ Exists | Editor form for Add/Edit user |

### 2.6 Desktop Layer — ViewModels

| File | Status | Features |
|------|--------|----------|
| `ViewModels/Users/UserListViewModel.cs` | ✅ Exists (343 lines) | AdminOnly base, Refresh/Add/Edit/ToggleStatus/ResetPassword commands, EventBus subscription |
| `ViewModels/Users/UserEditorViewModel.cs` | ✅ Exists (194 lines) | INotifyDataErrorInfo, ValidateAsync, Save with create/update, DialogService |

**UserListViewModel patterns used**:
- `AdminOnlyViewModel` base class with `ISessionService` injection
- `ExecuteAsync()` wrapper for all async commands (RULE-141)
- `LogSystemError()` for error logging (RULE-199)
- `_screenWindowService.OpenScreen()` for non-modal editing (RULE-160)
- `_dialogService.ShowConfirmationAsync()` for confirmations (RULE-054)
- `_toastService.ShowSuccess()` for minor success messages (RULE-056)
- `_eventBus.Subscribe<UserChangedMessage>()` with `Cleanup()` unsubscribe (RULE-012/013)
- `OrderByDescending(x => x.Id)` for newest-first sorting (RULE-220)

**UserEditorViewModel patterns used**:
- `SetDialogService(_dialogService)` in constructor (RULE-227)
- `INotifyDataErrorInfo` with `AddError`/`ClearErrors` (RULE-228)
- `ValidateAsync()` with `ClearAllErrors` + `AddError` + `ShowValidationErrorsAsync` (RULE-229)
- `HandleFailure()` for user-friendly error messages (RULE-172)
- Screen-specific dialog titles (RULE-173)

### 2.7 Desktop Layer — API Services

| File | Status | Methods |
|------|--------|---------|
| `IUserApiService` interface | ✅ Exists (in `IApiService.cs`) | 6 methods |
| `UserApiService` | ✅ Exists (59 lines) | HTTP client with `ExecuteAsync<T>` pattern |

### 2.8 Desktop Layer — Session & Permissions

| File | Status | Features |
|------|--------|----------|
| `Enums/Permission.cs` | ✅ Exists (86 lines) | `[Flags]` enum with 14 permissions + `GetPermissionsForRole()` + `HasPermission()` |
| `Services/App/SessionService.cs` | ✅ Exists (70 lines) | In-memory token/user/role session, `CanAccess(Permission)` |
| `Services/App/ISessionService.cs` | Interface ✅ | `IsAuthenticated`, `GetToken()`, `GetUserName()`, `GetUserId()`, `GetUserRole()`, `SetSession()`, `ClearSession()`, `HasPermission()`, `CanAccess()` |

### 2.9 MainWindow StatusBar

```xml
<!-- MainWindow.xaml Lines 395-412: -->
<StatusBar Grid.Row="2" Style="{StaticResource StatusBarStyle}">
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="المستخدم: " Margin="0,0,5,0"/>
            <TextBlock x:Name="TxtUserName" FontWeight="Bold"/>
        </StackPanel>
    </StatusBarItem>
    <Separator/>
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="الدور: " Margin="0,0,5,0"/>
            <TextBlock x:Name="TxtUserRole" FontWeight="Bold"/>
        </StackPanel>
    </StatusBarItem>
    <StatusBarItem HorizontalAlignment="Right">
        <TextBlock x:Name="CurrentDateText"/>
    </StatusBarItem>
</StatusBar>
```

### 2.10 Theme/Styles

| File | Status |
|------|--------|
| `Resources/Brushes.xaml` | ✅ Exists |
| `Resources/Styles.xaml` | ✅ Exists — includes `StatusBarStyle`, button/textbox compact styles |

---

## 3. BLOCKER Resolution — Critical Fixes

### 3.1 Blocker 1: Permission Model — Flags Enum vs DB Entity

**Problem**: The current `Permission.cs` uses a `[Flags]` enum with hardcoded `GetPermissionsForRole()` mapping. This is:
1. Not configurable by admin without code change
2. Not synced with API-side authorization
3. Missing 8+ permissions requested in analysis (CreateInvoice, CancelInvoice, CloseCashbox, DeleteRecord, ViewProfit, PriceOverride, DiscountOverride, AuditLogView)

**Decision**: **Keep both — Dual Model** for V1:
- `Permission.cs` `[Flags]` enum remains in DesktopPWF for fast in-memory UI checks
- **NEW** `Permission` DB entity + `RolePermission` join table for admin configurability
- Seed data synchronizes the Roles matrix from AGENTS.md Section 6
- Admin Permission Management screen reads from DB, writes to DB
- `SessionService` loads permissions from API on login (future: read from DB directly)

### 3.2 Blocker 2: Migration Strategy for Extended User Table

**Problem**: Adding `Phone`, `Email`, `AvatarPath`, `LastLoginAt`, `LoginAttempts`, `IsLocked` to an existing `Users` table with production data.

**Fix**: All new columns must be nullable or have safe defaults:
> See `docs/database-schema.md` Module 1.9 for the canonical Users table definition.

**Migration is additive** — no breaking changes, no data loss.

### 3.3 Blocker 3: Audit Log Volume — DB vs File Retention

**Problem**: If every action is logged to `AuditLog` table, the table can grow very large (~500K rows/month for busy retail). Query performance degrades.

**Decision** for V1:
- `AuditLog` stored in **SQL Server table** with clustered index on `Timestamp DESC`
- **Retention policy**: Configurable via `SystemSetting` key `AuditLog.RetentionDays` (default 365)
- **Background cleanup**: `ScheduledAuditLogCleanupWorker` (BackgroundService) runs daily at 3:00 AM
- **Index strategy**: Composite index on `(UserId, Timestamp DESC)` + `(EntityType, EntityId)`
- **No file-based audit** in V1 — DB table is sufficient for < 1M rows

---

## 4. Design Catalog

### 4.1 User Status Tracking

**Note**: The initial plan specified a `UserStatus` enum (Active=1, Inactive=2, Locked=3), but the **actual implementation** uses two booleans inherited from `ActivatableEntity`:
- `IsActive` (bool) — controls soft-delete via global query filter
- `IsLocked` (bool) — controls account lockout after 5 failed login attempts

This avoids the dual-state problem of maintaining both `BaseEntity.IsActive` and a separate `UserStatus` enum. The `RecordLoginAttempt()` domain method handles the lockout logic: 5 failures → `IsLocked = true`. Admin unlock sets `IsLocked = false` and `LoginAttempts = 0`.

### 4.2 User Entity (Extended — Default Password Flow)

**File**: `Domain/Entities/User.cs` — Extended from current

**Design decision (v4.6.9)**: Admin creates user with a default password "12345678". The user is forced to change it on first login (MustChangePassword = true). No passwordless creation, no token-based SetPassword flow. The default password is immediately hashed via BCrypt at creation time.

> See `docs/database-schema.md` Module 1.9 (Users table) for the canonical User entity definition and `docs/CONSTITUTION.md`/`AGENTS.md` for entity patterns (private set, Guard Clauses, domain methods).
- `if (string.IsNullOrWhiteSpace(userName))` → `throw new DomainException("اسم المستخدم مطلوب.")`
- `if (string.IsNullOrWhiteSpace(fullName))` → `throw new DomainException("الاسم الكامل مطلوب.")`
- `if (phone?.Length > 20)` → `throw new DomainException("رقم الهاتف لا يتجاوز 20 رقماً.")`
- `if (email?.Length > 100)` → `throw new DomainException("البريد الإلكتروني لا يتجاوز 100 حرفاً.")`
- `if (string.IsNullOrWhiteSpace(newPasswordHash))` → `throw new DomainException("كلمة المرور الجديدة مطلوبة.")`
- `if (LoginAttempts < 0)` → `throw new DomainException("محاولات تسجيل الدخول غير صالحة.")`

### 4.3 UserConfiguration (Extended)

> See `docs/database-schema.md` Module 1.9 for the canonical User Fluent API configuration and `docs/AGENTS.md` §2.16 for EF Core conventions.

Updated query filter: `HasQueryFilter(u => u.IsActive)` — user status is tracked via `IsActive` (soft delete) + `IsLocked` (lockout). No `UserStatus` enum was implemented; the simpler dual-boolean approach avoids EF Core value converter complexity.

### 4.4 Permission Entity (NEW — Using exact codes from Analysis Part 5 lines 3981-4022)

> See `docs/database-schema.md` Module 1.10 (Permissions table) for the canonical Permission entity definition and `docs/AGENTS.md`/`CONSTITUTION.md` for entity patterns (private set, Guard Clauses, domain methods).

### 4.5 RolePermission Join Entity (NEW — 9-role model per AGENTS.md Section 6)

> See `docs/database-schema.md` Module 1.11 (RolePermissions table) for the canonical definition. Roles are DB-driven via the `Role` entity — there is NO `UserRole` enum. The 9 seeded roles (Id=1..9) are: Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager.

### 4.6 AuditLog Entity (NEW)

> See `docs/database-schema.md` Module 8.1 (AuditLogs table) for the canonical AuditLog entity definition with bigint PK, indexes, and FK to Users.

**Actual implementation** (extends `LongEntity` — bigint PK):
- `Id` — `long` PK (bigint)
- `UserId` — `int?` FK to Users (nullable for system actions)
- `Action` — `string(100)` e.g., "LoginSuccess", "CancelInvoice"
- `EntityType` — `string(100)?` e.g., "SalesInvoice", "Product"
- `EntityId` — `int?` (int FK — deviates from schema's `nvarchar(50)` by design; all entity PKs are int)
- `OldValues` — `string?` JSON of values before change (deviates from schema's single `Details` field)
- `NewValues` — `string?` JSON of values after change
- `ChangedColumns` — `string?` comma-separated column names
- `IpAddress` — `string(50)?`
- `CreatedAt` — `datetime2` (from LongEntity)
- **Indexes**: `(UserId, CreatedAt DESC)`, `(EntityType, EntityId)`, `(CreatedAt DESC)`

### 4.7 UserSession Entity (NEW)

> See `docs/database-schema.md` Module 1.14 (UserSessions table) for the canonical UserSession entity definition.

**Actual implementation** (extends `AuditableEntity` — NOT ActivatableEntity):
- `Id` — `int PK`
- `UserId` — `int` FK to Users
- `SessionToken` — `string(200)` (NOT `TokenHash` — stores the actual token)
- `DeviceName` — `string(200)?`
- `IpAddress` — `string(50)?`
- `UserAgent` — `string(500)?`
- `LastActivityAt` — `datetime2` (updated via `Touch()`)
- `ExpiresAt` — `datetime2` (default 8 hours from creation)
- `IsRevoked` — `bool` (NOT `IsActive` — schema uses `IsRevoked` for explicit revocation)
- `HasExpired()` — returns `DateTime.UtcNow > ExpiresAt`
- `IsValid()` — returns `!IsRevoked && !HasExpired()`
- **Index**: `(UserId, IsRevoked)` — for active session lookup

### 4.8 DTO Changes (Default Password Flow)

**UserDto — Extended**:
> See `SalesSystem.Contracts/` for canonical DTO definitions.

**Request Changes — Default Password Flow**:
> See `SalesSystem.Contracts/` for canonical Request definitions.

**Response Changes**:
> See `SalesSystem.Contracts/` for canonical Response definitions.

**New DTOs**:
> See `SalesSystem.Contracts/` for canonical DTO definitions.

### 4.9 Seed Data — Permissions (30 exact codes from Analysis Part 5 lines 3981-4022)

**File**: `Infrastructure/Data/DbSeeder.cs`

**Note**: The final implementation seeds **45 permission codes** across 12 categories per the AGENTS.md Section 6 matrix. Each follows `Domain.Action` dot-notation. The CRUD + Post + Cancel model separates View/Create/Edit/Delete/Cancel/Return/Print as distinct permissions.

> See `Infrastructure/Data/DbSeeder.cs` for the canonical seeder implementation and `docs/AGENTS.md` Section 6 for the 45 permission codes matrix and 9-role assignments.

### 4.9.1 Seed Data — Default Admin User

**Critical requirement**: The system MUST include a default admin account on first run so the user can log in immediately after installation.

**Design**:
- Admin account created with default password "12345678" (BCrypt hashed immediately)
- `MustChangePassword = true` forces the admin to change password on first login
- The admin user has the `Admin` role with ALL permissions
- No passwordless creation, no token-based flow

> See `Infrastructure/Data/DbSeeder.cs` for the canonical admin user seed implementation.

**Note on first-run flow**:
1. On first application launch, no users exist → `User.AnyAsync()` returns false
2. `DbSeeder` creates the admin user with BCrypt hash of "12345678", `MustChangePassword = true`
3. Login screen: user enters "admin" as username and "12345678" as password
4. Login succeeds → `LoginResponse.RequiresPasswordChange = true`
5. Desktop displays mandatory password change screen (cannot be closed/dismissed)
6. User enters new password (min 8 chars) + confirmation → calls `ChangePasswordAsync`
7. `MustChangePassword = false`, user is logged in normally
8. Admin can then create additional users from the User Management screen

**IDs**: The seeded admin user always gets Id = 1 (first user in the system). This is referenced as `CreatedByUserId = 1` in seed data for other entities.

**Security**:
- Default password "12345678" is never stored in plaintext — hashed immediately via BCrypt (work factor 12)
- `MustChangePassword = true` — user cannot bypass the mandatory password change
- Password change requires: Current Password (verification) + New Password (min 8 chars) + Confirmation
- After password change, `MustChangePassword = false` and `PasswordHash` stores the new BCrypt hash

### 4.10 IAuditLogService Interface (NEW)

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

### 4.11 IPermissionService Interface (NEW)

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

---

## 5. Gap Analysis

### 5.1 User Entity Fields (Updated per Analysis Part 5 lines 4890-5043)

| Field | Current | Required | Action |
|-------|---------|----------|--------|
| `UserName` | ✅ | ✅ | No change |
| `PasswordHash` | ✅ (NOT NULL) | ✅ (NOT NULL) | **KEEP** — required, default "12345678" hash on create |
| `EmployeeId` | ❌ | ✅ | **ADD** — int null FK → Employees(Id) (link user to employee record) |
| `IsLocked` | ✅ | ✅ | No change — bool, true after 5 failed login attempts |
| `MustChangePassword` | ✅ | ✅ | No change — bool default true, forces change on first login |
| `IsActive (bool)` | ✅ | ✅ | **KEEP** — ActivatableEntity uses IsActive for soft delete, NOT Status enum |
| `LastLoginAt` | ✅ | ✅ | No change — datetime2 null, updated on successful login |
| `LoginAttempts` | ✅ (smallint) | ✅ (smallint) | No change — short in C#, smallint in schema |
| `PasswordChangedAt` | ❌ | ❌ | **NOT NEEDED** — UserSession.LastActivityAt tracks this |
| `DefaultCashBoxId` | ❌ | ❌ (V2) | **DEFERRED** to V2 — CashBox integration not yet implemented |
| `Phone` | ❌ | ❌ | **NOT NEEDED** — profile fields are on linked Employee/Party entity |
| `Email` | ❌ | ❌ | **NOT NEEDED** — profile fields are on linked Employee/Party entity |
| `AvatarPath` | ❌ | ❌ | **NOT NEEDED** — avatar handling deferred to V2 |
| `FullName` | ❌ | ❌ | **NOT NEEDED** — name is on linked Employee/Party entity |
| `Role (UserRole enum)` | ❌ | ❌ | **REPLACED** — roles use DB-driven `Role` entity + `UserRole` join table |

### 5.2 Auth (Default Password Flow)

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| User creation flow | Default password | **Default password** | **KEEP** — `UserService.CreateAsync()` always hashes "12345678" (BCrypt, work factor 12). `CreateUserRequest.Password` field exists but is IGNORED by the service — password is always default. `MustChangePassword = true`. |
| First login flow | Must change password | **Must change password** | **KEEP** — login with default password succeeds but `LoginResponse.RequiresPasswordChange = true`. Desktop shows mandatory password change screen (cannot close). User calls `ChangePasswordAsync` to set new password → `MustChangePassword = false`. |
| Password reset (admin) | Admin resets to default | **Admin resets to default** | **KEEP** — admin calls `ResetPasswordAsync()` → `PasswordHash` = hash of "12345678", `MustChangePassword = true`. Audit log entry written. |
| BCrypt work factor 12 | ✅ | ✅ | No change |
| JWT token generation | ✅ | ✅ | No change |
| Track login attempts | ✅ | ✅ | Already implemented — `User.RecordLoginAttempt()` increments and locks at 5 failures |
| Account lockout | ✅ | ✅ | Already implemented — `IsLocked = true` after 5 failures, login returns "الحساب مغلق مؤقتاً" |
| Auto-unlock | ❌ | ❌ | **DEFERRED** to V2 — manual unlock via Admin only for now |
| Last login display | ✅ | ✅ | Already implemented — `User.LastLoginAt` updated on successful login |
| Password change endpoint | ✅ | ✅ | Already exists — `POST /api/v1/auth/change-password` |
| Set password endpoint | ❌ | ❌ | **NOT NEEDED** — default password flow replaces token-based SetPassword |
| Rate limiting | ✅ | ✅ | Already on login endpoint |

### 5.3 Permissions (Updated per Analysis Part 5 lines 3981-4022)

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| Roles | 3 (Admin/Manager/Cashier) + DB Role entity | **9** (Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager) | **EXTEND** to 9 roles. Roles are DB-driven via `Role` entity + `UserRole` join table — NOT a UserRole enum on User. |
| Permission model | 22 DB-backed permissions with dot-notation codes | **45** per AGENTS.md Section 6 | **EXTEND** — add missing permissions across 12 categories |
| CRUD + Post + Cancel | Not fully separated | **Separate** per operation | **EXTEND** — Sales and Purchase domains need separate Create/EditDraft/Post/Cancel permissions |
| Permission codes | Dot-notation codes | **Dot-notation** | **KEEP** — `Sales.View`, `Purchase.Create`, etc. are correct. Add missing codes. |
| DB model | Permission + RolePermission entities | **Same** | **KEEP** — already implemented |
| SQL Server table | Permissions + RolePermissions | ✅ | Already created |
| Seed data | 22 permissions + 4-role mappings | **45** | **EXTEND** — add 23 more permissions across 12 categories |
| Admin configuration UI | ❌ | ✅ | CREATE Permission Management screen |
| API permission check | ✅ (policies) | ✅ | Same + `UserHasPermissionAsync()` via `IPermissionService` |
| Observer role (reports only) | ❌ | ✅ | **ADD** — Reports.View + AuditLog.View + View-only permissions |

### 5.4 Audit & Logging

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| Serilog file logging | ✅ | ✅ | No change |
| AuditLog DB table | ✅ (exists) | ✅ | **KEEP** — already implemented with `AuditLog` entity (LongEntity, bigint PK) + `IAuditLogService` |
| Audit log browser UI | ❌ | ✅ | **CREATE** ViewModel + View in Desktop |
| Login history tracking | ✅ | ✅ | Already implemented — `AuditLog` entries for login success/failure + `User.LastLoginAt` |
| User activity per-user | ✅ | ✅ | Already implemented — `IAuditLogService.GetUserHistoryAsync()` |

### 5.5 Desktop — Missing Screens

| Screen | Current | Required | Action |
|--------|---------|----------|--------|
| User Editor (current form) | ✅ (exists) | ✅ | **KEEP** — no Phone/Email/Avatar fields needed |
| Password Change screen | ❌ | ✅ | **CREATE** — new ViewModel + View for first-login forced change |
| Audit Log Browser | ❌ | ✅ | **CREATE** — new ViewModel + View |
| Permission Management (Admin) | ❌ | ✅ | **CREATE** — new ViewModel + View |
| Current User indicator | ✅ (basic text) | ✅ | **KEEP** — StatusBar shows user name + role. Avatar deferred to V2. |

### 5.6 API — Missing Endpoints

| Endpoint | Current | Required | Action |
|----------|---------|----------|--------|
| `POST /api/v1/auth/change-password` | ✅ (exists) | ✅ | **KEEP** — already implemented |
| `GET /api/v1/users/current` | ✅ (exists) | ✅ | **KEEP** — already returns `CurrentUserDto` with permissions |
| `GET /api/v1/audit-logs` | ❌ | ✅ | **CREATE** — paginated, filterable |
| `GET /api/v1/audit-logs/user/{id}` | ✅ (exists via `IAuditLogService`) | ✅ | **CREATE** API endpoint |
| `GET /api/v1/audit-logs/login-history` | ❌ | ✅ | **CREATE** — login history |
| `GET /api/v1/permissions` | ❌ | ✅ | **CREATE** — list all permissions |
| `GET /api/v1/permissions/roles` | ❌ | ✅ | **CREATE** — role-permission mappings |
| `PUT /api/v1/permissions/roles/{role}` | ❌ | ✅ | **CREATE** — update role permissions |
| `POST /api/v1/users/avatar` | ❌ | ❌ | **DEFERRED** to V2 — avatar upload not in schema |
| `POST /api/v1/users/{id}/reset-password` | ✅ (exists) | ✅ | **KEEP** — already implemented |

---

## 6. Architectural Decisions

### 6.1 Permission Model: DB-Backed with Dot-Notation Codes

The old `[Flags] Permission` enum in DesktopPWF has been **replaced** by **DB-backed permissions** via the `Permission` entity + `RolePermission` join table.

**Current state**:
- **Permission codes** use `Domain.Action` format: `Sales.View`, `Purchase.Create`, etc.
- **22 permissions currently seeded** across Sales, Purchases, Inventory, Customers, Suppliers, Products, Reports, Accounting, System, Operations, and Audit categories
- **9-role model** — Admin (1), Manager (2), Accountant (3), Treasurer (4), Cashier (5), Warehouse Supervisor (6), Sales Employee (7), Observer (8), Branch Manager (9) via `Role` entity with `Id = 1..9`
- `SessionService` loads `List<string> UserPermissions` from API on login via `IPermissionService.GetUserPermissionsAsync(userId)`, cached as `HashSet<string>`
- `HasPermission("Sales.View")` = `_permissionNames.Contains("Sales.View")`
- **Observer role** gets only View permissions + `Reports.View` + `AuditLog.View`

**Future work**:
- **EXTEND** from 22 to 45 permissions covering all operations from the AGENTS.md Section 6 matrix
- **ADD** CRUD + Post + Cancel separation for Sales and Purchase domains
- **CREATE** Permission Management screen (Admin grid with Roles × Permissions checkboxes)
- **API endpoints**: `GET /api/v1/permissions`, `GET /api/v1/permissions/roles`, `PUT /api/v1/permissions/roles/{role}`

### 6.2 Audit Log Storage: Database (V1)

**Decision**: Store in SQL Server `AuditLog` table with:
- **Clustered index**: `IX_AuditLog_Timestamp` on `(Timestamp DESC)`
- **Non-clustered index**: `IX_AuditLog_UserId` on `(UserId, Timestamp DESC)`
- **Non-clustered index**: `IX_AuditLog_Entity` on `(EntityType, EntityId)`
- **Partitioning**: Deferred to V2 (table is small enough < 5M rows/year)
- **Retention**: `AuditLogRetentionCleanupService` runs daily, deletes records older than configured days

### 6.3 Avatar Storage: File System + API

**Decision**: **DEFERRED to V2**. The Users table has no `AvatarPath` column in the schema. Profile fields (name, phone, email) live on the linked `Employees`/`Parties` entity, not on `User`. Avatar upload and display will be implemented when the Employee module is fully integrated with User.

### 6.4 Password Policy (Default Password Flow)

- **Default password**: UserService hashes "12345678" (BCrypt, work factor 12) for every new user. `MustChangePassword = true`.
- **First login**: User logs in with "12345678" → password verified via BCrypt → login succeeds but `LoginResponse.RequiresPasswordChange = true`. Desktop displays **mandatory** password change screen (cannot be dismissed/escaped). User enters CurrentPassword + NewPassword (min 8 chars) + ConfirmPassword → calls `POST /api/v1/auth/change-password` → `MustChangePassword = false`, user proceeds to main screen.
- **Minimum length**: 8 characters (validated by FluentValidation)
- **BCrypt work factor**: 12 (RULE-039 — already implemented)
- **No password expiry** in V1 (deferred to future phase)
- **Account lockout**: 5 failed attempts → `IsLocked = true`, login returns "الحساب مغلق مؤقتاً"
- **Admin unlock**: Admin sets `IsLocked = false`, `LoginAttempts = 0` via UserEditor
- **Password reset (Admin)**: Admin clicks "إعادة تعيين كلمة المرور" → `UserService.ResetPasswordAsync()` hashes "12345678", sets `MustChangePassword = true`, `IsLocked` unchanged, `LoginAttempts = 0`. Audit log entry. User must change on next login.

### 6.5 Session Timeout

- **Sliding**: 8 hours (reset on each API call via middleware)
- **Absolute**: 24 hours (user must re-login)
- Tracked via `UserSession` table + JWT expiry claim
- Desktop: `SessionService` checks `IsAuthenticated` and token expiry

### 6.6 9-Role Model (per AGENTS.md Section 6)

The final system uses **9 DB-driven roles** with clear separation of duties:

```text
Admin (1)              = مدير النظام       → Full system access (ALL 45 permissions)
Manager (2)            = مدير              → Full operational access except System/Users
Accountant (3)         = محاسب             → Accounting, reports, financial operations
Treasurer (4)          = أمين صندوق        → Cashbox, banking, expenses operations
Cashier (5)            = كاشير             → Sales transactions, customer payments
Warehouse Supervisor (6) = مشرف مخازن     → Inventory management, transfers, adjustments
Sales Employee (7)     = مندوب مبيعات     → Sales invoices, customers, returns
Observer (8)           = مراقب             → View-only access
Branch Manager (9)     = مدير فرع          → Branch-scoped full access
```

**Note**: Roles are fully DB-driven via the `Role` entity + `UserRole` join table. There is NO `UserRole` enum anywhere in the codebase. Permissions use dot-notation codes (e.g., `Sales.View`, `Purchase.Create`) stored in the `Permission` entity with 45 seeded codes across 12 categories. The `RolePermission` join table seeds role-permission mappings for all 9 roles per AGENTS.md Section 6 matrix.

**Decision**: Keep the existing **flat role-permission mapping**. It is simpler, more flexible, and admin-configurable via the Permission Management screen. No role hierarchy complexity in V1.

### 6.7 Why NOT Two-Factor Authentication (2FA)

2FA was mentioned in analysis as "نظام التشفير للحماية" (Encryption system). This refers to BCrypt for password hashing (already implemented), NOT a separate 2FA system. True 2FA (TOTP/SMS) is deferred to a future phase.

### 6.8 Design Rationale: Why Default Password Instead of Passwordless?

The original plan specified **passwordless user creation** — admin creates user without a password (`PasswordHash = null`), and the user must use a token-based `SetPassword` flow on first login. This was changed to a **default password flow** during v4.6.9 implementation. Here's why:

| Factor | Passwordless (original) | Default Password (new) | Winner |
|--------|------------------------|----------------------|--------|
| **Security** | `PasswordHash = null` required null-check guards everywhere; any code path that assumes non-null `PasswordHash` could NRE | `PasswordHash` is always non-null, no special null handling needed | ✅ Default Password |
| **Attack surface** | `POST /api/v1/auth/set-password` was `[AllowAnonymous]` with `userId` from query string — an attacker could call it for any user if `MustChangePassword` is true | No `set-password` endpoint exists — attacker cannot set/reset passwords via API | ✅ Default Password |
| **Token management** | Required `PasswordResetToken` + `PasswordResetTokenExpiresAt` fields on User entity, plus secure token generation/validation | No tokens needed — user simply logs in with default password and is forced to change | ✅ Default Password |
| **Admin experience** | Admin creates user and must communicate a separate token/link to the user — adds friction | Admin creates user, tells them "your password is 12345678, change it on first login" — simple and familiar | ✅ Default Password |
| **User experience** | User needs to understand a token-based flow — unfamiliar to many retail shop employees | User logs in with a password they already know (told by admin), then changes it — familiar pattern | ✅ Default Password |
| **Implementation complexity** | Required: `SetPasswordRequest` DTO, `SetPasswordAsync()`, `SetInitialPassword()`, `PasswordResetToken`, token validation, separate endpoint with auth | Required: Default hash in `User.Create()`, `RequiresPasswordChange` flag in `LoginResponse`, mandatory password change screen in desktop | ✅ Default Password |
| **Consistency with existing codebase** | `PasswordHash` column changes from NOT NULL to NULL — breaking schema change requiring migration | `PasswordHash` stays NOT NULL — no schema change; BCrypt hash of "12345678" is stored just like any other password | ✅ Default Password |

**Conclusion**: The default password flow is simpler, more secure (no anonymous SetPassword endpoint), more familiar to users, and requires fewer code changes. The passwordless/token approach was designed for enterprise systems with email infrastructure — overkill for a local retail sales management system.

---

## 7. Non-V1 Items (Deferred)

| Feature | Reason |
|---------|--------|
| Two-Factor Authentication (2FA/TOTP) | Complex UX, low retail demand |
| LDAP / Active Directory Integration | Enterprise feature, out of scope |
| IP-based access restriction | Requires network infrastructure |
| User group management | Over-engineering for V1 — fixed 9 roles suffice |
| Role inheritance hierarchy | Current flat mapping is simpler and sufficient |
| Bulk user import/export (Excel/CSV) | Low priority — admin can create individually |
| SSO (Single Sign-On) | Enterprise feature |
| Session management dashboard (force logout) | Deferred — active session tracking in V1, UI for admin in V2 |
| Password expiry policy | Deferred to V2 |
| Audit log partitioning/archiving | Not needed until > 5M rows |
| Permission audit trail (who changed permissions) | Deferred to V2 |
| Self-service password reset (email/SMS) | Requires email/SMS infrastructure |

---

## 8. Implementation Tasks

All tasks include logging (RULE-035/036), error handling (RULE-199/200/201), ToolTips (RULE-185-190), and UI Compact styles (RULE-262-274).

### Task 1 — Rebuild User Entity (Default Password + IsActive/IsLocked + MustChangePassword + EmployeeId) + Migration

**Files**:

| File | Change |
|------|--------|
| `Domain/Enums/UserStatus.cs` | **NEW** — enum: Active=1, Inactive=2, Locked=3 |
| `Domain/Entities/User.cs` | **REBUILD**: PasswordHash nullable, add Status, MustChangePassword, PasswordChangedAt, DefaultCashBoxId, Phone, Email, AvatarPath, LastLoginAt, LoginAttempts + all domain methods |
| `Infrastructure/Data/Configurations/UserConfiguration.cs` | Update Fluent API — PasswordHash nullable, Status replaces IsActive, add all new fields, query filter on Status |
| `Infrastructure/Data/Migrations/` | New migration: schema changes |
| `Contracts/DTOs/AllDtos.cs` — `UserDto` | Extend with all new fields |
| `Contracts/Requests/UserRequests.cs` | CreateUserRequest with optional Password (default "12345678"), no SetPasswordRequest needed |

**User.Create — Default Password Flow**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/database-schema.md` Module 1.9 for the canonical Users table definition.

**Domain methods**:
> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) — `RecordLoginAttempt`, `ChangePassword`, `ResetPassword`, `Lock`/`Unlock`, `Deactivate`/`Activate`, `SetAvatar`/`ClearAvatar`, `MarkAsDeleted`/`Restore` methods follow the same pattern.

**Logging**: 
- `Log.Information("User {UserId} profile updated: phone={Phone}, email={Email}", id, phone, email)`
- `Log.Warning("User {UserId} locked out after {Attempts} failed attempts", userId, attempts)`
- `Log.Information("Password reset by admin for user {UserId}", userId)` — RULE-182

**Validation** (RULE-044):
- `CreateUserRequestValidator` — UserName required, FullName required, Role valid, Password (optional — if provided, min 8 chars), Phone max 20 chars (optional), Email max 100 chars + valid format (optional)
- `UpdateUserRequestValidator` — Same
- `ChangePasswordRequestValidator` — CurrentPassword required, NewPassword min 8 chars, ConfirmPassword must match

**Estimate**: ~2 hours

---

### Task 2 — Create Permission + RolePermission Entities + Config + Migration + Seed + Default Admin User

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/Permission.cs` | Create entity with Name, DisplayNameAr, Category, IsSystem |
| `Domain/Entities/RolePermission.cs` | Create join entity with Role (UserRole enum), PermissionId |
| `Infrastructure/Data/Configurations/PermissionConfiguration.cs` | Fluent API — unique Name, IsSystem locked, Category index |
| `Infrastructure/Data/Configurations/RolePermissionConfiguration.cs` | Fluent API — Composite unique index on (Role, PermissionId) — Restrict delete |
| `Infrastructure/Data/Migrations/` | New migration: CREATE Permissions + RolePermissions tables |
| `Infrastructure/Data/DbSeeder.cs` | Seed 22 permissions + role mappings **+ default admin user** |

**PermissionConfiguration**: See `docs/database-schema.md` Module 1.10 for the canonical Permission Fluent API configuration.

**RolePermissionConfiguration**: See `docs/database-schema.md` Module 1.11 for the canonical RolePermission Fluent API configuration.

**Logging**: `Log.Information("Seeded {Count} permissions and {RpCount} role-permission mappings", count, rpCount)`

**Estimate**: ~1.5 hours

---

### Task 3 — Create AuditLog Entity + Configuration + Migration

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/AuditLog.cs` | Create entity with Id (long), UserId, Action, EntityType, EntityId, Details, IpAddress, Timestamp |
| `Infrastructure/Data/Configurations/AuditLogConfiguration.cs` | Fluent API — indexes on (UserId, Timestamp), (EntityType, EntityId), (Timestamp DESC) |
| `Infrastructure/Data/Migrations/` | New migration: CREATE AuditLogs table |

**AuditLogConfiguration**: See `docs/database-schema.md` Module 8.1 for the canonical AuditLog Fluent API configuration (bigint PK, indexes on Timestamp, (UserId, Timestamp), (EntityType, EntityId), FK to Users with Restrict).

**Logging**: No logging in AuditLog service (it IS the logging layer)

**Estimate**: ~40 minutes

---

### Task 4 — Create UserSession Entity + Configuration + Migration

**Files**:

| File | Change |
|------|--------|
| `Domain/Entities/UserSession.cs` | Create entity with UserId, TokenHash (SHA256), LoginAt, LastActivityAt, ExpiresAt, IsActive |
| `Infrastructure/Data/Configurations/UserSessionConfiguration.cs` | Fluent API — index on UserId, token hash |
| `Infrastructure/Data/Migrations/` | New migration: CREATE UserSessions table |

**Logging**: `Log.Information("Session created for user {UserId}, expires at {ExpiresAt}", userId, expiresAt)`

**Estimate**: ~30 minutes

---

### Task 5 — Create AuditLogService (Write + Query + Paginated Result)

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/IAuditLogService.cs` | Create interface with LogAsync, QueryAsync, GetUserHistoryAsync, GetLoginHistoryAsync |
| `Application/Services/AuditLogService.cs` | Implementation with IUnitOfWork (RULE-024), Result<T> (RULE-006) |
| `Contracts/DTOs/AllDtos.cs` | Add AuditLogDto, LoginHistoryDto, AuditLogQuery record |

**AuditLogService**:
> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns (IUnitOfWork, RULE-024).

**Logging**: `Log.Information("Audit log entry: {Action} on {EntityType}#{EntityId} by user {UserId}", ...)`

**Estimate**: ~1 hour

---

### Task 6 — Create PermissionService (CRUD + Role-Permission Mapping)

**Files**:

| File | Change |
|------|--------|
| `Application/Interfaces/Services/IPermissionService.cs` | Create interface with 6 methods |
| `Application/Services/PermissionService.cs` | Implementation with IUnitOfWork, Result<T> |
| `Contracts/DTOs/AllDtos.cs` | Add PermissionDto, RolePermissionDto |

**PermissionService.UpdateRolePermissionsAsync**:
> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns (transaction pattern RULE-003, IUnitOfWork RULE-024).

**Logging**: `Log.Warning("Permission {PermissionId} is system-locked — cannot modify", id)` on IsSystem guard
**Validation**: Must have at least 1 permission per role

**Estimate**: ~1.5 hours

---

### Task 7 — Update AuthService (Default Password Flow, Login Attempts, Lockout, Audit Trail, Mandatory Password Change)

**Files**:

| File | Change |
|------|--------|
| `Application/Services/AuthService.cs` | After BCrypt verify: call `user.RecordLoginAttempt(success)`, check `Status == Locked`, write AuditLog entry; check `MustChangePassword` and set `RequiresPasswordChange` in response |
| `Application/Interfaces/Services/IAuthService.cs` | Add `ChangePasswordAsync()` method signature |
| `Application/Services/AuthService.cs` | Add `ChangePasswordAsync(ChangePasswordRequest, int userId)` |

**LoginAsync logic — Default password with MustChangePassword check**:
> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for AuthService patterns (RULE-305 through RULE-315 for login flow, lockout, audit logging).

**ChangePasswordAsync**:
> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for AuthService patterns (RULE-314/315 for password change flow).

**Validation** (RULE-044):
- `ChangePasswordRequestValidator`: CurrentPassword required, NewPassword min 8 chars, ConfirmPassword must match
- `LoginRequestValidator`: UserName required, Password required

**Logging**:
- `Log.Warning("Login blocked: User {UserName} is locked")` — user mistake (RULE-183)
- `Log.Warning("Login failed: Incorrect password for user {UserName} (attempt {Attempts})")` — user mistake
- `Log.Information("Password changed for user {UserId}")` — system operation (RULE-182)
- `Log.Information("User {UserId} logged in with MustChangePassword=true")` — system operation
- Never log the password itself (RULE-037)

**Estimate**: ~2.5 hours

---

### Task 8 — Create New API Endpoints (Password Change, Reset Password, Avatar, Current User, Audit Log, Permissions)

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/AuthController.cs` | Add `POST /api/v1/auth/change-password` (authenticated — used for both first-login forced change and voluntary change) |
| `Api/Controllers/UsersController.cs` | Add `GET /api/v1/users/current`, `POST /api/v1/users/avatar`, `GET /api/v1/users/{id}/avatar`, `POST /api/v1/users/{id}/reset-password` (admin) |
| `Api/Controllers/AuditLogsController.cs` | **NEW** — 4 endpoints for audit log query |
| `Api/Controllers/PermissionsController.cs` | **NEW** — 3 endpoints for permission management |

**Note**: No `set-password` endpoint exists — the default password flow ("12345678") replaces the token-based SetPassword flow. Users log in with the default password, then are forced to change it via `change-password`.

**AuthController additions**:
> See `docs/AGENTS.md` for controller layer patterns (RULE-022/203) and `docs/CONSTITUTION.md` for the Result<T> pattern.

**UsersController additions**:
> See `docs/AGENTS.md` for controller layer patterns (RULE-022/203) and `docs/CONSTITUTION.md` for the Result<T> pattern.

**AuditLogsController**:
> See `docs/AGENTS.md` for controller layer patterns (RULE-022/203 — inject services only, no DbContext/IUnitOfWork) and `docs/CONSTITUTION.md` for the Result<T> pattern.

**PermissionsController**:
> See `docs/AGENTS.md` for controller layer patterns (RULE-022/203 — inject services only, no DbContext/IUnitOfWork) and `docs/CONSTITUTION.md` for the Result<T> pattern.

**Controller purity** (RULE-203): All controllers inject service interfaces only — NO direct DbContext or IUnitOfWork.

**Logging**: `Log.Information("Audit log query: page {Page}, size {Size}, filters: {Filters}", ...)`

**Validation**:
- `AuditLogQuery` — Page >= 1, PageSize between 10 and 500
- `ChangePasswordRequestValidator` — CurrentPassword not empty, NewPassword min 8, ConfirmPassword match

**Estimate**: ~2 hours

---

### Task 9 — Enhanced UserEditorViewModel (Avatar, Phone, Email, Password Change)

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Users/UserEditorViewModel.cs` | Add Phone, Email, AvatarPath, IsLocked properties + avatar upload logic + password change button |
| `Contracts/DTOs/AllDtos.cs` — `UserDto` | Extended with new fields |
| `Services/Api/IApiService.cs` — `IUserApiService` | Add `UploadAvatarAsync(int userId, Stream imageStream, string fileName)` |
| `Services/Api/UserApiService.cs` | Implement avatar upload via multipart form |

**New properties** in UserEditorViewModel:
> See `docs/AGENTS.md` for ViewModel patterns (ExecuteAsync wrapper, INotifyDataErrorInfo, DialogService, ScreenWindowService).

**ValidateAsync expanded**:
> See `docs/AGENTS.md` for ViewModel validation patterns (RULE-228/229: INotifyDataErrorInfo with AddError/ClearErrors, ValidateAllAsync).

**ToolTips** (RULE-185-190):
- Phone field: `"رقم الهاتف — اختياري، سيظهر في تقارير المستخدمين"`
- Email field: `"البريد الإلكتروني — اختياري، يستخدم للإشعارات مستقبلاً"`
- Avatar upload: `"اختيار صورة شخصية — ستظهر في شريط الحالة"`
- Remove avatar: `"إزالة الصورة الشخصية"`
- Change password: `"فتح شاشة تغيير كلمة المرور"`
- Unlock button: `"فتح الحساب المغلق — يعيد تعيين محاولات الدخول الفاشلة"`

**Estimate**: ~2 hours

---

### Task 10 — Enhanced UserEditorView.xaml (Avatar, Phone, Email, Compact Styles)

**Files**:

| File | Change |
|------|--------|
| `Views/Users/UserEditorView.xaml` | Add avatar preview Image + upload button, Phone/Email TextBox fields, IsLocked status, compact form layout |
| `Views/Users/UserEditorView.xaml.cs` | Handle avatar file drag/drop or browse dialog |

**XAML additions**:
```xml
<!-- Avatar Section -->
<Border Grid.Row="1" Style="{StaticResource CardStyle}" Margin="0,0,0,10">
    <StackPanel Orientation="Horizontal">
        <Border Width="80" Height="80" CornerRadius="40" Background="{StaticResource GrayLightBrush}"
                BorderBrush="{StaticResource BorderBrush}" BorderThickness="1">
            <Image Source="{Binding AvatarUrl}" Width="80" Height="80"
                   Stretch="UniformToFill" ToolTip="الصورة الشخصية للمستخدم"/>
            <Border.Style>
                <Style TargetType="Border">
                    <Setter Property="Visibility" Value="Visible"/>
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding HasAvatar}" Value="False">
                            <Setter Property="Visibility" Value="Collapsed"/>
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </Border.Style>
        </Border>
        <StackPanel VerticalAlignment="Center" Margin="16,0,0,0">
            <Button Content="📷 اختيار صورة" Command="{Binding UploadAvatarCommand}"
                    Style="{StaticResource SecondaryButton}" Margin="0,0,0,6"
                    ToolTip="اختيار صورة شخصية — سيتم عرضها في شريط الحالة"/>
            <Button Content="🗑️ إزالة الصورة" Command="{Binding RemoveAvatarCommand}"
                    Style="{StaticResource DangerButton}" Visibility="{Binding HasAvatar, Converter={StaticResource BoolToVisibility}}"
                    ToolTip="إزالة الصورة الشخصية المحددة"/>
        </StackPanel>
    </StackPanel>
</Border>

<!-- Contact Info Section -->
<StackPanel Grid.Row="2" Margin="0,0,0,8">
    <TextBlock Text="📞 معلومات الاتصال" FontWeight="Bold" FontSize="14" Margin="0,0,0,8"/>

    <TextBlock Text="رقم الهاتف" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding Phone}" Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"
             ToolTip="رقم الهاتف — اختياري، أقصى 20 رقم"/>

    <TextBlock Text="البريد الإلكتروني" Style="{StaticResource LabelStyle}"/>
    <TextBox Text="{Binding Email}" Style="{StaticResource ModernTextBox}" Margin="0,0,0,6"
             ToolTip="البريد الإلكتروني — اختياري، أدخل بريداً صحيحاً"/>

    <!-- Lock Status -->
    <CheckBox Content="الحساب مغلق" IsChecked="{Binding IsLocked, Mode=OneWay}"
              Visibility="{Binding IsEditMode, Converter={StaticResource BoolToVisibility}}"
              ToolTip="هذا الحساب مغلق بسبب كثرة محاولات الدخول الفاشلة — قم بإلغاء التحديد لفتحه"/>
</StackPanel>
```

**UI Compact** (RULE-262-274):
- No hardcoded `Height="36"` on TextBox/Button — use styles (28px default)
- Padding: `10,4` via styles
- Section margins: `Margin="0,0,0,6"` between fields
- Header: `FontSize="14"`
- Avatar dimensions: `80×80` (circular crop)

**ToolTips**:
- All buttons and inputs have Arabic ToolTips
- Upload button: `"اختيار صورة شخصية للمستخدم — ستظهر في شاشة الحالة"`
- Remove button: `"إزالة الصورة الشخصية المحددة"`
- Phone: `"رقم الهاتف — اختياري، أقصى 20 حرفاً"`
- Email: `"البريد الإلكتروني — اختياري، أدخل بريداً إلكترونياً صحيحاً"`
- Locked checkbox: `"هذا الحساب مغلق — قم بإلغاء التحديد لفتح الحساب"`
- Save: `"حفظ بيانات المستخدم"`
- Cancel: `"إلغاء التعديل والعودة"`

**Estimate**: ~1.5 hours

---

### Task 11 — Password Change Screen (ViewModel + View)

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Users/PasswordChangeViewModel.cs` | **NEW** — CurrentPassword, NewPassword, ConfirmPassword + ValidateAsync + SaveAsync |
| `Views/Users/PasswordChangeView.xaml` | **NEW** — Compact form with 3 password fields |
| `Views/Users/PasswordChangeView.xaml.cs` | **NEW** — Code-behind |
| `Services/Api/IApiService.cs` — `IAuthApiService` | Add `ChangePasswordAsync(ChangePasswordRequest)` |
| `Services/Api/AuthApiService.cs` | Implement HTTP POST to `/api/v1/auth/change-password` |
| `App.xaml.cs` | DI registration for PasswordChangeViewModel |

**PasswordChangeViewModel**:
> See `docs/AGENTS.md` for ViewModel patterns (ExecuteAsync wrapper RULE-141, INotifyDataErrorInfo RULE-228/229, DialogService RULE-054, HandleFailure RULE-172).

**Validation** (RULE-228/229):
- CurrentPassword: required
- NewPassword: required, min 8 characters
- ConfirmPassword: required, must match NewPassword
- ShowValidationErrorsAsync with clear Arabic messages

**ToolTips**:
- CurrentPassword: `"أدخل كلمة المرور الحالية"`
- NewPassword: `"كلمة المرور الجديدة — يجب أن تكون 8 أحرف على الأقل"`
- ConfirmPassword: `"أعد إدخال كلمة المرور الجديدة للتأكيد"`
- Save: `"حفظ كلمة المرور الجديدة"`
- Cancel: `"إلغاء التغيير والعودة"`

**Estimate**: ~1 hour

---

### Task 12 — AuditLog Browser ViewModel + View (Filterable, Searchable, Paginated)

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Audit/AuditLogListViewModel.cs` | **NEW** — Paginated query with filters (userId, action, entityType, date range) |
| `Views/Audit/AuditLogListView.xaml` | **NEW** — DataGrid + filter toolbar + pagination |
| `Views/Audit/AuditLogListView.xaml.cs` | **NEW** — Code-behind |
| `Services/Api/IApiService.cs` — `IAuditLogApiService` | **NEW** — QueryAsync, GetUserHistoryAsync, GetLoginHistoryAsync |
| `Services/Api/AuditLogApiService.cs` | **NEW** — HTTP client implementation |
| `App.xaml.cs` | DI registrations + navigation entry |

**AuditLogListViewModel**:
> See `docs/AGENTS.md` for ViewModel patterns (AdminOnlyViewModel base class RULE-130, ExecuteAsync wrapper RULE-141, paginated query, EventBus lifecycle RULE-012/013).

**Login History sub-view**:
- Shows `LoginSuccess` / `LoginFailed` entries from AuditLog
- Columns: Time, User, Action, IP Address, Status (Success/Failed badge)

**ToolTips** (RULE-185-190):
- Search: `"بحث في سجل الحركات حسب اسم المستخدم أو نوع العملية"`
- Date range: `"تصفية حسب الفترة الزمنية"`
- View user history: `"عرض جميع حركات هذا المستخدم"`
- View login history: `"عرض سجل تسجيل الدخول"`
- Previous/Next: `"الصفحة السابقة"` / `"الصفحة التالية"`
- Refresh: `"تحديث سجل الحركات"`

**UI Compact**: DataGrid compact row height (24px via style), compact filter toolbar

**Estimate**: ~2.5 hours

---

### Task 13 — Permission Management ViewModel + View (Admin Grid with Checkboxes)

**Files**:

| File | Change |
|------|--------|
| `ViewModels/Permissions/PermissionManagementViewModel.cs` | **NEW** — Load roles + permissions, show grid with checkboxes per role, Save |
| `Views/Permissions/PermissionManagementView.xaml` | **NEW** — Grouped by category, DataGrid with checkboxes |
| `Views/Permissions/PermissionManagementView.xaml.cs` | **NEW** — Code-behind |
| `Services/Api/IApiService.cs` — `IPermissionApiService` | **NEW** — GetAllPermissionsAsync, GetRolePermissionsAsync, UpdateRolePermissionsAsync |
| `Services/Api/PermissionApiService.cs` | **NEW** — HTTP client |
| `App.xaml.cs` | DI registrations + navigation entry |

**PermissionManagementViewModel**:
> See `docs/AGENTS.md` for ViewModel patterns (AdminOnlyViewModel RULE-130, ExecuteAsync RULE-141, DialogService RULE-054, EventBus RULE-012/013). The 9-role selector and permission checkboxes follow the permission matrix in AGENTS.md Section 6 above.

**XAML structure**:
```xml
<!-- 9-role selector tabs (per AGENTS.md Section 6 matrix) -->
<RadioButton Content="مدير النظام" IsChecked="{Binding IsAdminSelected}"/>
<RadioButton Content="مدير" IsChecked="{Binding IsManagerSelected}"/>
<RadioButton Content="محاسب" IsChecked="{Binding IsAccountantSelected}"/>
<RadioButton Content="أمين صندوق" IsChecked="{Binding IsTreasurerSelected}"/>
<RadioButton Content="كاشير" IsChecked="{Binding IsCashierSelected}"/>
<RadioButton Content="مشرف مخازن" IsChecked="{Binding IsWhseSupervisorSelected}"/>
<RadioButton Content="مندوب مبيعات" IsChecked="{Binding IsSalesEmployeeSelected}"/>
<RadioButton Content="مراقب" IsChecked="{Binding IsObserverSelected}"/>
<RadioButton Content="مدير فرع" IsChecked="{Binding IsBranchManagerSelected}"/>

<!-- Grouped by category -->
<ItemsControl ItemsSource="{Binding Categories}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <Expander Header="{Binding CategoryName}" IsExpanded="True">
                <ItemsControl ItemsSource="{Binding Permissions}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <CheckBox Content="{Binding DisplayNameAr}"
                                      IsChecked="{Binding IsChecked}"
                                      ToolTip="{Binding Name, StringFormat='الصلاحية: {0}'}"/>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </Expander>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```

**ToolTips**:
- Save: `"حفظ صلاحيات الدور المحدد — سيتم تطبيق التغييرات فوراً"`
- Select All: `"تحديد جميع الصلاحيات للدور الحالي"`
- Deselect All: `"إلغاء تحديد جميع الصلاحيات للدور الحالي"`
- Role tabs: `"اختيار الدور لعرض وتعديل صلاحياته"`

**Estimate**: ~2.5 hours

---

### Task 14 — Current User Indicator in MainWindow + Permission Filtering + Update Existing VMs

**Files**:

| File | Change |
|------|--------|
| `MainWindow.xaml` | Update StatusBar: add avatar Image (32×32), user name, role badge, "تغيير كلمة المرور" link, logout button |
| `MainWindow.xaml.cs` | Update `LoadSession()` to show avatar + bind permissions + apply `CanNavigateTo()` |
| `ViewModels/MainViewModel.cs` | Add `CurrentUser` property (CurrentUserDto) with avatar, permissions |
| `Services/Api/IApiService.cs` — `IUserApiService` | Add `GetCurrentUserAsync()` |
| `Services/Api/UserApiService.cs` | Implement HTTP GET `/api/v1/users/current` |
| `App.xaml.cs` | Register `IAuditLogApiService`, `IPermissionApiService`, `PasswordChangeViewModel` |

**MainWindow.xaml — Updated StatusBar**:
```xml
<StatusBar Grid.Row="2" Style="{StaticResource StatusBarStyle}">
    <!-- Avatar + Name -->
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <Border Width="28" Height="28" CornerRadius="14" Background="{StaticResource GrayLightBrush}"
                    BorderThickness="1" BorderBrush="{StaticResource BorderBrush}" Margin="0,0,8,0">
                <Image Source="{Binding CurrentUserAvatar}" Width="28" Height="28"
                       Stretch="UniformToFill" ToolTip="المستخدم الحالي"/>
            </Border>
            <TextBlock x:Name="TxtUserName" FontWeight="Bold" VerticalAlignment="Center"
                       ToolTip="اسم المستخدم الحالي"/>
        </StackPanel>
    </StatusBarItem>

    <!-- Role Badge -->
    <StatusBarItem>
        <Border Style="{StaticResource StatusBadgeStyle}" Background="{StaticResource InfoLightBrush}">
            <TextBlock x:Name="TxtUserRole" Foreground="{StaticResource InfoTextBrush}"
                       FontSize="11" Padding="6,2" ToolTip="صلاحية المستخدم الحالي"/>
        </Border>
    </StatusBarItem>

    <!-- Separator -->
    <Separator/>

    <!-- Change Password Link -->
    <StatusBarItem>
        <Button Content="🔑 تغيير كلمة المرور" Command="{Binding ChangePasswordCommand}"
                Style="{StaticResource LinkButtonStyle}" ToolTip="فتح شاشة تغيير كلمة المرور"/>
    </StatusBarItem>

    <!-- Logout -->
    <StatusBarItem HorizontalAlignment="Right">
        <StackPanel Orientation="Horizontal">
            <TextBlock x:Name="CurrentDateText" Margin="0,0,16,0" VerticalAlignment="Center"/>
            <Button Content="🚪 تسجيل خروج" Command="{Binding LogoutCommand}"
                    Style="{StaticResource DangerButton}" Padding="8,4"
                    ToolTip="تسجيل الخروج من النظام"/>
        </StackPanel>
    </StatusBarItem>
</StatusBar>
```

**ApplyPermissions** (using new dot-notation codes from analysis):
> See `docs/AGENTS.md` for permission filtering patterns (RULE-318: API-based permission checks using `HasPermission` with dot-notation codes). The permission codes used here match Section 1.2 matrix.

**ToolTips** (RULE-185-190):
- Avatar + name: `"المستخدم الحالي — {UserName}"`
- Role badge: `"{RoleDisplayName} — صلاحية المستخدم الحالي"`
- Change password: `"فتح شاشة تغيير كلمة المرور"`
- Logout: `"تسجيل الخروج من النظام — سيتم إغلاق الجلسة الحالية"`

**Estimate**: ~2 hours

---

### Task 15 — FluentValidation Updates + Rate Limiting Confirmation + DI Registration

**Files**:

| File | Change |
|------|--------|
| `Api/Validators/UserRequestValidators.cs` | Update CreateUserRequestValidator + UpdateUserRequestValidator with Phone/Email validation |
| `Api/Validators/AuthRequestValidators.cs` | **NEW** — ChangePasswordRequestValidator |
| `Api/Validators/AuditLogValidators.cs` | **NEW** — AuditLogQueryValidator (Page >= 1, PageSize between 10-500) |
| `Api/Program.cs` | Confirm RateLimiter configured before UseAuthentication() |
| `DesktopPWF/App.xaml.cs` | Register all new ViewModels + API services |

**DI Registrations needed**:
> See `DesktopPWF/App.xaml.cs` and `Api/Program.cs` for canonical DI registration patterns.

**Estimate**: ~45 minutes

### Task 16 — Unit Tests

**Files**: NEW test files in `SalesSystem.Domain.Tests`, `SalesSystem.Application.Tests`, `SalesSystem.Api.Tests`, `SalesSystem.Infrastructure.Tests`

#### 1. Domain Entity Tests

**User.Create()** — Test with valid inputs creates entity with `IsActive = true`, `IsLocked = false`, `MustChangePassword = true`, `PasswordHash` is BCrypt hash of default "12345678". Test with empty `userName` → `DomainException("اسم المستخدم مطلوب.")`. Test `RecordLoginAttempt(true)` resets `LoginAttempts` to 0 and sets `LastLoginAt`. Test `RecordLoginAttempt(false)` increments counter; after 5 failures → `IsLocked = true`. Test `ChangePassword()` sets hash, sets `MustChangePassword = false`. Test `ChangePassword()` with null/empty hash → `DomainException`. Test `ResetPassword()` sets password back to hash of "12345678" and sets `MustChangePassword = true`. Test `Lock()`, `Unlock()`, `MarkAsDeleted()`, `Restore()` — status transitions, `IsActive` stays in sync.

**Permission.Create()** — Valid inputs create entity. Empty `name` → `DomainException("اسم الصلاحية مطلوب")`. Empty `displayNameAr` → `DomainException("الاسم العربي للصلاحية مطلوب")`. `IsSystem = true` locks permissions.

**AuditLog.Create()** — Valid inputs create entity with `Timestamp` set to `DateTime.UtcNow`. Empty `action` → `DomainException`. Empty `entityType` → `DomainException`.

**UserSession.Create()** — Valid inputs create active session. `ExpiresAt` = `loginAt + 8 hours`. `Touch()` updates `LastActivityAt`. `Terminate()` sets `IsActive = false`.

#### 2. Service Tests (using Mock<IUnitOfWork>)

**AuthService.LoginAsync()**:
- Valid credentials with MustChangePassword=false → `Result<LoginResponse>.Success` with JWT token, `RequiresPasswordChange = false`
- Valid credentials with MustChangePassword=true → `Result<LoginResponse>.Success` with JWT token, `RequiresPasswordChange = true`
- Invalid username → `Result<LoginResponse>.Failure` with `ErrorCodes.NotFound`
- Invalid password → `Result<LoginResponse>.Failure` with `ErrorCodes.Unauthorized`; increments `LoginAttempts`
- Locked account (5 failed attempts) → `Result<LoginResponse>.Failure("الحساب مغلق مؤقتاً")`
- MustChangePassword = true + valid password → Login SUCCEEDS (not blocked), `RequiresPasswordChange = true`
- Successful login → AuditLog entry created with action "LoginSuccess"

**AuthService.ChangePasswordAsync()** (used for both first-login forced change and voluntary change):
- Valid request → `Result.Success()`; password hash updated; MustChangePassword = false; PasswordChangedAt set
- Incorrect current password → `Result.Failure("كلمة المرور الحالية غير صحيحة")`

**UserService.CreateAsync()**:
- Valid request → `Result<UserDto>.Success`; duplicate username → `Result<UserDto>.Failure`
- Missing required fields → validation failure from FluentValidation (not service)

**PermissionService.UpdateRolePermissionsAsync()**:
- Valid role+permission set → `Result.Success`; old mappings removed, new added; transaction committed
- Transaction rollback on failure

**AuditLogService.QueryAsync()**:
- Paginated results returned correctly; filters by userId, action, entityType, date range
- OrderByDescending(Timestamp) verified

#### 3. FluentValidation Tests

**ChangePasswordRequestValidator**:
- Valid request passes; missing CurrentPassword fails; NewPassword < 8 chars fails; ConfirmPassword != NewPassword fails

**CreateUserRequestValidator**:
- Valid passes (Password optional — defaults to "12345678"); empty UserName fails; invalid Role byte fails; Password < 8 chars (if provided) fails; Phone > 20 chars fails; invalid email format fails

#### 4. Database Configuration Tests

**UserConfiguration**: Verify `HasQueryFilter(u => u.IsActive)` applies soft-delete filter. Verify unique index on `UserName`. Verify `PasswordHash` uses `IsRequired()`. Verify `EmployeeId` FK uses `DeleteBehavior.Restrict`.

**PermissionConfiguration**: Verify unique index on `Name`. Verify FK to `RolePermission` uses `DeleteBehavior.Restrict`.

**AuditLogConfiguration**: Verify indexes on `(UserId, Timestamp DESC)`, `(EntityType, EntityId)`, `(Timestamp DESC)`. Verify FK to Users uses `DeleteBehavior.Restrict`. Verify `Id` uses `bigint` (UseIdentityColumn).

#### 5. Phase-specific Tests

- 45 permission codes match seed data exactly — check each against AGENTS.md Section 6 matrix
- User entity uses `IsActive` + `IsLocked` booleans (no `UserStatus` enum) — verify `HasQueryFilter(u => u.IsActive)` and `RecordLoginAttempt()` lockout logic
- MustChangePassword flow: login with default password succeeds with `RequiresPasswordChange = true`; desktop shows mandatory password change; after `ChangePasswordAsync()`, `MustChangePassword = false`; admin reset sets password back to hash of "12345678" and `MustChangePassword = true`
- Account lockout: 5 failed attempts → `IsLocked = true`; admin unlock → `LoginAttempts = 0`, `IsLocked = false`
- AuditLog: all audit events recorded correctly (LoginSuccess, LoginFailed, PasswordChanged, LoginBlocked_Locked)
- Permission matrix: all 9 roles (Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager) with correct permission assignments per AGENTS.md Section 6 matrix
- `UserSession.Terminate()` works across concurrent sessions
- Avatar upload: extension validation, size limit, resize behavior

**Estimate**: ~4 hours

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | No money fields in this phase | ✅ N/A |
| **RULE-002** | `decimal(18,3)` for ALL quantities | No quantity fields in this phase | ✅ N/A |
| **RULE-003** | Multi-table ops in transaction | PermissionService.UpdateRolePermissionsAsync — BeginTransactionAsync | ✅ |
| **RULE-006** | ALL services return `Result<T>` | AuditLogService, PermissionService, AuthService, UserService | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | All new entities (Permission, AuditLog, UserSession) | ✅ |
| **RULE-016** | BaseEntity audit fields | Permission inherits BaseEntity (CreatedAt, CreatedByUserId, IsActive) | ✅ |
| **RULE-024** | Services inject `IUnitOfWork` | AuditLogService, PermissionService, AuthService, UserService | ✅ |
| **RULE-035** | Serilog for logging | All services — login attempts, password changes, permission updates, audit writes | ✅ |
| **RULE-036** | Log critical operations | Login success/failure, account lockout, password change, permission update | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Verified — passwords never logged, audit stores no secrets | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` (except login) | AuditLogsController (AdminOnly), PermissionsController (AdminOnly), UsersController (AdminOnly) — NO `[AllowAnonymous]` SetPassword endpoint (removed in default-password redesign) | ✅ |
| **RULE-039** | BCrypt work factor 12 | AuthService.ChangePasswordAsync + UserService.CreateAsync | ✅ |
| **RULE-042** | Rich Domain — `private set` + domain methods | User: `RecordLoginAttempt()`, `Lock()`, `Unlock()`, `RecordLogin()` | ✅ |
| **RULE-044** | FluentValidation for EVERY Command | ChangePasswordRequestValidator, AuditLogQueryValidator, CreateUserRequestValidator | ✅ |
| **RULE-050** | DeleteStrategy for ALL deletes | UserService: PermanentDeleteAsync guarded (RULE-244) | ✅ |
| **RULE-052** | Guard Clauses on all entities | User.Create, AuditLog.Create, Permission.Create — all with Arabic DomainException | ✅ |
| **RULE-053** | DomainException in Arabic | All messages in Arabic: "اسم المستخدم مطلوب.", "الحساب مغلق مؤقتاً." | ✅ |
| **RULE-054** | IDialogService — no MessageBox | All ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all new ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | UserEditorViewModel, PasswordChangeViewModel | ✅ |
| **RULE-059** | Save always enabled, validate on click | All editor VMs — no CanExecute blocking | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | PasswordChangeViewModel, AuditLogListViewModel, PermissionManagementViewModel | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern everywhere | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | Permission management, Audit log browser, Password change — all OpenScreen() | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use HandleFailure() + LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | Arabic error messages for all user-facing failures | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في تغيير كلمة المرور"`, `"تحديث الصلاحيات"`, `"سجل الحركات"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync`, `ShowConfirmationAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable — password change crashes | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Login failed, invalid password, locked account — all Warning level | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | All new API services use base class content-type guard | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, MenuItems, inputs across all new XAML views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "فتح شاشة تغيير كلمة المرور" ✅, not "تغيير كلمة المرور" ❌ | ✅ |
| **RULE-187** | Action buttons explain consequences | Logout: "تسجيل الخروج من النظام — سيتم إغلاق الجلسة الحالية" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | "سجل الحركات — عرض وتصفية سجل عمليات النظام" | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | All empty-state buttons | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-199** | LogSystemError() is ONLY method for system error logging | All ViewModels use LogSystemError() — never direct Serilog.Log.Error | ✅ |
| **RULE-200** | ALL hard-delete catch DbUpdateException → Result.Failure | UserService.PermanentDeleteAsync — already guarded | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | AuditLogService, PermissionService, AuthService, UserService | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | AuditLogsController, PermissionsController — service only | ✅ |
| **RULE-214** | ALL FKs DeleteBehavior.Restrict | RolePermission → Permission (Restrict), AuditLog → User (Restrict) | ✅ |
| **RULE-220** | Newest-first sorting on lists | AuditLogListViewModel: OrderByDescending(Timestamp) | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | PasswordChangeViewModel, UserEditorViewModel | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | All editor VMs use AddError/ClearErrors | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation in all editor VMs | ✅ |
| **RULE-240** | Login endpoint rate limited (5/15min per IP) | AuthController.Login — [EnableRateLimiting("LoginPolicy")] already exists | ✅ |
| **RULE-241** | Global 100 req/min rate limit | Already in Program.cs | ✅ |
| **RULE-242** | Arabic 429 response | Already implemented | ✅ |
| **RULE-243** | Rate limiter before UseAuthentication() | Already in Program.cs pipeline | ✅ |
| **RULE-244** | User hard-delete — MUST return Result.Failure | UserService.PermanentDeleteAsync — already guarded | ✅ |
| **RULE-245** | Hard-delete attempt logged as warning | UserService.PermanentDeleteAsync — Log.LogWarning | ✅ |
| **RULE-246** | Users soft-deleted only | DeleteAsync → SoftDeleteAsync via IUnitOfWork | ✅ |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | All new XAML: compact 28px via styles | ✅ |
| **RULE-263** | No hardcoded Padding="16+" on buttons | All new XAML: 10,4 via styles | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | All new XAML views | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Between form fields in UserEditor, PasswordChange | ✅ |
| **RULE-266** | Dialog titles FontSize=16 max | All dialog windows | ✅ |
| **RULE-267** | Section headers FontSize=14 max | All section headers in UserEditor | ✅ |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | All empty-state views | ✅ |
| **RULE-269** | MainWindow sidebar Width=200 | Already set | ✅ N/A |
| **RULE-270** | Dialog icons: 44×44 max | All dialog windows | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | AuditLogBrowser, PermissionManagement | ✅ |
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | All dialogs | ✅ |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | All new XAML uses styles only | ✅ |

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Permission bypass via direct API call** | **HIGH** — user could call API endpoint they shouldn't access | Policy-based authorization on ALL endpoints; `[Authorize(Policy = "AdminOnly")]` on sensitive controllers; server-side permission check in services via `UserHasPermissionAsync()` |
| **Audit log table grows too fast** | Medium — query performance degrades | Composite indexes on (UserId, Timestamp DESC) and (EntityType, EntityId); retention cleanup worker; paginated queries with max 500 rows per page |
| **Avatar upload security (malicious file)** | Medium — XSS or RCE via uploaded image | Validate file extension (JPG/PNG/GIF only); validate magic bytes not just extension; resize to 128×128 max; store outside web root or serve via controller with content-type enforcement |
| **Account lockout DoS** | Medium — attacker can lock all accounts by repeated failed logins | Auto-unlock after 30 minutes; admin can unlock manually; only lock on consecutive failed attempts from same username; no lock on first failed attempt |
| **Session hijacking via JWT theft** | Medium — stolen token gives full access | Short expiry (8h sliding, 24h absolute); HTTPS-only; token stored in memory (SessionService) not localStorage; UserSession tracking for force-logout future |
| **Race condition on LoginAttempts** | Low — two concurrent requests could both fail and bypass lockout | Use `user.RecordLoginAttempt()` inside save scope; EF Core concurrency token on User row |
| **Role-permission desync between API and Desktop** | Low — Permission dot-notation codes mismatch between seed data and ViewModel checks | All 30 codes MUST match `Permission.Name` in seed data; integration test verifies all 30 seeded names; `HasPermission("Sales.View")` == `"Sales.View"` in DB; no enum conversion needed since DB-backed names are strings |
| **Password change without current password validation** | Medium — anyone with temporary access could change password | `AuthService.ChangePasswordAsync` validates current password via BCrypt.Verify before allowing change. |
| **Admin locks themselves out** | Low — last active admin cannot deactivate own account | UserService.DeleteAsync already guards against deactivating last admin. Same guard for locking last admin. |
| **AuditLog table bloat from automated actions** | Low — background workers generating excessive audit entries | Audit only CRITICAL actions (login, create/update/delete sensitive entities). Background workers do NOT generate audit entries. Configurable retention. |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| User entity extension causes issues | `ALTER TABLE Users DROP COLUMN Phone, Email, AvatarPath, LastLoginAt, LoginAttempts, IsLocked;` — columns are additive, no data loss on rollback |
| Permission module not needed | `DROP TABLE RolePermissions; DROP TABLE Permissions;` — remove controllers + services + ViewModels. Desktop Permission enum unaffected. |
| AuditLog table causes performance issues | `DROP TABLE AuditLogs;` — Serilog file logging still works. Remove AuditLogService + AuditLogsController. |
| UserSession tracking not needed | `DROP TABLE UserSessions;` — SessionService in-memory session still works. Remove UserSessionService. |
| Avatar storage has security issue | `DELETE FROM Users WHERE AvatarPath IS NOT NULL;` — avatar becomes null, system shows placeholder instead. Remove avatar upload endpoint. |
| Password change endpoint has bug | Revert `AuthController.cs` — remove `POST /api/v1/auth/change-password` endpoint. Users continue using old update method. |
| Permission Management UI not needed | Remove `PermissionManagementView.xaml` + `PermissionManagementViewModel.cs` + DI registration. DB seed data and PermissionService remain intact. |
| AuditLog browser UI has issues | Remove `AuditLogListView.xaml` + `AuditLogListViewModel.cs` + DI registration. API endpoints remain available for future use. |
