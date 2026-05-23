# Feature Specification: Foundation Setup

**Feature Branch**: `001-foundation-setup`
**Created**: 2026-05-06
**Status**: Draft
**Input**: Phase 1 — Foundation from PRD-MVP

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Initializes the Solution (Priority: P1)

A developer clones the repository and opens the solution in Visual Studio. They see a clean 6-project solution structure that builds without errors. All project references follow the Clean Architecture dependency chain: Desktop → Api → Application → Infrastructure → Domain, with Contracts referenced by all layers.

**Why this priority**: Without a buildable solution, no other work can proceed. This is the absolute prerequisite for all subsequent phases.

**Independent Test**: Open `SalesSystem.sln` in Visual Studio and run `dotnet build`. The build completes with zero errors and zero warnings related to missing references.

**Acceptance Scenarios**:

1. **Given** the solution is freshly cloned, **When** a developer runs `dotnet build`, **Then** all 6 projects compile successfully with zero errors.
2. **Given** the solution is open in Visual Studio, **When** a developer inspects project references, **Then** the dependency chain matches Clean Architecture rules (Domain has zero project references; Infrastructure references Domain; Application references Domain and Contracts; Api references Application, Infrastructure, and Contracts; Desktop references Contracts only).

---

### User Story 2 - Domain Entities Represent All Business Data (Priority: P1)

A developer reviews the Domain project and finds C# entity classes for all 22 database tables defined in the schema. Each entity enforces business rules through private setters, factory methods, or domain methods. Financial calculations (LineTotal, SubTotal, TotalAmount, DueAmount) exist only in entity methods.

**Why this priority**: Entities are the foundation that all services, repositories, and DTOs depend on. Without correct entities, no business logic can be implemented.

**Independent Test**: Review each entity class and verify it matches the corresponding database table schema. Run unit tests that exercise domain calculation methods.

**Acceptance Scenarios**:

1. **Given** the Domain project is built, **When** a developer counts entity classes, **Then** there are entity classes corresponding to all 22 tables (Users, Units, Categories, Products, Warehouses, WarehouseStocks, Suppliers, Customers, PurchaseInvoices, PurchaseInvoiceItems, SalesInvoices, SalesInvoiceItems, PurchaseReturns, PurchaseReturnItems, SalesReturns, SalesReturnItems, StockTransfers, StockTransferItems, CustomerPayments, SupplierPayments, InventoryMovements, StoreSettings) plus DocumentSequences and a BaseEntity.
2. **Given** a SalesInvoiceItem entity, **When** `Quantity=5`, `UnitPrice=100`, `DiscountAmount=50`, **Then** `LineTotal` is calculated as `(5 * 100) - 50 = 450`.
3. **Given** a SalesInvoice entity, **When** items have LineTotals of 450 and 300, InvoiceDiscount=50, TaxAmount=0, PaidAmount=500, **Then** SubTotal=750, TotalAmount=700, DueAmount=200.
4. **Given** a SalesInvoice entity, **When** PaidAmount exceeds TotalAmount, **Then** the entity throws a domain exception.

---

### User Story 3 - Contracts Provide DTOs and Request/Response Models (Priority: P1)

A developer reviews the Contracts project and finds DTO classes for every entity, request models for create/update operations, and the shared `Result<T>` wrapper. These contracts define the data shapes used across all layers without leaking domain logic.

**Why this priority**: Contracts decouple the API surface from domain internals. Without them, services and controllers cannot be written.

**Independent Test**: Verify every entity has a corresponding DTO. Verify `Result<T>` supports success/failure patterns with error codes.

**Acceptance Scenarios**:

1. **Given** the Contracts project, **When** a developer lists DTO classes, **Then** every entity has at least one corresponding DTO.
2. **Given** the Contracts project, **When** a developer inspects `Result<T>`, **Then** it exposes `IsSuccess`, `Value`, `Error`, and `ErrorCode` properties with static factory methods `Success(T)` and `Failure(string, string?)`.
3. **Given** the Contracts project, **When** a developer inspects request models, **Then** create/update requests exist for Products, Customers, Suppliers, Warehouses, and Units at minimum.

---

### User Story 4 - Database Is Created from EF Core Migration (Priority: P2)

A developer sets the connection string environment variable and runs `dotnet ef database update`. The database is created with all 22+ tables, correct data types, foreign keys, CHECK constraints, and unique indexes matching the schema document.

**Why this priority**: The database is the persistence backbone. Without it, no integration testing or data verification is possible. Ranked P2 because it depends on entities being defined first.

**Independent Test**: Run the migration against a clean SQL Server instance. Query `INFORMATION_SCHEMA` to verify table count, column types, and constraints.

**Acceptance Scenarios**:

1. **Given** a clean SQL Server instance and a configured connection string, **When** `dotnet ef database update` is run, **Then** the database is created with all tables.
2. **Given** the created database, **When** the `WarehouseStocks` table is inspected, **Then** a CHECK constraint `Quantity >= 0` exists.
3. **Given** the created database, **When** money columns are inspected (e.g., `SalePrice`, `TotalAmount`, `PaidAmount`), **Then** all are `decimal(18,2)`.
4. **Given** the created database, **When** quantity columns are inspected (e.g., `Quantity` in WarehouseStocks, SalesInvoiceItems), **Then** all are `decimal(18,3)`.
5. **Given** the created database, **When** text columns are inspected, **Then** all are `nvarchar` (never `varchar`).
6. **Given** the created database, **When** foreign key constraints are inspected, **Then** all use `ON DELETE NO ACTION` (Restrict behavior).

---

### User Story 5 - Seed Data Enables First Login (Priority: P2)

After migration, the database contains seed data that allows a developer to immediately test the system: a default admin user, a default warehouse, a cash customer for walk-in sales, basic units of measurement, and initialized document sequences.

**Why this priority**: Seed data is needed for any manual or automated testing. Without it, the system is unusable after migration.

**Independent Test**: After migration, query each seed table to verify expected records exist.

**Acceptance Scenarios**:

1. **Given** the database after migration, **When** the Users table is queried, **Then** an admin user exists with username `admin`, a BCrypt-hashed password for `admin123`, and Role=1.
2. **Given** the database after migration, **When** the Warehouses table is queried, **Then** a default warehouse exists with `IsDefault=true` and Arabic name "المخزن الرئيسي".
3. **Given** the database after migration, **When** the Customers table is queried, **Then** a cash customer exists with code "CASH", Arabic name "عميل نقدي", and Balance=0.
4. **Given** the database after migration, **When** the Units table is queried, **Then** at least 5 units exist: قطعة, كيلو, لتر, متر, صندوق.
5. **Given** the database after migration, **When** the DocumentSequences table is queried, **Then** sequences exist for INV, PUR, SR, PR, TRF, CP, SP with LastNumber=0.

---

### Edge Cases

- What happens when the solution is built on a machine without SQL Server? The build succeeds; only migration/runtime fails gracefully with a connection error.
- What happens when an entity's financial calculation receives negative values? The domain entity validates inputs and rejects negative prices or quantities via guard clauses.
- What happens when a duplicate seed record is inserted? The migration/seeding logic uses `HasData` or idempotent checks to prevent duplicate key violations.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a .NET 10 LTS solution with exactly 6 projects following Clean Architecture dependency rules.
- **FR-002**: System MUST define Domain entity classes for all 22 database tables plus a shared `BaseEntity` with `Id`, `CreatedAt`, `UpdatedAt`, `IsActive` fields.
- **FR-003**: System MUST define all enums in the Domain layer: `UserRole`, `InvoiceStatus`, `PaymentType`, `MovementType` with exact values from the constitution.
- **FR-004**: System MUST compute `LineTotal`, `SubTotal`, `TotalAmount`, and `DueAmount` exclusively within Domain entity methods.
- **FR-005**: System MUST enforce `PaidAmount <= TotalAmount` in Domain entity methods, throwing a `DomainException` on violation.
- **FR-006**: System MUST provide a `Result<T>` class in Contracts with `IsSuccess`, `Value`, `Error`, `ErrorCode`, and static factory methods.
- **FR-007**: System MUST provide DTO classes for all entities and request models for CRUD operations.
- **FR-008**: System MUST provide a `PagedResult<T>` class for list endpoints supporting pagination.
- **FR-009**: System MUST provide EF Core entity configurations using Fluent API only (no DataAnnotations on entities).
- **FR-010**: System MUST configure all money columns as `decimal(18,2)` and quantity columns as `decimal(18,3)` in EF Core configurations.
- **FR-011**: System MUST configure all foreign keys with `DeleteBehavior.Restrict` (no cascade deletes).
- **FR-012**: System MUST configure global query filters for soft delete (`IsActive == true`) on all applicable entities.
- **FR-013**: System MUST configure all string columns as `nvarchar` with explicit `MaxLength`.
- **FR-014**: System MUST generate an initial EF Core migration that creates the complete database schema.
- **FR-015**: System MUST seed initial data: admin user (BCrypt-hashed password), default warehouse, cash customer, measurement units, and document sequences.
- **FR-016**: System MUST define custom exception types: `DomainException`, `NotFoundException`, `ValidationException`.

### Key Entities

- **Users**: System authentication with username, BCrypt password hash, role (Admin/Manager/Cashier), soft delete.
- **Products**: Catalog items with code, barcode, name, category FK, unit FK, purchase/sale prices, minimum stock level.
- **Warehouses**: Storage locations with default flag; linked to WarehouseStocks for per-product inventory.
- **WarehouseStocks**: Junction tracking quantity per product per warehouse with CHECK >= 0 constraint.
- **Invoices (Sales/Purchase)**: Header-detail pattern with status lifecycle (Draft→Posted→Cancelled), payment type, financial totals computed in domain.
- **Returns (Sales/Purchase)**: Similar header-detail with optional reference to original invoice.
- **StockTransfers**: Source-to-destination warehouse transfers with item details.
- **Payments (Customer/Supplier)**: Payment records linked optionally to invoices.
- **InventoryMovements**: Immutable audit log of every stock change with before/after quantities.
- **StoreSettings**: Single-row configuration (store name, tax settings, currency).
- **DocumentSequences**: Thread-safe auto-incrementing invoice number generator.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The solution builds successfully with zero errors on a clean machine with .NET 10 SDK installed.
- **SC-002**: All 6 projects are present and their dependency references match Clean Architecture rules.
- **SC-003**: Entity count matches or exceeds 22 domain entity classes plus supporting types (BaseEntity, enums, exceptions).
- **SC-004**: A fresh database migration creates all tables, constraints, and indexes within 30 seconds.
- **SC-005**: Seed data is present and correct after migration — admin can log in, default warehouse exists, cash customer is available.
- **SC-006**: All financial calculations in entity unit tests produce correct results with decimal precision (no floating-point rounding).
- **SC-007**: No `float`, `double`, `real`, or SQL `money` type appears anywhere in the codebase.

## Assumptions

- Developer has .NET 10 LTS SDK installed on their machine.
- SQL Server 2019+ (or SQL Server Express/LocalDB) is available for migration testing.
- The connection string is provided via the `SALESSYSTEM_DB_CONNECTION` environment variable.
- Visual Studio 2022+ or JetBrains Rider is used for development (solution file compatibility).
- No runtime services (API, Desktop) are started in this phase — only build and migrate.
