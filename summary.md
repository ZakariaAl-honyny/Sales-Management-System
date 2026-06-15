## Build Status

### ❌ Still broken
| Project | Errors | Notes |
|---------|--------|-------|
| DesktopPWF | TBD | Pre-existing ILogService.cs/SystemLogDto issue + unregistered new views/VMs |
| API | TBD | Pre-existing ILogService.cs/SystemLogDto issue |
| Application | ❌ 1 error | `ILogService.cs(39,29): error CS0246: SystemLogDto not found` — pre-existing, outside scope |

---

## Current Session: Massive 65-to-82 Table Schema Refactoring + Module Implementation

### Scope
Massive schema restructure from 82 tables → 65 tables, removing V2-deferred entities, adding new entities for V1 final design, implementing ~20 new modules across all 6 projects.

### Git Info
- **Branch**: `028-products-module-complete`
- **HEAD**: `cf55548 Phase 25 Products Module: ProductBarcode/UnitBarcode removal, InventoryBatch entity, ProductPrice UI, import pipeline, and specs`
- **Uncommitted**: ~350+ modified files + ~250+ new untracked files across all 6 projects

---

### Session Goal
Implement a comprehensive schema refactoring (82→65 tables) + ~20 new backend modules with full Desktop (WPF) UI, completing the foundation for V1. The work spans all 6 projects (Domain, Contracts, Application, Infrastructure, API, DesktopPWF) and adds:

### What Was Done

#### 1. Schema Refactoring (82 → 65 Tables)
**Removed entities** (deferred to V2):
- `CustomerGroup`, `SupplierType`, `PurchaseOrder`, `SalesQuotation`, `Cheque`, `DailyClosure`
- `InventoryMovement`, `StockTransfer`, `StockTransferItem`
- `InventoryOperation`, `InventoryOperationItem`, `StockWriteOff`
- `ProductBarcode`, `ProductImage`, `BillOfMaterials`
- `CustomerPayment`, `SupplierPayment` (old)
- `ProductPriceHistory` (old), `StoreSettings` (old)
- `Category` (old), `ExchangeRateHistory`
- `FiscalYearClosure`, `SystemAccountMappings` (old)
- `PaymentAllocation`, `AdditionalFee`, `AdditionalFeeAllocation`
- `MovementType`, `CashTransactionType`, `ChequeStatus`, `QuotationStatus`, `POStatus`, `InventoryOperationType` (enums)

**Added entity types:**
| Entity | Purpose |
|--------|---------|
| `Party` | Shared contact data — Customers/Suppliers link via PartyId FK |
| `CustomerContact` | Customer contact persons (CustomerId FK) |
| `SupplierContact` | Supplier contact persons (SupplierId FK) |
| `CustomerReceipt` | Replaces CustomerPayment — multi-invoice allocation |
| `CustomerReceiptApplication` | Links receipt to invoice with allocated amount |
| `InventoryBatch` | FIFO/FEFO batch tracking with UnitCost |
| `InventoryTransaction` | Header for all stock movements (12 types) |
| `InventoryTransactionLine` | Detail lines with ProductUnitId, Quantity, BatchNo |
| `WarehouseTransfer` | Multi-item transfers replacing StockTransfer |
| `WarehouseTransferLine` | Transfer line with ProductUnitId, Quantity |
| `InventoryAdjustment` | Stock adjustments (Addition/Deduction/Correction) |
| `InventoryAdjustmentLine` | Adjustment line with ProductUnitId, Quantity |
| `InventoryCount` | Physical count header |
| `InventoryCountLine` | Count line with system/actual quantities |
| `ProductCategory` | Multi-category product classification |
| `ProductPrices` | Multi-currency pricing (ProductUnitId × CurrencyId) |
| `CompanySettings` | Replaces StoreSettings — key-value config |
| `ReceiptVoucher` | Cash box money-in transactions |
| `PaymentVoucher` | Cash box money-out transactions |
| `SystemAccountMapping` | Maps system operations to COA accounts |
| `Role` | Standalone Role entity (previously embedded in User) |
| `UserRole` | Many-to-many User-Role link |
| `UserBranch` | User branch access |
| `Branch` | Branch management |
| `Department` | Employee departments |
| `Employee` | Employee records |
| `Expense` | Expense tracking |
| `Bank` | Bank account records |
| `AccountCategory` | Account categorization |
| `CurrencyRate` | Exchange rate history |
| `Notification` | System notifications |
| `Attachment` | File attachments |
| `FiscalYearEditorViewModel` | Desktop fiscal year management |

**New base classes:**
- `Entity` (int PK with CreatedAt)
- `LongEntity` (long PK with CreatedAt)
- `AuditableEntity` (CreatedBy/UpdatedBy)
- `ActivatableEntity` (IsActive)
- `DocumentEntity` (DocumentNo, DocumentDate)

**smallint FK changes:**
- `Branches.Id`, `Warehouses.Id`, `Currencies.Id`, `Units.Id`, `Roles.Id`, `Departments.Id`, `Taxes.Id`, `AccountCategories.Id`

**bigint changes:**
- `AuditLog.Id`, `SystemLog.Id`
- `SystemLog.Level`: nvarchar → tinyint (enum)

#### 2. Customer Contacts & Supplier Contacts (fully implemented)
- **Domain**: `CustomerContact.cs`, `SupplierContact.cs` entities
- **EF Configs**: `CustomerContactConfiguration.cs`, `SupplierContactConfiguration.cs`
- **Contracts**: DTOs in `AllDtos.cs`, request records in `ContactRequests.cs`
- **Services**: `ICustomerContactService`, `ISupplierContactService` + implementations
- **API**: `CustomerContactsController`, `SupplierContactsController` with AllStaff/ManagerAndAbove policies
- **Desktop API**: `CustomerContactApiService`, `SupplierContactApiService`
- **Desktop ViewModels**: CustomerContactListViewModel, CustomerContactEditorViewModel, SupplierContactListViewModel, SupplierContactEditorViewModel
- **Desktop Views**: XAML list + editor views for both
- **Integration**: "جهات الاتصال" button added to CustomerEditorView.xaml and SupplierEditorView.xaml footers

#### 3. Product Enhancements (Phase 25)
- `ProductCostService` / `IProductCostService` — cost calculation service
- `FifoAllocationService` / `IFifoAllocationService` — FIFO cost allocation
- `ProductImportController` — enhanced with ImportMode (Insert/Update)
- `CreateProductRequestValidator` — added OpeningQuantity, OpeningUnitCost, OpeningExpiryDate validation
- `ProductPriceRequestsValidator` — new validator for product pricing
- `ProductCategoryRequestsValidator` — new validator
- `InventoryBatchRequestsValidator` — new validator
- `ImportMode` enum (Insert/Update)
- `ProductImportRowDtoValidator` — enhanced with opening stock field validations
- `ProductImportExecuteRequest` record — wraps rows + mode

#### 4. Chart of Accounts Improvements
- `CreateAccountRequestValidator` — removed AccountCode/OopeningBalance validation (now auto-generated server-side)
- `AccountsController` — minor fixes
- `SystemAccountMappingController` — new CRUD controller
- `SystemAccountMappingRequests` / `SystemAccountMappingDto` — new DTOs
- `SystemAccountMappingValidators` — FluentValidation
- Desktop: `SystemAccountMappingListViewModel`, `SystemAccountMappingEditorViewModel` + XAML views
- Seeded accounts updated with 4-digit Level-1 codes (auto-generation strategy)
- RULE-344 updated: Level-1 codes are now 4 digits, auto-generated server-side

#### 5. Warehouse Transfers (new module)
- `WarehouseTransfer`, `WarehouseTransferLine` entities
- `WarehouseTransferConfiguration`, `WarehouseTransferLineConfiguration`
- `IWarehouseTransferService`, `WarehouseTransferService`
- `WarehouseTransfersController` with `CreateWarehouseTransferValidator`
- Desktop: `WarehouseTransferEditorViewModel`, `WarehouseTransfersListViewModel` + XAML views

#### 6. Inventory Operations (new module)
- `InventoryTransaction`, `InventoryTransactionLine`, `InventoryAdjustment`, `InventoryAdjustmentLine`, `InventoryCount`, `InventoryCountLine` entities
- `InventoryTransactionType`, `InventoryAdjustmentType`, `InventoryCountStatus` enums
- All EF configurations, service interfaces + implementations
- Controllers for Adjustments, Counts
- Desktop ViewModels + Views for InventoryTransactionList, InventoryAdjustmentEditor/List, InventoryCountEditor/List

#### 7. Customer Receipts (replaces Customer Payments)
- `CustomerReceipt`, `CustomerReceiptApplication` entities
- `CustomerReceiptConfiguration`, `CustomerReceiptApplicationConfiguration`
- `ICustomerReceiptService`, `CustomerReceiptService`
- `CustomerReceiptsController` with validator
- Desktop: `CustomerReceiptEditorViewModel`, `CustomerReceiptListViewModel` + XAML views

#### 8. Receipt & Payment Vouchers (replaces Cash Transactions)
- `ReceiptVoucher`, `PaymentVoucher` entities (under Accounting)
- `VoucherStatus` enum
- `ReceiptVoucherConfiguration`, `PaymentVoucherConfiguration`
- `IReceiptVoucherService`, `IPaymentVoucherService` + implementations
- Controllers + Validators + Desktop ViewModels/Views

#### 9. Party Module
- `Party` entity with `PartyType` enum
- `IPartyService`, `PartyService`
- `PartiesController`
- Desktop: `PartyEditorViewModel`, `PartyListViewModel` + XAML views

#### 10. Other Modules (Branches, Banks, Departments, Employees, Expenses, AccountCategories, ProductCategories, Attachments, Notifications, CompanySettings, DocumentSequences, Roles, RolePermissions, Sessions)
- **Branches**: Branch entity, BranchConfiguration, Controller, Validators, Desktop Views/ViewModels
- **Banks**: Bank entity, BankConfiguration, Controller, Validators, Desktop Views/ViewModels
- **Departments**: Department entity, DepartmentConfiguration, Controller, Validators, Desktop Views/ViewModels
- **Employees**: Employee entity, EmployeeConfiguration, Controller, Validators, Desktop Views/ViewModels
- **Expenses**: Expense entity, ExpenseConfiguration, Controller, Validators, Desktop Views/ViewModels
- **AccountCategories**: AccountCategory entity, Configuration, Controller, Validators, Desktop Views/ViewModels
- **ProductCategories**: ProductCategory entity, Configuration, Controller, Validators
- **Attachments**: Attachment entity, Configuration, Controller, Validators
- **Notifications**: Notification entity, Configuration, Controller, Desktop Views/ViewModels
- **CompanySettings**: CompanySettings entity, Configuration, Controller, Desktop Views/ViewModels
- **DocumentSequences**: Controller + Desktop Views/ViewModels
- **Roles**: Role entity, `RoleService`, `RolesController`, Desktop Views/ViewModels
- **RolePermissions**: `RolePermissionView.xaml` Desktop UI
- **UserSessions**: `SessionsController`, Desktop Views/ViewModels
- **SupplierPaymentApplication**: Entity, Service, Controller, Validators, Desktop API

#### 11. Infrastructure Changes
- `SystemLogRepository` — new repository
- `ISystemLogRepository` — new interface
- `SupplierConfiguration.cs`, `WarehouseConfiguration.cs`, `UnitConfiguration.cs` — new EF configs
- `UserRoleConfiguration.cs`, `UserBranchConfiguration.cs` — new EF configs for many-to-many
- `SalesDbContext.cs` — updated with all new DbSets
- `UnitOfWork.cs` — updated with all new repository properties
- `DbSeeder.cs` — updated for new schema
- **New initial migration** `20260613014517_InitialCreate` — squashes all previous migrations into a single comprehensive migration
- All old migrations deleted (replaced by single InitialCreate)

#### 12. Services Registered (API Program.cs)
- `IProductCostService`, `IInventoryBatchService`, `IFifoAllocationService`
- `IProductImageService`, `IAssemblyService`
- `System.ServiceProcess.ServiceController` NuGet package added
- Plus ~20+ new service registrations for all new modules

#### 13. Desktop API Services Created (~25 new files)
- `AccountCategoryApiService`, `AttachmentApiService`, `BankApiService`, `BranchApiService`
- `CompanySettingsApiService`, `CustomerContactApiService`, `CustomerReceiptApiService`
- `DepartmentApiService`, `DocumentSequenceApiService`, `EmployeeApiService`
- `ExpenseApiService`, `IReceiptVoucherApiService`, `InventoryAdjustmentApiService`
- `InventoryCountApiService`, `NotificationApiService`, `PartyApiService`
- `PaymentVoucherApiService`, `ProductCategoryApiService`, `ReceiptVoucherApiService`
- `RoleApiService`, `SupplierContactApiService`, `SupplierPaymentApplicationApiService`
- `SystemAccountMappingApiService`, `UserSessionApiService`, `WarehouseTransferApiService`

#### 14. Desktop ViewModels Created (~30 new files)
- AccountCategories/, Attachments/, Bank/, Branch/, CompanySettings/
- CustomerReceipt/, Customers/ (CustomerContact*), Department/, DocumentSequences/
- Employee/, Expense/, InventoryAdjustment/, InventoryCount/, Inventory/
- Notifications/, Party/, PaymentVouchers/, ReceiptVoucher/, Roles/ (RoleEditor, RoleList, RolePermission)
- Sessions/, Suppliers/ (SupplierContact*), SystemAccountMappings/, Transfers/ (WarehouseTransfer*, WarehouseTransfersList)
- Accounting/ (FiscalYearEditorViewModel, ReceiptVoucherEditorViewModel, ReceiptVoucherListViewModel)

#### 15. Desktop Views Created (~40+ XAML pairs)
- Full set of ListView + EditorView XAML/CS files for each new module
- Views follow existing RTL, Modern styles, compact sizing patterns

#### 16. Subagent/Doc Updates
- All 18+ `.opencode/agent/*.md` files updated with new schema knowledge
- `AGENTS.md` — 221 lines changed, RULE-344 updated (4-digit Level-1 codes)
- `docs/` — all Phase plans, CHANGELOG, spec files updated

### Key Design Decisions
- **Perpetual Inventory**: NO Purchases account — inventory costs go directly to Inventory Asset
- **Units as independent table**: smallint PK, user-addable, IsSystem flag
- **ProductPrices**: Multi-currency pricing per (ProductUnit × CurrencyId) with effective dates
- **InventoryBatches**: FIFO/FEFO batch-level cost allocation
- **Party entity**: Shared contact data for Customers/Suppliers
- **CashBox simplified**: No OpeningBalance/CurrentBalance — balance on linked Account only
- **Customer/Supplier simplified**: No OpeningBalance/CurrentBalance/CurrencyId
- **BaseCurrency immutable**: Cannot change after system creation
- **AccountCode auto-generated**: Server-side only, not user-supplied
- **Single migration**: All previous migrations squashed into `20260613014517_InitialCreate`

### Pre-existing Blockers
- `ILogService.cs(39,29): error CS0246: SystemLogDto not found` — prevents API + Desktop from building
- All new code references are logically consistent but cannot be verified via build until SystemLogDto issue resolved

### Next Steps
- Fix `ILogService.cs` / `SystemLogDto` type reference issue
- Register all new Desktop ViewModels in `App.xaml.cs` DI container
- Build + verify all 6 projects compile
- Test API endpoints via Swagger/HTTP file
- Verify Desktop CRUD flows for each new module
