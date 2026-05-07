# Feature Specification: Backend Core

**Feature Branch**: `002-backend-core`  
**Created**: 2026-05-06  
**Status**: Draft  
**Input**: User description: "Phase 2 — Backend Core" (PRD-MVP-v3.0 §7 Phase 2)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Staff Login and Secure Access (Priority: P1)

A shop employee opens the desktop application, enters their username and password, and is authenticated. The system issues a secure token that determines which features the employee can access based on their role (Admin, Manager, or Cashier). All subsequent API calls use this token; unauthorized access is rejected.

**Why this priority**: Without authentication, no other feature can be accessed. This is the security gate for the entire system.

**Independent Test**: Can be fully tested by calling `/api/auth/login` with valid and invalid credentials and verifying that a token is returned or a 401 is returned respectively.

**Acceptance Scenarios**:

1. **Given** a registered Admin user with valid credentials, **When** the user submits their username and password to the login endpoint, **Then** the system returns a JWT token containing their role claim, the token expires in 8 hours, and user details are included in the response.
2. **Given** an invalid username or wrong password, **When** the user submits login credentials, **Then** the system returns 401 Unauthorized with a generic error message (no hint about which field is wrong).
3. **Given** a deactivated user (IsActive = false), **When** the user attempts to log in, **Then** the system returns 401 Unauthorized.
4. **Given** a valid JWT token for a Cashier, **When** the Cashier attempts to access an Admin-only endpoint (e.g., Warehouse management), **Then** the system returns 403 Forbidden.
5. **Given** an expired or missing JWT token, **When** any protected endpoint is called, **Then** the system returns 401 Unauthorized.

---

### User Story 2 - Product Catalog Management (Priority: P2)

A Manager logs in and manages the product catalog — creating new products, editing existing ones, searching by name/code/barcode, filtering by category, and deactivating products that are no longer sold. Products are never hard-deleted.

**Why this priority**: Products are the foundation entity for all invoices. Without products, no sales or purchases can happen. ProductService also serves as the template pattern for all other CRUD services.

**Independent Test**: Can be fully tested by creating, reading, updating, and soft-deleting products via the Products API, then verifying search and filter behavior.

**Acceptance Scenarios**:

1. **Given** a Manager is authenticated, **When** they submit valid product data (name, category, unit, purchase price, sale price), **Then** the product is persisted and returned with a system-generated ID.
2. **Given** a product with a duplicate Code or Barcode, **When** creation is attempted, **Then** the system rejects with a clear validation error.
3. **Given** existing products, **When** a Manager searches by partial name, **Then** matching products are returned.
4. **Given** an active product, **When** a Manager deactivates it, **Then** the product's `IsActive` becomes false and it no longer appears in normal queries.
5. **Given** a Cashier is authenticated, **When** they attempt to create or edit a product, **Then** the system returns 403 Forbidden.

---

### User Story 3 - Customer and Supplier Management (Priority: P3)

A Manager manages customer and supplier records — creating, editing, searching, and deactivating them. Each customer/supplier has a running balance that tracks how much is owed. A default "Cash Customer" (عميل نقدي) is pre-seeded for walk-in sales.

**Why this priority**: Customers and suppliers are required for invoicing. Their balance tracking is critical for credit operations.

**Independent Test**: Can be fully tested by performing CRUD operations on the Customers and Suppliers API, verifying balance fields initialize correctly, and confirming soft-delete behavior.

**Acceptance Scenarios**:

1. **Given** a Manager is authenticated, **When** they create a new customer with name, phone, and opening balance, **Then** the customer is persisted with `CurrentBalance` equal to `OpeningBalance`.
2. **Given** a Manager is authenticated, **When** they create a new supplier, **Then** the supplier is persisted with correct balance direction (positive = we owe supplier).
3. **Given** a Cashier is authenticated, **When** they request the customer list, **Then** they can view customers but cannot create, edit, or delete.
4. **Given** an existing customer, **When** deactivation is requested, **Then** the customer's `IsActive` becomes false and they no longer appear in default queries.

---

### User Story 4 - Warehouse Management (Priority: P4)

An Admin manages warehouse locations — creating, editing, and deactivating warehouses. Only one warehouse can be marked as the default. Warehouse management is restricted to Admin users only.

**Why this priority**: Warehouses are required for stock operations and invoicing, but their management is simpler and less frequently used than products or customers.

**Independent Test**: Can be fully tested by performing CRUD on the Warehouses API and verifying the single-default constraint.

**Acceptance Scenarios**:

1. **Given** an Admin is authenticated, **When** they create a new warehouse, **Then** the warehouse is persisted.
2. **Given** a warehouse is marked as default, **When** another warehouse is set as default, **Then** the previous default is unset and only one default remains.
3. **Given** a Manager is authenticated, **When** they attempt to create or modify a warehouse, **Then** the system returns 403 Forbidden.

---

### User Story 5 - Document Sequence Generation (Priority: P5)

When any service creates an invoice (sales, purchase, return, transfer, or payment), the system auto-generates a unique sequential document number in the correct format (e.g., INV-2026-000001). The number generation is thread-safe to prevent duplicates under concurrent access.

**Why this priority**: Document numbering is a supporting service needed by all invoice-related features in Phase 3. Building it now avoids blocking later.

**Independent Test**: Can be tested by calling the sequence generator multiple times concurrently and verifying all numbers are unique and sequential.

**Acceptance Scenarios**:

1. **Given** the current year is 2026 and the last invoice number was INV-2026-000005, **When** a new sales invoice number is requested, **Then** the system returns INV-2026-000006.
2. **Given** two requests arrive simultaneously, **When** both request a new number for the same prefix, **Then** each receives a unique sequential number with no gaps or duplicates.
3. **Given** a new calendar year begins, **When** a new invoice number is requested, **Then** the sequence resets to 000001 for the new year.

---

### User Story 6 - Structured Error Handling and Request Logging (Priority: P6)

All API errors are caught by a global middleware that returns a consistent JSON error response. All incoming requests and significant operations are logged using structured logging. Passwords and connection strings are never logged.

**Why this priority**: Cross-cutting concern that improves debugging and operational visibility. Lower priority because the system can function without it, but it significantly improves quality.

**Independent Test**: Can be tested by triggering invalid requests and unhandled exceptions, then verifying the response format and log output.

**Acceptance Scenarios**:

1. **Given** a request with invalid data, **When** FluentValidation rejects it, **Then** the API returns 400 with a structured JSON body listing field-level errors.
2. **Given** an unhandled exception occurs in a service, **When** the middleware catches it, **Then** a 500 response with a generic error message is returned and the full exception is logged.
3. **Given** any API request, **When** it is processed, **Then** the request method, path, and response status are logged via Serilog.
4. **Given** a login request, **When** it is logged, **Then** the password field is NOT present in the log output.

---

### Edge Cases

- What happens when a product code/barcode that already exists is submitted? → Rejected with a clear validation error indicating the duplicate field.
- What happens when attempting to deactivate the last Admin user? → Rejected to prevent lockout.
- What happens when the JWT signing key is missing from environment variables? → Application fails to start with a clear error message.
- How does the system handle concurrent document sequence requests? → Thread-safe via `SemaphoreSlim` locking in `DocumentSequenceService`.
- What happens when the database is unreachable? → Global middleware catches the exception and returns a 503 Service Unavailable.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST implement a generic repository pattern (`GenericRepository<T>`) providing standard CRUD operations for all domain entities.
- **FR-002**: System MUST implement the Unit of Work pattern (`IUnitOfWork`) to manage transactions across multiple repositories within a single database operation.
- **FR-003**: System MUST authenticate users via username and password, issuing a JWT token valid for 8 hours upon successful login.
- **FR-004**: System MUST enforce role-based authorization with three policies: `AdminOnly` (Admin), `ManagerAndAbove` (Admin + Manager), and `AllStaff` (all roles).
- **FR-005**: System MUST implement CRUD services for Product, Customer, Supplier, and Warehouse entities, all returning `Result<T>`.
- **FR-006**: System MUST implement FluentValidation validators for all API request models, rejecting invalid input with field-level error details.
- **FR-007**: System MUST implement a global exception middleware that catches unhandled errors and returns standardized JSON error responses.
- **FR-008**: System MUST implement a thread-safe `DocumentSequenceService` that generates unique, sequential document numbers per document type per year.
- **FR-009**: System MUST log all critical operations (logins, exceptions, data changes) using Serilog to file — `Console.WriteLine` is forbidden.
- **FR-010**: System MUST hash passwords using BCrypt with work factor 12.
- **FR-011**: System MUST soft-delete entities (set `IsActive = false`) rather than hard-deleting — especially for Users who are referenced by invoice FKs.
- **FR-012**: System MUST validate JWT tokens on every protected request and reject expired or invalid tokens with 401.
- **FR-013**: System MUST use EF Core global query filters to automatically exclude soft-deleted records from normal queries.
- **FR-014**: All service methods MUST return `Result<T>` — raw exceptions MUST NOT propagate to controllers.

### Key Entities

- **User**: System user with username, hashed password, full name, and role (Admin/Manager/Cashier). Soft-delete only.
- **Product**: Catalog item with code, barcode, name, category, unit, purchase price, sale price, and minimum stock threshold. Searchable by name/code/barcode.
- **Category**: Product grouping with name and description. Supports hierarchy through parent reference.
- **Unit**: Measurement unit (e.g., piece, kg, liter) with name and symbol.
- **Customer**: Business contact who purchases from the shop. Tracks opening and current balance. Positive balance = customer owes us.
- **Supplier**: Business contact who supplies goods. Tracks opening and current balance. Positive balance = we owe supplier.
- **Warehouse**: Physical storage location. One marked as default. Stock quantities tracked per product per warehouse.
- **DocumentSequence**: Auto-increment counter per document prefix (INV, PUR, SR, PR, TRF, CP, SP) per year.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All CRUD endpoints respond successfully within 500ms under normal load.
- **SC-002**: 100% of protected endpoints reject unauthenticated requests with 401.
- **SC-003**: 100% of role-restricted endpoints reject unauthorized roles with 403.
- **SC-004**: Invalid input on any endpoint returns a structured 400 response listing specific field errors.
- **SC-005**: Concurrent document number generation produces zero duplicates across 100 simultaneous requests.
- **SC-006**: Deactivated records do not appear in standard list/search queries.
- **SC-007**: All unhandled exceptions are logged with stack traces and return generic 500 responses (no internal details leaked to client).
- **SC-008**: Solution builds with 0 errors and 0 warnings.
- **SC-009**: All Swagger-documented endpoints are functional and testable via the Swagger UI.

## Assumptions

- Phase 1 Foundation is complete: all domain entities, database schema, EF Core configurations, and seed data are in place.
- The database connection string is configured via the `SALESSYSTEM_DB_CONNECTION` environment variable.
- JWT signing key is configured via environment variables.
- The system runs on a single machine (no distributed deployment concerns for Phase 2).
- Refresh token implementation follows standard JWT practices with secure in-memory storage on the desktop client.
- The ProductService serves as the template pattern — once completed, other entity services follow the same structure.
