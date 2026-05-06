# Tasks: Backend Core (Phase 2)

## Phase 1: Infrastructure & Repository Pattern
- [ ] T001 Implement `GenericRepository<T>` in `SalesSystem.Infrastructure/Data/Repositories/GenericRepository.cs`
- [ ] T002 Implement `UnitOfWork` in `SalesSystem.Infrastructure/Data/UnitOfWork.cs`
- [ ] T003 Register Repositories and UoW in `SalesSystem.Api/Program.cs`

## Phase 2: Authentication & Security
- [ ] T004 Create `AuthService` in `SalesSystem.Application/Services/AuthService.cs`
- [ ] T005 Implement JWT Token Generation logic in `SalesSystem.Infrastructure/Security/JwtTokenGenerator.cs`
- [ ] T006 Configure JWT Authentication middleware in `SalesSystem.Api/Program.cs`
- [ ] T007 Implement `AuthController` in `SalesSystem.Api/Controllers/AuthController.cs`

## Phase 3: Core Business Services (CRUD)
- [ ] T008 [P] [US2] Implement `ProductService` in `SalesSystem.Application/Services/ProductService.cs`
- [ ] T009 [P] [US2] Implement `CustomerService` in `SalesSystem.Application/Services/CustomerService.cs`
- [ ] T010 [P] [US2] Implement `SupplierService` in `SalesSystem.Application/Services/SupplierService.cs`
- [ ] T011 [P] [US2] Implement `WarehouseService` in `SalesSystem.Application/Services/WarehouseService.cs`

## Phase 4: API Polish & Validation
- [ ] T012 Implement `ExceptionMiddleware` in `SalesSystem.Api/Middleware/ExceptionMiddleware.cs`
- [ ] T013 [P] Add FluentValidation for Product Requests in `SalesSystem.Application/Validators/ProductValidator.cs`
- [ ] T014 Implement standard Controllers for Products, Customers, and Suppliers in `SalesSystem.Api/Controllers/`

## Phase 5: Verification
- [ ] T015 Run final build and verify all 002 endpoints in Swagger
