# Database Schema Design
# Sales Management System — V1 Final (Module-by-Module Organization)
# Platform: SQL Server 2019+
# 66 Tables | decimal-only financials | nvarchar text | Soft delete | All FK Restrict

---

# 1) Important Design Rules Before Tables

## Data Types
- **Primary Keys**: `int IDENTITY(1,1)` — Named `Id` in all entities. High-volume audit tables use `bigint`. Small lookup tables use `smallint`.
- **Texts**: `nvarchar` (to support Arabic/English).
- **Barcodes**: `varchar(50)` — barcodes are ASCII-only.
- **Currency/Money**: `decimal(18,2)` — Precision 18, Scale 2.
- **Quantities**: `decimal(18,3)` — Precision 18, Scale 3.
- **Percentages**: `decimal(5,2)` — Precision 5, Scale 2.
- **Status and Types**: `tinyint` (for Enums).
- **Flags**: `bit` (0/1).
- **Dates**: `date` for document dates, `datetime2` for timestamps.

## Base Inheritance Hierarchy

Five base classes control column inheritance:

| Class | Id | CreatedAt | CreatedByUserId | UpdatedAt | UpdatedByUserId | IsActive | Status |
|-------|----|-----------|-----------------|-----------|-----------------|----------|--------|
| **Entity** | int PK | — | — | — | — | — | — |
| **AuditableEntity** | int PK | datetime2 | int null | datetime2 null | int null | — | — |
| **ActivatableEntity** | int PK | datetime2 | int null | datetime2 null | int null | bit default 1 | — |
| **DocumentEntity** | int PK | datetime2 | int null | datetime2 null | int null | — | tinyint |
| **LongEntity** | bigint PK | — | — | — | — | — | — |

Status values for DocumentEntity: `1=Draft, 2=Posted, 3=Cancelled`

## Inheritance Rules by Table Category
- **Admin entities** (Customers, Suppliers, etc.): `ActivatableEntity` — has IsActive for soft delete
- **Document entities** (Invoices, Vouchers, etc.): `DocumentEntity` — has Status (Draft/Posted/Cancelled)
- **Junction tables** (UserRoles, RolePermissions, etc.): `Entity` — no audit fields needed
- **Live balances** (WarehouseStocks): `AuditableEntity` — no soft delete, tracks who updated
- **High-volume logs** (AuditLogs, SystemLogs): `LongEntity` — bigint PK

## Key Constraints
- **All FK relationships**: `DeleteBehavior.Restrict` — NO cascade delete anywhere.
- **Soft delete**: Global query filter `IsActive == true` on ActivatableEntity inheritors.
- **CHECK constraints**: Stock quantities, financial amounts, debit/credit rules enforced at DB level.
- **Filtered unique indexes**: Soft-deletable entities include `AND [IsActive] = 1` to prevent conflicts with soft-deleted records.

---

# 2) Database
Database Name: **`SalesSystemDb`**
Default schema: **`dbo`**

---

# 3) Module-by-Module Table Definitions

---

## Module 1: Core & Security (النواة والأمان)

### 1.1 Customers
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `Name` | nvarchar(200) not null | |
| `Phone` | nvarchar(20) null | |
| `Email` | nvarchar(100) null | |
| `Address` | nvarchar(500) null | |
| `TaxNumber` | nvarchar(30) null | |
| `Notes` | nvarchar(1000) null | |
| `AccountId` | int not null FK → Accounts(Id) | every customer = an account |
| `CategoryId` | int null FK → AccountCategories(Id) | |
| `CreditLimit` | decimal(18,2) not null default 0 | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **Indexes** | `AccountId`, `CategoryId`, `Name`, `Phone` | |

### 1.2 CustomerContacts
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `CustomerId` | int not null FK → Customers(Id) | |
| `Name` | nvarchar(150) not null | |
| `Phone` | nvarchar(30) null | |
| `Email` | nvarchar(100) null | |
| `Position` | nvarchar(100) null | |
| `Notes` | nvarchar(300) null | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 1.3 Suppliers
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `Name` | nvarchar(200) not null | |
| `Phone` | nvarchar(20) null | |
| `Email` | nvarchar(100) null | |
| `Address` | nvarchar(500) null | |
| `TaxNumber` | nvarchar(30) null | |
| `Notes` | nvarchar(1000) null | |
| `AccountId` | int not null FK → Accounts(Id) | every supplier = an account |
| `CategoryId` | int null FK → AccountCategories(Id) | |
| `CreditLimit` | decimal(18,2) not null default 0 | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **Indexes** | `AccountId`, `CategoryId`, `Name`, `Phone` | |

### 1.4 SupplierContacts
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `SupplierId` | int not null FK → Suppliers(Id) | |
| `Name` | nvarchar(150) not null | |
| `Phone` | nvarchar(30) null | |
| `Email` | nvarchar(100) null | |
| `Position` | nvarchar(100) null | |
| `Notes` | nvarchar(300) null | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 1.5 Users
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `UserName` | nvarchar(50) not null | unique |
| `PasswordHash` | nvarchar(256) not null | |
| `PermissionsMask` | bigint not null default 0 | **Active permission system** — bitwise flags. `-1` = Super Admin (all 64 bits = 1, bypasses all checks). `0` = no permissions. |
| `MustChangePassword` | bit not null default 0 | |
| `LoginAttempts` | smallint not null default 0 | |
| `IsLocked` | bit not null default 0 | |
| `LastLoginAt` | datetime2 null | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **Index** | `UNIQUE(UserName)` | |

> **Permission Check**: `(User.PermissionsMask & requiredPermission) == requiredPermission`. Super Admin: `PermissionsMask = -1`.
> **Role Assignment**: When admin assigns a role to a user → `User.PermissionsMask = Role.PermissionsMask`. The user's mask can be further customized after role assignment.

### 1.6 Roles
| Column | Type | Notes |
|--------|------|-------|
| `Id` | **smallint** PK | |
| `Name` | nvarchar(100) not null | unique |
| `PermissionsMask` | bigint not null default 0 | sum of permission bit flags for this role |
| `IsSystem` | bit not null default 0 | system roles (9 default) protected from deletion/rename |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

**Seeded Default Roles** (`IsSystem = 1`):

| Id | Name (Arabic) | PermissionsMask |
|----|---|-|
| 1 | مدير النظام | `-1` (Super Admin — all permissions) |
| 2 | مدير | Sum of all except System/Users management |
| 3 | محاسب | Accounting + Reports + View permissions |
| 4 | أمين صندوق | CashBox + ReceiptVoucher + PaymentVoucher |
| 5 | كاشير | Sales.Create + Sales.Post + Customer.View |
| 6 | مشرف مخازن | Inventory.* + Product.View |
| 7 | مندوب مبيعات | Sales.* + Customer.* + Product.View |
| 8 | مراقب | *.View + Reports.View + AuditLog.View only |

> **Custom Roles**: Admin can add new roles beyond the 8 defaults. `IsSystem = 0` roles can be edited/deleted. Role is just a named template — assigning it copies `PermissionsMask` to the user. No join table needed.


### 1.7 UserSessions
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `UserId` | int not null FK → Users(Id) | |
| `SessionToken` | nvarchar(200) not null | |
| `DeviceName` | nvarchar(200) null | |
| `IpAddress` | nvarchar(50) null | |
| `UserAgent` | nvarchar(500) null | |
| `LastActivityAt` | datetime2 not null | |
| `ExpiresAt` | datetime2 not null | |
| `IsRevoked` | bit not null default 0 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **Index** | `(UserId, IsRevoked)` | for active session lookup |

---

## Module 2: Organization, Currencies & Settings (التنظيم والإعدادات)

### 2.1 Warehouses
| Column | Type | Notes |
|--------|------|-------|
| `Id` | **smallint** PK | |
| `Name` | nvarchar(150) not null | |
| `Phone` | nvarchar(30) null | |
| `Address` | nvarchar(300) null | |
| `Notes` | nvarchar(500) null | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |


### 2.2 Taxes
| Column | Type | Notes |
|--------|------|-------|
| `Id` | **smallint** PK | |
| `Name` | nvarchar(100) not null | |
| `Code` | nvarchar(20) not null | e.g., "VAT-15" |
| `Rate` | decimal(5,2) not null | CHK 0-100 |
| `TaxType` | tinyint not null | 1=Standard, 2=ZeroRated, 3=Exempt |
| `IsDefault` | bit not null default 0 | unique filtered `IsDefault=1 AND IsActive=1` |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 2.3 CompanySettings
| Column | Type | Notes |
|--------|------|-------|
| `Id` | tinyint PK default 1 | singleton row |
| `CompanyName` | nvarchar(200) not null | |
| `Phone` | nvarchar(30) null | |
| `Email` | nvarchar(100) null | |
| `Address` | nvarchar(300) null | |
| `TaxNumber` | nvarchar(50) null | |
| `LogoPath` | nvarchar(500) null | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 2.4 SystemSettings
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `SettingKey` | nvarchar(100) not null | unique |
| `SettingValue` | nvarchar(500) not null | |
| `SettingType` | tinyint not null | 1=String, 2=Integer, 3=Decimal, 4=Boolean |
| `Category` | nvarchar(100) not null | |
| `DisplayName` | nvarchar(200) not null | |
| `Description` | nvarchar(1000) null | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 2.5 DocumentSequences
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `DocumentType` | nvarchar(50) not null | e.g., "SalesInvoice", "PurchaseInvoice" |
| `NextNumber` | int not null | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 2.6 FiscalYears
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `YearName` | nvarchar(20) not null | e.g., "2026" |
| `StartDate` | date not null | |
| `EndDate` | date not null | CHK `EndDate > StartDate` |
| `IsClosed` | bit not null default 0 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 2.7 Notifications
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `UserId` | int not null FK → Users(Id) | |
| `Type` | tinyint not null | 1=LowStock, 2=ExpirySoon, 3=CreditLimitExceeded, 4=System, 5=Reminder |
| `Title` | nvarchar(200) not null | |
| `Message` | nvarchar(1000) not null | |
| `ReferenceType` | nvarchar(50) null | |
| `ReferenceId` | int null | |
| `IsRead` | bit not null default 0 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **Indexes** | `(UserId, IsRead, CreatedAt DESC)` | for unread notification queries |

### 2.8 Attachments
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `ReferenceType` | nvarchar(50) not null | e.g., "PurchaseInvoice", "InventoryAdjustment" |
| `ReferenceId` | int not null | |
| `FileName` | nvarchar(255) not null | |
| `FilePath` | nvarchar(500) not null | |
| `FileSize` | bigint not null | |
| `ContentType` | nvarchar(100) null | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **Index** | `(ReferenceType, ReferenceId)` | for document attachment lookup |

---

## Module 3: Products (المنتجات)

### 3.1 ProductCategories
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `Name` | nvarchar(100) not null | unique |
| `Description` | nvarchar(500) null | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 3.2 Products
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `Name` | nvarchar(200) not null | |
| `Barcode` | varchar(50) null | unique filtered `Barcode IS NOT NULL AND IsActive=1` |
| `CategoryId` | int not null FK → ProductCategories(Id) | |
| `Description` | nvarchar(500) null | |
| `TrackExpiry` | bit not null default 0 | |
| `ImagePath` | nvarchar(500) null | primary image |
| `ReorderLevel` | decimal(18,3) not null default 0 | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 3.3 Units
| Column | Type | Notes |
|--------|------|-------|
| `Id` | **smallint** PK | |
| `Name` | nvarchar(50) not null | e.g., "حبة", "كرتون" |
| `Symbol` | nvarchar(20) null | e.g., "pc", "box" |
| `IsSystem` | bit not null default 0 | system units protected from modification |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 3.4 ProductUnits
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `ProductId` | int not null FK → Products(Id) | |
| `UnitId` | smallint not null FK → Units(Id) | |
| `Factor` | decimal(18,3) not null | conversion factor: base=1, box=24 |
| `IsBaseUnit` | bit not null default 0 | exactly one base unit per product |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 3.5 ProductPrices
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `ProductUnitId` | int not null FK → ProductUnits(Id) | |
| `Price` | decimal(18,2) not null | CHK `>= 0` |
| `EffectiveFrom` | date not null | |
| `EffectiveTo` | date null | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

**Design Notes:**
- NO `ProductCost`, `StockQuantity`, or `UnitPrice` stored directly on Products table
- Cost comes from `InventoryBatches`, stock from `WarehouseStocks`, prices from `ProductPrices`
- Barcode is `varchar(50)` (ASCII-only), not `nvarchar` — one barcode per product
- Tax is on **invoice level** (`SalesInvoices.TaxId`, `PurchaseInvoices.TaxId`) — NOT on Products (per analysis: same product may be exempt or taxable depending on invoice context)
- Opening stock is a **separate inventory transaction** (InventoryAdjustment + InventoryBatches + WarehouseStocks + JournalEntry) — NOT columns on Products
- Product creation = Products + ProductUnits + ProductPrices only (3 tables, atomic via `ExecuteTransactionAsync`)
- Pricing is per `ProductUnit` × `CurrencyId` with effective date ranges
- Units are decoupled from products via the `ProductUnits` junction table

---

## Module 4: Accounting (المحاسبة)

### 4.1 AccountCategories
| Column | Type | Notes |
|--------|------|-------|
| `Id` | **smallint** PK | |
| `Name` | nvarchar(100) not null | |
| `Description` | nvarchar(300) null | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 4.2 Accounts
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `ParentId` | int null FK → Accounts(Id) | self-referencing hierarchy |
| `AccountCode` | nvarchar(20) not null | unique filtered `[IsActive]=1` |
| `NameAr` | nvarchar(200) not null | Arabic name |
| `NameEn` | nvarchar(200) null | English name for bilingual support |
| `Nature` | tinyint not null | 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense |
| `Level` | tinyint not null default 1 | 1=Group(1 digit), 2=Main(2 digits), 3=Sub(4 digits), 4=Detail(8 digits) — see hierarchical numbering below |
| `IsLeaf` | bit not null default 1 | leaf accounts allow transactions |
| `IsSystem` | bit not null default 0 | system accounts protected from modification |
| `Description` | nvarchar(500) null | Explanation of account purpose |
| `ColorCode` | nvarchar(7) null | Auto-generated hex color from Nature: #2B579A (Asset), #D32F2F (Liability), #6A1B9A (Equity), #2E7D32 (Revenue), #795548 (Expense) |
| `Notes` | nvarchar(300) null | Additional notes |
| `CategoryId` | smallint null FK → AccountCategories(Id) |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### Hierarchical Account Numbering Scheme

Account codes follow an expanding hierarchical pattern that encodes the account's level:

| Level | Digits | Pattern | Example |
|-------|--------|---------|---------|
| 1 (Group) | 1 | Single digit | `1` (Assets) |
| 2 (Main) | 2 | Level1 + 1-digit sequence | `11` (Current Assets) |
| 3 (Sub) | 4 | Level2 + 2-digit sequence | `1101` (Cash & Equivalents) |
| 4 (Detail) | 8 | Level3 + 4-digit sequence | `11010001` (Cash on Hand) |

This scheme allows up to 9,999 detail accounts per sub-category (e.g., 9,999 customer accounts under `1103` — Accounts Receivable).

**Auto-generation rule:** When creating a detail account under parent `1103`, the system queries `LIKE '1103%'` for the current max code, increments the suffix, and assigns the new code (e.g., `11030001` → `11030002`). Thread safety via `SemaphoreSlim`.

### 4.3 CashBoxes
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `AccountId` | int not null FK → Accounts(Id) | balance lives on Account, NOT CashBox |
| `Name` | nvarchar(150) not null | |
| `Description` | nvarchar(300) null | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **Notes** | NO OpeningBalance, NO CurrentBalance | balance tracked on linked Account |

### 4.4 Banks
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `AccountId` | int not null FK → Accounts(Id) | |
| `Name` | nvarchar(150) not null | |
| `AccountNumber` | nvarchar(100) null | |
| `IBAN` | nvarchar(100) null | |
| `IsActive` | bit not null default 1 | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |

### 4.5 JournalEntries
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `EntryNo` | int not null | unique per fiscal year |
| `EntryDate` | date not null | |
| `EntryType` | tinyint not null | 1=Manual, 2=Sales, 3=Purchase, 4=Receipt, 5=Payment, 6=Inventory, 7=Adjustment |
| `ReferenceType` | nvarchar(50) null | 'SalesInvoice', 'PurchaseInvoice', etc. |
| `ReferenceId` | int null | |
| `Description` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `IsReversed` | bit not null default 0 | |
| `ReversedByEntryId` | int null FK → JournalEntries(Id) | Restrict |
| `CreatedByUserId` | int null FK → Users(Id) | extracted from JWT, never client-supplied |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |
| **Indexes** | `EntryNo`, `(ReferenceType, ReferenceId)` | |

### 4.6 JournalEntryLines
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `JournalEntryId` | int not null FK → JournalEntries(Id) | |
| `AccountId` | int not null FK → Accounts(Id) | |
| `Debit` | decimal(18,2) not null default 0 | |
| `Credit` | decimal(18,2) not null default 0 | |
| `Description` | nvarchar(300) null | |
| `SortOrder` | smallint not null default 0 | |
| **CHK** | `CHK_DebitOrCredit` | exactly one of Debit/Credit > 0, or both zero |
| **CHK** | `CHK_NoNegativeValues` | Debit >= 0 AND Credit >= 0 |

### 4.7 ReceiptVouchers (سندات قبض)
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `VoucherNo` | int not null | unique |
| `VoucherDate` | date not null | |
| `CashBoxId` | int not null FK → CashBoxes(Id) | |
| `AccountId` | int not null FK → Accounts(Id) | |
| `TotalAmount` | decimal(18,2) not null | |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |

### 4.8 PaymentVouchers (سندات صرف)
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `VoucherNo` | int not null | unique |
| `VoucherDate` | date not null | |
| `CashBoxId` | int not null FK → CashBoxes(Id) | |
| `AccountId` | int not null FK → Accounts(Id) | |
| `TotalAmount` | decimal(18,2) not null | |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |

### 4.9 Expenses
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `ExpenseNo` | int not null | unique |
| `ExpenseDate` | date not null | |
| `ExpenseAccountId` | int not null FK → Accounts(Id) | |
| `CashBoxId` | int not null FK → CashBoxes(Id) | |
| `Amount` | decimal(18,2) not null | |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |

### 4.10 SystemAccountMappings (NEW: Accounting Defaults)
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `MappingKey` | nvarchar(100) not null | e.g., "SalesRevenue", "COGS" |
| `AccountId` | int not null FK → Accounts(Id) | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **Notes** | Flexible key-value design | replaces fixed-column approach |

**Design Notes:**
- **OpeningBalance** is NOT stored on the Account entity. When an account is created with an opening balance, the system generates an automatic Journal Entry (Debit/Credit based on Nature) against the `OpeningBalanceEquity` (1422) account within a database transaction. This ensures double-entry integrity.
- NO CashTransactions table (replaced by ReceiptVouchers/PaymentVouchers)
- NO DailyClosures table (removed from V1)
- NO Cheques table (removed from V1)
- SystemAccountMappings uses flexible key-value design

---

## Module 5: Inventory (المخزون)

### 5.1 WarehouseStocks
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `WarehouseId` | smallint not null FK → Warehouses(Id) | |
| `ProductId` | int not null FK → Products(Id) | |
| `Quantity` | decimal(18,3) not null default 0 | CHK `>= 0` |
| `CreatedAt` | datetime2 not null | |
| `CreatedByUserId` | int null FK | |
| `UpdatedAt` | datetime2 null | |
| `UpdatedByUserId` | int null FK | |
| **UK** | `UNIQUE(WarehouseId, ProductId)` | one stock row per product per warehouse |
| **Notes** | NO IsActive | live balance entity |

### 5.2 InventoryBatches
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `BatchNo` | nvarchar(50) not null | internal batch number |
| `ProductId` | int not null FK → Products(Id) | |
| `WarehouseId` | smallint not null FK → Warehouses(Id) | |
| `PurchaseInvoiceId` | int null FK → PurchaseInvoices(Id) | source purchase invoice |
| `PurchaseInvoiceLineId` | int null FK → PurchaseInvoiceLines(Id) | source purchase invoice line |
| `SupplierBatchNo` | nvarchar(100) null | supplier's batch reference |
| `ExpiryDate` | date null | for FEFO tracking |
| `QuantityReceived` | decimal(18,3) not null | CHK `>= 0` |
| `QuantityRemaining` | decimal(18,3) not null | CHK `>= 0` |
| `UnitCost` | decimal(18,2) not null | CHK `>= 0` |
| `IsClosed` | bit not null default 0 | CHK: 0 when QtyRemaining > 0, 1 when fully consumed |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **Indexes** | `(ProductId, WarehouseId)`, `(ExpiryDate)` filtered, `(PurchaseInvoiceId)` | |

### 5.3 InventoryTransactions
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `TransactionNo` | int not null | unique |
| `TransactionType` | tinyint not null | 1=Purchase, 2=PurchaseReturn, 3=Sale, 4=SaleReturn, 5=TransferOut, 6=TransferIn, 7=Count, 8=Adjustment, 9=Damage, 10=OpeningBalance, 11=InternalIssue, 12=InternalReceipt |
| `WarehouseId` | smallint not null FK → Warehouses(Id) | |
| `ReferenceType` | tinyint null | 1=PurchaseInvoice, 2=SalesInvoice, 3=PurchaseReturn, 4=SalesReturn, 5=Transfer, 6=Count, 7=Adjustment |
| `ReferenceId` | int null | FK to the reference document |
| `TransactionDate` | date not null | |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **Indexes** | `(WarehouseId, TransactionDate DESC)`, `(ReferenceType, ReferenceId)` | |

### 5.4 InventoryTransactionLines
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `InventoryTransactionId` | int not null FK → InventoryTransactions(Id) | |
| `ProductId` | int not null FK → Products(Id) | |
| `ProductUnitId` | int not null FK → ProductUnits(Id) | |
| `BatchId` | int null FK → InventoryBatches(Id) | null for non-batched products |
| `Quantity` | decimal(18,3) not null | |
| `UnitCost` | decimal(18,2) not null | |
| `TotalCost` | decimal(18,2) not null | UnitCost x Quantity |

### 5.5 InventoryCounts
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `CountNo` | int not null | unique |
| `WarehouseId` | smallint not null FK → Warehouses(Id) | |
| `CountDate` | date not null | |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |

### 5.6 InventoryCountLines
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `InventoryCountId` | int not null FK → InventoryCounts(Id) | |
| `ProductId` | int not null FK → Products(Id) | |
| `BatchId` | int not null FK → InventoryBatches(Id) | links count to specific batch |
| `SystemQuantity` | decimal(18,3) not null | expected quantity from system |
| `ActualQuantity` | decimal(18,3) not null | counted quantity |
| `DifferenceQuantity` | decimal(18,3) not null | ActualQuantity - SystemQuantity |

### 5.7 InventoryAdjustments
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `AdjustmentNo` | int not null | unique |
| `WarehouseId` | smallint not null FK → Warehouses(Id) | |
| `AdjustmentType` | tinyint not null | 1=Opening, 2=Increase, 3=Shortage, 4=Damage |
| `AdjustmentDate` | date not null | |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |

### 5.8 InventoryAdjustmentLines
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `InventoryAdjustmentId` | int not null FK → InventoryAdjustments(Id) | |
| `ProductId` | int not null FK → Products(Id) | |
| `BatchId` | int null FK → InventoryBatches(Id) | null for opening adjustments |
| `Quantity` | decimal(18,3) not null | |
| `UnitCost` | decimal(18,2) not null | |
| `TotalCost` | decimal(18,2) not null | UnitCost x Quantity |

### 5.9 WarehouseTransfers
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `TransferNo` | int not null | unique |
| `FromWarehouseId` | smallint not null FK → Warehouses(Id) | source warehouse |
| `ToWarehouseId` | smallint not null FK → Warehouses(Id) | destination warehouse |
| `TransferDate` | date not null | |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |

### 5.10 WarehouseTransferLines
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `WarehouseTransferId` | int not null FK → WarehouseTransfers(Id) | |
| `ProductId` | int not null FK → Products(Id) | |
| `BatchId` | int not null FK → InventoryBatches(Id) | specific batch being transferred |
| `Quantity` | decimal(18,3) not null | |
| `UnitCost` | decimal(18,2) not null | |
| `TotalCost` | decimal(18,2) not null | UnitCost x Quantity |

**Design Notes:**
- NO InventoryMovements table (replaced by InventoryTransactions/InventoryTransactionLines)
- NO InventoryOperations table (merged into InventoryTransactions)
- NO StockTransfers (renamed to WarehouseTransfers)
- NO StockWriteOffs (covered by InventoryAdjustments with Damage type)
- WarehouseStocks has NO IsActive (live balance)
- Transfers do NOT create accounting entries (only adjustments do)

---

## Module 6: Sales (المبيعات)

### 6.1 SalesInvoices
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `InvoiceNo` | int not null | user-facing number, UNIQUE per table |
| `InvoiceDate` | date not null | |
| `CustomerId` | int not null FK → Customers(Id) | |
| `WarehouseId` | smallint not null FK → Warehouses(Id) | |
| `PaymentType` | tinyint not null | 1=Cash, 2=Credit, 3=Mixed |
| `CashBoxId` | int null FK → CashBoxes(Id) | |
| `TaxId` | smallint null FK → Taxes(Id) | |
| `SubTotal` | decimal(18,2) not null | |
| `DiscountAmount` | decimal(18,2) not null default 0 | |
| `DiscountType` | tinyint not null default 0 | 0=Amount, 1=Percentage |
| `DiscountRate` | decimal(5,2) null | percentage rate when DiscountType=1 |
| `CostInBaseCurrency` | decimal(18,2) null | total cost in base currency for profit calculation |
| `TaxAmount` | decimal(18,2) not null default 0 | |
| `OtherCharges` | decimal(18,2) not null default 0 | |
| `NetTotal` | decimal(18,2) not null | SubTotal - Discount + Tax + OtherCharges |
| `PaidAmount` | decimal(18,2) not null default 0 | CHK 0 to NetTotal |
| `RemainingAmount` | decimal(18,2) not null default 0 | NetTotal - PaidAmount |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |
| **UK** | `UNIQUE(InvoiceNo)` | |

### 6.2 SalesInvoiceLines
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `SalesInvoiceId` | int not null FK → SalesInvoices(Id) | |
| `ProductId` | int not null FK → Products(Id) | |
| `ProductUnitId` | int not null FK → ProductUnits(Id) | |
| `Quantity` | decimal(18,3) not null | |
| `UnitPrice` | decimal(18,2) not null | |
| `DiscountType` | tinyint not null default 0 | 0=Amount, 1=Percentage per line |
| `DiscountRate` | decimal(5,2) null | percentage rate when line DiscountType=1 |
| `DiscountAmount` | decimal(18,2) not null default 0 | line-level discount |
| `UnitCost` | decimal(18,2) not null default 0 | average cost at time of sale |
| `CostInBaseCurrency` | decimal(18,2) null | cost converted to base currency |
| `ProfitAmount` | decimal(18,2) not null default 0 | LineTotal - (CostInBaseCurrency × Quantity) |
| `LineTotal` | decimal(18,2) not null | (Quantity × UnitPrice) - DiscountAmount |
| **Notes** | Line-level DiscountType/DiscountAmount supported. CostInBaseCurrency populated at posting from FIFO batches. | |

### 6.3 SalesReturns
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `ReturnNo` | int not null | unique |
| `ReturnDate` | date not null | |
| `SalesInvoiceId` | int not null FK → SalesInvoices(Id) | |
| `CustomerId` | int not null FK → Customers(Id) | |
| `WarehouseId` | smallint not null FK → Warehouses(Id) | |
| `TotalAmount` | decimal(18,2) not null | |
| `ReturnedDiscountAmount` | decimal(18,2) not null default 0 | proportional discount from original invoice |
| `ReturnedTaxAmount` | decimal(18,2) not null default 0 | proportional tax from original invoice |
| `ReturnedChargeAmount` | decimal(18,2) not null default 0 | proportional other charges from original invoice |
| `RefundAmount` | decimal(18,2) not null default 0 | amount refunded to customer |
| `ReturnReason` | nvarchar(500) null | optional reason for return |
| `TaxId` | smallint null FK → Taxes(Id) | tax rate used from original invoice |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |

### 6.4 SalesReturnLines
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `SalesReturnId` | int not null FK → SalesReturns(Id) | |
| `SalesInvoiceLineId` | int not null FK → SalesInvoiceLines(Id) | links to original line |
| `Quantity` | decimal(18,3) not null | |
| `Amount` | decimal(18,2) not null | |
| `CostInBaseCurrency` | decimal(18,2) null | cost of returned goods in base currency |

### 6.5 CustomerReceipts (سندات قبض)
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `ReceiptNo` | int not null | unique |
| `ReceiptDate` | date not null | |
| `CustomerId` | int not null FK → Customers(Id) | |
| `CashBoxId` | int not null FK → CashBoxes(Id) | |
| `PaymentMethod` | tinyint not null default 1 | 1=Cash, 2=Cheque, 3=BankTransfer, 4=CreditCard |
| `Amount` | decimal(18,2) not null | |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |

### 6.6 CustomerReceiptApplications
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `CustomerReceiptId` | int not null FK → CustomerReceipts(Id) | |
| `SalesInvoiceId` | int not null FK → SalesInvoices(Id) | |
| `AppliedAmount` | decimal(18,2) not null | |
| **Notes** | Optional — only created when user explicitly distributes payment to specific invoices | |

### 6.7 SalesQuotations
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `QuotationNo` | int not null | user-facing number, UNIQUE |
| `QuotationDate` | date not null | |
| `ValidUntil` | date null | optional expiry date |
| `CustomerId` | int not null FK → Customers(Id) | |
| `WarehouseId` | smallint not null FK → Warehouses(Id) | |
| `PaymentType` | tinyint not null | 1=Cash, 2=Credit |
| `SubTotal` | decimal(18,2) not null | SUM(LineTotal) |
| `DiscountAmount` | decimal(18,2) not null | header-level discount |
| `TaxAmount` | decimal(18,2) not null | |
| `TotalAmount` | decimal(18,2) not null | SubTotal - Discount + Tax |
| `Notes` | nvarchar(500) null | |
| `TermsAndConditions` | nvarchar(2000) null | |
| `Status` | tinyint not null | 1=Draft, 2=Sent, 3=Accepted, 4=Converted, 5=Rejected |
| `ConvertedToInvoiceId` | int null FK → SalesInvoices(Id) | set when converted to invoice |
| `RejectionReason` | nvarchar(1000) null | |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| **UK** | `UNIQUE(QuotationNo)` | |

### 6.8 SalesQuotationItems
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `SalesQuotationId` | int not null FK → SalesQuotations(Id) | |
| `ProductId` | int not null FK → Products(Id) | |
| `ProductUnitId` | int not null FK → ProductUnits(Id) | |
| `Quantity` | decimal(18,3) not null | |
| `UnitPrice` | decimal(18,2) not null | |
| `DiscountAmount` | decimal(18,2) not null default 0 | line-level discount |
| `LineTotal` | decimal(18,2) not null | (Qty × UnitPrice) − Discount |
| `Notes` | nvarchar(500) null | |

**Design Notes:**
- NO CustomerPayments table (replaced by CustomerReceipts)
- SalesQuotations ARE full V1 citizens — 5-state lifecycle, full stack (Domain/EF/Service/Controller/Desktop), no stock/accounting impact until converted
- SalesQuotationItems DO support per-line DiscountAmount (unlike SalesInvoiceLines which only have header discount)
- SalesReturnLines link to SalesInvoiceLineId (not ProductId directly)

**Multi-Currency Design Notes:**
- Journal entries (Debit/Credit) are ALWAYS recorded in base currency
- Exchange rate is FROZEN after document posting — cannot be changed

---

## Module 7: Purchases (المشتريات)

### 7.1 PurchaseInvoices
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `InvoiceNo` | int not null | user-facing number, UNIQUE per table |
| `InvoiceDate` | date not null | |
| `SupplierId` | int not null FK → Suppliers(Id) | |
| `WarehouseId` | smallint not null FK → Warehouses(Id) | |
| `PaymentType` | tinyint not null | 1=Cash, 2=Credit, 3=Mixed |
| `CashBoxId` | int null FK → CashBoxes(Id) | |
| `TaxId` | smallint null FK → Taxes(Id) | |
| `SubTotal` | decimal(18,2) not null | |
| `DiscountAmount` | decimal(18,2) not null default 0 | |
| `DiscountType` | tinyint not null default 0 | 0=Amount, 1=Percentage |
| `DiscountRate` | decimal(5,2) null | percentage rate when DiscountType=1 |
| `TaxAmount` | decimal(18,2) not null default 0 | |
| `OtherCharges` | decimal(18,2) not null default 0 | additional costs (transport, customs) |
| `NetTotal` | decimal(18,2) not null | SubTotal - Discount + Tax + OtherCharges |
| `PaidAmount` | decimal(18,2) not null default 0 | |
| `RemainingAmount` | decimal(18,2) not null default 0 | NetTotal - PaidAmount |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |
| **UK** | `UNIQUE(InvoiceNo)` | |

### 7.2 PurchaseInvoiceLines
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `PurchaseInvoiceId` | int not null FK → PurchaseInvoices(Id) | |
| `ProductId` | int not null FK → Products(Id) | |
| `ProductUnitId` | int not null FK → ProductUnits(Id) | |
| `Quantity` | decimal(18,3) not null | |
| `UnitPrice` | decimal(18,2) not null | purchase unit cost |
| `LineTotal` | decimal(18,2) not null | Quantity x UnitPrice |

### 7.3 PurchaseReturns
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `ReturnNo` | int not null | unique |
| `ReturnDate` | date not null | |
| `PurchaseInvoiceId` | int null FK → PurchaseInvoices(Id) | null for standalone returns (RULE-487) |
| `SupplierId` | int not null FK → Suppliers(Id) | |
| `WarehouseId` | smallint not null FK → Warehouses(Id) | |
| `TotalAmount` | decimal(18,2) not null | |
| `ReturnedDiscountAmount` | decimal(18,2) not null default 0 | proportional discount from original invoice |
| `ReturnedTaxAmount` | decimal(18,2) not null default 0 | proportional tax from original invoice |
| `ReturnedChargeAmount` | decimal(18,2) not null default 0 | proportional other charges from original invoice |
| `TaxId` | smallint null FK → Taxes(Id) | tax rate used from original invoice |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |

### 7.4 PurchaseReturnLines
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `PurchaseReturnId` | int not null FK → PurchaseReturns(Id) | |
| `PurchaseInvoiceLineId` | int null FK → PurchaseInvoiceLines(Id) | null for standalone returns |
| `Quantity` | decimal(18,3) not null | |
| `Amount` | decimal(18,2) not null | |

### 7.5 SupplierPayments
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `PaymentNo` | int not null | unique |
| `PaymentDate` | date not null | |
| `SupplierId` | int not null FK → Suppliers(Id) | |
| `CashBoxId` | int not null FK → CashBoxes(Id) | |
| `Amount` | decimal(18,2) not null | |
| `Notes` | nvarchar(500) null | |
| `Status` | tinyint not null | 1=Draft, 2=Posted, 3=Cancelled |
| `CreatedByUserId` | int null FK | |
| `UpdatedByUserId` | int null FK | |
| `CreatedAt` | datetime2 not null | |
| `UpdatedAt` | datetime2 null | |
| `PostedAt` | datetime2 null | set when Status=2 |
| `CancelledAt` | datetime2 null | set when Status=3 |

### 7.6 SupplierPaymentApplications
| Column | Type | Notes |
|--------|------|-------|
| `Id` | int PK | |
| `SupplierPaymentId` | int not null FK → SupplierPayments(Id) | |
| `PurchaseInvoiceId` | int not null FK → PurchaseInvoices(Id) | |
| `AppliedAmount` | decimal(18,2) not null | |
| **Notes** | Optional — only created when user explicitly distributes payment to specific invoices | |

**Design Notes:**
- NO PurchaseOrders table (removed from V1)
- NO AdditionalFees/AdditionalFeeAllocations (OtherCharges on invoice handles this)
- NO per-line discount on purchase lines (discounts at invoice header level only)
- PurchaseReturnLines link to PurchaseInvoiceLineId
- On posting: creates InventoryBatch with BatchNo, QuantityReceived, QuantityRemaining, UnitCost

---

## Module 8: Infrastructure & Support (البنية التحتية والدعم)

### 8.1 AuditLogs
| Column | Type | Notes |
|--------|------|-------|
| `Id` | **bigint** PK | high-volume logging |
| `UserId` | int null FK → Users(Id) | performing user |
| `Action` | nvarchar(100) not null | e.g., "CreateCustomer", "CancelInvoice" |
| `EntityName` | nvarchar(100) null | e.g., "SalesInvoice", "Product" |
| `EntityId` | nvarchar(50) null | entity identifier |
| `Details` | nvarchar(max) null | JSON payload |
| `IpAddress` | nvarchar(50) null | client IP |
| `CreatedAt` | datetime2 not null | |
| **Indexes** | `(UserId, CreatedAt DESC)`, `(EntityName, EntityId)`, `(CreatedAt DESC)` | |

### 8.2 SystemLogs
| Column | Type | Notes |
|--------|------|-------|
| `Id` | **bigint** PK | high-volume logging |
| `Level` | tinyint not null | 1=Info, 2=Warning, 3=Error, 4=Critical |
| `Source` | nvarchar(100) null | component name |
| `Message` | nvarchar(max) not null | |
| `Exception` | nvarchar(max) null | serialized exception |
| `CreatedAt` | datetime2 not null | |
| **Index** | `(Level, CreatedAt DESC)` | for error monitoring |

---

# 4) Enums Reference

| Enum | Values |
|------|--------|
| `InvoiceStatus` | Draft=1, Posted=2, Cancelled=3 |
| `PaymentType` | Cash=1, Credit=2, Mixed=3 |
| `AccountNature` | Asset=1, Liability=2, Equity=3, Revenue=4, Expense=5 |
| `TaxType` | Standard=1, ZeroRated=2, Exempt=3 |
| `JournalEntryType` | Manual=1, Sales=2, Purchase=3, Receipt=4, Payment=5, Inventory=6, Adjustment=7 |
| `InventoryTransactionType` | Purchase=1, PurchaseReturn=2, Sale=3, SaleReturn=4, TransferOut=5, TransferIn=6, Count=7, Adjustment=8, Damage=9, OpeningBalance=10, InternalIssue=11, InternalReceipt=12 |
| `InventoryReferenceType` | PurchaseInvoice=1, SalesInvoice=2, PurchaseReturn=3, SalesReturn=4, Transfer=5, Count=6, Adjustment=7 |
| `AdjustmentType` | Opening=1, Increase=2, Shortage=3, Damage=4 |
| `DiscountType` | Amount=0, Percentage=1 |
| `PaymentMethod` | Cash=1, Cheque=2, BankTransfer=3, CreditCard=4 |
| `SettingType` | String=1, Integer=2, Decimal=3, Boolean=4 |
| `NotificationType` | LowStock=1, ExpirySoon=2, CreditLimitExceeded=3, System=4, Reminder=5 |
| `LogLevel` | Info=1, Warning=2, Error=3, Critical=4 |

---

# 5) Table Count Summary

| Module | Table Count | Tables |
|--------|------------|--------|
| 1. Core & Security | 11 | Customers, CustomerContacts, Suppliers, SupplierContacts, Roles, Users, UserRoles, Permissions, RolePermissions, UserSessions, UserPermissions |
| 2. Organization & Settings | 8 | Warehouses, Taxes, CompanySettings, SystemSettings, DocumentSequences, FiscalYears, Notifications, Attachments |
| 3. Products | 5 | ProductCategories, Products, Units, ProductUnits, ProductPrices |
| 4. Accounting | 10 | AccountCategories, Accounts, CashBoxes, Banks, JournalEntries, JournalEntryLines, ReceiptVouchers, PaymentVouchers, Expenses, SystemAccountMappings |
| 5. Inventory | 10 | WarehouseStocks, InventoryBatches, InventoryTransactions, InventoryTransactionLines, InventoryCounts, InventoryCountLines, InventoryAdjustments, InventoryAdjustmentLines, WarehouseTransfers, WarehouseTransferLines |
| 6. Sales | 8 | SalesInvoices, SalesInvoiceLines, SalesReturns, SalesReturnLines, CustomerReceipts, CustomerReceiptApplications, SalesQuotations, SalesQuotationItems |
| 7. Purchases | 6 | PurchaseInvoices, PurchaseInvoiceLines, PurchaseReturns, PurchaseReturnLines, SupplierPayments, SupplierPaymentApplications |
| 8. Infrastructure & Support | 2 | AuditLogs, SystemLogs |
| **Total** | **66** | |

---

# 6) Key Financial Formulas

## Sales Invoice
```
LineTotal = (Quantity × UnitPrice) - DiscountAmount
SubTotal  = SUM(LineTotal)
NetTotal  = SubTotal - DiscountAmount + TaxAmount + OtherCharges
RemainingAmount = NetTotal - PaidAmount
Constraint: PaidAmount <= NetTotal
```

## Purchase Invoice
```
LineTotal = Quantity x UnitPrice
SubTotal  = SUM(LineTotal)
NetTotal  = SubTotal - DiscountAmount + TaxAmount + OtherCharges
RemainingAmount = NetTotal - PaidAmount
Constraint: PaidAmount <= NetTotal
```

## FIFO Costing (Default Method)
```
-On Sale: consume from earliest batch first (oldest QuantityRemaining > 0)
-On Purchase Return: debit from the original batch's QuantityRemaining
-On Expiry/FEFO: consume nearest expiry date first (when TrackExpiry = true)
```

## Journal Entry Balance
```
Per Line: (Debit > 0 AND Credit = 0) OR (Credit > 0 AND Debit = 0) OR (Debit = 0 AND Credit = 0)
Per Entry: SUM(Debit) = SUM(Credit)
```

---

# 7) Soft Delete Strategy

- **Soft delete**: `IsActive = false` on all ActivatableEntity inheritors
- **Filter**: Global EF Core query filter `.HasQueryFilter(x => x.IsActive)`
- **Live balances**: WarehouseStocks has NO IsActive (auditable entity only)
- **Invoices**: `Status = Cancelled` — NEVER hard delete invoiced transactions
- **Users**: NEVER hard-delete — soft delete via `IsActive = false`
- **Document entities**: use Status (Draft→Posted→Cancelled), not IsActive

---

# 8) FK Rules

- **ALL foreign keys**: `DeleteBehavior.Restrict` — NO cascade delete anywhere
- **Self-referencing FKs**: Accounts.ParentId
- **Soft-delete reference safety**: Filtered unique indexes include `AND [IsActive] = 1`



