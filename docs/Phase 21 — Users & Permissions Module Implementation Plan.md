# Phase 21 — Users & Permissions Module: Comprehensive Implementation Plan

> **Version**: 2.0 — Updated after full re-read of Analysis Part 5 (lines 3711-5044) — Added 4-role model, exact permission codes from analysis, passwordless user creation flow, MustChangePassword, UserStatus (Active/Inactive/Locked)
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
| Roles | 3 fixed (Admin=1, Manager=2, Cashier=3) | **4 roles** (Admin=1, Accountant=2, Cashier=3, Observer=4) per analysis |
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
| `Id` | `int PK` | ✅ | Inherited from `BaseEntity` |
| `UserName` | `string(50)` | ✅ | Unique index |
| `PasswordHash` | `string(256)` | ✅ | BCrypt hash |
| `FullName` | `string(150)` | ✅ | Display name |
| `Role` | `UserRole (byte)` | ✅ | 1=Admin, 2=Manager, 3=Cashier |
| `IsActive` | `bool` | ✅ | Global query filter |
| `CreatedAt` | `DateTime` | ✅ | Inherited from BaseEntity |
| `CreatedByUserId` | `int?` | ✅ | FK to Users table |
| `Phone` | `string(20)?` | ❌ Missing | **NEW** |
| `Email` | `string(100)?` | ❌ Missing | **NEW** |
| `AvatarPath` | `string(255)?` | ❌ Missing | **NEW** |
| `LastLoginAt` | `DateTime?` | ❌ Missing | **NEW** |
| `LoginAttempts` | `int` | ❌ Missing | **NEW** — default 0 |
| `IsLocked` | `bool` | ❌ Missing | **NEW** — default false |

**Factory methods**:
- `User.Create(userName, passwordHash, fullName, role, createdByUserId?)` — ✅ Exists
- `User.Update(fullName, role, updatedByUserId?)` — ✅ Exists
- `User.ChangePassword(newPasswordHash, updatedByUserId?)` — ✅ Exists

**Configuration**: `Infrastructure/Data/Configurations/UserConfiguration.cs` (21 lines) — ✅ Exists with:
- `.ToTable("Users")`, `.HasKey(u => u.Id)`
- `.Property(u => u.UserName).HasMaxLength(50).IsRequired()` + Unique Index
- `.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired()`
- `.Property(u => u.FullName).HasMaxLength(150).IsRequired()`
- `.Property(u => u.Role).HasConversion<byte>()`
- `.HasQueryFilter(u => u.IsActive)`

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
- `CreateAsync`: checks duplicate username, hashes password, saves
- `UpdateAsync`: updates fullName + role + optional password + IsActive
- `DeleteAsync`: soft delete with guard against deactivating last admin
- `PermanentDeleteAsync`: **GUARDED** — returns `Result.Failure("لا يمكن حذف المستخدمين بشكل نهائي")` per RULE-244
- All methods return `Result<T>` per RULE-006
- Uses `IUnitOfWork` per RULE-024

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
```csharp
public record UserDto(int Id, string UserName, string FullName, byte Role, bool IsActive);
```

**Requests** (in `SalesSystem.Contracts.Requests.UserRequests.cs`):
```csharp
public record CreateUserRequest(string UserName, string Password, string FullName, byte Role);
public record UpdateUserRequest(string FullName, byte Role, bool IsActive, string? Password);
```

**Responses** (in `SalesSystem.Contracts.Responses.LoginResponse.cs`):
```csharp
public record LoginResponse(int UserId, string UserName, string FullName, byte Role, string Token, DateTime ExpiresAt);
```

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
```sql
ALTER TABLE Users ADD Phone nvarchar(20) NULL;
ALTER TABLE Users ADD Email nvarchar(100) NULL;
ALTER TABLE Users ADD AvatarPath nvarchar(255) NULL;
ALTER TABLE Users ADD LastLoginAt datetime2 NULL;
ALTER TABLE Users ADD LoginAttempts int NOT NULL DEFAULT 0;
ALTER TABLE Users ADD IsLocked bit NOT NULL DEFAULT 0;
```

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

### 4.1 UserStatus Enum (NEW)

**File**: `Domain/Enums/UserStatus.cs`

```csharp
public enum UserStatus : byte
{
    Active = 1,
    Inactive = 2,
    Locked = 3
}
```

Note: Replaces the simple `IsActive` bool per analysis requirement (lines 5007-5018 of Analysis Part 5 — Active/Inactive/Locked states).

### 4.2 User Entity (Extended per Analysis Part 5 lines 4890-5043)

**File**: `Domain/Entities/User.cs` — Extended from current

**Critical analysis requirement**: Admin creates user WITHOUT password. User must set password on first login (MustChangePassword).

```csharp
public class User : BaseEntity
{
    // Existing
    public string UserName { get; private set; } = string.Empty;

    // CHANGED: PasswordHash is NULLABLE — user created without password (analysis lines 4908-4915)
    public string? PasswordHash { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    // REPLACES IsActive: UserStatus with Active/Inactive/Locked (analysis lines 5007-5018)
    public UserStatus Status { get; private set; } = UserStatus.Active;

    // NEW fields from analysis
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? AvatarPath { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public int LoginAttempts { get; private set; }

    // CHANGED: IsLocked replaced by UserStatus.Locked, but keep as computed helper
    public bool IsLocked => Status == UserStatus.Locked;

    // NEW: Password management (analysis lines 4890-5043)
    public bool MustChangePassword { get; private set; } = true;  // Default: must change on first login
    public DateTime? PasswordChangedAt { get; private set; }

    // NEW: Default cashbox assignment per user (analysis line 4905)
    public int? DefaultCashBoxId { get; private set; }

    protected User() { } // EF Core

    // CHANGED: PasswordHash is OPTIONAL — admin creates WITHOUT password (analysis lines 4898-4915)
    // MustChangePassword = true by default
    public static User Create(string userName, string fullName,
        UserRole role, string? phone = null, string? email = null,
        int? defaultCashBoxId = null, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("اسم المستخدم مطلوب.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("الاسم الكامل مطلوب.");
        return new User
        {
            UserName = userName.Trim(),
            FullName = fullName.Trim(),
            Role = role,
            Status = UserStatus.Active,
            Phone = phone?.Trim(),
            Email = email?.Trim(),
            DefaultCashBoxId = defaultCashBoxId,
            MustChangePassword = true,      // Forces password creation on first login
            PasswordHash = null,             // No password set by admin
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    // NEW: Set initial password on first login (analysis lines 4921-4949)
    public void SetInitialPassword(string passwordHash)
    {
        if (!MustChangePassword)
            throw new DomainException("كلمة المرور تم تعيينها مسبقاً.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("كلمة المرور مطلوبة.");
        PasswordHash = passwordHash;
        MustChangePassword = false;
        PasswordChangedAt = DateTime.UtcNow;
    }

    // NEW methods
    public void UpdateProfile(string fullName, UserRole role, string? phone, string? email,
        int? defaultCashBoxId = null, int? updatedByUserId = null)
    public void ChangePassword(string newPasswordHash, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("كلمة المرور الجديدة مطلوبة.");
        PasswordHash = newPasswordHash;
        PasswordChangedAt = DateTime.UtcNow;
        MustChangePassword = false;
        UpdatedByUserId = updatedByUserId;
        UpdatedAt = DateTime.UtcNow;
    }
    public void RecordLoginAttempt(bool success)  // Increments on fail, resets on success
    public void Lock()    // Status = UserStatus.Locked
    public void Unlock()  // Status = UserStatus.Active, LoginAttempts = 0
    public void SetAvatar(string avatarPath)
    public void ClearAvatar()
    public void RecordLogin() // Sets LastLoginAt, resets LoginAttempts = 0, MustChangePassword = false
    public void ResetPassword() // Admin reset: PasswordHash = null, MustChangePassword = true (analysis lines 4965-4986)
    public void Deactivate() // Status = UserStatus.Inactive (soft deactivate)
    public void Activate()   // Status = UserStatus.Active
}
```

**Guard Clauses** (RULE-052) — All updated:
- `if (string.IsNullOrWhiteSpace(userName))` → `throw new DomainException("اسم المستخدم مطلوب.")`
- `if (string.IsNullOrWhiteSpace(fullName))` → `throw new DomainException("الاسم الكامل مطلوب.")`
- `if (phone?.Length > 20)` → `throw new DomainException("رقم الهاتف لا يتجاوز 20 رقماً.")`
- `if (email?.Length > 100)` → `throw new DomainException("البريد الإلكتروني لا يتجاوز 100 حرفاً.")`
- `if (!MustChangePassword && string.IsNullOrWhiteSpace(PasswordHash))` → `throw new DomainException("كلمة المرور غير محددة.")` — integrity check
- `if (LoginAttempts < 0)` → `throw new DomainException("محاولات تسجيل الدخول غير صالحة.")`

### 4.3 UserConfiguration (Extended)

**File**: `Infrastructure/Data/Configurations/UserConfiguration.cs`

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.UserName).IsRequired().HasMaxLength(50);
        builder.HasIndex(u => u.UserName).IsUnique();

        // CHANGED: PasswordHash is NULLABLE (user created without password)
        builder.Property(u => u.PasswordHash).HasMaxLength(256);  // No IsRequired

        builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
        builder.Property(u => u.Role).IsRequired().HasConversion<byte>();

        // CHANGED: UserStatus replaces IsActive
        builder.Property(u => u.Status).IsRequired().HasConversion<byte>().HasDefaultValue(UserStatus.Active);

        // NEW properties
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.Email).HasMaxLength(100);
        builder.Property(u => u.AvatarPath).HasMaxLength(255);
        builder.Property(u => u.LastLoginAt);
        builder.Property(u => u.LoginAttempts).HasDefaultValue(0);

        // NEW: Password management fields
        builder.Property(u => u.MustChangePassword).HasDefaultValue(true);
        builder.Property(u => u.PasswordChangedAt);

        // NEW: DefaultCashBoxId FK (nullable)
        builder.Property(u => u.DefaultCashBoxId);
        builder.HasOne(u => u.DefaultCashBox)
            .WithMany()
            .HasForeignKey(u => u.DefaultCashBoxId)
            .OnDelete(DeleteBehavior.Restrict);

        // CHANGED: Query filter uses UserStatus, not IsActive
        // Cast to byte because HasConversion<byte>() prevents EF from translating enum constants
        builder.HasQueryFilter(u => (byte)u.Status == (byte)UserStatus.Active);
    }
}
```

### 4.4 Permission Entity (NEW — Using exact codes from Analysis Part 5 lines 3981-4022)

**File**: `Domain/Entities/Permission.cs`

```csharp
public class Permission : BaseEntity
{
    public string Name { get; private set; } = string.Empty;          // e.g., "Sales.View"
    public string DisplayNameAr { get; private set; } = string.Empty;  // e.g., "عرض فواتير البيع"
    public string Category { get; private set; } = string.Empty;       // e.g., "Sales"
    public bool IsSystem { get; private set; }                        // System = cannot delete

    private readonly List<RolePermission> _rolePermissions = new();
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    protected Permission() { }

    public static Permission Create(string name, string displayNameAr, string category, bool isSystem = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الصلاحية مطلوب");
        if (string.IsNullOrWhiteSpace(displayNameAr))
            throw new DomainException("الاسم العربي للصلاحية مطلوب");
        return new Permission
        {
            Name = name.Trim(),
            DisplayNameAr = displayNameAr.Trim(),
            Category = category.Trim(),
            IsSystem = isSystem,
            IsActive = true
        };
    }
}
```

### 4.5 RolePermission Join Entity (NEW — 4-role model per analysis)

**File**: `Domain/Entities/RolePermission.cs`

**Note**: The analysis (lines 3721-3737) specifies **4 roles**, not 3:
- 1 = مدير النظام (Admin) — all permissions
- 2 = محاسب (Accountant) — sales, purchases, reports, entries
- 3 = كاشير (Cashier) — sales only, cash receipt only
- 4 = مراقب (Observer) — reports only

The existing `UserRole` enum must be extended to add `Accountant = 2` (current Manager becomes obsolete) and `Observer = 4`.

```csharp
// EXTENDED UserRole enum:
public enum UserRole : byte
{
    Admin = 1,
    Accountant = 2,   // NEW: replaces old "Manager" — handles sales, purchases, reports
    Cashier = 3,
    Observer = 4      // NEW: reports-only access
}
```

```csharp
public class RolePermission
{
    public int Id { get; private set; }
    public UserRole Role { get; private set; }  // 1=Admin, 2=Accountant, 3=Cashier, 4=Observer
    public int PermissionId { get; private set; }
    public Permission Permission { get; private set; } = null!;

    protected RolePermission() { }

    public static RolePermission Create(UserRole role, int permissionId)
    {
        return new RolePermission
        {
            Role = role,
            PermissionId = permissionId
        };
    }
}
```

### 4.6 AuditLog Entity (NEW)

**File**: `Domain/Entities/AuditLog.cs`

```csharp
public class AuditLog
{
    public long Id { get; private set; }  // bigint PK (high volume)
    public int? UserId { get; private set; }
    public User? User { get; private set; }
    public string Action { get; private set; } = string.Empty;      // "Login", "CreateUser", "PostInvoice"
    public string EntityType { get; private set; } = string.Empty;   // "User", "SalesInvoice"
    public int? EntityId { get; private set; }
    public string? Details { get; private set; }                     // JSON diff or description
    public string? IpAddress { get; private set; }
    public DateTime Timestamp { get; private set; }

    protected AuditLog() { }

    public static AuditLog Create(int? userId, string action, string entityType,
        int? entityId = null, string? details = null, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("نوع الحركة مطلوب");
        if (string.IsNullOrWhiteSpace(entityType))
            throw new DomainException("نوع الكيان مطلوب");

        return new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };
    }
}
```

### 4.7 UserSession Entity (NEW)

**File**: `Domain/Entities/UserSession.cs`

```csharp
public class UserSession
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string TokenHash { get; private set; } = string.Empty;  // SHA256 of JWT
    public DateTime LoginAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsActive { get; private set; }

    protected UserSession() { }

    public static UserSession Create(int userId, string tokenHash,
        DateTime loginAt, int expirationHours = 8)
    {
        return new UserSession
        {
            UserId = userId,
            TokenHash = tokenHash,
            LoginAt = loginAt,
            LastActivityAt = loginAt,
            ExpiresAt = loginAt.AddHours(expirationHours),
            IsActive = true
        };
    }

    public void Touch() => LastActivityAt = DateTime.UtcNow;
    public void Terminate() => IsActive = false;
}
```

### 4.8 DTO Changes (Passwordless Creation)

**UserDto — Extended**:
```csharp
// Current:   UserDto(int Id, string UserName, string FullName, byte Role, bool IsActive)
// New: Status replaces IsActive, adds MustChangePassword, PasswordChangedAt, DefaultCashBoxId
public record UserDto(int Id, string UserName, string FullName, byte Role,
    byte Status,                       // UserStatus: 1=Active, 2=Inactive, 3=Locked
    bool MustChangePassword,           // NEW: true = user must set password on login
    DateTime? PasswordChangedAt,       // NEW: when password was last changed
    string? Phone, string? Email, string? AvatarPath,
    DateTime? LastLoginAt,
    int LoginAttempts,
    int? DefaultCashBoxId);            // NEW: default cashbox for user receipts
```

**Request Changes — Passwordless Creation**:
```csharp
// CHANGED (analysis lines 4898-4915): No Password field — admin creates WITHOUT password
public record CreateUserRequest(string UserName, string FullName, byte Role,
    string? Phone, string? Email, int? DefaultCashBoxId);

// CHANGED: IsActive replaced by Status
public record UpdateUserRequest(string FullName, byte Role, byte Status, string? Password,     // Password is optional (only when changing)
    string? Phone, string? Email, int? DefaultCashBoxId);

// NEW: Set password on first login (used by first-login flow)
public record SetPasswordRequest(string Password, string ConfirmPassword);

// NEW: Admin reset user password
public record ResetUserPasswordRequest(int UserId);
```

**New DTOs**:
```csharp
public record AuditLogDto(long Id, int? UserId, string? UserName, string Action,
    string EntityType, int? EntityId, string? Details, string? IpAddress, DateTime Timestamp);

public record PermissionDto(int Id, string Name, string DisplayNameAr, string Category, bool IsActive);
public record RolePermissionDto(UserRole Role, List<int> PermissionIds);

public record LoginHistoryDto(DateTime Timestamp, string Action, string? IpAddress, bool IsSuccess);
public record UserSessionDto(int Id, DateTime LoginAt, DateTime? LastActivityAt, bool IsActive);
public record CurrentUserDto(int Id, string UserName, string FullName, byte Role,
    string? AvatarPath, List<string> Permissions);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
public record AvatarUploadResponse(string AvatarUrl);
```

### 4.9 Seed Data — Permissions (30 exact codes from Analysis Part 5 lines 3981-4022)

**File**: `Infrastructure/Data/DbSeeder.cs`

**Note**: These are the EXACT 33 permission codes from Analysis Part 5 (lines 3981-4022). Each follows `Domain.Action` dot-notation. The CRUD + Post + Cancel model (analysis lines 4200-4243) separates View/Create/EditDraft/Post/Cancel as distinct permissions.

```csharp
// ═══════════════════════════════════════════════════════
// Seed Permissions (33 system permissions — Analysis Part 5 lines 3981-4022)
// Format: Domain.Action (dot-notation)
// ═══════════════════════════════════════════════════════
if (!await db.Set<Permission>().AnyAsync())
{
    var permissions = new List<Permission>
    {
        // ── Sales (7 permissions) ──
        Permission.Create("Sales.View",         "عرض فواتير البيع", "Sales", isSystem: true),
        Permission.Create("Sales.Create",       "إنشاء فاتورة بيع", "Sales", isSystem: true),
        Permission.Create("Sales.EditDraft",    "تعديل مسودة بيع", "Sales", isSystem: true),
        Permission.Create("Sales.Post",         "ترحيل فاتورة بيع", "Sales", isSystem: true),
        Permission.Create("Sales.Cancel",       "إلغاء فاتورة بيع", "Sales", isSystem: true),
        Permission.Create("Sales.ViewProfit",   "عرض الربح في البيع", "Sales", isSystem: true),
        Permission.Create("Sales.EditPrice",    "تعديل سعر البيع", "Sales", isSystem: true),

        // ── Purchases (5 permissions) ──
        Permission.Create("Purchase.View",       "عرض فواتير الشراء", "Purchase", isSystem: true),
        Permission.Create("Purchase.Create",     "إنشاء فاتورة شراء", "Purchase", isSystem: true),
        Permission.Create("Purchase.EditDraft",  "تعديل مسودة شراء", "Purchase", isSystem: true),
        Permission.Create("Purchase.Post",       "ترحيل فاتورة شراء", "Purchase", isSystem: true),
        Permission.Create("Purchase.Cancel",     "إلغاء فاتورة شراء", "Purchase", isSystem: true),

        // ── Inventory (3 permissions) ──
        Permission.Create("Inventory.View",      "عرض المخزون", "Inventory", isSystem: true),
        Permission.Create("Inventory.Transfer",  "نقل مخزني", "Inventory", isSystem: true),
        Permission.Create("Inventory.Adjust",    "تسوية مخزنية", "Inventory", isSystem: true),

        // ── Customers (3 permissions) ──
        Permission.Create("Customer.View",       "عرض العملاء", "Customer", isSystem: true),
        Permission.Create("Customer.Create",     "إضافة عميل", "Customer", isSystem: true),
        Permission.Create("Customer.Edit",       "تعديل عميل", "Customer", isSystem: true),

        // ── Suppliers (3 permissions) ──
        Permission.Create("Supplier.View",       "عرض الموردين", "Supplier", isSystem: true),
        Permission.Create("Supplier.Create",     "إضافة مورد", "Supplier", isSystem: true),
        Permission.Create("Supplier.Edit",       "تعديل مورد", "Supplier", isSystem: true),

        // ── Products (3 permissions) ──
        Permission.Create("Product.View",        "عرض المنتجات", "Product", isSystem: true),
        Permission.Create("Product.Create",      "إضافة منتج", "Product", isSystem: true),
        Permission.Create("Product.Edit",        "تعديل منتج", "Product", isSystem: true),

        // ── Reports (1 permission) ──
        Permission.Create("Reports.View",        "عرض التقارير", "Reports", isSystem: true),

        // ── Accounting (2 permissions) ──
        Permission.Create("Accounting.Manage",   "إدارة الحسابات", "Accounting", isSystem: true),
        Permission.Create("Accounting.Journal",  "قيد يومية", "Accounting", isSystem: true),

        // ── System (2 permissions) ──
        Permission.Create("Settings.Manage",     "إدارة الإعدادات", "System", isSystem: true),
        Permission.Create("UserManagement",      "إدارة المستخدمين", "System", isSystem: true),

        // ── Operations (3 permissions) ──
        Permission.Create("Backup.Manage",       "النسخ الاحتياطي", "Operations", isSystem: true),
        Permission.Create("Cashbox.Close",       "إغلاق الصندوق", "Operations", isSystem: true),
        Permission.Create("DeleteRecord",        "حذف سجل", "Operations", isSystem: true),

        // ── Audit (1 permission) ──
        Permission.Create("AuditLog.View",       "عرض سجل الحركات", "Audit", isSystem: true),
    };
    db.Set<Permission>().AddRange(permissions);
    await db.SaveChangesAsync(ct);

    // ════════════════════════════════════════
    // Seed RolePermissions (4 roles — Analysis Part 5 lines 3721-3737)
    // ════════════════════════════════════════
    var rolePermissions = new List<RolePermission>();
    var pByName = permissions.ToDictionary(p => p.Name);

    // ── Admin (1) = ALL 30 permissions ──
    foreach (var p in permissions)
        rolePermissions.Add(RolePermission.Create(UserRole.Admin, p.Id));

    // ── Accountant (2) = Sales, Purchases, Inventory, Customers, Suppliers, Products, Reports, Accounting, Journal, AuditLog ──
    var accountantPerms = new[]
    {
        "Sales.View", "Sales.Create", "Sales.EditDraft", "Sales.Post", "Sales.Cancel",
        "Purchase.View", "Purchase.Create", "Purchase.EditDraft", "Purchase.Post", "Purchase.Cancel",
        "Inventory.View", "Inventory.Transfer", "Inventory.Adjust",
        "Customer.View", "Customer.Create", "Customer.Edit",
        "Supplier.View", "Supplier.Create", "Supplier.Edit",
        "Product.View", "Product.Create", "Product.Edit",
        "Reports.View",
        "Accounting.Manage", "Accounting.Journal",
        "AuditLog.View"
    };
    foreach (var pName in accountantPerms)
        if (pByName.TryGetValue(pName, out var p))
            rolePermissions.Add(RolePermission.Create(UserRole.Accountant, p.Id));

    // ── Cashier (3) = Sales only + Customer View + Cashbox Close ──
    var cashierPerms = new[]
    {
        "Sales.View", "Sales.Create", "Sales.EditPrice",
        "Customer.View", "Customer.Create",
        "Inventory.View",
        "Cashbox.Close"
    };
    foreach (var pName in cashierPerms)
        if (pByName.TryGetValue(pName, out var p))
            rolePermissions.Add(RolePermission.Create(UserRole.Cashier, p.Id));

    // ── Observer (4) = Reports + View only ──
    var observerPerms = new[]
    {
        "Sales.View",
        "Purchase.View",
        "Inventory.View",
        "Customer.View",
        "Supplier.View",
        "Product.View",
        "Reports.View",
        "AuditLog.View"
    };
    foreach (var pName in observerPerms)
        if (pByName.TryGetValue(pName, out var p))
            rolePermissions.Add(RolePermission.Create(UserRole.Observer, p.Id));

    db.Set<RolePermission>().AddRange(rolePermissions);
    logger?.LogInformation("Seeded {Count} permissions and {RpCount} role-permission mappings.",
        permissions.Count, rolePermissions.Count);
}
```

### 4.9.1 Seed Data — Default Admin User

**Critical requirement** (Analysis Part 5 lines 4890-4919): The system MUST include a default admin account on first run so the user can log in immediately after installation.

**Design**:
- Password is NOT set during seeding (passwordless creation flow — Analysis Part 5)
- `MustChangePassword = true` forces the admin to set a password on first login
- The admin user has the `Admin` role with ALL permissions

```csharp
// ═══════════════════════════════════════════════════════
// Seed Default Admin User (passwordless — must change on first login)
// ═══════════════════════════════════════════════════════
if (!await db.Set<User>().AnyAsync())
{
    var adminUser = User.Create(
        username: "admin",
        nameAr: "مدير النظام",
        nameEn: "System Admin",
        role: UserRole.Admin,
        phone: "",
        email: "",
        createdByUserId: null  // System-created
    );
    // PasswordHash remains null — user MUST set password on first login
    adminUser.SetMustChangePassword(true);
    db.Set<User>().Add(adminUser);
    logger?.LogInformation("Seeded default admin user: admin/مدير النظام");
    await db.SaveChangesAsync(ct);
}
```

**Note on first-run flow**:
1. On first application launch, no users exist → `User.AnyAsync()` returns false
2. `DbSeeder` creates the admin user with null password
3. Login screen: user enters "admin" as username, leaves password empty (or enters anything — system detects MustChangePassword)
4. System redirects to password creation screen with message: "مرحباً بك في النظام — يرجى إنشاء كلمة المرور الخاصة بك"
5. After setting password, user can log in normally
6. Admin can then create additional users from the User Management screen

**IDs**: The seeded admin user always gets Id = 1 (first user in the system). This is referenced as `CreatedByUserId = 1` in seed data for other entities (customer cash, supplier cash, default warehouse, etc.).

**Security**:
- Without a password set, login attempts with any password fail
- The password creation screen requires: New Password + Confirm Password (minimum 8 chars, complexity requirements)
- After password is set, `MustChangePassword = false` and `PasswordHash` stores the BCrypt hash

### 4.10 IAuditLogService Interface (NEW)

```csharp
public interface IAuditLogService
{
    Task<Result> LogAsync(int? userId, string action, string entityType,
        int? entityId = null, string? details = null, string? ipAddress = null,
        CancellationToken ct = default);
    Task<Result<PaginatedResult<AuditLogDto>>> QueryAsync(AuditLogQuery query, CancellationToken ct = default);
    Task<Result<List<AuditLogDto>>> GetUserHistoryAsync(int userId, int limit = 50, CancellationToken ct = default);
    Task<Result<List<LoginHistoryDto>>> GetLoginHistoryAsync(int? userId = null, int limit = 50, CancellationToken ct = default);
}

public record AuditLogQuery
{
    public int? UserId { get; init; }
    public string? Action { get; init; }
    public string? EntityType { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
```

### 4.11 IPermissionService Interface (NEW)

```csharp
public interface IPermissionService
{
    Task<Result<List<PermissionDto>>> GetAllPermissionsAsync(CancellationToken ct = default);
    Task<Result<PermissionDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<List<RolePermissionDto>>> GetRolePermissionsAsync(CancellationToken ct = default);
    Task<Result> UpdateRolePermissionsAsync(UserRole role, List<int> permissionIds, CancellationToken ct = default);
    Task<Result<List<string>>> GetUserPermissionsAsync(int userId, CancellationToken ct = default);
    Task<Result<bool>> UserHasPermissionAsync(int userId, string permissionName, CancellationToken ct = default);
}
```

---

## 5. Gap Analysis

### 5.1 User Entity Fields (Updated per Analysis Part 5 lines 4890-5043)

| Field | Current | Required | Action |
|-------|---------|----------|--------|
| `UserName` | ✅ | ✅ | No change |
| `PasswordHash` | ✅ (NOT NULL) | ✅ (NULLABLE) | **CHANGE** — nullable (passwordless creation; admin never sets password) |
| `FullName` | ✅ | ✅ | No change |
| `Role` | ✅ (3 roles) | ✅ (4 roles) | **EXTEND** — add Accountant=2, Observer=4 |
| `IsActive (bool)` | ✅ | ❌ | **REPLACE** with `Status` enum (Active/Inactive/Locked) |
| `Status` (UserStatus) | ❌ | ✅ | **ADD** — enum: Active=1, Inactive=2, Locked=3 |
| `MustChangePassword` | ❌ | ✅ | **ADD** — bool default true (forces password creation on first login) |
| `PasswordChangedAt` | ❌ | ✅ | **ADD** — nullable datetime2 |
| `DefaultCashBoxId` | ❌ | ✅ | **ADD** — nullable int FK → CashBox.Id (assigns default cashbox per user) |
| `Phone` | ❌ | ✅ | ADD — nullable string(20) |
| `Email` | ❌ | ✅ | ADD — nullable string(100) |
| `AvatarPath` | ❌ | ✅ | ADD — nullable string(255) |
| `LastLoginAt` | ❌ | ✅ | ADD — nullable datetime2 |
| `LoginAttempts` | ❌ | ✅ | ADD — int default 0 |

### 5.2 Auth (Updated per Analysis Part 5 lines 4890-4995)

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| User creation flow | Requires password | **Passwordless** (analysis lines 4898-4915) | **CHANGE** — `CreateUserRequest` has NO Password field; admin creates user without password; `PasswordHash` = null, `MustChangePassword` = true |
| First login flow | Normal password check | **Must set password** (analysis lines 4921-4949) | **CHANGE** — if `MustChangePassword=true`, redirect to `SetPassword` page; user creates password on first login |
| Password reset (admin) | Admin can set password | **Admin forces reset** (analysis lines 4965-4986) | **CHANGE** — admin chooses "إعادة تعيين كلمة المرور" → `PasswordHash` = null, `MustChangePassword` = true; user must set new password on next login |
| BCrypt work factor 12 | ✅ | ✅ | No change |
| JWT token generation | ✅ | ✅ | No change |
| Track login attempts | ❌ | ✅ | ADD — `RecordLoginAttempt()` + lock after 5 fails |
| Account lockout | ❌ | ✅ | ADD — `Status = UserStatus.Locked`, login returns "الحساب مغلق مؤقتاً" |
| Auto-unlock | ❌ | ✅ | ADD — deferred to V2 (manual unlock via Admin for now) |
| Last login display | ❌ | ✅ | ADD — `User.LastLoginAt` on success |
| Password change endpoint | ❌ | ✅ | ADD — `POST /api/v1/auth/change-password` |
| Set password endpoint | ❌ | ✅ | ADD — `POST /api/v1/auth/set-password` (for first-login flow) |
| Rate limiting | ✅ | ✅ | Already on login endpoint |

### 5.3 Permissions (Updated per Analysis Part 5 lines 3981-4022)

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| Roles | 3 (Admin/Manager/Cashier) | **4** (Admin/Accountant/Cashier/Observer) | **CHANGE** — add Accountant=2 and Observer=4; Manager=2 renumbered to Accountant=2 |
| Permission model | 14 flat flags + policies | **30 dot-notation codes** per analysis (lines 3981-4022) | **CHANGE** — use `Domain.Action` codes (e.g. `Sales.View`, `Sales.Create`, `Sales.Post`, `Sales.Cancel`) |
| CRUD + Post + Cancel | Not separated | **Separate** per operation (analysis lines 4200-4243) | **ADD** — View, Create, EditDraft, Post, Cancel as separate permissions per domain |
| Permission codes | Simple names | **Dot-notation** (analysis lines 3981-4022) | **CHANGE** — 30 permission codes in Domain.Action format |
| DB model | Flat flags enum | **DB-backed** (Permission + RolePermission tables) | **CHANGE** — create NEW entities |
| SQL Server table | ❌ | ✅ | CREATE `Permissions` + `RolePermissions` |
| Seed data | ❌ | ✅ | ADD 30 permissions + 4-role mappings |
| Admin configuration UI | ❌ | ✅ | CREATE Permission Management screen |
| API permission check | ✅ (policies) | ✅ | Same + `UserHasPermissionAsync()` |
| Observer role (reports only) | ❌ | ✅ | ADD — Reports.View only |

### 5.4 Audit & Logging

| Feature | Current | Required | Action |
|---------|---------|----------|--------|
| Serilog file logging | ✅ | ✅ | No change |
| AuditLog DB table | ❌ | ✅ | CREATE table + service + endpoints |
| Audit log browser UI | ❌ | ✅ | CREATE ViewModel + View |
| Login history tracking | ❌ | ✅ | ADD via AuditLog + User.LastLoginAt |
| User activity per-user | ❌ | ✅ | ADD query endpoint + UI |

### 5.5 Desktop — Missing Screens

| Screen | Current | Required | Action |
|--------|---------|----------|--------|
| Enhanced UserEditor (Phone, Email, Avatar) | ❌ | ✅ | UPDATE existing ViewModel + View |
| Password Change screen | ❌ | ✅ | CREATE new ViewModel + View |
| Audit Log Browser | ❌ | ✅ | CREATE new ViewModel + View |
| Permission Management (Admin) | ❌ | ✅ | CREATE new ViewModel + View |
| Current User indicator with avatar | ❌ | ✅ | UPDATE MainWindow StatusBar |

### 5.6 API — Missing Endpoints

| Endpoint | Current | Required | Action |
|----------|---------|----------|--------|
| `POST /api/v1/auth/change-password` | ❌ | ✅ | ADD |
| `POST /api/v1/users/avatar` | ❌ | ✅ | ADD — avatar upload |
| `GET /api/v1/users/current` | ❌ | ✅ | ADD — current user profile |
| `GET /api/v1/audit-logs` | ❌ | ✅ | ADD — paginated, filterable |
| `GET /api/v1/audit-logs/user/{id}` | ❌ | ✅ | ADD — per-user history |
| `GET /api/v1/audit-logs/login-history` | ❌ | ✅ | ADD — login history |
| `GET /api/v1/permissions` | ❌ | ✅ | ADD — list all permissions |
| `GET /api/v1/permissions/roles` | ❌ | ✅ | ADD — role-permission mappings |
| `PUT /api/v1/permissions/roles/{role}` | ❌ | ✅ | ADD — update role permissions |

---

## 6. Architectural Decisions

### 6.1 Permission Model: DB-Backed with Dot-Notation Codes

The current `[Flags] Permission` enum in DesktopPWF will be migrated to **DB-backed permissions** with 30 exact dot-notation codes from Analysis Part 5 lines 3981-4022.

**Decision**: 
- **Permission codes** use `Domain.Action` format: `Sales.View`, `Sales.Create`, `Sales.EditDraft`, `Sales.Post`, `Sales.Cancel`, etc.
- **CRUD + Post + Cancel** model per analysis (lines 4200-4243): View, Create, EditDraft, Post, Cancel as separate permissions for Sales and Purchase domains
- **4 roles** per analysis (lines 3721-3737): Admin (1), Accountant (2), Cashier (3), Observer (4)
- `SessionService` will:
  1. On login, load `List<string> UserPermissions` from API via `GetUserPermissionsAsync(role)`
  2. Cache in-memory as `HashSet<string>`
  3. `HasPermission("Sales.View")` = `_permissionNames.Contains("Sales.View")`
  4. The old flat `Permission` enum is **replaced** by DB-backed permission names
- **Observer role** gets only `View` + `Reports.View` + `AuditLog.View` permissions (reports-only access)

### 6.2 Audit Log Storage: Database (V1)

**Decision**: Store in SQL Server `AuditLog` table with:
- **Clustered index**: `IX_AuditLog_Timestamp` on `(Timestamp DESC)`
- **Non-clustered index**: `IX_AuditLog_UserId` on `(UserId, Timestamp DESC)`
- **Non-clustered index**: `IX_AuditLog_Entity` on `(EntityType, EntityId)`
- **Partitioning**: Deferred to V2 (table is small enough < 5M rows/year)
- **Retention**: `AuditLogRetentionCleanupService` runs daily, deletes records older than configured days

### 6.3 Avatar Storage: File System + API

**Decision**:
- Avatar images stored in `wwwroot/uploads/avatars/` directory
- Filename: `avatar_{userId}_{timestamp}.{ext}`
- Size limit: 2 MB, allowed formats: JPG, PNG, GIF
- API serves via `GET /api/v1/users/{id}/avatar`
- Server resizes to 128×128 max on upload
- Desktop shows avatar in StatusBar (small 32×32) and UserEditor (128×128)

### 6.4 Password Policy (Updated per Analysis Part 5 lines 4890-4995)

- **Passwordless creation**: Admin creates user WITHOUT password (analysis lines 4898-4915). `PasswordHash` = null, `MustChangePassword` = true
- **First login**: User must set password via `POST /api/v1/auth/set-password` (analysis lines 4921-4949). After setting password, `MustChangePassword` = false, `PasswordChangedAt` = now
- **Minimum length**: 8 characters (validated by FluentValidation)
- **BCrypt work factor**: 12 (RULE-039 — already implemented)
- **No password expiry** in V1 (deferred to future phase)
- **Account lockout**: 5 failed attempts → `Status = UserStatus.Locked`, login returns "الحساب مغلق مؤقتاً"
- **Admin unlock**: Admin sets `Status = UserStatus.Active`, `LoginAttempts = 0` via UserEditor
- **Password reset (Admin)**: Admin clicks "إعادة تعيين كلمة المرور" → `PasswordHash` = null, `MustChangePassword` = true, `Status` unchanged. User must set new password on next login (analysis lines 4965-4986)

### 6.5 Session Timeout

- **Sliding**: 8 hours (reset on each API call via middleware)
- **Absolute**: 24 hours (user must re-login)
- Tracked via `UserSession` table + JWT expiry claim
- Desktop: `SessionService` checks `IsAuthenticated` and token expiry

### 6.6 4-Role Model (per Analysis Part 5 lines 3721-3737)

The analysis specifies **4 roles** with clear separation of duties:

```text
Admin (1)      = مدير النظام     → Full access (all 30 permissions)
Accountant (2) = محاسب          → Sales, Purchases, Inventory, Customers, Suppliers,
                                   Products, Reports, Journal Entries, Audit Logs
Cashier (3)    = كاشير          → Sales only (view/create, customer view), cashbox close
Observer (4)   = مراقب          → Reports only (view sales/purchases/inventory/reports/audit)
```

**Note**: The old `Manager` role (2) is **replaced** by `Accountant` (2). The `Observer` role (4) is **new**. Any existing user with `Role=2 (Manager)` must be **migrated** to `Role=2 (Accountant)` — same value, new name. Role-based authorization policies (AGENTS.md Section 6) must be updated to reflect these 4 roles.
```

**Decision**: Keep the existing **flat role-permission mapping**. It is simpler, more flexible, and admin-configurable. No role hierarchy complexity in V1.

### 6.7 Why NOT Two-Factor Authentication (2FA)

2FA was mentioned in analysis as "نظام التشفير للحماية" (Encryption system). This refers to BCrypt for password hashing (already implemented), NOT a separate 2FA system. True 2FA (TOTP/SMS) is deferred to a future phase.

---

## 7. Non-V1 Items (Deferred)

| Feature | Reason |
|---------|--------|
| Two-Factor Authentication (2FA/TOTP) | Complex UX, low retail demand |
| LDAP / Active Directory Integration | Enterprise feature, out of scope |
| IP-based access restriction | Requires network infrastructure |
| User group management | Over-engineering for V1 — fixed 4 roles suffice |
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

### Task 1 — Rebuild User Entity (Passwordless Creation + UserStatus + MustChangePassword + Phone/Email/Avatar/DefaultCashBox) + Migration

**Files**:

| File | Change |
|------|--------|
| `Domain/Enums/UserStatus.cs` | **NEW** — enum: Active=1, Inactive=2, Locked=3 |
| `Domain/Entities/User.cs` | **REBUILD**: PasswordHash nullable, add Status, MustChangePassword, PasswordChangedAt, DefaultCashBoxId, Phone, Email, AvatarPath, LastLoginAt, LoginAttempts + all domain methods |
| `Infrastructure/Data/Configurations/UserConfiguration.cs` | Update Fluent API — PasswordHash nullable, Status replaces IsActive, add all new fields, query filter on Status |
| `Infrastructure/Data/Migrations/` | New migration: schema changes |
| `Contracts/DTOs/AllDtos.cs` — `UserDto` | Extend with all new fields |
| `Contracts/Requests/UserRequests.cs` | Passwordless CreateUserRequest (no Password field), add SetPasswordRequest, ResetUserPasswordRequest |

**User.Create — Passwordless (Analysis Part 5 lines 4898-4915)**:
```csharp
public static User Create(string userName, string fullName, UserRole role,
    string? phone = null, string? email = null, int? defaultCashBoxId = null,
    int? createdByUserId = null)
{
    if (string.IsNullOrWhiteSpace(userName))
        throw new DomainException("اسم المستخدم مطلوب.");
    if (string.IsNullOrWhiteSpace(fullName))
        throw new DomainException("الاسم الكامل مطلوب.");
    return new User
    {
        UserName = userName.Trim(),
        FullName = fullName.Trim(),
        Role = role,
        Status = UserStatus.Active,
        Phone = phone?.Trim(),
        Email = email?.Trim(),
        DefaultCashBoxId = defaultCashBoxId,
        MustChangePassword = true,       // Forces password creation on first login
        PasswordHash = null,              // No password set by admin
        LoginAttempts = 0,
        CreatedByUserId = createdByUserId,
        CreatedAt = DateTime.UtcNow
    };
}
```

**Domain methods**:
```csharp
public void RecordLoginAttempt(bool success)
{
    if (success)
    {
        LoginAttempts = 0;
        Status = UserStatus.Active;
        LastLoginAt = DateTime.UtcNow;
    }
    else
    {
        LoginAttempts++;
        if (LoginAttempts >= 5)
            Status = UserStatus.Locked;
    }
}

public void SetInitialPassword(string passwordHash)  // First-login flow (analysis lines 4921-4949)
{
    if (!MustChangePassword)
        throw new DomainException("كلمة المرور تم تعيينها مسبقاً.");
    if (string.IsNullOrWhiteSpace(passwordHash))
        throw new DomainException("كلمة المرور مطلوبة.");
    PasswordHash = passwordHash;
    MustChangePassword = false;
    PasswordChangedAt = DateTime.UtcNow;
}

public void ResetPassword()  // Admin reset flow (analysis lines 4965-4986)
{
    PasswordHash = null;
    MustChangePassword = true;
    UpdatedAt = DateTime.UtcNow;
}

public void ChangePassword(string newPasswordHash, int? updatedByUserId = null)
{
    if (string.IsNullOrWhiteSpace(newPasswordHash))
        throw new DomainException("كلمة المرور الجديدة مطلوبة.");
    PasswordHash = newPasswordHash;
    PasswordChangedAt = DateTime.UtcNow;
    MustChangePassword = false;
    UpdatedByUserId = updatedByUserId;
    UpdatedAt = DateTime.UtcNow;
}

public void Lock() => Status = UserStatus.Locked;
public void Unlock() => Status = UserStatus.Active;
public void Deactivate() => Status = UserStatus.Inactive;
public void Activate() => Status = UserStatus.Active;
public void SetAvatar(string avatarPath) => AvatarPath = avatarPath;
public void ClearAvatar() => AvatarPath = null;
```

**Logging**: 
- `Log.Information("User {UserId} profile updated: phone={Phone}, email={Email}", id, phone, email)`
- `Log.Warning("User {UserId} locked out after {Attempts} failed attempts", userId, attempts)`
- `Log.Information("Password reset by admin for user {UserId}", userId)` — RULE-182

**Validation** (RULE-044):
- `CreateUserRequestValidator` — UserName required, FullName required, Role valid, Phone max 20 chars (optional), Email max 100 chars + valid format (optional)
- `UpdateUserRequestValidator` — Same
- `SetPasswordRequestValidator` — Password min 8 chars, ConfirmPassword must match
- `ChangePasswordRequestValidator` — CurrentPassword required, NewPassword min 8 chars

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

**PermissionConfiguration**:
```csharp
public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.Name).IsUnique();
        builder.Property(p => p.DisplayNameAr).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Category).HasMaxLength(50);
        builder.HasQueryFilter(p => p.IsActive);

        builder.HasMany(p => p.RolePermissions)
            .WithOne(rp => rp.Permission)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);  // RULE-214
    }
}
```

**RolePermissionConfiguration**:
```csharp
public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");
        builder.HasKey(rp => rp.Id);
        builder.Property(rp => rp.Role).IsRequired().HasConversion<byte>();
        builder.HasIndex(rp => new { rp.Role, rp.PermissionId }).IsUnique();
    }
}
```

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

**AuditLogConfiguration**:
```csharp
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        // Use bigint for high-volume audit table
        builder.Property(a => a.Id).UseIdentityColumn(1, 1);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Details).HasMaxLength(2000);
        builder.Property(a => a.IpAddress).HasMaxLength(50);

        // Indexes for performance
        builder.HasIndex(a => a.Timestamp).IsDescending();
        builder.HasIndex(a => new { a.UserId, a.Timestamp }).IsDescending();
        builder.HasIndex(a => new { a.EntityType, a.EntityId });

        // FK to Users with Restrict (RULE-214)
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

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
```csharp
public async Task<Result> LogAsync(int? userId, string action, string entityType,
    int? entityId = null, string? details = null, string? ipAddress = null,
    CancellationToken ct = default)
{
    var log = AuditLog.Create(userId, action, entityType, entityId, details, ipAddress);
    await _uow.AuditLogs.AddAsync(log, ct);
    await _uow.SaveChangesAsync(ct);
    return Result.Success();
}

public async Task<Result<PaginatedResult<AuditLogDto>>> QueryAsync(AuditLogQuery query, CancellationToken ct)
{
    var q = _uow.AuditLogs.GetQueryable();

    if (query.UserId.HasValue)
        q = q.Where(a => a.UserId == query.UserId);
    if (!string.IsNullOrWhiteSpace(query.Action))
        q = q.Where(a => a.Action == query.Action);
    if (!string.IsNullOrWhiteSpace(query.EntityType))
        q = q.Where(a => a.EntityType == query.EntityType);
    if (query.From.HasValue)
        q = q.Where(a => a.Timestamp >= query.From.Value);
    if (query.To.HasValue)
        q = q.Where(a => a.Timestamp <= query.To.Value);

    var total = await q.CountAsync(ct);
    var items = await q.OrderByDescending(a => a.Timestamp)  // RULE-220
        .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize)
        .Select(a => new AuditLogDto(a.Id, a.UserId, a.User!.UserName, a.Action,
            a.EntityType, a.EntityId, a.Details, a.IpAddress, a.Timestamp))
        .ToListAsync(ct);

    return Result<PaginatedResult<AuditLogDto>>.Success(
        new PaginatedResult<AuditLogDto>(items, total, query.Page, query.PageSize));
}
```

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
```csharp
public async Task<Result> UpdateRolePermissionsAsync(UserRole role,
    List<int> permissionIds, CancellationToken ct)
{
    await using var tx = await _uow.BeginTransactionAsync(ct);
    try
    {
        // Remove existing mappings for this role
        var existing = await _uow.RolePermissions.GetQueryable()
            .Where(rp => rp.Role == role)
            .ToListAsync(ct);
        _uow.RolePermissions.RemoveRange(existing);

        // Add new mappings
        foreach (var permId in permissionIds)
        {
            var rp = RolePermission.Create(role, permId);
            await _uow.RolePermissions.AddAsync(rp, ct);
        }

        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("Permissions updated for role {Role}: {Count} permissions", role, permissionIds.Count);
        return Result.Success();
    }
    catch
    {
        await tx.RollbackAsync(ct);
        throw;
    }
}
```

**Logging**: `Log.Warning("Permission {PermissionId} is system-locked — cannot modify", id)` on IsSystem guard
**Validation**: Must have at least 1 permission per role

**Estimate**: ~1.5 hours

---

### Task 7 — Update AuthService (Passwordless Flow, SetPassword, Login Attempts, Lockout, Audit Trail)

**Files**:

| File | Change |
|------|--------|
| `Application/Services/AuthService.cs` | After BCrypt verify: call `user.RecordLoginAttempt(success)`, check `Status == Locked`, write AuditLog entry; ADD `SetPasswordAsync()` for first-login flow |
| `Application/Interfaces/Services/IAuthService.cs` | Add `ChangePasswordAsync()` and `SetPasswordAsync()` method signatures |
| `Application/Services/AuthService.cs` | Add `SetPasswordAsync(SetPasswordRequest, int userId)` + `ChangePasswordAsync(ChangePasswordRequest, int userId)` |

**NEW: SetPasswordAsync — First-login passwordless flow (Analysis Part 5 lines 4921-4949)**:
```csharp
public async Task<Result> SetPasswordAsync(SetPasswordRequest request, int userId, CancellationToken ct)
{
    var user = await _uow.Users.GetByIdAsync(userId, ct);
    if (user == null)
        return Result.Failure("المستخدم غير موجود", ErrorCodes.NotFound);
    
    // User must be in MustChangePassword state
    if (!user.MustChangePassword)
        return Result.Failure("كلمة المرور تم تعيينها مسبقاً", ErrorCodes.ValidationError);

    // Verify passwords match
    if (request.Password != request.ConfirmPassword)
        return Result.Failure("كلمة المرور وتأكيدها غير متطابقتين", ErrorCodes.ValidationError);

    // Verify minimum length
    if (request.Password.Length < 8)
        return Result.Failure("كلمة المرور يجب أن تكون 8 أحرف على الأقل", ErrorCodes.ValidationError);

    // Hash and save
    string newHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);
    user.SetInitialPassword(newHash);
    await _uow.SaveChangesAsync(ct);

    _logger.LogInformation("Initial password set for user {UserId} (first login)", userId);
    await _auditLogService.LogAsync(userId, "InitialPasswordSet", "User", userId,
        "First login password set", null, ct);

    return Result.Success();
}
```

**Updated LoginAsync logic — Check MustChangePassword**:
```csharp
public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
{
    // 1. Find user
    // 2. Check if active
    // 3. Check if locked
    if (user.Status == UserStatus.Locked)
    {
        _logger.LogWarning("Login blocked: User {UserName} is locked", request.UserName);
        await _auditLogService.LogAsync(user.Id, "LoginBlocked_Locked", "User", user.Id,
            "Account locked due to too many failed attempts", ipAddress, ct);
        return Result<LoginResponse>.Failure("الحساب مغلق مؤقتاً. يرجى الاتصال بالمدير",
            ErrorCodes.Forbidden);
    }

    // 4. Check MustChangePassword (passwordless creation)
    if (user.MustChangePassword || string.IsNullOrWhiteSpace(user.PasswordHash))
    {
        _logger.LogInformation("User {UserName} must set password before login", request.UserName);
        return Result<LoginResponse>.Failure("يجب تعيين كلمة المرور قبل تسجيل الدخول لأول مرة",
            ErrorCodes.RequiresPasswordSetup);
    }

    // 5. Verify password
    bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
    user.RecordLoginAttempt(isPasswordValid);

    if (!isPasswordValid)
    {
        _logger.LogWarning("Login failed: Incorrect password for user {UserName} (attempt {Attempts})",
            request.UserName, user.LoginAttempts);
        await _auditLogService.LogAsync(user.Id, "LoginFailed", "User", user.Id,
            $"Failed login attempt #{user.LoginAttempts}", ipAddress, ct);
        await _uow.SaveChangesAsync(ct);

        if (user.Status == UserStatus.Locked)
            return Result<LoginResponse>.Failure("الحساب مغلق مؤقتاً. يرجى الاتصال بالمدير",
                ErrorCodes.Forbidden);

        return Result<LoginResponse>.Failure("بيانات الاعتماد غير صالحة", ErrorCodes.Unauthorized);
    }

    // 6. Success — record login
    await _auditLogService.LogAsync(user.Id, "LoginSuccess", "User", user.Id, null, ipAddress, ct);

    // 7. Generate JWT
    string token = _jwtTokenGenerator.GenerateToken(user);
    // ... return LoginResponse
}
```

**ChangePasswordAsync**:
```csharp
public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, int userId, CancellationToken ct)
{
    var user = await _uow.Users.GetByIdAsync(userId, ct);
    if (user == null)
        return Result.Failure("المستخدم غير موجود", ErrorCodes.NotFound);

    // Verify current password
    if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
    {
        _logger.LogWarning("Password change failed: Incorrect current password for user {UserId}", userId);
        await _auditLogService.LogAsync(userId, "PasswordChangeFailed", "User", userId,
            "Incorrect current password", null, ct);
        return Result.Failure("كلمة المرور الحالية غير صحيحة", ErrorCodes.ValidationError);
    }

    // Verify new password matches confirm
    if (request.NewPassword != request.ConfirmPassword)
        return Result.Failure("كلمة المرور الجديدة وتأكيدها غير متطابقتين", ErrorCodes.ValidationError);

    // Verify minimum length
    if (request.NewPassword.Length < 8)
        return Result.Failure("كلمة المرور يجب أن تكون 8 أحرف على الأقل", ErrorCodes.ValidationError);

    // Hash and save
    string newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
    user.ChangePassword(newHash, userId);
    await _uow.SaveChangesAsync(ct);

    _logger.LogInformation("Password changed for user {UserId}", userId);
    await _auditLogService.LogAsync(userId, "PasswordChanged", "User", userId, null, null, ct);

    return Result.Success();
}
```

**Validation** (RULE-044):
- `ChangePasswordRequestValidator`: CurrentPassword required, NewPassword min 8 chars, ConfirmPassword must match
- `SetPasswordRequestValidator`: Password min 8 chars, ConfirmPassword must match
- `LoginRequestValidator`: UserName required, Password required (skipped when MustChangePassword)

**Logging**:
- `Log.Warning("Login blocked: User {UserName} is locked")` — user mistake (RULE-183)
- `Log.Warning("Login failed: Incorrect password for user {UserName} (attempt {Attempts})")` — user mistake
- `Log.Information("Password changed for user {UserId}")` — system operation (RULE-182)
- `Log.Information("Initial password set for user {UserId} (first login)")` — system operation
- Never log the password itself (RULE-037)

**Estimate**: ~2.5 hours

---

### Task 8 — Create New API Endpoints (SetPassword, Password Change, Reset Password, Avatar, Current User, Audit Log, Permissions)

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/AuthController.cs` | Add `POST /api/v1/auth/set-password` (first login — NO auth required), `POST /api/v1/auth/change-password` (authenticated), `POST /api/v1/auth/reset-password` (admin only) |
| `Api/Controllers/UsersController.cs` | Add `GET /api/v1/users/current`, `POST /api/v1/users/avatar`, `GET /api/v1/users/{id}/avatar`, `POST /api/v1/users/{id}/reset-password` (admin) |
| `Api/Controllers/AuditLogsController.cs` | **NEW** — 4 endpoints for audit log query |
| `Api/Controllers/PermissionsController.cs` | **NEW** — 3 endpoints for permission management |

**AuthController additions**:
```csharp
[AllowAnonymous]
[HttpPost("set-password")]
public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request,
    [FromQuery] int userId, CancellationToken ct)
{
    var result = await _authService.SetPasswordAsync(request, userId, ct);
    if (result.IsSuccess)
        return Ok(new { message = "تم تعيين كلمة المرور بنجاح" });
    return BadRequest(new { error = result.Error });
}

[Authorize]
[HttpPost("change-password")]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var result = await _authService.ChangePasswordAsync(request, userId, ct);
    if (result.IsSuccess)
        return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
    return BadRequest(new { error = result.Error });
}
```

**UsersController additions**:
```csharp
[HttpGet("current")]
public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var result = await _userService.GetByIdAsync(userId, ct);
    return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
}

[HttpPost("{id:int}/reset-password")]
[Authorize(Policy = "AdminOnly")]
public async Task<IActionResult> ResetPassword(int id, CancellationToken ct)
{
    var result = await _userService.ResetPasswordAsync(id, ct);
    if (result.IsSuccess)
        return Ok(new { message = "تم إعادة تعيين كلمة المرور — سيطلب من المستخدم تعيين كلمة جديدة عند تسجيل الدخول" });
    return BadRequest(new { error = result.Error });
}

// POST /api/v1/users/avatar — upload avatar
// GET /api/v1/users/{id}/avatar — serve avatar image
```

**AuditLogsController**:
```csharp
[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Policy = "AdminOnly")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] AuditLogQuery query, CancellationToken ct)
    {
        var result = await _auditLogService.QueryAsync(query, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("user/{userId:int}")]
    public async Task<IActionResult> GetUserHistory(int userId, [FromQuery] int limit = 50, CancellationToken ct)
    {
        var result = await _auditLogService.GetUserHistoryAsync(userId, limit, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("login-history")]
    public async Task<IActionResult> GetLoginHistory([FromQuery] int? userId, [FromQuery] int limit = 50, CancellationToken ct)
    {
        var result = await _auditLogService.GetLoginHistoryAsync(userId, limit, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
```

**PermissionsController**:
```csharp
[ApiController]
[Route("api/v1/permissions")]
[Authorize(Policy = "AdminOnly")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) { /* ... */ }

    [HttpGet("roles")]
    public async Task<IActionResult> GetRolePermissions(CancellationToken ct) { /* ... */ }

    [HttpPut("roles/{role}")]
    public async Task<IActionResult> UpdateRolePermissions(byte role, [FromBody] List<int> permissionIds, CancellationToken ct) { /* ... */ }
}
```

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
```csharp
private string _phone = string.Empty;
private string _email = string.Empty;
private string? _avatarUrl;
private bool _isLocked;
private bool _hasAvatar;
private byte[]? _avatarImageData;  // For preview

public string Phone { get => _phone; set { /* validate + set */ } }
public string Email { get => _email; set { /* validate + set */ } }
public string? AvatarUrl { get => _avatarUrl; set => SetProperty(ref _avatarUrl, value); }
public bool IsLocked { get => _isLocked; set => SetProperty(ref _isLocked, value); }
public bool HasAvatar { get => _hasAvatar; set => SetProperty(ref _hasAvatar, value); }

public ICommand UploadAvatarCommand { get; private set; }
public ICommand RemoveAvatarCommand { get; private set; }
public ICommand ChangePasswordCommand { get; private set; }
```

**ValidateAsync expanded**:
```csharp
private async Task<bool> ValidateAsync()
{
    var errors = new List<string>();
    if (string.IsNullOrWhiteSpace(Username))
        errors.Add("• اسم المستخدم مطلوب — تأكد من إدخال اسم فريد للدخول إلى النظام");
    if (string.IsNullOrWhiteSpace(FullName))
        errors.Add("• الاسم بالكامل مطلوب — سيظهر هذا الاسم في الفواتير والتقارير");
    if (!IsEditMode && string.IsNullOrWhiteSpace(Password))
        errors.Add("• كلمة المرور مطلوبة — يجب أن تكون كلمة مرور قوية لحماية الحساب");
    if (!string.IsNullOrWhiteSpace(Email) && !IsValidEmail(Email))
        errors.Add("• البريد الإلكتروني غير صالح — أدخل بريداً إلكترونياً صحيحاً");
    if (!string.IsNullOrWhiteSpace(Phone) && Phone.Length > 20)
        errors.Add("• رقم الهاتف لا يتجاوز 20 رقماً");

    if (errors.Any())
    {
        await _dialogService.ShowValidationErrorsAsync("بيانات غير مكتملة", errors);
        RequestFocusFirstInvalidField();
        return false;
    }
    return true;
}
```

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
```csharp
public class PasswordChangeViewModel : ViewModelBase
{
    private readonly IAuthApiService _authService;
    private readonly IDialogService _dialogService;
    private readonly ISessionService _sessionService;

    private string _currentPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;

    // Properties with INotifyDataErrorInfo validation
    // ValidateAsync: all 3 fields required, NewPassword min 8, ConfirmPassword must match

    public ICommand SaveCommand { get; private set; }
    public ICommand CancelCommand { get; private set; }

    private async Task SaveOperationAsync()
    {
        if (!await ValidateAsync()) return;

        var request = new ChangePasswordRequest(CurrentPassword, NewPassword, ConfirmPassword);
        var result = await _authService.ChangePasswordAsync(request);

        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync("تغيير كلمة المرور", "تم تغيير كلمة المرور بنجاح");
            RequestClose();
        }
        else
        {
            ErrorMessage = HandleFailure(result.Error ?? "فشل في تغيير كلمة المرور",
                "PasswordChangeViewModel.SaveAsync",
                "[PasswordChangeViewModel.SaveAsync] Password change failed.");
            await _dialogService.ShowErrorAsync("خطأ في تغيير كلمة المرور", ErrorMessage!);
        }
    }
}
```

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
```csharp
public class AuditLogListViewModel : AdminOnlyViewModel
{
    private readonly IAuditLogApiService _auditLogService;
    private readonly IDialogService _dialogService;

    // Filters
    private string? _searchText;
    private string? _selectedAction;
    private string? _selectedEntityType;
    private DateTime? _fromDate;
    private DateTime? _toDate;
    private int _page = 1;
    private int _totalPages;
    private int _totalRecords;

    public ObservableCollection<AuditLogDto> Logs { get; } = new();
    public ObservableCollection<string> ActionFilters { get; }  // Distinct actions from API
    public ObservableCollection<string> EntityTypeFilters { get; }  // Distinct entity types

    public ICommand SearchCommand { get; private set; }
    public ICommand NextPageCommand { get; private set; }
    public ICommand PrevPageCommand { get; private set; }
    public ICommand RefreshCommand { get; private set; }
    public ICommand ViewUserHistoryCommand { get; private set; }
    public ICommand ViewLoginHistoryCommand { get; private set; }

    private async Task LoadLogsOperationAsync()
    {
        var query = new AuditLogQuery
        {
            UserId = null,  // Could parse searchText as int
            Action = SelectedAction,
            EntityType = SelectedEntityType,
            From = FromDate,
            To = ToDate,
            Page = Page,
            PageSize = 50
        };
        var result = await _auditLogService.QueryAsync(query);
        if (result.IsSuccess && result.Value != null)
        {
            await InvokeOnUIThreadAsync(() =>
            {
                Logs.Clear();
                foreach (var log in result.Value.Items)
                    Logs.Add(log);
                TotalPages = result.Value.TotalPages;
                TotalRecords = result.Value.TotalCount;
            });
        }
    }
}
```

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
```csharp
public class PermissionManagementViewModel : AdminOnlyViewModel
{
    private readonly IPermissionApiService _permissionService;
    private readonly IEventBus _eventBus;

    public ObservableCollection<PermissionCategoryGroup> Categories { get; } = new();
    // Each PermissionCategoryGroup contains:
    //   string CategoryName
    //   ObservableCollection<PermissionCheckItem> Permissions
    // RoleSelection = Admin, Manager, Cashier (tabs or radio buttons)

    private UserRole _selectedRole = UserRole.Admin;
    public UserRole SelectedRole { get => _selectedRole; set { /* reload check states */ } }

    public ICommand SaveCommand { get; private set; }
    public ICommand SelectAllCommand { get; private set; }
    public ICommand DeselectAllCommand { get; private set; }

    public record PermissionCheckItem : ViewModelBase
    {
        public int Id { get; init; }
        public string Name { get; init; }
        public string DisplayNameAr { get; init; }
        private bool _isChecked;
        public bool IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }
    }

    private async Task SaveOperationAsync()
    {
        var selectedIds = Categories.SelectMany(c => c.Permissions)
            .Where(p => p.IsChecked)
            .Select(p => p.Id)
            .ToList();

        var result = await _permissionService.UpdateRolePermissionsAsync(SelectedRole, selectedIds);
        if (result.IsSuccess)
        {
            await _dialogService.ShowSuccessAsync("تحديث الصلاحيات",
                $"تم تحديث صلاحيات دور {GetRoleDisplayName(SelectedRole)} بنجاح");
            _eventBus.Publish(new PermissionsChangedMessage());
        }
    }
}
```

**XAML structure**:
```xml
<!-- 4-role selector tabs (Analysis Part 5 lines 3721-3737) -->
<RadioButton Content="مدير النظام" IsChecked="{Binding IsAdminSelected}"/>
<RadioButton Content="محاسب" IsChecked="{Binding IsAccountantSelected}"/>
<RadioButton Content="كاشير" IsChecked="{Binding IsCashierSelected}"/>
<RadioButton Content="مراقب" IsChecked="{Binding IsObserverSelected}"/>

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
```csharp
private void ApplyPermissions()
{
    var s = _session;

    // Sales
    NavSalesItem.Visibility = s.HasPermission("Sales.View") ? Visibility.Visible : Visibility.Collapsed;
    NavSalesReturnsItem.Visibility = s.HasPermission("Sales.View") ? Visibility.Visible : Visibility.Collapsed;

    // Purchases
    NavPurchasesItem.Visibility = s.HasPermission("Purchase.View") ? Visibility.Visible : Visibility.Collapsed;
    NavPurchaseReturnsItem.Visibility = s.HasPermission("Purchase.View") ? Visibility.Visible : Visibility.Collapsed;

    // Products
    NavProductsItem.Visibility = s.HasPermission("Product.View") ? Visibility.Visible : Visibility.Collapsed;

    // Customers
    NavCustomersItem.Visibility = s.HasPermission("Customer.View") ? Visibility.Visible : Visibility.Collapsed;
    NavCustomerPaymentsItem.Visibility = s.HasPermission("Customer.View") ? Visibility.Visible : Visibility.Collapsed;

    // Suppliers
    NavSuppliersItem.Visibility = s.HasPermission("Supplier.View") ? Visibility.Visible : Visibility.Collapsed;
    NavSupplierPaymentsItem.Visibility = s.HasPermission("Supplier.View") ? Visibility.Visible : Visibility.Collapsed;

    // Inventory
    NavStockTransfersItem.Visibility = s.HasPermission("Inventory.Transfer") ? Visibility.Visible : Visibility.Collapsed;
    NavLowStockItem.Visibility = s.HasPermission("Reports.View") ? Visibility.Visible : Visibility.Collapsed;

    // Reports
    NavReportsItem.Visibility = s.HasPermission("Reports.View") ? Visibility.Visible : Visibility.Collapsed;

    // Settings (Admin only)
    NavCategoriesItem.Visibility = s.HasPermission("Settings.Manage") ? Visibility.Visible : Visibility.Collapsed;
    NavUnitsItem.Visibility = s.HasPermission("Settings.Manage") ? Visibility.Visible : Visibility.Collapsed;
    NavWarehousesItem.Visibility = s.HasPermission("Settings.Manage") ? Visibility.Visible : Visibility.Collapsed;
    NavUsersItem.Visibility = s.HasPermission("UserManagement") ? Visibility.Visible : Visibility.Collapsed;
    NavSettingsItem.Visibility = s.HasPermission("Settings.Manage") ? Visibility.Visible : Visibility.Collapsed;

    // NEW: Audit log menu (Admin only)
    NavAuditLogItem.Visibility = s.HasPermission("AuditLog.View") ? Visibility.Visible : Visibility.Collapsed;
    // NEW: Permission management menu (Admin only)
    NavPermissionsItem.Visibility = s.HasPermission("UserManagement") ? Visibility.Visible : Visibility.Collapsed;
}
```

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
```csharp
// NEW API Services
services.AddSingleton<IAuditLogApiService, AuditLogApiService>();
services.AddSingleton<IPermissionApiService, PermissionApiService>();

// NEW ViewModels
services.AddTransient<PasswordChangeViewModel>();
services.AddTransient<AuditLogListViewModel>();
services.AddTransient<PermissionManagementViewModel>();
```

**Estimate**: ~45 minutes

### Task 16 — Unit Tests

**Files**: NEW test files in `SalesSystem.Domain.Tests`, `SalesSystem.Application.Tests`, `SalesSystem.Api.Tests`, `SalesSystem.Infrastructure.Tests`

#### 1. Domain Entity Tests

**User.Create()** — Test with valid inputs creates entity with `Status = UserStatus.Active`, `MustChangePassword = true`, `PasswordHash = null`. Test with empty `userName` → `DomainException("اسم المستخدم مطلوب.")`. Test with empty `fullName` → `DomainException("الاسم الكامل مطلوب.")`. Test `RecordLoginAttempt(true)` resets `LoginAttempts` to 0 and sets `LastLoginAt`. Test `RecordLoginAttempt(false)` increments counter; after 5 failures → `Status = UserStatus.Locked`. Test `SetInitialPassword()` sets hash, sets `MustChangePassword = false`. Test `SetInitialPassword()` when `MustChangePassword = false` → `DomainException`. Test `Lock()`, `Unlock()`, `Deactivate()`, `Activate()` — status transitions. Test `ChangePassword()` with null/empty hash → `DomainException`.

**Permission.Create()** — Valid inputs create entity. Empty `name` → `DomainException("اسم الصلاحية مطلوب")`. Empty `displayNameAr` → `DomainException("الاسم العربي للصلاحية مطلوب")`. `IsSystem = true` locks permissions.

**AuditLog.Create()** — Valid inputs create entity with `Timestamp` set to `DateTime.UtcNow`. Empty `action` → `DomainException`. Empty `entityType` → `DomainException`.

**UserSession.Create()** — Valid inputs create active session. `ExpiresAt` = `loginAt + 8 hours`. `Touch()` updates `LastActivityAt`. `Terminate()` sets `IsActive = false`.

#### 2. Service Tests (using Mock<IUnitOfWork>)

**AuthService.LoginAsync()**:
- Valid credentials → `Result<LoginResponse>.Success` with JWT token
- Invalid username → `Result<LoginResponse>.Failure` with `ErrorCodes.NotFound`
- Invalid password → `Result<LoginResponse>.Failure` with `ErrorCodes.Unauthorized`; increments `LoginAttempts`
- Locked account (5 failed attempts) → `Result<LoginResponse>.Failure("الحساب مغلق مؤقتاً")`
- `MustChangePassword = true` → `Result<LoginResponse>.Failure("يجب تعيين كلمة المرور")`
- Successful login → AuditLog entry created with action "LoginSuccess"

**AuthService.SetPasswordAsync()**:
- Valid request → `Result.Success()`; user.PasswordHash set; MustChangePassword = false
- User not found → `Result.Failure` with `ErrorCodes.NotFound`
- MustChangePassword = false → `Result.Failure("كلمة المرور تم تعيينها مسبقاً")`

**AuthService.ChangePasswordAsync()**:
- Valid request → `Result.Success()`; password hash updated; password history updated
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

**SetPasswordRequestValidator**:
- Valid request passes; Password < 8 chars fails; ConfirmPassword mismatch fails

**CreateUserRequestValidator**:
- Valid passes; empty UserName fails; invalid Role byte fails; Phone > 20 chars fails; invalid email format fails

#### 4. Database Configuration Tests

**UserConfiguration**: Verify `HasQueryFilter` translates `(byte)u.Status == (byte)UserStatus.Active`. Verify unique index on `UserName`. Verify `PasswordHash` nullable (no `IsRequired()`). Verify `DefaultCashBoxId` FK uses `DeleteBehavior.Restrict`.

**PermissionConfiguration**: Verify unique index on `Name`. Verify FK to `RolePermission` uses `DeleteBehavior.Restrict`.

**AuditLogConfiguration**: Verify indexes on `(UserId, Timestamp DESC)`, `(EntityType, EntityId)`, `(Timestamp DESC)`. Verify FK to Users uses `DeleteBehavior.Restrict`. Verify `Id` uses `bigint` (UseIdentityColumn).

#### 5. Phase-specific Tests

- 33 permission codes match seed data exactly — check each against Section 1.2 table
- EF Core query filter: `(byte)u.Status == (byte)UserStatus.Active` translates to correct SQL
- MustChangePassword flow: login redirects to password change when `MustChangePassword = true`; admin reset sets `PasswordHash = null` and `MustChangePassword = true`
- Account lockout: 5 failed attempts → `UserStatus.Locked`; admin unlock → `LoginAttempts = 0`, `Status = UserStatus.Active`
- `DefaultCashBoxId` FK integrity: delete CashBox → Restrict prevents if user references it
- AuditLog: all audit events recorded correctly (LoginSuccess, LoginFailed, PasswordChanged, InitialPasswordSet, LoginBlocked_Locked)
- Permission matrix: all 4 roles (Admin/Manager/Cashier/Observer) with correct CRUD per Section 1.2 table values
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
| **RULE-038** | ALL endpoints `[Authorize]` (except login) | AuditLogsController (AdminOnly), PermissionsController (AdminOnly), UsersController (AdminOnly) | ✅ |
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
