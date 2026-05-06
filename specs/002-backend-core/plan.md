# Implementation Plan - Backend Core (Phase 2)

**Feature**: Backend Core
**Branch**: `002-backend-core`

## Technical Context

- **Framework**: .NET 10 LTS
- **Patterns**: Generic Repository, Unit of Work, Result Pattern, JWT Auth
- **Security**: BCrypt hashing, JWT (8-hour tokens)
- **Error Handling**: Global Middleware with standardized JSON response

## Implementation Phases

### Phase 1: Infrastructure Layer - Repositories
1. Implement `GenericRepository<T>` in `Infrastructure/Data/Repositories`.
2. Implement `UnitOfWork` in `Infrastructure/Data`.
3. Register services in `Api/Program.cs`.

### Phase 2: Application Layer - Authentication
1. Implement `AuthService` with Login/Register logic.
2. Implement JWT token generator.
3. Configure JWT Bearer Authentication in `Api/Program.cs`.

### Phase 3: Application Layer - Core Services
1. Implement `BaseService<T>` for CRUD operations.
2. Implement specific services (ProductService, CustomerService, etc.).
3. Add `FluentValidation` for all Request DTOs.

### Phase 4: Api Layer - Controllers & Middleware
1. Implement `ExceptionMiddleware`.
2. Implement Base `ApiController` with common helpers.
3. Implement specific Controllers (AuthController, ProductsController, etc.).

## Constitution Check (Gates)

- [ ] **RULE-006**: Service methods return `Result<T>`?
- [ ] **RULE-024**: Using `IUnitOfWork` for operations?
- [ ] **RULE-038**: `[Authorize]` applied?
- [ ] **RULE-039**: BCrypt factor 12 used?
- [ ] **RULE-025**: Result-to-HTTP translation in controllers?

## Success Criteria

- Clean architecture boundaries respected (Domain -> Application -> Infrastructure).
- 100% build pass.
- Standardized error responses.
