# Tasks: Business Logic Implementation

**Branch**: `003-business-logic` | **Spec**: [spec.md](./spec.md) | **Plan**: [plan.md](./plan.md)
**Stack**: C# 13 / .NET 10 | ASP.NET Core | EF Core 10 | FluentValidation 11 | Serilog
**Pattern template**: Follow `ProductService.cs` / `ProductsController.cs` / `IProductService.cs` exactly

> ⚠️ **For smaller model**: Every task includes exact file path and precise instruction. Read `AGENTS.md` before starting any task. All money = `decimal`, all quantities = `decimal`, all services return `Result<T>`.

---

## Phase 1: Setup — Extend IUnitOfWork & UnitOfWork

**Purpose**: Add repositories for all Phase 3 entities. BLOCKS all other phases.

- [x] T001 Extend `SalesSystem\SalesSystem.Application\Interfaces\IUnitOfWork.cs` — add these 7 property declarations after line 19 (after `IGenericRepository<StoreSettings> StoreSettings { get; }`): `IGenericRepository<SalesInvoice> SalesInvoices { get; }`, `IGenericRepository<PurchaseInvoice> PurchaseInvoices { get; }`, `IGenericRepository<SalesReturn> SalesReturns { get; }`, `IGenericRepository<PurchaseReturn> PurchaseReturns { get; }`, `IGenericRepository<StockTransfer> StockTransfers { get; }`, `IGenericRepository<CustomerPayment> CustomerPayments { get; }`, `IGenericRepository<SupplierPayment> SupplierPayments { get; }`

- [x] T002 Extend `SalesSystem\SalesSystem.Infrastructure\Data\UnitOfWork.cs` — add 7 private nullable backing fields (after `_storeSettings` field) and 7 lazy-init properties (after `StoreSettings` property) matching the pattern `IGenericRepository<X>? _x; ... public IGenericRepository<X> X => _x ??= new GenericRepository<X>(_context);` for each of: `SalesInvoice`, `PurchaseInvoice`, `SalesReturn`, `PurchaseReturn`, `StockTransfer`, `CustomerPayment`, `SupplierPayment`

**Checkpoint**: Build succeeds (`dotnet build SalesSystem\SalesSystem.sln`) before proceeding.

---

## Phase 2: Foundational — InventoryService (BLOCKS US1–US5)

**Purpose**: The single authority for all stock reads/writes. All business services depend on it.

- [x] T003 Create `SalesSystem\SalesSystem.Application\Interfaces\Services\IInventoryService.cs` — interface in namespace `SalesSystem.Application.Interfaces.Services` with these methods: `Task<Result<decimal>> GetStockAsync(int productId, int warehouseId, CancellationToken ct)`, `Task<Result> IncreaseStockAsync(int productId, int warehouseId, decimal quantity, MovementType movementType, string referenceType, int referenceId, decimal? unitCost, int? userId, CancellationToken ct)`, `Task<Result> DecreaseStockAsync(int productId, int warehouseId, decimal quantity, MovementType movementType, string referenceType, int referenceId, decimal? unitCost, int? userId, CancellationToken ct)`, `Task<Result> ValidateStockAsync(int productId, int warehouseId, decimal requiredQty, CancellationToken ct)`

- [x] T004 Create `SalesSystem\SalesSystem.Application\Services\InventoryService.cs` — class `InventoryService : IInventoryService` in namespace `SalesSystem.Application.Services`. Constructor: `(IUnitOfWork uow, ILogger<InventoryService> logger)`. Implement `GetStockAsync`: query `_uow.WarehouseStocks.Query().FirstOrDefaultAsync(ws => ws.WarehouseId == warehouseId && ws.ProductId == productId, ct)`, return `Result<decimal>.Failure("Stock record not found")` if null else `Result<decimal>.Success(stock.Quantity)`. Implement `ValidateStockAsync`: call `GetStockAsync`, return failure if qty < requiredQty with message `$"Insufficient stock for product {productId}: available {qty}, required {requiredQty}"`. Implement `IncreaseStockAsync`: get or create `WarehouseStock`, compute `qtyBefore = stock.Quantity`, call `stock.IncreaseQuantity(quantity)` (or set via EF tracking), create `InventoryMovement.Create(productId, warehouseId, movementType, quantity, qtyBefore, qtyBefore+quantity, referenceType, referenceId, unitCost, null, userId)`, add movement, save — all already inside caller's transaction. Implement `DecreaseStockAsync`: same pattern but call `stock.DecreaseQuantity(quantity)` and use negative quantity for movement. Log every operation with `_logger.LogInformation`.

- [x] T005 Register `InventoryService` in `SalesSystem\SalesSystem.Api\Program.cs` — add `builder.Services.AddScoped<IInventoryService, InventoryService>();` in the DI section alongside existing service registrations. Add required `using` statements.

**Checkpoint**: Build succeeds before proceeding.

---

## Phase 3: US1 — Purchase Flow (Priority: P1) 🎯

**Goal**: Manager can create, post, and cancel purchase invoices with full stock + supplier balance effects.

**Independent Test**: POST `/api/v1/purchase-invoices` → POST `/{id}/post` → verify stock increased + `InventoryMovement` created + supplier balance updated.

### Implementation

- [x] T006 [P] [US1] Create `SalesSystem\SalesSystem.Contracts\Requests\Purchases\CreatePurchaseInvoiceRequest.cs` — record in namespace `SalesSystem.Contracts.Requests.Purchases`: `public record CreatePurchaseInvoiceRequest(int SupplierId, int WarehouseId, DateTime? InvoiceDate, DateOnly? DueDate, PaymentType PaymentType, decimal DiscountAmount, decimal TaxAmount, decimal PaidAmount, string? Notes, List<PurchaseInvoiceItemRequest> Items)` and `public record PurchaseInvoiceItemRequest(int ProductId, decimal Quantity, decimal UnitCost, decimal DiscountAmount)`

- [x] T007 [P] [US1] Create `SalesSystem\SalesSystem.Application\Interfaces\Services\IPurchaseService.cs` — interface in namespace `SalesSystem.Application.Interfaces.Services` with: `Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest request, int userId, CancellationToken ct)`, `Task<Result<PurchaseInvoiceDto>> PostAsync(int id, int userId, CancellationToken ct)`, `Task<Result<PurchaseInvoiceDto>> CancelAsync(int id, int userId, CancellationToken ct)`, `Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct)`, `Task<Result<PagedResult<PurchaseInvoiceDto>>> GetAllAsync(int? supplierId, int? status, int page, int pageSize, CancellationToken ct)`

- [x] T008 [US1] Create `SalesSystem\SalesSystem.Application\Services\PurchaseService.cs` — class `PurchaseService : IPurchaseService`. Constructor: `(IUnitOfWork uow, IInventoryService inventoryService, IDocumentSequenceService docSeq, ILogger<PurchaseService> logger)`. Implement `CreateAsync`: generate invoice number via `docSeq.GenerateAsync("PUR", ct)`, call `PurchaseInvoice.Create(invoiceNo, request.SupplierId, request.WarehouseId, ...)`, call `invoice.AddItem(PurchaseInvoiceItem.Create(...))` per item, `invoice.SetPaidAmount(request.PaidAmount)`, `invoice.SetTaxAmount(request.TaxAmount)`, add to `_uow.PurchaseInvoices`, save, return dto. Implement `PostAsync`: load invoice with items, validate status=Draft, open transaction, call `invoice.Post()`, save, then for each item call `_inventoryService.IncreaseStockAsync(item.ProductId, invoice.WarehouseId, item.Quantity, MovementType.PurchaseIn, "PurchaseInvoice", invoice.Id, item.UnitCost, userId, ct)`, if `invoice.DueAmount > 0` load supplier and call `supplier.IncreaseBalance(invoice.DueAmount)` and save, commit, return dto. Implement `CancelAsync`: load invoice, if Posted reverse stock (DecreaseStockAsync with MovementType.PurchaseReturnOut) and reverse supplier balance, set `invoice.Cancel()`, save, commit. Log all critical operations.

- [x] T009 [P] [US1] Create `SalesSystem\SalesSystem.Api\Validators\Purchases\CreatePurchaseInvoiceValidator.cs` — class `CreatePurchaseInvoiceValidator : AbstractValidator<CreatePurchaseInvoiceRequest>` in namespace `SalesSystem.Api.Validators.Purchases`. Rules: `SupplierId > 0`, `WarehouseId > 0`, `PaidAmount >= 0`, `DiscountAmount >= 0`, `TaxAmount >= 0`, `Items` not empty, each item `ProductId > 0`, `Quantity > 0`, `UnitCost >= 0`, `DiscountAmount >= 0`

- [x] T010 [US1] Create `SalesSystem\SalesSystem.Api\Controllers\PurchaseInvoicesController.cs` — `[ApiController][Route("api/v1/purchase-invoices")][Authorize(Policy = "ManagerAndAbove")]`. Constructor: `(IPurchaseService service)`. Endpoints: `GET /` → `GetAllAsync`, `GET /{id}` → `GetByIdAsync`, `POST /` → `CreateAsync` (extract userId from `User.FindFirst(ClaimTypes.NameIdentifier)`), `POST /{id}/post` → `PostAsync`, `POST /{id}/cancel` → `CancelAsync`. Translate `Result` to HTTP: Success→200/201, NotFound error→404, other failure→400.

- [x] T011 [US1] Register in `SalesSystem\SalesSystem.Api\Program.cs` — add `builder.Services.AddScoped<IPurchaseService, PurchaseService>();` and register `CreatePurchaseInvoiceValidator` (or rely on assembly scan if already configured).

**Checkpoint**: Purchase flow end-to-end works via Scalar UI.

---

## Phase 4: US2 — Sales Flow (Priority: P1) 🎯

**Goal**: Cashier/Manager can create, post, and cancel sales invoices with stock validation and customer balance effects.

**Independent Test**: POST `/api/v1/sales-invoices` → POST `/{id}/post` (with sufficient stock) → verify stock decreased + movement + customer balance. Attempt oversell → 400 error, stock unchanged.

### Implementation

- [x] T012 [P] [US2] Create `SalesSystem\SalesSystem.Contracts\Requests\Sales\CreateSalesInvoiceRequest.cs` — `public record CreateSalesInvoiceRequest(int? CustomerId, int WarehouseId, DateTime? InvoiceDate, DateOnly? DueDate, PaymentType PaymentType, decimal DiscountAmount, decimal TaxAmount, decimal PaidAmount, string? Notes, List<SalesInvoiceItemRequest> Items)` and `public record SalesInvoiceItemRequest(int ProductId, decimal Quantity, decimal UnitPrice, decimal DiscountAmount)`

- [x] T013 [P] [US2] Create `SalesSystem\SalesSystem.Application\Interfaces\Services\ISalesService.cs` — same shape as `IPurchaseService` but for sales: `CreateAsync(CreateSalesInvoiceRequest, int userId, ct)`, `PostAsync(int id, int userId, ct)`, `CancelAsync(int id, int userId, ct)`, `GetByIdAsync`, `GetAllAsync(int? customerId, int? warehouseId, int? status, int page, int pageSize, ct)`

- [x] T014 [US2] Create `SalesSystem\SalesSystem.Application\Services\SalesService.cs` — `SalesService : ISalesService`. Constructor: `(IUnitOfWork uow, IInventoryService inventoryService, IDocumentSequenceService docSeq, ILogger<SalesService> logger)`. `CreateAsync`: generate `INV-YYYY-NNNNNN` number, call `SalesInvoice.Create(...)`, `AddItem` per item, `SetPaidAmount`, `SetTaxAmount`, save. `PostAsync`: **BEFORE transaction** validate stock per item via `_inventoryService.ValidateStockAsync` — return failure immediately if any item fails. Then open transaction, call `invoice.Post()`, save, for each item call `_inventoryService.DecreaseStockAsync(..., MovementType.SaleOut, "SalesInvoice", invoice.Id, ...)`, if `invoice.DueAmount > 0` increase customer balance, commit. `CancelAsync`: if Posted, restore stock (`IncreaseStockAsync` with `MovementType.SaleReturnIn`) and reverse customer balance, cancel invoice, save. Log all operations with Serilog.

- [x] T015 [P] [US2] Create `SalesSystem\SalesSystem.Api\Validators\Sales\CreateSalesInvoiceValidator.cs` — `CreateSalesInvoiceValidator : AbstractValidator<CreateSalesInvoiceRequest>`. Rules: `WarehouseId > 0`, `PaidAmount >= 0`, `DiscountAmount >= 0`, `TaxAmount >= 0`, `Items` not empty, each item `ProductId > 0`, `Quantity > 0`, `UnitPrice >= 0`, `DiscountAmount >= 0`

- [x] T016 [US2] Create `SalesSystem\SalesSystem.Api\Controllers\SalesInvoicesController.cs` — `[Authorize(Policy = "AllStaff")]`. Same structure as `PurchaseInvoicesController` but calls `ISalesService`. Route: `api/v1/sales-invoices`.

- [x] T017 [US2] Register in `SalesSystem\SalesSystem.Api\Program.cs` — add `builder.Services.AddScoped<ISalesService, SalesService>();`

**Checkpoint**: Sales flow end-to-end works. Oversell returns 400. Stock and balance correct.

---

## Phase 5: US3 — Return Processing (Priority: P2)

**Goal**: Manager/Cashier can post sales returns (stock in, customer balance down) and purchase returns (stock out, supplier balance down).

**Independent Test**: Post sale of 10 units → post sales return of 4 → stock +4, customer balance reduced. Post purchase of 20 → post purchase return of 5 → stock -5, supplier balance reduced.

### Implementation

- [x] T018 [P] [US3] Create `SalesSystem\SalesSystem.Contracts\Requests\Returns\CreateSalesReturnRequest.cs` — `public record CreateSalesReturnRequest(int? SalesInvoiceId, int? CustomerId, int WarehouseId, DateTime? ReturnDate, string? Notes, List<ReturnItemRequest> Items)` and `public record ReturnItemRequest(int ProductId, decimal Quantity, decimal UnitPrice, decimal DiscountAmount)`

- [x] T019 [P] [US3] Create `SalesSystem\SalesSystem.Contracts\Requests\Returns\CreatePurchaseReturnRequest.cs` — `public record CreatePurchaseReturnRequest(int? PurchaseInvoiceId, int SupplierId, int WarehouseId, DateTime? ReturnDate, string? Notes, List<ReturnItemRequest> Items)`

- [x] T020 [P] [US3] Create `SalesSystem\SalesSystem.Application\Interfaces\Services\ISalesReturnService.cs` — `Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest, int userId, ct)`, `Task<Result<SalesReturnDto>> GetByIdAsync(int id, ct)`, `Task<Result<PagedResult<SalesReturnDto>>> GetAllAsync(int? customerId, int page, int pageSize, ct)`

- [x] T021 [P] [US3] Create `SalesSystem\SalesSystem.Application\Interfaces\Services\IPurchaseReturnService.cs` — same shape for purchase returns

- [x] T022 [US3] Create `SalesSystem\SalesSystem.Application\Services\SalesReturnService.cs` — `SalesReturnService : ISalesReturnService`. `CreateAsync`: if `SalesInvoiceId` provided, load original invoice and validate each return item qty ≤ original line qty (return failure if exceeded). Generate `SR-YYYY-NNNNNN` number. Open transaction, create `SalesReturn` + items, save (get ID), then for each item: `_inventoryService.IncreaseStockAsync(productId, warehouseId, qty, MovementType.SaleReturnIn, "SalesReturn", returnId, unitPrice, userId, ct)`. If CustomerId provided, load customer and call `customer.DecreaseBalance(lineTotal)`. Commit. Log.

- [x] T023 [US3] Create `SalesSystem\SalesSystem.Application\Services\PurchaseReturnService.cs` — mirror of `SalesReturnService` but: validate stock in source warehouse before transaction (`_inventoryService.ValidateStockAsync`), use `MovementType.PurchaseReturnOut`, call `_inventoryService.DecreaseStockAsync`, call `supplier.DecreaseBalance(lineTotal)`, use doc prefix `PR`.

- [x] T024 [P] [US3] Create `SalesSystem\SalesSystem.Api\Validators\Returns\CreateSalesReturnValidator.cs` and `CreatePurchaseReturnValidator.cs` — `WarehouseId > 0`, items not empty, each `ProductId > 0`, `Quantity > 0`, `UnitPrice >= 0`. Purchase return also: `SupplierId > 0`.

- [x] T025 [US3] Create `SalesSystem\SalesSystem.Api\Controllers\SalesReturnsController.cs` — `[Authorize(Policy = "AllStaff")]`, route `api/v1/sales-returns`, calls `ISalesReturnService`. POST `/` → 201, GET `/`, GET `/{id}`.

- [x] T026 [US3] Create `SalesSystem\SalesSystem.Api\Controllers\PurchaseReturnsController.cs` — `[Authorize(Policy = "ManagerAndAbove")]`, route `api/v1/purchase-returns`, calls `IPurchaseReturnService`.

- [x] T027 [US3] Register in `SalesSystem\SalesSystem.Api\Program.cs` — `AddScoped<ISalesReturnService, SalesReturnService>()` and `AddScoped<IPurchaseReturnService, PurchaseReturnService>()`

**Checkpoint**: Sales and purchase return flows work end-to-end with correct stock and balance effects.

---

## Phase 6: US4 — Stock Transfer (Priority: P2)

**Goal**: Manager can atomically move stock between two warehouses with dual InventoryMovement records.

**Independent Test**: W1=50, W2=10 → transfer 20 → W1=30, W2=30. TransferOut + TransferIn movements exist. Same-warehouse transfer → 400.

### Implementation

- [x] T028 [P] [US4] Create `SalesSystem\SalesSystem.Contracts\Requests\Transfers\CreateStockTransferRequest.cs` — `public record CreateStockTransferRequest(int FromWarehouseId, int ToWarehouseId, DateTime? TransferDate, string? Notes, List<TransferItemRequest> Items)` and `public record TransferItemRequest(int ProductId, decimal Quantity, string? Notes)`

- [x] T029 [P] [US4] Create `SalesSystem\SalesSystem.Application\Interfaces\Services\IStockTransferService.cs` — `Task<Result<StockTransferDto>> CreateAsync(CreateStockTransferRequest, int userId, ct)`, `Task<Result<StockTransferDto>> GetByIdAsync(int id, ct)`, `Task<Result<PagedResult<StockTransferDto>>> GetAllAsync(int? fromWarehouseId, int? toWarehouseId, int page, int pageSize, ct)`

- [x] T030 [US4] Create `SalesSystem\SalesSystem.Application\Services\StockTransferService.cs` — `StockTransferService : IStockTransferService`. `CreateAsync`: (1) if `FromWarehouseId == ToWarehouseId` return `Result.Failure("Cannot transfer to the same warehouse.")`. (2) **BEFORE transaction** validate stock per item in `FromWarehouseId` via `_inventoryService.ValidateStockAsync`. (3) Generate `TRF-YYYY-NNNNNN`. (4) Open transaction. (5) Create `StockTransfer` + items, call `transfer.Post()`, save (get ID). (6) For each item: `DecreaseStockAsync(productId, fromWarehouseId, qty, MovementType.TransferOut, "StockTransfer", id, null, userId, ct)` then `IncreaseStockAsync(productId, toWarehouseId, qty, MovementType.TransferIn, "StockTransfer", id, null, userId, ct)`. (7) Commit. Log.

- [x] T031 [P] [US4] Create `SalesSystem\SalesSystem.Api\Validators\Transfers\CreateStockTransferValidator.cs` — `FromWarehouseId > 0`, `ToWarehouseId > 0`, `FromWarehouseId != ToWarehouseId` (custom rule), items not empty, each `ProductId > 0`, `Quantity > 0`

- [x] T032 [US4] Create `SalesSystem\SalesSystem.Api\Controllers\StockTransfersController.cs` — `[Authorize(Policy = "ManagerAndAbove")]`, route `api/v1/stock-transfers`, calls `IStockTransferService`. POST `/` → 201, GET `/`, GET `/{id}`.

- [x] T033 [US4] Register in `SalesSystem\SalesSystem.Api\Program.cs` — `AddScoped<IStockTransferService, StockTransferService>()`

**Checkpoint**: Stock transfer works. Self-transfer returns 400. Both warehouse stocks updated correctly.

---

## Phase 7: US5 — Payment Recording (Priority: P2)

**Goal**: Cashier/Manager can record customer and supplier payments, reducing their outstanding balances.

**Independent Test**: Customer balance=500 → record payment 200 → balance=300. Supplier balance=1000 → payment 400 → balance=600. Amount=0 → 400 error.

### Implementation

- [x] T034 [P] [US5] Create `SalesSystem\SalesSystem.Contracts\Requests\Payments\CreateCustomerPaymentRequest.cs` — `public record CreateCustomerPaymentRequest(int CustomerId, int? SalesInvoiceId, decimal Amount, PaymentType PaymentMethod, DateTime? PaymentDate, string? Notes)`

- [x] T035 [P] [US5] Create `SalesSystem\SalesSystem.Contracts\Requests\Payments\CreateSupplierPaymentRequest.cs` — `public record CreateSupplierPaymentRequest(int SupplierId, int? PurchaseInvoiceId, decimal Amount, PaymentType PaymentMethod, DateTime? PaymentDate, string? Notes)`

- [x] T036 [P] [US5] Create `SalesSystem\SalesSystem.Application\Interfaces\Services\IPaymentService.cs` — `Task<Result<CustomerPaymentDto>> CreateCustomerPaymentAsync(CreateCustomerPaymentRequest, int userId, ct)`, `Task<Result<SupplierPaymentDto>> CreateSupplierPaymentAsync(CreateSupplierPaymentRequest, int userId, ct)`, `Task<Result<PagedResult<CustomerPaymentDto>>> GetCustomerPaymentsAsync(int? customerId, int page, int pageSize, ct)`, `Task<Result<PagedResult<SupplierPaymentDto>>> GetSupplierPaymentsAsync(int? supplierId, int page, int pageSize, ct)`

- [x] T037 [US5] Create `SalesSystem\SalesSystem.Application\Services\PaymentService.cs` — `PaymentService : IPaymentService`. `CreateCustomerPaymentAsync`: generate `CP-YYYY-NNNNNN`, open transaction, load customer (return 404 if not found), create `CustomerPayment` entity, add to `_uow.CustomerPayments`, call `customer.DecreaseBalance(request.Amount)`, save, commit, log. `CreateSupplierPaymentAsync`: same but `SP-YYYY-NNNNNN`, `SupplierPayment`, `supplier.DecreaseBalance`.

- [x] T038 [P] [US5] Create `SalesSystem\SalesSystem.Api\Validators\Payments\CreateCustomerPaymentValidator.cs` — `CustomerId > 0`, `Amount > 0`, `PaymentMethod` in [1,2,3]. Create `CreateSupplierPaymentValidator.cs` — `SupplierId > 0`, `Amount > 0`, `PaymentMethod` in [1,2,3].

- [x] T039 [US5] Create `SalesSystem\SalesSystem.Api\Controllers\PaymentsController.cs" — `[Authorize(Policy = "AllStaff")]`, route `api/v1/payments`. Customer endpoints under `/customer`, supplier under `/supplier`. Supplier payment endpoints: `[Authorize(Policy = "ManagerAndAbove")]`.

- [x] T040 [US5] Register in `SalesSystem\SalesSystem.Api\Program.cs` — `AddScoped<IPaymentService, PaymentService>()`

**Checkpoint**: Customer and supplier payments reduce balances. Zero-amount payment returns 400.

---

## Phase 8: Polish & Verification

- [x] T041 Add missing DTOs to `SalesSystem\SalesSystem.Contracts\DTOs\AllDtos.cs` — add `PurchaseInvoiceDto`, `PurchaseInvoiceItemDto`, `SalesInvoiceDto`, `SalesInvoiceItemDto`, `SalesReturnDto`, `SalesReturnItemDto`, `PurchaseReturnDto`, `PurchaseReturnItemDto`, `StockTransferDto`, `StockTransferItemDto`, `CustomerPaymentDto`, `SupplierPaymentDto` as C# records following existing DTO patterns in that file. All money fields `decimal`, all quantity fields `decimal`.

- [x] T042 Verify Serilog coverage — ensure every service logs: invoice creation, posting, cancellation, stock increase/decrease, payment recording. Use `_logger.LogInformation` for success, `_logger.LogError` for exceptions caught in catch blocks. Never use `Console.WriteLine`.

- [x] T043 Full build verification — run `dotnet build SalesSystem\SalesSystem.sln` and fix all errors. Treat `CS8602` nullable warnings in controllers as non-blocking but fix `CS0246` (missing type) errors.

- [x] T044 End-to-end smoke test — follow `specs/003-business-logic/quickstart.md` steps 1–9 against running API to verify all 5 user stories work. Check the Definition of Done checklist at the bottom of quickstart.md.

---

## Dependencies & Execution Order

```
Phase 1 (T001–T002)  →  Phase 2 (T003–T005)  →  Phase 3–7 can start in parallel
                                                   Phase 3 (US1 Purchase)
                                                   Phase 4 (US2 Sales)      ← depends on stock from US1
                                                   Phase 5 (US3 Returns)    ← depends on US1 + US2
                                                   Phase 6 (US4 Transfer)
                                                   Phase 7 (US5 Payments)
                                                   ↓
                                               Phase 8 (Polish)
```

**Within each phase**: [P] tasks can run in parallel → service and validator created simultaneously → then controller → then DI registration.

---

## Implementation Strategy (smaller model guidance)

### MVP First (Purchase + Sales — US1 + US2)
1. Complete T001–T002 (IUnitOfWork extension)
2. Complete T003–T005 (InventoryService)
3. Complete T006–T011 (Purchase flow)
4. Complete T012–T017 (Sales flow)
5. **Validate**: Run quickstart.md steps 3–5 before proceeding

### Incremental (add remaining stories one at a time)
- T018–T027 (Returns) → T028–T033 (Transfer) → T034–T040 (Payments)
- T041–T044 (Polish)

### Key rules for the implementing model
- Copy the exact pattern from `ProductService.cs` and `ProductsController.cs`
- Never compute `LineTotal`, `SubTotal`, `TotalAmount`, `DueAmount` outside domain entities
- Always validate stock BEFORE calling `BeginTransactionAsync()`
- Every `IncreaseStockAsync` / `DecreaseStockAsync` call happens INSIDE an open transaction
- All service methods return `Result<T>` — never throw exceptions to controllers
- All controllers have `[Authorize]` — no anonymous endpoints
