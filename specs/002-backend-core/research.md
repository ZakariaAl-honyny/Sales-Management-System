# Research: Backend Core (Phase 2)

**Feature**: Backend Core  
**Date**: 2026-05-06

## Research Summary

No NEEDS CLARIFICATION items exist in the Technical Context. All technology decisions are pre-determined by the project Constitution and PRD-MVP-v3.0. This document records the rationale for each key decision.

---

## R1: Repository Pattern Implementation

**Decision**: Implement `GenericRepository<T>` backed by EF Core `DbSet<T>`.

**Rationale**: The `IGenericRepository<T>` interface already exists in the Application layer (Phase 1). It defines `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, and `SoftDeleteAsync`. The implementation wraps EF Core's `DbSet<T>` and respects the global query filter for soft-deleted records.

**Alternatives Considered**:
- **Specific repositories per entity**: Rejected — the CRUD operations are identical across all master data entities. Specialized repositories can be added later for complex queries (e.g., invoice search) without breaking the generic pattern.
- **No repository (direct DbContext)**: Rejected — RULE-024 mandates `IUnitOfWork` for multi-table operations. The repository pattern supports this.

---

## R2: Unit of Work Transaction Wrapper

**Decision**: `UnitOfWork` wraps `SalesDbContext` and exposes typed `IGenericRepository<T>` properties. `BeginTransactionAsync()` returns a wrapper implementing `IDbContextTransaction` (which extends `IAsyncDisposable, IDisposable`).

**Rationale**: The `IUnitOfWork` interface (Phase 1) already defines 7 repository properties + `SaveChangesAsync` + `BeginTransactionAsync`. The implementation lazily creates repository instances and delegates transaction management to EF Core's `Database.BeginTransactionAsync()`.

**Alternatives Considered**:
- **Ambient TransactionScope**: Rejected — requires MSDTC on SQL Server, adds complexity for a single-machine deployment.

---

## R3: JWT Authentication Strategy

**Decision**: JWT Bearer tokens with 8-hour expiry. Role stored as claim. JWT secret stored in environment variable `SALESSYSTEM_JWT_SECRET`.

**Rationale**: PRD §3.1 specifies JWT with 8-hour expiry and role in claims. The Constitution (§VIII) mandates `[Authorize]` on all endpoints except login. BCrypt with work factor 12 for password verification.

**Alternatives Considered**:
- **Cookie-based auth**: Rejected — the desktop client communicates via HttpClient, not a browser. JWT is the standard for API-to-client authentication.
- **API key**: Rejected — does not support per-user roles or session expiry.

---

## R4: Authorization Policies

**Decision**: Three policies defined in `Program.cs`:

| Policy | Roles Allowed | Used For |
|--------|--------------|----------|
| `AdminOnly` | Admin (1) | Warehouses, Users, Settings |
| `ManagerAndAbove` | Admin (1), Manager (2) | Products, Customers, Suppliers, Reports, Purchases |
| `AllStaff` | Admin (1), Manager (2), Cashier (3) | Sales, Sales Returns, Customer view |

**Rationale**: Matches the Permissions Matrix in AGENTS.md §6 exactly.

---

## R5: Service Pattern (Result<T>)

**Decision**: Every service method returns `Result<T>` or `Result`. Controllers translate to HTTP status codes.

**Rationale**: RULE-006 mandates this pattern. The `Result<T>` class already exists in `SalesSystem.Contracts.Common`. Controllers use a helper pattern:
- `result.IsSuccess` → `Ok(result.Value)`
- `result.ErrorCode == ErrorCodes.NotFound` → `NotFound(...)`
- `result.ErrorCode == ErrorCodes.ValidationError` → `BadRequest(...)`
- Default failure → `BadRequest(...)`

---

## R6: Serilog Configuration

**Decision**: Configure Serilog with File sink. Log to `logs/salessystem-.log` with daily rolling. Structured JSON format.

**Rationale**: RULE-035 mandates Serilog. RULE-036 specifies what to log (exceptions, invoice ops, stock changes, logins). RULE-037 specifies what NOT to log (passwords, connection strings).

**Alternatives Considered**:
- **NLog**: Rejected — Serilog is in the approved packages list.
- **Console sink only**: Rejected — file logging is required for production debugging on a local machine.

---

## R7: FluentValidation Strategy

**Decision**: One validator class per Request DTO. Registered via `AddValidatorsFromAssemblyContaining<>` in Program.cs. Automatic validation via ASP.NET Core pipeline.

**Rationale**: RULE-009 (Four-Layer Validation) requires API-layer validation. Request DTOs already exist for all entities. FluentValidation 11.x is in the approved packages list.

---

## R8: Global Exception Middleware

**Decision**: Custom `ExceptionMiddleware` that catches all unhandled exceptions, logs them via Serilog, and returns a standardized JSON response `{ "error": "...", "errorCode": "..." }`.

**Rationale**: Prevents stack traces from leaking to clients. Provides consistent error format for the desktop HttpClient to parse.

---

## R9: DocumentSequenceService Thread Safety

**Decision**: Use `SemaphoreSlim(1,1)` to serialize access to the document sequence table. Lock → read current number → increment → save → release lock.

**Rationale**: RULE-011 mandates `SemaphoreSlim` for thread-safe sequence generation. Even in a single-server scenario, concurrent API requests could race on sequence numbers.

**Alternatives Considered**:
- **Database-level locking (UPDLOCK)**: Viable but adds SQL Server-specific logic. SemaphoreSlim is simpler and sufficient for single-server.
- **GUID-based identifiers**: Rejected — PRD requires human-readable sequential numbers (INV-2026-000001).
