<!--
  SYNC IMPORT REPORT
  ==================
  Version change: 1.0.0 → 1.1.0
  Modified principles: N/A (added new principles)
  Added sections:
    - XIII. Delete Strategy (Conditional Soft/Hard Delete)
    - XIV. Defensive Programming & Guard Clauses
    - XV. WPF Interactive Dialogs
    - XVI. Toast Notifications
    - XVII. Real-Time UI Validation
  Removed sections: None
  Templates requiring updates:
    ✅ plan-template.md — Constitutional Check section aligns
    ✅ spec-template.md — Requirements section compatible
    ✅ tasks-template.md — Phase structure compatible
  Follow-up TODOs: None
-->

# Sales Management System Constitution

## Core Principles

### I. Decimal-Only Financial Precision (NON-NEGOTIABLE)

- ALL monetary values MUST use `decimal(18,2)` in C# and SQL Server.
- ALL quantity values MUST use `decimal(18,3)`.
- The types `float`, `double`, `real`, and SQL `money` are FORBIDDEN for
  money or quantity fields under all circumstances.
- **Rationale**: Floating-point arithmetic introduces rounding errors that
  corrupt financial records. Retail systems demand exact precision.

### II. Domain-Computed Financial Formulas (NON-NEGOTIABLE)

- ALL financial calculations MUST execute inside Domain entity methods:
  - `LineTotal = (Quantity * UnitPrice) - DiscountAmount`
  - `SubTotal = Items.Sum(i => i.LineTotal)`
  - `TotalAmount = SubTotal - InvoiceDiscount + TaxAmount`
  - `DueAmount = TotalAmount - PaidAmount`
- `PaidAmount <= TotalAmount` MUST be enforced in Domain AND as a DB
  CHECK constraint.
- Controllers, UI layers, and JavaScript MUST NOT compute these values.
- **Rationale**: Single source of truth for calculations prevents
  discrepancies between layers.

### III. Transactional Integrity (NON-NEGOTIABLE)

- Every operation affecting more than one table MUST run inside
  `BeginTransactionAsync`.
- The 7-step transaction protocol MUST be followed:
  1. Validate ALL preconditions BEFORE opening transaction
  2. `BeginTransactionAsync()`
  3. Save invoice → get ID
  4. Modify stock (using invoice ID as reference)
  5. Create `InventoryMovement` records
  6. Update customer/supplier balance
  7. `CommitAsync()` — on ANY exception: `RollbackAsync()`
- NO partial commits are permitted under any circumstance.
- **Rationale**: Financial data corruption from partial writes is
  unrecoverable in a retail POS context.

### IV. Invoice Lifecycle State Machine

- Invoice states: `Draft=1 → Posted=2 → Cancelled=3`.
- Permitted transitions:
  - Draft → Posted (triggers stock + balance changes)
  - Draft → Cancelled (no stock/balance impact)
  - Posted → Cancelled (MUST reverse stock + balance)
- FORBIDDEN transitions: Posted → Draft, Cancelled → any state.
- NO hard delete for any invoice — EVER.
- NO editing a Posted invoice — cancel and create new instead.
- **Rationale**: Immutable posted invoices preserve audit integrity and
  prevent financial record tampering.

### V. Stock Integrity

- Stock availability MUST be validated BEFORE opening a transaction.
- Stock MUST be deducted AFTER saving the invoice (to have reference ID).
- EVERY stock change MUST create an `InventoryMovement` record with:
  `ProductId`, `WarehouseId`, `MovementType`, `QuantityChange`,
  `QuantityBefore`, `QuantityAfter`, `ReferenceType`, `ReferenceId`.
- Negative quantities MUST be prevented at DB level: `CHECK (Quantity >= 0)`.
- **Rationale**: Complete movement audit trail enables stock reconciliation
  and fraud detection.

### VI. Result Pattern (NO Exceptions to Controllers)

- Every Application Service method MUST return `Result<T>` or `Result`.
- Raw exceptions MUST NOT be exposed to Controllers or UI.
- Controllers MUST translate `Result` to appropriate HTTP status codes.
- **Rationale**: Predictable error handling prevents uncontrolled crashes
  and enables consistent client-side error processing.

### VII. Clean Architecture Boundaries

- Desktop MUST NOT connect to the database directly — only via
  `HttpClient` → API.
- API Controllers MUST NOT contain business logic — delegate to
  Application Services.
- Domain layer MUST have ZERO dependencies on Infrastructure
  (no EF Core, no NuGet packages).
- All multi-table operations MUST use the `IUnitOfWork` pattern.
- **Rationale**: Strict dependency direction enables independent testing,
  future client additions (web, mobile), and prevents coupling.

### VIII. Security

- ALL API endpoints MUST have `[Authorize]` with JWT Bearer
  (except `/api/auth/login`).
- Passwords MUST use BCrypt hash with work factor = 12.
- Connection strings MUST use environment variables or encrypted config —
  NEVER in source code or appsettings.json.
- JWT token storage in Desktop MUST be in-memory only — NEVER persisted.
- Sensitive data (passwords, connection strings) MUST NOT appear in logs.
- **Rationale**: Local deployment does not excuse weak security; insider
  threats and POS malware are real risks.

### IX. Four-Layer Validation

- Validation MUST be enforced at four layers:

| Layer | Responsibility | Example |
|-------|---------------|---------|
| Domain | Business rules in Entity methods | `PaidAmount <= TotalAmount` |
| Application | Pre-conditions in Service | Stock availability |
| API | FluentValidation on Request models | `Quantity > 0` |
| Database | CHECK constraints | `Quantity >= 0` |

- **Rationale**: Defense-in-depth prevents invalid data from any entry
  point.

### X. Logging Standard

- Framework: Serilog — `Console.WriteLine` is FORBIDDEN.
- MUST log: exceptions, invoice creation/cancellation, stock changes,
  login attempts.
- MUST NOT log: passwords, full connection strings, personal data.
- **Rationale**: Structured logging enables operational debugging;
  sensitive data exclusion prevents compliance violations.

### XI. EF Core Conventions

- Configuration style: Fluent API ONLY — DataAnnotations on Domain
  entities are FORBIDDEN.
- Delete behavior: `Restrict` on ALL foreign keys — Cascade is FORBIDDEN.
- Soft delete: Global query filter `IsActive == true`.
- Strings: `nvarchar` with explicit `MaxLength`.
- Decimals: `.HasPrecision(18, 2)` or `.HasPrecision(18, 3)`.
- **Rationale**: Consistent ORM configuration prevents schema drift and
  accidental data loss from cascade deletes.

### XII. Audit Trail

- ALL invoice/financial tables MUST use `CreatedByUserId int FK`
  referencing the Users table.
- The Users table MUST use soft delete only (`IsActive = false`) —
  hard delete is FORBIDDEN.
- **Rationale**: FK integrity on audit fields ensures historical records
  always trace back to a valid user, even after user deactivation.

### XIII. Delete Strategy (v4.2)

- ALL delete operations MUST use the `DeleteStrategy` enum:
  - `Cancel = 0` — Abort operation
  - `Deactivate = 1` — Soft delete (`IsActive = false`)
  - `Permanent = 2` — Hard delete (physical removal if not referenced)
- Soft delete preserves entity history for audit trails.
- Hard delete MUST validate references before removal:
  - Product → Check SalesInvoiceLines, PurchaseInvoiceLines
  - Category → Check Products
  - Unit → Check Products (UnitId, RetailUnitId, WholesaleUnitId)
  - Warehouse → Check WarehouseStocks, StockTransfers
  - Customer → Check SalesInvoices, CustomerPayments
  - Supplier → Check PurchaseInvoices, SupplierPayments
  - User → Check not last Admin
- **Rationale**: Conditional delete gives users flexibility while preserving
  financial audit trails.

### XIV. Defensive Programming & Guard Clauses

- ALL Domain entity constructors/factories MUST have Guard Clauses.
- Exception type: `DomainException` — `ArgumentException` in Domain is FORBIDDEN.
- Guard Clause Examples:
```csharp
if (string.IsNullOrWhiteSpace(name))
    throw new DomainException("الاسم مطلوب");
if (price < 0)
    throw new DomainException("السعر لا يمكن أن يكون سالباً");
if (quantity <= 0)
    throw new DomainException("الكمية يجب أن تكون أكبر من الصفر");
```
- **Rationale**: Invalid state prevention at the source eliminates
  downstream bugs and improves error messages.

### XV. WPF Interactive Dialogs

- ALL user-facing messages MUST use `IDialogService` — raw `MessageBox.Show`
  is FORBIDDEN in ViewModels.
- DialogService Interface:
```csharp
public interface IDialogService
{
    Task ShowErrorAsync(string title, string message);
    Task ShowSuccessAsync(string title, string message);
    Task ShowWarningAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<DeleteStrategy> ShowDeleteConfirmationAsync(string itemDescription);
}
```
- Styled Dialogs (RTL Arabic): Error, Success, Warning, Confirmation,
  DeleteConfirmation (3 buttons)
- **Rationale**: Consistent UI experience with localized error messages.

### XVI. Toast Notifications

- Use `IToastNotificationService` for minor success messages.
- Auto-dismiss: Success/Info = 3 seconds, Error = 5 seconds.
- Toast NOT for critical errors — use DialogService instead.
- **Rationale**: Non-blocking feedback improves UX without interrupting workflow.

### XVII. Real-Time UI Validation

- `ViewModelBase` MUST implement `INotifyDataErrorInfo`.
- Save buttons MUST be disabled via `CanExecute` when `HasErrors = true`.
- Validation Error Messages (Arabic):
  - "الاسم مطلوب"
  - "الكمية يجب أن تكون أكبر من الصفر"
  - "السعر لا يمكن أن يكون سالباً"
  - "يجب اختيار منتج"
- XAML Red Border Style on invalid inputs.
- **Rationale**: Immediate feedback prevents submission of invalid forms.

## Technology Stack & Constraints

| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET | 10 LTS |
| API | ASP.NET Core Web API | 10 |
| Desktop UI | WPF (MVVM) + INotifyDataErrorInfo | .NET 10 |
| Database | SQL Server | 2019+ |
| ORM | Entity Framework Core | 10 |
| Auth | JWT Bearer + BCrypt.Net-Next | 4.x |
| Validation | FluentValidation | 11.x |
| Logging | Serilog + Serilog.Sinks.File | 8.x / 5.x |
| API Docs | Swashbuckle (Swagger) | 6.x |
| Desktop HTTP | IHttpClientFactory + System.Text.Json | 10.x |
| Deployment | Windows Service (API) + Inno Setup (Desktop) | — |

### Enums (Canonical Values)

```csharp
public enum UserRole : byte { Admin = 1, Manager = 2, Cashier = 3 }
public enum InvoiceStatus : byte { Draft = 1, Posted = 2, Cancelled = 3 }
public enum PaymentType : byte { Cash = 1, Credit = 2, Mixed = 3 }
public enum MovementType : byte
{
    PurchaseIn = 1, SaleOut = 2, SaleReturnIn = 3,
    PurchaseReturnOut = 4, TransferOut = 5, TransferIn = 6, Adjustment = 7
}
public enum DeleteStrategy { Cancel = 0, Deactivate = 1, Permanent = 2 }
```

### EventBus Rules (Desktop)

- Subscribe in `OnLoad`, unsubscribe in `Dispose(bool disposing)`.
- Marshal handlers to UI thread via `Invoke`/`BeginInvoke`.
- Messages carry entity ID only — NO data payloads.

### Text Encoding

- ALL text columns MUST use `nvarchar` (supports Arabic + English).
- `varchar` is FORBIDDEN.

## Development Workflow & Quality Gates

### Solution Structure (6 Projects)

```text
SalesSystem/
├── SalesSystem.Contracts/       ← DTOs, Requests, Responses, Result<T>
├── SalesSystem.Domain/          ← Entities, Business Rules, Exceptions
├── SalesSystem.Application/     ← Services, Interfaces, Use Cases
├── SalesSystem.Infrastructure/  ← EF Core, DbContext, Repositories
├── SalesSystem.Api/             ← Controllers, FluentValidation, Middleware
└── SalesSystem.DesktopPWF/     ← WPF UI, MVVM, EventBus
```

### Implementation Phases

1. Foundation — Solution structure, Domain entities, Contracts
2. Infrastructure — DbContext, Fluent API configs, Migrations, Repositories
3. Business Logic — Sales, Purchase, Inventory, Return Services
4. API & Desktop Shell — Controllers, Auth, Navigation, EventBus
5. Desktop Modules — Products, Customers, Sales, Purchases, Returns
6. Printing — A4 Invoices, 80mm Thermal Receipts
7. Production — Backup, Windows Service, Installer

### Pre-Submission Checklist (ALL MUST PASS)

- [ ] All money fields = `decimal` (not float/double)
- [ ] All quantities = `decimal` (not int)
- [ ] Financial calculations in Domain only
- [ ] Multi-table operations in a transaction
- [ ] Stock checked BEFORE transaction opens
- [ ] InventoryMovement created for every stock change
- [ ] Service returns `Result<T>` (no raw exceptions)
- [ ] Controller has `[Authorize]`
- [ ] FluentValidation validator exists for Request model
- [ ] Fluent API config (no DataAnnotations on entities)
- [ ] All FKs use `DeleteBehavior.Restrict`
- [ ] Serilog logs critical operations
- [ ] EventBus: subscribe in OnLoad, unsubscribe in Dispose
- [ ] Users soft-deleted only (never hard delete)

### Delete & Defensive Programming
- [ ] Delete uses DeleteStrategy enum (not MessageBox)?
- [ ] Guard Clauses exist for all entity constructors?
- [ ] DomainException used (not ArgumentException)?
- [ ] Permanent delete checks references before removal?

### WPF UI
- [ ] DialogService used (not raw MessageBox)?
- [ ] Toast notifications for minor success messages?
- [ ] INotifyDataErrorInfo with HasErrors property?
- [ ] Save buttons disabled when form has errors?
- [ ] Red border styles on invalid fields?

## Governance

- This Constitution is the supreme authority for the Sales Management
  System. It supersedes ALL other documents, instructions, and practices
  when conflicts arise.
- The companion files `AGENTS.md` and `docs/CONSTITUTION.md` MUST remain
  synchronized with this document.
- **Amendment procedure**: Any principle change requires documentation in
  this file with a version bump and updated `LAST_AMENDED_DATE`.
- **Versioning policy**: MAJOR for principle removals/redefinitions, MINOR
  for new principles or expanded guidance, PATCH for clarifications.
- **Compliance review**: Every code submission MUST pass the Pre-Submission
  Checklist above. The `code-reviewer` agent enforces this at merge time.
- **Cross-reference**: See `AGENTS.md` §2 (Constitution Rules),
  `docs/database-schema.md` (SQL schema), `docs/PRD-MVP.md`
  (full requirements).

**Version**: 1.1.0 | **Ratified**: 2026-05-06 | **Last Amended**: 2026-05-20
