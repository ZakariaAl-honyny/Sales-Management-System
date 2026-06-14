# Implementation Plan: Phase 2 — Backend Core (ASP.NET Core API Layer)

**Branch**: `002-backend-core` | **Date**: 2026-05-06  
**Input**: Spec from `specs/002-backend-core/spec.md`, research from `research.md`, data model from `data-model.md`
**Dependencies**: Phase 1 Foundation (Domain entities, DbContext, migrations, seed data)

---

## Summary

Build the ASP.NET Core 10 API layer that sits between the WPF desktop client and SQL Server. This phase establishes: JWT authentication with role-based authorization, FluentValidation on all requests, Serilog structured logging, the Result<T> service pattern, rate limiting against brute-force login, global exception middleware (including DB failure detection), health check endpoints, and CORS for the desktop client. Every controller delegates to application services — NO DbContext injection in controllers is allowed. This is the secure, observable gateway for all subsequent feature phases.

---

## Middleware Pipeline

The Program.cs middleware pipeline order is critical. The correct sequence (top to bottom) is:

1. **Serilog request logging** (`UseSerilogRequestLogging()`) — captures method, path, status, and duration for every request. Must be first to log all activity including early failures.
2. **ExceptionMiddleware** — global try/catch wrapping the entire pipeline. Catches unhandled exceptions and returns standardized JSON. Detects database connection failures (via `IsDatabaseConnectionException()` helper checking `InvalidOperationException` with connection string keywords, `SqlException` type name, and inner exception recursion) and returns HTTP 503 with code `DATABASE_CONNECTION_ERROR`. All other exceptions return HTTP 500 with code `INTERNAL_ERROR`. Response body is always `{ "error": "...", "errorCode": "..." }` with content type `application/json`. Exception details are logged at Error level via Serilog — never leaked to the client.
3. **Rate Limiter** (`UseRateLimiter()`) — enforces the LoginPolicy and GlobalLimit BEFORE authentication (per RULE-243, rate limiter middleware must be placed BEFORE `UseAuthentication()`). This prevents brute-force attacks from reaching the auth layer.
4. **CORS** — allows the desktop client origin. Configured with a named policy allowing specific origins, methods, and headers. The WPF desktop app typically runs on `http://localhost:5000` or a configurable port.
5. **Authentication** (`UseAuthentication()`) — validates the JWT bearer token on every request. Extracts claims (sub → UserId, role → UserRole) and sets `HttpContext.User`.
6. **Authorization** (`UseAuthorization()`) — checks `[Authorize]` attributes and role-based policies on controllers/actions.
7. **Swagger** (`UseSwagger()`, `UseSwaggerUI()`) — available in development only. Provides interactive API documentation at `/swagger` with JWT bearer token input.

DI registration order in `Program.cs`:
- Serilog: `builder.Host.UseSerilog()` before anything else
- DbContext + Identity: `AddDbContext<SalesDbContext>()`, `AddAuthentication()`, `AddAuthorization()`
- Application services: `AddScoped<IProductService, ProductService>()`, etc.
- Infrastructure: `AddScoped<IUnitOfWork, UnitOfWork>()`, `AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>))`
- FluentValidation: `AddValidatorsFromAssemblyContaining<Program>()`
- Swagger: `AddEndpointsApiExplorer()` + `AddSwaggerGen()` with JWT security definition
- Rate limiting: `AddRateLimiter()` with two policies
- CORS: `AddCors()` with named policy

---

## Authentication & Authorization

**JWT Token Flow:**
- The `AuthController.Login` endpoint (POST `/api/auth/login`) accepts username + password.
- `AuthService.AuthenticateAsync()` looks up the user by username, verifies password via `BCrypt.Verify()` with work factor 12, and returns `Result<LoginResponse>`.
- On success, `IJwtTokenGenerator.GenerateToken()` creates a JWT with claims: `sub` (UserId), `unique_name` (Username), `role` (role ID as string: "1" for Admin, "2" for Manager, "3" for Cashier). Token expires in 8 hours.
- The login response includes: `token` (string), `expiresAt` (datetime), `user` object with id, username, full name, and role.
- Failed logins return HTTP 401 with generic error message — no hint about which credential field is wrong.
- Deactivated users (`IsActive = false`) also return 401.

**Token Validation:**
- `AddJwtBearer()` validates: issuer signing key (from env var `SALESSYSTEM_JWT_SECRET`), issuer, audience, and expiry.
- Missing or expired tokens → HTTP 401.
- Valid token → `HttpContext.User` populated with claims.
- The JWT signing key is loaded from the `SALESSYSTEM_JWT_SECRET` environment variable. If missing in production, the application throws `InvalidOperationException` on startup — never start with a default or hardcoded key.

**Authorization Policies (three tiers):**
- `AdminOnly`: requires role "1" — for warehouse management, settings, user management, fiscal year operations.
- `ManagerAndAbove`: requires role "1" or "2" — for products, suppliers, purchases, stock transfers, reports.
- `AllStaff`: requires role "1", "2", or "3" — for sales, customers (Cashier can view), cash boxes, currencies.

All controllers and actions carry the `[Authorize]` attribute. Only `AuthController.Login` is unauthenticated (carries `[AllowAnonymous]`). Role-specific actions carry `[Authorize(Policy = "AdminOnly")]` or equivalent.

---

## Result<T> Pattern

Every application service method returns `Result<T>` (success) or `Result` (void success) or `Result<T>.Failure(errorMessage, errorCode)`.

**Controller Translation Contract:**
- Service returns `Result<T>.Success(value)` → Controller returns `Ok(value)` → HTTP 200.
- Service returns `Result.Failure(errorMessage, ErrorCodes.NotFound)` → Controller returns `NotFound(new { error = errorMessage })` → HTTP 404.
- Service returns `Result.Failure(errorMessage, anyOtherCode)` → Controller returns `BadRequest(new { error = errorMessage })` → HTTP 400.
- Service returns `Result.Failure(errorMessage, ErrorCodes.Unauthorized)` → Controller returns `Unauthorized(new { error = errorMessage })` → HTTP 401.
- Services NEVER throw exceptions like `KeyNotFoundException` or `InvalidOperationException` — all error paths return `Result.Failure`.

This pattern (RULE-006 + RULE-025) ensures predictable HTTP semantics: "not found" is 404, "validation/business error" is 400, "credentials invalid" is 401. The `ErrorCodes` class in the Contracts layer defines canonical error codes (e.g., `NotFound`, `DuplicateName`, `DuplicateBarcode`, `ValidationError`, `Unauthorized`, `Forbidden`).

**Controller Purity (RULE-022, RULE-203):**
- Controllers inject ONLY application service interfaces (e.g., `IProductService`).
- Controllers NEVER inject `SalesDbContext`, `IUnitOfWork`, or any infrastructure type.
- Controllers NEVER contain business logic, data queries, or domain calculations.
- Controller methods follow the pattern: receive HTTP request → call service method → translate `Result<T>` to HTTP response.

---

## FluentValidation

Every POST/PUT request model has a dedicated `AbstractValidator<T>` class.

**Validator Design:**
- Located in `SalesSystem.Api/Validators/` following the pattern `{Entity}RequestValidator`.
- Rules enforced: `NotEmpty()` with Arabic error messages for required fields, `MaximumLength()` for string truncation, `GreaterThan(0)` for positive-only numeric fields (quantities, prices), `Matches(@"^05\d{8}$")` for Saudi phone numbers, `.EmailAddress()` for email fields, `InclusiveBetween(1, 3)` for enum values that must be within range.
- Validators register automatically via `AddValidatorsFromAssemblyContaining<Program>()` (FluentValidation.AspNetCore integration).
- Validation failures return HTTP 400 with structured JSON: `{ "errors": { "fieldName": ["error message"] } }` with the `ValidationBehavior` or automatic model validation from `[ApiController]` attribute.

**Validation Middleware Integration:**
The `[ApiController]` attribute automatically validates the model state before the action executes. Custom FluentValidation validators plug into this pipeline. Invalid requests never reach the service layer — they are rejected at the API boundary.

---

## Serilog Logging

**Configuration (in Program.cs):**
- `builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration))`
- Sinks: File (rolling daily to `logs/salessystem-.log`), Console (development only), EventLog (when running as Windows Service).
- Minimum level: Information for file, Warning for EventLog.

**Log Level Separation Policy (RULE-182, RULE-183):**
- `Log.Error` — SYSTEM ERRORS ONLY: database connection failures, JSON parse crashes, file I/O failures, unhandled exceptions caught by ExceptionMiddleware, API unreachable. These are infrastructure-level failures that require administrator attention.
- `Log.Warning` — USER MISTAKES: validation errors from user input, business rule violations (e.g., "المخزون غير كافٍ"), "not found" from user-submitted IDs. These are normal operational events — not system errors.
- `Log.Information` — SIGNIFICANT OPERATIONS: invoice creation/cancellation, stock changes, logins, user creation. Each log includes correlation context (user ID, entity type, entity ID) for audit trail.

**Never Logged (RULE-037):** passwords, connection strings, JWT secrets, or any credential material. Serilog's `Destructure` policies and custom `IOperationFilter` on the auth endpoint prevent password field serialization.

---

## Health Check Endpoints

Two endpoints, both unauthenticated (carry `[AllowAnonymous]`):

**`GET /api/v1/health`:**
Returns overall system health including database connectivity check. Response format:
```json
{
  "status": "OK" | "Degraded",
  "database": "Connected" | "Disconnected",
  "version": "1.0.0",
  "timestamp": "2026-05-06T12:00:00Z"
}
```
The `status` field is "Degraded" when the database is unreachable. The `database` field is determined by calling `DbContext.Database.CanConnectAsync()` wrapped in try/catch.

**`GET /api/v1/health/database`:**
Dedicated database connectivity probe. Returns `{ "status": "connected" }` with HTTP 200 on success, or `{ "status": "disconnected" }` with HTTP 503 on failure. Used by the Desktop's `DatabaseHealthCheckService` at startup (RULE-153).

Both endpoints inject `SalesDbContext` directly — this is the sole exception to controller purity (health checks are infrastructure probes, not business operations).

---

## Rate Limiting

Two rate limiter policies configured via `builder.Services.AddRateLimiter()`:

**LoginPolicy** (RULE-240):
- Applied to `AuthController.Login` via `[EnableRateLimiting("LoginPolicy")]`.
- Limits: 5 requests per 15-minute sliding window per IP address.
- Queue: 0 (no queuing — excess requests are rejected immediately).
- Response: HTTP 429 with Arabic body `{ "error": "محاولات تسجيل دخول كثيرة جداً. يرجى الانتظار 15 دقيقة.", "errorCode": "RATE_LIMIT_EXCEEDED" }`.

**GlobalLimit** (RULE-241):
- Applied globally to all endpoints (including unauthenticated).
- Limits: 100 requests per 1-minute sliding window per IP address.
- Queue: 2 (allows brief bursts).
- Response: HTTP 429 with Arabic message.

**Middleware Placement (RULE-243):** Rate limiter middleware (`UseRateLimiter()`) is placed BEFORE `UseAuthentication()` in the pipeline — the middleware does not require authentication to enforce limits.

---

## CORS Configuration

The API runs as a local HTTP server. The WPF desktop client connects via `HttpClient`. CORS configuration:

- Named policy: `"AllowDesktopClient"`
- Allowed origins: `http://localhost:5221` (development), configurable via `appsettings.json`
- Allowed methods: `GET, POST, PUT, DELETE, PATCH`
- Allowed headers: `Authorization, Content-Type, Accept`
- Supports credentials: `false` (token-based auth, not cookies)
- Preflight (OPTIONS) requests are handled automatically by the CORS middleware

Applied in the middleware pipeline after ExceptionMiddleware and before Authentication.

---

## Tasks Summary

The tasks.md file breaks implementation into 4 phases with 15+ tasks:

**Phase 1 — Setup (Shared Infrastructure)**
- Add NuGet packages to Infrastructure (BCrypt.Net-Next) and API projects (FluentValidation, Serilog, JwtBearer, Swashbuckle)
- Create `JwtSettings` record in Contracts layer

**Phase 2 — Foundational (Blocking Prerequisites)**
- T004: Implement `GenericRepository<T>` in Infrastructure/Data/Repositories
- T005: Implement `UnitOfWork` in Infrastructure/Data with `BeginTransactionAsync` and lazy repository properties
- T006: Implement `JwtTokenGenerator` + `IJwtTokenGenerator` interface
- T007: Implement `ExceptionMiddleware` with DB detection + standardized error response
- T008: Configure `Program.cs` — wire Serilog, DI, JWT auth, authorization policies, FluentValidation, Swagger, middleware pipeline

**Phase 3 — User Story 1 (Staff Login)**
- Implement `AuthService` (authenticate, verify BCrypt, return token)
- Implement `AuthController` with `[AllowAnonymous]` on Login, `[EnableRateLimiting("LoginPolicy")]`
- Add FluentValidation for login request
- Add `UsersController` with Admin-only CRUD

**Phase 4 — User Stories 2-5 (Entity CRUD)**
- Implement `ProductsController`, `CustomersController`, `SuppliersController`, `WarehousesController`, `CategoriesController`, `UnitsController`
- Each follows the same pattern: service → Result<T> → controller translates to HTTP
- FluentValidators per request model
- Soft-delete via `IsActive = false` (global query filter)
- DocumentSequenceService for auto-numbered document IDs (thread-safe via SemaphoreSlim)

**Phase 5 — User Story 6 (Error Handling + Logging)**
- Verify ExceptionMiddleware catches all unhandled exceptions
- Verify Serilog writes to file with correct level separation
- Verify FluentValidation handles field-level errors
- Add HealthController with `/api/v1/health` and `/api/v1/health/database`

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| JWT secret missing in production | Application fails to start | Startup check throws `InvalidOperationException` with clear message. WPF installer validates env var during setup. |
| Concurrent document number requests cause duplicates | Invoice numbering corrupted | `DocumentSequenceService` uses `SemaphoreSlim(1,1)` lock + DB-level unique index on `(Prefix, Year)` — thread-safe by design. |
| Database unreachable after deployment | All API calls fail with 500 | ExceptionMiddleware detects DB exceptions and returns 503 with `DATABASE_CONNECTION_ERROR`. Desktop shows `DatabaseErrorDialog` with retry/exit. |
| Brute-force attack on login endpoint | Account compromise | Rate limiting (5/15min per IP) + account lockout after 5 failed attempts (RULE-307 from Phase 21). |
| Controller accidentally injects DbContext | Architecture violation | Code review checklist explicitly checks controller constructors. RULE-203/204 mandate service-only injection. |
| Swagger exposed in production | Attack surface | `UseSwagger()` / `UseSwaggerUI()` wrapped in `if (app.Environment.IsDevelopment())` block. |
| Large result sets cause memory pressure | Slow responses, OOM | All list endpoints support pagination (page + pageSize query params). Default page size = 50, max = 500. |

---

## Verification Criteria

Prior to accepting Phase 2 as complete, verify:
- [ ] `dotnet build` — 0 errors, 0 warnings across all 6 projects
- [ ] POST `/api/auth/login` with valid credentials returns JWT token
- [ ] POST `/api/auth/login` with invalid credentials returns 401
- [ ] Protected endpoint without JWT returns 401
- [ ] Cashier accessing Admin-only endpoint returns 403
- [ ] Invalid input on any POST/PUT returns 400 with field-level errors
- [ ] Non-existent entity GET returns 404 (not 400)
- [ ] Database down returns 503 with `DATABASE_CONNECTION_ERROR`
- [ ] 5 rapid login attempts trigger 429 rate limit
- [ ] `GET /api/v1/health` returns `{ database: "Connected" }` when DB is up
- [ ] `GET /api/v1/health/database` returns 200 when DB is up
- [ ] All services return `Result<T>` — no exceptions propagate to controllers
- [ ] No controller injects DbContext or IUnitOfWork directly
- [ ] Swagger UI accessible at `/swagger` in development
- [ ] Serilog writes to `logs/salessystem-*.log` with correct level separation
