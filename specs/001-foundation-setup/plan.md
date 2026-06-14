# Implementation Plan: Foundation Setup

**Branch**: `001-foundation-setup` | **Date**: 2026-06-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-foundation-setup/spec.md`, database schema from `docs/database-schema.md`

## Summary

Establish the complete .NET 10 LTS solution with 6 Clean Architecture projects, a 5-class base entity hierarchy spanning 65 database tables across 8 business modules, a Contracts layer (DTOs, Requests, Result<T>), EF Core Infrastructure with exclusive Fluent API configurations, an initial database migration, and comprehensive seed data. This phase produces a buildable solution and a fully migrated database — the foundation for all subsequent phases.

The schema has evolved significantly from the original 22-table design. The 65 tables are organized by module rather than flat. Key architectural additions include: a Parties table decoupling shared contact data (name, phone, email, address) from Customers, Suppliers, and Employees; a full Chart of Accounts (Accounts table with self-referencing ParentId hierarchy for 4-level nesting); InventoryBatches for FIFO/FEFO cost layer tracking; JournalEntry/JournalEntryLines implementing double-entry bookkeeping with CHECK constraints; an RBAC security model with Roles, Permissions, RolePermissions, and UserSessions; separate receipt/voucher tables (CustomerReceipts, SupplierPayments, ReceiptVouchers, PaymentVouchers) replacing monolithic payment tables; and Infrastructure support tables (AuditLogs, SystemLogs, Attachments, Notifications) for operational concerns.

The base class hierarchy was split from a single BaseEntity into five distinct abstract classes — Entity, AuditableEntity, ActivatableEntity, DocumentEntity, LongEntity — each modeling a specific column inheritance pattern. This eliminates nullable audit columns on junction tables and ensures each entity inherits exactly the columns it needs. The enum surface grew from 4 to 13 enums, with all enum values mapped as tinyint via explicit HasConversion<int>() in Fluent API configurations.

Seed data is comprehensive: 4 roles (including Observer for read-only access), 2 currencies with an initial exchange rate, 7 base units of measure, one default branch with one default warehouse, one default customer and supplier (each with auto-linked Chart of Accounts entries via the Parties table), 33 permissions across 9 categories with a 4-role assignment matrix, one admin user (BCrypt-hashed password), one default tax record, and initialized DocumentSequences for all document types.

## Technical Context

**Language/Version**: C# / .NET 10 LTS
**Primary Dependencies**: Entity Framework Core 10, BCrypt.Net-Next 4.x
**Storage**: SQL Server 2019+ via EF Core (Code-First migrations)
**Testing**: Manual build verification
**Target Platform**: Windows (Desktop + Web API backend)
**Project Type**: Desktop application with Web API backend (Clean Architecture)
**Performance Goals**: Solution build < 30s, Migration < 30s
**Constraints**: Decimal-only financials, nvarchar-only text, barcodes as varchar (ASCII-only), Fluent API only, Restrict-only FKs
**Scale/Scope**: 65 database tables, 6 projects, ~140+ C# files, ~100+ seed records

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal-Only Financial Precision | ✅ PASS | All money = decimal(18,2), quantities = decimal(18,3), percentages = decimal(5,2) |
| II | Domain-Computed Financial Formulas | ✅ PASS | LineTotal, SubTotal, NetTotal, RemainingAmount computed exclusively in entity methods |
| III | Transactional Integrity | ⬜ N/A | No service-layer transactions in Phase 1 |
| IV | Invoice Lifecycle State Machine | ✅ PASS | DocumentEntity base class with Status (Draft/Posted/Cancelled) and guard methods |
| V | Stock Integrity | ✅ PASS | CHECK constraint on WarehouseStocks.Quantity >= 0; InventoryBatches track FIFO layers |
| VI | Result Pattern | ✅ PASS | Result<T> defined in Contracts with IsSuccess, Value, Error, ErrorCode |
| VII | Clean Architecture Boundaries | ✅ PASS | 6-project structure with strict dependency direction — Domain has zero references |
| VIII | Security | ✅ PASS | BCrypt-hashed seed password (work factor 12); Roles/Permissions entities defined |
| IX | Four-Layer Validation | ✅ PASS | Domain guard clauses + DB CHECK constraints on critical columns (quantities, rates, amounts) |
| X | Logging Standard | ⬜ N/A | Serilog configured in Phase 2 |
| XI | EF Core Conventions | ✅ PASS | Fluent API only, DeleteBehavior.Restrict on all FKs, nvarchar text, HasPrecision, HasConversion<int>() on enums |
| XII | Audit Trail | ✅ PASS | CreatedByUserId + CreatedAt on all ActivatableEntity and DocumentEntity inheritors |

**Gate Result**: ✅ ALL applicable principles satisfied.

## Solution Architecture

The 6-project Clean Architecture solution enforces strict dependency direction. No project may reference a layer it does not depend on. The dependency chain reads: DesktopPWF → Api → Application → Infrastructure → Domain, with Contracts referenced by all layers. Domain is the innermost ring with zero outward references.

**Domain** sits at the center with zero NuGet dependencies and zero project references. It contains entity classes (one per database table), enum definitions, domain exception types (DomainException, NotFoundException, ValidationException), and the five base classes. No EF Core package, no JSON serializer, no external NuGet package touches this project. This is the most critical architectural constraint — violations break the entire Clean Architecture model because every other layer depends on Domain remaining pure. Entity classes use only C# built-in types (int, decimal, DateTime, string, bool) and enums. There is no interface, no attribute, no base class from any framework.

**Contracts** defines data transfer objects (DTOs) for every entity surface that crosses layer boundaries, request models (CreateXxxRequest, UpdateXxxRequest) organized by module folder, response models (LoginResponse), and shared infrastructure such as Result<T>, PagedResult<T>, and ErrorCodes. It is referenced by all other layers but references no other project — it is an independent leaf. It carries no business logic — only data shapes and serialization attributes (System.Text.Json annotations). Result<T> is the universal return type that all Application services will use in later phases.

**Application** depends on Domain (for entity types and enums) and Contracts (for DTOs and Result<T>). It defines service interfaces (IProductService, ICustomerService, ISalesInvoiceService, etc.), repository abstractions (IGenericRepository<TEntity>), and the IUnitOfWork contract. It contains no concrete implementations — those live in Infrastructure. This layer is where business logic coordination and orchestration will live in later phases. In Phase 1, it defines only the infrastructure contracts (IUnitOfWork, IGenericRepository) and leaves service interfaces for Phase 2.

**Infrastructure** references Application (for interfaces) and Domain (for entities). It owns the EF Core SalesDbContext subclass, all Fluent API entity configuration classes (one file per entity in Configurations/), the concrete UnitOfWork implementation wrapping DbContext transactions, and the concrete GenericRepository<T> implementation. It also registers dependency injection extension methods for composition root use. It is the only project with SQL Server and EF Core package references. It accepts Microsoft.EntityFrameworkCore.SqlServer 10.x, Microsoft.EntityFrameworkCore.Tools 10.x, BCrypt.Net-Next 4.x, and QuestPDF as approved dependencies.

**Api** depends on Application, Infrastructure, and Contracts. It hosts ASP.NET Core controllers, middleware pipeline (ExceptionMiddleware, JWT Bearer authentication), Swagger/OpenAPI configuration, and the DI composition root in Program.cs. It contains no business logic — only endpoint method bodies that call Application service methods and translate Result<T> to HTTP responses. In Phase 1, it contains only Program.cs with minimal DI setup (DbContext, UnitOfWork, basic health endpoint) and placeholder appsettings.json. CORS, authentication, and full controller infrastructure arrive in Phase 2.

**DesktopPWF** depends on Contracts only. It communicates with the API exclusively via typed HttpClient services that deserialize Contract DTOs. It never references Infrastructure, never connects to the database, never instantiates DbContext, and never imports EF Core. The WPF UI uses the MVVM pattern with ViewModels, Views (XAML UserControls and Windows), an EventBus for cross-ViewModel communication, and IDialogService for user-facing dialogs. In Phase 1, it contains only Program.cs as a WPF entry point stub — no windows or ViewModels yet.

## Base Class Hierarchy

The original single BaseEntity has been replaced with five distinct abstract base classes, each modeling a specific column inheritance pattern from the database schema. This design eliminates nullable audit columns on tables that do not need them, prevents schema bloat on high-volume tables, ensures compile-time type safety, and makes the intent of each entity explicit at the class declaration level.

**Entity** is the simplest base — a single int Id property (auto-increment PK) with no audit columns, no soft-delete flag, no status field. Used exclusively for lightweight junction tables, line-item tables, and lookup mappings where change tracking is unnecessary because the parent document or transaction owns the audit trail. Which entities inherit Entity: UserRoles, RolePermissions, UserBranches, SalesInvoiceLines, PurchaseInvoiceLines, SalesReturnLines, PurchaseReturnLines, JournalEntryLines, InventoryTransactionLines, InventoryCountLines, InventoryAdjustmentLines, WarehouseTransferLines, CustomerReceiptApplications, SupplierPaymentApplications.

**AuditableEntity** extends Entity with CreatedAt (DateTime, set once on creation via constructor), UpdatedAt (DateTime?, set on every modification via UpdateTimestamp()), CreatedByUserId (int? FK to Users), and UpdatedByUserId (int? FK to Users). Used for live-balance tracking tables where change tracking matters but soft-delete does not — these records represent current state and should never be hidden from queries. Which entities inherit AuditableEntity: WarehouseStocks, InventoryBatches, CurrencyRates.

**ActivatableEntity** extends AuditableEntity with an IsActive bit column (default true) for soft-delete support. A global EF Core query filter (HasQueryFilter(x => x.IsActive)) automatically excludes soft-deleted records from all LINQ queries, preventing accidental inclusion of inactive records in reports, lookups, and lists. Used by all administration, configuration, party, and reference data entities that can be deactivated without removing their historical references. Which entities inherit ActivatableEntity: Parties, Customers, CustomerContacts, Suppliers, SupplierContacts, Departments, Employees, Roles, Users, Permissions, Branches, Warehouses, Currencies, Taxes, ProductCategories, Products, Units, AccountCategories, Accounts, CashBoxes, Banks, CompanySettings, SystemSettings, DocumentSequences, FiscalYears, Notifications, Attachments, ProductUnits, ProductPrices, SystemAccountMappings. For all these entities, soft-delete means IsActive = false (never hard-delete). Filtered unique indexes on these tables include the condition AND [IsActive] = 1 to prevent unique-key conflicts with soft-deleted records.

**DocumentEntity** extends AuditableEntity with a Status tinyint column (Draft=1, Posted=2, Cancelled=3) that controls the document lifecycle state machine. Used by all transactional documents that follow the invoice lifecycle — they are never soft-deleted; instead they transition through statuses. Domain methods on each entity enforce valid transitions: a Draft document can be Posted or Cancelled, a Posted document can only be Cancelled (with stock/reversal effects), and a Cancelled document is terminal with no further transitions. Which entities inherit DocumentEntity: SalesInvoices, PurchaseInvoices, SalesReturns, PurchaseReturns, JournalEntries, ReceiptVouchers, PaymentVouchers, Expenses, InventoryTransactions, InventoryCounts, InventoryAdjustments, WarehouseTransfers, CustomerReceipts, SupplierPayments.

**LongEntity** provides a single bigint Id property with no audit columns. Used exclusively for high-volume logging and audit tables where int PKs (max 2.1 billion records) would overflow over the system's lifetime. Which entities inherit LongEntity: AuditLogs, SystemLogs.

All five base classes are defined in the Domain/Common/ folder with protected constructors or init-only setters as appropriate. Factory methods (static Create methods with guard clauses) and business logic (Post, Cancel, UpdateDetails, MarkAsDeleted, IncreaseBalance) reside in the concrete entity classes — never in the base classes. The bases define only property storage and minimal constructor logic for setting Id and timestamps.

## Domain Entities — 8 Module Walkthrough

The 65 entities are organized into 8 business modules that mirror the schema's module boundaries. Entity names match table names exactly, and each entity has a corresponding Fluent API configuration class in Infrastructure/Data/Configurations/.

### Module 1: Core, Parties & Security (14 entities)

Parties, Customers, CustomerContacts, Suppliers, SupplierContacts, Departments, Employees, Roles, Users, UserRoles, Permissions, RolePermissions, UserBranches, UserSessions.

The Parties table is the shared contact repository — it stores name, phone, email, address, tax number, and notes. Customers, Suppliers, and Employees each link to Parties via a PartyId FK rather than duplicating these fields. AccountId FK on Customers and Suppliers connects each transacting party to the Chart of Accounts. Users store UserName (unique, nvarchar 50), PasswordHash (nvarchar 256), MustChangePassword flag, LoginAttempts counter, IsLocked flag, and LastLoginAt timestamp. UserRoles provides many-to-many linking between Users and Roles. Permissions store Code, DisplayName, and Category; RolePermissions links roles to permissions. UserBranches restricts users to specific branches. UserSessions tracks active session tokens with expiration and revocation support.

### Module 2: Organization, Currencies & Settings (11 entities)

Branches, Warehouses, Currencies, CurrencyRates, Taxes, CompanySettings, SystemSettings, DocumentSequences, FiscalYears, Notifications, Attachments.

Branches use smallint PKs and represent organizational locations. Warehouses use smallint PKs, belong to a Branch via BranchId FK, and store phone, address, and notes. Currencies use smallint PKs with ISO 4217 char(3) Code, filtered unique indexes on Code and Name when IsActive=1, and a filtered unique index on IsBaseCurrency when IsBaseCurrency=1 AND IsActive=1. CurrencyRates store RateToBase (decimal 18,2, CHECK > 0) with effective date ranges. Taxes use smallint PKs with Name, Code, Rate (decimal 5,2, CHECK 0-100), TaxType (Standard/ZeroRated/Exempt), and a filtered IsDefault index. CompanySettings is a singleton (tinyint PK default 1) storing company name, contact info, logo path, and default currency. SystemSettings uses a flexible key-value design with SettingKey (unique), SettingValue, SettingType (String/Integer/Decimal/Boolean), Category, DisplayName, and Description. DocumentSequences track NextNumber per DocumentType for thread-safe invoice number generation. FiscalYears define accounting periods with StartDate, EndDate, and IsClosed flag. Notifications store user-targeted messages with type, read status, and reference linking. Attachments provide polymorphic file references via ReferenceType/ReferenceId pattern.

### Module 3: Products (5 entities)

ProductCategories, Products, Units, ProductUnits, ProductPrices.

ProductCategories store category name and description. Products store Name, Barcode (varchar 50, filtered unique), CategoryId FK, TaxId FK, Description, TrackExpiry flag, ImagePath, and ReorderLevel (decimal 18,3). Products carry no price or cost columns — pricing is fully delegated to the ProductPrices table. Units use smallint PKs with Name and Symbol, plus an IsSystem flag to protect core units from modification. ProductUnits is the junction table linking Products to Units with a Factor (conversion factor, decimal 18,3) and IsBaseUnit flag — exactly one base unit per product. ProductPrices stores price per ProductUnit per CurrencyId with effective date ranges and a CHECK (Price >= 0) constraint.

### Module 4: Accounting (10 entities)

AccountCategories, Accounts, CashBoxes, Banks, JournalEntries, JournalEntryLines, ReceiptVouchers, PaymentVouchers, Expenses, SystemAccountMappings.

AccountCategories classify accounts by type group. Accounts implement a self-referencing hierarchy (ParentId FK to self) for the 4-level Chart of Accounts. Each account has AccountCode (filtered unique), Name, Nature (Asset/Liability/Equity/Revenue/Expense), IsLeaf flag (leaf accounts allow transactions), IsSystem flag (protects system accounts from modification), and CategoryId FK. CashBoxes link to Accounts via AccountId — balance is tracked on the linked Account, not on CashBox itself. Banks similarly link to Accounts via AccountId and store AccountNumber and IBAN. JournalEntries and JournalEntryLines implement double-entry bookkeeping with EntryNo, EntryDate, EntryType (Manual/Sales/Purchase/Receipt/Payment/Inventory/Adjustment), ReferenceType/ReferenceId pattern, IsReversed flag, and ReversedByEntryId self-reference. CHECK constraints enforce CHK_DebitOrCredit (exactly one of Debit/Credit is non-zero) and CHK_NoNegativeValues on JournalEntryLines. ReceiptVouchers and PaymentVouchers handle cash receipt and payment transactions with voucher number, date, currency, cash box, account, and amount. Expenses track operational expenditures. SystemAccountMappings use a flexible key-value design to map logical account roles (SalesRevenue, COGS, AccountsReceivable) to specific AccountIds.

### Module 5: Inventory (10 entities)

WarehouseStocks, InventoryBatches, InventoryTransactions, InventoryTransactionLines, InventoryCounts, InventoryCountLines, InventoryAdjustments, InventoryAdjustmentLines, WarehouseTransfers, WarehouseTransferLines.

WarehouseStocks uses a unique composite key on (WarehouseId, ProductId) with a CHECK constraint ensuring Quantity >= 0. It is an AuditableEntity (no IsActive — it is a live balance). InventoryBatches track cost layers with BatchNo (internal), ProductId, WarehouseId, PurchaseInvoiceId (source document), SupplierBatchNo, ExpiryDate, QuantityReceived, QuantityRemaining (both CHECK >= 0), and UnitCost (CHECK >= 0). Indexed on (ProductId, WarehouseId), (ExpiryDate) filtered, and (PurchaseInvoiceId). InventoryTransactions use a header-detail pattern replacing the old InventoryMovements table, supporting 12 transaction types (Purchase through InternalReceipt). InventoryCounts and InventoryCountLines perform physical inventory counts with SystemQuantity, ActualQuantity, and DifferenceQuantity per product-batch combination. InventoryAdjustments and InventoryAdjustmentLines handle stock corrections for Opening, Increase, Shortage, and Damage scenarios. WarehouseTransfers and WarehouseTransferLines manage stock movement between warehouses with full batch tracking.

### Module 6: Sales (6 entities)

SalesInvoices, SalesInvoiceLines, SalesReturns, SalesReturnLines, CustomerReceipts, CustomerReceiptApplications.

SalesInvoices use InvoiceNo (int, unique per table) as a user-facing number, separate from the auto-increment Id PK. They store CustomerId, WarehouseId, CurrencyId, PaymentType (Cash/Credit/Mixed), CashBoxId, TaxId, and all financial totals (SubTotal, DiscountAmount, TaxAmount, OtherCharges, NetTotal, PaidAmount with CHECK 0 to NetTotal, RemainingAmount). SalesInvoiceLines link to SalesInvoiceId and store ProductId, ProductUnitId, Quantity (decimal 18,3), UnitPrice, and LineTotal (computed as Quantity x UnitPrice). No per-line discount — discounts apply at header level only. SalesReturns link to the original SalesInvoiceId and store ReturnNo, ReturnDate, CustomerId, WarehouseId, CurrencyId, TotalAmount, and Status. SalesReturnLines link to the original SalesInvoiceLineId for precise return tracking. CustomerReceipts track incoming payments with ReceiptNo, date, CustomerId, CashBoxId, CurrencyId, Amount, and Status. CustomerReceiptApplications optionally distribute a receipt across specific invoices via SalesInvoiceId and AppliedAmount.

### Module 7: Purchases (6 entities)

PurchaseInvoices, PurchaseInvoiceLines, PurchaseReturns, PurchaseReturnLines, SupplierPayments, SupplierPaymentApplications.

Mirror the Sales module structure with SupplierId replacing CustomerId and UnitPrice on lines representing purchase unit cost. OtherCharges on the invoice header capture landed costs (transport, customs, insurance) for full cost accounting. On posting, the service creates InventoryBatches with BatchNo, QuantityReceived, QuantityRemaining, and UnitCost. PurchaseReturns link to the original PurchaseInvoiceLineId. SupplierPayments use PaymentNo, and SupplierPaymentApplications optionally distribute across specific purchase invoices.

### Module 8: Infrastructure & Support (2 entities)

AuditLogs, SystemLogs.

AuditLogs uses bigint PK with UserId, Action, EntityName, EntityId, Details (nvarchar max JSON), IpAddress, and CreatedAt. Indexed on (UserId, CreatedAt DESC), (EntityName, EntityId), and (CreatedAt DESC) for query performance. SystemLogs uses bigint PK with Level (Info/Warning/Error/Critical), Source, Message, Exception (serialized), and CreatedAt. Indexed on (Level, CreatedAt DESC) for error monitoring.

## Entity-to-Base-Class Mapping Summary

The 65 entities map to base classes as follows, organized by counts. Entity (no audit columns, no soft delete): 15 entities covering junction tables (UserRoles, RolePermissions, UserBranches) and all line-item tables (SalesInvoiceLines, PurchaseInvoiceLines, SalesReturnLines, PurchaseReturnLines, JournalEntryLines, InventoryTransactionLines, InventoryCountLines, InventoryAdjustmentLines, WarehouseTransferLines, CustomerReceiptApplications, SupplierPaymentApplications, InventoryTransactionLines via Entity). AuditableEntity (audit columns but no soft delete): 3 entities — WarehouseStocks (live balance), InventoryBatches (cost layer tracking), CurrencyRates (rate history). ActivatableEntity (full audit + soft delete): 33 entities covering all party, product, configuration, and reference data — Parties, Customers, CustomerContacts, Suppliers, SupplierContacts, Departments, Employees, Roles, Users, Permissions, Branches, Warehouses, Currencies, Taxes, ProductCategories, Products, Units, AccountCategories, Accounts, CashBoxes, Banks, CompanySettings, SystemSettings, DocumentSequences, FiscalYears, Notifications, Attachments, ProductUnits, ProductPrices, SystemAccountMappings. DocumentEntity (audit + status lifecycle): 13 entities covering all transactional documents — SalesInvoices, PurchaseInvoices, SalesReturns, PurchaseReturns, JournalEntries, ReceiptVouchers, PaymentVouchers, Expenses, InventoryTransactions, InventoryCounts, InventoryAdjustments, WarehouseTransfers, CustomerReceipts, SupplierPayments. LongEntity (bigint PK): 2 entities — AuditLogs, SystemLogs.

## File Organization

The 6 projects follow a consistent folder structure within the SalesSystem/ solution root. Each project mirrors the namespace hierarchy.

The Domain project is organized into Common/ (the five base classes), Entities/ (one file per entity, 65 files), Entities/Parties/ (Party-related entities), Entities/Products/ (product-related entities), Entities/Accounting/ (accounting entities), Entities/Inventory/ (inventory entities), Entities/Sales/ (sales entities), Entities/Purchases/ (purchase entities), Entities/Organization/ (branch, warehouse, currency, tax, settings entities), Entities/Security/ (user, role, permission entities), Enums/ (13 enum files), and Exceptions/ (3 exception files). Entity files are grouped by module subfolder rather than dumped flat into a single Entities directory — this keeps the project navigable at 65 files.

The Infrastructure project mirrors this with Data/Configurations/ (one Fluent API configuration file per entity, 65+ files, organized in the same module subfolder structure), Data/SalesDbContext.cs, Repositories/GenericRepository.cs, Repositories/UnitOfWork.cs, and DependencyInjection.cs for DI registration extensions.

The Contracts project organizes files into Common/ (Result.cs, PagedResult.cs, ErrorCodes.cs), DTOs/ (one DTO file per entity, grouped by module subfolder), Requests/ (Create and Update request models grouped by module subfolder), and Responses/ (login response, paged response wrappers).

The Application project contains Interfaces/Repositories/IGenericRepository.cs, Interfaces/IUnitOfWork.cs, and empty Interfaces/Services/ directory for Phase 2 service interfaces.

The Api project contains Program.cs and appsettings.json as stubs.

The DesktopPWF project contains Program.cs as a WPF stub with App.xaml and MainWindow.xaml framework — the actual ViewModels and Views arrive in Phase 5.

This organization ensures that as the codebase grows to its full size (~500+ files across all phases), related files remain discoverable by module rather than by type.

## Enum Strategy

Thirteen enums are defined in Domain/Enums/, each stored as tinyint in the database via explicit HasConversion<int>() in the Fluent API configuration. This ensures the database stores numeric values that remain stable even if enum member names change. The enums are:

InvoiceStatus: Draft=1, Posted=2, Cancelled=3. PaymentType: Cash=1, Credit=2, Mixed=3. AccountNature: Asset=1, Liability=2, Equity=3, Revenue=4, Expense=5. TaxType: Standard=1, ZeroRated=2, Exempt=3. JournalEntryType: Manual=1, Sales=2, Purchase=3, Receipt=4, Payment=5, Inventory=6, Adjustment=7. InventoryTransactionType: 12 values from Purchase=1 through InternalReceipt=12. InventoryReferenceType: 7 values from PurchaseInvoice=1 through Adjustment=7. AdjustmentType: Opening=1, Increase=2, Shortage=3, Damage=4. SettingType: String=1, Integer=2, Decimal=3, Boolean=4. NotificationType: LowStock=1, ExpirySoon=2, CreditLimitExceeded=3, System=4, Reminder=5. LogLevel: Info=1, Warning=2, Error=3, Critical=4. UserStatus: Active=1, Inactive=2, Locked=3. UserRole (not stored as enum in the new schema — roles are records in the Roles table with smallint PK): Admin=1, Accountant=2, Cashier=3, Observer=4.

All enum values must match the AGENTS.md Section 3 specification exactly. No deviations are permitted. If a new enum is needed, it must follow the same pattern and be approved by the lead architect.

## Contracts Layer

The Contracts project is organized in three sub-folders: Common (shared infrastructure), DTOs (data shapes for every entity), and module-specific Request/Response folders.

In Common, Result<T> provides IsSuccess, Value, Error, and ErrorCode properties with static factory methods Result<T>.Success(value) and Result<T>.Failure(error, errorCode). This is the universal return type for all service methods across all phases. PagedResult<T> wraps list endpoints with TotalCount, Page, PageSize, and Items properties. ErrorCodes defines string constants for common error scenarios. The DuplicateCode constant is explicitly removed per RULE-197 — product, customer, and supplier entities use auto-increment Id as sole identifier, not manual codes. DuplicateBarcode remains for barcode uniqueness validation.

DTOs are flat projection classes with no business logic. Each entity has at minimum one DTO (XxxDto) exposing the fields that cross service boundaries. Sensitive fields (PasswordHash) are never exposed in DTOs. Module folders organize DTOs by business domain (Products, Customers, Suppliers, Sales, Purchases, Inventory, Accounting, Organization).

Request models (CreateXxxRequest, UpdateXxxRequest) carry data annotation validation attributes for ASP.NET Core model binding. They are organized by module folder and contain only the fields the client is allowed to specify — computed fields, audit fields, and system-managed fields are excluded.

## DbContext and Fluent API Configuration

The SalesDbContext class in Infrastructure/Data/ registers all entity configurations by scanning the Configurations folder in OnModelCreating. Each entity has a dedicated IEntityTypeConfiguration<TEntity> class in Infrastructure/Data/Configurations/ named XxxConfiguration.

Configuration conventions enforced across all 65+ configuration files:

String properties use HasMaxLength with explicit values matching the schema (nvarchar 20-1000 depending on field). Money fields (decimal 18,2) use HasPrecision(18, 2). Quantity fields (decimal 18,3) use HasPrecision(18, 3). Percentage fields (decimal 5,2) use HasPrecision(5, 2). ExchangeRate fields use HasPrecision(18, 2). All enum properties use HasConversion<int>() to store as tinyint. All string columns are nvarchar except barcodes (varchar 50).

Foreign keys universally use OnDelete(DeleteBehavior.Restrict). Zero exceptions. Self-referencing FKs (Accounts.ParentId, JournalEntries.ReversedByEntryId) also use Restrict. Navigation properties are mapped explicitly with HasOne(x => x.Property).WithMany(x => x.Collection).HasForeignKey(x => x.FkColumn) — never bare WithOne() or WithMany() without specifying the navigation lambda.

Global query filters are applied in OnModelCreating after all configurations are loaded. Every ActivatableEntity inheritor receives HasQueryFilter(x => x.IsActive). DocumentEntity inheritors receive no soft-delete filter — their Status column handles lifecycle. WarehouseStocks and similar AuditableEntity inheritors have no soft-delete filter.

CHECK constraints are added via HasCheckConstraint on the table entity type builder. Critical constraints include: WarehouseStocks.Quantity >= 0, InventoryBatches.QuantityReceived >= 0, InventoryBatches.QuantityRemaining >= 0, InventoryBatches.UnitCost >= 0, JournalEntryLines CHK_DebitOrCredit (exactly one of Debit/Credit is non-zero, or both zero), JournalEntryLines CHK_NoNegativeValues (Debit >= 0 AND Credit >= 0), CurrencyRates.RateToBase > 0, Taxes.Rate >= 0 AND Rate <= 100, Products.Price >= 0 (on ProductPrices table), FiscalYears.EndDate > StartDate.

Filtered unique indexes use the EF Core HasIndex().IsUnique().HasFilter() pattern. Soft-deletable entities include AND [IsActive] = 1 in the filter. Key filtered indexes: Currencies on Code and Name (WHERE IsActive = 1), Currencies on IsBaseCurrency (WHERE IsBaseCurrency = 1 AND IsActive = 1), Taxes on IsDefault (WHERE IsDefault = 1 AND IsActive = 1), Products on Barcode (WHERE Barcode IS NOT NULL AND IsActive = 1), Accounts on AccountCode (WHERE IsActive = 1).

Composite indexes for query performance are added on frequently queried columns: WarehouseStocks (WarehouseId, ProductId) unique, InventoryBatches (ProductId, WarehouseId), InventoryTransactions (WarehouseId, TransactionDate DESC), InventoryTransactions (ReferenceType, ReferenceId), AuditLogs (UserId, CreatedAt DESC), AuditLogs (EntityName, EntityId), AuditLogs (CreatedAt DESC), SystemLogs (Level, CreatedAt DESC), JournalEntries (EntryNo), JournalEntries (ReferenceType, ReferenceId), Notifications (UserId, IsRead, CreatedAt DESC), Attachments (ReferenceType, ReferenceId), UserSessions (UserId, IsRevoked), CurrencyRates (CurrencyId, EffectiveFrom).

## Seed Data Strategy

Seed data ensures the system boots into a usable state after the first migration. All seed records are applied via the HasData() method in each entity's configuration class. This approach is idempotent — EF Core checks for existing records by key and only inserts missing ones.

Four roles are seeded: Admin (Id=1, name "مدير النظام"), Accountant (Id=2, name "محاسب"), Cashier (Id=3, name "كاشير"), Observer (Id=4, name "مشاهد"). This is expanded from the original 3-role design to support read-only access.

Two currencies are seeded: Saudi Riyal (SAR, code="SAR", symbol="﷼", fraction="هللة", decimal places=2, IsBaseCurrency=true) and US Dollar (USD, code="USD", symbol="$", fraction="Cent", decimal places=2, IsBaseCurrency=false). An initial CurrencyRate maps USD at 3.75 SAR per USD.

Seven base units are seeded: حبة (Piece, symbol="pc"), كرتون (Box, symbol="box"), كيلو (Kilo, symbol="kg"), لتر (Liter, symbol="l"), متر (Meter, symbol="m"), علبة (Pack, symbol="pck"), كيس (Sack, symbol="sk"). All have IsSystem=true to prevent deletion.

One default branch is seeded: الفرع الرئيسي (The Main Branch). One default warehouse under that branch: المستودع الرئيسي (The Main Warehouse), with IsActive=true.

A default Party record is created with name "عميل نقدي" for the cash customer. A Customer record links to this Party and to an auto-created Account under the 1210 parent. A second Party with name "مورد نقدي" serves as the default supplier, linked to an Account under 2100 parent. These ensure cash transactions always have a valid counterparty.

Thirty-three permissions are seeded across 9 categories (Sales, Purchases, Inventory, Customers, Suppliers, Products, Reports, Accounting, System, Operations, Audit) with a 4-role assignment matrix. Each permission has a unique Code string (e.g., "Sales.Create", "Sales.View", "Purchases.Approve") and a DisplayName in Arabic. The Admin role receives all 33 permissions. The Accountant role receives permissions for Sales (Create, Edit, View, Cancel, Return), Purchases (Create, Edit, View, Cancel, Return), Inventory (View, Adjust, Transfer), Customers (Create, Edit, View), Suppliers (Create, Edit, View), Products (Create, Edit, View), Reports (View, Export), Accounting (View, CreateEntry), but NOT System or Audit permissions. The Cashier role receives only Sales permissions (Create, Edit, View, Return, Discount) plus Customer View and Product View — no purchasing, accounting, or system access. The Observer role receives only View permissions across all modules plus Report View — no Create, Edit, Cancel, or Delete on any entity. This matrix is enforced by the RolePermissions junction table with unique (RoleId, PermissionId) tuples.

One admin user is seeded with username "admin", PasswordHash set to BCrypt("admin123") with work factor 12, MustChangePassword=false, linked to the Admin role via UserRoles. No other users are seeded.

One tax record is seeded: "ضريبة القيمة المضافة" (VAT) at 15% rate, TaxType=Standard, IsDefault=true.

The CompanySettings singleton is seeded with placeholder company name ("شركتي") and DefaultCurrencyId referencing the SAR currency.

DocumentSequences are initialized with NextNumber=1 for the following document types: SalesInvoice, PurchaseInvoice, SalesReturn, PurchaseReturn, WarehouseTransfer, CustomerReceipt, SupplierPayment.

SystemSettings are seeded across 8 categories with 29+ settings covering: inventory tracking method (FIFO), default costing method (WeightedAverage), decimal places (2), low stock alert threshold, expiry alert days, credit limit enforcement, barcode format, thermal/A4 printer names, store tax number, logo path, receipt footer note, and notification preferences. The StoreTaxRate setting is seeded as 0.00 (Tax entity is the source of truth).

## Key Architectural Patterns

**UnitOfWork with ExecutionStrategy**: The IUnitOfWork interface exposes SaveChangesAsync, BeginTransactionAsync, CommitAsync, RollbackAsync, and the critical ExecuteTransactionAsync method. The concrete UnitOfWork implementation wraps SqlServerRetryingExecutionStrategy for retry-safe transactions. Raw BeginTransactionAsync is never used when retry strategy is configured — RULE-275 enforcement. ChangeTracker.AutoDetectChangesEnabled is set to false for performance, with manual DetectChanges calls before SaveChanges.

**Domain Factory Methods**: Entities use static Create methods or parameterized constructors with guard clauses. Factory methods validate all inputs, apply defaults, and return a fully constructed entity in a valid state. Private setters prevent external mutation — state changes go through domain methods (MarkAsDeleted, Post, Cancel, UpdateDetails).

**Financial Formula Centralization**: LineTotal, SubTotal, NetTotal, and RemainingAmount are computed exclusively inside entity methods using decimal arithmetic. No service, controller, or ViewModel computes these values. The formulas use decimal multiplication and addition only — never floating-point.

**Soft Delete Discipline**: ActivatableEntity inheritors use IsActive=false for deletion. Document entities use Status=Cancelled. Users are never hard-deleted. Hard-delete endpoints (PermanentDeleteAsync) return Result.Failure for user entities. All services that attempt hard-delete catch DbUpdateException and return a user-friendly Arabic error message.

**Mapping Without AutoMapper**: DTO-to-entity and entity-to-DTO mapping is done via explicit mapping methods in Application service implementations. This avoids the AutoMapper dependency and makes mappings explicit and debuggable.

**Result Pattern for Service Boundaries**: All Application service methods return Result<T> or Result. Domain exceptions from entity guard clauses are caught by the service and translated to Result.Failure with Arabic error messages and appropriate ErrorCodes. Controllers translate Result to HTTP 200/400/404/503 status codes. This pattern ensures consistent error handling across the entire API surface.

## Task Sequencing

The implementation follows 17 ordered tasks. Each task produces buildable output that can be verified before proceeding to the next.

1. Create solution structure: New solution file SalesSystem.sln in SalesSystem/ root. Six projects created with correct SDKs (Microsoft.NET.Sdk for Domain/Application/Contracts, Microsoft.NET.Sdk.Web for Api, Microsoft.NET.Sdk.Desktop for DesktopPWF, Microsoft.NET.Sdk for Infrastructure). Project references set: Domain references nothing, Contracts references nothing, Application references Domain + Contracts, Infrastructure references Application + Domain, Api references Application + Infrastructure + Contracts, DesktopPWF references Contracts only. Directory.Build.props sets Nullable=enable, ImplicitUsings=enable, LangVersion=net10.0. NuGet packages added per approved list.

2. Define Domain base classes and enums: Five abstract base classes in Domain/Common/. All 13 enums in Domain/Enums/. Three exception types in Domain/Exceptions/.

3. Implement Module 1 entities: 14 entity classes with full guard clauses, factory methods, and domain validation. Use ActivatableEntity for Parties, Customers, Suppliers, Departments, Employees, Roles, Users, Permissions. Use Entity for UserRoles, RolePermissions, UserBranches. Use AuditableEntity for UserSessions.

4. Implement Module 2 entities: 11 entity classes. Use ActivatableEntity for Branches, Warehouses, Currencies, Taxes, CompanySettings, SystemSettings, DocumentSequences, FiscalYears, Notifications. Use Entity for CurrencyRates (no IsActive). Attachments uses ActivatableEntity.

5. Implement Module 3 entities: 5 entity classes. ProductCategories, Products use ActivatableEntity. Units use ActivatableEntity with IsSystem flag. ProductUnits and ProductPrices use AuditableEntity.

6. Implement Module 4 entities: 10 entity classes. AccountCategories, Accounts, CashBoxes, Banks use ActivatableEntity. JournalEntries and JournalEntryLines use DocumentEntity/Entity respectively. ReceiptVouchers, PaymentVouchers, Expenses use DocumentEntity. SystemAccountMappings uses ActivatableEntity.

7. Implement Module 5 entities: 10 entity classes. WarehouseStocks uses AuditableEntity (no IsActive). InventoryBatches uses AuditableEntity. InventoryTransactions uses DocumentEntity. InventoryTransactionLines uses Entity. InventoryCounts and InventoryAdjustments use DocumentEntity. CountLines and AdjustmentLines use Entity. WarehouseTransfers uses DocumentEntity. TransferLines uses Entity.

8. Implement Module 6 entities: 6 entity classes. SalesInvoices uses DocumentEntity. SalesInvoiceLines uses Entity. SalesReturns uses DocumentEntity. SalesReturnLines uses Entity. CustomerReceipts uses DocumentEntity. CustomerReceiptApplications uses Entity.

9. Implement Module 7 entities: 6 entity classes. Same pattern as Module 6 — DocumentEntity for headers, Entity for lines and applications.

10. Implement Module 8 entities: 2 entity classes. Both use LongEntity for bigint PK.

11. Define Contracts project: Result<T>, PagedResult<T>, ErrorCodes in Common/. DTO classes for all entities organized by module folder. Request models (Create/Update) per module. Response models for auth endpoints.

12. Implement Application interfaces: IUnitOfWork with SaveChangesAsync, BeginTransactionAsync, CommitAsync, RollbackAsync, ExecuteTransactionAsync. IGenericRepository<T> with GetByIdAsync, GetAllAsync, AddAsync, Update, Remove, GetQueryable.

13. Build Infrastructure DbContext: SalesDbContext class with OnModelCreating override. All 65+ entity configuration files in Configurations/ folder, one per entity. Global query filters for IsActive. CHECK constraints via HasCheckConstraint. Filtered unique indexes via HasFilter. Composite query indexes.

14. Build Infrastructure UnitOfWork and Repository: Concrete UnitOfWork implementing IUnitOfWork with DbContext transaction management and ExecutionStrategy integration. GenericRepository<T> implementing IGenericRepository<T> with full LINQ method support.

15. Create initial EF Core migration: Run dotnet ef migrations add InitialCreate targeting the Infrastructure project with SalesSystem.Api as startup project. Review generated migration for correctness — verify all 65 CreateTable calls, all FK Restrict behaviors, all CHECK constraints, all indexes.

16. Apply seed data: Add HasData() calls to all appropriate entity configurations. Two-pass approach for Accounts: Level 1 saved first, then Level 2-4 reference correct ParentId. Verify seed data by running dotnet ef database update against a clean SQL Server and querying all seeded tables.

17. Build and verify: Run dotnet build from solution root — zero errors, zero warnings. Run dotnet ef database update in a test environment — verify all 65 tables created with correct schema. Spot-check column types (decimal 18,2 for money, decimal 18,3 for quantities, nvarchar for text, varchar for barcodes). Verify no Cascade delete on any table. Verify seed data count meets expectations (4 roles, 2 currencies, 7 units, 1 branch, 1 warehouse, 33 permissions, 1 user, 1 tax, 1 company settings, 7+ document sequences, 29+ system settings, 1 default customer with party + account, 1 default supplier with party + account).

## Risks and Mitigations

**Migration ordering complexity**: The 65-table schema has dependencies across modules — InventoryBatches references PurchaseInvoices, UserRoles references both Users and Roles, CustomerReceiptApplications references CustomerReceipts and SalesInvoices. The EF Core migration generator handles this automatically when all configurations are applied in OnModelCreating, but manual review of the generated migration is essential. Mitigation: configurations are added in strict module order (Core → Organization → Products → Accounting → Inventory → Sales → Purchases → Infrastructure) and the migration is reviewed line-by-line before applying.

**Two-pass seed for self-referencing Accounts**: The Accounts table has a self-referencing ParentId FK with Restrict behavior. Level 1 accounts must be inserted before Level 2 accounts that reference them. EF Core's HasData() evaluates at model-building time, so all levels must be seeded in the same HasData() call with correct Id values assigned manually. Mitigation: seed data uses explicit int Ids (1-60) with ParentId values that reference already-known Ids. The two-pass approach (Level 1 → SaveChanges → Level 2+) used in services is NOT applicable to HasData() — instead, all account seed data is defined in a single AccountsConfiguration.HasData() with self-consistent Id and ParentId values.

**Seed data volume**: The combined seed records exceed 100 rows across 20+ tables. Large HasData() blocks can slow migration and make the migration file verbose. Mitigation: seed data is split per entity configuration file rather than a single centralized seeder. Each entity's configuration owns its seed data, keeping individual HasData() calls manageable.

**FK Restrict enforcement**: The constitutional mandate that all FKs use DeleteBehavior.Restrict must be verified exhaustively. A single Cascade delete would violate the constitution. Mitigation: after the migration is generated, a script or manual inspection scans ALL CreateTable calls for ON DELETE CASCADE and flags any occurrence for immediate correction.

**Enum value stability**: If an enum member is reordered or removed in a later phase, the database values become inconsistent. Mitigation: enum values are explicitly assigned (not relying on default 0,1,2 ordering) and never reused after removal. Deprecated enum members remain defined but unused.
