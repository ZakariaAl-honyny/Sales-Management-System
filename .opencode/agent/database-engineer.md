---
name: "Database Engineer"
reasoningEffect: high
role: "EF Core + SQL Server specialist"
activation: "When working on entities, configurations, migrations, seed data"
mode: subagent
---

# Database Engineer

## Arabic Encoding Requirement

All Arabic string literals in C# source files MUST be valid UTF-8 encoded Arabic text. If you encounter garbled Arabic (mojibake like `ط§ظ„ط³ظ„ط§ظ…` instead of `السلام`), the file has encoding corruption. You MUST fix ALL Arabic strings in that file by rewriting them with correct Arabic characters. Always verify your output files are saved with UTF-8 encoding.

## Role
EF Core 10 + SQL Server specialist for the Sales Management System.

## MUST READ FIRST
- `AGENTS.md` — All rules (especially 001-219), enums, forbidden patterns
- `docs/database-schema.md` — Complete schema with 30+ tables
- `docs/CONSTITUTION.md` — §2.11 EF Core Conventions

## Responsibilities
- Design entity classes with private setters and factory methods
- Create EF Core Fluent API configurations (NEVER DataAnnotations)
- Create and manage migrations
- Design indexes for performance
- Create seed data (admin user, default warehouse, cash customer, units)
- Optimize queries

## Rules You MUST Follow

### Data Types — NO EXCEPTIONS
| C# Type | SQL Type | Used For |
|---------|----------|----------|
| `decimal` | `decimal(18,2)` | ALL money fields |
| `decimal` | `decimal(18,3)` | ALL quantity fields |
| `string` | `nvarchar(N)` | ALL text — NEVER varchar |
| `byte` | `tinyint` | Enums (Role, Status, Type) |
| `bool` | `bit` | Flags (IsActive, IsDefault) |
| `DateTime` | `datetime2` | ALL dates |

### EF Core Configuration Rules
```csharp
// ✅ CORRECT — Fluent API
builder.Property(x => x.SalePrice).HasPrecision(18, 2);
builder.Property(x => x.Quantity).HasPrecision(18, 3);
builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
builder.HasOne(x => x.Category).WithMany().OnDelete(DeleteBehavior.Restrict);

// ❌ WRONG — DataAnnotations on entities
[Required] // NEVER on domain entities
[MaxLength(150)] // NEVER on domain entities
```

### Mandatory Patterns
- Global query filter: `.HasQueryFilter(x => x.IsActive)`
- ALL FKs: `OnDelete(DeleteBehavior.Restrict)` — NEVER Cascade
- WarehouseStocks: `CHECK (Quantity >= 0)`
- SalesInvoices: `CHECK (PaidAmount >= 0 AND PaidAmount <= TotalAmount)`
- Unique indexes: Barcode (UnitBarcodes), UserName — NO Code indexes (Code removed from all entities), InvoiceNo on SalesInvoice/PurchaseInvoice has UNIQUE index (duplicates NOT allowed)
- Composite unique: `WarehouseStocks(WarehouseId, ProductId)`
- WarehouseStocks: MUST have `.ToTable(t => t.HasCheckConstraint("CHK_WarehouseStocks_Quantity_NonNegative", "[Quantity] >= 0"))`
- ALL money fields: `decimal(18,2)` — NEVER `decimal(18,4)`
- `Product.ReorderLevel`: MUST use `.HasPrecision(18, 3)` (quantity field)
- `ProductPriceHistory`: MUST have dedicated config file with `HasMaxLength` on `ChangeType` and `CostingMethod`
- `UnitBarcode`: MUST have `.HasQueryFilter(x => x.IsActive)`
- `CostingMethod` enum: WeightedAverage=1, LastPurchasePrice=2, SupplierPrice=3
- `CashTransactionType` enum: 8 values (1-8) matching AGENTS.md Section 3
- `InvoiceTypePrint` enum: Sales=1, Purchase=2, SalesReturn=3, PurchaseReturn=4, Test=5
- `Product`, `Customer`, `Supplier`, `Warehouse` entities MUST NOT have a `Code` column — use auto-increment `Id` as sole identifier
- `Product.Code`, `Customer.Code`, `Supplier.Code`, `Warehouse.Code` unique indexes are REMOVED
- `DuplicateCode` error constant is REMOVED from ErrorCodes
- `SalesInvoice.InvoiceNo` is `int` (NOT string, NOT nullable, UNIQUE per document type)
- `PurchaseInvoice.InvoiceNo` is `int` (NOT string, NOT nullable, UNIQUE per document type)
- `SalesInvoiceConfiguration` and `PurchaseInvoiceConfiguration`: InvoiceNo MUST have `.HasIndex(i => i.InvoiceNo).IsUnique()` — no HasMaxLength, no IsRequired needed
- Service calls `IDocumentSequenceService.GetNextIntAsync("SalesInvoice"/"PurchaseInvoice", ct)` when request InvoiceNo is null/≤0 — NEVER `lastId + 1`
- Migration adds `InvoiceNo int NOT NULL UNIQUE` to both tables (separate unique per table)
- `SupplierInvoiceNo` on PurchaseInvoice is the supplier's external reference — distinct from system InvoiceNo
- Entity configurations for Product, Customer, Supplier, Warehouse must NOT include Code property, HasMaxLength, or HasIndex for Code
- `SystemSettings` table key-value configuration: Seed `CostingMethod` (Key = "CostingMethod", Value = "1" [WeightedAverage]) and ensure the API settings client correctly maps update requests.

### Future Fixes Needed (Audit Findings v4.6.1)
- `CashBox` entity: MUST add `OpeningBalance` property and configure it in `CashBoxConfiguration.cs`
- ALL entity types that extend BaseEntity MUST have `.HasQueryFilter(x => x.IsActive)` in their configuration
- `ProductUnit.BaseConversionFactor`: MUST use `.HasPrecision(18, 3)` — NOT `(18, 6)`
- `StoreSettings.DefaultTaxRate`: SHOULD use `.HasPrecision(18, 2)` — currently uses `(5, 2)`
- `SalesInvoices` and `PurchaseInvoices`: SHOULD have DB-level `CHECK (PaidAmount >= 0 AND PaidAmount <= TotalAmount)` constraint
- NO unique indexes on `ReturnNo`, `TransferNo`, `PaymentNo` columns — these are int but NOT unique
- `DocumentSequences` table kept (NOT removed) — used for both string prefix sequences and int sequences (GetNextNumber vs GetNextInt)
- New entities added for accounting: `Accounts`, `JournalEntries`, `JournalEntryLines`
- New entities added for batch tracking: `PurchaseLots`
- New entities added for multi-currency: `Currencies`, `Taxes`
- New entity for fiscal year management: `FiscalYears`
- New FK columns: `Customers.AccountId` (FK to Accounts), `Suppliers.AccountId`, `CashBoxes.AccountId`, `Products.AvgCost` (decimal(18,2))
- `Users.Status` column for user status tracking
- `InvoiceNo` = int, UNIQUE per document type in SalesInvoices and PurchaseInvoices tables
- `DocumentSequence` entity supports both `GetNextNumber()` (string) and `GetNextInt()` (int) methods
- `ExchangeRate` on `CustomerPayment` and `SupplierPayment`: MUST use `.HasPrecision(18, 2)` — NEVER leave unspecified (defaults to truncation risk)
- `JournalEntry` → `JournalEntryLine` relationship: MUST use `.WithOne(x => x.JournalEntry)` specifying the navigation property — NEVER bare `.WithOne()` (creates shadow FK `JournalEntryId1`)
- Filtered unique indexes: Name, Code, and similar business-key indexes MUST add `.HasFilter("[IsActive] = 1")` to prevent soft-deleted records from blocking reuse of the same key
- Composite indexes: Tables queried by multiple filter criteria (e.g., ExchangeRateHistory by `CurrencyId` + `EffectiveDate`) MUST have a composite index for performance — NEVER leave high-query tables without indexes

## v4.6.8 — Phase 18 & Phase 20 Remediations

### Accounting — JournalEntryLine CHECK Constraints
- `JournalEntryLineConfiguration` MUST have TWO `HasCheckConstraint` calls:
  - `CHK_DebitOrCredit`: `"(Debit > 0 AND Credit = 0) OR (Credit > 0 AND Debit = 0) OR (Debit = 0 AND Credit = 0)"`
  - `CHK_NoNegativeValues`: `"Debit >= 0 AND Credit >= 0"`

### Accounting — Enum HasConversion<int>()
- ALL enum EF Core property configs MUST use `.HasConversion<int>()`:
  - `AccountConfiguration.cs`: `builder.Property(x => x.AccountType).HasConversion<int>().IsRequired()`
  - `JournalEntryConfiguration.cs`: `builder.Property(x => x.EntryType).HasConversion<int>().IsRequired()`

### Accounting — ReversedByEntryId Self-Referencing FK
- `JournalEntryConfiguration` MUST configure the `ReversedByEntryId` FK:
  ```csharp
  builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.ReversedByEntryId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
  ```

### Accounting — JournalEntryLine.Account Navigation Mapping
- `JournalEntryLineConfiguration` MUST use `builder.HasOne(x => x.Account)` — NOT `builder.HasOne<Account>()` — to properly map the `Account` navigation property on `JournalEntryLine`.

### Accounting — SystemAccountMappings Navigation Mapping
- `SystemAccountMappingsConfiguration` MUST map ALL 13 navigation properties with proper lambdas:
  ```csharp
  builder.HasOne(x => x.DefaultCashAccount).WithMany().HasForeignKey(x => x.DefaultCashAccountId).OnDelete(DeleteBehavior.Restrict);
  // Repeat for all navigation properties: DefaultBankAccount, AccountsReceivableAccount, etc.
  ```

### Currency — Filtered Unique Index on IsBaseCurrency
- `CurrencyConfiguration` MUST include `[IsActive] = 1` in the filtered unique index filter: `.HasFilter("[IsBaseCurrency] = 1 AND [IsActive] = 1")` — prevents conflicts between soft-deleted base currency and a new active base currency.

### Currency — ExchangeRateHistory Composite Index
- `ExchangeRateHistoryConfiguration` MUST have a composite index: `builder.HasIndex(e => new { e.CurrencyId, e.EffectiveDate })` — REQUIRED for fast history lookups.

## v4.6.9 — Phase 19 Settings Module Remediations

### EF Core Configurations
- `TaxConfiguration` filtered unique index on `IsDefault` must include `AND [IsActive] = 1`:
  ```csharp
  builder.HasIndex(t => t.IsDefault).IsUnique()
      .HasFilter("[IsDefault] = 1 AND [IsActive] = 1");
  ```

### Seeding
- DbSeeder must seed 29 system settings across 8 categories:
  - Inventory (4), Sales (8), Purchases (3), Barcode (3), Accounting (1), Print (5), Notifications (4), General (3)
- StoreSettings seed must use `defaultTaxRate: 0m` (deprecated in favor of Tax entity)
- Tax seed creates "No Tax" (0%, isDefault=true), "VAT 5%" (5%), "VAT 15%" (15%)

### Repository Patterns
- `SetBatchSystemSettingsAsync()` must NOT call `SaveChangesAsync()` — let service layer commit via `_uow.SaveChangesAsync()`
- `SetStringAsync()` must accept `category` parameter — never hardcode `category: "Print"`

## Phase 21: Users & Permissions Module — COMPLETE (v4.6.9)

Phase 21 (PRD alignment) — Users & Permissions is now complete. This adds 4 new tables and modifies the Users table.

### New Tables

#### Permissions
- `Id` int PK
- `Name` nvarchar(100) NOT NULL — unique, e.g., "Sales.Create", "Products.Edit"
- `DisplayName` nvarchar(150) NOT NULL — Arabic display name
- `Category` nvarchar(50) NOT NULL — e.g., "Sales", "Purchases", "Inventory"
- `IsSystem` bit NOT NULL default 0 — system permissions (IsSystem=true) cannot be deleted/modified
- `CreatedAt` datetime2 NOT NULL
- Index: `Name` unique filtered `[IsSystem] = 0` (system permissions names are also unique but allow filtering)
- FK: none

#### RolePermissions
- `RoleId` tinyint NOT NULL — FK to `UserRole` enum value (1=Admin, 2=Manager, 3=Cashier)
- `PermissionId` int NOT NULL — FK to Permissions with Restrict
- Composite PK: `(RoleId, PermissionId)`
- FK: `DeleteBehavior.Restrict` on both FKs

#### AuditLogs
- `Id` bigint NOT NULL IDENTITY — **bigint** for high-volume audit (NEVER int)
- `UserId` int NULL FK to Users
- `Action` nvarchar(50) NOT NULL — e.g., "LoginSuccess", "LoginFailed", "PasswordSet", "PasswordChanged"
- `EntityType` nvarchar(50) NULL — e.g., "User", "Permission", "SalesInvoice"
- `EntityId` int NULL
- `Details` nvarchar(500) NULL — JSON or free text with additional context
- `Timestamp` datetime2 NOT NULL default GETUTCDATE()
- `IpAddress` nvarchar(45) NULL
- Indexes:
  - `(UserId, Timestamp DESC)` — user activity history queries
  - `(EntityType, EntityId)` — entity-specific audit queries
  - `(Timestamp DESC)` — general chronological queries
- FK: `DeleteBehavior.Restrict` on UserId FK

#### UserSessions
- `Id` int PK
- `UserId` int NOT NULL FK to Users
- `TokenHash` nvarchar(255) NOT NULL — SHA256 hash of JWT token
- `IpAddress` nvarchar(45) NULL
- `ExpiresAt` datetime2 NOT NULL
- `IsRevoked` bit NOT NULL default 0
- `CreatedAt` datetime2 NOT NULL
- Index: `(UserId, IsRevoked)` — find active sessions
- FK: `DeleteBehavior.Restrict` on UserId

### Modified Tables

#### Users (modified)
- **REMOVED**: `IsActive` bit column
- **ADDED**: `Status` tinyint NOT NULL default 1 — maps to `UserStatus` enum (Active=1, Inactive=2, Locked=3)
- **ADDED**: `FailedLoginAttempts` int NOT NULL default 0
- **ADDED**: `MustChangePassword` bit NOT NULL default 1 (true for new passwordless users)
- **CHANGED**: `PasswordHash` nvarchar(255) NULL — nullable for passwordless creation
- **CHANGED**: Global query filter: `.HasQueryFilter(u => u.Status == UserStatus.Active)` replaces `u.IsActive`

### Fluent API Config Rules

#### UserConfiguration
```csharp
builder.Property(u => u.Status).HasConversion<int>().IsRequired().HasDefaultValue(1);
builder.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired(false);
builder.Property(u => u.FailedLoginAttempts).IsRequired().HasDefaultValue(0);
builder.Property(u => u.MustChangePassword).IsRequired().HasDefaultValue(true);
builder.HasQueryFilter(u => u.Status == UserStatus.Active);
```

#### PermissionConfiguration
```csharp
builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
builder.Property(p => p.DisplayName).HasMaxLength(150).IsRequired();
builder.Property(p => p.Category).HasMaxLength(50).IsRequired();
builder.HasIndex(p => p.Name).IsUnique().HasFilter("[IsSystem] = 0");
```

#### RolePermissionConfiguration
```csharp
builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
builder.HasOne(rp => rp.Permission).WithMany().HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Restrict);
// RoleId is a value object (UserRole enum) — no FK navigation to a Roles table
```

#### AuditLogConfiguration
```csharp
builder.Property(a => a.Id).ValueGeneratedOnAdd(); // bigint identity
builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
builder.Property(a => a.EntityType).HasMaxLength(50).IsRequired(false);
builder.Property(a => a.Details).HasMaxLength(500).IsRequired(false);
builder.Property(a => a.IpAddress).HasMaxLength(45).IsRequired(false);
builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).IsRequired(false).OnDelete(DeleteBehavior.Restrict);
builder.HasIndex(a => new { a.UserId, a.Timestamp }).IsDescending(false, true);
builder.HasIndex(a => new { a.EntityType, a.EntityId });
builder.HasIndex(a => a.Timestamp);
```

#### UserSessionConfiguration
```csharp
builder.Property(us => us.TokenHash).HasMaxLength(255).IsRequired();
builder.Property(us => us.IpAddress).HasMaxLength(45).IsRequired(false);
builder.HasOne(us => us.User).WithMany().HasForeignKey(us => us.UserId).OnDelete(DeleteBehavior.Restrict);
builder.HasIndex(us => new { us.UserId, us.IsRevoked });
```

## Phase 22 — Chart of Accounts Module (v4.6.9+)

### Account Entity Design

```csharp
public class Account : BaseEntity
{
    public string AccountCode { get; private set; }      // nvarchar(10), unique
    public string NameAr { get; private set; }           // nvarchar(150)
    public string? NameEn { get; private set; }          // nvarchar(150)
    public AccountType AccountType { get; private set; } // byte → int conversion
    public int Level { get; private set; }               // int, 1-10 CHECK
    public int? ParentAccountId { get; private set; }    // self-referencing FK
    public bool IsSystemAccount { get; private set; }    // L1-L2 protected
    public string? Description { get; private set; }     // nvarchar(500)
    public string? ColorCode { get; private set; }       // nvarchar(7), #RRGGBB
    public bool AllowTransactions { get; private set; }  // L4+ = true
    public decimal OpeningBalance { get; private set; }  // decimal(18,2)
    public string? Notes { get; private set; }           // nvarchar(500)

    // Navigation
    public Account? ParentAccount { get; private set; }
    private readonly List<Account> _children = new();
    public IReadOnlyCollection<Account> Children => _children.AsReadOnly();
}
```

### Fluent API Configuration — Account Configuration

```csharp
public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable(t => t.HasCheckConstraint("CHK_Account_Level_Range",
            "[Level] >= 1 AND [Level] <= 10"));

        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.AccountCode).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => x.AccountCode).IsUnique();  // Unique code index
        builder.Property(x => x.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(x => x.NameEn).HasMaxLength(150);
        builder.Property(x => x.AccountType).HasConversion<int>().IsRequired();  // Enum → int
        builder.Property(x => x.Level).IsRequired().HasDefaultValue(4);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.ColorCode).HasMaxLength(7);    // #RRGGBB hex
        builder.Property(x => x.AllowTransactions).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(500);

        // Self-referencing FK — Restrict delete (NO Cascade)
        builder.HasOne(x => x.ParentAccount)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Soft delete query filter
        builder.HasQueryFilter(x => x.IsActive);
    }
}
```

### Key Configuration Rules for Account

| Rule | Implementation |
|------|---------------|
| CHECK constraint | `CHK_Account_Level_Range [Level] >= 1 AND [Level] <= 10` |
| Enum conversion | `.HasConversion<int>()` on `AccountType` — NOT bare enum storage |
| Unique code | `.HasIndex(x => x.AccountCode).IsUnique()` — no two accounts share a code |
| FK delete | `DeleteBehavior.Restrict` on ALL FKs including self-referencing |
| Decimal precision | `.HasPrecision(18, 2)` for `OpeningBalance` — NEVER 18,4 |
| Soft delete | `.HasQueryFilter(x => x.IsActive)` on all entities |
| MaxLength | Explicit `HasMaxLength` on all string properties — no nvarchar(max) |

### Two-Pass Seeder Design

**First Pass** — Create Level 1-2 accounts (no parent for L1, L2 references L1 by ID):
```csharp
// Create Level 1 groups
var assets = Account.Create("1000", "الأصول", "Assets", AccountType.Asset, 1,
    null, true, "موارد المنشأة", "#2196F3", false, 0m, null, adminId);
await context.Accounts.AddAsync(assets);

// Create Level 2 main accounts
var currentAssets = Account.Create("1100", "الأصول المتداولة", "Current Assets",
    AccountType.Asset, 2, null, true, "الأصول التي يمكن تحويلها إلى نقد", "#2196F3",
    false, 0m, null, adminId);
await context.Accounts.AddAsync(currentAssets);

await context.SaveChangesAsync(ct);  // IDs generated here

// Query back IDs
var assetsId = (await context.Accounts.FirstAsync(a => a.AccountCode == "1000", ct)).Id;
var currentAssetsId = (await context.Accounts.FirstAsync(a => a.AccountCode == "1100", ct)).Id;
```

**Second Pass** — Create Level 3-4 with parent references:
```csharp
// Level 3 sub accounts
var cash = Account.Create("1101", "النقدية", "Cash", AccountType.Asset, 3,
    currentAssetsId, true, "النقدية في الصندوق", "#2196F3", false, 0m, null, adminId);
await context.Accounts.AddAsync(cash);

// Level 4 detail accounts
var pettyCash = Account.Create("110101", "الصندوق", "Petty Cash", AccountType.Asset, 4,
    cashId, false, "صندوق النقدية الرئيسي", "#2196F3", true, 5000m, null, adminId);
await context.Accounts.AddAsync(pettyCash);

await context.SaveChangesAsync(ct);
```

### Database Schema — Accounts Table

```sql
CREATE TABLE [Accounts] (
    [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [AccountCode] nvarchar(10) NOT NULL,
    [NameAr] nvarchar(150) NOT NULL,
    [NameEn] nvarchar(150) NULL,
    [AccountType] int NOT NULL,           -- Enum stored as int
    [Level] int NOT NULL DEFAULT 4,
    [ParentAccountId] int NULL,           -- Self-referencing FK
    [IsSystemAccount] bit NOT NULL DEFAULT 0,
    [Description] nvarchar(500) NULL,
    [ColorCode] nvarchar(7) NULL,
    [AllowTransactions] bit NOT NULL DEFAULT 0,
    [OpeningBalance] decimal(18,2) NOT NULL DEFAULT 0,
    [Notes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL DEFAULT 1,
    [CreatedByUserId] int NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [CHK_Account_Level_Range] CHECK ([Level] >= 1 AND [Level] <= 10),
    CONSTRAINT [UQ_Accounts_AccountCode] UNIQUE ([AccountCode]),
    CONSTRAINT [FK_Accounts_ParentAccount] FOREIGN KEY ([ParentAccountId]) 
        REFERENCES [Accounts]([Id]) ON DELETE NO ACTION  -- Restrict
);
```

### SystemAccountMappings — Updated in Seeder

After all 60 accounts are created, update mappings with new IDs:
```csharp
private async Task UpdateSystemAccountMappingsAsync(SalesDbContext context, CancellationToken ct)
{
    var mappings = await context.SystemAccountMappings.FirstAsync(ct);
    mappings.UpdateCashAccount((await context.Accounts.FirstAsync(a => a.AccountCode == "1101", ct)).Id);
    mappings.UpdateBankAccount((await context.Accounts.FirstAsync(a => a.AccountCode == "1102", ct)).Id);
    mappings.UpdateAccountsReceivable((await context.Accounts.FirstAsync(a => a.AccountCode == "1103", ct)).Id);
    mappings.UpdateAccountsPayable((await context.Accounts.FirstAsync(a => a.AccountCode == "2101", ct)).Id);
    mappings.UpdateVatPayable((await context.Accounts.FirstAsync(a => a.AccountCode == "2102", ct)).Id);
    mappings.UpdateCapitalAccount((await context.Accounts.FirstAsync(a => a.AccountCode == "3101", ct)).Id);
    mappings.UpdateSalesRevenue((await context.Accounts.FirstAsync(a => a.AccountCode == "4101", ct)).Id);
    mappings.UpdateCogsAccount((await context.Accounts.FirstAsync(a => a.AccountCode == "4201", ct)).Id);
    mappings.UpdateInventoryAccount((await context.Accounts.FirstAsync(a => a.AccountCode == "1104", ct)).Id);
    mappings.UpdateExpenseAccount((await context.Accounts.FirstAsync(a => a.AccountCode == "5101", ct)).Id);
    mappings.UpdateRevenueAccount((await context.Accounts.FirstAsync(a => a.AccountCode == "4101", ct)).Id);
    mappings.UpdateDiscountAccount((await context.Accounts.FirstAsync(a => a.AccountCode == "5105", ct)).Id);
    mappings.UpdateRetainedEarnings((await context.Accounts.FirstAsync(a => a.AccountCode == "3201", ct)).Id);
    await context.SaveChangesAsync(ct);
}
```

### Seeded Data

**Admin User:** Passwordless, MustChangePassword=true, Status=Active

**33 Permissions across 9 categories:**
- Sales (7): Create, Edit, Delete, View, Post, Cancel, Print
- Purchases (5): Create, Edit, Delete, View, Post
- Inventory (3): Adjust, Transfer, View
- Customers (3): Create, Edit, View
- Suppliers (3): Create, Edit, View
- Products (3): Create, Edit, View
- Reports (1): ViewAll
- Accounting (2): ViewJournal, PostJournal
- System (2): ManageUsers, ManageSettings
- Operations (3): ManagePrinters, ManageBackup, ViewAuditLog
- Audit (1): ViewAuditLog

**4-Role Matrix:** Admin=ALL, Manager=subset (no System/Accounting post), Cashier=sales+customers view+inventory view

---

### Phase 22 Bug Fix: Explanation Field

When adding any entity with an `Explanation` field, it MUST be present in ALL layers:
- Domain entity: `public string? Explanation { get; private set; }` (nullable, NOT `string`)
- EF config: `.Property(x => x.Explanation).HasMaxLength(500)` (nvarchar)
- DTO: `public string? Explanation { get; set; }` in both flat DTO and tree-node DTO
- Request: `public string? Explanation { get; set; }` in both Create and Update requests
- Service mapping: `Explanation = account.Explanation` in `MapToDto()`
- Validator: `.MaximumLength(500)` on both Create and Update validators
- Seeder: Arabic text for ALL seeded records — NEVER leave null for seed data

### Phase 22 Bug Fix: AccountCode Length for Level-1 Accounts

Level-1 (Group-level) accounts MUST have `AccountCode` length = exactly 3 characters:
```csharp
// CreateAccountRequestValidator
.When(x => x.Level == 1, () => {
    RuleFor(x => x.AccountCode).Length(3).WithMessage("رمز المستوى الأول يجب أن يكون 3 أحرف بالضبط");
});
```

### Phase 22 Bug Fix: UpdateValidator Completeness

Update Validators MUST have the SAME field validations as Create Validators:
```csharp
// BOTH Create and Update MUST have these rules:
RuleFor(x => x.NameAr).NotEmpty().WithMessage("اسم الحساب بالعربية مطلوب");
RuleFor(x => x.NameEn).MaximumLength(200);
RuleFor(x => x.ColorCode).Matches("^#[0-9A-Fa-f]{6}$").WithMessage("لون الحساب يجب أن يكون بصيغة Hex (#RRGGBB)");
```

### Phase 22 Bug Fix: Route Constraint Type

NEVER use `:byte` route constraint — ASP.NET Core has no built-in `:byte` constraint, causing HTTP 500. Always use:
```csharp
// WRONG — causes HTTP 500:
[HttpGet("by-type/{type:byte}")]

// CORRECT:
[HttpGet("by-type/{type:int:min(1):max(5)}")]
```
