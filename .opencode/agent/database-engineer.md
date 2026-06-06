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
