# Data Model: Phase 7 — Production Readiness

**Phase**: 1 — Design & Contracts  
**Date**: 2026-05-23

---

## Entities

### 1. User *(existing — verify/update)*

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK, auto-increment | |
| `FullName` | `nvarchar(150)` | Required, MaxLength 150 | Guard Clause: not null/empty |
| `UserName` | `nvarchar(100)` | Required, Unique index | Guard Clause: not null/empty |
| `PasswordHash` | `nvarchar(255)` | Required | BCrypt, work factor 12 |
| `Role` | `tinyint` | `UserRole` enum (1/2/3) | Admin=1, Manager=2, Cashier=3 |
| `IsActive` | `bit` | Default `true` | **Soft delete only** — NEVER hard delete |
| `CreatedAt` | `datetime2` | Set on creation | |
| `CreatedByUserId` | `int` | FK → Users, Nullable | Null for seeded admin |

**State transitions**:
- Created → Active (`IsActive = true`)
- Active → Deactivated (`IsActive = false`) — reversible by Admin
- **Forbidden**: Hard delete of any User record

**Guard Clauses (Domain)**:
```csharp
if (string.IsNullOrWhiteSpace(fullName)) throw new DomainException("الاسم الكامل مطلوب");
if (string.IsNullOrWhiteSpace(userName)) throw new DomainException("اسم المستخدم مطلوب");
if (role is < UserRole.Admin or > UserRole.Cashier) throw new DomainException("دور المستخدم غير صالح");
```

**Last-Admin Guard** (Application layer):
```csharp
var adminCount = await _uow.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive);
if (adminCount == 1 && user.Role == UserRole.Admin)
    return Result.Failure("لا يمكن إلغاء تفعيل آخر مسؤول في النظام");
```

---

### 2. StoreSettings *(existing — update)*

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK = 1 (single row) | |
| `StoreName` | `nvarchar(200)` | Required | |
| `PhoneNumber` | `nvarchar(50)` | Nullable | |
| `Address` | `nvarchar(500)` | Nullable | |
| `LogoPath` | `nvarchar(500)` | Nullable | File path on server disk |
| `DefaultTaxRate` | `decimal(18,2)` | Default 0.00 | Percentage |
| `IsTaxEnabled` | `bit` | Default `false` | |
| `DefaultWarehouseId` | `int` | FK → Warehouses, Nullable | |
| `CostingMethod` | `tinyint` | Default 1 | WeightedAverage=1, LastPurchasePrice=2, SupplierPrice=3 |
| `UpdatedAt` | `datetime2` | Set on every save | |
| `UpdatedByUserId` | `int` | FK → Users | |

---

### 3. SystemSettings *(existing — key-value store)*

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `int` | PK | |
| `Key` | `nvarchar(100)` | Unique index | e.g., `"CostingMethod"`, `"BackupRetentionDays"` |
| `Value` | `nvarchar(500)` | | String-encoded value |
| `Category` | `nvarchar(50)` | | e.g., `"Costing"`, `"Backup"`, `"Print"` |

**Seeded values**:
| Key | Value | Category |
|-----|-------|----------|
| `CostingMethod` | `1` | `Costing` |
| `BackupRetentionDays` | `30` | `Backup` |
| `BackupPath` | `C:\SalesSystemBackups` | `Backup` |
| `AutoUpdateEnabled` | `true` | `Update` |
| `SkippedVersion` | `` (empty) | `Update` |

---

### 4. BackupRecord *(implicit — no DB table)*

Backup files are managed on disk as timestamped `.bak` files. No database table required. Metadata is:
- **File name format**: `SalesSystem_{YYYYMMDD}_{HHmmss}.bak`
- **Location**: Configured `BackupPath` from SystemSettings
- **Listing**: `Directory.GetFiles(path, "*.bak").OrderByDescending(f => File.GetCreationTime(f))`

---

### 5. UpdateManifest *(remote JSON — no DB table)*

Fetched from GitHub Releases. Schema:

```json
{
  "version": "4.6.4",
  "releaseDate": "2026-05-23",
  "downloadUrl": "https://github.com/.../setup-4.6.4.exe",
  "sha256": "a3b4c5d6...",
  "releaseNotes": "..."
}
```

---

## Relationships

```
Users ────────────────── FK ──────► StoreSettings.UpdatedByUserId
Users ────────────────── FK ──────► CreatedByUserId (on all financial tables)
Warehouses ──────────── FK ──────► StoreSettings.DefaultWarehouseId
```

---

## Validation Rules

| Entity | Field | Rule |
|--------|-------|------|
| User | UserName | Unique across active + inactive users |
| User | Role | Must be 1, 2, or 3 |
| User | Deactivate | Cannot deactivate last active Admin |
| StoreSettings | DefaultTaxRate | `>= 0 AND <= 100` |
| StoreSettings | CostingMethod | Must be 1, 2, or 3 |
| Backup | BackupPath | Directory must exist and be writable before operation |
| Backup | RestoreFile | File must exist and be a valid `.bak` extension |
