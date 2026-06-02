# Product Requirements Document (PRD) & Master Implementation Reference
# Sales Management System (v4.6.4 — Security Hardening & Code Quality)
# Platform: .NET 10 LTS | Clean Architecture | Year: 2026

---


<!-- START OF SECTION: 1. Functional & Non-Functional Requirements (English) -->

> [!NOTE]
> Core functional scope and non-functional engineering standards of the Sales Management System, including specifications for modules v4.0 through v4.6.

## 1. Executive Summary

A local desktop sales management system for small retail shops.
Built on Clean Architecture with WPF Desktop (MVVM) + ASP.NET Core 10 API
+ SQL Server. Designed for future expansion to Web and Mobile.

---

## 2. Scope

### In Scope
- User authentication with role-based access (Admin/Manager/Cashier) — JWT with BCrypt
- **Dynamic Unit of Measure (v4.3)**: ProductUnits with per-unit pricing, unit-specific barcodes, SmartUnitFormatter
- **Costing Strategy (v4.3)**: WeightedAverage (1), LastPurchasePrice (2), SupplierPrice (3)
- **Cash Box Management (v4.3)**: Multi-box, immutable CashTransactions, DailyClosure
- **Product Price History (v4.3)**: Audit trail for every price/cost change
- Multi-warehouse inventory management with stock transfer between warehouses
- Purchase invoices (Cash/Credit/Mixed) with line-level/invoice-level discounts
- Sales invoices (Cash/Credit/Mixed) with real-time stock validation
- Sales returns and purchase returns with quantity validation against originals
- Customer and supplier balance tracking with payment management
- **Invoice Printing (v4.3)**: A4 PDF via QuestPDF + 80mm thermal via Win32 raw printing
- **Auto-Update System (v4.4)**: SHA256-verified, fire-and-forget, GitHub-based
- **Backup & Restore (v4.4)**: Raw SQL BACKUP DATABASE, scheduled daily at 2 AM
- **Security & DPAPI (v4.4)**: Encrypted connection strings, DataProtection keys
- **Windows Service (v4.4)**: API runs as `SalesSystemService` with auto-restart
- **Database Health Check (v4.5)**: Desktop verifies DB before login; API exposes `/health/database`
- **Multi-Window Screen Management (v4.5)**: Non-modal editors via ScreenWindowService
- **WPF Validation ErrorTemplate (v4.6.2)**: Red border + ❗ icon, INotifyDataErrorInfo, ValidateAllAsync()
- **LogSystemError centralized (v4.6)**: Unified system error logging in ViewModelBase
- **Identifier Strategy — No Code columns (v4.5.3)**: Auto-increment Id only; Code removed from Product/Customer/Supplier/Warehouse
- Delete operations with 3-tier strategy (Cancel/Deactivate/Permanent)
- Audit tracking on all entities (CreatedAt, CreatedByUserId, IsActive)
- Role-based UI visibility and API authorization
- Serilog logging for all critical operations

### Out of Scope (Future Phases)
- Full accounting system with general ledger
- Multi-branch management with branch-level P&L
- Web or mobile client
- External API integrations (payment gateways, e-commerce)

---

## 3. Functional Requirements

### 3.1 Authentication
- Login with username + password
- JWT token issued on successful login (expires: 8 hours)
- Refresh token for session continuity
- Role stored in JWT claims: Admin=1, Manager=2, Cashier=3
- Failed login attempts logged
- Logout clears token from Desktop memory

### 3.2 Product Management (v4.6)
- Add / Edit / Deactivate products (no hard delete)
- Identifier: Auto-increment `Id` only — no `Code` field (v4.5.3)
- Fields: Name, Barcode (unique), Category, Description
- **Dynamic Units (v4.3)**: Each product has multiple `ProductUnit` entries with per-unit pricing:
  - One `IsBaseUnit = true` (smallest unit, ConversionFactor=1)
  - Derived units have ConversionFactor > 1 (e.g., Box=24)
  - Per-unit: `RetailPrice`, `WholesalePrice`, `UnitName`
- **UnitBarcodes (v4.3)**: Each product-unit combination can have multiple barcodes
- Search by: Id, Name, Barcode
- Filter by: Category, Active status, Warehouse stock
- Barcode scanner support (keyboard input simulation)
- **Cost cascade**: When purchase cost updates, all product units recalculate from base unit cost × conversion factor

### 3.3 Warehouse Management (v4.6)
- Add / Edit / Deactivate warehouses (no Code field — auto-increment Id only)
- One warehouse flagged as IsDefault
- View stock per warehouse per product
- Stock transfer between warehouses (source ≠ destination enforced)
- `CHECK (Quantity >= 0)` constraint on WarehouseStocks at DB level

### 3.4 Purchase Invoice
- Select supplier and destination warehouse
- Add multiple products with quantity and unit cost
- Apply line-level discount per item
- Apply invoice-level discount
- Tax calculation (optional, from settings)
- Payment type: Cash / Credit / Mixed
- If Mixed: enter paid amount, system calculates due amount
- Auto-generate invoice number (format: PUR-2026-000001)
- On Post: increase stock in selected warehouse
- On Post: update supplier balance if credit/mixed
- On Cancel: reverse stock and balance

### 3.5 Sales Invoice
- Select customer (or use default Cash customer)
- Select source warehouse
- Add multiple products with quantity and unit price
- Apply line-level and invoice-level discounts
- Payment type: Cash / Credit / Mixed
- Validate: quantity available in selected warehouse
- Auto-generate invoice number (format: INV-2026-000001)
- On Post: decrease stock from selected warehouse
- On Post: update customer balance if credit/mixed
- On Cancel: reverse stock and balance
- Print invoice after saving

### 3.6 Sales Return
- Reference original invoice (optional)
- If referenced: validate return quantity ≤ sold quantity
  minus previously returned quantity
- On Post: increase stock in selected warehouse
- On Post: decrease customer balance by return amount

### 3.7 Purchase Return
- Reference original purchase invoice (optional)
- On Post: decrease stock from selected warehouse
- On Post: decrease supplier balance by return amount

### 3.8 Stock Transfer
- Select source warehouse and destination warehouse
- Source ≠ Destination (enforced at DB and application level)
- Add products with quantities
- Validate: sufficient stock in source warehouse
- On Post: decrease source warehouse stock
- On Post: increase destination warehouse stock
- Record both movements in InventoryMovements

### 3.9 Payments
- Customer payment: record cash received from customer
  → decreases customer balance
- Supplier payment: record cash paid to supplier
  → decreases supplier balance
- Both linked optionally to specific invoice

### 3.10 Reports
- Daily sales report (by date range)
- Daily purchases report (by date range)
- Stock report (per warehouse or all warehouses)
- Customer balance report (all or specific customer)
- Supplier balance report (all or specific supplier)
- Product movement report (history for one product)
- **[UPDATED]** Intelligent Low Stock Alert Report (below MinStock):
  - Dedicated screen filtered by Warehouse/Branch.
  - Automatically calculates and displays suggested reorder amounts in "Wholesale Units + Remaining Retail Units" (e.g., 2 Boxes and 5 Pieces) based on the Conversion Factor.
  - Professional PDF export capability grouped by Warehouse.

### 3.11 Settings
- Store name, phone, address
- Logo upload (stored as file path)
- Default tax rate and tax toggle
- Default warehouse selection

### 3.12 Backup
- Backup database via SQL Server BACKUP DATABASE command
- Restore database via API endpoint (requires restart)
- Only Admin can perform backup/restore
- Backup creates timestamped `.bak` file
- Restore confirmation requires re-login

### 3.13 Dynamic Unit of Measure (v4.3)

**ProductUnits:**
- Each product can have multiple units (e.g., "Piece", "Box", "Carton")
- One unit must be marked as `IsBaseUnit = true` (the smallest/foundational unit)
- Base unit always has `ConversionFactor = 1`
- Derived units have `ConversionFactor > 1` (e.g., Box=24 means 1 Box = 24 base units)
- Per-unit pricing: `RetailPrice` and `WholesalePrice` stored on each ProductUnit
- **Enforced**: `ProductMustHaveAtLeastOneUnit` — throw DomainException if deleting last unit

**UnitBarcodes:**
- Barcodes stored in `UnitBarcode` table (FK → ProductUnits)
- One barcode uniquely identifies one specific product unit
- Replaces the old `ProductBarcodes` multi-barcode approach

**SmartUnitFormatter:**
- UI-only service that selects best display unit based on quantity threshold
- Example: 48 pieces → "2 Boxes" (if Box factor = 24)

**Conversion Math (Domain-Only):**
```csharp
var baseQty = quantity * sourceUnit.ConversionFactor;
var targetQty = baseQty / targetUnit.ConversionFactor;
```

### 3.14 Costing Strategy (v4.3)

**Three Methods (configurable):**

| Method | Enum | Formula | Use Case |
|--------|------|---------|----------|
| WeightedAverage | 1 | `(OldStock*OldCost + NewQty*NewCost) / TotalQty` | Default — smooths cost fluctuations |
| LastPurchasePrice | 2 | Direct overwrite: `AvgCost = NewUnitCost` | When latest price is most relevant |
| SupplierPrice | 3 | Use `Product.SupplierPrice` | Catalog pricing, no calculation |

**Flow:**
1. Costing method stored in `SystemSettings` table (seeded as `WeightedAverage`)
2. When purchase invoice is **Posted**: `UpdateProductPricingService` fires
3. Service reads costing method from settings
4. Calculates new cost using the selected formula
5. Updates `Product.AvgCost` and cascades to ALL product units
6. Records change in `ProductPriceHistory`
7. NEVER write costing logic outside `UpdateProductPricingService`

**Cost Cascade:**
```csharp
// After base unit cost is updated:
foreach (var unit in product.Units)
    unit.Cost = baseUnitCost * unit.ConversionFactor;
```

### 3.15 Cash Box Management (v4.3)

**CashBox Entities:**
- Multiple cash boxes with `OpeningBalance` and `CurrentBalance`
- `CurrentBalance` is computed from `CashTransaction` sum
- One cash box flagged as `IsDefault`
- **Constraint**: `CurrentBalance >= 0` (never negative)

**CashTransaction Types:**
| Type | Enum | Direction |
|------|------|-----------|
| OpeningBalance | 1 | Initial |
| SalesIncome | 2 | IN |
| Expense | 3 | OUT |
| TransferOut | 4 | OUT (to another box) |
| TransferIn | 5 | IN (from another box) |
| RefundOut | 6 | OUT (sales return refund) |
| SupplierPayment | 7 | OUT |
| CustomerPayment | 8 | IN |

**Rules:**
- Cash transactions are **immutable** — no editing, no deletion
- Cancellations done via **offsetting entry**
- Transfer between boxes requires TWO entries (Out from source, In to destination)
- `DailyClosure` computes: OpeningBalance + TotalIncome - TotalExpense = ClosingBalance
- Every invoice payment references `CashBoxId`

### 3.16 Invoice Printing (v4.3)

- Two print formats: A4 PDF (QuestPDF) + 80mm thermal receipt (ESC/POS)
- A4: RTL layout with store logo, tax breakdown, invoice details
- Thermal: 42-char monospaced columns, Windows-1256 encoding for Arabic
- Desktop calls `PrintController` API — never prints directly (v4.3)
- Preview A4 in `PdfPreviewWindow` before printing
- Print settings stored in `SystemSetting` table with `Category = "Print"`
- Supports: Sales, Purchase, SalesReturn, PurchaseReturn, Test page
- Logo is optional — missing logo handled gracefully (null check)

### 3.17 Auto-Update System (v4.4)

- Fire-and-forget on startup — never blocks login
- SHA256 checksum verification before launching installer
- Skipped version persisted to `%AppData%\SalesSystem\settings.json`
- Timeout = 8 seconds with silent failure
- Version comparison via `System.Version` (never string comparison)

### 3.18 Database Backup & Restore (v4.4)

- Backup uses raw SQL `BACKUP DATABASE` — no SMO dependency
- Restore uses `SINGLE_USER WITH ROLLBACK AFTER 30` — 30s grace period
- `ScheduledBackupWorker` runs daily at 2:00 AM as `BackgroundService`
- Backup retention = configurable days (default 30)
- Restore failure triggers `TrySetMultiUserAsync` recovery

### 3.19 Security & DPAPI (v4.4)

- Connection strings encrypted via DPAPI with `"DPAPI:"` prefix
- `FirstRunSetupService` encrypts on first run (idempotent)
- DataProtection keys stored in `%ProgramData%\SalesSystem\DataProtectionKeys`
- JWT secret from environment variable — throws in production if missing
- `appsettings.json` writes use atomic pattern: `.tmp` → `File.Replace()` → `.bak`

### 3.20 Windows Service (v4.4)

- API runs as `SalesSystemService` via `UseWindowsService()`
- Auto-recovery: 3 restarts on failure (1min, 5min, 15min delays)
- Serilog EventLog sink for Windows Service logging
- Database migration runs on service startup (auto-migrate)

### 3.21 Database Health Check (v4.5)

- API exposes `GET /api/v1/health` — returns `Database: Connected/Disconnected`
- API exposes `GET /api/v1/health/database` — checks `DbContext.Database.CanConnectAsync()`
- Desktop checks DB on startup before showing login via `IDatabaseHealthCheckService`
- `DatabaseErrorDialog` with Retry/Exit on connection failure
- `ExceptionMiddleware` detects DB exceptions → returns `503` with `DATABASE_CONNECTION_ERROR`

### 3.22 Multi-Window Screen Management (v4.5)

- Editors open non-modally via `ScreenWindowService.OpenScreen()` — never `ShowDialog()`
- `ScreenWindow.xaml` hosts any View/ViewModel pair generically
- Window tracking via `WeakReference<Window>` — no memory leaks
- Cascade positioning: 30px offset × (count % 10) from MainWindow
- Auto-titles in Arabic (e.g., "فاتورة بيع") — not English type names

### 3.23 WPF Validation ErrorTemplate & INotifyDataErrorInfo (v4.6.2)

- ErrorTemplate in `Styles.xaml`: Red border (#EF4444, 1.5px) + ❗ icon badge
- ToolTip on error icon bound to `[0].ErrorContent`
- Applies to TextBox, PasswordBox, ComboBox
- `ViewModelBase.cs`: `SetDialogService()`, `ValidateAllAsync()`, `ValidateField()`
- All Editor VMs call `SetDialogService()` in constructors
- Pre-save validation: `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()`
- No `HasXxxError` booleans — use `INotifyDataErrorInfo` directly
- Save buttons always enabled — validate on click with warning dialog

### 3.24 Identifier Strategy — No Code Columns (v4.5.3)

- Product, Customer, Supplier, Warehouse: auto-increment `Id` only
- No `Code` property on entities, DTOs, or editor ViewModels
- Search/filter by `Id` (int) or `Name` (string) — never by Code
- `DuplicateCode` error constant removed from ErrorCodes
- Code auto-generation services (`DocumentSequenceService` for PRD/CUST/SUP/WH) removed

---

## 4. Non-Functional Requirements

### 4.1 Performance
- Invoice save time: < 2 seconds
- Report generation: < 5 seconds for up to 1 year of data
- Product search: < 500ms

### 4.2 Security
- JWT authentication on all API endpoints
- Role-based access at API level (policy-based authorization)
- Role-based UI element visibility at Desktop level
- FluentValidation on all incoming API requests
- BCrypt password hashing (work factor: 12)
- Connection string encrypted or in environment variable

### 4.3 Reliability
- All financial operations wrapped in database transactions
- Automatic rollback on any failure
- Serilog file logging for all errors and critical operations
- Database constraints as final validation layer

### 4.4 Hardware Integration (New)
- **Desktop Version**: 
  - Barcode scanner support via Keyboard Emulator.
  - Automatic `Enter` key trigger to fetch and add products to the POS invoice instantly.
- **Mobile Version (Future Phase)**: 
  - Support for device camera barcode scanning using image processing libraries (e.g., ZXing or Google ML Kit) for rapid sales and inventory audits.

### 4.5 Deployment
- API runs as Windows Service (no console window)
- Desktop distributed as MSI installer
- .NET 10 runtime bundled in installer (self-contained)
- Setup wizard guides user through installation

---

## 5. Database Entities Summary
Users → Authentication and authorization
Units → Product measurement units
Categories → Product categories
Products → Product catalog
Warehouses → Storage locations
WarehouseStocks → Stock per product per warehouse ⚠️ Critical
            (includes: Quantity, ReorderLevel for low-stock alerts)
Suppliers → Supplier master data with balance
Customers → Customer master data with balance
PurchaseInvoices → Purchase invoice headers
PurchaseInvoiceItems → Purchase invoice line items
SalesInvoices → Sales invoice headers
SalesInvoiceItems → Sales invoice line items
PurchaseReturns → Purchase return headers
PurchaseReturnItems → Purchase return line items
SalesReturns → Sales return headers
SalesReturnItems → Sales return line items
StockTransfers → Transfer headers
StockTransferItems → Transfer line items
CustomerPayments → Payments received from customers
SupplierPayments → Payments made to suppliers
InventoryMovements → Complete audit trail of stock changes ⚠️ Critical
StoreSettings → Store configuration (single row)
DocumentSequences → Auto-increment invoice numbers
ProductUnits → Per-product dynamic units with pricing and conversion (v4.3)
UnitBarcodes → Unit-specific barcodes (replaces ProductBarcodes) (v4.3)
CashBoxes → Cash box management (v4.3)
CashTransactions → Immutable cash transaction log (v4.3)
ProductPriceHistory → Audit trail for price/cost changes (v4.3)
SystemSettings → Application-level settings (costing method, etc.) (v4.3)
SystemLog → Application/system log entries (v4.3)

text


---

## 6. Solution Architecture

> ⚠️ The Desktop project is **WPF (Windows Presentation Foundation)** with MVVM pattern.
> Project name: `SalesSystem.DesktopPWF` — NOT WinForms. All UI files are `.xaml`.
> Data Flow: `Desktop → (HttpClient) → Api → Application → Infrastructure → SQL Server`

```text
SalesSystem/
├── SalesSystem.Contracts/       ← DTOs + Requests + Responses + Result<T>
├── SalesSystem.Domain/          ← Entities + Business Rules + Exceptions
├── SalesSystem.Application/     ← Services + Interfaces + Use Cases + CQRS
├── SalesSystem.Infrastructure/  ← EF Core + DbContext + Repositories + UoW
├── SalesSystem.Api/             ← Controllers + FluentValidation + Middleware + JWT
└── SalesSystem.DesktopPWF/     ← WPF UI + MVVM + EventBus + Printing
│       ├── Result.cs
│       ├── PagedResult.cs
│       └── ErrorCodes.cs
│
├── SalesSystem.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── Exceptions/
│   └── Common/
│       └── BaseEntity.cs
│
├── SalesSystem.Application/
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   ├── Services/
│   │   └── IUnitOfWork.cs
│   ├── Services/
│   │   ├── ProductService.cs
│   │   ├── SalesService.cs ⚠️ Critical
│   │   ├── PurchaseService.cs ⚠️ Critical
│   │   ├── SalesReturnService.cs ⚠️ Critical
│   │   ├── PurchaseReturnService.cs ⚠️ Critical
│   │   ├── InventoryService.cs ⚠️ Critical
│   │   ├── StockTransferService.cs ⚠️ Critical
│   │   ├── PaymentService.cs
│   │   ├── ReportService.cs
│   │   ├── AuthService.cs
│   │   ├── BackupService.cs
│   │   └── DocumentSequenceService.cs ⚠️ Thread-safe
│   └── Queries/                        ← [NEW] CQRS Read models (SPEC-009)
│       └── GetLowStockReportQuery.cs
│
├── SalesSystem.Infrastructure/
│   ├── Data/
│   │   ├── SalesDbContext.cs
│   │   └── Configurations/
│   ├── Repositories/
│   ├── Migrations/
│   └── Services/
│       └── BackupService.cs
│
├── SalesSystem.Api/
│   ├── Controllers/
│   ├── Validators/               ← FluentValidation
│   ├── Middleware/
│   │   ├── ExceptionMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   └── Extensions/
│       ├── AuthExtensions.cs
│       └── ValidationExtensions.cs
│
└── SalesSystem.DesktopPWF/       ← WPF + MVVM (NOT WinForms)
    │
    ├── MainWindow.xaml            ← Shell / main layout
    ├── LoginWindow.xaml           ← Login screen
    ├── App.xaml                   ← Application entry point
    ├── App.xaml.cs                ← DI registration (App.GetService<T>())
    ├── appsettings.json
    │
    ├── Views/                     ← .xaml UI files (no logic)
    │   ├── Categories/
    │   ├── Common/                ← Shared controls / dialogs
    │   ├── Customers/
    │   ├── Dashboard/
    │   ├── Inventory/
    │   ├── Invoices/              ← Selection dialogs for invoices
    │   ├── Login/
    │   ├── Payments/
    │   ├── Products/
    │   ├── Purchases/
    │   ├── Reports/
    │   │   └── LowStockView.xaml  ← [NEW] SPEC-009
    │   ├── Returns/
    │   ├── Sales/
    │   ├── Settings/
    │   ├── Suppliers/
    │   ├── Transfers/
    │   ├── Units/
    │   ├── Users/
    │   └── Warehouses/
    │
    ├── ViewModels/                ← MVVM binding logic
    │   ├── ViewModelBase.cs
    │   ├── DashboardViewModel.cs
    │   ├── LoginWindowViewModel.cs
    │   ├── ReportsViewModel.cs
    │   ├── SettingsViewModel.cs
    │   ├── WarehouseListViewModel.cs
    │   ├── WarehouseEditorViewModel.cs
    │   ├── Categories/
    │   ├── Customers/
    │   ├── Inventory/
    │   ├── Invoices/
    │   ├── Payments/
    │   ├── Products/
    │   ├── Purchases/
    │   ├── Returns/
    │   ├── Sales/
    │   ├── Suppliers/
    │   ├── Transfers/
    │   ├── Units/
    │   └── Users/
    │
    ├── Services/
    │   ├── Api/                   ← HttpClient wrappers → API calls
    │   │   ├── IApiService.cs     ← All API interfaces in one file
    │   │   ├── AuthApiService.cs
    │   │   ├── ProductApiService.cs
    │   │   ├── SalesInvoiceApiService.cs
    │   │   ├── PurchaseInvoiceApiService.cs
    │   │   ├── SalesReturnApiService.cs
    │   │   ├── PurchaseReturnApiService.cs
    │   │   ├── InventoryApiService.cs
    │   │   ├── StockTransferApiService.cs
    │   │   ├── CustomerApiService.cs
    │   │   ├── SupplierWarehouseApiService.cs
    │   │   ├── CustomerPaymentApiService.cs
    │   │   ├── SupplierPaymentApiService.cs
    │   │   ├── ReportApiService.cs
    │   │   ├── DashboardApiService.cs
    │   │   ├── SettingsApiService.cs
    │   │   ├── CategoryApiService.cs
    │   │   ├── UnitApiService.cs
    │   │   ├── UserApiService.cs
    │   │   └── LogsApiService.cs
    │   └── App/                   ← App-level services (DI singletons)
    │       ├── EventBus.cs        ← Pub/Sub event bus
    │       ├── NavigationService.cs
    │       ├── SessionService.cs  ← JWT token storage (in-memory only)
    │       ├── DialogService.cs
    │       ├── ISoundService.cs
    │       ├── SoundService.cs
    │       └── IPrinterService.cs ← [NEW] SPEC-006 print contract
    │
    ├── Messaging/
    │   └── Messages/              ← EventBus message types (ID only, no payload)
    │
    ├── Converters/                ← WPF IValueConverter implementations
    ├── Helpers/                   ← UI helpers / ThemeHelper etc.
    ├── Models/                    ← Local view models / display models
    └── Resources/                 ← Styles, brushes, icons, themes
```


---

## 7. Implementation Phases

### Phase 1 — Foundation
Tasks:

Create Solution with 6 projects
Configure .NET 10 project references
Write all Domain Entities
Write all Contracts (DTOs, Requests, Result<T>)
Write all Custom Exceptions
Write SQL creation scripts with seed data
Configure SalesDbContext with all Configurations
Run initial EF Core Migration
Definition of Done:

Solution builds without errors
Database created from migration
Seed data inserted (admin user, default warehouse,
cash customer, units)
text


### Phase 2 — Backend Core
Tasks:

Implement IUnitOfWork and repositories
Implement AuthService with JWT
Implement ProductService (simplest — use as template)
Implement CustomerService, SupplierService
Implement WarehouseService
Implement DocumentSequenceService (thread-safe)
Wire up API Controllers (Products, Customers,
Suppliers, Warehouses, Auth)
Add FluentValidation for all request models
Add Serilog configuration
Add Global Exception Handler middleware
Add JWT authorization policies per role
Definition of Done:

All basic CRUD endpoints work via Swagger
Invalid requests rejected with clear error messages
All requests logged
Unauthorized requests return 401/403
text


### Phase 3 — Business Logic (Critical)
Tasks:

Implement InventoryService ⚠️
Implement PurchaseService ⚠️
Implement SalesService ⚠️
Implement SalesReturnService ⚠️
Implement PurchaseReturnService ⚠️
Implement StockTransferService ⚠️
Implement PaymentService
Wire up remaining API Controllers
Definition of Done:

Complete purchase flow works end-to-end
Complete sales flow works end-to-end
Stock deducted correctly on sale
Stock increased correctly on purchase
Returns reverse stock and balance correctly
All operations are transactional (rollback tested)
InventoryMovements populated on every stock change
text


### Phase 4 — Desktop Shell
Tasks:

Implement EventBus (with WeakReference and UI thread safety)
Implement NavigationService
Implement AuthApiService and token storage
Build MainForm with Sidebar
Build LoginForm
Implement role-based sidebar visibility
Build Common UserControls
(SearchBar, Loading, SummaryCard, PaymentPanel)
Implement NotificationService
Implement DialogService
Definition of Done:

Login works and retrieves JWT token
Role determines which sidebar items are visible
Navigation between screens works
Common controls render correctly
text


### Phase 5 — Desktop Modules
Order of implementation:

Products Module ← Use as template for others
Customers Module
Suppliers Module
Warehouses Module
Purchases Module ⚠️
Sales Module ⚠️ Most important
Returns Module ⚠️
Stock Transfer Module
Reports Module
Payments Module
Definition of Done per module:

List screen with search and filter
Add/Edit dialog
Delete/Deactivate with confirmation
EventBus messages published on changes
Other screens refresh automatically on change
text


### Phase 6 — Printing
Tasks:

Build InvoicePrinter class
Build ReceiptPrinter class (80mm thermal)
Support store logo in header
Support store name, phone, address in header
Support invoice details and totals
Print preview before printing
Definition of Done:

Full A4 invoice prints correctly
80mm receipt prints correctly
Logo appears in header when configured
text


### Phase 7 — Production Readiness
Tasks:

Implement BackupService (SQL Server .bak)
Build Settings screen (Admin only)
Build User Management screen (Admin only)
Configure API as Windows Service
Create Inno Setup installer script
Encrypt connection string
Final security review
Performance testing with 1 year of data
Definition of Done:

System installs on clean Windows machine
API starts automatically with Windows
Backup creates valid .bak file
Restore successfully restores data
All role restrictions working in both API and Desktop
text

### Phase 8 — Dynamic UOM & Costing (v4.3)
Tasks:
- Implement ProductUnit entity with per-unit pricing and conversion factors
- Implement UnitBarcode entity for multi-barcode support per product-unit
- Build UpdateProductPricingService with 3 costing strategies (WeightedAverage, LastPurchasePrice, SupplierPrice)
- Record all price/cost changes in ProductPriceHistory
- Enforce ProductMustHaveAtLeastOneUnit rule in Domain
- Add SmartUnitFormatter (UI-only) for quantity display
Definition of Done:
- Products support multiple units with per-unit pricing
- Purchase posting triggers cost recalculation
- All cost changes are audited in ProductPriceHistory
- Cascade cost updates from base unit to all derived units × conversion factor
text

### Phase 9 — Cash Boxes (v4.3)
Tasks:
- Implement CashBox and CashTransaction entities
- Link CashTransaction to invoices via CashBoxId
- Enforce CashBox.CurrentBalance >= 0 validation
- Implement cash transfer between boxes (dual transaction)
- Build DailyClosure computation
- Make CashTransactions immutable (offsetting entry for corrections)
Definition of Done:
- Cash box balance tracks correctly
- Invoice payments reference correct cash box
- Transfer between boxes creates two entries (out + in)
- Daily closure calculates correctly
text

### Phase 10 — Print Engine (v4.3)
Tasks:
- Implement QuestPDF A4 invoice document (RTL, logo, tax breakdown)
- Implement Win32 raw printing for 80mm thermal receipts (ESC/POS)
- Build EscPos command builder (no external NuGet for thermal)
- Desktop calls PrintController API for all printing (never direct)
- Add PdfPreviewWindow for A4 preview
- Store print settings in SystemSetting table (Category = "Print")
- Handle missing logo gracefully (null check)
Definition of Done:
- A4 PDF generates correctly with store details
- 80mm thermal receipt prints with Arabic text
- Preview shows before printing
- PrintResult returned (never throws exceptions)
text

### Phase 11 — Production Hardening (v4.4)
Tasks:
- Implement Auto-Update System (SHA256 verification, fire-and-forget, 8s timeout)
- Implement DPAPI connection string encryption (ConnectionStringProtector)
- Implement FirstRunSetupService with atomic file writes
- Build ScheduledBackupWorker (daily 2 AM, configurable retention)
- Implement BackupService (raw SQL, no SMO)
- Configure API as Windows Service (SalesSystemService, auto-recovery)
- Add Database Health Check endpoint (/health/database)
- Desktop startup check with DatabaseErrorDialog (Retry/Exit)
Definition of Done:
- Updates downloaded and verified via SHA256
- Connection strings encrypted with "DPAPI:" prefix
- Backups created and old backups auto-cleaned
- Windows Service starts with Windows, recovers on failure
- Desktop shows friendly dialog on DB connection loss
text

### Phase 12 — Multi-Window & UI Polish (v4.5)
Tasks:
- Implement ScreenWindowService for non-modal editor opening
- Build generic ScreenWindow.xaml (hosts any View/ViewModel)
- Use WeakReference<Window> for window tracking (no memory leaks)
- Implement cascade window positioning (30px × count % 10)
- Add EventBus subscription management (subscribe in OnLoad, dispose in Dispose)
- Implement newest-first sorting across all list screens
- Add Arabic ToolTips to all interactive controls
- Fix dialog self-ownership guard (PositionOverOwner)
Definition of Done:
- Editors open non-modally via ScreenWindowService
- No MessageBox.Show in ViewModels — all via IDialogService
- All buttons have Arabic ToolTips
- Lists display newest items first
text

### Phase 13 — Identifier Strategy & Validation (v4.5.3–v4.6.2)
Tasks:
- Remove Code columns from Product, Customer, Supplier, Warehouse entities
- Remove Code from all DTOs, ViewModels, and API responses
- Remove DocumentSequenceService for entity codes (keep only for invoices)
- Remove DuplicateCode from ErrorCodes (keep DuplicateBarcode)
- Implement WPF ErrorTemplate: Red border + ❗ icon with ToolTip
- Add INotifyDataErrorInfo standardization (no HasXxxError booleans)
- Add SetDialogService() + ValidateAllAsync() to ViewModelBase
- Update all 14 Editor VMs to use INotifyDataErrorInfo pattern
Definition of Done:
- No Code property exists anywhere in the system
- All entities use auto-increment Id as sole identifier
- Validation shows red border on invalid fields
- Warning dialog lists all missing fields on save click
text


---

### Phase 14 — Product Lifecycle & Media Management

**Tasks:**

1. **Database Schema Update (EF Core / SQL Server):**
* Add a nullable `ExpirationDate` (`DateTime?`) to the `Products` table to support products without an expiry.
* Add an optional `ImagePath` (`string?`) or `ImageSubPath` field to store the local reference of the product image.
* Create a `StockWriteOff` (جدول الإتلاف/المستبعد) table to log quantities removed due to expiration or damage, linked to the `JournalEntries` for automatic accounting impact.

2. **UI Enhancements (Product Management Screen - WPF):**
* Implement a CheckBox labeled "له تاريخ انتهاء" (Has Expiration Date).
* Dynamically enable/disable the `DatePicker` control based on the CheckBox state using MVVM binding.
* Add an optional Image Upload component (Image control, "اختيار صورة" button, and "حذف الصورة" button).
* Ensure image loading utilizes **Lazy Loading** or async drawing to prevent the main product list Grid from lagging during scroll.

3. **Backend Logic & Guard Clauses:**
* Enforce validation: If "Has Expiration Date" is checked, the `ExpirationDate` must be provided and cannot be a past date during the initial stock entry.
* Implement image validation restricting files to standard formats (JPG, PNG) and limiting file size (e.g., max 2MB) before saving.

4. **Expired & Damaged Products Report:**
* Build a dedicated reporting view with advanced data filtering:
* *Expired Items:* Products whose expiration date is less than or equal to the current system date.
* *Near-Expiry Items:* Products expiring within an adjustable threshold (e.g., next 30, 60, or 90 days) for proactive inventory control.

* Add a "ترحيل كحذف/إتلاف" button inside the report to clear expired quantities from the active inventory and trigger a background accounting entry (قيد آلية: من حـ/ خسائر بضاعة تالفة إلى حـ/ المخزون).

**Definition of Done:**

* Products can be created and updated smoothly with or without an expiration date.
* Optional images render correctly in the UI without impacting application memory or rendering speed.
* The Expired Products Report displays accurate real-time data based on the system clock.
* Writing off expired stock decreases the available warehouse inventory immediately and reflects in the backend financial logs.

---

### 💡 توجيهات معمارية هامة لك قبل البدء في تطبيق هذه المرحلة:

بما أنك تبني نظاماً تجارياً احترافياً ومستقراً، يرجى مراعاة النمذجة التالية أثناء توجيه المبرمجين أو الوكلاء لتنفيذ هذه المهام:

1. **معالجة الصور (Image Storage Strategy):**
* **احذر** من حفظ الصور كـ `byte[]` (BLOB) مباشرة داخل قاعدة بيانات SQL Server، لأن هذا سيجعل حجم ملف الـ `.bak` ضخماً جداً ويتسبب في بطء شديد أثناء النسخ الاحتياطي (Backup) والاسترجاع في المرحلة 7.
* **الأفضل برمجياً:** حفظ الصورة في مجلد محلي داخل مسار النظام (مثلاً `%AppData%\SalesSystem\Images`) وحفظ **مسار الملف (String Path)** فقط في قاعدة البيانات.

2. **محرك التنبيهات التلقائي (Proactive UX):**
* بما أنك تركز على جعل النظام "يشرح نفسه للمستخدم بكل مرونة وسلاسة"، اجعل النظام يقوم بفحص التواريخ تلقائياً عند فتح شاشة النظام الرئيسية (Dashboard) في بداية اليوم.
* إذا وجد النظام منتجات منتهية أو تشرف على الانتهاء، يعرض تنبيهاً علوياً خفيفاً (Badge/Notification) دون إزعاج المستخدم، لكي يتحرك التاجر ويتخذ إجراءً سريعاً قبل تكبد خسائر مالية.

---

### Phase 15 — Touch-Optimized Quick POS Interface (Restaurant-Style Layout)

**Tasks:**

1. **UI/UX Architecture (Dual-Mode Sales Interface):**
* Implement a switchable view mechanism within `SalesView.xaml` allowing the user to toggle between "المظهر القياسي (Retail Grid)" و "البيع السريع (Touch POS Layout)".
* Apply a fully responsive **RTL Layout** customized for rapid screen interactions:
* **الجانب الأيمن (Right Panel):** يحتوي على شجرة الفئات والأصناف مصفوفة كأزرار كبيرة (Tiles).
* **الجانب الأيسر (Left Panel):** يحتوي على سلة البيع الحالية (Active Cart Grid) مع مجاميع الفاتورة وأزرار الدفع السريع في الأسفل.

2. **Dynamic Categories & Items Component (WPF Layout):**
* **Category Selector:** Use an `ItemsControl` bound to a `WrapPanel` or `UniformGrid` to display Product Categories as large, touch-friendly styled buttons. Clicking a category smoothly loads its corresponding products.
* **Product Grid:** Displays products belonging to the selected category. Each product button must support:
* Displaying the Product Name and Price clearly.
* Rendering the optional product image (from Phase 8) as a background or icon with lazy loading.
* Clicking/Touching the product button instantly executes an `AddToCartCommand` adding the item to the invoice lines (or incrementing its quantity if already present) without closing any menus.

3. **In-Line Fast Cart Management:**
* Redesign the active cart lines list using a lightweight DataGrid or customized ListView template.
* Every invoice item line must feature immediate action buttons directly on the row:
* `+` and `-` buttons to modify quantity instantly.
* A quick delete/remove icon.
* All total calculations (Total, Tax/VAT, Grand Total) must update in real-time instantly in the ViewModel via `RaisePropertyChanged` upon any quantity or item change.

4. **Quick Checkout & On-Screen Numeric Keypad:**
* Integrate an optional on-screen numeric keypad for touch-screen monitors to allow fast entry of paid amounts.
* Add dedicated, one-click action buttons at the bottom of the invoice:
* **[ كاش / Cash ]:** ترحيل فوري وطباعة الفاتورة مباشرة.
* **[ شبكة / Card ]:** ترحيل الفاتورة كدفع إلكتروني.
* **[ حفظ كمسودة / Draft ]:** تعليق الفاتورة لخدمة العملاء المترددين والعودة لها لاحقاً.

5. **Backend & Shared ViewModel Integration:**
* Ensure this new Touch UI reuses the EXACT same core Business Logic, Entities, and Application commands (`CreateInvoiceCommand`, `DraftInvoiceCommand`) used by the standard screen to enforce Clean Architecture principles and avoid duplication of validation rules.

**Definition of Done:**

* The user can toggle between the Retail Grid view and the Touch POS view seamlessly without losing current cart data.
* Clicking a product button adds it to the active invoice in less than 50 milliseconds (No UI lag).
* The layout adapts correctly to standard touch-screen monitors without any element overlapping or overflowing.
* Executing a quick checkout validates the invoice locally (using Client-Side Validation) and saves/posts the data correctly to the database.

---

### 💡 توجيهات معمارية لإصدار الـ MVP:

عندما يبدأ الوكلاء الذكيون (Agents) في كتابة كود هذه الشاشة، وجههم للالتزام بالآتي لضمان أعلى أداء (Performance):

1. **الابتعاد عن الأبعاد الثابتة (No Hardcoded Sizes):**
يجب أن تُبنى أزرار المنتجات والفئات داخل `UniformGrid` أو `WrapPanel` مع ضبط الـ `Width` والـ `Height` لتكون ديناميكية أو تعتمد على نسبة مئوية، لكي يتكيف حجم الأزرار تلقائياً سواء كان العميل يعرض النظام على شاشة كاشير صغيرة (15 بوصة) أو شاشة حاسوب ضخمة.
2. **استغلال الـ Virtualization:**
إذا كان لدى العميل فئة تحتوي على مئات الأصناف، فإن رندرة (Rendering) مئات الأزرار دفعة واحدة سيسبب بطء في الواجهة. يجب تفعيل خاصية `VirtualizingStackPanel.IsVirtualizing="True"` لضمان سرعة استجابة الشاشة وثبات الذاكرة (Memory) أثناء التنقل الفوري بين الفئات.

### Phase 16 — Critical Business Rules Reference
SALES FLOW:

Validate all items have sufficient stock in warehouse
Generate invoice number (thread-safe)
BEGIN TRANSACTION
Create invoice (Draft status)
Add all items (Domain calculates LineTotal)
Domain validates: PaidAmount <= TotalAmount
Post invoice (status = Posted)
Save invoice to DB (get invoice ID)
For each item: decrease WarehouseStock
For each item: create InventoryMovement record
If DueAmount > 0: increase Customer.CurrentBalance
COMMIT TRANSACTION
→ Any failure: ROLLBACK ALL
PURCHASE FLOW: (mirror of sales with suppliers)
RETURN FLOW: (reverse of original operation)
TRANSFER FLOW: (decrease source, increase destination — same transaction)
PAYMENT FLOW: (decrease balance, no stock change)

---

### Phase 17 — Collapsible Tree Sidebar Navigation

تحويل القائمة الجانبية (Sidebar) إلى قائمة شجرية منسدلة أو ممتدة (Collapsible/Accordion Menu) هو الحل المعياري والمثالي للأنظمة الكبيرة. هذا التصميم يمنح النظام مظهراً رسمياً ومحترفاً يشبه مواقع الويب الحديثة، ويحل مشكلة تكدس الأزرار مع نمو النظام وإضافة ميزات جديدة.

في واجهات المستخدم الاحترافية، نقوم بهيكلة هذه القائمة إلى مستويين (Two Levels) لضمان عدم تشتيت المستخدم:

1. **المستوى الأول (Main Modules):** المجموعات الرئيسية وتكون مصحوبة بأيقونة معبرة وسهم يشير لحالة القائمة (مفتوحة/مغلقة).
2. **المستوى الثاني (Sub-Items / Screens):** الشاشات التفصيلية التي تظهر فقط عندما يضغط المستخدم على المجموعة الرئيسية.

إليك التصميم الهيكلي للقائمة الجانبية متوافقاً مع اتجاه القراءة العربي (RTL - من اليمين إلى اليسار):

#### 1. الهيكل البصري للقائمة (RTL Sidebar Structure)

```text
[شعار النظام أو اسم المؤسسة]
-----------------------------------------
🔻 [أيقونة] المبيعات والتوزيع
      ▪️ شاشة البيع السريع (POS)
      ▪️ الفواتير المعلقة (المسودات)
      ▪️ إدارة المرتجعات
🔻 [أيقونة] المشتريات والموردين
      ▪️ فاتورة مشتريات جديدة
      ▪️ إدارة الموردين
🔻 [أيقونة] الحسابات والمالية
      ▪️ القيود اليومية
      ▪️ شجرة الحسابات
🔻 [أيقونة] التقارير والتحليلات
      ▪️ التقارير التشغيلية
      ▪️ الأرباح والخسائر
      ▪️ تقارير العملاء التفصيلية
🔻 [أيقونة] الإعدادات والتهيئة
      ▪️ إعدادات النظام
      ▪️ صلاحيات المستخدمين
```

#### 2. التنفيذ البرمجي النظيف (WPF XAML Example)

إذا كنت تبني الواجهات باستخدام **WPF**، فإن أفضل وأسهل أداة برمجية تحقق مظهر الويب المرن دون تعقيد هي أداة **`Expander`** المضمنة داخل `StackPanel` أو `ScrollViewer`. تتيح لك هذه الأداة فتح وإغلاق المجموعات تلقائياً وحفظ المساحة.

إليك كود XAML نظيف ورسمي يدعم الـ RTL:

```xml
<Grid FlowDirection="RightToLeft" Background="#F8F9FA">
    <ScrollViewer VerticalScrollBarVisibility="Auto" Width="260" HorizontalAlignment="Right">
        <StackPanel Background="#1E293B"> <Border Padding="20" Background="#0F172A">
                <TextBlock Text="نظام إدارة المبيعات" Foreground="White" FontSize="16" FontWeight="Bold" HorizontalAlignment="Center"/>
            </Border>

            <Expander Header="المبيعات والتوزيع" Foreground="White" FontSize="14" Margin="5" IsExpanded="True">
                <StackPanel Background="#334155" Margin="0,5,0,0">
                    <Button Content="شاشة البيع السريع" Style="{StaticResource SidebarSubMenuButtonStyle}"/>
                    <Button Content="الفواتير المعلقة (Drafts)" Style="{StaticResource SidebarSubMenuButtonStyle}"/>
                    <Button Content="إدارة المرتجعات" Style="{StaticResource SidebarSubMenuButtonStyle}"/>
                </StackPanel>
            </Expander>

            <Expander Header="الحسابات والمالية" Foreground="White" FontSize="14" Margin="5">
                <StackPanel Background="#334155" Margin="0,5,0,0">
                    <Button Content="القيود اليومية" Style="{StaticResource SidebarSubMenuButtonStyle}"/>
                    <Button Content="شجرة الحسابات" Style="{StaticResource SidebarSubMenuButtonStyle}"/>
                </StackPanel>
            </Expander>

            <Expander Header="التقارير والتحليلات" Foreground="White" FontSize="14" Margin="5">
                <StackPanel Background="#334155" Margin="0,5,0,0">
                    <Button Content="التقرير المالي العام" Style="{StaticResource SidebarSubMenuButtonStyle}"/>
                    <Button Content="ربحية العملاء" Style="{StaticResource SidebarSubMenuButtonStyle}"/>
                </StackPanel>
            </Expander>

        </StackPanel>
    </ScrollViewer>
</Grid>
```

*(ملاحظة: الستايل `SidebarSubMenuButtonStyle` نقوم فيه بإلغاء الحواف وجعل الخلفية شفافة لتبدو الأزرار الفرعية كنصوص أنيقة يتم النقر عليها، مع إزاحة خفيفة جهة اليمين Padding لإعطاء انطباع التبعية للمجموعة الرئيسية).*

#### 3. ربط القائمة مع هندسة النظام (Navigation Pattern)

لكي تظل معمارية الكود نظيفة (Clean Architecture) ومتوافقة مع نمط MVVM، لا تقم بكتابة كود فتح الشاشات داخل أحداث النقر المباشر (Click Events) في الواجهة. بدلاً من ذلك، استخدم **`NavigationService`** أو **`MainViewModel`** لإدارة تبديل الشاشات:

1. كل زر فرعي يتم ربطه بأمر `Command` يمرر نوع الشاشة المستهدفة كمعامل (Parameter).
2. الشاشة الرئيسية تحتوي على منطقة مخصصة لعرض المحتوى النشط `ContentControl`.
3. عند الضغط على زر فرعي، يتغير الـ `CurrentViewModel` داخل الـ `ContentControl` ليتم رسم الشاشة الجديدة فوراً في المساحة البيضاء المتبقية من التطبيق دون إعادة تحميل القائمة الجانبية.

هذا التصميم يضمن لك مرونة تشغيلية لا نهائية؛ فمهما أضفت من موديولات أو شاشات مستقبلاً، كل ما عليك فعله هو إضافة سطر جديد داخل الـ `Expander` المناسب، وسيتكفل النظام بالباقي دون أي تداخل في الكود.

---

### Phase 18 — Accounting Foundation Implementation Plan

يرجى مراجعة ملف المواصفات وخطة التنفيذ المخصص لهذه المرحلة (بناء الهيكل المحاسبي المركزي):
[Phase 18 — Accounting Foundation Implementation Plan](./Phase%2018%20%E2%80%94%20Accounting%20Foundation%20Implementation%20Plan.md)


Sales Management System — PRD v4.6.2 (النسخة النهائية الشاملة)
1. معلومات المشروع
text

الاسم:     Sales Management System
الإصدار:   v4.6.2
التاريخ:   2026
المعمارية: Clean Architecture + WPF Desktop (MVVM) + ASP.NET Core API
قاعدة البيانات: SQL Server
2. وصف المشروع
نظام إدارة مبيعات لمحل صغير يعمل على Desktop محلياً عبر ASP.NET Core Web API وقاعدة بيانات SQL Server. يشمل إدارة المنتجات، المخازن المتع5. قاعدة البيانات — Schema الكامل
(Updated to match docs/database-schema.md)


<!-- END OF SECTION: 1. Functional & Non-Functional Requirements (English) -->


<!-- START OF SECTION: 2. Detailed Module Specifications & Database Metadata (Arabic/Bilingual) -->

> [!NOTE]
> المواصفات التفصيلية للجداول وهيكل قاعدة البيانات ومخطط العلاقات والأعمدة للمشروع.

# 1) Important Design Rules Before Tables
These rules will save you many problems later:

## Data Types
- **Primary Keys**: `int IDENTITY(1,1)` — Named `Id` in all entities (BaseEntity pattern).
- **Texts**: `nvarchar` (to support Arabic/English).
- **Currency/Money**: `decimal(18,2)` — Precision 18, Scale 2.
- **Quantities**: `decimal(18,3)` — Precision 18, Scale 3.
- **Status and Types**: `tinyint` (for Enums).
- **Flags**: `bit` (0/1).
- **Dates and Times**: `datetime2`.
- **Audit Tracking**: Standardized across all entities:
    - `CreatedAt` datetime2
    - `CreatedByUserId` int null (FK to Users.Id)
    - `UpdatedAt` datetime2 null
    - `UpdatedByUserId` int null (FK to Users.Id)
    - `IsActive` bit (Soft delete flag)

---

# 2) Proposed Database
Database Name example: **`SalesSystemDb`**
Default schema: **`dbo`**

---

# 3) Core Tables

---

## A) Users
### Columns
- `Id` int PK
- `UserName` nvarchar(50) not null unique
- `PasswordHash` nvarchar(256) not null
- `FullName` nvarchar(150) not null
- `Role` tinyint not null (1=Admin, 2=Manager, 3=Cashier)
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## B) Units
### Columns
- `Id` int PK
- `Name` nvarchar(50) not null
- `Symbol` nvarchar(20) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## C) Categories
### Columns
- `Id` int PK
- `Name` nvarchar(100) not null
- `Description` nvarchar(250) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## D) Products (v4.6 — No Code, uses ProductUnits)
### Columns
- `Id` int PK
- `Barcode` nvarchar(50) null unique
- `Name` nvarchar(150) not null
- `CategoryId` int null FK
- `SupplierPrice` decimal(18,2) not null default 0
- `ReorderLevel` decimal(18,3) not null default 0
- `Description` nvarchar(500) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## E) Warehouses (v4.6 — No Code)
### Columns
- `Id` int PK
- `Name` nvarchar(100) not null
- `Location` nvarchar(250) null
- `IsDefault` bit not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## F) WarehouseStocks
### Columns
- `Id` int PK
- `WarehouseId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 not null

### Important Constraints
- `UNIQUE(WarehouseId, ProductId)`
- `CHECK (Quantity >= 0)` — CRITICAL: prevents negative stock at DB level

---

## G) Suppliers (v4.6 — No Code)
### Columns
- `Id` int PK
- `Name` nvarchar(150) not null
- `Phone` nvarchar(20) null
- `Email` nvarchar(100) null
- `Address` nvarchar(250) null
- `OpeningBalance` decimal(18,2) not null default 0
- `CurrentBalance` decimal(18,2) not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## H) Customers (v4.6 — No Code)
### Columns
- `Id` int PK
- `Name` nvarchar(150) not null
- `Phone` nvarchar(20) null
- `Email` nvarchar(100) null
- `Address` nvarchar(250) null
- `OpeningBalance` decimal(18,2) not null default 0
- `CurrentBalance` decimal(18,2) not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

# 4) Purchases

## I) PurchaseInvoices
### Columns
- `Id` int PK
- `SupplierId` int not null FK
- `WarehouseId` int not null FK
- `InvoiceDate` datetime2 not null
- `DueDate` date null
- `PaymentType` tinyint not null (1=Cash, 2=Credit, 3=Mixed)
- `SubTotal` decimal(18,2) not null default 0
- `DiscountAmount` decimal(18,2) not null default 0
- `TaxAmount` decimal(18,2) not null default 0
- `TotalAmount` decimal(18,2) not null default 0
- `PaidAmount` decimal(18,2) not null default 0
- `DueAmount` decimal(18,2) not null default 0
- `Notes` nvarchar(500) null
- `Status` tinyint not null (1=Draft, 2=Posted, 3=Cancelled)
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## J) PurchaseInvoiceItems
### Columns
- `PurchaseInvoiceItemId` int PK
- `PurchaseInvoiceId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null
- `UnitCost` decimal(18,2) not null
- `DiscountAmount` decimal(18,2) not null default 0
- `LineTotal` decimal(18,2) not null
- `Notes` nvarchar(250) null

---

# 5) Sales

## K) SalesInvoices
### Columns
- `Id` int PK
- `CustomerId` int null FK
- `WarehouseId` int not null FK
- `InvoiceDate` datetime2 not null
- `DueDate` date null
- `PaymentType` tinyint not null (1=Cash, 2=Credit, 3=Mixed)
- `SubTotal` decimal(18,2) not null default 0
- `DiscountAmount` decimal(18,2) not null default 0
- `TaxAmount` decimal(18,2) not null default 0
- `TotalAmount` decimal(18,2) not null default 0
- `PaidAmount` decimal(18,2) not null default 0
- `DueAmount` decimal(18,2) not null default 0
- `Notes` nvarchar(500) null
- `Status` tinyint not null (1=Draft, 2=Posted, 3=Cancelled)
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## L) SalesInvoiceItems
### Columns
- `SalesInvoiceItemId` int PK
- `SalesInvoiceId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null
- `UnitPrice` decimal(18,2) not null
- `DiscountAmount` decimal(18,2) not null default 0
- `LineTotal` decimal(18,2) not null
- `Notes` nvarchar(250) null

---

# 6) Returns

## M) PurchaseReturns
### Columns
- `Id` int PK
- `ReturnNo` nvarchar(30) not null unique
- `PurchaseInvoiceId` int null FK
- `SupplierId` int not null FK
- `WarehouseId` int not null FK
- `ReturnDate` datetime2 not null
- `Reason` nvarchar(250) null
- `SubTotal` decimal(18,2) not null default 0
- `TotalAmount` decimal(18,2) not null default 0
- `Status` tinyint not null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## N) PurchaseReturnItems
### Columns
- `PurchaseReturnItemId` int PK
- `PurchaseReturnId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null
- `UnitCost` decimal(18,2) not null
- `LineTotal` decimal(18,2) not null

---

## O) SalesReturns
### Columns
- `Id` int PK
- `ReturnNo` nvarchar(30) not null unique
- `SalesInvoiceId` int null FK
- `CustomerId` int not null FK
- `WarehouseId` int not null FK
- `ReturnDate` datetime2 not null
- `Reason` nvarchar(250) null
- `SubTotal` decimal(18,2) not null default 0
- `TotalAmount` decimal(18,2) not null default 0
- `Status` tinyint not null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## P) SalesReturnItems
### Columns
- `SalesReturnItemId` int PK
- `SalesReturnId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null
- `UnitPrice` decimal(18,2) not null
- `LineTotal` decimal(18,2) not null

---

# 7) Stock Transfer Between Warehouses

## Q) StockTransfers
### Columns
- `Id` int PK
- `TransferNo` nvarchar(30) not null unique
- `FromWarehouseId` int not null FK
- `ToWarehouseId` int not null FK
- `TransferDate` datetime2 not null
- `Notes` nvarchar(500) null
- `Status` tinyint not null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## R) StockTransferItems
### Columns
- `StockTransferItemId` int PK
- `StockTransferId` int not null FK
- `ProductId` int not null FK
- `Quantity` decimal(18,3) not null
- `Notes` nvarchar(250) null

---

# 8) Payments and Collections

## S) CustomerPayments
### Columns
- `Id` int PK
- `PaymentNo` nvarchar(30) not null unique
- `CustomerId` int not null FK
- `SalesInvoiceId` int null FK
- `PaymentDate` datetime2 not null
- `Amount` decimal(18,2) not null
- `PaymentMethod` tinyint not null (1=Cash, 2=Bank Transfer, 3=Card, 4=Other)
- `ReferenceNo` nvarchar(50) null
- `Notes` nvarchar(500) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## T) SupplierPayments
### Columns
- `Id` int PK
- `PaymentNo` nvarchar(30) not null unique
- `SupplierId` int not null FK
- `PurchaseInvoiceId` int null FK
- `PaymentDate` datetime2 not null
- `Amount` decimal(18,2) not null
- `PaymentMethod` tinyint not null
- `ReferenceNo` nvarchar(50) null
- `Notes` nvarchar(500) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

# 9) Store Settings

## U) StoreSettings
### Columns
- `Id` int PK
- `StoreName` nvarchar(150) not null
- `Phone` nvarchar(20) null
- `Address` nvarchar(250) null
- `LogoPath` nvarchar(255) null
- `CurrencyCode` nvarchar(10) not null default 'SAR'
- `DefaultTaxRate` decimal(5,2) not null default 0
- `IsTaxEnabled` bit not null default 0
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

# 10) Inventory Movement Log (Critical)
## V) InventoryMovements
### Columns
- `Id` int PK
- `ProductId` int not null FK
- `WarehouseId` int not null FK
- `MovementType` tinyint not null (1=PurchaseIn, 2=SaleOut, 3=SaleReturnIn, 4=PurchaseReturnOut, 5=TransferOut, 6=TransferIn, 7=Adjustment)
- `QuantityChange` decimal(18,3) not null — positive=IN, negative=OUT
- `QuantityBefore` decimal(18,3) not null — stock before this movement
- `QuantityAfter` decimal(18,3) not null — stock after (= Before + Change)
- `ReferenceType` nvarchar(30) not null
- `ReferenceId` int not null
- `UnitCost` decimal(18,2) null
- `MovementDate` datetime2 not null
- `Notes` nvarchar(500) null
- `CreatedByUserId` int null FK
- `UpdatedByUserId` int null FK
- `IsActive` bit not null default 1
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## W) ProductUnits (v4.3 — Dynamic UOM)
### Columns
- `Id` int PK
- `ProductId` int not null FK
- `UnitName` nvarchar(50) not null (e.g., "Piece", "Box", "Carton")
- `ConversionFactor` decimal(18,3) not null default 1 (Base=1, Box=24, Carton=144)
- `RetailPrice` decimal(18,2) not null default 0
- `WholesalePrice` decimal(18,2) not null default 0
- `Cost` decimal(18,2) not null default 0 — updated via cost cascade
- `IsBaseUnit` bit not null default 0 (exactly one per product)
- `IsActive` bit not null default 1
- (audit fields)
### Constraints
- `UNIQUE(ProductId, UnitName)` — no duplicate unit names per product
- Product must have at least one unit (Domain enforced)

---

## X) UnitBarcodes (v4.3)
### Columns
- `Id` int PK
- `ProductUnitId` int not null FK
- `Barcode` nvarchar(50) not null
- `IsActive` bit not null default 1
- (audit fields)
### Constraints
- `UNIQUE(Barcode)` — global barcode uniqueness

---

## Y) CashBoxes (v4.3)
### Columns
- `Id` int PK
- `Name` nvarchar(100) not null
- `OpeningBalance` decimal(18,2) not null default 0
- `CurrentBalance` decimal(18,2) not null default 0
- `IsDefault` bit not null default 0
- `IsActive` bit not null default 1
- (audit fields)

---

## Z) CashTransactions (v4.3 — Immutable)
### Columns
- `Id` int PK
- `CashBoxId` int not null FK
- `TransactionType` tinyint not null (1=Opening, 2=SalesIncome, 3=Expense, 4=TransferOut, 5=TransferIn, 6=RefundOut, 7=SupplierPayment, 8=CustomerPayment)
- `Amount` decimal(18,2) not null
- `BalanceBefore` decimal(18,2) not null
- `BalanceAfter` decimal(18,2) not null
- `ReferenceType` nvarchar(30) null (e.g., "SalesInvoice", "Expense")
- `ReferenceId` int null
- `Notes` nvarchar(500) null
- `CreatedByUserId` int null FK
- `CreatedAt` datetime2 not null
### Rules
- Immutable — once created, never edited or deleted
- Cancellations via offsetting entry only

---

## AA) ProductPriceHistory (v4.3)
### Columns
- `Id` int PK
- `ProductUnitId` int not null FK
- `OldRetailPrice` decimal(18,2) null
- `NewRetailPrice` decimal(18,2) null
- `OldWholesalePrice` decimal(18,2) null
- `NewWholesalePrice` decimal(18,2) null
- `OldCost` decimal(18,2) null
- `NewCost` decimal(18,2) null
- `ChangedByUserId` int null FK
- `ChangeReason` nvarchar(200) null
- `CreatedAt` datetime2 not null

---

## BB) SystemSettings (v4.3)
### Columns
- `Id` int PK
- `Key` nvarchar(100) not null unique
- `Value` nvarchar(500) null
- `Category` nvarchar(50) null (e.g., "Print", "General", "Costing")
- `Description` nvarchar(250) null
- `CreatedAt` datetime2 not null
- `UpdatedAt` datetime2 null

---

## CC) SystemLog (v4.3)
### Columns
- `Id` int PK
- `Level` nvarchar(20) not null (Information, Warning, Error)
- `Message` nvarchar(4000) not null
- `Exception` nvarchar(max) null
- `UserId` int null FK
- `Source` nvarchar(200) null
- `CreatedAt` datetime2 not null

---


<!-- END OF SECTION: 2. Detailed Module Specifications & Database Metadata (Arabic/Bilingual) -->


# 3. Consolidated Database SQL Scripts

This section consolidates both the baseline SQL Server schema creation script and the advanced modules (v4.3+) database migration schema.

<!-- START OF SECTION: 3.1 Baseline Database Schema SQL Script -->

> [!NOTE]
> Baseline database tables creation and initial constraints.

# 11) Full SQL Server Implementation Script

```sql
IF DB_ID(N'SalesSystemDb') IS NULL
BEGIN
    CREATE DATABASE SalesSystemDb;
END
GO

USE SalesSystemDb;
GO

-- 1. Users
CREATE TABLE dbo.Users
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    UserName        NVARCHAR(50)  NOT NULL,
    PasswordHash    NVARCHAR(256) NOT NULL,
    FullName        NVARCHAR(150) NOT NULL,
    Role            TINYINT       NOT NULL, -- 1=Admin, 2=Manager, 3=Cashier
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_Users_UserName UNIQUE (UserName),
    CONSTRAINT CK_Users_Role CHECK (Role IN (1,2,3))
);

-- 2. Units
CREATE TABLE dbo.Units
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Units PRIMARY KEY,
    Name            NVARCHAR(50)  NOT NULL,
    Symbol          NVARCHAR(20)  NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Units_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Units_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 3. Categories
CREATE TABLE dbo.Categories
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL,
    Description     NVARCHAR(250) NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 4. Warehouses (v4.6 — No Code)
CREATE TABLE dbo.Warehouses
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Warehouses PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL,
    Location        NVARCHAR(250) NULL,
    IsDefault       BIT           NOT NULL CONSTRAINT DF_Warehouses_IsDefault DEFAULT(0),
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Warehouses_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Warehouses_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 5. Products (v4.6 — No Code, uses ProductUnits)
CREATE TABLE dbo.Products
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Barcode         NVARCHAR(50)  NULL,
    Name            NVARCHAR(150) NOT NULL,
    CategoryId      INT           NULL REFERENCES dbo.Categories(Id),
    SupplierPrice   DECIMAL(18,2) NOT NULL DEFAULT 0,
    ReorderLevel    DECIMAL(18,3) NOT NULL DEFAULT 0,
    Description     NVARCHAR(500) NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL,

    CONSTRAINT UQ_Products_Barcode UNIQUE (Barcode)
);

-- 6. WarehouseStocks
CREATE TABLE dbo.WarehouseStocks
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WarehouseStocks PRIMARY KEY,
    WarehouseId     INT NOT NULL REFERENCES dbo.Warehouses(Id),
    ProductId       INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity        DECIMAL(18,3) NOT NULL DEFAULT 0,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_WarehouseStocks_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_WarehouseStocks_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NOT NULL CONSTRAINT DF_WarehouseStocks_UpdatedAt DEFAULT(SYSDATETIME()),

    CONSTRAINT UQ_WarehouseStocks_Warehouse_Product UNIQUE (WarehouseId, ProductId),
    CONSTRAINT CK_WarehouseStocks_Qty CHECK (Quantity >= 0)
);

-- 7. Suppliers (v4.6 — No Code)
CREATE TABLE dbo.Suppliers
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suppliers PRIMARY KEY,
    Name            NVARCHAR(150) NOT NULL,
    Phone           NVARCHAR(20)  NULL,
    Email           NVARCHAR(100) NULL,
    Address         NVARCHAR(250) NULL,
    OpeningBalance  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CurrentBalance  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Suppliers_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 8. Customers (v4.6 — No Code)
CREATE TABLE dbo.Customers
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    Name            NVARCHAR(150) NOT NULL,
    Phone           NVARCHAR(20)  NULL,
    Email           NVARCHAR(100) NULL,
    Address         NVARCHAR(250) NULL,
    OpeningBalance  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CurrentBalance  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 9. PurchaseInvoices
CREATE TABLE dbo.PurchaseInvoices
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseInvoices PRIMARY KEY,
    SupplierId      INT           NOT NULL REFERENCES dbo.Suppliers(Id),
    WarehouseId     INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    InvoiceDate     DATETIME2     NOT NULL,
    DueDate         DATE          NULL,
    PaymentType     TINYINT       NOT NULL,
    SubTotal        DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount  DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount     DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
    DueAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    Notes           NVARCHAR(500) NULL,
    Status          TINYINT       NOT NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2     NULL
);

-- 10. PurchaseInvoiceItems
CREATE TABLE dbo.PurchaseInvoiceItems
(
    PurchaseInvoiceItemId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseInvoiceItems PRIMARY KEY,
    PurchaseInvoiceId       INT NOT NULL REFERENCES dbo.PurchaseInvoices(Id),
    ProductId               INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity                DECIMAL(18,3) NOT NULL,
    UnitCost                DECIMAL(18,2) NOT NULL,
    DiscountAmount          DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineTotal               DECIMAL(18,2) NOT NULL
);

-- 11. SalesInvoices
CREATE TABLE dbo.SalesInvoices
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesInvoices PRIMARY KEY,
    CustomerId      INT           NULL REFERENCES dbo.Customers(Id),
    WarehouseId     INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    InvoiceDate     DATETIME2     NOT NULL,
    DueDate         DATE          NULL,
    PaymentType     TINYINT       NOT NULL,
    SubTotal        DECIMAL(18,2) NOT NULL DEFAULT 0,
    DiscountAmount  DECIMAL(18,2) NOT NULL DEFAULT 0,
    TaxAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount     DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount      DECIMAL(18,2) NOT NULL DEFAULT 0,
    DueAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    Notes           NVARCHAR(500) NULL,
    Status          TINYINT       NOT NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2     NULL
);

-- 12. SalesInvoiceItems
CREATE TABLE dbo.SalesInvoiceItems
(
    SalesInvoiceItemId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesInvoiceItems PRIMARY KEY,
    SalesInvoiceId       INT NOT NULL REFERENCES dbo.SalesInvoices(Id),
    ProductId            INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity             DECIMAL(18,3) NOT NULL,
    UnitPrice            DECIMAL(18,2) NOT NULL,
    DiscountAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    LineTotal            DECIMAL(18,2) NOT NULL
);

-- 13. InventoryMovements
CREATE TABLE dbo.InventoryMovements
(
    Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryMovements PRIMARY KEY,
    ProductId           INT NOT NULL REFERENCES dbo.Products(Id),
    WarehouseId         INT NOT NULL REFERENCES dbo.Warehouses(Id),
    MovementType        TINYINT NOT NULL,
    QuantityChange      DECIMAL(18,3) NOT NULL,
    QuantityBefore      DECIMAL(18,3) NOT NULL,
    QuantityAfter       DECIMAL(18,3) NOT NULL,
    ReferenceType       NVARCHAR(30) NOT NULL,
    ReferenceId         INT NOT NULL,
    UnitCost            DECIMAL(18,2) NULL,
    MovementDate        DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    Notes               NVARCHAR(500) NULL,
    CreatedByUserId     INT NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId     INT NULL REFERENCES dbo.Users(Id),
    IsActive            BIT NOT NULL DEFAULT(1),
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt           DATETIME2 NULL
);

CREATE INDEX IX_InventoryMovements_Product ON dbo.InventoryMovements(ProductId, MovementDate DESC);
CREATE INDEX IX_InventoryMovements_Reference ON dbo.InventoryMovements(ReferenceType, ReferenceId);

-- 14. StoreSettings
CREATE TABLE dbo.StoreSettings
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StoreSettings PRIMARY KEY,
    StoreName       NVARCHAR(150) NOT NULL,
    Phone           NVARCHAR(20)  NULL,
    Address         NVARCHAR(250) NULL,
    LogoPath        NVARCHAR(255) NULL,
    CurrencyCode    NVARCHAR(10)  NOT NULL CONSTRAINT DF_StoreSettings_Currency DEFAULT(N'SAR'),
    DefaultTaxRate  DECIMAL(5,2)  NOT NULL CONSTRAINT DF_StoreSettings_TaxRate DEFAULT(0),
    IsTaxEnabled    BIT           NOT NULL DEFAULT(0),
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL CONSTRAINT DF_StoreSettings_IsActive DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL CONSTRAINT DF_StoreSettings_CreatedAt DEFAULT(SYSDATETIME()),
    UpdatedAt       DATETIME2     NULL
);

-- 15. PurchaseReturns
CREATE TABLE dbo.PurchaseReturns
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseReturns PRIMARY KEY,
    ReturnNo          NVARCHAR(30)  NOT NULL UNIQUE,
    PurchaseInvoiceId INT           NULL REFERENCES dbo.PurchaseInvoices(Id),
    SupplierId        INT           NOT NULL REFERENCES dbo.Suppliers(Id),
    WarehouseId       INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    ReturnDate        DATETIME2     NOT NULL,
    Reason            NVARCHAR(250) NULL,
    SubTotal          DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status            TINYINT       NOT NULL, -- 1=Draft, 2=Posted, 3=Cancelled
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    IsActive          BIT           NOT NULL DEFAULT(1),
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2     NULL
);

-- 16. PurchaseReturnItems
CREATE TABLE dbo.PurchaseReturnItems
(
    PurchaseReturnItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseReturnItems PRIMARY KEY,
    PurchaseReturnId     INT NOT NULL REFERENCES dbo.PurchaseReturns(Id),
    ProductId            INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity             DECIMAL(18,3) NOT NULL,
    UnitCost             DECIMAL(18,2) NOT NULL,
    LineTotal            DECIMAL(18,2) NOT NULL
);

-- 17. SalesReturns
CREATE TABLE dbo.SalesReturns
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesReturns PRIMARY KEY,
    ReturnNo          NVARCHAR(30)  NOT NULL UNIQUE,
    SalesInvoiceId    INT           NULL REFERENCES dbo.SalesInvoices(Id),
    CustomerId        INT           NOT NULL REFERENCES dbo.Customers(Id),
    WarehouseId       INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    ReturnDate        DATETIME2     NOT NULL,
    Reason            NVARCHAR(250) NULL,
    SubTotal          DECIMAL(18,2) NOT NULL DEFAULT 0,
    TotalAmount       DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status            TINYINT       NOT NULL, -- 1=Draft, 2=Posted, 3=Cancelled
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    IsActive          BIT           NOT NULL DEFAULT(1),
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2     NULL
);

-- 18. SalesReturnItems
CREATE TABLE dbo.SalesReturnItems
(
    SalesReturnItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesReturnItems PRIMARY KEY,
    SalesReturnId     INT NOT NULL REFERENCES dbo.SalesReturns(Id),
    ProductId         INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity          DECIMAL(18,3) NOT NULL,
    UnitPrice         DECIMAL(18,2) NOT NULL,
    LineTotal         DECIMAL(18,2) NOT NULL
);

-- 19. StockTransfers
CREATE TABLE dbo.StockTransfers
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockTransfers PRIMARY KEY,
    TransferNo        NVARCHAR(30)  NOT NULL UNIQUE,
    FromWarehouseId   INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    ToWarehouseId     INT           NOT NULL REFERENCES dbo.Warehouses(Id),
    TransferDate      DATETIME2     NOT NULL,
    Notes             NVARCHAR(500) NULL,
    Status            TINYINT       NOT NULL, -- 1=Draft, 2=Posted, 3=Cancelled
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    IsActive          BIT           NOT NULL DEFAULT(1),
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2     NULL
);

-- 20. StockTransferItems
CREATE TABLE dbo.StockTransferItems
(
    StockTransferItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockTransferItems PRIMARY KEY,
    StockTransferId     INT NOT NULL REFERENCES dbo.StockTransfers(Id),
    ProductId           INT NOT NULL REFERENCES dbo.Products(Id),
    Quantity            DECIMAL(18,3) NOT NULL,
    Notes               NVARCHAR(250) NULL
);

-- 21. CustomerPayments
CREATE TABLE dbo.CustomerPayments
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerPayments PRIMARY KEY,
    PaymentNo       NVARCHAR(30)  NOT NULL UNIQUE,
    CustomerId      INT           NOT NULL REFERENCES dbo.Customers(Id),
    SalesInvoiceId  INT           NULL REFERENCES dbo.SalesInvoices(Id),
    PaymentDate     DATETIME2     NOT NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    PaymentMethod   TINYINT       NOT NULL, -- 1=Cash, 2=Bank Transfer, 3=Card, 4=Other
    ReferenceNo     NVARCHAR(50)  NULL,
    Notes           NVARCHAR(500) NULL,
    CreatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT           NULL REFERENCES dbo.Users(Id),
    IsActive        BIT           NOT NULL DEFAULT(1),
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2     NULL
);

-- 22. SupplierPayments
CREATE TABLE dbo.SupplierPayments
(
    Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplierPayments PRIMARY KEY,
    PaymentNo         NVARCHAR(30)  NOT NULL UNIQUE,
    SupplierId        INT           NOT NULL REFERENCES dbo.Suppliers(Id),
    PurchaseInvoiceId INT           NULL REFERENCES dbo.PurchaseInvoices(Id),
    PaymentDate       DATETIME2     NOT NULL,
    Amount            DECIMAL(18,2) NOT NULL,
    PaymentMethod     TINYINT       NOT NULL,
    ReferenceNo       NVARCHAR(50)  NULL,
    Notes             NVARCHAR(500) NULL,
    CreatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId   INT           NULL REFERENCES dbo.Users(Id),
    IsActive          BIT           NOT NULL DEFAULT(1),
    CreatedAt         DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt         DATETIME2     NULL
);

-- 23. DocumentSequences
CREATE TABLE dbo.DocumentSequences
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentSequences PRIMARY KEY,
    Prefix          NVARCHAR(10)  NOT NULL UNIQUE,
    LastNumber      INT           NOT NULL CONSTRAINT DF_DocumentSequences_LastNumber DEFAULT(0),
    UpdatedAt       DATETIME2     NOT NULL CONSTRAINT DF_DocumentSequences_UpdatedAt DEFAULT(SYSDATETIME())
);

-- 24. ProductUnits (v4.3 — Dynamic UOM)
CREATE TABLE dbo.ProductUnits
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductUnits PRIMARY KEY,
    ProductId       INT NOT NULL REFERENCES dbo.Products(Id),
    UnitName        NVARCHAR(50)  NOT NULL,
    ConversionFactor DECIMAL(18,3) NOT NULL DEFAULT 1,
    RetailPrice     DECIMAL(18,2) NOT NULL DEFAULT 0,
    WholesalePrice  DECIMAL(18,2) NOT NULL DEFAULT 0,
    Cost            DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsBaseUnit      BIT NOT NULL DEFAULT 0,
    CreatedByUserId INT NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT NULL REFERENCES dbo.Users(Id),
    IsActive        BIT NOT NULL CONSTRAINT DF_ProductUnits_IsActive DEFAULT(1),
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2 NULL,

    CONSTRAINT UQ_ProductUnits_Product_UnitName UNIQUE (ProductId, UnitName)
);

-- 25. UnitBarcodes (v4.3)
CREATE TABLE dbo.UnitBarcodes
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UnitBarcodes PRIMARY KEY,
    ProductUnitId   INT NOT NULL REFERENCES dbo.ProductUnits(Id),
    Barcode         NVARCHAR(50)  NOT NULL,
    CreatedByUserId INT NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT NULL REFERENCES dbo.Users(Id),
    IsActive        BIT NOT NULL CONSTRAINT DF_UnitBarcodes_IsActive DEFAULT(1),
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2 NULL,

    CONSTRAINT UQ_UnitBarcodes_Barcode UNIQUE (Barcode)
);

-- 26. CashBoxes (v4.3)
CREATE TABLE dbo.CashBoxes
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashBoxes PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL,
    OpeningBalance  DECIMAL(18,2) NOT NULL DEFAULT 0,
    CurrentBalance  DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsDefault       BIT NOT NULL DEFAULT 0,
    CreatedByUserId INT NULL REFERENCES dbo.Users(Id),
    UpdatedByUserId INT NULL REFERENCES dbo.Users(Id),
    IsActive        BIT NOT NULL CONSTRAINT DF_CashBoxes_IsActive DEFAULT(1),
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt       DATETIME2 NULL
);

-- 27. CashTransactions (v4.3 — Immutable)
CREATE TABLE dbo.CashTransactions
(
    Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CashTransactions PRIMARY KEY,
    CashBoxId       INT NOT NULL REFERENCES dbo.CashBoxes(Id),
    TransactionType TINYINT NOT NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    BalanceBefore   DECIMAL(18,2) NOT NULL,
    BalanceAfter    DECIMAL(18,2) NOT NULL,
    ReferenceType   NVARCHAR(30) NULL,
    ReferenceId     INT NULL,
    Notes           NVARCHAR(500) NULL,
    CreatedByUserId INT NULL REFERENCES dbo.Users(Id),
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

-- 28. ProductPriceHistory (v4.3)
CREATE TABLE dbo.ProductPriceHistory
(
    Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductPriceHistory PRIMARY KEY,
    ProductUnitId       INT NOT NULL REFERENCES dbo.ProductUnits(Id),
    OldRetailPrice      DECIMAL(18,2) NULL,
    NewRetailPrice      DECIMAL(18,2) NULL,
    OldWholesalePrice   DECIMAL(18,2) NULL,
    NewWholesalePrice   DECIMAL(18,2) NULL,
    OldCost             DECIMAL(18,2) NULL,
    NewCost             DECIMAL(18,2) NULL,
    ChangedByUserId     INT NULL REFERENCES dbo.Users(Id),
    ChangeReason        NVARCHAR(200) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);

-- 29. SystemSettings (v4.3)
CREATE TABLE dbo.SystemSettings
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemSettings PRIMARY KEY,
    [Key]       NVARCHAR(100) NOT NULL,
    [Value]     NVARCHAR(500) NULL,
    Category    NVARCHAR(50) NULL,
    Description NVARCHAR(250) NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    UpdatedAt   DATETIME2 NULL,

    CONSTRAINT UQ_SystemSettings_Key UNIQUE ([Key])
);

-- 30. SystemLog (v4.3)
CREATE TABLE dbo.SystemLog
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemLog PRIMARY KEY,
    [Level]     NVARCHAR(20) NOT NULL,
    [Message]   NVARCHAR(4000) NOT NULL,
    [Exception] NVARCHAR(MAX) NULL,
    UserId      INT NULL REFERENCES dbo.Users(Id),
    [Source]    NVARCHAR(200) NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
```

6. Domain Entities — C# Classes
6.1 Base Entity
csharp


<!-- END OF SECTION: 3.1 Baseline Database Schema SQL Script -->


<!-- START OF SECTION: 3.2 Advanced Modules SQL Schema Migrations (v4.3) -->

> [!NOTE]
> Database schema migrations for advanced features (Dynamic UOM, Multi-Barcode, Cash Boxes, Costing strategies, Price history).

🗂️ Phase 0: Database Schema — Complete Migration
Agent Rule: Run ALL scripts in order. Do not skip any table.

Task 0.1 — Remove Old Columns, Add Dynamic UOM Tables
SQL

-- =============================================
-- STEP 1: Backup first (ALWAYS)
-- =============================================
-- Run on staging environment first

-- =============================================
-- STEP 2: Create ProductUnits (Dynamic UOM)
-- =============================================
CREATE TABLE ProductUnits (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    ProductId       INT NOT NULL,
    UnitName        NVARCHAR(100) NOT NULL,      -- e.g., "حبة", "طبق", "كرتون"
    BaseConversionFactor DECIMAL(18,6) NOT NULL, -- Pieces per this unit
    IsBaseUnit      BIT NOT NULL DEFAULT 0,       -- TRUE for exactly ONE unit per product
    SalesPrice      DECIMAL(18,4) NOT NULL DEFAULT 0,
    PurchaseCost    DECIMAL(18,4) NOT NULL DEFAULT 0,
    SupplierPrice   DECIMAL(18,4) NOT NULL DEFAULT 0, -- Catalog price from supplier
    LastPurchasePrice DECIMAL(18,4) NOT NULL DEFAULT 0,-- Last actual invoice price
    SortOrder       INT NOT NULL DEFAULT 0,        -- Display order in UI
    IsActive        BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_ProductUnits_Products
        FOREIGN KEY (ProductId) REFERENCES Products(Id)
            ON DELETE CASCADE,

    CONSTRAINT CHK_BaseUnitFactor
        CHECK (IsBaseUnit = 0 OR BaseConversionFactor = 1)
);

CREATE INDEX IX_ProductUnits_ProductId ON ProductUnits(ProductId);

-- =============================================
-- STEP 3: Unit Barcodes (Many per Unit)
-- =============================================
CREATE TABLE UnitBarcodes (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    ProductUnitId   INT NOT NULL,
    BarcodeValue    NVARCHAR(100) NOT NULL,
    IsDefault       BIT NOT NULL DEFAULT 0,
    SupplierCode    NVARCHAR(100) NULL, -- Optional: which supplier uses this code
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_UnitBarcodes_ProductUnits
        FOREIGN KEY (ProductUnitId) REFERENCES ProductUnits(Id)
            ON DELETE CASCADE,

    CONSTRAINT UQ_UnitBarcodes_Value
        UNIQUE (BarcodeValue)
);

CREATE INDEX IX_UnitBarcodes_Value ON UnitBarcodes(BarcodeValue);

-- =============================================
-- STEP 4: Cash Boxes
-- =============================================
CREATE TABLE CashBoxes (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    BoxName         NVARCHAR(100) NOT NULL,
    CurrentBalance  DECIMAL(18,4) NOT NULL DEFAULT 0,
    BranchId        INT NULL,
    CurrencyCode    NVARCHAR(10) NOT NULL DEFAULT 'SAR',
    AssignedUserId  INT NULL,         -- NULL = shared box
    IsActive        BIT NOT NULL DEFAULT 1,
    Notes           NVARCHAR(500) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- =============================================
-- STEP 5: Cash Transactions Log
-- =============================================
CREATE TABLE CashTransactions (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    CashBoxId       INT NOT NULL,
    TransactionType INT NOT NULL, -- 0=SaleIn, 1=PurchaseOut, 2=TransferIn, 3=TransferOut, 4=Manual
    Amount          DECIMAL(18,4) NOT NULL,
    BalanceBefore   DECIMAL(18,4) NOT NULL,  -- Snapshot for audit
    BalanceAfter    DECIMAL(18,4) NOT NULL,  -- Snapshot for audit
    ReferenceType   NVARCHAR(50) NULL,       -- "SalesInvoice", "PurchaseInvoice"
    ReferenceId     INT NULL,
    Notes           NVARCHAR(500) NULL,
    CreatedBy       INT NOT NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_CashTransactions_CashBoxes
        FOREIGN KEY (CashBoxId) REFERENCES CashBoxes(Id)
);

CREATE INDEX IX_CashTransactions_CashBoxId ON CashTransactions(CashBoxId);
CREATE INDEX IX_CashTransactions_Reference ON CashTransactions(ReferenceType, ReferenceId);

-- =============================================
-- STEP 6: System Settings (Costing Strategy)
-- =============================================
CREATE TABLE SystemSettings (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    SettingKey      NVARCHAR(100) NOT NULL UNIQUE,
    SettingValue    NVARCHAR(500) NOT NULL,
    DataType        NVARCHAR(50) NOT NULL DEFAULT 'string', -- 'string','int','bool','decimal'
    Category        NVARCHAR(100) NOT NULL,
    DisplayName     NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(1000) NULL,
    UpdatedBy       INT NULL,
    UpdatedAt       DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Seed costing strategy default
INSERT INTO SystemSettings (SettingKey, SettingValue, DataType, Category, DisplayName, Description)
VALUES (
    'CostingMethod',
    'WeightedAverage',
    'string',
    'Inventory',
    'طريقة احتساب التكلفة',
    'تحدد كيف يحتسب النظام تكلفة البضاعة في المخزن'
);

-- =============================================
-- STEP 7: Add CashBoxId to invoices
-- =============================================
ALTER TABLE SalesInvoices    ADD CashBoxId INT NULL;
ALTER TABLE PurchaseInvoices ADD CashBoxId INT NULL;

ALTER TABLE SalesInvoices    ADD CONSTRAINT FK_Sales_CashBox
    FOREIGN KEY (CashBoxId) REFERENCES CashBoxes(Id);
ALTER TABLE PurchaseInvoices ADD CONSTRAINT FK_Purchase_CashBox
    FOREIGN KEY (CashBoxId) REFERENCES CashBoxes(Id);

-- =============================================
-- STEP 8: Price History Log (Audit Trail)
-- =============================================
CREATE TABLE ProductPriceHistory (
    Id              INT PRIMARY KEY IDENTITY(1,1),
    ProductUnitId   INT NOT NULL,
    ChangeType      NVARCHAR(50) NOT NULL, -- 'PurchaseCost','SalesPrice','SupplierPrice'
    OldValue        DECIMAL(18,4) NOT NULL,
    NewValue        DECIMAL(18,4) NOT NULL,
    CostingMethod   NVARCHAR(50) NULL,
    InvoiceId       INT NULL,
    ChangedBy       INT NOT NULL,
    ChangedAt       DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_PriceHistory_ProductUnits
        FOREIGN KEY (ProductUnitId) REFERENCES ProductUnits(Id)
);
✅ Phase 0 Checklist
 All 8 SQL blocks executed without errors
 ProductUnits has CHECK constraint on base unit factor
 UnitBarcodes has UNIQUE constraint on barcode value
 CashBoxes linked to invoices tables
 SystemSettings seeded with default costing method
 ProductPriceHistory created for audit trail

<!-- END OF SECTION: 3.2 Advanced Modules SQL Schema Migrations (v4.3) -->


# 4. Domain Architecture & C# Entity Models

The enterprise domain layer contains core aggregate roots, entities, domain enums, and rules with ZERO external dependencies.

<!-- START OF SECTION: 4.1 Baseline C# Domain Entities -->

> [!NOTE]
> Baseline business entities including Product, Customer, Supplier, Warehouse, and Sales Invoice.

// SalesSystem.Domain/Common/BaseEntity.cs
namespace SalesSystem.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;
    protected void Deactivate() => IsActive = false;
}
6.2 Product Entity
csharp

// SalesSystem.Domain/Entities/Product.cs
namespace SalesSystem.Domain.Entities;

public class Product : BaseEntity
{
    public string? Barcode { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int? CategoryId { get; private set; }
    public decimal SupplierPrice { get; private set; }
    public decimal ReorderLevel { get; private set; }
    public string? Description { get; private set; }

    // Navigation
    public Category? Category { get; private set; }
    private readonly List<ProductUnit> _units = new();
    public IReadOnlyCollection<ProductUnit> Units => _units.AsReadOnly();

    protected Product() { } // EF Core

    public static Product Create(
        string name,
        decimal supplierPrice = 0,
        decimal reorderLevel = 0,
        string? barcode = null,
        int? categoryId = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب");
        if (supplierPrice < 0)
            throw new DomainException("سعر المورد لا يمكن أن يكون سالباً");
        if (reorderLevel < 0)
            throw new DomainException("الحد الأدنى لا يمكن أن يكون سالباً");

        return new Product
        {
            Name = name.Trim(),
            SupplierPrice = supplierPrice,
            ReorderLevel = reorderLevel,
            Barcode = barcode?.Trim(),
            CategoryId = categoryId,
            Description = description?.Trim()
        };
    }

    public void Update(
        string name,
        decimal supplierPrice,
        decimal reorderLevel,
        string? barcode,
        int? categoryId,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب");
        if (supplierPrice < 0)
            throw new DomainException("سعر المورد لا يمكن أن يكون سالباً");

        Name = name.Trim();
        SupplierPrice = supplierPrice;
        ReorderLevel = reorderLevel;
        Barcode = barcode?.Trim();
        CategoryId = categoryId;
        Description = description?.Trim();
        SetUpdated();
    }

    public void AddUnit(string unitName, decimal conversionFactor,
        decimal retailPrice, decimal wholesalePrice, bool isBaseUnit)
    {
        if (_units.Any(u => u.UnitName == unitName))
            throw new DomainException($"الوحدة '{unitName}' موجودة بالفعل");
        if (isBaseUnit && _units.Any(u => u.IsBaseUnit))
            throw new DomainException("يوجد بالفعل وحدة أساسية للمنتج");

        var unit = ProductUnit.Create(Id, unitName, conversionFactor,
            retailPrice, wholesalePrice, isBaseUnit);
        _units.Add(unit);
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdated();
    }

    public void DeleteUnit(int productUnitId)
    {
        if (_units.Count <= 1)
            throw new DomainException("يجب أن يحتوي المنتج على وحدة واحدة على الأقل");
        var unit = _units.FirstOrDefault(u => u.Id == productUnitId);
        if (unit == null)
            throw new DomainException("الوحدة غير موجودة");
        _units.Remove(unit);
    }
}
6.3 WarehouseStock Entity ⚠️ الأهم
csharp

// SalesSystem.Domain/Entities/WarehouseStock.cs
namespace SalesSystem.Domain.Entities;

public class WarehouseStock : BaseEntity
{
    public int WarehouseId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Warehouse? Warehouse { get; private set; }
    public Product? Product { get; private set; }

    protected WarehouseStock() { }

    public static WarehouseStock Create(int warehouseId, int productId)
    {
        return new WarehouseStock
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = 0
        };
    }

    /// <summary>
    /// زيادة المخزون — للشراء ومرتجع البيع والتحويل الداخل
    /// </summary>
    public void Increase(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException($"كمية الزيادة يجب أن تكون أكبر من صفر، القيمة المُدخلة: {quantity}");

        Quantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// تقليل المخزون — للبيع ومرتجع الشراء والتحويل الخارج
    /// </summary>
    public void Decrease(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException($"كمية الخصم يجب أن تكون أكبر من صفر، القيمة المُدخلة: {quantity}");

        if (Quantity < quantity)
            throw new InsufficientStockException(
                ProductId, WarehouseId, quantity, Quantity);

        Quantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// التحقق فقط بدون تغيير — للتحقق المسبق
    /// </summary>
    public bool HasSufficientStock(decimal requestedQuantity)
        => Quantity >= requestedQuantity;
}
6.4 SalesInvoice Entity ⚠️ أهم Entity في النظام
csharp

// SalesSystem.Domain/Entities/SalesInvoice.cs
namespace SalesSystem.Domain.Entities;

public class SalesInvoice : BaseEntity
{
    public int CustomerId { get; private set; }
    public int WarehouseId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal DueAmount { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public int? CreatedByUserId { get; private set; }

    // Navigation
    public Customer? Customer { get; private set; }
    public Warehouse? Warehouse { get; private set; }
    private readonly List<SalesInvoiceItem> _items = new();
    public IReadOnlyCollection<SalesInvoiceItem> Items => _items.AsReadOnly();

    protected SalesInvoice() { }

    public static SalesInvoice Create(
        int customerId,
        int warehouseId,
        PaymentType paymentType,
        decimal paidAmount,
        string? notes,
        int? createdByUserId)
    {
        if (customerId <= 0)
            throw new DomainException("العميل مطلوب");
        if (warehouseId <= 0)
            throw new DomainException("المخزن مطلوب");
        if (paidAmount < 0)
            throw new DomainException("المبلغ المدفوع لا يمكن أن يكون سالباً");

        return new SalesInvoice
        {
            CustomerId = customerId,
            WarehouseId = warehouseId,
            InvoiceDate = DateTime.UtcNow,
            PaymentType = paymentType,
            PaidAmount = paidAmount,
            Notes = notes?.Trim(),
            Status = InvoiceStatus.Draft,
            CreatedByUserId = createdByUserId
        };
    }

    public void AddItem(int productId, decimal quantity, 
                        decimal unitPrice, decimal discountAmount = 0,
                        int? productUnitId = null, SaleMode saleMode = SaleMode.Retail)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن تعديل فاتورة مرحَّلة أو ملغاة");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من صفر");
        if (unitPrice < 0)
            throw new DomainException("السعر لا يمكن أن يكون سالباً");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً");

        // ⚠️ LineTotal لا تُحسب في الـ UI — تُحسب هنا فقط
        var lineTotal = (quantity * unitPrice) - discountAmount;
        if (lineTotal < 0)
            throw new DomainException("إجمالي السطر لا يمكن أن يكون سالباً");

        // ⚠️ v4.3+ — productUnitId links to ProductUnit for pricing audit
        // SaleMode: 1=Retail, 2=Wholesale
        var item = SalesInvoiceItem.Create(
            Id, productId, quantity, unitPrice, discountAmount, lineTotal,
            productUnitId, saleMode);
        _items.Add(item);

        RecalculateTotals();
    }

    /// <summary>
    /// ⚠️ الحساب النهائي — يُنفذ عند كل إضافة/حذف عنصر
    /// </summary>
    private void RecalculateTotals()
    {
        // ✅ decimal arithmetic — لا float
        SubTotal = _items.Sum(i => i.LineTotal);
        
        // TaxAmount يُحسب على SubTotal بعد الخصم الكلي
        TaxAmount = 0; // يُحسب لاحقاً إذا كانت الضريبة مفعلة
        
        TotalAmount = SubTotal - DiscountAmount + TaxAmount;
        
        if (TotalAmount < 0)
            throw new DomainException("إجمالي الفاتورة لا يمكن أن يكون سالباً");

        if (PaidAmount > TotalAmount)
            throw new DomainException(
                $"المبلغ المدفوع ({PaidAmount}) أكبر من إجمالي الفاتورة ({TotalAmount})");

        DueAmount = TotalAmount - PaidAmount;
    }

    /// <summary>
    /// ترحيل الفاتورة — يُستدعى مرة واحدة فقط
    /// </summary>
    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException($"لا يمكن ترحيل الفاتورة بحالة: {Status}");
        if (!_items.Any())
            throw new DomainException("لا يمكن ترحيل فاتورة فارغة");

        RecalculateTotals(); // تحقق أخير قبل الترحيل

        Status = InvoiceStatus.Posted;
        SetUpdated();
    }

    public void Cancel(string reason)
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("الفاتورة ملغاة بالفعل");

        Status = InvoiceStatus.Cancelled;
        Notes = string.IsNullOrEmpty(Notes)
            ? $"ملغاة: {reason}"
            : $"{Notes} | ملغاة: {reason}";
        SetUpdated();
    }
}
7. Contracts — DTOs و Requests
7.1 Common Result
csharp


<!-- END OF SECTION: 4.1 Baseline C# Domain Entities -->


<!-- START OF SECTION: 4.2 Advanced Modules C# Domain Entities (v4.3) -->

> [!NOTE]
> Entities and domain aggregates for Dynamic UOM (ProductUnit, UnitBarcode), Cash Box Management, and costing price histories.

🏗️ Phase 1: Domain Layer — Entities & Enums
Task 1.1 — New Enums
csharp

// File: Domain/Enums/CostingMethod.cs
public enum CostingMethod
{
    WeightedAverage = 0,    // متوسط التكلفة المرجح
    LastPurchasePrice = 1,  // آخر سعر توريد
    SupplierPrice = 2       // سعر المورد
}

// File: Domain/Enums/CashTransactionType.cs
public enum CashTransactionType
{
    SaleIn = 0,         // مبيعات (وارد)
    PurchaseOut = 1,    // مشتريات (صادر)
    TransferIn = 2,     // تحويل وارد
    TransferOut = 3,    // تحويل صادر
    ManualIn = 4,       // إيداع يدوي
    ManualOut = 5       // سحب يدوي
}
Task 1.2 — ProductUnit Entity
csharp

// File: Domain/Entities/ProductUnit.cs

public class ProductUnit : BaseEntity
{
    // ─── Properties ───────────────────────────────
    public int ProductId { get; private set; }
    public string UnitName { get; private set; }

    /// <summary>
    /// How many BASE UNITS does this unit contain?
    /// Base unit itself = 1. Box of 12 = 12. Pallet of 360 = 360.
    /// </summary>
    public decimal BaseConversionFactor { get; private set; }

    public bool IsBaseUnit { get; private set; }
    public decimal SalesPrice { get; private set; }
    public decimal PurchaseCost { get; private set; }
    public decimal SupplierPrice { get; private set; }
    public decimal LastPurchasePrice { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation
    public Product Product { get; private set; }

    private readonly List<UnitBarcode> _barcodes = new();
    public IReadOnlyCollection<UnitBarcode> Barcodes => _barcodes.AsReadOnly();

    private ProductUnit() { } // EF Core

    // ─── Factory ──────────────────────────────────
    public static ProductUnit CreateBaseUnit(
        int productId,
        string unitName)
    {
        if (string.IsNullOrWhiteSpace(unitName))
            throw new DomainException("Unit name cannot be empty");

        return new ProductUnit
        {
            ProductId = productId,
            UnitName = unitName.Trim(),
            BaseConversionFactor = 1,
            IsBaseUnit = true,
            IsActive = true,
            SortOrder = 0
        };
    }

    public static ProductUnit CreateDerivedUnit(
        int productId,
        string unitName,
        decimal baseConversionFactor,
        int sortOrder = 1)
    {
        if (string.IsNullOrWhiteSpace(unitName))
            throw new DomainException("Unit name cannot be empty");

        if (baseConversionFactor <= 1)
            throw new DomainException(
                $"وحدة '{unitName}' يجب أن تحتوي على أكثر من وحدة صغرى واحدة. " +
                $"أدخل كم وحدة صغرى بداخلها (مثال: الكرتون يحتوي على 12 حبة، ادخل 12).");

        return new ProductUnit
        {
            ProductId = productId,
            UnitName = unitName.Trim(),
            BaseConversionFactor = baseConversionFactor,
            IsBaseUnit = false,
            IsActive = true,
            SortOrder = sortOrder
        };
    }

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Converts quantity in this unit to base unit quantity.
    /// ALWAYS use this before touching stock.
    /// </summary>
    public decimal ToBaseUnitQuantity(decimal quantity)
        => quantity * BaseConversionFactor;

    /// <summary>
    /// Updates cost after purchase. Returns old cost for history logging.
    /// </summary>
    public decimal UpdatePurchaseCost(decimal newCost)
    {
        if (newCost < 0)
            throw new DomainException("التكلفة لا يمكن أن تكون سالبة");

        var oldCost = PurchaseCost;
        LastPurchasePrice = newCost;
        PurchaseCost = newCost;
        return oldCost;
    }

    public decimal UpdateSalesPrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new DomainException("سعر البيع لا يمكن أن يكون سالباً");

        var oldPrice = SalesPrice;
        SalesPrice = newPrice;
        return oldPrice;
    }

    public void UpdateSupplierPrice(decimal supplierPrice)
    {
        if (supplierPrice < 0)
            throw new DomainException("سعر المورد لا يمكن أن يكون سالباً");
        SupplierPrice = supplierPrice;
    }

    public void AddBarcode(string barcodeValue, bool isDefault = false, 
        string? supplierCode = null)
    {
        if (string.IsNullOrWhiteSpace(barcodeValue))
            throw new DomainException("قيمة الباركود لا يمكن أن تكون فارغة");

        // If this is default, unmark others
        if (isDefault)
        {
            foreach (var b in _barcodes)
                b.UnmarkDefault();
        }

        _barcodes.Add(UnitBarcode.Create(Id, barcodeValue, isDefault, supplierCode));
    }

    /// <summary>
    /// Calculates cost for this unit based on base unit cost.
    /// e.g., if base unit costs 1 SAR and this unit = 12 pieces → cost = 12 SAR
    /// </summary>
    public decimal CalculateCostFromBaseUnitCost(decimal baseUnitCost)
        => baseUnitCost * BaseConversionFactor;
}
Task 1.3 — UnitBarcode Entity
csharp

// File: Domain/Entities/UnitBarcode.cs

public class UnitBarcode : BaseEntity
{
    public int ProductUnitId { get; private set; }
    public string BarcodeValue { get; private set; }
    public bool IsDefault { get; private set; }
    public string? SupplierCode { get; private set; }

    private UnitBarcode() { }

    public static UnitBarcode Create(
        int productUnitId,
        string barcodeValue,
        bool isDefault = false,
        string? supplierCode = null)
    {
        return new UnitBarcode
        {
            ProductUnitId = productUnitId,
            BarcodeValue = barcodeValue.Trim().ToUpperInvariant(),
            IsDefault = isDefault,
            SupplierCode = supplierCode?.Trim()
        };
    }

    public void UnmarkDefault() => IsDefault = false;
}
Task 1.4 — CashBox Entity
csharp

// File: Domain/Entities/CashBox.cs

public class CashBox : BaseEntity
{
    public string BoxName { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public int? BranchId { get; private set; }
    public string CurrencyCode { get; private set; }
    public int? AssignedUserId { get; private set; }    // NULL = shared
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<CashTransaction> _transactions = new();
    public IReadOnlyCollection<CashTransaction> Transactions 
        => _transactions.AsReadOnly();

    private CashBox() { }

    public static CashBox Create(
        string boxName,
        int? branchId = null,
        int? assignedUserId = null,
        string currencyCode = "SAR",
        decimal initialBalance = 0)
    {
        if (string.IsNullOrWhiteSpace(boxName))
            throw new DomainException("اسم الصندوق مطلوب");

        return new CashBox
        {
            BoxName = boxName.Trim(),
            BranchId = branchId,
            AssignedUserId = assignedUserId,
            CurrencyCode = currencyCode,
            CurrentBalance = initialBalance,
            IsActive = true
        };
    }

    // ─── Domain Methods ───────────────────────────

    public CashTransaction Deposit(
        decimal amount,
        CashTransactionType type,
        string? referenceType = null,
        int? referenceId = null,
        int createdBy = 0,
        string? notes = null)
    {
        if (amount <= 0)
            throw new DomainException("مبلغ الإيداع يجب أن يكون أكبر من صفر");

        var balanceBefore = CurrentBalance;
        CurrentBalance += amount;

        var transaction = CashTransaction.Create(
            Id, type, amount, balanceBefore, CurrentBalance,
            referenceType, referenceId, createdBy, notes);

        _transactions.Add(transaction);
        return transaction;
    }

    public CashTransaction Withdraw(
        decimal amount,
        CashTransactionType type,
        string? referenceType = null,
        int? referenceId = null,
        int createdBy = 0,
        string? notes = null)
    {
        if (amount <= 0)
            throw new DomainException("مبلغ السحب يجب أن يكون أكبر من صفر");

        if (CurrentBalance < amount)
            throw new DomainException(
                $"رصيد الصندوق غير كافٍ. الرصيد الحالي: {CurrentBalance:N2}، " +
                $"المبلغ المطلوب: {amount:N2}");

        var balanceBefore = CurrentBalance;
        CurrentBalance -= amount;

        var transaction = CashTransaction.Create(
            Id, type, -amount, balanceBefore, CurrentBalance,
            referenceType, referenceId, createdBy, notes);

        _transactions.Add(transaction);
        return transaction;
    }

    public void CanUserAccess(int userId)
    {
        if (AssignedUserId.HasValue && AssignedUserId.Value != userId)
            throw new DomainException(
                $"ليس لديك صلاحية الوصول إلى الصندوق '{BoxName}'. " +
                $"تواصل مع المدير لتغيير الصلاحيات.");
    }
}
Task 1.5 — CashTransaction Entity
csharp

// File: Domain/Entities/CashTransaction.cs

public class CashTransaction : BaseEntity
{
    public int CashBoxId { get; private set; }
    public CashTransactionType TransactionType { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string? ReferenceType { get; private set; }
    public int? ReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public int CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CashTransaction() { }

    internal static CashTransaction Create(
        int cashBoxId,
        CashTransactionType type,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        string? referenceType,
        int? referenceId,
        int createdBy,
        string? notes)
    {
        return new CashTransaction
        {
            CashBoxId = cashBoxId,
            TransactionType = type,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            CreatedBy = createdBy,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }
}
Task 1.6 — Update Product Entity
csharp

// File: Domain/Entities/Product.cs
// MODIFY existing Product — remove old price fields, add Units collection

public class Product : BaseEntity
{
    // ─── Keep existing fields ─────────────────────
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public int CategoryId { get; private set; }
    public bool IsActive { get; private set; }

    // ─── REMOVED: WholesalePrice, RetailPrice, ConversionFactor ───
    // These now live in ProductUnits

    // Navigation
    private readonly List<ProductUnit> _units = new();
    public IReadOnlyCollection<ProductUnit> Units => _units.AsReadOnly();

    // ─── Domain Methods ───────────────────────────

    /// <summary>
    /// Returns the base unit (Piece/Egg/etc). ALWAYS exists.
    /// Throws if not found — product data is corrupted.
    /// </summary>
    public ProductUnit GetBaseUnit()
    {
        return _units.FirstOrDefault(u => u.IsBaseUnit)
            ?? throw new DomainException(
                $"المنتج '{Name}' لا يحتوي على وحدة أساسية. " +
                $"يرجى تعريف وحدة صغرى أولاً (مثال: حبة) من شاشة إدارة المنتجات.");
    }

    public ProductUnit GetUnitById(int unitId)
    {
        return _units.FirstOrDefault(u => u.Id == unitId)
            ?? throw new DomainException(
                $"الوحدة المحددة غير موجودة في المنتج '{Name}'");
    }

    /// <summary>
    /// Validates product has exactly ONE base unit before saving.
    /// Call this in the Command Handler before persisting.
    /// </summary>
    public void ValidateUnits()
    {
        var baseUnits = _units.Where(u => u.IsBaseUnit).ToList();

        if (baseUnits.Count == 0)
            throw new DomainException(
                "⚠️ يجب تعريف وحدة صغرى واحدة على الأقل.\n" +
                "مثال: أضف وحدة باسم 'حبة' واجعل معامل التحويل = 1");

        if (baseUnits.Count > 1)
            throw new DomainException(
                "⚠️ لا يمكن تعريف أكثر من وحدة صغرى واحدة للمنتج الواحد.\n" +
                $"الوحدات المعرّفة كأساسية: {string.Join(", ", baseUnits.Select(u => u.UnitName))}");

        var invalidDerived = _units
            .Where(u => !u.IsBaseUnit && u.BaseConversionFactor <= 1)
            .ToList();

        if (invalidDerived.Any())
            throw new DomainException(
                $"⚠️ الوحدات التالية لها معامل تحويل غير صحيح:\n" +
                $"{string.Join("\n", invalidDerived.Select(u => $"- {u.UnitName}: يجب أن يكون أكبر من 1"))}\n" +
                $"أدخل كم وحدة صغرى بداخل كل وحدة أكبر.");
    }
}
✅ Phase 1 Checklist
 ProductUnit.CreateBaseUnit() sets factor to 1 automatically
 ProductUnit.CreateDerivedUnit() rejects factor <= 1 with Arabic message
 Product.ValidateUnits() catches 0 base units, >1 base units, and invalid factors
 CashBox.Withdraw() rejects insufficient balance with Arabic message
 CashBox.CanUserAccess() enforces user-box assignment
 All error messages are in Arabic and user-friendly

<!-- END OF SECTION: 4.2 Advanced Modules C# Domain Entities (v4.3) -->


# 5. Contracts, Common DTOs & Domain Exceptions

DTO representations, service request/response models, common result patterns, and domain exception handlers.

<!-- START OF SECTION: 5.1 Common Contracts & Request DTOs -->

> [!NOTE]
> Core data contract classes, generic success/failure Result wrappers, and request DTOs.

// SalesSystem.Contracts/Common/Result.cs
namespace SalesSystem.Contracts.Common;

/// <summary>
/// ⚠️ كل العمليات ترجع Result<T> — لا exceptions مكشوفة للـ UI
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result() { }

    public static Result<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public static Result<T> Failure(string error, string? errorCode = null) => new()
    {
        IsSuccess = false,
        Error = error,
        ErrorCode = errorCode
    };
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}

// أكواد الأخطاء القياسية
public static class ErrorCodes
{
    public const string NotFound                = "NOT_FOUND";
    public const string InsufficientStock       = "INSUFFICIENT_STOCK";
    public const string DuplicateBarcode        = "DUPLICATE_BARCODE";
    public const string InvalidAmount           = "INVALID_AMOUNT";
    public const string InvalidQuantity         = "INVALID_QUANTITY";
    public const string InvoiceAlreadyPosted    = "ALREADY_POSTED";
    public const string InvoiceAlreadyCancelled = "ALREADY_CANCELLED";
    public const string ValidationError         = "VALIDATION_ERROR";
    public const string DatabaseError           = "DATABASE_ERROR";
}
7.2 DTOs الأساسية
csharp

// SalesSystem.Contracts/DTOs/ProductDto.cs
public record ProductDto(
    int ProductId,
    string? Barcode,
    string Name,
    int? CategoryId,
    string? CategoryName,
    decimal SupplierPrice,
    decimal ReorderLevel,
    bool IsActive,
    decimal TotalStock,
    List<ProductUnitDto> Units
);

public record ProductUnitDto(
    int ProductUnitId,
    string UnitName,
    decimal ConversionFactor,
    decimal RetailPrice,
    decimal WholesalePrice,
    decimal Cost,
    bool IsBaseUnit
);

// SalesSystem.Contracts/DTOs/WarehouseStockDto.cs
public record WarehouseStockDto(
    int WarehouseId,
    string WarehouseName,
    int ProductId,
    string ProductName,
    decimal Quantity
);

// SalesSystem.Contracts/DTOs/SalesInvoiceDto.cs
public record SalesInvoiceDto(
    int SalesInvoiceId,
    int CustomerId,
    string CustomerName,
    int WarehouseId,
    string WarehouseName,
    DateTime InvoiceDate,
    string PaymentType,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount,
    string Status,
    List<SalesInvoiceItemDto> Items
);

public record SalesInvoiceItemDto(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal
);
7.3 Requests مع Validation
csharp

// SalesSystem.Contracts/Requests/Sales/CreateSalesInvoiceRequest.cs
public record CreateSalesInvoiceRequest(
    int CustomerId,
    int WarehouseId,
    int PaymentType,           // 1=Cash, 2=Credit, 3=Mixed
    decimal PaidAmount,
    string? Notes,
    List<SalesInvoiceItemRequest> Items
);

public record SalesInvoiceItemRequest(
    int ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount
);

// SalesSystem.Contracts/Requests/Products/CreateProductRequest.cs
public record CreateProductRequest(
    string Name,
    string? Barcode,
    int? CategoryId,
    decimal SupplierPrice,
    decimal ReorderLevel,
    string? Description,
    List<CreateProductUnitRequest> Units
);

public record CreateProductUnitRequest(
    string UnitName,
    decimal ConversionFactor,
    decimal RetailPrice,
    decimal WholesalePrice,
    bool IsBaseUnit
);
8. Application Services — منطق العمل
8.1 ISalesService Interface
csharp


<!-- END OF SECTION: 5.1 Common Contracts & Request DTOs -->


<!-- START OF SECTION: 5.2 Domain Custom Exceptions -->

> [!NOTE]
> Custom business exception definitions used to enforce domain guard clauses (e.g., InsufficientStockException).

// SalesSystem.Domain/Exceptions/

// الاستثناء الأساسي لقواعد العمل
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

// استثناء المخزون غير الكافي — الأكثر أهمية
public class InsufficientStockException : DomainException
{
    public int ProductId { get; }
    public int WarehouseId { get; }
    public decimal RequestedQuantity { get; }
    public decimal AvailableQuantity { get; }

    public InsufficientStockException(
        int productId, int warehouseId,
        decimal requested, decimal available)
        : base($"المخزون غير كافٍ. المنتج: {productId}, " +
               $"المخزن: {warehouseId}, " +
               $"المطلوب: {requested}, المتوفر: {available}")
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}

10. Infrastructure — EF Core
10.1 DbContext
csharp


<!-- END OF SECTION: 5.2 Domain Custom Exceptions -->


# 6. Application Services & Use Case Realization

Application services orchestrate business use cases, coordinate database transactions, and return Result pattern responses.

<!-- START OF SECTION: 6.1 Baseline Application Services -->

> [!NOTE]
> Transactional services for Sales invoices, inventory tracking, document sequencing, and return operations.

// SalesSystem.Application/Interfaces/ISalesService.cs
namespace SalesSystem.Application.Interfaces;

public interface ISalesService
{
    Task<Result<SalesInvoiceDto>> CreateInvoiceAsync(
        CreateSalesInvoiceRequest request, int? userId,
        CancellationToken ct = default);

    Task<Result<SalesInvoiceDto>> GetByIdAsync(
        int invoiceId, CancellationToken ct = default);

    Task<Result<PagedResult<SalesInvoiceDto>>> GetAllAsync(
        SalesInvoiceFilter filter, CancellationToken ct = default);

    Task<Result> CancelInvoiceAsync(
        int invoiceId, string reason, int? userId,
        CancellationToken ct = default);
}
8.2 SalesService Implementation ⚠️ الأهم والأحرج
csharp

// SalesSystem.Application/Services/SalesService.cs
namespace SalesSystem.Application.Services;

public class SalesService : ISalesService
{
    private readonly IUnitOfWork _uow;
    private readonly IDocumentSequenceService _sequenceService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<SalesService> _logger;

    public SalesService(
        IUnitOfWork uow,
        IDocumentSequenceService sequenceService,
        IInventoryService inventoryService,
        ILogger<SalesService> logger)
    {
        _uow = uow;
        _sequenceService = sequenceService;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async Task<Result<SalesInvoiceDto>> CreateInvoiceAsync(
        CreateSalesInvoiceRequest request,
        int? userId,
        CancellationToken ct = default)
    {
        // === Step 1: Validate Request ===
        var validationResult = ValidateRequest(request);
        if (!validationResult.IsSuccess)
            return Result<SalesInvoiceDto>.Failure(
                validationResult.Error!, ErrorCodes.ValidationError);

        // === Step 2: التحقق من وجود العميل والمخزن ===
        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct);
        if (customer == null)
            return Result<SalesInvoiceDto>.Failure(
                "العميل غير موجود", ErrorCodes.NotFound);

        var warehouse = await _uow.Warehouses.GetByIdAsync(request.WarehouseId, ct);
        if (warehouse == null || !warehouse.IsActive)
            return Result<SalesInvoiceDto>.Failure(
                "المخزن غير موجود أو غير نشط", ErrorCodes.NotFound);

        // === Step 3: التحقق المسبق من المخزون لكل عنصر ===
        // ⚠️ مهم: نتحقق أولاً قبل فتح Transaction
        foreach (var item in request.Items)
        {
            var stock = await _uow.WarehouseStocks
                .GetByWarehouseAndProductAsync(request.WarehouseId, item.ProductId, ct);

            var available = stock?.Quantity ?? 0;
            if (available < item.Quantity)
            {
                var product = await _uow.Products.GetByIdAsync(item.ProductId, ct);
                return Result<SalesInvoiceDto>.Failure(
                    $"المخزون غير كافٍ للمنتج '{product?.Name}'. " +
                    $"المتوفر: {available}, المطلوب: {item.Quantity}",
                    ErrorCodes.InsufficientStock);
            }
        }

        // === Step 5: Database Transaction ===
        // ⚠️ كل العمليات التالية داخل Transaction واحدة
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            // 5a. إنشاء الفاتورة
            var invoice = SalesInvoice.Create(
                request.CustomerId,
                request.WarehouseId,
                (PaymentType)request.PaymentType,
                request.PaidAmount,
                request.Notes,
                userId);

            // 5b. إضافة العناصر (الحساب يتم في Domain)
            foreach (var itemRequest in request.Items)
            {
                invoice.AddItem(
                    itemRequest.ProductId,
                    itemRequest.Quantity,
                    itemRequest.UnitPrice,
                    itemRequest.DiscountAmount);
            }

            // 5c. التحقق من المبلغ المدفوع
            if (request.PaidAmount > invoice.TotalAmount)
                return Result<SalesInvoiceDto>.Failure(
                    $"المبلغ المدفوع ({request.PaidAmount:N2}) " +
                    $"أكبر من إجمالي الفاتورة ({invoice.TotalAmount:N2})",
                    ErrorCodes.InvalidAmount);

            // 5d. ترحيل الفاتورة
            invoice.Post();

            // 5e. حفظ الفاتورة في DB
            await _uow.SalesInvoices.AddAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            // 5f. خصم المخزون لكل عنصر
            // ⚠️ يتم بعد حفظ الفاتورة وتوليد ID
            foreach (var item in invoice.Items)
            {
                await _inventoryService.DecreaseStockAsync(
                    warehouseId: request.WarehouseId,
                    productId: item.ProductId,
                    quantity: item.Quantity,
                    movementType: MovementType.SaleOut,
                    referenceType: "SalesInvoice",
                    referenceId: invoice.Id,
                    unitCost: item.UnitPrice,
                    userId: userId,
                    ct: ct);
            }

            // 5g. تحديث رصيد العميل (إذا كان دين)
            if (invoice.DueAmount > 0)
            {
                customer.IncreaseBalance(invoice.DueAmount);
                await _uow.SaveChangesAsync(ct);
            }

            // 5h. Commit
            await transaction.CommitAsync(ct);

            var dto = MapToDto(invoice);
            return Result<SalesInvoiceDto>.Success(dto);
        }
        catch (InsufficientStockException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "مخزون غير كافٍ أثناء البيع");
            return Result<SalesInvoiceDto>.Failure(
                ex.Message, ErrorCodes.InsufficientStock);
        }
        catch (DomainException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "خطأ في قواعد العمل أثناء البيع");
            return Result<SalesInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "خطأ غير متوقع أثناء إنشاء فاتورة البيع");
            return Result<SalesInvoiceDto>.Failure(
                "حدث خطأ غير متوقع. يرجى المحاولة مرة أخرى.");
        }
    }

    private Result ValidateRequest(CreateSalesInvoiceRequest request)
    {
        if (request.Items == null || !request.Items.Any())
            return Result.Failure("يجب إضافة منتج واحد على الأقل");

        if (request.PaidAmount < 0)
            return Result.Failure("المبلغ المدفوع لا يمكن أن يكون سالباً");

        if (request.PaymentType is < 1 or > 3)
            return Result.Failure("نوع الدفع غير صحيح");

        foreach (var item in request.Items)
        {
            if (item.ProductId <= 0)
                return Result.Failure("معرّف المنتج غير صحيح");
            if (item.Quantity <= 0)
                return Result.Failure("الكمية يجب أن تكون أكبر من صفر");
            if (item.UnitPrice < 0)
                return Result.Failure("السعر لا يمكن أن يكون سالباً");
            if (item.DiscountAmount < 0)
                return Result.Failure("الخصم لا يمكن أن يكون سالباً");

            // ⚠️ التحقق من LineTotal منطقياً
            var lineTotal = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
            if (lineTotal < 0)
                return Result.Failure(
                    $"إجمالي السطر سالب للمنتج {item.ProductId}. " +
                    "الخصم لا يمكن أن يتجاوز سعر البيع.");
        }

        return Result.Success();
    }
}
8.3 InventoryService ⚠️ حرج جداً
csharp

// SalesSystem.Application/Services/InventoryService.cs
namespace SalesSystem.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _uow;

    public InventoryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <summary>
    /// ⚠️ يُستدعى دائماً داخل Transaction خارجية
    /// </summary>
    public async Task DecreaseStockAsync(
        int warehouseId, int productId, decimal quantity,
        MovementType movementType, string referenceType,
        int referenceId, decimal? unitCost, int? userId,
        CancellationToken ct = default)
    {
        // جلب أو إنشاء سجل المخزون
        var stock = await _uow.WarehouseStocks
            .GetByWarehouseAndProductAsync(warehouseId, productId, ct);

        if (stock == null)
            throw new DomainException(
                $"لا يوجد سجل مخزون للمنتج {productId} في المخزن {warehouseId}");

        var quantityBefore = stock.Quantity;

        // ⚠️ هذا يرمي InsufficientStockException إذا لم تكن الكمية كافية
        stock.Decrease(quantity);

        // تسجيل الحركة
        var movement = new InventoryMovement
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            MovementType = movementType,
            QuantityChange = -quantity,    // سالب = خروج
            QuantityBefore = quantityBefore,
            QuantityAfter = stock.Quantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            UnitCost = unitCost,
            MovementDate = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _uow.InventoryMovements.AddAsync(movement, ct);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task IncreaseStockAsync(
        int warehouseId, int productId, decimal quantity,
        MovementType movementType, string referenceType,
        int referenceId, decimal? unitCost, int? userId,
        CancellationToken ct = default)
    {
        var stock = await _uow.WarehouseStocks
            .GetByWarehouseAndProductAsync(warehouseId, productId, ct);

        // إذا لم يكن هناك سجل، نُنشئ واحداً
        if (stock == null)
        {
            stock = WarehouseStock.Create(warehouseId, productId);
            await _uow.WarehouseStocks.AddAsync(stock, ct);
            await _uow.SaveChangesAsync(ct); // للحصول على ID
        }

        var quantityBefore = stock.Quantity;
        stock.Increase(quantity);

        var movement = new InventoryMovement
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            MovementType = movementType,
            QuantityChange = quantity,     // موجب = دخول
            QuantityBefore = quantityBefore,
            QuantityAfter = stock.Quantity,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            UnitCost = unitCost,
            MovementDate = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await _uow.InventoryMovements.AddAsync(movement, ct);
        await _uow.SaveChangesAsync(ct);
    }
}
8.4 DocumentSequenceService — توليد أرقام الوثائق
csharp

// SalesSystem.Application/Services/DocumentSequenceService.cs
namespace SalesSystem.Application.Services;

public class DocumentSequenceService : IDocumentSequenceService
{
    private readonly IUnitOfWork _uow;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public DocumentSequenceService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    /// <summary>
    /// ⚠️ Thread-safe — يُستخدم Lock لمنع الأرقام المكررة
    /// المخرج: INV-2025-000001
    /// </summary>
    public async Task<string> GenerateAsync(
        string prefix, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var sequence = await _uow.DocumentSequences
                .GetByPrefixAsync(prefix, ct);

            if (sequence == null)
                throw new InvalidOperationException(
                    $"Sequence prefix '{prefix}' غير موجود");

            sequence.LastNumber++;
            sequence.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync(ct);

            // Format: INV-2025-000001
            return $"{prefix}-{DateTime.Now.Year}-{sequence.LastNumber:D6}";
        }
        finally
        {
            _lock.Release();
        }
    }
}
8.5 SalesReturn Service ⚠️ منطق المرتجع الحرج
csharp

// SalesSystem.Application/Services/SalesReturnService.cs
public async Task<Result<SalesReturnDto>> CreateReturnAsync(
    CreateSalesReturnRequest request, int? userId, CancellationToken ct = default)
{
    // === التحقق المسبق ===
    // إذا كان مرتجع لفاتورة محددة، تحقق من الكميات
    if (request.SalesInvoiceId.HasValue)
    {
        var originalInvoice = await _uow.SalesInvoices
            .GetByIdWithItemsAsync(request.SalesInvoiceId.Value, ct);

        if (originalInvoice == null)
            return Result<SalesReturnDto>.Failure("الفاتورة الأصلية غير موجودة");

        if (originalInvoice.Status == InvoiceStatus.Cancelled)
            return Result<SalesReturnDto>.Failure("لا يمكن إرجاع فاتورة ملغاة");

        // ⚠️ التحقق: الكمية المُرجعة لا تتجاوز الكمية الأصلية
        foreach (var returnItem in request.Items)
        {
            var originalItem = originalInvoice.Items
                .FirstOrDefault(i => i.ProductId == returnItem.ProductId);

            if (originalItem == null)
                return Result<SalesReturnDto>.Failure(
                    $"المنتج {returnItem.ProductId} غير موجود في الفاتورة الأصلية");

            // ⚠️ حساب ما تم إرجاعه مسبقاً
            var previouslyReturned = await _uow.SalesReturnItems
                .GetTotalReturnedQuantityAsync(
                    request.SalesInvoiceId.Value, returnItem.ProductId, ct);

            var maxReturnable = originalItem.Quantity - previouslyReturned;

            if (returnItem.Quantity > maxReturnable)
                return Result<SalesReturnDto>.Failure(
                    $"الكمية المطلوب إرجاعها ({returnItem.Quantity}) " +
                    $"أكبر من الكمية القابلة للإرجاع ({maxReturnable})");
        }
    }

    // === Transaction ===
    await using var transaction = await _uow.BeginTransactionAsync(ct);
    try
    {
        var returnNo = await _sequenceService.GenerateAsync("SR", ct);

        // حساب الإجمالي
        var totalAmount = request.Items
            .Sum(i => i.Quantity * i.UnitPrice);   // ✅ decimal

        // إنشاء المرتجع
        var salesReturn = new SalesReturn
        {
            ReturnNo = returnNo,
            SalesInvoiceId = request.SalesInvoiceId,
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            ReturnDate = DateTime.UtcNow,
            Reason = request.Reason,
            TotalAmount = totalAmount,
            Status = InvoiceStatus.Posted,
            CreatedByUserId = userId
        };

        await _uow.SalesReturns.AddAsync(salesReturn, ct);
        await _uow.SaveChangesAsync(ct);

        // إضافة العناصر
        foreach (var item in request.Items)
        {
            var returnItem = new SalesReturnItem
            {
                SalesReturnId = salesReturn.SalesReturnId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice  // ✅ decimal
            };
            await _uow.SalesReturnItems.AddAsync(returnItem, ct);
        }

        await _uow.SaveChangesAsync(ct);

        // ⚠️ إعادة المخزون (مرتجع البيع يزيد المخزون)
        foreach (var item in request.Items)
        {
            await _inventoryService.IncreaseStockAsync(
                warehouseId: request.WarehouseId,
                productId: item.ProductId,
                quantity: item.Quantity,
                movementType: MovementType.SaleReturnIn,
                referenceType: "SalesReturn",
                referenceId: salesReturn.SalesReturnId,
                unitCost: item.UnitPrice,
                userId: userId,
                ct: ct);
        }

        // ⚠️ تخفيض رصيد العميل (المرتجع يُقلل الدين)
        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct);
        customer!.DecreaseBalance(totalAmount);
        await _uow.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Result<SalesReturnDto>.Success(MapToDto(salesReturn));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);
        _logger.LogError(ex, "خطأ في إنشاء مرتجع البيع");
        return Result<SalesReturnDto>.Failure("حدث خطأ غير متوقع");
    }
}
9. Exceptions المخصصة
csharp


<!-- END OF SECTION: 6.1 Baseline Application Services -->


<!-- START OF SECTION: 6.2 Advanced Modules Application Services (v4.3) -->

> [!NOTE]
> Services implementing Dynamic UOM costing cascade (UpdateProductPricingService) and transactional command handlers.

⚙️ Phase 3: Application Layer — Pricing Service
Task 3.1 — UpdateProductPricingService
csharp

// File: Application/Services/UpdateProductPricingService.cs

public interface IUpdateProductPricingService
{
    Task UpdateFromPurchaseAsync(
        UpdatePricingRequest request,
        CancellationToken ct = default);
}

public record UpdatePricingRequest(
    int ProductUnitId,
    decimal NewPurchaseCost,
    decimal NewQuantityPurchased,
    decimal? NewSalesPrice,          // Optional — user may override
    int InvoiceId,
    int ChangedBy
);

public class UpdateProductPricingService : IUpdateProductPricingService
{
    private readonly AppDbContext _context;
    private readonly ISystemSettingsRepository _settings;
    private readonly ILogger<UpdateProductPricingService> _logger;

    public UpdateProductPricingService(
        AppDbContext context,
        ISystemSettingsRepository settings,
        ILogger<UpdateProductPricingService> logger)
    {
        _context = context;
        _settings = settings;
        _logger = logger;
    }

    public async Task UpdateFromPurchaseAsync(
        UpdatePricingRequest request,
        CancellationToken ct = default)
    {
        // ─── 1. Load the purchased unit and ALL units for this product ───
        var purchasedUnit = await _context.ProductUnits
            .Include(u => u.Product)
                .ThenInclude(p => p.Units)
            .FirstOrDefaultAsync(u => u.Id == request.ProductUnitId, ct)
            ?? throw new NotFoundException("ProductUnit", request.ProductUnitId);

        var product = purchasedUnit.Product;
        var allUnits = product.Units.Where(u => u.IsActive).ToList();
        var baseUnit = product.GetBaseUnit();

        // ─── 2. Calculate new BASE UNIT cost ─────────────────────────────
        var costingMethod = await _settings.GetCostingMethodAsync(ct);

        var newBaseUnitCost = await CalculateNewBaseUnitCostAsync(
            costingMethod,
            baseUnit,
            purchasedUnit,
            request.NewPurchaseCost,
            request.NewQuantityPurchased,
            ct);

        _logger.LogInformation(
            "Updating costs for Product {ProductId} using {Method}. " +
            "New base unit cost: {Cost}",
            product.Id, costingMethod, newBaseUnitCost);

        // ─── 3. Cascade cost update to ALL units ─────────────────────────
        var historyEntries = new List<ProductPriceHistory>();

        foreach (var unit in allUnits)
        {
            var newUnitCost = unit.CalculateCostFromBaseUnitCost(newBaseUnitCost);
            var oldCost = unit.UpdatePurchaseCost(newUnitCost);

            historyEntries.Add(new ProductPriceHistory
            {
                ProductUnitId = unit.Id,
                ChangeType = "PurchaseCost",
                OldValue = oldCost,
                NewValue = newUnitCost,
                CostingMethod = costingMethod.ToString(),
                InvoiceId = request.InvoiceId,
                ChangedBy = request.ChangedBy,
                ChangedAt = DateTime.UtcNow
            });
        }

        // ─── 4. Update sales price if user provided one ──────────────────
        if (request.NewSalesPrice.HasValue && request.NewSalesPrice.Value > 0)
        {
            var oldSalesPrice = purchasedUnit.UpdateSalesPrice(request.NewSalesPrice.Value);

            historyEntries.Add(new ProductPriceHistory
            {
                ProductUnitId = purchasedUnit.Id,
                ChangeType = "SalesPrice",
                OldValue = oldSalesPrice,
                NewValue = request.NewSalesPrice.Value,
                InvoiceId = request.InvoiceId,
                ChangedBy = request.ChangedBy,
                ChangedAt = DateTime.UtcNow
            });
        }

        // ─── 5. Save history ──────────────────────────────────────────────
        _context.ProductPriceHistory.AddRange(historyEntries);
        await _context.SaveChangesAsync(ct);
    }

    private async Task<decimal> CalculateNewBaseUnitCostAsync(
        CostingMethod method,
        ProductUnit baseUnit,
        ProductUnit purchasedUnit,
        decimal invoiceCostForPurchasedUnit,
        decimal quantityPurchased,
        CancellationToken ct)
    {
        // Convert invoice cost to base unit cost first
        var newBaseCostFromInvoice = purchasedUnit.IsBaseUnit
            ? invoiceCostForPurchasedUnit
            : invoiceCostForPurchasedUnit / purchasedUnit.BaseConversionFactor;

        return method switch
        {
            CostingMethod.LastPurchasePrice =>
                // Simple: just use the new price
                newBaseCostFromInvoice,

            CostingMethod.SupplierPrice =>
                // Use the supplier catalog price (don't change cost from invoice)
                baseUnit.SupplierPrice > 0
                    ? baseUnit.SupplierPrice
                    : newBaseCostFromInvoice,

            CostingMethod.WeightedAverage =>
                // Weighted average: [(OldStock × OldCost) + (NewQty × NewCost)] / TotalQty
                await CalculateWeightedAverageAsync(
                    baseUnit,
                    newBaseCostFromInvoice,
                    quantityPurchased * purchasedUnit.BaseConversionFactor,
                    ct),

            _ => newBaseCostFromInvoice
        };
    }

    private async Task<decimal> CalculateWeightedAverageAsync(
        ProductUnit baseUnit,
        decimal newBaseUnitCost,
        decimal newQuantityInBaseUnits,
        CancellationToken ct)
    {
        // Get current stock in base units
        var currentStock = await _context.Stocks
            .AsNoTracking()
            .Where(s => s.ProductId == baseUnit.ProductId)
            .Select(s => s.CurrentQuantityInPieces)
            .FirstOrDefaultAsync(ct);

        var oldCost = baseUnit.PurchaseCost;

        // If no existing stock, just use new cost
        if (currentStock <= 0) return newBaseUnitCost;

        // Weighted Average Formula
        var weightedAverage =
            ((currentStock * oldCost) + (newQuantityInBaseUnits * newBaseUnitCost))
            / (currentStock + newQuantityInBaseUnits);

        return Math.Round(weightedAverage, 4);
    }
}
Task 3.2 — Purchase Invoice Command (Updated)
csharp

// File: Application/Commands/CreatePurchaseInvoice/CreatePurchaseInvoiceCommand.cs

public record CreatePurchaseInvoiceCommand : IRequest<int>
{
    public int SupplierId { get; init; }
    public int CashBoxId { get; init; }     // NEW: Which cash box pays
    public int CashierId { get; init; }
    public string? Notes { get; init; }
    public List<PurchaseInvoiceItemRequest> Items { get; init; } = new();
}

public record PurchaseInvoiceItemRequest(
    int ProductUnitId,
    decimal Quantity,
    decimal UnitCost,
    decimal? NewSalesPrice,     // Optional: override sales price from invoice screen
    decimal Discount
);
csharp

// File: Application/Commands/CreatePurchaseInvoice/CreatePurchaseInvoiceCommandHandler.cs

public class CreatePurchaseInvoiceCommandHandler
    : IRequestHandler<CreatePurchaseInvoiceCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUpdateProductPricingService _pricingService;
    private readonly AppDbContext _context;

    public CreatePurchaseInvoiceCommandHandler(
        IUnitOfWork unitOfWork,
        IUpdateProductPricingService pricingService,
        AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _pricingService = pricingService;
        _context = context;
    }

    public async Task<int> Handle(
        CreatePurchaseInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        // ─── 1. Validate CashBox access ──────────────────────────────────
        var cashBox = await _unitOfWork.CashBoxes
            .GetByIdAsync(command.CashBoxId, cancellationToken)
            ?? throw new NotFoundException("CashBox", command.CashBoxId);

        cashBox.CanUserAccess(command.CashierId); // Throws if no permission

        // ─── 2. Create invoice ────────────────────────────────────────────
        var invoice = PurchaseInvoice.Create(
            command.SupplierId,
            command.CashBoxId,
            command.CashierId,
            command.Notes);

        decimal totalAmount = 0;

        foreach (var itemRequest in command.Items)
        {
            // Load unit with product
            var productUnit = await _context.ProductUnits
                .Include(u => u.Product)
                    .ThenInclude(p => p.Units)
                .Include(u => u.Product)
                    .ThenInclude(p => p.Stock)
                .FirstOrDefaultAsync(u => u.Id == itemRequest.ProductUnitId, cancellationToken)
                ?? throw new NotFoundException("ProductUnit", itemRequest.ProductUnitId);

            // Add to invoice
            invoice.AddItem(
                productUnit.Id,
                productUnit.Product.Name,
                productUnit.UnitName,
                itemRequest.Quantity,
                itemRequest.UnitCost,
                itemRequest.Discount);

            // Add stock — Domain converts to base units internally
            productUnit.Product.Stock.AddStock(
                itemRequest.Quantity,
                productUnit.BaseConversionFactor);

            totalAmount += (itemRequest.Quantity * itemRequest.UnitCost) - itemRequest.Discount;
        }

        // ─── 3. Deduct from cash box ──────────────────────────────────────
        cashBox.Withdraw(
            totalAmount,
            CashTransactionType.PurchaseOut,
            referenceType: "PurchaseInvoice",
            referenceId: invoice.Id,
            createdBy: command.CashierId,
            notes: $"دفع فاتورة مشتريات رقم {invoice.Id}");

        // ─── 4. Save invoice and stock ────────────────────────────────────
        _unitOfWork.PurchaseInvoices.Add(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // ─── 5. Update product pricing (AFTER save so invoice.Id exists) ──
        foreach (var itemRequest in command.Items)
        {
            await _pricingService.UpdateFromPurchaseAsync(
                new UpdatePricingRequest(
                    itemRequest.ProductUnitId,
                    itemRequest.UnitCost,
                    itemRequest.Quantity,
                    itemRequest.NewSalesPrice,
                    invoice.Id,
                    command.CashierId),
                cancellationToken);
        }

        return invoice.Id;
    }
}
Task 3.3 — Cash Transfer Command
csharp

// File: Application/Commands/TransferCash/TransferCashCommand.cs

public record TransferCashCommand(
    int FromCashBoxId,
    int ToCashBoxId,
    decimal Amount,
    int TransferredBy,
    string? Notes
) : IRequest<Unit>;

public class TransferCashCommandHandler
    : IRequestHandler<TransferCashCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransferCashCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        TransferCashCommand command,
        CancellationToken cancellationToken)
    {
        if (command.FromCashBoxId == command.ToCashBoxId)
            throw new DomainException("لا يمكن التحويل من الصندوق إلى نفسه");

        var fromBox = await _unitOfWork.CashBoxes
            .GetByIdAsync(command.FromCashBoxId, cancellationToken)
            ?? throw new NotFoundException("CashBox", command.FromCashBoxId);

        var toBox = await _unitOfWork.CashBoxes
            .GetByIdAsync(command.ToCashBoxId, cancellationToken)
            ?? throw new NotFoundException("CashBox", command.ToCashBoxId);

        fromBox.CanUserAccess(command.TransferredBy);

        // These two domain calls maintain balance integrity
        fromBox.Withdraw(command.Amount, CashTransactionType.TransferOut,
            notes: $"تحويل إلى: {toBox.BoxName} | {command.Notes}",
            createdBy: command.TransferredBy);

        toBox.Deposit(command.Amount, CashTransactionType.TransferIn,
            notes: $"تحويل من: {fromBox.BoxName} | {command.Notes}",
            createdBy: command.TransferredBy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
✅ Phase 3 Checklist
 WeightedAverage formula is correct: (OldQty×OldCost + NewQty×NewCost) / TotalQty
 LastPurchasePrice just overwrites — no formula
 Cost cascade goes to ALL units (base and derived)
 Derived unit cost = baseUnitCost × ConversionFactor
 Pricing update happens AFTER invoice saved (so ID exists for history)
 Cash transfer uses domain methods (not direct property assignment)

<!-- END OF SECTION: 6.2 Advanced Modules Application Services (v4.3) -->


# 7. Infrastructure Persistence & Repositories

EF Core DbContext, entity fluent configuration bindings, restrict delete behaviors, and global filters.

<!-- START OF SECTION: 7.1 Baseline EF Core Infrastructure -->

> [!NOTE]
> DbContext declaration, base configuration models, and standard configurations for baseline entities.

// SalesSystem.Infrastructure/DbContext/SalesDbContext.cs
namespace SalesSystem.Infrastructure.Data;

public class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) 
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnItem> SalesReturnItems => Set<SalesReturnItem>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<CustomerPayment> CustomerPayments => Set<CustomerPayment>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();
    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // تطبيق كل الـ Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SalesDbContext).Assembly);
    }
}
10.2 WarehouseStock Configuration ⚠️
csharp

// SalesSystem.Infrastructure/Configurations/WarehouseStockConfiguration.cs
public class WarehouseStockConfiguration 
    : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks");
        builder.HasKey(x => x.WarehouseStockId);

        // ⚠️ decimal(18,3) للكمية — إلزامي
        builder.Property(x => x.Quantity)
            .HasColumnType("decimal(18,3)")
            .HasDefaultValue(0m);

        // ⚠️ Unique Constraint حرج
        builder.HasIndex(x => new { x.WarehouseId, x.ProductId })
            .IsUnique();

        // CHECK Constraint
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_WarehouseStocks_Qty", "[Quantity] >= 0"));

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2");
    }
}
10.3 SalesInvoice Configuration
csharp

public class SalesInvoiceConfiguration 
    : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("SalesInvoices");
        builder.HasKey(x => x.SalesInvoiceId);

        // ⚠️ كل الأموال decimal(18,2)
        builder.Property(x => x.SubTotal)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.DiscountAmount)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.TaxAmount)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.TotalAmount)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.PaidAmount)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(x => x.DueAmount)
            .HasColumnType("decimal(18,2)").HasDefaultValue(0m);

        // CHECK Constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SI_Total",
                "[TotalAmount] >= 0");
            t.HasCheckConstraint("CK_SI_Paid",
                "[PaidAmount] >= 0 AND [PaidAmount] <= [TotalAmount]");
            t.HasCheckConstraint("CK_SI_Due",
                "[DueAmount] >= 0");
        });

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(i => i.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict); // ⚠️ v4.6 — NO Cascade delete
    }
}
11. API Controllers
11.1 SalesController
csharp


<!-- END OF SECTION: 7.1 Baseline EF Core Infrastructure -->


<!-- START OF SECTION: 7.2 Advanced Infrastructure configurations & Services -->

> [!NOTE]
> DbContext bindings and configs for advanced entities, plus infrastructural services like BarcodeLookupService.

⚙️ Phase 2: Infrastructure — EF Core & Services
Task 2.1 — EF Configurations
csharp

// File: Infrastructure/Persistence/Configurations/ProductUnitConfiguration.cs

public class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.ToTable("ProductUnits");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UnitName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BaseConversionFactor).HasPrecision(18, 6);
        builder.Property(x => x.SalesPrice).HasPrecision(18, 4);
        builder.Property(x => x.PurchaseCost).HasPrecision(18, 4);
        builder.Property(x => x.SupplierPrice).HasPrecision(18, 4);
        builder.Property(x => x.LastPurchasePrice).HasPrecision(18, 4);

        // Enforce: base unit must have factor = 1
        builder.ToTable(t => t.HasCheckConstraint(
            "CHK_BaseUnitFactor",
            "IsBaseUnit = 0 OR BaseConversionFactor = 1"));

        builder.HasMany(x => x.Barcodes)
            .WithOne()
            .HasForeignKey(x => x.ProductUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Units)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// File: Infrastructure/Persistence/Configurations/CashBoxConfiguration.cs

public class CashBoxConfiguration : IEntityTypeConfiguration<CashBox>
{
    public void Configure(EntityTypeBuilder<CashBox> builder)
    {
        builder.ToTable("CashBoxes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BoxName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CurrentBalance).HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).HasMaxLength(10);

        builder.HasMany(x => x.Transactions)
            .WithOne()
            .HasForeignKey(x => x.CashBoxId)
            .OnDelete(DeleteBehavior.Restrict); // Keep history if box deleted
    }
}
Task 2.2 — Barcode Lookup Service (Updated)
csharp

// File: Infrastructure/Services/BarcodeLookupService.cs

public interface IBarcodeLookupService
{
    Task<BarcodeSearchResult?> LookupAsync(string barcode, CancellationToken ct = default);
}

public record BarcodeSearchResult(
    int ProductId,
    string ProductName,
    int ProductUnitId,
    string UnitName,
    decimal BaseConversionFactor,
    bool IsBaseUnit,
    decimal SalesPrice,
    decimal PurchaseCost,
    decimal CurrentStockInBaseUnits
);

public class BarcodeLookupService : IBarcodeLookupService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BarcodeLookupService> _logger;

    public BarcodeLookupService(AppDbContext context, ILogger<BarcodeLookupService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BarcodeSearchResult?> LookupAsync(
        string barcode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        var normalized = barcode.Trim().ToUpperInvariant();

        // Search in UnitBarcodes table (new multi-barcode system)
        var result = await _context.UnitBarcodes
            .Where(b => b.BarcodeValue == normalized)
            .Select(b => new BarcodeSearchResult(
                b.ProductUnit.ProductId,
                b.ProductUnit.Product.Name,
                b.ProductUnitId,
                b.ProductUnit.UnitName,
                b.ProductUnit.BaseConversionFactor,
                b.ProductUnit.IsBaseUnit,
                b.ProductUnit.SalesPrice,
                b.ProductUnit.PurchaseCost,
                b.ProductUnit.Product.Stock.CurrentQuantityInPieces
            ))
            .FirstOrDefaultAsync(ct);

        if (result == null)
            _logger.LogWarning("Barcode not found: {Barcode}", normalized);

        return result;
    }
}
Task 2.3 — Settings Repository
csharp

// File: Infrastructure/Repositories/SystemSettingsRepository.cs

public interface ISystemSettingsRepository
{
    Task<CostingMethod> GetCostingMethodAsync(CancellationToken ct = default);
    Task SetCostingMethodAsync(CostingMethod method, CancellationToken ct = default);
}

public class SystemSettingsRepository : ISystemSettingsRepository
{
    private readonly AppDbContext _context;

    public SystemSettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CostingMethod> GetCostingMethodAsync(CancellationToken ct = default)
    {
        var setting = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SettingKey == "CostingMethod", ct);

        if (setting == null) return CostingMethod.WeightedAverage; // Safe default

        return Enum.TryParse<CostingMethod>(setting.SettingValue, out var method)
            ? method
            : CostingMethod.WeightedAverage;
    }

    public async Task SetCostingMethodAsync(CostingMethod method, CancellationToken ct = default)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.SettingKey == "CostingMethod", ct);

        if (setting != null)
            setting.UpdateValue(method.ToString());

        await _context.SaveChangesAsync(ct);
    }
}
✅ Phase 2 Checklist
 EF Core configurations registered in AppDbContext.OnModelCreating()
 BarcodeLookupService searches UnitBarcodes (not old Barcode column)
 SystemSettingsRepository returns safe default if setting missing
 All repositories registered in DI container

<!-- END OF SECTION: 7.2 Advanced Infrastructure configurations & Services -->


# 8. WPF Presentation & MVVM Design Patterns

Centralized MVVM patterns, editor ViewModels, dynamic components, and styled views for WPF Desktop PWF.

<!-- START OF SECTION: 8.1 WPF Styled Dialog Service (AGENTS.md & Constitution Rules) -->

> [!NOTE]
> Strict presentation guidelines for user confirmations, error dialogs, real-time input fields, and toast messages.

### WPF Dialog Service
WPF UI requires `IDialogService` and `IToastNotificationService` for consistent user communication, preventing ugly generic message boxes.

#### Dialog Service Methods
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

#### Toast Notification Service
```csharp
public interface IToastNotificationService
{
    void ShowSuccess(string message);  // Green, 3s
    void ShowError(string message);    // Red, 5s
    void ShowInfo(string message);     // Blue, 3s
}
```

<!-- END OF SECTION: 8.1 WPF Styled Dialog Service (AGENTS.md & Constitution Rules) -->


<!-- START OF SECTION: 8.2 Advanced WPF ViewModels -->

> [!NOTE]
> ViewModels handling dynamic Unit of Measure builders, invoice rows, and retail/wholesale cost adjustments.

🖥️ Phase 4: WPF ViewModels
Task 4.1 — Product Unit Builder ViewModel
csharp

// File: WPF/ViewModels/Products/ProductUnitBuilderViewModel.cs

public class ProductUnitBuilderViewModel : BaseViewModel
{
    private bool _hasShownOnboarding = false;

    public ObservableCollection<ProductUnitRowViewModel> Units { get; } = new();

    // Validation summary shown in UI
    public string ValidationSummary { get; private set; } = string.Empty;
    public bool HasValidationError { get; private set; }

    // Commands
    public IRelayCommand AddUnitCommand { get; }
    public IRelayCommand<ProductUnitRowViewModel> RemoveUnitCommand { get; }
    public IRelayCommand ShowHelpCommand { get; }

    public ProductUnitBuilderViewModel()
    {
        AddUnitCommand = new RelayCommand(AddNewUnit);
        RemoveUnitCommand = new RelayCommand<ProductUnitRowViewModel>(RemoveUnit);
        ShowHelpCommand = new RelayCommand(ShowOnboarding);

        Units.CollectionChanged += (_, _) => Validate();
    }

    public void Initialize(List<ProductUnitRowViewModel>? existingUnits = null)
    {
        Units.Clear();

        if (existingUnits?.Any() == true)
        {
            foreach (var unit in existingUnits.OrderBy(u => u.SortOrder))
                AddUnitWithChangeTracking(unit);
        }
        else
        {
            // New product — show onboarding and pre-add base unit row
            ShowOnboarding();
            AddBaseUnitRow();
        }
    }

    private void AddBaseUnitRow()
    {
        var baseRow = new ProductUnitRowViewModel
        {
            IsBaseUnit = true,
            BaseConversionFactor = 1,
            SortOrder = 0,
            Placeholder_UnitName = "مثال: حبة، قطعة، بيضة"
        };
        baseRow.PropertyChanged += (_, _) => Validate();
        Units.Add(baseRow);
    }

    private void AddNewUnit()
    {
        var row = new ProductUnitRowViewModel
        {
            IsBaseUnit = false,
            SortOrder = Units.Count,
            Placeholder_UnitName = "مثال: طبق، كرتون"
        };
        row.PropertyChanged += (_, _) => Validate();
        Units.Add(row);
    }

    private void AddUnitWithChangeTracking(ProductUnitRowViewModel unit)
    {
        unit.PropertyChanged += (_, _) => Validate();
        Units.Add(unit);
    }

    private void RemoveUnit(ProductUnitRowViewModel unit)
    {
        if (unit.IsBaseUnit && Units.Count > 1)
        {
            ValidationSummary = "⚠️ لا يمكن حذف الوحدة الأساسية إذا كانت هناك وحدات أخرى مرتبطة بها.";
            HasValidationError = true;
            OnPropertyChanged(nameof(ValidationSummary));
            OnPropertyChanged(nameof(HasValidationError));
            return;
        }

        Units.Remove(unit);
        Validate();
    }

    public bool Validate()
    {
        var errors = new List<string>();

        var baseUnits = Units.Where(u => u.IsBaseUnit).ToList();

        if (baseUnits.Count == 0)
            errors.Add("⚠️ أضف وحدة صغرى واحدة (مثال: حبة) واجعل معامل التحويل = 1");

        if (baseUnits.Count > 1)
            errors.Add("⚠️ لا يمكن تعريف أكثر من وحدة صغرى واحدة");

        foreach (var unit in Units)
        {
            if (string.IsNullOrWhiteSpace(unit.UnitName))
                errors.Add($"⚠️ الصف {unit.SortOrder + 1}: اسم الوحدة مطلوب");

            if (!unit.IsBaseUnit && unit.BaseConversionFactor <= 1)
                errors.Add(
                    $"⚠️ '{unit.UnitName}': معامل التحويل يجب أن يكون أكبر من 1 " +
                    $"(كم وحدة صغرى بداخلها؟)");
        }

        ValidationSummary = errors.Any()
            ? string.Join("\n", errors)
            : "✅ وحدات المنتج صحيحة";

        HasValidationError = errors.Any();

        OnPropertyChanged(nameof(ValidationSummary));
        OnPropertyChanged(nameof(HasValidationError));

        return !errors.Any();
    }

    private void ShowOnboarding()
    {
        var dialog = new OnboardingDialog
        {
            Message =
                "💡 كيف تبني وحدات المنتج؟\n\n" +
                "1️⃣  ابدأ دائماً بإضافة الوحدة الصغرى\n" +
                "     التي لا يمكن تجزئتها (مثل: حبة)\n" +
                "     واجعل معامل التحويل = 1\n\n" +
                "2️⃣  ثم أضف الوحدات الأكبر\n" +
                "     (مثل: طبق، كرتون)\n" +
                "     واكتب كم (حبة) بداخلها.\n\n" +
                "     مثال: طبق البيض = 30 حبة\n" +
                "              كرتون = 12 طبق = 360 حبة\n\n" +
                "✅  النظام سيحسب كل شيء تلقائياً!"
        };
        dialog.ShowDialog();
    }
}
Task 4.2 — Purchase Invoice ViewModel (with Price Sync Indicator)
csharp

// File: WPF/ViewModels/Invoice/PurchaseInvoiceItemViewModel.cs

public class PurchaseInvoiceItemViewModel : BaseViewModel
{
    private readonly IMediator _mediator;

    private int _productUnitId;
    private decimal _quantity = 1;
    private decimal _unitCost;
    private decimal _newSalesPrice;
    private decimal _oldCostInDatabase;
    private string _unitName = string.Empty;

    // ─── Properties ───────────────────────────────

    public int ProductUnitId
    {
        get => _productUnitId;
        set => SetProperty(ref _productUnitId, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
                OnPropertyChanged(nameof(TotalCost));
        }
    }

    public decimal UnitCost
    {
        get => _unitCost;
        set
        {
            if (SetProperty(ref _unitCost, value))
            {
                OnPropertyChanged(nameof(TotalCost));
                OnPropertyChanged(nameof(CostChangedFromDatabase));
                OnPropertyChanged(nameof(PriceDifferenceIndicator));
            }
        }
    }

    public decimal NewSalesPrice
    {
        get => _newSalesPrice;
        set => SetProperty(ref _newSalesPrice, value);
    }

    public decimal TotalCost => (Quantity * UnitCost) - Discount;
    public decimal Discount { get; set; }

    // ⭐ KEY: Shows sync warning icon when cost differs from DB
    public bool CostChangedFromDatabase =>
        _oldCostInDatabase > 0 &&
        Math.Abs(UnitCost - _oldCostInDatabase) > 0.0001m;

    public string PriceDifferenceIndicator
    {
        get
        {
            if (!CostChangedFromDatabase) return string.Empty;

            var diff = UnitCost - _oldCostInDatabase;
            var direction = diff > 0 ? "↑ ارتفع" : "↓ انخفض";
            return $"🔄 {direction} عن السعر القديم ({_oldCostInDatabase:N2}) " +
                   $"| سيتم تحديث التكلفة في بطاقة الصنف عند الحفظ";
        }
    }

    // ─── Available units for ComboBox ─────────────
    public ObservableCollection<ProductUnitOption> AvailableUnits { get; } = new();

    private ProductUnitOption? _selectedUnit;
    public ProductUnitOption? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (SetProperty(ref _selectedUnit, value) && value != null)
                _ = OnUnitChangedAsync(value);
        }
    }

    private async Task OnUnitChangedAsync(ProductUnitOption unit)
    {
        ProductUnitId = unit.UnitId;

        // Load current cost from DB for comparison
        var currentData = await _mediator.Send(
            new GetProductUnitPricingQuery(unit.UnitId));

        _oldCostInDatabase = currentData.PurchaseCost;
        UnitCost = currentData.PurchaseCost;         // Pre-fill with DB cost
        NewSalesPrice = currentData.SalesPrice;      // Pre-fill sales price

        OnPropertyChanged(nameof(CostChangedFromDatabase));
        OnPropertyChanged(nameof(PriceDifferenceIndicator));
    }

    public void SetProduct(BarcodeSearchResult result, List<ProductUnitOption> units)
    {
        AvailableUnits.Clear();
        foreach (var unit in units)
            AvailableUnits.Add(unit);

        _selectedUnit = units.First(u => u.UnitId == result.ProductUnitId);
        ProductUnitId = result.ProductUnitId;
        _oldCostInDatabase = result.PurchaseCost;
        UnitCost = result.PurchaseCost;
        NewSalesPrice = result.SalesPrice;

        OnPropertyChanged(nameof(SelectedUnit));
        OnPropertyChanged(nameof(TotalCost));
        OnPropertyChanged(nameof(CostChangedFromDatabase));
    }
}

public record ProductUnitOption(int UnitId, string UnitName, decimal ConversionFactor);
✅ Phase 4 Checklist
 ProductUnitBuilderViewModel.Validate() shows Arabic error messages
 Onboarding dialog shows automatically for new products
 CostChangedFromDatabase triggers when user edits cost field
 PriceDifferenceIndicator shows direction (↑/↓) and old value
 Unit ComboBox in purchase invoice pre-fills cost from DB

<!-- END OF SECTION: 8.2 Advanced WPF ViewModels -->


<!-- START OF SECTION: 8.3 Advanced WPF XAML Controls -->

> [!NOTE]
> XAML layout declarations for Dynamic UOM sliders, inline invoice rows, and settings views.

🖼️ Phase 5: WPF XAML
Task 5.1 — Unit Hierarchy Builder XAML
XML

<!-- File: Views/Products/UnitHierarchyBuilderControl.xaml -->
<UserControl x:Class="YourApp.Views.Products.UnitHierarchyBuilderControl"
             FlowDirection="RightToLeft">
    <StackPanel>

        <!-- Help Box (always visible) -->
        <Border Background="#E8F5E9" BorderBrush="#4CAF50"
                BorderThickness="1" CornerRadius="6"
                Padding="12" Margin="0,0,0,8">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBlock TextWrapping="Wrap" FontSize="12" Foreground="#2E7D32">
                    <Run FontWeight="Bold">💡 كيف تبني وحدات المنتج؟  </Run>
                    <LineBreak/>
                    <Run>1. ابدأ بالوحدة الصغرى (مثال: حبة) — معامل التحويل = 1</Run>
                    <LineBreak/>
                    <Run>2. أضف الوحدات الأكبر واكتب كم وحدة صغرى بداخلها</Run>
                </TextBlock>
                <Button Grid.Column="1"
                        Content="تفاصيل أكثر ؟"
                        Command="{Binding ShowHelpCommand}"
                        Background="Transparent"
                        Foreground="#4CAF50"
                        BorderThickness="0"
                        FontSize="11"/>
            </Grid>
        </Border>

        <!-- Validation Summary -->
        <Border Background="#FFEBEE"
                BorderBrush="#F44336"
                BorderThickness="1"
                CornerRadius="4"
                Padding="10"
                Margin="0,0,0,8"
                Visibility="{Binding HasValidationError,
                             Converter={StaticResource BoolToVisibility}}">
            <TextBlock Text="{Binding ValidationSummary}"
                       Foreground="#C62828"
                       TextWrapping="Wrap"/>
        </Border>

        <!-- Units DataGrid -->
        <DataGrid ItemsSource="{Binding Units}"
                  AutoGenerateColumns="False"
                  CanUserAddRows="False"
                  HeadersVisibility="Column"
                  GridLinesVisibility="Horizontal">
            <DataGrid.Columns>

                <!-- Unit Name -->
                <DataGridTemplateColumn Header="اسم الوحدة" Width="140">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBox Text="{Binding UnitName, UpdateSourceTrigger=PropertyChanged}"
                                     PlaceholderText="{Binding Placeholder_UnitName}"
                                     BorderThickness="0" Padding="4"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>

                <!-- Conversion Factor -->
                <DataGridTemplateColumn Header="يساوي كم وحدة صغرى؟" Width="160">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Grid>
                                <!-- Show "1 (أساسية)" for base unit -->
                                <TextBlock
                                    Text="1  ✅ (وحدة أساسية)"
                                    Foreground="#4CAF50"
                                    VerticalAlignment="Center"
                                    Padding="4"
                                    Visibility="{Binding IsBaseUnit,
                                                 Converter={StaticResource BoolToVisibility}}"/>

                                <!-- Editable for derived units -->
                                <TextBox
                                    Text="{Binding BaseConversionFactor,
                                                   UpdateSourceTrigger=PropertyChanged}"
                                    Visibility="{Binding IsBaseUnit,
                                                 Converter={StaticResource InverseBoolToVisibility}}"
                                    BorderThickness="0"
                                    Padding="4"/>
                            </Grid>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>

                <!-- Sales Price -->
                <DataGridTextColumn Header="سعر البيع"
                    Binding="{Binding SalesPrice, UpdateSourceTrigger=PropertyChanged}"
                    Width="90"/>

                <!-- Purchase Cost -->
                <DataGridTextColumn Header="تكلفة الشراء"
                    Binding="{Binding PurchaseCost, UpdateSourceTrigger=PropertyChanged}"
                    Width="90"/>

                <!-- Supplier Price -->
                <DataGridTextColumn Header="سعر المورد"
                    Binding="{Binding SupplierPrice, UpdateSourceTrigger=PropertyChanged}"
                    Width="90"/>

                <!-- Last Purchase Price (Read Only) -->
                <DataGridTextColumn Header="آخر سعر توريد"
                    Binding="{Binding LastPurchasePrice, StringFormat=N2}"
                    Width="110"
                    IsReadOnly="True">
                    <DataGridTextColumn.ElementStyle>
                        <Style TargetType="TextBlock">
                            <Setter Property="Foreground" Value="#1565C0"/>
                            <Setter Property="FontWeight" Value="Bold"/>
                        </Style>
                    </DataGridTextColumn.ElementStyle>
                </DataGridTextColumn>

                <!-- Barcode Count -->
                <DataGridTextColumn Header="عدد الباركودات"
                    Binding="{Binding BarcodesCount}"
                    Width="110"
                    IsReadOnly="True"/>

                <!-- Delete Button -->
                <DataGridTemplateColumn Header="" Width="40">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Button Content="🗑"
                                    Command="{Binding DataContext.RemoveUnitCommand,
                                              RelativeSource={RelativeSource AncestorType=UserControl}}"
                                    CommandParameter="{Binding}"
                                    Background="Transparent"
                                    BorderThickness="0"
                                    Foreground="#EF5350"
                                    FontSize="14"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>
            </DataGrid.Columns>
        </DataGrid>

        <!-- Add Unit Button -->
        <Button Content="+ إضافة وحدة جديدة"
                Command="{Binding AddUnitCommand}"
                HorizontalAlignment="Left"
                Margin="0,8,0,0"
                Padding="12,6"
                Background="#E3F2FD"
                Foreground="#1565C0"
                BorderThickness="1"
                BorderBrush="#90CAF9"/>
    </StackPanel>
</UserControl>
Task 5.2 — Purchase Invoice Item Row (Price Sync Indicator)
XML

<!-- Inside Purchase Invoice DataGrid -->
<!-- Add these two columns to existing DataGrid -->

<!-- Unit Cost with sync indicator -->
<DataGridTemplateColumn Header="تكلفة الوحدة" Width="130">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBox Text="{Binding UnitCost, UpdateSourceTrigger=PropertyChanged}"
                         BorderThickness="0"/>
                <!-- Sync warning — only shows when cost differs from DB -->
                <TextBlock Text="{Binding PriceDifferenceIndicator}"
                           FontSize="10"
                           Foreground="#E65100"
                           TextWrapping="Wrap"
                           Visibility="{Binding CostChangedFromDatabase,
                                        Converter={StaticResource BoolToVisibility}}"/>
            </StackPanel>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>

<!-- New Sales Price (optional override) -->
<DataGridTemplateColumn Header="سعر البيع الجديد" Width="120">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <TextBox Text="{Binding NewSalesPrice, UpdateSourceTrigger=PropertyChanged}"
                     PlaceholderText="اختياري"
                     BorderThickness="0"
                     ToolTip="إذا أدخلت سعراً جديداً، سيتم تحديث سعر بيع الصنف فور حفظ الفاتورة"/>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
Task 5.3 — Settings Screen (Costing Method Selector)
XML

<!-- File: Views/Settings/CostingMethodSettingView.xaml -->
<StackPanel Margin="16" FlowDirection="RightToLeft">

    <TextBlock Text="طريقة احتساب تكلفة المخزون"
               FontSize="16" FontWeight="Bold" Margin="0,0,0,12"/>

    <!-- Option 1: Weighted Average -->
    <Border BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="8"
            Padding="16" Margin="0,4">
        <StackPanel>
            <RadioButton Content="متوسط التكلفة المرجح  (Weighted Average)"
                         IsChecked="{Binding IsWeightedAverageSelected}"
                         FontSize="14" FontWeight="Bold"/>
            <TextBlock Margin="24,6,0,0" TextWrapping="Wrap" Foreground="#555"
                       Text="يجمع بين سعر البضاعة القديمة في المخزن والجديدة ليعطيك تكلفة موحدة ومتوازنة. ✅ الأنسب للتقارير الضريبية الدقيقة وللمحاسبة القياسية."/>
        </StackPanel>
    </Border>

    <!-- Option 2: Last Purchase Price -->
    <Border BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="8"
            Padding="16" Margin="0,4">
        <StackPanel>
            <RadioButton Content="آخر سعر توريد  (Last Purchase Price)"
                         IsChecked="{Binding IsLastPriceSelected}"
                         FontSize="14" FontWeight="Bold"/>
            <TextBlock Margin="24,6,0,0" TextWrapping="Wrap" Foreground="#555"
                       Text="يستبدل تكلفة المنتج بسعر آخر فاتورة شراء مباشرةً. ✅ مناسب للأسواق المتقلبة حيث تريد دائماً أن يعكس السعر الواقع الحالي."/>
        </StackPanel>
    </Border>

    <!-- Option 3: Supplier Price -->
    <Border BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="8"
            Padding="16" Margin="0,4">
        <StackPanel>
            <RadioButton Content="سعر المورد  (Supplier Catalog Price)"
                         IsChecked="{Binding IsSupplierPriceSelected}"
                         FontSize="14" FontWeight="Bold"/>
            <TextBlock Margin="24,6,0,0" TextWrapping="Wrap" Foreground="#555"
                       Text="يعتمد على السعر المدخل في بطاقة الصنف من قائمة المورد ولا يتغير تلقائياً عند الشراء. ✅ مناسب عندما تتفاوض على سعر ثابت مع المورد لفترة طويلة."/>
        </StackPanel>
    </Border>

    <Button Content="💾  حفظ الإعداد"
            Command="{Binding SaveCostingMethodCommand}"
            HorizontalAlignment="Left"
            Margin="0,16,0,0"
            Padding="20,10"
            Background="#1976D2" Foreground="White" BorderThickness="0"/>
</StackPanel>
✅ Phase 5 Checklist
 Help box always visible (not just on first open)
 Validation error box only shows when HasValidationError = true
 Base unit row shows "✅ وحدة أساسية" and factor is read-only
 Sync warning shows correct direction (↑/↓) with old price
 Each costing method has Arabic explanation text
 All interactive elements minimum 36px height (touch-friendly)

<!-- END OF SECTION: 8.3 Advanced WPF XAML Controls -->


# 9. Clean Architecture & Production Engineering Guidelines

Detailed breakdown of Clean Architecture layers, project dependencies, design systems, and cross-cutting concerns.

<!-- START OF SECTION: 9.1 Core Philosophy & Layered Architecture -->

> [!NOTE]
> Architecture constraints, boundary directories, and clean separation of concerns.

# MASTER-PLAN — Sales Management System (v4.6.2 — Validation ErrorTemplate & INotifyDataErrorInfo)

## 📋 Core Philosophy

**One source of truth. AGENTS.md is LAW.** Every rule lives in exactly ONE place. Agents cannot break what they cannot bypass.

- **Clean Architecture (Layered)** — NOT Vertical Slices, NOT Feature Folders
- **Domain is king** — ZERO dependencies, rich entities, business rules enforced at the entity level
- **Desktop → API → SQL Server** — Desktop NEVER connects to the database
- **Result<T> over exceptions** — Services return results, controllers translate to HTTP
- **Bilingual UI** — Arabic labels, English code. All text columns use `nvarchar`
- **AGENTS.md > everything** — If code conflicts with AGENTS.md, the code is wrong

---

## 🏗️ Actual Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        SOLUTION STRUCTURE (11 Projects)                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  SalesSystem.slnx                                                       │
│  ├── 📦 SalesSystem.Domain/          ← Entities + Enums + Exceptions    │
│  │      (net10.0, ZERO NuGet deps)                                      │
│  │                                                                       │
│  ├── 📦 SalesSystem.Contracts/       ← DTOs + Requests + Result<T>      │
│  │      (net10.0, ZERO NuGet deps)                                      │
│  │                                                                       │
│  ├── 📦 SalesSystem.Application/     ← Service interfaces + impls       │
│  │      (net10.0)                                                        │
│  │                                                                       │
│  ├── 📦 SalesSystem.Infrastructure/  ← EF Core + DbContext + Repos      │
│  │      (net10.0-windows)           + Printing + Backup                 │
│  │                                                                       │
│  ├── 📦 SalesSystem.Api/             ← Controllers + FluentValidation   │
│  │      (net10.0-windows)           + JWT + Serilog + Swagger           │
│  │                                                                       │
│  ├── 📦 SalesSystem.DesktopPWF/      ← WPF UI + MVVM + EventBus         │
│  │      (net10.0-windows)           + Navigation + Dialogs              │
│  │                                                                       │
│  └── 🧪 Tests/ (5 projects)          ← Unit + Integration tests         │
│                                                                         │
│  Legacy/ (NOT in solution)                                              │
│  └── 🗑️ SalesSystem.Desktop/         ← Abandoned WinForms (safe delete) │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

Data Flow (NEVER break this chain):

  Desktop (WPF)
      ↓ HttpClient
  SalesSystem.Api (Controllers + FluentValidation + JWT)
      ↓ delegates to
  SalesSystem.Application (Service interfaces + implementations)
      ↓ delegates to
  SalesSystem.Infrastructure (EF Core + DbContext + Repositories)
      ↓ connects to
  SQL Server
      ↑
  SalesSystem.Domain (ZERO dependencies — referenced by ALL layers)
```

### Architecture Pattern: Clean Architecture (Layered)

| Layer | Responsibility | Dependencies |
|-------|---------------|--------------|
| **Domain** | Entities, Enums, Exceptions, Business Rules | NONE |
| **Contracts** | DTOs, Request/Response models, `Result<T>` | Domain |
| **Application** | Service interfaces + implementations, Use Cases | Domain, Contracts |
| **Infrastructure** | EF Core DbContext, Repositories, UoW, Printing, Backup | Application, Contracts, Domain |
| **Api** | Controllers, FluentValidation, JWT Auth, Serilog, Swagger | Application, Infrastructure |
| **DesktopPWF** | WPF Views, ViewModels (MVVM), EventBus, Navigation | Contracts (via HTTP) |

**Key decisions:**
- **Service Layer** pattern (NOT CQRS/MediatR)
- **IUnitOfWork** for multi-table operations
- **Rich Domain Model** — entities have `private set` + factory methods + guard clauses
- **4-layer validation** — Domain → Application → API (FluentValidation) → Database (CHECK constraints)

---

## ✅ Implemented Features (Phases 1-7)

| Phase | Status | Key Deliverables |
|-------|--------|-----------------|
| **Phase 1: Foundation** | ✅ Complete | Domain entities (Product, Customer, Supplier, Invoice, etc.), Enums, DomainException, Guard Clauses, Contracts (DTOs, Requests, Result<T>) |
| **Phase 2: Infrastructure** | ✅ Complete | EF Core DbContext, Repositories, IUnitOfWork, Migrations, Fluent API config, CHECK constraints, Seed data |
| **Phase 3: Application** | ✅ Complete | Service interfaces + implementations for all modules (Products, Customers, Suppliers, Sales, Purchases, Returns, Stock, Reports, Settings, Users, CashBoxes, Inventory) |
| **Phase 4: API** | ✅ Complete | REST Controllers for all modules, FluentValidation validators, JWT authentication, Policy-based authorization, Swagger/OpenAPI, Serilog logging, Error middleware |
| **Phase 5: Desktop Shell** | ✅ Complete | WPF application, Navigation system, MVVM infrastructure, ViewModelBase (292 lines), EventBus, Login screen, Session management, Role-based UI |
| **Phase 6: Desktop Modules** | ✅ Complete | All CRUD screens (Products, Customers, Suppliers, Categories, Units, Warehouses), Sales/Purchase invoices, Returns, Stock transfers, Payments, Reports (Excel export), Barcode input |
| **Phase 7: Production** | ✅ Complete | Auto-Update system, DPAPI encryption, Backup/Restore (raw SQL), Windows Service, Admin screens, Inno Setup installer, Styled dialogs (6 types), Toast notifications, Print engine (A4 + Thermal) |

---

## 🔧 Actual Code Patterns

### ViewModel Pattern

```csharp
// ViewModelBase.cs (292 lines)
// Located: SalesSystem.DesktopPWF/Services/App/ViewModelBase.cs

public abstract class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
{
    // INotifyPropertyChanged
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null);
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null);

    // Commands
    public class RelayCommand : ICommand { ... }
    public class AsyncRelayCommand : ICommand { ... }

    // INotifyDataErrorInfo
    public void AddError(string propertyName, string errorMessage);
    public void ClearErrors(string propertyName);
    public void ClearAllErrors();
    public bool HasErrors { get; }

    // Error handling
    protected void HandleException(Exception ex, string context);
    protected void HandleFailure(string error, string context);

    // State
    public bool IsBusy { get; protected set; }
    public string StatusMessage { get; protected set; }
}
```

**Key features:**
- `INotifyDataErrorInfo` for real-time validation with red border styles
- `RelayCommand` and `AsyncRelayCommand` with `CanExecute`
- `HandleException()` and `HandleFailure()` for centralized error handling
- Save buttons disabled via `CanExecute` when `HasErrors` is true

### Service Pattern

```csharp
// Interface
public interface IProductService
{
    Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<List<ProductDto>>> GetAllAsync(CancellationToken ct);
    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}

// Implementation
public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<ProductService> _logger;

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        // 1. Validate pre-conditions
        // 2. Open transaction
        // 3. Save entity
        // 4. Commit
        // 5. Return Result<T>
    }
}
```

**Key rules:**
- ALL services return `Result<T>` or `Result` — NEVER throw exceptions
- Multi-table operations use `IUnitOfWork.BeginTransactionAsync()`
- Stock validated BEFORE opening transaction
- `InventoryMovement` recorded for EVERY stock change

### Controller Pattern

```csharp
[Authorize(Policy = "ManagerAndAbove")]
[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _service;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
```

**Key rules:**
- Controllers have ZERO business logic — delegate to services
- `[Authorize]` on ALL endpoints (except `/api/auth/login`)
- Policy-based authorization (`AdminOnly`, `ManagerAndAbove`, `AllStaff`)
- Translate `Result<T>` to HTTP status codes

### Domain Pattern

```csharp
public class Product : EntityBase
{
    public string Name { get; private set; }
    public decimal AvgCost { get; private set; }
    public ICollection<ProductUnit> Units { get; private set; }

    // Factory method with guard clauses
    public static Product Create(string name, int categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المنتج مطلوب");
        if (categoryId <= 0)
            throw new DomainException("الفئة مطلوبة");

        return new Product { Name = name, CategoryId = categoryId };
    }

    // State change via method
    public void UpdatePrice(decimal retailPrice, decimal wholesalePrice)
    {
        if (retailPrice < 0)
            throw new DomainException("سعر التجزئة لا يمكن أن يكون سالباً");
        if (wholesalePrice < 0)
            throw new DomainException("سعر الجملة لا يمكن أن يكون سالباً");
        // ... update logic
    }
}
```

**Key rules:**
- `private set` on ALL critical properties
- State changes via methods ONLY — never direct property modification
- Guard clauses in constructors and factory methods
- `DomainException` with Arabic messages

### Validation (4 Layers)

| Layer | Where | Example |
|-------|-------|---------|
| **Domain** | Entity methods | `if (price < 0) throw DomainException("السعر لا يمكن أن يكون سالباً")` |
| **Application** | Service methods | Stock availability check before transaction |
| **API** | FluentValidation | `RuleFor(x => x.Name).NotEmpty().WithMessage("الاسم مطلوب")` |
| **Database** | CHECK constraints | `CHECK (Quantity >= 0)`, `CHECK (PaidAmount <= TotalAmount)` |

---


<!-- END OF SECTION: 9.1 Core Philosophy & Layered Architecture -->


<!-- START OF SECTION: 9.2 WPF Input Validation Architecture (INotifyDataErrorInfo & ErrorTemplate) -->

> [!NOTE]
> Real-time validation engine replacing archaic booleans with centralized async input validation.

## 📊 Test Coverage

| Test Project | Target | Status |
|-------------|--------|--------|
| **SalesSystem.Domain.Tests** | Domain entities, guard clauses, business rules | ✅ Active |
| **SalesSystem.Application.Tests** | Service logic, Result<T> patterns | ✅ Active |
| **SalesSystem.Infrastructure.Tests** | EF Core mappings, repositories, migrations | ✅ Active |
| **SalesSystem.Api.Tests** | Controller endpoints, validation, auth | ✅ Active |
| **SalesSystem.Integration.Tests** | End-to-end flows, API + DB integration | ✅ Active |

---


<!-- END OF SECTION: 9.2 WPF Input Validation Architecture (INotifyDataErrorInfo & ErrorTemplate) -->


<!-- START OF SECTION: 9.3 Project Dependencies & Design System -->

> [!NOTE]
> Nuget package manifests and WPF resource styles used to implement modern Dark Mode / Glassmorphism layouts.

## 📦 Project Dependencies

```
SalesSystem.Domain
  └── (ZERO dependencies — pure C#)

SalesSystem.Contracts
  └── SalesSystem.Domain

SalesSystem.Application
  ├── SalesSystem.Domain
  └── SalesSystem.Contracts
  └── Microsoft.Extensions.Logging.Abstractions
  └── MediatR (installed, minimally used)

SalesSystem.Infrastructure
  ├── SalesSystem.Application
  ├── SalesSystem.Contracts
  ├── SalesSystem.Domain
  └── Microsoft.EntityFrameworkCore.SqlServer 10.x
  └── BCrypt.Net-Next 4.x
  └── QuestPDF 2024.3.x
  └── SixLabors.ImageSharp 3.1.x
  └── System.Drawing.Common 10.x
  └── Microsoft.Extensions.Hosting.WindowsServices 10.x
  └── Microsoft.AspNetCore.DataProtection 10.x

SalesSystem.Api
  ├── SalesSystem.Application
  ├── SalesSystem.Contracts
  ├── SalesSystem.Infrastructure
  ├── SalesSystem.Domain
  └── FluentValidation.AspNetCore 11.x
  └── Serilog.AspNetCore 8.x
  └── Microsoft.AspNetCore.Authentication.JwtBearer 10.x
  └── Swashbuckle.AspNetCore 6.x
  └── Serilog.Sinks.EventLog 8.x

SalesSystem.DesktopPWF
  ├── SalesSystem.Contracts
  ├── SalesSystem.Domain
  └── Microsoft.Extensions.Http 10.x
  └── System.Text.Json 10.x
  └── ClosedXML 0.102.x
```

---

## 🎨 Design System (Actual)

**Location:** `SalesSystem.DesktopPWF/Resources/Styles.xaml` (782 lines)

**NOT** `DesignTokens.cs` — that file was NEVER created. All styles are centralized in a single XAML ResourceDictionary.

### What's in Styles.xaml:

- **Color Brushes** — Primary, Success, Warning, Error, Info, Neutral palette
- **Typography** — TextBlock styles for Display, Header, SubHeader, Body, Caption
- **Button Styles** — Primary, Secondary, Danger, Success, Ghost, Icon
- **Card Styles** — Card (with shadow), CardFlat (no shadow)
- **Input Styles** — TextBox, ComboBox, PasswordBox
- **DataGrid Styles** — Standard grid with alternating rows, styled headers
- **Status Badges** — Success, Warning, Error badges
- **Validation Styles** — Red border for validation errors
- **Dialog Styles** — Styled dialogs (Error, Success, Warning, Info, Confirmation, DeleteConfirmation)
- **Navigation Styles** — Sidebar, menu items, active state
- **Toast Styles** — Notification toasts with auto-dismiss

### Usage Pattern:

```xml
<!-- In any XAML view -->
<Button Style="{StaticResource ButtonPrimary}" Content="حفظ"/>
<TextBlock Style="{StaticResource TextHeader}" Text="المنتجات"/>
<TextBox Style="{StaticResource TextBoxStandard}" Text="{Binding Name}"/>
<DataGrid Style="{StaticResource DataGridStandard}" .../>
<Border Style="{StaticResource BadgeSuccess}" .../>
```

**Rule:** NEVER hardcode colors or sizes in XAML views — always use `{StaticResource ...}`.

---


<!-- END OF SECTION: 9.3 Project Dependencies & Design System -->


# 10. Core Enterprise & Integration Services

Production-grade hardware integration, machine data encryption, high-performance reporting, and system hosting.

<!-- START OF SECTION: 10.1 Hardware Barcode Scanner Service -->

> [!NOTE]
> Event-driven barcode listener capturing continuous keystrokes for instant product scanning.

## 📡 Barcode Service (Actual)

**Interface:** `IBarcodeInputService` (NOT `IBarcodeScanner`)
**Implementation:** `BarcodeInputService`
**Location:** `SalesSystem.DesktopPWF/Services/App/Barcode/`

### How it works:

USB barcode scanners act as keyboard emulators — they type the barcode characters then send Enter. The service intercepts at the application level using a keyboard buffer with timing detection.

```csharp
public interface IBarcodeInputService
{
    event Action<string> BarcodeScanned;
    void StartListening();
    void StopListening();
}
```

### Key characteristics:
- **Keyboard buffer** — accumulates characters typed by scanner
- **100ms timeout** — distinguishes scanner (fast) from human typing (slow)
- **Application-level** — works across all screens, no per-screen setup
- **USB/HID only** — NO camera-based scanning (MAUI was never built)
- **Event-driven** — fires `BarcodeScanned` event with barcode string

### Usage in ViewModel:

```csharp
public class SalesInvoiceCreateViewModel : ViewModelBase
{
    public SalesInvoiceCreateViewModel(IBarcodeInputService barcodeService)
    {
        barcodeService.BarcodeScanned += OnBarcodeScanned;
    }

    private void OnBarcodeScanned(string barcode)
    {
        // Lookup product by barcode and add to invoice
        _ = AddProductByBarcodeAsync(barcode);
    }
}
```

---


<!-- END OF SECTION: 10.1 Hardware Barcode Scanner Service -->


<!-- START OF SECTION: 10.2 Production-Grade DPAPI Encryption Security -->

> [!NOTE]
> Local machine-bound cryptography securing database credentials on local POS terminals.

## 🔐 Security (Actual)

### DPAPI Connection String Encryption

- Connection strings encrypted via `IDataProtector` with `"DPAPI:"` prefix
- `FirstRunSetupService` encrypts plaintext connection string on first run (idempotent)
- `SecureDbContextFactory` decrypts before creating DbContext
- DataProtection keys stored in `%ProgramData%\SalesSystem\DataProtectionKeys`
- `appsettings.json` writes use atomic pattern: `.tmp` → `File.Replace()` → `.bak`

### JWT Authentication

- JWT secret from environment variable — throws `InvalidOperationException` in production if missing
- `BCrypt` passwords with work factor = 12
- Policy-based authorization: `AdminOnly`, `ManagerAndAbove`, `AllStaff`
- ALL endpoints require `[Authorize]` (except `/api/auth/login`)

### Security Audit

- `SecurityAudit.cs` runs in DEBUG only — checks for unencrypted strings, hardcoded passwords
- NEVER log: passwords, connection strings
- Serilog for all logging — NEVER `Console.WriteLine`

---


<!-- END OF SECTION: 10.2 Production-Grade DPAPI Encryption Security -->


<!-- START OF SECTION: 10.3 High-Performance Print Engine (QuestPDF & Thermal Raw Printing) -->

> [!NOTE]
> High-performance Win32 raw buffer thermal printing and QuestPDF invoice report generator.

## 🖨️ Print Engine (Actual)

**NOT WPF FixedDocument/PrintDialog** — uses QuestPDF + Win32 raw printing.

### A4 Invoices (QuestPDF)

- **Library:** QuestPDF Community (free for < $1M revenue)
- **Document:** `A4InvoiceDocument.cs` — RTL layout, logo, tax breakdown
- **Output:** PDF files
- **Preview:** WPF `PdfPreviewWindow` (WebBrowser control)

### Thermal Receipts (Win32 Raw Printing)

- **API:** Win32 `OpenPrinter` / `WritePrinter` via `DllImport`
- **Builder:** Custom `EscPos` static class — NOT external NuGet packages
- **Format:** 42-char monospaced columns, Windows-1256 encoding for Arabic
- **Output:** Direct to thermal printer (80mm)

### Architecture:

```
Desktop → IPrintApiService (HTTP) → PrintController (API) → IPrintService → Printer
```

**Desktop NEVER prints directly** — always goes through the API.

### API Endpoints:

```
GET    /api/v1/print/sales/{id}/preview
POST   /api/v1/print/sales/{id}/a4
POST   /api/v1/print/sales/{id}/thermal
POST   /api/v1/print/sales/{id}/save
GET    /api/v1/print/purchases/{id}/preview
POST   /api/v1/print/purchases/{id}/a4
POST   /api/v1/print/purchases/{id}/thermal
POST   /api/v1/print/purchases/{id}/save
POST   /api/v1/print/test
```

### PrintResult Pattern:

```csharp
public class PrintResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public string? FilePath { get; }
}
```

**NEVER throw from printing code** — always return `PrintResult`.

### Project Structure:

```
SalesSystem.Application/Printing/
├── Contracts/
│   ├── InvoicePrintDto.cs
│   ├── InvoiceItemPrintDto.cs
│   ├── InvoiceTypePrint.cs
│   └── PrintResult.cs
├── InvoicePrintDtoBuilder.cs
└── IPrintService.cs

SalesSystem.Infrastructure/Printing/
├── A4InvoiceDocument.cs
├── ThermalReceiptGenerator.cs
├── EscPos.cs
├── PrintService.cs
├── PrinterException.cs
└── PrintingBootstrapper.cs
```

---


<!-- END OF SECTION: 10.3 High-Performance Print Engine (QuestPDF & Thermal Raw Printing) -->


<!-- START OF SECTION: 10.4 Automated Enterprise Administration (Auto-Update & Daily Backup) -->

> [!NOTE]
> Silent background updater with SHA256 integrity check and automated database backup routines.

## 🔄 Auto-Update (Actual)

**Location:** `SalesSystem.DesktopPWF/Services/Update/`

### Key rules:
- **NEVER blocks startup** — fire-and-forget with silent failure
- **8-second timeout** — user never waits for update check
- **SHA256 checksum** verification before launching installer
- **Skipped version** persisted to `%AppData%\SalesSystem\settings.json`
- **Desktop calls API** for updates — NEVER implements its own HTTP download
- **`Environment.Exit(0)` is FORBIDDEN** — return `Result<bool>` instead

### IUpdaterService Interface:

```csharp
public interface IUpdaterService
{
    Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(CancellationToken ct = default);
    Task<Result<string>> DownloadUpdateAsync(string downloadUrl, string expectedChecksum,
        IProgress<DownloadProgress> progress, CancellationToken ct = default);
    Task<Result<bool>> LaunchInstallerAndExitAsync(string installerPath);
    Result<string> GetCurrentVersion();
    Result SkipVersion(string version);
    Result<string> GetSkippedVersion();
}
```

### Version Comparison:

Uses `System.Version` — NEVER string comparison.

---

## 💾 Backup System (Actual)

**Location:** `SalesSystem.Infrastructure/Backup/`

### Key rules:
- **Raw SQL `BACKUP DATABASE`** — NEVER SMO dependency
- **Restore uses `SINGLE_USER WITH ROLLBACK AFTER 30`** — gives active transactions 30s
- **Scheduled backup** runs daily at 2:00 AM as `BackgroundService`
- **Retention** = configurable days (default 30) — old backups auto-deleted
- **Restore failure** triggers `TrySetMultiUserAsync` recovery
- **Config parsing** uses `int.TryParse` — NEVER `int.Parse`

### ScheduledBackupWorker:

```csharp
public class ScheduledBackupWorker : BackgroundService
{
    // Uses IServiceScopeFactory for scoped service resolution
    // Respects CancellationToken for graceful shutdown
    // Runs backup at 2:00 AM daily → then cleanup old backups
}
```

---


<!-- END OF SECTION: 10.4 Automated Enterprise Administration (Auto-Update & Daily Backup) -->


<!-- START OF SECTION: 10.5 Service Hosting & Database Diagnostics -->

> [!NOTE]
> Configuring the API to run as a native Windows Service and validating connection status on bootstrap.

## 🖥️ Windows Service (Actual)

**Location:** `SalesSystem.Api/Program.cs`

### Configuration:
- **Service name:** `SalesSystemService` (Arabic display name)
- **Auto-recovery:** 3 restarts on failure (1min, 5min, 15min delays)
- **Serilog EventLog sink** for Windows Service logging
- **SQL retry on startup:** 3 attempts × 5 second delay
- **Database migration** runs on service startup (auto-migrate)

### Program.cs Integration:

```csharp
builder.Host.UseWindowsService(options => options.ServiceName = "SalesSystemService");
// + Serilog EventLog sink + SQL retry + FirstRunSetupService
```

---


<!-- END OF SECTION: 10.5 Service Hosting & Database Diagnostics -->


# 11. API Controllers & Messaging Infrastructure

HTTP endpoints mapping requests to use cases and weak-reference desktop EventBus decoupling ViewModels.

<!-- START OF SECTION: 11.1 Baseline API Controllers -->

> [!NOTE]
> Controller mapping sales commands and returns to HTTP status codes.

// SalesSystem.Api/Controllers/SalesController.cs
[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;

    public SalesController(ISalesService salesService)
    {
        _salesService = salesService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSalesInvoiceRequest request,
        CancellationToken ct)
    {
        // userId من JWT لاحقاً — الآن null
        var result = await _salesService.CreateInvoiceAsync(request, null, ct);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCodes.InsufficientStock => 
                    BadRequest(new { error = result.Error, code = result.ErrorCode }),
                ErrorCodes.NotFound => 
                    NotFound(new { error = result.Error }),
                ErrorCodes.ValidationError => 
                    BadRequest(new { error = result.Error }),
                _ => 
                    StatusCode(500, new { error = result.Error })
            };
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.SalesInvoiceId },
            result.Value);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _salesService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpDelete("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int id, [FromBody] CancelRequest request, CancellationToken ct)
    {
        var result = await _salesService.CancelInvoiceAsync(
            id, request.Reason, null, ct);

        return result.IsSuccess
            ? Ok(new { message = "تم إلغاء الفاتورة بنجاح" })
            : BadRequest(new { error = result.Error });
    }
}
12. Desktop — EventBus
csharp


<!-- END OF SECTION: 11.1 Baseline API Controllers -->


<!-- START OF SECTION: 11.2 Weak-Reference EventBus & Messaging -->

> [!NOTE]
> WPF decoupled messaging engine using weak-references to prevent memory leaks.

// SalesSystem.Desktop/Messaging/EventBus.cs
namespace SalesSystem.Desktop.Messaging;

public interface IEventBus
{
    IDisposable Subscribe<T>(Action<T> handler);
    void Publish<T>(T message);
}

public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<WeakReference<Delegate>>> 
        _subscribers = new();
    private readonly object _lock = new();

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        lock (_lock)
        {
            var type = typeof(T);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<WeakReference<Delegate>>();

            var weakRef = new WeakReference<Delegate>(handler);
            _subscribers[type].Add(weakRef);

            return new Subscription(() =>
            {
                lock (_lock)
                    _subscribers[type].Remove(weakRef);
            });
        }
    }

    public void Publish<T>(T message)
    {
        List<WeakReference<Delegate>> handlers;
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(typeof(T), out var list))
                return;
            handlers = list.ToList();
        }

        foreach (var weakRef in handlers)
        {
            if (weakRef.TryGetTarget(out var del) && del is Action<T> handler)
            {
                // ⚠️ تنفيذ على UI Thread (WPF)
                Application.Current.Dispatcher.InvokeAsync(() => handler(message));
            }
        }
    }

    private class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose() => _unsubscribe();
    }
}

// Messages (ID only — no data payloads per RULE-034)
public record ProductChangedMessage(int ProductId);
public record SaleInvoiceChangedMessage(int InvoiceId);
public record PurchaseInvoiceChangedMessage(int InvoiceId);
public record StockChangedMessage(int ProductId, int WarehouseId);
public record CustomerBalanceChangedMessage(int CustomerId);
public record SupplierBalanceChangedMessage(int SupplierId);

<!-- END OF SECTION: 11.2 Weak-Reference EventBus & Messaging -->


# 12. System Testing Suite & Automation Framework

Unit tests validating domain cost updates, dynamic unit adjustments, cash box rules, and coverage standards.

<!-- START OF SECTION: 12.1 Advanced Modules Unit Tests -->

> [!NOTE]
> C# XUnit test suites verifying retail average prices, stock reductions, and cash box opening limits.

🧪 Phase 6: Unit Tests
csharp

// File: Tests/Domain/ProductUnitTests.cs

public class ProductUnitTests
{
    [Fact]
    public void CreateBaseUnit_AlwaysHasFactorOne()
    {
        var unit = ProductUnit.CreateBaseUnit(productId: 1, unitName: "حبة");
        Assert.Equal(1, unit.BaseConversionFactor);
        Assert.True(unit.IsBaseUnit);
    }

    [Fact]
    public void CreateDerivedUnit_WithFactorOne_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            ProductUnit.CreateDerivedUnit(1, "كرتون", baseConversionFactor: 1));

        Assert.Contains("أكبر من وحدة صغرى واحدة", ex.Message);
    }

    [Fact]
    public void ToBaseUnitQuantity_MultipliesCorrectly()
    {
        var box = ProductUnit.CreateDerivedUnit(1, "كرتون", 12);
        var baseQty = box.ToBaseUnitQuantity(3); // 3 boxes × 12 = 36 pieces
        Assert.Equal(36, baseQty);
    }

    [Fact]
    public void CalculateCostFromBaseUnitCost_ScalesCorrectly()
    {
        var box = ProductUnit.CreateDerivedUnit(1, "كرتون", 12);
        var boxCost = box.CalculateCostFromBaseUnitCost(baseUnitCost: 2m);
        Assert.Equal(24m, boxCost); // 2 SAR/piece × 12 pieces = 24 SAR/box
    }
}

// File: Tests/Application/WeightedAverageCostingTests.cs

public class WeightedAverageCostingTests
{
    [Fact]
    public async Task WeightedAverage_CalculatesCorrectly()
    {
        // Old stock: 10 pieces at 100 SAR each
        // New stock: 10 pieces at 150 SAR each
        // Expected: (10×100 + 10×150) / 20 = 125 SAR

        var mockContext = CreateMockContextWithStock(currentPieces: 10, currentCost: 100);
        var service = CreateService(mockContext, CostingMethod.WeightedAverage);

        var result = await service.CalculateNewCostAsync(
            currentCost: 100,
            currentStock: 10,
            newCost: 150,
            newQuantity: 10);

        Assert.Equal(125m, result);
    }

    [Fact]
    public async Task WeightedAverage_ZeroOldStock_ReturnsNewCost()
    {
        var result = await CalculateWeightedAverage(
            currentStock: 0, currentCost: 100,
            newCost: 150, newQuantity: 10);

        Assert.Equal(150m, result); // No old stock to average with
    }
}

// File: Tests/Domain/CashBoxTests.cs

public class CashBoxTests
{
    [Fact]
    public void Withdraw_InsufficientBalance_ThrowsDomainException()
    {
        var box = CashBox.Create("صندوق الكاشير");
        box.Deposit(100, CashTransactionType.ManualIn, createdBy: 1);

        var ex = Assert.Throws<DomainException>(() =>
            box.Withdraw(200, CashTransactionType.PurchaseOut, createdBy: 1));

        Assert.Contains("رصيد الصندوق غير كافٍ", ex.Message);
    }

    [Fact]
    public void Deposit_UpdatesBalanceAndCreatesTransaction()
    {
        var box = CashBox.Create("صندوق الكاشير");
        box.Deposit(500, CashTransactionType.SaleIn, createdBy: 1);

        Assert.Equal(500, box.CurrentBalance);
        Assert.Single(box.Transactions);
        Assert.Equal(0, box.Transactions.First().BalanceBefore);
        Assert.Equal(500, box.Transactions.First().BalanceAfter);
    }

    [Fact]
    public void CanUserAccess_WrongUser_ThrowsDomainException()
    {
        var box = CashBox.Create("صندوق كاشير 1", assignedUserId: 5);

        Assert.Throws<DomainException>(() => box.CanUserAccess(userId: 99));
    }

    [Fact]
    public void CanUserAccess_SharedBox_AllowsAnyUser()
    {
        var sharedBox = CashBox.Create("الصندوق الرئيسي"); // No assigned user

        // Should NOT throw for any user
        var exception = Record.Exception(() => sharedBox.CanUserAccess(userId: 99));
        Assert.Null(exception);
    }
}
📦 Final Summary
text

┌──────────────────────────────────────────────────────────────────┐
│         DYNAMIC UOM + COSTING + CASH BOXES — IMPLEMENTATION      │
├──────┬──────────────────────────────────────────┬────────────────┤
│ Step │ Deliverable                              │ Key Rule       │
├──────┼──────────────────────────────────────────┼────────────────┤
│  0   │ 8 SQL migrations                         │ Run in order   │
│  1   │ 6 Domain entities + 2 enums              │ No DB in Domain│
│  2   │ EF configs + Barcode + Settings repos    │ Register in DI │
│  3   │ Pricing service + Commands               │ Save then price│
│  4   │ ViewModels (Builder + Invoice)           │ Arabic errors  │
│  5   │ XAML (Builder + Settings + Invoice)      │ 36px min touch │
│  6   │ 7 Unit tests                             │ Never skip     │
└──────┴──────────────────────────────────────────┴────────────────┘

CRITICAL RULES — NEVER VIOLATE:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Stock ALWAYS stored in base units (pieces) — never in boxes/trays
✅ Cost cascade: base unit cost × ConversionFactor = derived unit cost
✅ Pricing update runs AFTER invoice.Id is persisted
✅ WeightedAverage: zero old stock → return new cost (no division by zero)
✅ CashBox.Withdraw() uses domain method — never subtract balance directly
✅ Product.ValidateUnits() called in command handler before ANY save
✅ All user-facing errors in Arabic with actionable guidance
✅ Price history logged for EVERY cost change (audit trail)


<!-- END OF SECTION: 12.1 Advanced Modules Unit Tests -->


<!-- START OF SECTION: 12.2 Quality Assurance & Code Coverage Standard -->

> [!NOTE]
> Testing rules, mocking boundaries, and automated pipeline integration.

### Test Coverage Strategy
- **Mocking Framework:** Use `Moq` for service-layer mock requirements.
- **Database Testing:** Use EF Core In-Memory or SQLite in-memory to validate repository queries.
- **Coverage target:** Maintain >85% logic coverage on all core domain aggregates.
- **Validation tests:** Ensure command validation rules are covered by dedicated FluentValidation tests.

<!-- END OF SECTION: 12.2 Quality Assurance & Code Coverage Standard -->


<!-- START OF SECTION: 13. Critical Business Rules Reference (Arabic) -->

> [!NOTE]
> قواعد العمل والتحقق والحسابات المالية المعتمده للنظام بالتفصيل.

13. قواعد العمل الحرجة — مرجع سريع
text

╔══════════════════════════════════════════════════════════════════╗
║          قواعد العمل الحرجة — يجب تطبيقها في كل مكان           ║
╠══════════════════════════════════════════════════════════════════╣
║                                                                  ║
║  FINANCIAL CALCULATIONS                                          ║
║  ─────────────────────                                          ║
║  ✅ LineTotal = (Quantity * UnitPrice) - DiscountAmount          ║
║  ✅ SubTotal  = Sum(LineTotal)                                   ║
║  ✅ Total     = SubTotal - InvoiceDiscount + TaxAmount           ║
║  ✅ Due       = Total - PaidAmount                               ║
║  ❌ لا تحسب في الـ UI — الحساب في Domain Layer فقط             ║
║  ❌ لا float/double في أي حساب مالي                             ║
║                                                                  ║
║  STOCK RULES                                                     ║
║  ───────────                                                     ║
║  ✅ تحقق من المخزون قبل فتح Transaction                         ║
║  ✅ اخصم المخزون بعد حفظ الفاتورة وتوليد ID                    ║
║  ✅ سجل في InventoryMovements مع كل تغيير                       ║
║  ❌ لا بيع بكمية تتجاوز المتوفر في المخزن المحدد               ║
║  ❌ لا كميات سالبة في WarehouseStocks                           ║
║                                                                  ║
║  BALANCE RULES                                                   ║
║  ─────────────                                                   ║
║  Customer Balance موجب = العميل مدين لنا                        ║
║  Customer Balance سالب = لدينا رصيد للعميل                     ║
║  Supplier Balance موجب = علينا للمورد                           ║
║  Supplier Balance سالب = المورد مدين لنا                        ║
║                                                                  ║
║  TRANSACTION RULES                                               ║
║  ─────────────────                                               ║
║  ✅ كل فاتورة (بيع/شراء/مرتجع/تحويل) في Transaction واحدة     ║
║  ✅ إذا فشل أي خطوة → Rollback كامل                             ║
║  ❌ لا Partial Commits                                           ║
║                                                                  ║
║  INVOICE RULES                                                   ║
║  ─────────────                                                   ║
║  ✅ الفاتورة تبدأ Draft ثم تُرحَّل Posted                       ║
║  ✅ المخزون يتأثر عند Posted فقط                                ║
║  ✅ الإلغاء يعكس أثر المخزون والرصيد                           ║
║  ❌ لا حذف نهائي للفواتير                                       ║
║  ❌ لا تعديل على فاتورة Posted                                  ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝

<!-- END OF SECTION: 13. Critical Business Rules Reference (Arabic) -->


# 14. Implementation History & Evolution Roadmap

Comprehensive milestones history, technological decisions, legacy cleanup, and system roadmap.

<!-- START OF SECTION: 14.1 Baseline Project Implementation Phases (Phases 1-13) -->

> [!NOTE]
> Original implementation plan mapping out baseline modules.

14. خطة التنفيذ المرحلية
text

Phase 1 — Foundation (الأساس)
├── إنشاء الـ 6 مشاريع في Solution
├── Domain Entities كاملة
├── Contracts (DTOs + Requests + Result<T>)
├── Custom Exceptions
└── SQL Script للجداول كاملة مع Seed Data

Phase 2 — Infrastructure
├── SalesDbContext + جميع الـ Configurations
├── EF Core Migrations
├── IUnitOfWork + UnitOfWork
├── Repositories الأساسية
└── اختبار الاتصال بقاعدة البيانات

Phase 3 — Application Services
├── ProductService (البداية — الأبسط)
├── CustomerService + SupplierService
├── WarehouseService
├── DocumentSequenceService ⚠️ مهم
├── InventoryService ⚠️ حرج
├── PurchaseService ⚠️ حرج
├── SalesService ⚠️ الأحرج
├── SalesReturnService ⚠️
└── PurchaseReturnService ⚠️

Phase 4 — API
├── جميع الـ Controllers
├── Global Exception Handler
└── Swagger Setup

Phase 5 — Desktop Shell
├── EventBus
├── NavigationService
├── MainForm + Sidebar
├── LoginForm
└── Common Controls

Phase 6 — Desktop Modules
├── Products Module (القالب)
├── Customers + Suppliers
├── Warehouses
├── Purchases Module
├── Sales Module ⚠️ الأهم
├── Returns Module
└── Reports

Phase 7 — Polish
├── BackupService
├── Settings Screen
└── Error Handling UI

Phase 8 — Dynamic UOM & Costing (v4.3)
├── ProductUnits: وحدات ديناميكية لكل منتج مع أسعار منفصلة
├── UnitBarcodes: باركود لكل وحدة منتج
├── UpdateProductPricingService: 3 استراتيجيات تسعير
├── ProductPriceHistory: سجل تغييرات الأسعار والتكلفة
└── Cost cascade: تحديث تكلفة جميع الوحدات × عامل التحويل

Phase 9 — Cash Boxes (v4.3)
├── CashBoxes: صناديق نقدية متعددة
├── CashTransactions: حركات نقدية غير قابلة للتعديل
├── DailyClosure: إقفال يومي
└── Transfer: تحويل بين الصناديق (حركتين)

Phase 10 — Print Engine (v4.3)
├── A4: QuestPDF مع شعار المتجر
├── Thermal: ESC/POS طباعة حرارية 80mm
├── API: Desktop يطلب الطباعة عبر PrintController
├── Preview: معاينة قبل الطباعة
└── Logo: معالجة غياب الشعار (null check)

Phase 11 — Production (v4.4)
├── Auto-Update: تحديث تلقائي مع التحقق من SHA256
├── DPAPI: تشفير connection strings
├── Backup: نسخ احتياطي تلقائي يومي
├── Windows Service: تشغيل API كخدمة ويندوز
└── Health Check: فحص قاعدة البيانات عند بدء التشغيل

Phase 12 — UI Polish (v4.5)
├── Multi-Window: فتح النوافذ بدون حظر (ScreenWindowService)
├── ToolTips: تلميحات عربية لجميع الأزرار
├── Sorting: ترتيب القوائم من الأحدث للأقدم
└── EventBus: إدارة الاشتراكات (subscribe/dispose)

Phase 13 — Validation & Cleanup (v4.6–v4.6.2)
├── ID Strategy: إزالة Code من المنتجات والعملاء والموردين والمخازن
├── ErrorTemplate: إطار أحمر + ❗ مع ToolTip
├── INotifyDataErrorInfo: تحقق فوري في حقول الإدخال
├── ValidateAllAsync: تحقق موحد قبل الحفظ
└── SetDialogService: ربط خدمة الحوار في جميع الـ ViewModels

<!-- END OF SECTION: 14.1 Baseline Project Implementation Phases (Phases 1-13) -->


<!-- START OF SECTION: 14.2 Advanced Security & Quality Hardening Phases (Phases 18-20) -->

> [!NOTE]
> Recent phases executing real-time validation templates, architecture alignment, and security rate-limiting.

## ✅ Phase 18: WPF Validation ErrorTemplate & INotifyDataErrorInfo (v4.6.2)

**Goal**: Standardize validation UI with red border + ❗ icon ErrorTemplate, replace `HasXxxError` boolean pattern with `INotifyDataErrorInfo`, and add `ValidateAllAsync()` to ViewModelBase.

### Key Changes
- **New ErrorTemplate**: Red border (#EF4444, 1.5px) + ❗ icon badge with ToolTip — applies to TextBox, PasswordBox, ComboBox when `Validation.HasError = true`
- **ViewModelBase.cs**: Added `SetDialogService(IDialogService)`, `ValidateAllAsync()`, `ValidateField()`
- **14 Editor VMs**: All call `SetDialogService()` in constructors
- **ProductEditorViewModel**: Removed 7 `HasXxxError` booleans — real-time `AddError`/`ClearErrors`
- **CustomerEditorViewModel**: Removed 3 `HasXxxError` booleans — real-time `AddError`/`ClearErrors`

### New Rules (AGENTS.md)
| Rule | Description |
|------|-------------|
| RULE-227 | `SetDialogService()` in every Editor VM constructor |
| RULE-228 | Use `INotifyDataErrorInfo` (`AddError/ClearErrors`) — no `HasXxxError` booleans |
| RULE-229 | Pre-save validation: `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()` |
| RULE-230 | ErrorTemplate: red border + ❗ icon with ToolTip bound to `[0].ErrorContent` |

### File Impact
| Layer | Files |
|-------|-------|
| UI (XAML) | `Resources/Styles.xaml` — new ErrorTemplate + HasError triggers |
| ViewModels | `ViewModelBase.cs` — SetDialogService, ValidateAllAsync, ValidateField |
| Editor VMs | 14 files — SetDialogService() in constructors |
| Refactored VMs | ProductEditor, CustomerEditor — removed HasXxxError pattern |
| Documentation | AGENTS.md, README.md, 5 subagent files, CHANGELOG.md, MASTER-PLAN.md, CONSTITUTION.md |

---

## ✅ Phase 19: Architecture Alignment & Code Quality Remediation (v4.6.3)

**Goal**: Align Costing settings with Clean Architecture boundaries (moving to `ISettingsApiService` via HTTP Client), resolve ViewModel compiler shadowing (CS0108 warnings), wrap async void operations in ViewModels with safe try-catches, and correct garbled Arabic text.

### Key Changes
- **Costing Settings Refactor**: Migrated `CostingMethodSettingsViewModel` from repository calling to HTTP setting API client calls. Registered the VM inside `App.xaml.cs`.
- **WPF VM Quality Standard**: Avoided CS0108 by calling base class property setting helpers and calling `SetDialogService()`. Safe try-catch wrappers for async void initialization workflows.
- **RTL Arabic Corrections**: Rectified Mojibake in transfers/payments ViewModels.

---

## ✅ Phase 20: Security Hardening & Code Quality (v4.6.4)

**Goal**: Harden security with rate limiting, protect user integrity, secure connection strings, enhance validation, fix build warnings.

### Key Changes
- **Rate Limiting**: Added `AddRateLimiter` with `LoginPolicy` (5 attempts per 15 min per IP) and global policy (100 req/min). Arabic 429 response with `RATE_LIMIT_EXCEEDED` code.
- **User Hard-Delete Guarded**: `UserService.PermanentDeleteAsync()` returns `Result.Failure("لا يمكن حذف المستخدمين بشكل نهائي")` — enforces RULE-038.
- **Connection String Security**: Removed plaintext connection string from `appsettings.Development.json`. Uses `SALESSYSTEM_DB_CONNECTION` env var only.
- **FluentValidator Enhancements**: Enhanced all 7 invoice/payment/transfer validators with date, enum, and max-length rules.
- **FallbackErrorDialog**: Added `FallbackErrorDialog.xaml` for thread-safe unhandled exception display.
- **Build Fixes**: Resolved 10 CS0109 warnings and 4 CS1540 errors across 8 ViewModels.
- **Test Coverage**: 5 new tests + 50 test files compiling.

### Key Rules
- RULE-240 through RULE-248 (Rate Limiting, User Hard-Delete, Connection String Security)

### Verification
- [ ] `dotnet build` — 0 errors, 0 warnings
- [ ] Login endpoint rate-limited (5/15min)
- [ ] User permanent delete returns failure
- [ ] No plaintext connection strings
- [ ] Security-Plan.md reflects implementation status

---


<!-- END OF SECTION: 14.2 Advanced Security & Quality Hardening Phases (Phases 18-20) -->


<!-- START OF SECTION: 14.3 Technical Debt & CQRS Roadmap -->

> [!NOTE]
> Partially implemented MediatR/CQRS trade-off explanations, technical debt notes, and retired WinForms components.

## ⚠️ Partially Implemented

### MediatR

- **Package:** MediatR v12.4.1 installed in `SalesSystem.Application`
- **Usage:** Only 1 file uses it (`ProductPriceQuery`)
- **No Commands/Queries directories** exist
- **No MediatR pipeline behaviors** registered
- **Status:** Installed but NOT adopted

### CQRS

- **Mentioned in AGENTS.md** RULE-043: "Strictly separate Read operations (Queries) from Write operations (Commands)"
- **NOT implemented** — the codebase uses Service Layer pattern
- **Services handle both reads and writes** in the same class
- **Status:** Documented but not built

### Why the gap?

The project started with Service Layer pattern and it proved sufficient for the use cases. MediatR was installed as an experiment but never adopted project-wide. AGENTS.md RULE-043 reflects an aspirational goal, not current reality.

---

## 📋 Future Plans (NOT Implemented)

These are documented in AGENTS.md or discussed but **have zero code in the codebase**:

| Feature | Status | Notes |
|---------|--------|-------|
| **MAUI Mobile App** | ❌ Not started | `Presentation.MAUI` directory never created |
| **SharedKernel project** | ❌ Not started | Architecture uses layered, not shared kernel |
| **DesignTokens.cs** | ❌ Not created | Styles live in `Resources/Styles.xaml` |
| **Roslyn Analyzer** | ❌ Not created | No `HardcodedColorAnalyzer` or similar |
| **ExecuteAsync() wrapper** | ❌ Not in ViewModelBase | Error handling uses `HandleException()` / `HandleFailure()` |
| **Vertical Slices** | ❌ Not adopted | Layered architecture is the standard |
| **Camera-based barcode** | ❌ Not started | Only USB/HID keyboard scanner implemented |
| **BarcodeScanViewModel** | ❌ Not created | Barcode handled via `IBarcodeInputService` event |
| **BaseViewModel in SharedKernel** | ❌ Not created | ViewModelBase lives in DesktopPWF |

---

## 🗑️ Legacy Code

### `Legacy/SalesSystem.Desktop/`

- **What it is:** Abandoned WinForms desktop application
- **Status:** NOT in solution file, NOT compiled, NOT referenced
- **Safe to delete:** Yes — all functionality has been rebuilt in `DesktopPWF` (WPF)
- **Why abandoned:** WinForms couldn't support the modern MVVM + EventBus + styled dialog architecture
- **Recommendation:** Delete when convenient — it's dead weight

---


<!-- END OF SECTION: 14.3 Technical Debt & CQRS Roadmap -->


<!-- START OF SECTION: 14.4 Architectural Design Decisions -->

> [!NOTE]
> Detailed rationale explaining core architectural choices (Service Layer, WPF, Rich Domain, Result Pattern).

## 📐 Architecture Decisions

### Why Service Layer over CQRS/MediatR?

- The application has ~20 aggregate roots, not 200+ — Service Layer is simpler and sufficient
- CQRS adds ceremony (Command/Query classes, handlers, validators) without proportional benefit at this scale
- Service Layer is easier for junior developers to understand and maintain
- Can migrate to CQRS later if complexity demands it

### Why DesktopPWF (WPF) over WinForms?

- WPF supports MVVM pattern with data binding
- XAML enables centralized styling (`Styles.xaml`)
- Better support for modern UI (animations, templates, resources)
- EventBus integration works naturally with WPF's dispatcher
- WinForms required code-behind logic — violated separation of concerns

### Why Layered over Vertical Slices?

- Small team (2-3 developers) — layered is easier to navigate
- Clear separation of concerns: Domain → Application → Infrastructure → API → Desktop
- Each layer has a single responsibility and single dependency direction
- Vertical slices work better for large teams with many independent features

### Why NOT MAUI?

- Target users are desktop-only (retail shops with POS terminals)
- Mobile would require entirely different UX (touch-optimized, offline-first)
- API already provides mobile-ready endpoints — MAUI can be added later
- Focus on perfecting desktop first

### Why Result<T> over Exceptions?

- Exceptions are for exceptional conditions — validation failures are expected
- Result<T> makes error handling explicit and type-safe
- Controllers can cleanly map Result to HTTP status codes
- Avoids try/catch boilerplate in every service method

### Why Rich Domain Model?

- Entities own their business rules — can't be bypassed from outside
- `private set` prevents accidental state corruption
- Factory methods enforce invariants at creation time
- Guard clauses catch invalid states early with clear Arabic messages

---


<!-- END OF SECTION: 14.4 Architectural Design Decisions -->


<!-- START OF SECTION: 15. System Version & Evolution History -->

> [!NOTE]
> Consolidated version history listing versions from inception to current production-hardened v4.6.4 release.

## 📝 Version History

| Version | Date | Description |
|---------|------|-------------|
| **v4.6.4** | **2026-05-23** | **Security Hardening & Code Quality** — Rate limiting, user hard-delete guard, connection string security, FluentValidator enhancements, FallbackErrorDialog, build warning fixes |

| Version | Date | Description |
|---------|------|-------------|
| v4.6.3 | 2026-05-23 | Architecture Alignment & Code Quality — Costing settings HTTP refactoring, VM DI registration, CS0108 member hiding resolutions, async void try-catch safety, RTL Arabic corrections |
| v4.6.2 | 2026-05-23 | WPF Validation ErrorTemplate — Red border + ❗ icon ErrorTemplate, INotifyDataErrorInfo standardization, ValidateAllAsync() base method, 14 Editor VMs updated |
| v4.6.1 | 2026-05-23 | UI Sorting & Dialog Safety — Newest-first sorting, DatabaseErrorDialog self-owner fix, comprehensive audit |
| v4.6 | 2026-05-22 | Audit & Polish — LogSystemError centralized, Dialog overlay, ValidationErrorsDialog, auto-focus, hard-delete safety, login/settings fixes |
| v4.5.3 | 2026-05-22 | Identifier Strategy Complete — Code removal (Product, Customer, Supplier, Warehouse) — all entities use auto-increment Id |
| v4.5.2 | 2026-05-22 | Identifier Strategy — Code removal (Product/Customer/Supplier) |
| v4.5.1 | 2026-05-22 | Error & Shutdown improvements — Error message overhaul, Arabic-friendly errors, MessageBox elimination |
| v4.5 | 2026-05-21 | Multi-Window Screen Management — Non-modal editors, ScreenWindowService |
| v4.4 | 2026-05-21 | Production release — Auto-Update, DPAPI, Backup, Windows Service, Installer |
| v4.3 | 2026-05-15 | Print engine (QuestPDF + Win32), Dynamic UOM, Costing strategy, Cash boxes, Price history |
| v4.2 | 2026-05-10 | Delete strategy, Defensive programming, WPF dialogs, Toast notifications, Real-time validation |
| v4.1 | 2026-05-05 | Wholesale/Retail pricing, Unit conversion in Domain |
| v4.0 | 2026-05-01 | Clean Architecture rewrite — 6 projects, Service Layer, Result<T> |
| v3.0 | 2026-04-15 | Initial architecture — PRD-MVP |


<!-- END OF SECTION: 15. System Version & Evolution History -->


<!-- START OF SECTION: 16. AI Agent Core Guidelines & Rules -->

> [!NOTE]
> Rules that AI agents must strictly follow when writing code or applying database changes to the codebase.

15. تعليمات للـ AI Agent
text

⚠️ تعليمات إلزامية لكل طلب برمجي:

1. DECIMAL ONLY
   - جميع الحسابات المالية: decimal وليس float/double
   - أمثلة: decimal total = items.Sum(i => i.LineTotal);

2. TRANSACTIONS ALWAYS
   - كل عملية تؤثر على أكثر من جدول: داخل BeginTransactionAsync
   - عند أي استثناء: RollbackAsync

3. RESULT PATTERN
   - كل Service Method ترجع: Result<T> أو Result
   - لا رمي Exception للـ Controllers مباشرة

4. DOMAIN VALIDATION
   - التحقق من المنطق في Domain Entities
   - التحقق من البيانات في Application Services
   - الـ Controller يُعيد HTTP Response فقط

5. STOCK INTEGRITY
   - تحقق من المخزون قبل Transaction
   - اخصم بعد حفظ الفاتورة
   - سجل دائماً في InventoryMovements

6. NO DIRECT DB
   - Desktop لا يصل لـ DB مباشرة
   - كل طلب عبر HttpClient → API

7. BALANCE DIRECTION
   - Customer.IncreaseBalance() عند البيع الآجل
   - Customer.DecreaseBalance() عند الدفع أو مرتجع البيع
   - Supplier.IncreaseBalance() عند الشراء الآجل
   - Supplier.DecreaseBalance() عند دفعنا للمورد

<!-- END OF SECTION: 16. AI Agent Core Guidelines & Rules -->
