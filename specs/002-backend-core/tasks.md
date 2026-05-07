# Tasks: Backend Core (Phase 2)

**Input**: Design documents from `specs/002-backend-core/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-endpoints.md, quickstart.md

**Tests**: Not requested — no test tasks generated.

**Organization**: Tasks grouped by user story. Each task is self-contained with exact file paths, class names, and implementation details so a smaller model can execute without additional context.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1–US6)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: No new projects needed. Phase 1 Foundation created the solution. This phase adds NuGet packages and configuration files required by Phase 2.

- [x] T001 Add NuGet packages to `SalesSystem.Infrastructure/SalesSystem.Infrastructure.csproj`: add `BCrypt.Net-Next` version 4.x. Run: `dotnet add SalesSystem/SalesSystem.Infrastructure/SalesSystem.Infrastructure.csproj package BCrypt.Net-Next`
- [x] T002 Add NuGet packages to `SalesSystem.Api/SalesSystem.Api.csproj`: add `FluentValidation.AspNetCore` 11.x, `Serilog.AspNetCore` 8.x, `Serilog.Sinks.File` 5.x, `Swashbuckle.AspNetCore` 6.x, `Microsoft.AspNetCore.Authentication.JwtBearer` 10.x. Run `dotnet add` for each package.
- [x] T003 Create JWT settings section. Add `JwtSettings` record in `SalesSystem.Contracts/Common/JwtSettings.cs` with properties: `Secret` (string), `Issuer` (string), `Audience` (string), `ExpirationHours` (int, default 8). This is a simple POCO — no logic.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story. Includes GenericRepository, UnitOfWork, ExceptionMiddleware, Serilog config, and JWT setup.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [x] T004 Implement `GenericRepository<T>` in `SalesSystem.Infrastructure/Data/Repositories/GenericRepository.cs`. Class implements `IGenericRepository<T>` (from `SalesSystem.Application.Interfaces.Repositories`). Constructor takes `SalesDbContext`. Methods: `GetByIdAsync` → `FindAsync(id)`; `GetAllAsync` → `ToListAsync()`; `AddAsync` → `AddAsync` + return entity; `UpdateAsync` → `Entry(entity).State = Modified`; `SoftDeleteAsync` → find by id, set `IsActive = false`. All methods use CancellationToken. The global query filter on `IsActive` in DbContext handles filtering automatically.
- [x] T005 Implement `UnitOfWork` in `SalesSystem.Infrastructure/Data/UnitOfWork.cs`. Class implements `IUnitOfWork`. Constructor takes `SalesDbContext`. Expose lazy-initialized `IGenericRepository<T>` properties for: Users, Units, Categories, Products, Warehouses, Suppliers, Customers (matching the interface). `SaveChangesAsync` → delegates to `_context.SaveChangesAsync(ct)`. `BeginTransactionAsync` → calls `_context.Database.BeginTransactionAsync(ct)` and wraps the returned `Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction` in a private `DbContextTransactionWrapper` class that implements the custom `SalesSystem.Application.Interfaces.IDbContextTransaction` (which extends `IAsyncDisposable, IDisposable`).
- [x] T006 Implement `JwtTokenGenerator` in `SalesSystem.Infrastructure/Security/JwtTokenGenerator.cs`. Create interface `IJwtTokenGenerator` in `SalesSystem.Application/Interfaces/Services/IJwtTokenGenerator.cs` with method `string GenerateToken(User user)`. Implementation: takes `JwtSettings` via constructor. Uses `System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler`. Claims: `sub` = UserId, `unique_name` = UserName, `role` = Role (as string). Signs with `SymmetricSecurityKey` from `JwtSettings.Secret`. Expiry = `JwtSettings.ExpirationHours` hours.
- [x] T007 Implement `ExceptionMiddleware` in `SalesSystem.Api/Middleware/ExceptionMiddleware.cs`. Constructor takes `RequestDelegate next` and `ILogger<ExceptionMiddleware>`. In `InvokeAsync(HttpContext)`: wrap `await _next(context)` in try/catch. On exception: log with `_logger.LogError(ex, "Unhandled exception")`, set response status 500, write JSON `{ "error": "An unexpected error occurred", "errorCode": "INTERNAL_ERROR" }`. Content type = `application/json`.
- [x] T008 Configure `SalesSystem.Api/Program.cs` — Wire up ALL infrastructure. This is the most critical task. Add in order: (1) Serilog: `builder.Host.UseSerilog((ctx, cfg) => cfg.WriteTo.File("logs/salessystem-.log", rollingInterval: RollingInterval.Day))`. (2) DI registrations: `AddScoped<IUnitOfWork, UnitOfWork>`, `AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>))`, `AddScoped<IJwtTokenGenerator, JwtTokenGenerator>`, bind `JwtSettings` from env vars. (3) JWT Auth: `AddAuthentication(JwtBearerDefaults)` + `AddJwtBearer(opts => { opts.TokenValidationParameters = new { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(...), ValidateIssuer/Audience = true } })`. (4) Authorization policies: `AddAuthorization(opts => { opts.AddPolicy("AdminOnly", p => p.RequireRole("1")); opts.AddPolicy("ManagerAndAbove", p => p.RequireRole("1","2")); opts.AddPolicy("AllStaff", p => p.RequireRole("1","2","3")); })`. (5) FluentValidation: `AddValidatorsFromAssemblyContaining<Program>()`. (6) Swagger: `AddEndpointsApiExplorer()` + `AddSwaggerGen()` with JWT security definition. (7) Middleware pipeline: `UseSwagger()`, `UseSwaggerUI()`, `UseMiddleware<ExceptionMiddleware>()`, `UseAuthentication()`, `UseAuthorization()`. (8) Keep existing seed logic intact.

**Checkpoint**: Foundation ready — build must pass with 0 errors. All services registered in DI. Swagger accessible at `/swagger`. JWT auth pipeline active.

---

## Phase 3: User Story 1 — Staff Login and Secure Access (Priority: P1) 🎯 MVP

**Goal**: Users can log in with username/password and receive a JWT token. All endpoints reject unauthenticated/unauthorized requests.

**Independent Test**: `POST /api/auth/login` with admin/admin123 returns JWT. Using that JWT on protected endpoints succeeds. No JWT → 401. Wrong role → 403.

### Implementation for User Story 1

- [x] T009 [US1] Create `IAuthService` interface in `SalesSystem.Application/Interfaces/Services/IAuthService.cs`. Single method: `Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct)`. Uses `SalesSystem.Contracts.Requests.Auth.LoginRequest` and `SalesSystem.Contracts.Responses.LoginResponse`.
- [x] T010 [US1] Implement `AuthService` in `SalesSystem.Application/Services/AuthService.cs`. Constructor takes `IUnitOfWork` and `IJwtTokenGenerator`. `LoginAsync`: (1) find user by UserName using `_uow.Users.GetAllAsync()` then filter (or add a custom query method), (2) check `IsActive`, (3) verify password with `BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)`, (4) generate token via `_jwtTokenGenerator.GenerateToken(user)`, (5) return `Result<LoginResponse>.Success(new LoginResponse(...))`. On failure: return `Result<LoginResponse>.Failure("Invalid credentials", ErrorCodes.Unauthorized)`. Log login attempts via `ILogger<AuthService>`.
- [x] T011 [US1] Register `AuthService` in `SalesSystem.Api/Program.cs`: add `builder.Services.AddScoped<IAuthService, AuthService>()`.
- [x] T012 [US1] Create `LoginRequestValidator` in `SalesSystem.Api/Validators/LoginRequestValidator.cs`. Rules: `UserName` required + max 50 chars; `Password` required + min 3 chars.
- [x] T013 [US1] Implement `AuthController` in `SalesSystem.Api/Controllers/AuthController.cs`. Route: `[Route("api/auth")]`. Single endpoint: `[HttpPost("login")] [AllowAnonymous]` — takes `LoginRequest`, calls `_authService.LoginAsync(request, ct)`, returns `Ok(result.Value)` on success, `Unauthorized(new { error = result.Error })` on failure.

**Checkpoint**: Login works via Swagger. Token received. Protected endpoints return 401 without token.

---

## Phase 4: User Story 2 — Product Catalog Management (Priority: P2)

**Goal**: Managers can create, read, update, search, filter, and soft-delete products. Cashiers are blocked.

**Independent Test**: Create a product via `POST /api/products`, list via `GET /api/products`, update and deactivate. Verify duplicate code/barcode rejected.

### Implementation for User Story 2

- [x] T014 [P] [US2] Create `IProductService` interface in `SalesSystem.Application/Interfaces/Services/IProductService.cs`. Methods: `Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct)`, `Task<Result<PagedResult<ProductDto>>> GetAllAsync(string? search, int? categoryId, int page, int pageSize, CancellationToken ct)`, `Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct)`, `Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct)`, `Task<Result> DeleteAsync(int id, CancellationToken ct)`.
- [x] T015 [US2] Implement `ProductService` in `SalesSystem.Application/Services/ProductService.cs`. Constructor takes `IUnitOfWork` and `ILogger<ProductService>`. For `CreateAsync`: check duplicate Code (if provided) and Barcode (if provided) by querying all products and filtering — return `Result.Failure` with `ErrorCodes.DuplicateCode`/`DuplicateBarcode` if found. Create entity via `Product.Create(...)`, add via `_uow.Products.AddAsync(...)`, save via `_uow.SaveChangesAsync(ct)`. Map to `ProductDto` (include CategoryName and UnitName by loading related entities). For `GetAllAsync`: load all products, apply search filter (Name contains, Code contains, Barcode contains), apply categoryId filter, apply pagination manually. For `UpdateAsync`: find by id, null-check, update fields, save. For `DeleteAsync`: call `_uow.Products.SoftDeleteAsync(id, ct)`, save. All methods return `Result<T>`.
- [x] T016 [P] [US2] Create `ProductRequestValidators` in `SalesSystem.Api/Validators/ProductRequestValidators.cs`. `CreateProductRequestValidator`: Name required 2-150 chars, SalePrice >= 0, PurchasePrice >= 0, MinStock >= 0, Code max 30, Barcode max 50. `UpdateProductRequestValidator`: same rules.
- [x] T017 [US2] Register `ProductService` in `SalesSystem.Api/Program.cs`: add `builder.Services.AddScoped<IProductService, ProductService>()`.
- [x] T018 [US2] Implement `ProductsController` in `SalesSystem.Api/Controllers/ProductsController.cs`. Route: `[Route("api/products")]`, `[Authorize(Policy = "ManagerAndAbove")]`. Endpoints: `[HttpGet]` → `GetAllAsync` with query params (search, categoryId, page=1, pageSize=20) → returns `Ok(result.Value)`; `[HttpGet("{id:int}")]` → `GetByIdAsync` → Ok or NotFound; `[HttpPost]` → `CreateAsync` → `CreatedAtAction` or BadRequest; `[HttpPut("{id:int}")]` → `UpdateAsync` → Ok or NotFound/BadRequest; `[HttpDelete("{id:int}")]` → `DeleteAsync` → NoContent or NotFound.
- [x] T019 [US2] Checkpoint: All product CRUD endpoints work. Manager can create products, Cashier is forbidden from POST/PUT/DELETE.

**Checkpoint**: Products CRUD works in Swagger. Search and filter by category works. Duplicate code/barcode rejected. Cashier gets 403.

---

## Phase 5: User Story 3 — Customer and Supplier Management (Priority: P3)

**Goal**: Managers manage customers and suppliers. Cashiers can view customers only.

**Independent Test**: Create customer and supplier, verify balance initialization, verify Cashier view-only access.

### Implementation for User Story 3

- [x] T019 [P] [US3] Create `ICustomerService` interface in `SalesSystem.Application/Interfaces/Services/ICustomerService.cs`. Methods mirror ProductService pattern: GetById, GetAll (with search + pagination), Create, Update, Delete. All return `Result<T>`.
- [x] T020 [P] [US3] Create `ISupplierService` interface in `SalesSystem.Application/Interfaces/Services/ISupplierService.cs`. Same pattern as ICustomerService.
- [x] T021 [US3] Implement `CustomerService` in `SalesSystem.Application/Services/CustomerService.cs`. Follow ProductService pattern. On Create: set `CurrentBalance = OpeningBalance`. Check duplicate Code if provided. Map to `CustomerDto`.
- [x] T022 [US3] Implement `SupplierService` in `SalesSystem.Application/Services/SupplierService.cs`. Follow ProductService pattern. On Create: set `CurrentBalance = OpeningBalance`. Check duplicate Code if provided. Map to `SupplierDto`.
- [x] T023 [P] [US3] Create `CustomerRequestValidators` in `SalesSystem.Api/Validators/CustomerRequestValidators.cs`. Name required 2-150, Phone max 20, Email max 100 + valid format if provided, Code max 30.
- [x] T024 [P] [US3] Create `SupplierRequestValidators` in `SalesSystem.Api/Validators/SupplierRequestValidators.cs`. Same rules as Customer validators.
- [x] T025 [US3] Register both services in `SalesSystem.Api/Program.cs`.
- [x] T026 [US3] Implement `CustomersController` in `SalesSystem.Api/Controllers/CustomersController.cs`. Route: `[Route("api/customers")]`. GET endpoints use `[Authorize(Policy = "AllStaff")]`. POST/PUT/DELETE use `[Authorize(Policy = "ManagerAndAbove")]`. Same endpoint pattern as ProductsController.
- [x] T027 [US3] Implement `SuppliersController` in `SalesSystem.Api/Controllers/SuppliersController.cs`. Route: `[Route("api/suppliers")]`, `[Authorize(Policy = "ManagerAndAbove")]` on class. Same endpoint pattern as ProductsController.

**Checkpoint**: Customer/Supplier CRUD works. Cashier can GET customers but gets 403 on POST/PUT/DELETE. Supplier fully restricted to Manager+.

---

## Phase 6: User Story 4 — Warehouse Management (Priority: P4)

**Goal**: Admin manages warehouses. Single-default constraint enforced.

**Independent Test**: Create warehouse, set as default, verify previous default is unset. Manager gets 403.

### Implementation for User Story 4

- [x] T028 [P] [US4] Create `IWarehouseService` interface in `SalesSystem.Application/Interfaces/Services/IWarehouseService.cs`. Same CRUD pattern. Add special logic note: on Create/Update when `IsDefault = true`, must unset all other warehouses' IsDefault.
- [x] T029 [US4] Implement `WarehouseService` in `SalesSystem.Application/Services/WarehouseService.cs`. Follow ProductService pattern. Special logic in Create/Update: if `request.IsDefault == true`, load all warehouses, set all `IsDefault = false`, then set the new/updated one to `IsDefault = true`, save all changes. Check duplicate Code if provided. Map to `WarehouseDto`.
- [x] T030 [P] [US4] Create `WarehouseRequestValidators` in `SalesSystem.Api/Validators/WarehouseRequestValidators.cs`. Name required 2-100, Code max 30.
- [x] T031 [US4] Register `WarehouseService` in `SalesSystem.Api/Program.cs`.
- [x] T032 [US4] Implement `WarehousesController` in `SalesSystem.Api/Controllers/WarehousesController.cs`. Route: `[Route("api/warehouses")]`, `[Authorize(Policy = "AdminOnly")]`. Same endpoint pattern as ProductsController.

**Checkpoint**: Warehouse CRUD works. Only one default warehouse at a time. Only Admin can access. Manager gets 403.

---

## Phase 7: User Story 5 — Document Sequence Generation (Priority: P5)

**Goal**: Thread-safe auto-generation of sequential document numbers (INV-2026-000001).

**Independent Test**: Call GetNextNumber multiple times, verify sequential unique output.

### Implementation for User Story 5

- [x] T033 [US5] Create `IDocumentSequenceService` interface in `SalesSystem.Application/Interfaces/Services/IDocumentSequenceService.cs`. Single method: `Task<Result<string>> GetNextNumberAsync(string prefix, CancellationToken ct)`. Returns formatted string like "INV-2026-000001".
- [x] T034 [US5] Implement `DocumentSequenceService` in `SalesSystem.Application/Services/DocumentSequenceService.cs`. Constructor takes `IUnitOfWork` and `ILogger<DocumentSequenceService>`. Private static field: `private static readonly SemaphoreSlim _lock = new(1, 1)`. In `GetNextNumberAsync`: (1) `await _lock.WaitAsync(ct)`, (2) try: find DocumentSequence by prefix and current year (load all, filter). If not found and year changed, create new record for current year with LastNumber=0. Increment `LastNumber`, save. Format: `$"{prefix}-{year}-{number:D6}"`. Return Success. (3) finally: `_lock.Release()`. Log each sequence generation.
- [x] T035 [US5] Register `DocumentSequenceService` in `SalesSystem.Api/Program.cs`.

**Checkpoint**: Sequence generation works. Numbers are unique and sequential. Thread-safe under concurrent access.

---

## Phase 8: User Story 6 — Error Handling and Logging (Priority: P6)

**Goal**: All errors return consistent JSON. All operations logged via Serilog. Passwords never logged.

**Independent Test**: Send invalid request → get structured 400. Trigger exception → get generic 500. Check log file exists and contains entries without passwords.

### Implementation for User Story 6

- [x] T036 [P] [US6] Create `CategoryService` in `SalesSystem.Application/Services/CategoryService.cs` (with `ICategoryService` interface in `SalesSystem.Application/Interfaces/Services/ICategoryService.cs`). Simple CRUD following ProductService pattern. Map to `CategoryDto`. Check duplicate Name.
- [x] T037 [P] [US6] Create `UnitService` in `SalesSystem.Application/Services/UnitService.cs` (with `IUnitService` interface in `SalesSystem.Application/Interfaces/Services/IUnitService.cs`). Simple CRUD following ProductService pattern. Map to `UnitDto`.
- [x] T038 [P] [US6] Create `CategoryRequestValidators` in `SalesSystem.Api/Validators/CategoryRequestValidators.cs`. Name required 2-100.
- [x] T039 [P] [US6] Create `UnitRequestValidators` in `SalesSystem.Api/Validators/UnitRequestValidators.cs`. Name required 1-50.
- [x] T040 [US6] Register CategoryService and UnitService in `SalesSystem.Api/Program.cs`.
- [x] T041 [US6] Implement `CategoriesController` in `SalesSystem.Api/Controllers/CategoriesController.cs`. Route: `[Route("api/categories")]`, `[Authorize(Policy = "ManagerAndAbove")]`. Standard CRUD endpoints.
- [x] T042 [US6] Implement `UnitsController` in `SalesSystem.Api/Controllers/UnitsController.cs`. Route: `[Route("api/units")]`, `[Authorize(Policy = "ManagerAndAbove")]`. Standard CRUD endpoints.
- [x] T043 [US6] Enhance `ExceptionMiddleware` (created in T007) to also handle `FluentValidation.ValidationException` — return 400 with `{ "error": "Validation failed", "errorCode": "VALIDATION_ERROR", "details": [...field errors...] }`.

**Checkpoint**: Categories and Units CRUD works. All validation errors return structured JSON. Log file at `logs/` contains entries. No passwords in logs.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, cleanup, and build validation.

- [x] T044 Verify all service registrations in `SalesSystem.Api/Program.cs` — ensure every IXxxService is registered as scoped.
- [x] T045 Run `dotnet build SalesSystem/SalesSystem.slnx` and fix any compilation errors. Must achieve 0 errors, 0 warnings.
- [x] T046 Verify Swagger UI loads at `/swagger` and shows all endpoints with correct auth requirements.
- [x] T047 Run quickstart.md verification steps: login → create product → list products → test Cashier 403 → test validation 400.
- [x] T048 Fix `System.NullReferenceException` in `Program.cs` by ensuring OpenAPI document transformer correctly handles null components and security collections.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS all user stories**
- **Phase 3–8 (User Stories)**: All depend on Phase 2 completion
  - US1 (Auth) should be done first — other stories need a token to test
  - US2–US6 can proceed in any order after US1
- **Phase 9 (Polish)**: Depends on all stories being complete

### User Story Dependencies

- **US1 (Auth)**: After Phase 2 — no dependencies on other stories. **MVP.**
- **US2 (Products)**: After US1 (needs token for testing) — independent otherwise
- **US3 (Customer/Supplier)**: After US1 — independent of US2
- **US4 (Warehouse)**: After US1 — independent of US2/US3
- **US5 (DocSequence)**: After Phase 2 — no controller needed, internal service only
- **US6 (Categories/Units + Error Polish)**: After US1 — independent

### Within Each User Story

- Interface before Service
- Service before Controller
- Validators can be parallel with Service [P]
- DI registration after Service implementation

### Parallel Opportunities

- T014 + T016 (ProductService interface + validators) — different files
- T019 + T020 (Customer interface + Supplier interface) — different files
- T023 + T024 (Customer validators + Supplier validators) — different files
- T036 + T037 (CategoryService + UnitService) — different files
- T038 + T039 (Category validators + Unit validators) — different files

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T008) — **CRITICAL**
3. Complete Phase 3: US1 Auth (T009–T013)
4. **STOP and VALIDATE**: Login works, JWT issued, protected endpoints reject unauthorized
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 Auth → Login works (MVP!)
3. US2 Products → CRUD works → Demo
4. US3 Customer/Supplier → CRUD works → Demo
5. US4 Warehouse → CRUD works → Demo
6. US5 DocSequence → Internal service ready
7. US6 Categories/Units + Error polish → Full Phase 2 complete
8. Polish → Build clean, Swagger verified

---

## Notes

- **IMPORTANT FOR SMALLER MODELS**: Each task contains exact file paths, class names, method signatures, and implementation details. Follow them precisely.
- [P] tasks = different files, safe to parallelize
- [Story] label maps task to specific user story
- ALL services MUST return `Result<T>` — NEVER throw exceptions
- ALL controllers MUST have `[Authorize]` (except login)
- ALL money/quantity fields MUST use `decimal` — NEVER float/double
- Commit after each task or logical group
- Reference `AGENTS.md` for full Constitution rules if uncertain
