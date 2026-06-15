# Tasks: Foundation Setup

**Input**: Design documents from `specs/001-foundation-setup/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/
**Tests**: Not requested for this phase.
**IMPORTANT**: Each task is self-contained. Read `AGENTS.md` and `docs/CONSTITUTION.md` BEFORE starting ANY task.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story (US1-US5)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create the .NET 10 solution with 6 projects and correct references.

- [X] T001 [US1] Create solution file at `SalesSystem/SalesSystem.sln` using `dotnet new sln` inside a new `SalesSystem/` folder
- [X] T002 [US1] Create class library project `SalesSystem/SalesSystem.Domain/SalesSystem.Domain.csproj` using `dotnet new classlib`. Target `net10.0`. Add to solution. This project has ZERO NuGet packages and ZERO project references.
- [X] T003 [US1] Create class library project `SalesSystem/SalesSystem.Contracts/SalesSystem.Contracts.csproj` using `dotnet new classlib`. Target `net10.0`. Add to solution. No project references, no NuGet packages.
- [X] T004 [US1] Create class library project `SalesSystem/SalesSystem.Application/SalesSystem.Application.csproj`. Add project references to `SalesSystem.Domain` and `SalesSystem.Contracts`. No NuGet packages. Add to solution.
- [X] T005 [US1] Create class library project `SalesSystem/SalesSystem.Infrastructure/SalesSystem.Infrastructure.csproj`. Add project references to `SalesSystem.Domain` and `SalesSystem.Application`. Add NuGet: `Microsoft.EntityFrameworkCore.SqlServer` 10.x, `Microsoft.EntityFrameworkCore.Tools` 10.x, `BCrypt.Net-Next` 4.x. Add to solution.
- [X] T006 [US1] Create web API project `SalesSystem/SalesSystem.Api/SalesSystem.Api.csproj` using `dotnet new webapi`. Add project references to `SalesSystem.Application`, `SalesSystem.Infrastructure`, `SalesSystem.Contracts`. Add to solution. Add minimal `Program.cs` that reads connection string from env var `SALESSYSTEM_DB_CONNECTION`.
- [X] T007 [US1] Create WinForms project `SalesSystem/SalesSystem.Desktop/SalesSystem.Desktop.csproj` using `dotnet new winforms`. Add project reference to `SalesSystem.Contracts` ONLY. Add to solution. Create minimal `Program.cs` entry point.

**Checkpoint**: Run `dotnet build SalesSystem/SalesSystem.sln` — must compile with 0 errors.

---

## Phase 2: Foundational (Domain Layer)

**Purpose**: Create all Domain entities, enums, exceptions, and base types. ALL financial calculations MUST be in entity methods. Use `decimal` for money (18,2) and quantities (18,3). NEVER use float/double/int for these.

### Enums and Base Types

- [X] T008 [P] [US2] Create enum `SalesSystem/SalesSystem.Domain/Enums/UserRole.cs`: `public enum UserRole : byte { Admin = 1, Manager = 2, Cashier = 3 }`
- [X] T009 [P] [US2] Create enum `SalesSystem/SalesSystem.Domain/Enums/InvoiceStatus.cs`: `public enum InvoiceStatus : byte { Draft = 1, Posted = 2, Cancelled = 3 }`
- [X] T010 [P] [US2] Create enum `SalesSystem/SalesSystem.Domain/Enums/PaymentType.cs`: `public enum PaymentType : byte { Cash = 1, Credit = 2, Mixed = 3 }`
- [X] T011 [P] [US2] Create enum `SalesSystem/SalesSystem.Domain/Enums/MovementType.cs`: `public enum MovementType : byte { PurchaseIn = 1, SaleOut = 2, SaleReturnIn = 3, PurchaseReturnOut = 4, TransferOut = 5, TransferIn = 6, Adjustment = 7 }`
- [X] T012 [P] [US2] Create abstract class `SalesSystem/SalesSystem.Domain/Common/BaseEntity.cs` with properties: `int Id`, `DateTime CreatedAt`, `DateTime? UpdatedAt`, `bool IsActive` (default true). Use protected setters.
- [X] T013 [P] [US2] Create exception `SalesSystem/SalesSystem.Domain/Exceptions/DomainException.cs` extending `Exception` with a string message constructor.
- [X] T014 [P] [US2] Create exception `SalesSystem/SalesSystem.Domain/Exceptions/NotFoundException.cs` extending `Exception` with constructor taking `(string entityName, object key)`.
- [X] T015 [P] [US2] Create exception `SalesSystem/SalesSystem.Domain/Exceptions/ValidationException.cs` extending `Exception` with a `Dictionary<string, string[]> Errors` property.

### Core Entities (no financial logic)

- [X] T016 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/User.cs` inheriting `BaseEntity`. Properties: `string UserName`, `string PasswordHash`, `string FullName`, `UserRole Role`, `string? CreatedBy`, `string? UpdatedBy`. Private setters. Factory method `Create(...)`.
- [X] T017 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/Unit.cs` inheriting `BaseEntity`. Properties: `string Name`, `string? Symbol`, `string? CreatedBy`, `string? UpdatedBy`.
- [X] T018 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/Category.cs` inheriting `BaseEntity`. Properties: `string Name`, `string? Description`, `string? CreatedBy`, `string? UpdatedBy`.
- [X] T019 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/Product.cs` inheriting `BaseEntity`. Properties: `string? Code`, `string? Barcode`, `string Name`, `int? CategoryId`, `int? UnitId`, `decimal PurchasePrice`, `decimal SalePrice`, `decimal MinStock`, `string? Description`, `string? CreatedBy`, `string? UpdatedBy`. Navigation: `Category? Category`, `Unit? Unit`.
- [X] T020 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/Warehouse.cs` inheriting `BaseEntity`. Properties: `string? Code`, `string Name`, `string? Location`, `bool IsDefault`, `string? CreatedBy`, `string? UpdatedBy`.
- [X] T021 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/WarehouseStock.cs` — does NOT inherit BaseEntity. Properties: `int WarehouseStockId`, `int WarehouseId`, `int ProductId`, `decimal Quantity`, `DateTime UpdatedAt`. Navigation: `Warehouse Warehouse`, `Product Product`.
- [X] T022 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/Supplier.cs` inheriting `BaseEntity`. Properties: `string? Code`, `string Name`, `string? Phone`, `string? Email`, `string? Address`, `decimal OpeningBalance`, `decimal CurrentBalance`, `string? CreatedBy`, `string? UpdatedBy`. Add method `IncreaseBalance(decimal amount)` and `DecreaseBalance(decimal amount)`.
- [X] T023 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/Customer.cs` inheriting `BaseEntity`. Same pattern as Supplier. Balance convention: positive = customer owes us.

### Invoice Entities (WITH financial logic in domain)

- [X] T024 [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/SalesInvoiceLine.cs` — does NOT inherit BaseEntity. Properties: `int SalesInvoiceLineId`, `int SalesInvoiceId`, `int ProductId`, `decimal Quantity`, `decimal UnitPrice`, `decimal DiscountAmount`, `decimal LineTotal`. Add domain method that computes `LineTotal = (Quantity * UnitPrice) - DiscountAmount`. Navigation: `Product Product`.
- [X] T025 [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/SalesInvoice.cs` inheriting `BaseEntity`. Properties: `string InvoiceNo`, `int? CustomerId`, `int WarehouseId`, `DateTime InvoiceDate`, `DateOnly? DueDate`, `PaymentType PaymentType`, `decimal SubTotal`, `decimal DiscountAmount`, `decimal TaxAmount`, `decimal TotalAmount`, `decimal PaidAmount`, `decimal DueAmount`, `string? Notes`, `InvoiceStatus Status`, `string? CreatedBy`, `string? UpdatedBy`. Collection: `List<SalesInvoiceLine> Items`. Domain methods: `RecalculateTotals()` computes SubTotal/TotalAmount/DueAmount. Guard: throw `DomainException` if PaidAmount > TotalAmount. State transition methods enforcing Draft→Posted→Cancelled rules.
- [X] T026 [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/PurchaseInvoiceLine.cs` — same pattern as SalesInvoiceLine but `UnitCost` instead of `UnitPrice`. `LineTotal = (Quantity * UnitCost) - DiscountAmount`.
- [X] T027 [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/PurchaseInvoice.cs` — same pattern as SalesInvoice but with `int SupplierId` (required) instead of `CustomerId`. Same financial logic and state machine.

### Return, Transfer, Payment, System Entities

- [X] T028 [P] [US2] Create entities `SalesSystem/SalesSystem.Domain/Entities/SalesReturn.cs` and `SalesReturnItem.cs`. SalesReturn: `ReturnNo`, `SalesInvoiceId?`, `CustomerId`, `WarehouseId`, `ReturnDate`, `Reason?`, `SubTotal`, `TotalAmount`, `Status`. Item: `SalesReturnItemId`, `SalesReturnId`, `ProductId`, `Quantity`, `UnitPrice`, `LineTotal`.
- [X] T029 [P] [US2] Create entities `SalesSystem/SalesSystem.Domain/Entities/PurchaseReturn.cs` and `PurchaseReturnItem.cs`. Same pattern with `SupplierId`, `PurchaseInvoiceId?`, `UnitCost`.
- [X] T030 [P] [US2] Create entities `SalesSystem/SalesSystem.Domain/Entities/StockTransfer.cs` and `StockTransferItem.cs`. Transfer: `TransferNo`, `FromWarehouseId`, `ToWarehouseId`, `TransferDate`, `Notes?`, `Status`. Item: `StockTransferItemId`, `StockTransferId`, `ProductId`, `Quantity`, `Notes?`. Domain guard: FromWarehouseId != ToWarehouseId.
- [X] T031 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/CustomerPayment.cs` — does NOT inherit BaseEntity fully (no UpdatedAt). Properties: `int CustomerPaymentId`, `string PaymentNo`, `int CustomerId`, `int? SalesInvoiceId`, `DateTime PaymentDate`, `decimal Amount`, `byte PaymentMethod`, `string? ReferenceNo`, `string? Notes`, `string? CreatedBy`, `DateTime CreatedAt`.
- [X] T032 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/SupplierPayment.cs` — same pattern as CustomerPayment with `SupplierId`, `PurchaseInvoiceId?`.
- [X] T033 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/InventoryMovement.cs` — immutable audit record. PK: `long InventoryMovementId`. Properties: `int ProductId`, `int WarehouseId`, `MovementType MovementType`, `decimal QuantityChange`, `decimal QuantityBefore`, `decimal QuantityAfter`, `string ReferenceType`, `int ReferenceId`, `decimal? UnitCost`, `DateTime MovementDate`, `string? Notes`, `int? CreatedByUserId`.
- [X] T034 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/StoreSettings.cs` — does NOT inherit BaseEntity. Properties: `int StoreSettingsId`, `string StoreName`, `string? Phone`, `string? Address`, `string? LogoPath`, `string CurrencyCode` (default "SAR"), `decimal DefaultTaxRate`, `bool IsTaxEnabled`, `DateTime? UpdatedAt`.
- [X] T035 [P] [US2] Create entity `SalesSystem/SalesSystem.Domain/Entities/DocumentSequence.cs` — does NOT inherit BaseEntity. Properties: `int DocumentSequenceId`, `string DocumentType`, `string Prefix`, `int Year`, `int LastNumber`.

**Checkpoint**: `dotnet build SalesSystem/SalesSystem.Domain` — 0 errors. All 23 entity files exist.

---

## Phase 3: User Story 3 — Contracts (Priority: P1)

**Goal**: DTOs, Requests, Result<T>, PagedResult<T>, ErrorCodes for all entities.

- [X] T036 [P] [US3] Create `SalesSystem/SalesSystem.Contracts/Common/Result.cs` — generic `Result<T>` with `IsSuccess`, `Value`, `Error`, `ErrorCode` properties and static `Success(T)` / `Failure(string, string?)` factory methods. Also non-generic `Result` class.
- [X] T037 [P] [US3] Create `SalesSystem/SalesSystem.Contracts/Common/PagedResult.cs` with `Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`, `HasNext`, `HasPrevious`.
- [X] T038 [P] [US3] Create `SalesSystem/SalesSystem.Contracts/Common/ErrorCodes.cs` — static class with constants: `NotFound`, `ValidationError`, `DuplicateEntry`, `InsufficientStock`, `InvalidOperation`, `Unauthorized`, `Forbidden`.
- [X] T039 [P] [US3] Create DTO records in `SalesSystem/SalesSystem.Contracts/DTOs/`: `UserDto.cs`, `UnitDto.cs`, `CategoryDto.cs`, `ProductDto.cs` (include CategoryName, UnitName), `WarehouseDto.cs`, `WarehouseStockDto.cs` (include ProductName, WarehouseName), `SupplierDto.cs`, `CustomerDto.cs`. Use C# `record` types.
- [X] T040 [P] [US3] Create DTO records in `SalesSystem/SalesSystem.Contracts/DTOs/`: `SalesInvoiceDto.cs` (with `IReadOnlyList<SalesInvoiceLineDto> Items`), `SalesInvoiceLineDto.cs`, `PurchaseInvoiceDto.cs` (with Items), `PurchaseInvoiceLineDto.cs`.
- [X] T041 [P] [US3] Create DTO records in `SalesSystem/SalesSystem.Contracts/DTOs/`: `SalesReturnDto.cs`, `SalesReturnItemDto.cs`, `PurchaseReturnDto.cs`, `PurchaseReturnItemDto.cs`, `StockTransferDto.cs`, `StockTransferItemDto.cs`.
- [X] T042 [P] [US3] Create DTO records in `SalesSystem/SalesSystem.Contracts/DTOs/`: `CustomerPaymentDto.cs`, `SupplierPaymentDto.cs`, `InventoryMovementDto.cs`, `StoreSettingsDto.cs`, `DocumentSequenceDto.cs`.
- [X] T043 [P] [US3] Create request records in `SalesSystem/SalesSystem.Contracts/Requests/Products/`: `CreateProductRequest.cs` and `UpdateProductRequest.cs`. Fields: Code?, Barcode?, Name, CategoryId?, UnitId?, PurchasePrice, SalePrice, MinStock, Description?.
- [X] T044 [P] [US3] Create request records for remaining CRUD entities in `SalesSystem/SalesSystem.Contracts/Requests/`: `Customers/CreateCustomerRequest.cs`, `Customers/UpdateCustomerRequest.cs`, `Suppliers/CreateSupplierRequest.cs`, `Suppliers/UpdateSupplierRequest.cs`, `Warehouses/CreateWarehouseRequest.cs`, `Warehouses/UpdateWarehouseRequest.cs`, `Units/CreateUnitRequest.cs`, `Units/UpdateUnitRequest.cs`, `Categories/CreateCategoryRequest.cs`, `Categories/UpdateCategoryRequest.cs`.
- [X] T045 [P] [US3] Create auth contracts in `SalesSystem/SalesSystem.Contracts/Requests/Auth/LoginRequest.cs` (UserName, Password) and `SalesSystem/SalesSystem.Contracts/Responses/LoginResponse.cs` (Token, UserName, FullName, Role, ExpiresAt).

**Checkpoint**: `dotnet build SalesSystem/SalesSystem.Contracts` — 0 errors.

---

## Phase 4: User Story 4 — EF Core Infrastructure (Priority: P2)

**Purpose**: DbContext, ALL Fluent API configurations, and initial migration. RULES: Fluent API ONLY (no DataAnnotations), ALL FKs = DeleteBehavior.Restrict, ALL money = HasPrecision(18,2), ALL quantities = HasPrecision(18,3), ALL strings = nvarchar with MaxLength.

### Application Interfaces (needed by Infrastructure)

- [X] T046 [P] [US4] Create interface `SalesSystem/SalesSystem.Application/Interfaces/Repositories/IGenericRepository.cs` — generic repository with `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `Update`, `SoftDelete`.
- [X] T047 [P] [US4] Create interface `SalesSystem/SalesSystem.Application/Interfaces/IUnitOfWork.cs` — `SaveChangesAsync(CancellationToken)`, `BeginTransactionAsync(CancellationToken)`. Repository properties added in Phase 2.

### Entity Configurations (Fluent API)

- [X] T048 [P] [US4] Create `SalesSystem/SalesSystem.Infrastructure/Data/Configurations/UserConfiguration.cs` implementing `IEntityTypeConfiguration<User>`. Map to table "Users". Configure: UserName MaxLength(50) unique, PasswordHash MaxLength(256), FullName MaxLength(150), Role as tinyint with CHECK, CreatedBy/UpdatedBy MaxLength(150). HasQueryFilter(u => u.IsActive).
- [X] T049 [P] [US4] Create configurations for `UnitConfiguration.cs`, `CategoryConfiguration.cs` in `SalesSystem/SalesSystem.Infrastructure/Data/Configurations/`. Map string lengths per schema. Add `HasQueryFilter(x => x.IsActive)`.
- [X] T050 [P] [US4] Create `ProductConfiguration.cs`. Map: Code MaxLength(30) unique, Barcode MaxLength(50) unique, Name MaxLength(150), PurchasePrice HasPrecision(18,2), SalePrice HasPrecision(18,2), MinStock HasPrecision(18,3), Description MaxLength(500). FKs to Category and Unit with DeleteBehavior.Restrict. HasQueryFilter.
- [X] T051 [P] [US4] Create `WarehouseConfiguration.cs` and `WarehouseStockConfiguration.cs`. WarehouseStock: map to "WarehouseStocks", HasKey(WarehouseStockId), Quantity HasPrecision(18,3), unique index on (WarehouseId, ProductId), FKs with Restrict. NO query filter on WarehouseStock.
- [X] T052 [P] [US4] Create `SupplierConfiguration.cs` and `CustomerConfiguration.cs`. Map balance fields with HasPrecision(18,2). Code unique, Name MaxLength(150). HasQueryFilter.
- [X] T053 [P] [US4] Create `SalesInvoiceConfiguration.cs` and `SalesInvoiceLineConfiguration.cs`. Invoice: InvoiceNo unique, all money fields HasPrecision(18,2), FKs (Customer nullable, Warehouse required) with Restrict. HasMany Items with cascade on invoice delete ONLY (items belong to parent). Item: Quantity HasPrecision(18,3), UnitPrice/DiscountAmount/LineTotal HasPrecision(18,2), FK to Product with Restrict.
- [X] T054 [P] [US4] Create `PurchaseInvoiceConfiguration.cs` and `PurchaseInvoiceLineConfiguration.cs`. Same pattern as Sales but Supplier FK required, UnitCost instead of UnitPrice.
- [X] T055 [P] [US4] Create configurations for `SalesReturnConfiguration.cs`, `SalesReturnItemConfiguration.cs`, `PurchaseReturnConfiguration.cs`, `PurchaseReturnItemConfiguration.cs`. Same Fluent API patterns.
- [X] T056 [P] [US4] Create `StockTransferConfiguration.cs` and `StockTransferItemConfiguration.cs`. Two FK to Warehouses (FromWarehouseId, ToWarehouseId) both with Restrict.
- [X] T057 [P] [US4] Create `CustomerPaymentConfiguration.cs` and `SupplierPaymentConfiguration.cs`. Amount HasPrecision(18,2). FKs with Restrict.
- [X] T058 [P] [US4] Create `InventoryMovementConfiguration.cs`. PK is long (bigint). All quantity fields HasPrecision(18,3), UnitCost HasPrecision(18,2). Indexes: (ProductId, MovementDate DESC), (ReferenceType, ReferenceId). FK to Users with Restrict.
- [X] T059 [P] [US4] Create `StoreSettingsConfiguration.cs` and `DocumentSequenceConfiguration.cs`. StoreSettings: DefaultTaxRate HasPrecision(5,2). DocumentSequence: DocumentType MaxLength(10) unique.

### DbContext

- [X] T060 [US4] Create `SalesSystem/SalesSystem.Infrastructure/Data/SalesDbContext.cs`. Add DbSet for ALL 23 entities. Override `OnModelCreating` to apply all configurations via `modelBuilder.ApplyConfigurationsFromAssembly(...)`. Read connection string from env var in `OnConfiguring` or via DI.

### Migration

- [X] T061 [US4] Generate initial EF Core migration by running: `dotnet ef migrations add InitialCreate --project SalesSystem/SalesSystem.Infrastructure --startup-project SalesSystem/SalesSystem.Api`. Verify the generated migration SQL creates all tables with correct types.

**Checkpoint**: Run `dotnet ef database update` — database created with all tables, correct types, constraints.

---

## Phase 5: User Story 5 — Seed Data (Priority: P2)

**Goal**: Pre-populate database with admin user, default warehouse, cash customer, units, document sequences.

- [X] T062 [US5] Add seed data programmatically at application startup (HasData blocked by private setters)
- [X] T063 [US5] Add seed data programmatically at application startup
- [X] T064 [US5] Add seed data programmatically at application startup
- [X] T065 [US5] Add seed data programmatically at application startup
- [X] T066 [US5] Add seed data programmatically at application startup
- [X] T067 [US5] Regenerate migration after adding seed data

**Checkpoint**: Run migration, then query DB to verify all seed records exist per quickstart.md.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T068 [P] Delete auto-generated `Class1.cs` files from all class library projects.
- [X] T069 [P] Add `.gitignore` for .NET at `SalesSystem/.gitignore` (bin/, obj/, *.user, etc).
- [X] T070 Verify final build: `dotnet build SalesSystem/SalesSystem.sln` — 0 errors, 0 warnings.
- [X] T071 Run full verification from `specs/001-foundation-setup/quickstart.md` — all SQL queries return expected results.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Domain)**: Depends on Phase 1 — all entity files need projects to exist
- **Phase 3 (Contracts)**: Depends on Phase 1 only — can run in PARALLEL with Phase 2
- **Phase 4 (Infrastructure)**: Depends on Phase 2 (entities) and Phase 3 (interfaces)
- **Phase 5 (Seed Data)**: Depends on Phase 4 (configurations exist)
- **Phase 6 (Polish)**: Depends on all phases

### Parallel Opportunities

- **Phase 2**: ALL enum tasks (T008-T011) parallel. ALL exception tasks (T013-T015) parallel. ALL entity tasks (T016-T035) parallel.
- **Phase 3**: ALL contract tasks (T036-T045) parallel with each other AND with Phase 2.
- **Phase 4**: ALL configuration tasks (T048-T059) parallel. DbContext (T060) after configs.

## Implementation Strategy

### MVP First

1. Complete Phase 1 (Setup) → verify build
2. Complete Phase 2 (Domain entities) → verify build
3. Complete Phase 3 (Contracts) → verify build
4. Complete Phase 4 (Infrastructure) → verify migration
5. Complete Phase 5 (Seed data) → verify queries
6. Complete Phase 6 (Polish) → final verification

### Key Rules for Implementer

1. **ALWAYS** read `AGENTS.md` before writing any code
2. **NEVER** use `float`, `double`, or `int` for money/quantities
3. **NEVER** use DataAnnotations on Domain entities
4. **ALWAYS** use `DeleteBehavior.Restrict` on foreign keys
5. **ALWAYS** use `nvarchar` (never `varchar`) for strings
6. **ALWAYS** use `HasPrecision(18,2)` for money, `HasPrecision(18,3)` for quantities
7. **ALWAYS** add `HasQueryFilter(x => x.IsActive)` on entities that inherit BaseEntity

---

## Notes

- [P] tasks = different files, no dependencies
- Each task has exact file path — create the file at that path
- Commit after each phase or logical group
- Stop at any checkpoint to validate
- If build fails: fix before proceeding to next phase
