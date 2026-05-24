---
description: "Task list for Cash Boxes (v4.3)"
---

# Tasks: Cash Boxes (v4.3)

**Input**: `specs/009-cash-boxes/`
**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/ ✅

> **Implementation Note**: Each task includes exact file paths and class names so it can be executed by any model without additional context. Tasks marked [P] can run in parallel with other [P]-marked tasks in the same phase.

---

## Phase 1: Setup

- [X] T001 Verify solution builds with 0 errors: `dotnet build SalesSystem/SalesSystem.slnx` — FILE: `SalesSystem/SalesSystem.slnx`

---

## Phase 2: Foundational (BLOCKS all user stories)

**⚠️ Complete and verify before any Phase 3+ work.**

- [X] T002 Add `CashTransactionType` enum: `public enum CashTransactionType : byte { OpeningBalance = 1, SalesIncome = 2, Expense = 3, TransferOut = 4, TransferIn = 5, RefundOut = 6, SupplierPayment = 7, CustomerPayment = 8 }` — FILE: `SalesSystem/SalesSystem.Domain/Enums/CashTransactionType.cs` — RULE-082

- [X] T003 [P] Create `CashBox` entity in Domain. Properties: `Id (int)`, `Name (nvarchar 100)`, `OpeningBalance (decimal 18,2)`, `IsActive (bool)`, `CreatedAt (datetime2)`, `CreatedByUserId (int)`. Constructor guard clauses via `DomainException`: name not empty, openingBalance >= 0. Navigation: `ICollection<CashTransaction> Transactions`. NO `CurrentBalance` property stored — RULE-077, RULE-052 — FILE: `SalesSystem/SalesSystem.Domain/Entities/CashBox.cs`

- [X] T004 [P] Create `CashTransaction` entity. Properties: `Id (int)`, `CashBoxId (int)`, `TransactionType (CashTransactionType)`, `Amount (decimal 18,2)`, `BalanceBefore (decimal 18,2)`, `BalanceAfter (decimal 18,2)`, `ReferenceType (nvarchar 50, nullable)`, `ReferenceId (int?, nullable)`, `Notes (nvarchar 255, nullable)`, `CreatedAt (datetime2)`, `CreatedByUserId (int)`. Constructor guard: amount > 0. All properties `private set` — immutable after construction — RULE-082 — FILE: `SalesSystem/SalesSystem.Domain/Entities/CashTransaction.cs`

- [X] T005 [P] Create `DailyClosure` entity. Properties: `Id (int)`, `CashBoxId (int)`, `ClosureDate (DateOnly)`, `OpeningBalance (decimal 18,2)`, `TotalIncome (decimal 18,2)`, `TotalExpense (decimal 18,2)`, `ClosingBalance (decimal 18,2)`, `ClosedByUserId (int)`, `CreatedAt (datetime2)`. All `private set` — immutable. Guard: closingBalance == openingBalance + totalIncome - totalExpense else throw `DomainException` — RULE-083, RULE-052 — FILE: `SalesSystem/SalesSystem.Domain/Entities/DailyClosure.cs`

- [X] T006 [P] Add Response DTOs to Contracts project:
  - `CashBoxDto { Id, Name, OpeningBalance, CurrentBalance, IsActive, CreatedAt }`
  - `CashTransactionDto { Id, CashBoxId, TransactionType, TransactionTypeName, Amount, BalanceBefore, BalanceAfter, ReferenceType, ReferenceId, Notes, CreatedAt, CreatedByUserName }`
  - `DailyClosureDto { Id, CashBoxId, ClosureDate, OpeningBalance, TotalIncome, TotalExpense, ClosingBalance, ClosedByUserName, CreatedAt }`
  — FILE: `SalesSystem/SalesSystem.Contracts/Responses/CashBoxDto.cs`, `CashTransactionDto.cs`, `DailyClosureDto.cs`

- [X] T007 [P] Add Request DTOs to Contracts project:
  - `CreateCashBoxRequest { Name, OpeningBalance }`
  - `AddCashTransactionRequest { TransactionType, Amount, Notes }`
  - `CashTransferRequest { SourceCashBoxId, DestinationCashBoxId, Amount, Notes }`
  — FILE: `SalesSystem/SalesSystem.Contracts/Requests/CreateCashBoxRequest.cs`, `AddCashTransactionRequest.cs`, `CashTransferRequest.cs`

- [X] T008 Add EF Core Fluent API configurations (no DataAnnotations):
  - `CashBoxConfiguration`: table `CashBoxes`, `Name` nvarchar(100) required, `OpeningBalance` HasPrecision(18,2), FK to Users (CreatedByUserId) Restrict, global query filter `IsActive == true`
  - `CashTransactionConfiguration`: table `CashTransactions`, `Amount/BalanceBefore/BalanceAfter` HasPrecision(18,2), `TransactionType` stored as byte, `ReferenceType` nvarchar(50), `Notes` nvarchar(255), FK to CashBoxes Restrict, FK to Users Restrict, index `IX_CashTransactions_CashBoxId_CreatedAt`, DB CHECK constraint `Amount > 0`
  - `DailyClosureConfiguration`: table `DailyClosures`, all decimal fields HasPrecision(18,2), FK to CashBoxes Restrict, FK to Users Restrict, UNIQUE INDEX `IX_DailyClosures_CashBoxId_ClosureDate`
  — FILE: `SalesSystem/SalesSystem.Infrastructure/Data/Configurations/CashBoxConfiguration.cs`, `CashTransactionConfiguration.cs`, `DailyClosureConfiguration.cs` — RULE-016, RULE-008

- [X] T009 Add `DbSet<CashBox> CashBoxes`, `DbSet<CashTransaction> CashTransactions`, `DbSet<DailyClosure> DailyClosures` to `SalesDbContext` and register the three new configurations in `OnModelCreating` — FILE: `SalesSystem/SalesSystem.Infrastructure/Data/SalesDbContext.cs`

- [X] T010 Add `CashBoxId int? FK → CashBoxes` (nullable, DeleteBehavior.Restrict) to `SalesInvoice` entity and EF config. Add same to `PurchaseInvoice`. Nullable because credit invoices (PaidAmount=0) have no cash box — FILE: `SalesSystem/SalesSystem.Domain/Entities/SalesInvoice.cs`, `PurchaseInvoice.cs` and their EF configs

- [X] T011 [P] Add `ICashBoxService` interface to Application layer with methods:
  - `Task<Result<List<CashBoxDto>>> GetAllAsync(CancellationToken ct)`
  - `Task<Result<CashBoxDto>> CreateAsync(CreateCashBoxRequest req, int userId, CancellationToken ct)`
  - `Task<Result> DeactivateAsync(int id, CancellationToken ct)`
  - `Task<Result<List<CashTransactionDto>>> GetTransactionsAsync(int cashBoxId, DateOnly? from, DateOnly? to, CancellationToken ct)`
  - `Task<Result<CashTransactionDto>> RecordExpenseAsync(int cashBoxId, AddCashTransactionRequest req, int userId, CancellationToken ct)`
  - `Task<Result> TransferAsync(CashTransferRequest req, int userId, CancellationToken ct)`
  - `Task<Result<DailyClosureDto>> PerformDailyClosureAsync(int cashBoxId, int userId, CancellationToken ct)`
  - `Task<Result<List<DailyClosureDto>>> GetDailyClosuresAsync(int cashBoxId, CancellationToken ct)`
  - `Task<Result<CashTransactionDto>> RecordInvoicePaymentAsync(int cashBoxId, decimal amount, CashTransactionType type, string referenceType, int referenceId, int userId, CancellationToken ct)`
  — FILE: `SalesSystem/SalesSystem.Application/Interfaces/Services/ICashBoxService.cs` — RULE-006

- [X] T012 Create EF Core migration: `dotnet ef migrations add AddCashBoxes --project SalesSystem/SalesSystem.Infrastructure --startup-project SalesSystem/SalesSystem.Api`. Verify generated SQL: decimal columns correct precision, CHECK (Amount > 0), UNIQUE index on DailyClosures, nullable CashBoxId on SalesInvoices/PurchaseInvoices — FILE: `SalesSystem/SalesSystem.Infrastructure/Migrations/`

**Checkpoint**: `dotnet build` passes. Migration runs. Three new tables created in DB.

---

## Phase 3: US1 — Cash Box Setup & Balance Tracking (Priority: P1)

**Goal**: Admins can create cash boxes and record manual expenses. Balance is always computed from transaction sum, never stored.

**Independent Test**: Create cash box (OpeningBalance=500) → POST Expense 50 → GET balance: must equal 450. Balance computed from sum of transactions.

- [X] T013 [US1] Implement `CashBoxService` class (implements `ICashBoxService`). Inject `IUnitOfWork`, `ILogger<CashBoxService>`. Key logic for `CreateAsync`: create `CashBox` entity, then create an `OpeningBalance` CashTransaction (amount = openingBalance, BalanceBefore = 0, BalanceAfter = openingBalance) inside `BeginTransactionAsync`. `ComputeBalance` helper: sum credit types minus sum debit types from transactions. Log all writes via Serilog — FILE: `SalesSystem/SalesSystem.Application/Services/CashBoxService.cs` — RULE-077, RULE-003, RULE-035

- [X] T014 [P] [US1] Add `CreateCashBoxRequestValidator`: `Name` NotEmpty MaxLength(100); `OpeningBalance` GreaterThanOrEqualTo(0) — FILE: `SalesSystem/SalesSystem.Api/Validators/CreateCashBoxRequestValidator.cs` — RULE-044

- [X] T015 [P] [US1] Add `AddCashTransactionRequestValidator`: `Amount` GreaterThan(0); `TransactionType` must equal `Expense (3)` only (manual endpoint only allows expenses) — FILE: `SalesSystem/SalesSystem.Api/Validators/AddCashTransactionRequestValidator.cs` — RULE-044

- [X] T016 [US1] Create `CashBoxesController` with `[Authorize]` and `[Route("api/v1/cash-boxes")]`. Inject `ICashBoxService`. Implement:
  - `GET /` → `GetAllAsync` → 200
  - `POST /` → validate `CreateCashBoxRequest`, `CreateAsync` → 201/400
  - `DELETE /{id}` → `DeactivateAsync` → 200/404
  - `GET /{id}/transactions` → accepts `?from=&to=` query params → `GetTransactionsAsync` → 200
  - `POST /{id}/transactions` → validate `AddCashTransactionRequest`, `RecordExpenseAsync` → 201/400
  — FILE: `SalesSystem/SalesSystem.Api/Controllers/CashBoxesController.cs` — RULE-025, RULE-038

- [X] T017 [P] [US1] Add `ICashBoxApiService` interface and `CashBoxApiService` using `IHttpClientFactory`. Methods matching all `CashBoxesController` endpoints, all returning `Result<T>` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/CashBoxApiService.cs` — RULE-007

- [X] T018 [US1] Build `CashBoxEditorView.xaml` + `CashBoxEditorViewModel.cs`. Fields: `Name*` (TextBox, ToolTip="اسم الصندوق إلزامي"), `OpeningBalance*` (TextBox numeric ≥ 0, ToolTip="الرصيد الافتتاحي"). `SaveCommand` always enabled; `Validate()` shows `ShowWarningAsync` on errors. On success: toast `ShowSuccess("تم إنشاء الصندوق بنجاح")` + publish `CashBoxChangedMessage` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/CashBoxes/CashBoxEditorView.xaml` + `ViewModels/CashBoxes/CashBoxEditorViewModel.cs` — RULE-054, RULE-059

- [X] T019 [US1] Build `CashBoxesListView.xaml` + `CashBoxesListViewModel.cs`. DataGrid columns: Name, OpeningBalance, CurrentBalance, IsActive. Toolbar: "إضافة صندوق" button. Subscribe `CashBoxChangedMessage` on EventBus, unsubscribe in `Dispose()`. Double-click opens `CashBoxTransactionsView` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/CashBoxes/CashBoxesListView.xaml` + `ViewModels/CashBoxes/CashBoxesListViewModel.cs` — RULE-012, RULE-013

- [X] T020 [US1] Build `CashBoxTransactionsView.xaml` + `CashBoxTransactionsViewModel.cs`. Read-only DataGrid: TransactionTypeName, Amount, BalanceBefore, BalanceAfter, ReferenceType, Notes, CreatedAt, CreatedByUserName. Header shows `CurrentBalance` (last BalanceAfter). "تسجيل مصروف" button shows inline dialog for Amount+Notes. Date range filter (from/to) — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/CashBoxes/CashBoxTransactionsView.xaml` + `ViewModels/CashBoxes/CashBoxTransactionsViewModel.cs`

- [X] T021 [US1] Register `ICashBoxService` → `CashBoxService` (Scoped) in Infrastructure DI. Register `ICashBoxApiService` → `CashBoxApiService` (Scoped) and all CashBox ViewModels in `App.xaml.cs` — FILE: `SalesSystem/SalesSystem.Infrastructure/DependencyInjection.cs` + `SalesSystem/SalesSystem.DesktopPWF/App.xaml.cs`

**Checkpoint**: Create cash box → record expense → balance reflects sum. `GET /api/v1/cash-boxes` returns boxes with computed balance.

---

## Phase 4: US2 — Invoice Payment Linked to Cash Box (Priority: P1)

**Goal**: Posting a sales or purchase invoice with PaidAmount > 0 creates a CashTransaction in the selected cash box atomically within the same transaction.

**Independent Test**: POST sales invoice (PaidAmount=300, CashBoxId=1) → CashBox #1 balance +300 → CashTransaction(SalesIncome, 300, ReferenceType="SalesInvoice") exists. Cancel the invoice → offsetting CashTransaction created.

- [X] T022 [US2] Implement `CashBoxService.RecordInvoicePaymentAsync`: compute `currentBalance` (sum transactions), validate balance ≥ amount for debit types (SupplierPayment, RefundOut), create and save `CashTransaction` with `BalanceBefore/After` snapshots. Must be called INSIDE the caller's transaction — no new transaction opened here — FILE: `SalesSystem/SalesSystem.Application/Servn  admin123ices/CashBoxService.cs` — RULE-079, RULE-082
admi
- [X] T023 [US2] In `SalesInvoiceService.PostAsync`: after invoice is saved and has ID (inside transaction), if `invoice.PaidAmount > 0 && invoice.CashBoxId != null`, call `await _cashBoxService.RecordInvoicePaymentAsync(invoice.CashBoxId.Value, invoice.PaidAmount, CashTransactionType.SalesIncome, "SalesInvoice", invoice.Id, userId, ct)`. Log Serilog warning on failure but do NOT rollback — FILE: `SalesSystem/SalesSystem.Application/Services/SalesInvoiceService.cs` — RULE-003, RULE-079

- [X] T024 [US2] In `SalesInvoiceService.CancelAsync`: if invoice had `PaidAmount > 0 && CashBoxId != null`, create offsetting `CashTransaction` of type `SalesIncome` with negative effect by using `RefundOut` type for same amount. Log cancellation via Serilog — FILE: `SalesSystem/SalesSystem.Application/Services/SalesInvoiceService.cs` — RULE-018, RULE-082

- [X] T025 [P] [US2] Apply same pattern as T023+T024 to `PurchaseInvoiceService.PostAsync` and `CancelAsync`. Post → `SupplierPayment` type (debit). Cancel → offsetting `TransferIn` equivalent credit entry — FILE: `SalesSystem/SalesSystem.Application/Services/PurchaseInvoiceService.cs`

- [X] T026 [P] [US2] Add `CashBoxId` field to existing invoice request DTOs and FluentValidation: `CreateSalesInvoiceRequest` and `CreatePurchaseInvoiceRequest` — add nullable `int? CashBoxId`. Validator: if `PaidAmount > 0` then `CashBoxId` must not be null — FILE: `SalesSystem/SalesSystem.Contracts/Requests/` + `SalesSystem/SalesSystem.Api/Validators/`

- [X] T027 [P] [US2] Add CashBox selector ComboBox to `SalesInvoiceEditorView.xaml` and `PurchaseInvoiceEditorView.xaml`. Bind to `ObservableCollection<CashBoxDto> CashBoxes` loaded on VM init. Required when PaidAmount > 0 — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/Sales/SalesInvoiceEditorView.xaml` + `Views/Purchases/PurchaseInvoiceEditorView.xaml` and their ViewModels

**Checkpoint**: Post invoice with payment → CashTransaction created → balance updated. Cancel invoice → offsetting CashTransaction created.

---

## Phase 5: US3 — Cash Transfer Between Boxes (Priority: P2)

**Goal**: Manager transfers cash from one box to another atomically — two CashTransaction entries (TransferOut + TransferIn) created or neither.

**Independent Test**: Transfer 200 from Box#1 (balance=500) to Box#2 (balance=100) → Box#1 balance=300, Box#2 balance=300. Transfer 600 from Box#1 → rejected "الرصيد غير كافٍ لإتمام التحويل".

- [X] T028 [US3] Implement `CashBoxService.TransferAsync`. Logic: (1) validate source ≠ destination, (2) compute source balance, (3) validate source balance ≥ amount, (4) `BeginTransactionAsync`, (5) re-compute balance inside tx, (6) re-validate, (7) create `TransferOut` on source, (8) create `TransferIn` on destination, (9) `CommitAsync`. On failure: `RollbackAsync`. Log transfer via Serilog — FILE: `SalesSystem/SalesSystem.Application/Services/CashBoxService.cs` — RULE-081, RULE-003

- [X] T029 [P] [US3] Add `CashTransferRequestValidator`: `SourceCashBoxId` NotEqual `DestinationCashBoxId`; `Amount` GreaterThan(0) — FILE: `SalesSystem/SalesSystem.Api/Validators/CashTransferRequestValidator.cs` — RULE-044

- [X] T030 [US3] Add `POST /api/v1/cash-boxes/transfer` endpoint to `CashBoxesController`. Validate `CashTransferRequest`, call `_service.TransferAsync`, return 200/400/409 — FILE: `SalesSystem/SalesSystem.Api/Controllers/CashBoxesController.cs`

- [X] T031 [P] [US3] Add `TransferAsync(CashTransferRequest)` to `ICashBoxApiService` and implement in `CashBoxApiService` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/CashBoxApiService.cs`

- [X] T032 [US3] Build `CashTransferView.xaml` + `CashTransferViewModel.cs`. Fields: SourceCashBox (ComboBox), DestinationCashBox (ComboBox), Amount* (TextBox > 0, ToolTip="مبلغ التحويل"), Notes (TextBox). `TransferCommand` always enabled. On success: toast `ShowSuccess("تم التحويل بنجاح")` + publish `CashBoxChangedMessage` — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/CashBoxes/CashTransferView.xaml` + `ViewModels/CashBoxes/CashTransferViewModel.cs`

**Checkpoint**: Transfer completes atomically. Source insufficient → rejected. Two CashTransaction rows created per successful transfer.

---

## Phase 6: US4 — Daily Closure Computation (Priority: P2)

**Goal**: Manager performs end-of-day closure. Closure is immutable, unique per box per date. ClosingBalance = OpeningBalance + TotalIncome - TotalExpense.

**Independent Test**: Box OpeningBalance=500, SalesIncome=800, Expense=200 → PerformDailyClosure → ClosingBalance=1100. Second closure same day+box → rejected "تم إغلاق الصندوق بالفعل لهذا اليوم".

- [X] T033 [US4] Implement `CashBoxService.PerformDailyClosureAsync`. Logic: (1) check no existing closure for `cashBoxId + today`, (2) sum today's credit transactions → `totalIncome`, (3) sum today's debit transactions → `totalExpense`, (4) `closingBalance = openingBalance + totalIncome - totalExpense`, (5) create `DailyClosure` entity, (6) save. Return `Result.Failure("تم إغلاق الصندوق بالفعل لهذا اليوم")` on duplicate. Implement `GetDailyClosuresAsync` via EF query ordered by ClosureDate desc — FILE: `SalesSystem/SalesSystem.Application/Services/CashBoxService.cs` — RULE-083

- [X] T034 [US4] Add endpoints to `CashBoxesController`:
  - `GET /{id}/daily-closures` → `GetDailyClosuresAsync` → 200
  - `POST /{id}/daily-closures` → `PerformDailyClosureAsync` → 201/409
  — FILE: `SalesSystem/SalesSystem.Api/Controllers/CashBoxesController.cs`

- [X] T035 [P] [US4] Add `GetDailyClosuresAsync` and `PerformDailyClosureAsync` to `ICashBoxApiService` and implement — FILE: `SalesSystem/SalesSystem.DesktopPWF/Services/Api/CashBoxApiService.cs`

- [X] T036 [US4] Build `DailyClosureView.xaml` + `DailyClosureViewModel.cs`. Shows today's projected summary: TotalIncome, TotalExpense, ProjectedClosingBalance. "إغلاق اليوم" button calls `ShowConfirmationAsync` before posting. Read-only DataGrid of past closures — FILE: `SalesSystem/SalesSystem.DesktopPWF/Views/CashBoxes/DailyClosureView.xaml` + `ViewModels/CashBoxes/DailyClosureViewModel.cs`

**Checkpoint**: Daily closure computes correctly. Duplicate rejected. Past closures viewable.

---

## Phase 7: Polish

- [X] T037 [P] Verify all `CashBoxesController` action methods have `[Authorize]`: `grep` for action methods and confirm attribute present — RULE-038

- [X] T038 [P] Verify no `MessageBox.Show` in CashBox ViewModels: `grep -r "MessageBox.Show" SalesSystem/SalesSystem.DesktopPWF/ViewModels/CashBoxes/` — RULE-055

- [X] T039 [P] Add Arabic ToolTips to all interactive XAML controls in CashBoxEditorView, CashBoxTransactionsView, CashTransferView, DailyClosureView — RULE-059

- [X] T040 [P] Add unit tests for `CashBoxService`: (a) balance computed as sum of transactions, (b) expense rejected when balance insufficient, (c) transfer rejected when source insufficient, (d) transfer creates two entries atomically, (e) duplicate closure rejected — FILE: `SalesSystem/Tests/SalesSystem.Application.Tests/Services/CashBoxServiceTests.cs`

- [X] T041 Update `docs/CHANGELOG.md` with v4.3 entry: Cash Boxes (CashBox, CashTransaction, DailyClosure), invoice payment linking, atomic transfers, daily closure — FILE: `docs/CHANGELOG.md`

---

## Dependencies

- **Phase 1 (Setup)**: No dependencies
- **Phase 2 (Foundational)**: Depends on Phase 1 — **BLOCKS Phase 3–6**
- **Phase 3 (US1)**: Depends on Phase 2
- **Phase 4 (US2)**: Depends on Phase 2 + T013 (CashBoxService with RecordInvoicePaymentAsync)
- **Phase 5 (US3)**: Depends on Phase 2 + T013
- **Phase 6 (US4)**: Depends on Phase 2 + T013
- **Phase 7 (Polish)**: Depends on all story phases

---

## Implementation Strategy

### MVP First (US1 + US2 only)

1. Complete Phase 2: Foundational (entities, migration)
2. Complete Phase 3: US1 — Cash box CRUD + expense recording
3. Complete Phase 4: US2 — Invoice payment → cash box link
4. **STOP AND VALIDATE** — invoice posting creates correct CashTransaction
5. Phases 5+6 can follow (transfers + closures)
