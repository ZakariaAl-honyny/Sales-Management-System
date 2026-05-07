# Implementation Plan: Backend Core

**Branch**: `002-backend-core` | **Date**: 2026-05-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/002-backend-core/spec.md`

## Summary

Phase 2 implements the backend infrastructure layer: Generic Repository, Unit of Work, JWT Authentication, CRUD services for master data entities (Product, Customer, Supplier, Warehouse, Category, Unit), thread-safe document sequence generation, FluentValidation, Serilog logging, and global exception handling. All services follow the `Result<T>` pattern. All endpoints are secured with JWT Bearer + role-based policies.

## Technical Context

**Language/Version**: C# / .NET 10 LTS  
**Primary Dependencies**: ASP.NET Core 10, EF Core 10, BCrypt.Net-Next 4.x, FluentValidation 11.x, Serilog 8.x, Swashbuckle 6.x  
**Storage**: SQL Server 2019+ via EF Core (DbContext + Migrations already in place from Phase 1)  
**Testing**: Manual testing via Swagger UI (automated tests deferred to future phase)  
**Target Platform**: Windows Desktop (local deployment)  
**Project Type**: Desktop-app with local API backend  
**Performance Goals**: CRUD < 500ms, product search < 500ms, invoice save < 2s  
**Constraints**: Single-machine deployment, decimal-only financials, Arabic + English text (nvarchar)  
**Scale/Scope**: Small retail shop, ~50 products typical, ~10k invoices/year

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal-Only Financial Precision | ✅ PASS | All DTOs use `decimal`. No float/double anywhere. |
| II | Domain-Computed Financial Formulas | ✅ PASS | Phase 2 services do NOT compute financials — entities already do. |
| III | Transactional Integrity | ✅ PASS | `IUnitOfWork.BeginTransactionAsync()` already defined. UoW implementation will wrap EF Core transactions. |
| IV | Invoice Lifecycle State Machine | ⬜ N/A | No invoice services in Phase 2 (Phase 3 scope). |
| V | Stock Integrity | ⬜ N/A | No stock operations in Phase 2 (Phase 3 scope). |
| VI | Result Pattern | ✅ PASS | `Result<T>` exists in Contracts. All services will return it. |
| VII | Clean Architecture Boundaries | ✅ PASS | Desktop → HttpClient → API → Application → Infrastructure → SQL Server. Domain has zero dependencies. |
| VIII | Security | ✅ PASS | JWT Bearer + BCrypt (factor 12) + env var connection string + `[Authorize]` on all endpoints. |
| IX | Four-Layer Validation | ✅ PASS | FluentValidation (API) + Service pre-conditions (Application) + Entity rules (Domain) + CHECK constraints (DB). |
| X | Logging Standard | ✅ PASS | Serilog configured. Console.WriteLine forbidden. |
| XI | EF Core Conventions | ✅ PASS | Fluent API only, Restrict on all FKs, global query filter for soft delete. Already in Phase 1 configs. |
| XII | Audit Trail | ✅ PASS | `CreatedByUserId` FK on all invoice/financial tables. Users soft-delete only. |

**Gate Result**: ✅ ALL PASSED — No violations.

## Project Structure

### Documentation (this feature)

```text
specs/002-backend-core/
├── plan.md              # This file
├── research.md          # Phase 0: Technology decisions
├── data-model.md        # Phase 1: Entity mapping for services
├── quickstart.md        # Phase 1: How to verify the feature
├── contracts/           # Phase 1: API endpoint contracts
│   └── api-endpoints.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (via /speckit-tasks)
```

### Source Code (repository root)

```text
SalesSystem/
├── SalesSystem.Contracts/
│   ├── Common/              ← Result<T>, PagedResult<T>, ErrorCodes (EXISTS)
│   ├── DTOs/                ← All DTOs (EXISTS)
│   ├── Requests/            ← Request models per entity (EXISTS)
│   └── Responses/           ← LoginResponse (EXISTS)
│
├── SalesSystem.Domain/
│   ├── Common/              ← BaseEntity (EXISTS)
│   ├── Entities/            ← All 20 entities (EXISTS)
│   ├── Enums/               ← UserRole, InvoiceStatus, etc. (EXISTS)
│   └── Exceptions/          ← DomainException (EXISTS)
│
├── SalesSystem.Application/
│   ├── Interfaces/
│   │   ├── IUnitOfWork.cs              (EXISTS)
│   │   ├── Repositories/
│   │   │   └── IGenericRepository.cs   (EXISTS)
│   │   └── Services/                   (NEW)
│   │       ├── IAuthService.cs
│   │       ├── IProductService.cs
│   │       ├── ICustomerService.cs
│   │       ├── ISupplierService.cs
│   │       ├── IWarehouseService.cs
│   │       ├── ICategoryService.cs
│   │       ├── IUnitService.cs
│   │       └── IDocumentSequenceService.cs
│   └── Services/                       (NEW)
│       ├── AuthService.cs
│       ├── ProductService.cs
│       ├── CustomerService.cs
│       ├── SupplierService.cs
│       ├── WarehouseService.cs
│       ├── CategoryService.cs
│       ├── UnitService.cs
│       └── DocumentSequenceService.cs
│
├── SalesSystem.Infrastructure/
│   ├── Data/
│   │   ├── SalesDbContext.cs           (EXISTS)
│   │   ├── Configurations/            (EXISTS)
│   │   ├── Repositories/              (NEW)
│   │   │   └── GenericRepository.cs
│   │   └── UnitOfWork.cs              (NEW)
│   └── Security/                      (NEW)
│       └── JwtTokenGenerator.cs
│
├── SalesSystem.Api/
│   ├── Controllers/                   (NEW)
│   │   ├── AuthController.cs
│   │   ├── ProductsController.cs
│   │   ├── CustomersController.cs
│   │   ├── SuppliersController.cs
│   │   ├── WarehousesController.cs
│   │   ├── CategoriesController.cs
│   │   └── UnitsController.cs
│   ├── Middleware/                     (NEW)
│   │   └── ExceptionMiddleware.cs
│   ├── Validators/                    (NEW)
│   │   ├── LoginRequestValidator.cs
│   │   ├── ProductRequestValidators.cs
│   │   ├── CustomerRequestValidators.cs
│   │   ├── SupplierRequestValidators.cs
│   │   ├── WarehouseRequestValidators.cs
│   │   ├── CategoryRequestValidators.cs
│   │   └── UnitRequestValidators.cs
│   └── Program.cs                     (MODIFY — add DI, JWT, Serilog, Swagger)
│
└── SalesSystem.Desktop/               (NOT TOUCHED in Phase 2)
```

**Structure Decision**: The existing 6-project Clean Architecture is preserved. Phase 2 adds implementation files within existing projects — no new projects needed.

## Complexity Tracking

> No Constitution violations to justify. All gates passed.
