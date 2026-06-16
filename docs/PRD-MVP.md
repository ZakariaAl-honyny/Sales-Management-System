# Product Requirements Document (PRD) & Master Implementation Reference
# Sales Management System (v4.10 — 65-Table Schema, Per-Unit Pricing, Immutable Base Currency)
# Platform: .NET 10 LTS | Clean Architecture | Year: 2026

## Schema Reference
- **Table definitions**: See `docs/database-schema.md` (65 tables, single source of truth)
- **Domain entities & patterns**: See `docs/CONSTITUTION.md` and `AGENTS.md`
- **Implementation plans**: See `specs/` directory (32 phases)

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
- User authentication with 9 DB-driven roles (Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager) â€” JWT with BCrypt
- **Dynamic Unit of Measure (v4.10)**: Units as independent table (seed + user-addable); ProductUnits junction (Factor, IsBaseUnit); ProductPrices per (unit × currency); barcode on Products table
- **Costing Strategy (v4.10)**: FIFO Only via InventoryBatches — WeightedAverage/LastPurchasePrice/SupplierPrice removed
- **Cash Box Management (v4.9)**: CashBox linked to Account (balance lives on Account, NOT CashBox); ReceiptVouchers/PaymentVouchers replace CashTransactions
- **ProductPrices (v4.10)**: Pricing per (ProductUnit × CurrencyId) with EffectiveFrom/EffectiveTo date ranges
- Multi-warehouse inventory management with stock transfer between warehouses
- Purchase invoices (Cash/Credit/Mixed) with invoice-level discount
- Sales invoices (Cash/Credit/Mixed, multi-currency) with real-time stock validation
- Sales returns and purchase returns with quantity validation against originals
- Customer and supplier balance tracking with payment management
- **Invoice Printing (v4.3)**: A4 PDF via QuestPDF + 80mm thermal via Win32 raw printing
- **Auto-Update System (v4.4)**: SHA256-verified, fire-and-forget, GitHub-based
- **Backup & Restore (v4.4)**: Raw SQL BACKUP DATABASE, scheduled daily at 2 AM
- **Security & DPAPI (v4.4)**: Encrypted connection strings, DataProtection keys
- **Windows Service (v4.4)**: API runs as `SalesSystemService` with auto-restart
- **Database Health Check (v4.5)**: Desktop verifies DB before login; API exposes `/health/database`
- **Multi-Window Screen Management (v4.5)**: Non-modal editors via ScreenWindowService
- **WPF Validation ErrorTemplate (v4.6.2)**: Red border + â‌— icon, INotifyDataErrorInfo, ValidateAllAsync()
- **LogSystemError centralized (v4.6)**: Unified system error logging in ViewModelBase
- **Identifier Strategy â€” No Code columns (v4.5.3)**: Auto-increment Id only; Code removed from Product/Customer/Supplier/Warehouse
- Delete operations with 3-tier strategy (Cancel/Deactivate/Permanent)
- Audit tracking on all entities (CreatedAt, CreatedByUserId, IsActive)
- Role-based UI visibility and API authorization
- Serilog logging for all critical operations
- **Accounting Foundation (Phase 18)**: JournalEntry, Account, FiscalYear entities with multi-currency support; Simple Mode UX; Annual Closing workflow
- **Chart of Accounts (Phase 22)**: 60-account hierarchical seed data with 5 account types (Asset/Liability/Equity/Revenue/Expense)
- **Currencies Module (Phase 20)**: Multi-currency CRUD with exchange rate history, FractionName, IsSystem delete guard
- **Users & Permissions (Phase 21)**: 9-role model (Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager), 45 permission codes, UserRole enum removed, DB-driven Role entity, MustChangePassword, lockout policy
- **Journal Entries (Phase 30)**: Auto-journal entries from all financial operations; manual entry; Annual Closing
- **Reports (Phase 31)**: 35+ report DTOs; Hierarchical Income Statement + Balance Sheet; Excel via ClosedXML
- **FIFO/FEFO Batch Tracking (Phases 25/27/28)**: InventoryBatches entity with batch-level FIFO cost allocation and FEFO expiry-based inventory deduction
- **Party entity (v4.10)**: Shared contact data for Customers and Suppliers via PartyId FK
- **Units as independent table (v4.10)**: Seed data + user-addable units, with ProductUnits junction table
- **Perpetual Inventory (v4.10)**: No Purchases account â€” all inventory costs go directly to Inventory Asset account

### Out of Scope (Future Phases)
- Multi-branch management with branch-level P&L
- Web or mobile client
- External API integrations (payment gateways, e-commerce)
- Payroll management
- Fixed assets management with depreciation

---

## 3. Functional Requirements

### 3.1 Authentication
- Login with username + password
- JWT token issued on successful login (expires: 8 hours)
- Refresh token for session continuity
- Role stored in JWT claims: role ID (byte) — roles are DB-driven via Role entity; no UserRole enum
- Failed login attempts logged
- Login rate limiting: 5 attempts per 15 minutes per IP
- Logout clears token from Desktop memory

### 3.2 Product Management (v4.10)
- Add / Edit / Deactivate products (no hard delete)
- Identifier: Auto-increment `Id` only â€” no `Code` field (v4.5.3)
- Fields: Name, Category, Description, TrackExpiry, ImagePath, ReorderLevel
- **DefaultPurchaseUnitId and DefaultSalesUnitId** on Product for faster data entry
- **Units (v4.10)**: Independent `Units` table (seed data + user-addable) with `ProductUnits` junction table:
  - One `IsBaseUnit = true` (smallest unit, Factor=1)
  - Derived units have Factor > 1 (e.g., Box=24)
- **ProductPrices (v4.10)**: Pricing per (ProductUnit أ— CurrencyId) with effective date ranges â€” NO RetailPrice/WholesalePrice on ProductUnit
- **Barcodes**: Stored on `Products.Barcode` (varchar, ASCII-only, unique filtered for active products)
- **Costing**: Cost sourced from `InventoryBatches.UnitCost` â€” NO cost field on Product or ProductUnit
- Search by: Id, Name, Barcode
- Filter by: Category, Active status, Warehouse stock
- Barcode scanner support (keyboard input simulation)

### 3.3 Warehouse Management (v4.10)
- Add / Edit / Deactivate warehouses (no Code field â€” auto-increment Id only)
- WarehouseType: Main (1), Store (2), Showroom (3)
- ManagerName, Phone, Address metadata fields
- View stock per warehouse per product
- WarehouseTransfers replace StockTransfers â€” multi-item transfers with batch tracking
- InventoryTransactions replace InventoryMovements â€” full audit trail (Purchase, Sale, Return, Transfer, Adjustment, etc.)
- `CHECK (Quantity >= 0)` constraint on WarehouseStocks at DB level

### 3.4 Purchase Invoice
- Select supplier and destination warehouse
- Add multiple products with quantity and unit cost
- Discount at invoice header level only (no line-level discount)
- Tax calculation (optional, from settings)
- Payment type: Cash / Credit / Mixed
- If Mixed: enter paid amount, system calculates due amount
- InvoiceNo = int, UNIQUE per purchase invoice, auto-generated via DocumentSequenceService.GetNextIntAsync("PurchaseInvoice"), user can override (validated for uniqueness)
- On Post: create InventoryBatch per line with BatchNo, QuantityReceived, QuantityRemaining, UnitCost
- FEFO: if product has expiry, batches tracked by expiry date
- On Post: increase stock in selected warehouse
- On Post: update supplier balance if credit/mixed
- On Cancel: reverse stock and balance

### 3.5 Sales Invoice
- Select customer (or use default Cash customer)
- Select source warehouse
- Add multiple products with quantity and unit price
- Discount at invoice header level only (no line-level discount)
- Payment type: Cash / Credit / Mixed
- CurrencyId and multi-currency support on invoice
- Validate: quantity available in selected warehouse
- InvoiceNo = int, UNIQUE per sales invoice, auto-generated via DocumentSequenceService.GetNextIntAsync("SalesInvoice"), user can override (validated for uniqueness)
- Batch allocation: FIFO (oldest batch first) or FEFO (closest expiry first) â€” consumes from InventoryBatches
- COGS computed from actual batch cost, not average
- On Post: decrease stock from selected warehouse
- On Post: update customer balance if credit/mixed
- On Cancel: reverse stock and balance
- Print invoice after saving

### 3.6 Sales Return
- Reference original invoice (optional)
- If referenced: validate return quantity â‰¤ sold quantity
  minus previously returned quantity
- On Post: increase stock in selected warehouse
- On Post: decrease customer balance by return amount

### 3.7 Purchase Return
- Reference original purchase invoice (optional)
- On Post: decrease stock from selected warehouse
- On Post: decrease supplier balance by return amount

### 3.8 Warehouse Transfer
- Select source warehouse and destination warehouse
- Source ≠ Destination (enforced at DB and application level)
- Add products with quantities (linked to specific InventoryBatches)
- Validate: sufficient stock in source warehouse
- On Post: decrease source warehouse stock
- On Post: increase destination warehouse stock
- Record both movements in InventoryTransactions

### 3.9 Payments
- Customer payment: record cash received from customer
  â†’ decreases customer balance
- Supplier payment: record cash paid to supplier
  â†’ decreases supplier balance
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

### 3.11 Settings (v4.6.7)
- 5-category system settings catalog: Company, System, Print, Tax, Security
- **System Settings**: CostingMethod (1-3), AllowNegativeStock, EnableFefo, AutoPost, ShowProfitInInvoice, EnableBarcode, BarcodeInputType, DecimalPlaces, PreventBelowRetailPrice, AllowBelowCostSale, DefaultWarehouseId, LowStockAlertThreshold, CurrencyDisplayFormat
- **Print Settings**: PaperSize, PrintCopies, ShowLogo, ShowBalanceOnPrint, PrintSignature, FooterNote
- **Tax Settings**: Full Tax entity CRUD with multiple tax rates (compound/inclusive/exclusive)
- **Store Info**: Store name, phone, address, logo (file path), signature, tax number
- **Security**: User, Role, Permission management (Admin only)
- CostingMethod exposed as RadioButton group in SettingsView (WeightedAverage/LastPurchasePrice/SupplierPrice)

### 3.11a Party Management (v4.10)
- Party entity stores shared contact data (Name, Phone, Email, Address, TaxNumber)
- Customers and Suppliers each have PartyId FK to share contact data
- AccountId mandatory on Customer and Supplier â€” auto-created under 1210 (AR) and 2100 (AP) respectively
- NO OpeningBalance or CurrentBalance on Customer/Supplier â€” balance tracked on linked GL Account
- NO CurrencyId on Customer/Supplier â€” currency is per-transaction
- NO CustomerGroup or SupplierType in V1
- CheckCreditLimit returns bool (soft warning, caller decides to block)

### 3.12 Backup
- Backup database via SQL Server BACKUP DATABASE command
- Restore database via API endpoint (requires restart)
- Only Admin can perform backup/restore
- Backup creates timestamped `.bak` file
- Restore confirmation requires re-login

### 3.13 Dynamic Unit of Measure (v4.10)

**Units (Independent Table):**
- `Units` is an independent table seeded with system units (حبة/PCS, كرتون/CTN, كيلو/KG, جرام/G, لتر/L, متر/M, بالة/BAL)
- User-addable with `IsSystem` flag protecting seed units from deletion
- Each product links to multiple units via `ProductUnits` junction table

**ProductUnits:**
- Each product has at least one base unit + one additional unit
- One unit must be marked as `IsBaseUnit = true` (the smallest/foundational unit)
- Base unit always has `Factor = 1`
- Derived units have `Factor > 1` (e.g., Box=24 means 1 Box = 24 base units)
- NO pricing on ProductUnit — prices stored in `ProductPrices` table per (ProductUnit × CurrencyId)
- **Enforced**: `ProductMustHaveAtLeastOneUnit` — throw DomainException if deleting last unit

**Barcodes:**
- Barcode stored on `Products` table as `varchar(50)` — unique filtered for active products
- ASCII-only barcodes (not nvarchar)

**SmartUnitFormatter:**
- UI-only service that selects best display unit based on quantity threshold
- Example: 48 pieces → "2 Boxes" (if Box factor = 24)

**Conversion Math (Domain-Only):**
```csharp
var baseQty = quantity * sourceUnit.Factor;
var targetQty = baseQty / targetUnit.Factor;
```

### 3.14 Costing Strategy (v4.10 â€” InventoryBatches FIFO Only)

**Architectural Decision: FIFO Only with Batch Tracking â€” InventoryBatches Exclusively**
The system's inventory valuation and cost calculation is built on **FIFO (First-In, First-Out)** via `InventoryBatches`:
- `CostingMethod` enum (WeightedAverage, LastPurchasePrice, SupplierPrice) is **removed** â€” the costing data model uses `InventoryBatches` exclusively.
- Each purchase creates an `InventoryBatch` with `BatchNo`, `QuantityReceived`, `QuantityRemaining`, and `UnitCost` per base unit.
- Sales consume from `InventoryBatches.QuantityRemaining` â€” oldest batch first (FIFO), or by expiry date if `Product.TrackExpiry = true` (FEFO).
- COGS at sale = SUM of (quantity consumed from each batch أ— `batch.UnitCost`)

**System Settings (Inventory Rules):**
1. `AllowNegativeStock` (ط§ظ„ط³ظ…ط§ط­ ط¨ط§ظ„ظ…ط®ط²ظˆظ† ط§ظ„ط³ط§ظ„ط¨)
2. `AllowSalesBelowCost` (ط§ظ„ط³ظ…ط§ط­ ط¨ط§ظ„ط¨ظٹط¹ ط¨ط£ظ‚ظ„ ظ…ظ† ط§ظ„طھظƒظ„ظپط©)
3. `AutoSelectBatchByExpiry` (ط§ط®طھظٹط§ط± ط§ظ„ط¯ظپط¹ط© طھظ„ظ‚ط§ط¦ظٹط§ظ‹ ط­ط³ط¨ FEFO)
4. `DefaultTaxId` (ط§ظ„ط¶ط±ظٹط¨ط© ط§ظ„ط§ظپطھط±ط§ط¶ظٹط©)
5. `ExpiryAlertDays` (ط¹ط¯ط¯ ط£ظٹط§ظ… ط§ظ„طھظ†ط¨ظٹظ‡ ظ‚ط¨ظ„ ط§ظ„ط§ظ†طھظ‡ط§ط،)

**Cost Computation (no Cost column on ProductUnit â€” computed on-the-fly):**
```csharp
// Average cost computed from all available batches for a product+warehouse
var avgCost = batches
    .Where(b => b.QuantityRemaining > 0)
    .Sum(b => b.QuantityRemaining * b.UnitCost)
    / batches.Sum(b => b.QuantityRemaining);

// Per-unit cost for display (non-base units):
// baseCost * unit.Factor â€” NO cost stored on ProductUnit
var displayCost = avgCost * unitFactor;
```

### 3.15 Cash Box Management (v4.9 â€” Refactored)

**CashBox Entities:**
- CashBox is a **lightweight register entity** with `AccountId` (required FK to Account)
- **NO** `OpeningBalance` or `CurrentBalance` on CashBox â€” balance lives on linked Account
- Balance tracked on the Chart of Accounts Account, NOT on CashBox
- CashBox has metadata: `CategoryId`, `PhoneNumber`, `TaxNumber`, `Address`
- One cash box flagged as `IsDefault`
- Account balance NEVER goes negative

**CashTransaction Types (v4.9+):**
Replaced by `ReceiptVouchers` (ط³ظ†ط¯ط§طھ ظ‚ط¨ط¶) and `PaymentVouchers` (ط³ظ†ط¯ط§طھ طµط±ظپ)
| Type | Entity | Direction |
|------|--------|-----------|
| Customer Receipt | ReceiptVoucher | IN (from customer) |
| Supplier Payment | PaymentVoucher | OUT (to supplier) |
| Expense | PaymentVoucher | OUT (expense) |
| Transfer | Two vouchers | Out + In |

**Key Design Changes (v4.9+):**
- NO `DailyClosure` in V1 (deferred)
- NO `Cheque` management in V1 (deferred)
- Account auto-created under "1110 â€” ط§ظ„ظ†ظ‚ط¯ظٹط©" when CashBox created without AccountId
- Opening balance = Journal Entry (Dr Cash Account / Cr OpeningBalanceEquity)
- Cash transactions are immutable â€” cancellations via offsetting entry

### 3.16 Invoice Printing (v4.3)

- Two print formats: A4 PDF (QuestPDF) + 80mm thermal receipt (ESC/POS)
- A4: RTL layout with store logo, tax breakdown, invoice details
- Thermal: 42-char monospaced columns, Windows-1256 encoding for Arabic
- Desktop calls `PrintController` API â€” never prints directly (v4.3)
- Preview A4 in `PdfPreviewWindow` before printing
- Print settings stored in `SystemSetting` table with `Category = "Print"`
- Supports: Sales, Purchase, SalesReturn, PurchaseReturn, Test page
- Logo is optional â€” missing logo handled gracefully (null check)

### 3.17 Auto-Update System (v4.4)

- Fire-and-forget on startup â€” never blocks login
- SHA256 checksum verification before launching installer
- Skipped version persisted to `%AppData%\SalesSystem\settings.json`
- Timeout = 8 seconds with silent failure
- Version comparison via `System.Version` (never string comparison)

### 3.18 Database Backup & Restore (v4.4)

- Backup uses raw SQL `BACKUP DATABASE` â€” no SMO dependency
- Restore uses `SINGLE_USER WITH ROLLBACK AFTER 30` â€” 30s grace period
- `ScheduledBackupWorker` runs daily at 2:00 AM as `BackgroundService`
- Backup retention = configurable days (default 30)
- Restore failure triggers `TrySetMultiUserAsync` recovery

### 3.19 Security & DPAPI (v4.4)

- Connection strings encrypted via DPAPI with `"DPAPI:"` prefix
- `FirstRunSetupService` encrypts on first run (idempotent)
- DataProtection keys stored in `%ProgramData%\SalesSystem\DataProtectionKeys`
- JWT secret from environment variable â€” throws in production if missing
- `appsettings.json` writes use atomic pattern: `.tmp` â†’ `File.Replace()` â†’ `.bak`

### 3.20 Windows Service (v4.4)

- API runs as `SalesSystemService` via `UseWindowsService()`
- Auto-recovery: 3 restarts on failure (1min, 5min, 15min delays)
- Serilog EventLog sink for Windows Service logging
- Database migration runs on service startup (auto-migrate)

### 3.21 Database Health Check (v4.5)

- API exposes `GET /api/v1/health` â€” returns `Database: Connected/Disconnected`
- API exposes `GET /api/v1/health/database` â€” checks `DbContext.Database.CanConnectAsync()`
- Desktop checks DB on startup before showing login via `IDatabaseHealthCheckService`
- `DatabaseErrorDialog` with Retry/Exit on connection failure
- `ExceptionMiddleware` detects DB exceptions â†’ returns `503` with `DATABASE_CONNECTION_ERROR`

### 3.22 Multi-Window Screen Management (v4.5)

- Editors open non-modally via `ScreenWindowService.OpenScreen()` â€” never `ShowDialog()`
- `ScreenWindow.xaml` hosts any View/ViewModel pair generically
- Window tracking via `WeakReference<Window>` â€” no memory leaks
- Cascade positioning: 30px offset أ— (count % 10) from MainWindow
- Auto-titles in Arabic (e.g., "ظپط§طھظˆط±ط© ط¨ظٹط¹") â€” not English type names

### 3.23 WPF Validation ErrorTemplate & INotifyDataErrorInfo (v4.6.2)

- ErrorTemplate in `Styles.xaml`: Red border (#EF4444, 1.5px) + â‌— icon badge
- ToolTip on error icon bound to `[0].ErrorContent`
- Applies to TextBox, PasswordBox, ComboBox
- `ViewModelBase.cs`: `SetDialogService()`, `ValidateAllAsync()`, `ValidateField()`
- All Editor VMs call `SetDialogService()` in constructors
- Pre-save validation: `ClearAllErrors()` â†’ `AddError()` â†’ `await ValidateAllAsync()`
- No `HasXxxError` booleans â€” use `INotifyDataErrorInfo` directly
- Save buttons always enabled â€” validate on click with warning dialog

### 3.24 Identifier Strategy â€” No Code Columns (v4.5.3)

- Product, Customer, Supplier, Warehouse: auto-increment `Id` only
- No `Code` property on entities, DTOs, or editor ViewModels
- Search/filter by `Id` (int) or `Name` (string) â€” never by Code
- `DuplicateCode` error constant removed from ErrorCodes
- Code auto-generation services (`DocumentSequenceService` for PRD/CUST/SUP/WH) removed

### 3.25 Accounting Foundation (Phase 18)
- JournalEntry entity with multi-currency support (SourceType, SourceId, Status, Description)
- JournalEntryLine: AccountId, Debit/Credit amounts, CurrencyId, ExchangeRate; CHECK (Debit > 0 OR Credit > 0)
- Account entity: hierarchical (ParentAccountId), 5 types (Asset/Liability/Equity/Revenue/Expense), â“ک per account
- FiscalYear: StartDate, EndDate, IsClosed, close(userId) â€” prevents double close
- 60 accounts seeded as default chart of accounts
- 7 auto-journal entry providers (Sales, Purchases, Returns, Payments, Receipts, Transfers, Annual Closing)
- Annual Closing: close revenue/expense accounts â†’ transfer P&L to Retained Earnings â†’ open new FiscalYear
- Simple Mode UX: toggle hides/shows Debit/Credit columns for non-accountants

### 3.26 FIFO/FEFO Batch Tracking (v4.10 â€” InventoryBatches)
- `InventoryBatches` entity replaces old `PurchaseLot` concept
- Batch properties: BatchNo (int), ProductId FK, WarehouseId FK, PurchaseInvoiceId FK (nullable), SupplierBatchNo, ExpiryDate (nullable for non-expiry products)
- `QuantityReceived` (total ever received) and `QuantityRemaining` (currently available) â€” both decimal(18,3) with CHECK >= 0
- `UnitCost` decimal(18,2) â€” the actual cost per base unit for this batch
- Each purchase creates InventoryBatch per line with unit cost allocation
- On sale: FIFO deducts from oldest batch first (`OrderBy(b => b.Id)`); if `TrackExpiry=true`, FEFO deducts from closest-expiry first (`OrderBy(b => b.ExpiryDate)`)
- Sales returns restore QuantityRemaining on the original batch (if referenced)
- Opening stock creates an opening batch (BatchNo = 1)
- COGS = SUM of (QuantityConsumed أ— Batch.UnitCost) per batch consumed

**Batch Indexes:**
- `(ProductId, WarehouseId)` â€” for stock location queries
- `(ExpiryDate)` filtered â€” for FEFO allocation
- `(PurchaseInvoiceId)` â€” for purchase return lookups
- Products with TrackExpiry=true â†’ FEFO applied during batch selection

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

See `docs/database-schema.md` for the complete 65-table schema across 8 modules (Core/Parties/Security, Organization/Currencies/Settings, Products, Accounting, Inventory, Sales, Purchases, Infrastructure/Support).


---

## 6. Solution Architecture

> âڑ ï¸ڈ The Desktop project is **WPF (Windows Presentation Foundation)** with MVVM pattern.
> Project name: `SalesSystem.DesktopPWF` â€” NOT WinForms. All UI files are `.xaml`.
> Data Flow: `Desktop â†’ (HttpClient) â†’ Api â†’ Application â†’ Infrastructure â†’ SQL Server`

```text
SalesSystem/
â”œâ”€â”€ SalesSystem.Contracts/       â†گ DTOs + Requests + Responses + Result<T>
â”œâ”€â”€ SalesSystem.Domain/          â†گ Entities + Business Rules + Exceptions
â”œâ”€â”€ SalesSystem.Application/     â†گ Services + Interfaces + Use Cases
â”œâ”€â”€ SalesSystem.Infrastructure/  â†گ EF Core + DbContext + Repositories + UoW
â”œâ”€â”€ SalesSystem.Api/             â†گ Controllers + FluentValidation + Middleware + JWT
â””â”€â”€ SalesSystem.DesktopPWF/     â†گ WPF UI + MVVM + EventBus + Printing
â”‚       â”œâ”€â”€ Result.cs
â”‚       â”œâ”€â”€ PagedResult.cs
â”‚       â””â”€â”€ ErrorCodes.cs
â”‚
â”œâ”€â”€ SalesSystem.Domain/
â”‚   â”œâ”€â”€ Entities/
â”‚   â”œâ”€â”€ Enums/
â”‚   â”œâ”€â”€ Exceptions/
â”‚   â””â”€â”€ Common/
â”‚       â””â”€â”€ BaseEntity.cs
â”‚
â”œâ”€â”€ SalesSystem.Application/
â”‚   â”œâ”€â”€ Interfaces/
â”‚   â”‚   â”œâ”€â”€ Repositories/
â”‚   â”‚   â”œâ”€â”€ Services/
â”‚   â”‚   â””â”€â”€ IUnitOfWork.cs
â”‚   â”œâ”€â”€ Services/
â”‚   â”‚   â”œâ”€â”€ ProductService.cs
â”‚   â”‚   â”œâ”€â”€ SalesService.cs âڑ ï¸ڈ Critical
â”‚   â”‚   â”œâ”€â”€ PurchaseService.cs âڑ ï¸ڈ Critical
â”‚   â”‚   â”œâ”€â”€ SalesReturnService.cs âڑ ï¸ڈ Critical
â”‚   â”‚   â”œâ”€â”€ PurchaseReturnService.cs âڑ ï¸ڈ Critical
â”‚   â”‚   â”œâ”€â”€ InventoryBatchService.cs âڑ ï¸ڈ Critical
â”‚   â”‚   â”œâ”€â”€ InventoryTransactionService.cs âڑ ï¸ڈ Critical
â”‚   â”‚   â”œâ”€â”€ WarehouseTransferService.cs âڑ ï¸ڈ Critical
â”‚   â”‚   â”œâ”€â”€ PartyService.cs
â”‚   â”‚   â”œâ”€â”€ PaymentService.cs
â”‚   â”‚   â”œâ”€â”€ ReportService.cs
â”‚   â”‚   â”œâ”€â”€ AuthService.cs
â”‚   â”‚   â”œâ”€â”€ BackupService.cs
â”‚   â”‚   â””â”€â”€ DocumentSequenceService.cs âڑ ï¸ڈ Thread-safe
â”‚
â”œâ”€â”€ SalesSystem.Infrastructure/
â”‚   â”œâ”€â”€ Data/
â”‚   â”‚   â”œâ”€â”€ SalesDbContext.cs
â”‚   â”‚   â””â”€â”€ Configurations/
â”‚   â”œâ”€â”€ Repositories/
â”‚   â”œâ”€â”€ Migrations/
â”‚   â””â”€â”€ Services/
â”‚       â””â”€â”€ BackupService.cs
â”‚
â”œâ”€â”€ SalesSystem.Api/
â”‚   â”œâ”€â”€ Controllers/
â”‚   â”œâ”€â”€ Validators/               â†گ FluentValidation
â”‚   â”œâ”€â”€ Middleware/
â”‚   â”‚   â”œâ”€â”€ ExceptionMiddleware.cs
â”‚   â”‚   â””â”€â”€ RequestLoggingMiddleware.cs
â”‚   â””â”€â”€ Extensions/
â”‚       â”œâ”€â”€ AuthExtensions.cs
â”‚       â””â”€â”€ ValidationExtensions.cs
â”‚
â””â”€â”€ SalesSystem.DesktopPWF/       â†گ WPF + MVVM (NOT WinForms)
    â”‚
    â”œâ”€â”€ MainWindow.xaml            â†گ Shell / main layout
    â”œâ”€â”€ LoginWindow.xaml           â†گ Login screen
    â”œâ”€â”€ App.xaml                   â†گ Application entry point
    â”œâ”€â”€ App.xaml.cs                â†گ DI registration (App.GetService<T>())
    â”œâ”€â”€ appsettings.json
    â”‚
    â”œâ”€â”€ Views/                     â†گ .xaml UI files (no logic)
    â”‚   â”œâ”€â”€ Categories/
    â”‚   â”œâ”€â”€ Common/                â†گ Shared controls / dialogs
    â”‚   â”œâ”€â”€ Customers/
    â”‚   â”œâ”€â”€ Dashboard/
    â”‚   â”œâ”€â”€ Inventory/
    â”‚   â”œâ”€â”€ Invoices/              â†گ Selection dialogs for invoices
    â”‚   â”œâ”€â”€ Login/
    â”‚   â”œâ”€â”€ Payments/
    â”‚   â”œâ”€â”€ Products/
    â”‚   â”œâ”€â”€ Purchases/
    â”‚   â”œâ”€â”€ Reports/
    â”‚   â”‚   â””â”€â”€ LowStockView.xaml  â†گ [NEW] SPEC-009
    â”‚   â”œâ”€â”€ Returns/
    â”‚   â”œâ”€â”€ Sales/
    â”‚   â”œâ”€â”€ Settings/
    â”‚   â”œâ”€â”€ Suppliers/
    â”‚   â”œâ”€â”€ Transfers/
    â”‚   â”œâ”€â”€ Units/
    â”‚   â”œâ”€â”€ Users/
    â”‚   â””â”€â”€ Warehouses/
    â”‚
    â”œâ”€â”€ ViewModels/                â†گ MVVM binding logic
    â”‚   â”œâ”€â”€ ViewModelBase.cs
    â”‚   â”œâ”€â”€ DashboardViewModel.cs
    â”‚   â”œâ”€â”€ LoginWindowViewModel.cs
    â”‚   â”œâ”€â”€ ReportsViewModel.cs
    â”‚   â”œâ”€â”€ SettingsViewModel.cs
    â”‚   â”œâ”€â”€ WarehouseListViewModel.cs
    â”‚   â”œâ”€â”€ WarehouseEditorViewModel.cs
    â”‚   â”œâ”€â”€ Categories/
    â”‚   â”œâ”€â”€ Customers/
    â”‚   â”œâ”€â”€ Inventory/
    â”‚   â”œâ”€â”€ Invoices/
    â”‚   â”œâ”€â”€ Payments/
    â”‚   â”œâ”€â”€ Products/
    â”‚   â”œâ”€â”€ Purchases/
    â”‚   â”œâ”€â”€ Returns/
    â”‚   â”œâ”€â”€ Sales/
    â”‚   â”œâ”€â”€ Suppliers/
    â”‚   â”œâ”€â”€ Transfers/
    â”‚   â”œâ”€â”€ Units/
    â”‚   â””â”€â”€ Users/
    â”‚
    â”œâ”€â”€ Services/
    â”‚   â”œâ”€â”€ Api/                   â†گ HttpClient wrappers â†’ API calls
    â”‚   â”‚   â”œâ”€â”€ IApiService.cs     â†گ All API interfaces in one file
    â”‚   â”‚   â”œâ”€â”€ AuthApiService.cs
    â”‚   â”‚   â”œâ”€â”€ ProductApiService.cs
    â”‚   â”‚   â”œâ”€â”€ SalesInvoiceApiService.cs
    â”‚   â”‚   â”œâ”€â”€ PurchaseInvoiceApiService.cs
    â”‚   â”‚   â”œâ”€â”€ SalesReturnApiService.cs
    â”‚   â”‚   â”œâ”€â”€ PurchaseReturnApiService.cs
    â”‚   â”‚   â”œâ”€â”€ InventoryApiService.cs
    â”‚   â”‚   â”œâ”€â”€ WarehouseTransferApiService.cs
    â”‚   â”‚   â”œâ”€â”€ PartyApiService.cs
    â”‚   â”‚   â”œâ”€â”€ CustomerApiService.cs
    â”‚   â”‚   â”œâ”€â”€ SupplierWarehouseApiService.cs
    â”‚   â”‚   â”œâ”€â”€ CustomerPaymentApiService.cs
    â”‚   â”‚   â”œâ”€â”€ SupplierPaymentApiService.cs
    â”‚   â”‚   â”œâ”€â”€ ReportApiService.cs
    â”‚   â”‚   â”œâ”€â”€ DashboardApiService.cs
    â”‚   â”‚   â”œâ”€â”€ SettingsApiService.cs
    â”‚   â”‚   â”œâ”€â”€ CategoryApiService.cs
    â”‚   â”‚   â”œâ”€â”€ UnitApiService.cs
    â”‚   â”‚   â”œâ”€â”€ UserApiService.cs
    â”‚   â”‚   â””â”€â”€ LogsApiService.cs
    â”‚   â””â”€â”€ App/                   â†گ App-level services (DI singletons)
    â”‚       â”œâ”€â”€ EventBus.cs        â†گ Pub/Sub event bus
    â”‚       â”œâ”€â”€ NavigationService.cs
    â”‚       â”œâ”€â”€ SessionService.cs  â†گ JWT token storage (in-memory only)
    â”‚       â”œâ”€â”€ DialogService.cs
    â”‚       â”œâ”€â”€ ISoundService.cs
    â”‚       â”œâ”€â”€ SoundService.cs
    â”‚       â””â”€â”€ IPrinterService.cs â†گ [NEW] SPEC-006 print contract
    â”‚
    â”œâ”€â”€ Messaging/
    â”‚   â””â”€â”€ Messages/              â†گ EventBus message types (ID only, no payload)
    â”‚
    â”œâ”€â”€ Converters/                â†گ WPF IValueConverter implementations
    â”œâ”€â”€ Helpers/                   â†گ UI helpers / ThemeHelper etc.
    â”œâ”€â”€ Models/                    â†گ Local view models / display models
    â””â”€â”€ Resources/                 â†گ Styles, brushes, icons, themes
```


---

## 7. Implementation Phases (Current - 14 Phases, Re-ordered as 18-31)

The current implementation follows Phases 18-31, corresponding to the post-MVP feature modules. Earlier phases (1-17) were completed in prior iterations.

| Phase | Module | Key Deliverables |
|-------|--------|-----------------|
| **18** | **Accounting Foundation** | JournalEntry, Account, FiscalYear entities; Simple Mode UX toggle; 19 tooltips; 7 auto-journal entry providers; Annual Closing workflow |
| **19** | **Settings Module** | 13 system settings per analysis; CostingMethod RadioButton; Print/Notification/Tax/Security settings; 8 seed entities |
| **20** | **Currencies Module** | Multi-currency CRUD with exchange rate history; FractionName field; IsSystem delete guard; YER/USD/SAR seed |
| **21** | **Users & Permissions** | 9 roles (Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager); 45 permission codes; UserRole enum removed; DB-driven roles; default admin with MustChangePassword; lockout policy |
| **22** | **Chart of Accounts** | 60-account hierarchical seed; 5 account types; Level validation (max 10); tooltip per account |
| **23** | **Customers Module** | Party entity shared with Supplier; AccountId mandatory (auto-created under 1210); NO CustomerType; NO OpeningBalance on entity — via Journal Entry |
| **24** | **Suppliers Module** | Party entity shared with Customer; AccountId mandatory (auto-created under 2100); NO OpeningBalance on entity — via Journal Entry |
| **25** | **Products Module** | Multi-unit via ProductUnit; ProductPrices per (ProductUnit x Currency); Opening Stock via InventoryBatch; TrackExpiry; Barcode via UnitBarcode |
| **26** | **Warehouses Module** | Warehouse CRUD with Type (Main/Store/Showroom); WarehouseTransfers replace StockTransfers; InventoryTransaction replace InventoryMovement; NO Physical Count in V1 |
| **27** | **Purchases Module** | FIFO batch costing per line; InventoryBatch creation on Post; AdditionalCharge via OtherCharges (header level); standalone returns |
| **28** | **Sales Module** | FIFO/FEFO batch allocation from InventoryBatches; Barcode auto-add with ISoundService; RefundOut for returns; credit limit check |
| **29** | **Receipts & Payments** | CashBox.AccountId FK; ReceiptVouchers/PaymentVouchers replace CashTransactions; CustomerReceipts/SupplierPayments with multi-invoice allocation; NO Cheque, NO DailyClosure in V1 |
| **30** | **Journal Entries** | SystemAccountMappings in 7 auto-entry providers; FiscalYear integration; Annual Closing; Simple Mode UX |
| **31** | **Reports Module** | 35+ report DTOs; Hierarchical Income Statement + Balance Sheet; BuildAccountTree; General Ledger; Excel via ClosedXML |

---

### Phase 18: Accounting Foundation
- JournalEntry entity: EntryDate, EntryNumber, Description, SourceType, SourceId, Status, FiscalYearId, CurrencyId, ExchangeRate
- JournalEntryLine: AccountId FK, DebitAmount(18,2), CreditAmount(18,2); CHECK (Debit > 0 OR Credit > 0)
- Account entity: hierarchical (ParentAccountId self-FK), AccountType(1-5), AccountCode, IsSystemAccount
- FiscalYear: StartDate, EndDate, IsClosed, ClosedByUserId; prevents double close
- 60 accounts seeded as default chart of accounts with tooltip descriptions
- 7 auto-journal entry providers (Sales, Purchases, Returns, Payments, Receipts, Transfers, Annual Closing)
- Simple Mode UX: toggle hides/shows Debit/Credit columns
- Annual Closing: close Revenue/Expense -> P&L to Retained Earnings -> open new FiscalYear
- All DomainException messages in Arabic; 19 tooltips across all dialogs

### Phase 19: Settings Module
- 8 seed entities: StoreSettings, SystemSetting, PrintSetting, NotificationSetting, TaxSetting, SecuritySetting, IntegrationSetting, BackupSetting
- 13 system settings: CostingMethod(1-3), AllowNegativeStock, EnableFefo, AutoPost, ShowProfitInInvoice, EnableBarcode, BarcodeInputType, DecimalPlaces, PreventBelowRetailPrice, AllowBelowCostSale, DefaultWarehouseId, LowStockAlertThreshold, CurrencyDisplayFormat
- CostingMethod RadioButton group (WeightedAverage/LastPurchasePrice/SupplierPrice)
- Print Settings: PaperSize, PrintCopies, ShowLogo, ShowBalanceOnPrint, PrintSignature, FooterNote
- Tax entity CRUD with multiple tax rates
- Notification Settings: LowStockAlerts, ExpiryAlerts, DailyReport, EmailNotifications
- FluentValidation for each settings category

### Phase 20: Currencies Module
- Currency entity: Name, Code, Symbol, FractionName, ExchangeRateToBase, IsBaseCurrency, IsSystem
- CRUD with IsSystem delete guard (prevents deletion of YER/USD/SAR)
- ExchangeRateHistory entity with date tracking
- Seed: YER (base, rate=1), USD (rate~250), SAR (rate~66) - IsSystem=true
- Validation: base currency exchange rate must be 1

### Phase 21: Users & Permissions
- User entity: IsActive (soft delete), IsLocked, LoginAttempts, EmployeeId (optional), MustChangePassword
- 9 DB-driven roles: Admin, Manager, Accountant, Treasurer, Cashier, Warehouse Supervisor, Sales Employee, Observer, Branch Manager — stored in Role entity (not UserRole enum)
- 45 permission codes across 12 categories with dot-notation format (Sales.View, Purchase.Create, etc.)
- Role-permission mappings seeded via RolePermission join table; Admin-only Permission Management screen
- UserService.CreateAsync() hashes default password "12345678" (BCrypt, work factor 12), sets MustChangePassword=true
- Password policy: BCrypt work factor 12, min 8 chars, complexity
- Lockout after 5 failed login attempts (IsLocked=true)
- AuditLog entity (long PK = bigint) with indexes on (UserId, CreatedAt DESC), (EntityType, EntityId), (CreatedAt DESC)
- Default admin seed: username="admin", name="ظ…ط¯ظٹط± ط§ظ„ظ†ط¸ط§ظ…", MustChangePassword=true
- Desktop screens: PasswordChangeView (mandatory first-login change), AuditLogListView, PermissionManagementView

### Phase 22: Chart of Accounts
- 60 accounts seeded in hierarchical tree (max depth 10)
- 5 account types: Asset(1), Liability(2), Equity(3), Revenue(4), Expense(5)
- Level validation: child.Level > parent.Level (max 10, not strict +1)
- IsSystemAccount on L1-L2 (prevents deletion)
- tooltip per account: type, code, balance direction, level
- SystemAccountMappings maps system operations to AccountId

### Phase 23: Customers Module
- Party entity stores shared contact data (Name, Phone, Email, Address, TaxNumber) — shared with Suppliers
- Customer has PartyId FK (mandatory) and AccountId FK (mandatory, auto-created under 1210 — Accounts Receivable)
- NO CustomerType (Cash/Credit is per-invoice via PaymentType)
- NO CurrencyId on Customer (currency per-transaction)
- NO OpeningBalance on Customer entity — opening balance via Journal Entry (Dr AR / Cr OpeningBalanceEquity)
- CreditLimit with CheckCreditLimit() returning bool (soft warning, caller decides)
- tooltips: Account impact, credit limit flows, opening balance entries

### Phase 24: Suppliers Module
- Party entity stores shared contact data (Name, Phone, Email, Address, TaxNumber) — shared with Customers
- Supplier has PartyId FK (mandatory) and AccountId FK (mandatory, auto-created under 2100 — Accounts Payable)
- NO CurrencyId on Supplier (currency per-transaction)
- NO OpeningBalance on Supplier entity — opening balance via Journal Entry (Dr OpeningBalanceEquity / Cr AP)
- CreditLimit with CheckCreditLimit() returning bool (soft warning, caller decides)
- tooltips: Account impact, opening balance entries

### Phase 25: Products Module
- Multi-unit via ProductUnits junction table (Units independent table + Factor)
- ProductPrices table: pricing per (ProductUnit x CurrencyId) with EffectiveFrom/EffectiveTo date ranges
- NO RetailPrice/WholesalePrice on ProductUnit
- TrackExpiry flag for FEFO support
- Opening Stock via InventoryBatch with BatchNo=1 (opening batch)
- Cost sourced from InventoryBatches.UnitCost — no cost field on Product or ProductUnit
- Barcode stored on Products table (varchar, ASCII-only, unique filtered)
- DefaultPurchaseUnitId and DefaultSalesUnitId on Product
- RULE-191: No Code column - auto-increment Id as sole identifier

### Phase 26: Warehouses Module
- Warehouse CRUD with Type (Main=1, Store=2, Showroom=3), ManagerName, Phone, Address
- WarehouseTransfers replace StockTransfers — multi-item transfers with batch tracking
- InventoryTransactions replace InventoryMovements — 12 transaction types (Purchase, Sale, Return, Transfer, Adjustment, etc.)
- InventoryBatches with FIFO cost allocation — BatchNo, QuantityReceived, QuantityRemaining, UnitCost
- InventoryCounts/InventoryCountLines for physical count (count + adjust workflow)
- InventoryAdjustments/InventoryAdjustmentLines for stock corrections
- CHECK (Quantity >= 0) on WarehouseStocks

### Phase 27: Purchases Module
- Purchase Invoice with FIFO batch costing per line
- InventoryBatch created on Post (BatchNo, QuantityReceived, QuantityRemaining, UnitCost)
- Discount at invoice header level only (no line-level discount)
- OtherCharges for additional costs (transport, customs, etc.)
- Status: Draft(1) -> Posted(2) -> Cancelled(3)
- Standalone purchase return
- SupplierInvoiceNo as external reference
- InvoiceNo: int, UNIQUE, auto-generated via DocumentSequenceService.GetNextIntAsync("PurchaseInvoice"), user-editable (uniqueness validated on save)

### Phase 28: Sales Module
- FIFO/FEFO batch allocation from InventoryBatches (FIFO = oldest batch, FEFO = closest expiry)
- Barcode auto-add event-driven with ISoundService.PlaySuccess()
- Discount at invoice header level only (no line-level discount)
- CurrencyId and multi-currency support on invoice
- Credit limit check on save (per-invoice PaymentType=Credit)
- Customer account auto-credit on Post
- 10 tooltips across all dialogs

### Phase 29: Receipts & Payments
- CashBox with AccountId FK (balance on linked Account, NOT CashBox)
- NO OpeningBalance, NO CurrentBalance on CashBox entity
- ReceiptVouchers (سندات قبض) and PaymentVouchers (سندات صرف) replace CashTransactions
- CustomerReceipts with auto-accounting (debit Cash, credit Customer account)
- SupplierPayments with auto-accounting (debit Supplier account, credit Cash)
- CustomerReceiptApplications/SupplierPaymentApplications for multi-invoice payment distribution
- NO Cheque management in V1 (deferred)
- NO DailyClosure in V1 (deferred)
- Immutability: offsetting entry for reversals
- 10 tooltips for journal impact, immutability rules

### Phase 30: Journal Entries
- 7 auto-entry providers: SalesInvoice, PurchaseInvoice, CustomerPayment, SupplierPayment, Receipt, Return, AnnualClosing
- Manual journal entry with Debit/Credit CHECK balance constraint
- FiscalYear: cannot post to closed year
- Annual Closing: close Revenue/Expense -> P&L to Retained Earnings
- Cascade->Restrict on all FKs
- Simple Mode UX toggle

### Phase 31: Reports Module
- 35+ report DTOs by module
- Hierarchical Income Statement: Revenue -> COGS -> Gross Profit -> Expenses -> Net Income
- Hierarchical Balance Sheet: Assets(Current/Non-Current), Liabilities(Current/Non-Current), Equity
- BuildAccountTree recursive construction
- General Ledger, Trial Balance, Account Statement
- Product/Customer/Supplier reports
- Excel via ClosedXML; PDF via QuestPDF
- 5-step data flow: Desktop -> HttpClient -> API -> Service -> SQL
<!-- END OF SECTION: 1. Functional & Non-Functional Requirements (English) -->

<!-- database-schema.md is the single source of truth for all table definitions. See `docs/database-schema.md` for the complete 65-table schema with exact column types, constraints, and indexes. -->

> See `docs/database-schema.md` for the canonical database schema (65 tables across 8 modules).

> See `AGENTS.md` for domain entity patterns and `docs/database-schema.md` for table-to-entity mappings.

# 5. Contracts, Common DTOs & Domain Exceptions

DTO representations, service request/response models, common result patterns, and domain exception handlers.

<!-- START OF SECTION: 5.1 Common Contracts & Request DTOs -->

> [!NOTE]
> Core data contract classes, generic success/failure Result wrappers, and request DTOs.

// SalesSystem.Contracts/Common/Result.cs
namespace SalesSystem.Contracts.Common;

/// <summary>
/// âڑ ï¸ڈ ظƒظ„ ط§ظ„ط¹ظ…ظ„ظٹط§طھ طھط±ط¬ط¹ Result<T> â€” ظ„ط§ exceptions ظ…ظƒط´ظˆظپط© ظ„ظ„ظ€ UI
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

// ط£ظƒظˆط§ط¯ ط§ظ„ط£ط®ط·ط§ط، ط§ظ„ظ‚ظٹط§ط³ظٹط©
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
7.2 DTOs ط§ظ„ط£ط³ط§ط³ظٹط©
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
    List<SalesInvoiceLineDto> Items
);

public record SalesInvoiceLineDto(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal LineTotal
);
7.3 Requests ظ…ط¹ Validation
csharp

// SalesSystem.Contracts/Requests/Sales/CreateSalesInvoiceRequest.cs
public record CreateSalesInvoiceRequest(
    int CustomerId,
    int WarehouseId,
    int PaymentType,           // 1=Cash, 2=Credit, 3=Mixed
    decimal PaidAmount,
    string? Notes,
    List<SalesInvoiceLineRequest> Items
);

public record SalesInvoiceLineRequest(
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
8. Application Services â€” ظ…ظ†ط·ظ‚ ط§ظ„ط¹ظ…ظ„
8.1 ISalesService Interface
csharp


<!-- END OF SECTION: 5.1 Common Contracts & Request DTOs -->


<!-- START OF SECTION: 5.2 Domain Custom Exceptions -->

> [!NOTE]
> Custom business exception definitions used to enforce domain guard clauses (e.g., InsufficientStockException).

// SalesSystem.Domain/Exceptions/

// ط§ظ„ط§ط³طھط«ظ†ط§ط، ط§ظ„ط£ط³ط§ط³ظٹ ظ„ظ‚ظˆط§ط¹ط¯ ط§ظ„ط¹ظ…ظ„
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

// ط§ط³طھط«ظ†ط§ط، ط§ظ„ظ…ط®ط²ظˆظ† ط؛ظٹط± ط§ظ„ظƒط§ظپظٹ â€” ط§ظ„ط£ظƒط«ط± ط£ظ‡ظ…ظٹط©
public class InsufficientStockException : DomainException
{
    public int ProductId { get; }
    public int WarehouseId { get; }
    public decimal RequestedQuantity { get; }
    public decimal AvailableQuantity { get; }

    public InsufficientStockException(
        int productId, int warehouseId,
        decimal requested, decimal available)
        : base($"ط§ظ„ظ…ط®ط²ظˆظ† ط؛ظٹط± ظƒط§ظپظچ. ط§ظ„ظ…ظ†طھط¬: {productId}, " +
               $"ط§ظ„ظ…ط®ط²ظ†: {warehouseId}, " +
               $"ط§ظ„ظ…ط·ظ„ظˆط¨: {requested}, ط§ظ„ظ…طھظˆظپط±: {available}")
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}

10. Infrastructure â€” EF Core
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
8.2 SalesService Implementation âڑ ï¸ڈ ط§ظ„ط£ظ‡ظ… ظˆط§ظ„ط£ط­ط±ط¬
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

        // === Step 2: ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ظˆط¬ظˆط¯ ط§ظ„ط¹ظ…ظٹظ„ ظˆط§ظ„ظ…ط®ط²ظ† ===
        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct);
        if (customer == null)
            return Result<SalesInvoiceDto>.Failure(
                "ط§ظ„ط¹ظ…ظٹظ„ ط؛ظٹط± ظ…ظˆط¬ظˆط¯", ErrorCodes.NotFound);

        var warehouse = await _uow.Warehouses.GetByIdAsync(request.WarehouseId, ct);
        if (warehouse == null || !warehouse.IsActive)
            return Result<SalesInvoiceDto>.Failure(
                "ط§ظ„ظ…ط®ط²ظ† ط؛ظٹط± ظ…ظˆط¬ظˆط¯ ط£ظˆ ط؛ظٹط± ظ†ط´ط·", ErrorCodes.NotFound);

        // === Step 3: ط§ظ„طھط­ظ‚ظ‚ ط§ظ„ظ…ط³ط¨ظ‚ ظ…ظ† ط§ظ„ظ…ط®ط²ظˆظ† ظ„ظƒظ„ ط¹ظ†طµط± ===
        // âڑ ï¸ڈ ظ…ظ‡ظ…: ظ†طھط­ظ‚ظ‚ ط£ظˆظ„ط§ظ‹ ظ‚ط¨ظ„ ظپطھط­ Transaction
        foreach (var item in request.Items)
        {
            var stock = await _uow.WarehouseStocks
                .GetByWarehouseAndProductAsync(request.WarehouseId, item.ProductId, ct);

            var available = stock?.Quantity ?? 0;
            if (available < item.Quantity)
            {
                var product = await _uow.Products.GetByIdAsync(item.ProductId, ct);
                return Result<SalesInvoiceDto>.Failure(
                    $"ط§ظ„ظ…ط®ط²ظˆظ† ط؛ظٹط± ظƒط§ظپظچ ظ„ظ„ظ…ظ†طھط¬ '{product?.Name}'. " +
                    $"ط§ظ„ظ…طھظˆظپط±: {available}, ط§ظ„ظ…ط·ظ„ظˆط¨: {item.Quantity}",
                    ErrorCodes.InsufficientStock);
            }
        }

        // === Step 5: Database Transaction ===
        // âڑ ï¸ڈ ظƒظ„ ط§ظ„ط¹ظ…ظ„ظٹط§طھ ط§ظ„طھط§ظ„ظٹط© ط¯ط§ط®ظ„ Transaction ظˆط§ط­ط¯ط©
        await using var transaction = await _uow.BeginTransactionAsync(ct);
        try
        {
            // 5a. ط¥ظ†ط´ط§ط، ط§ظ„ظپط§طھظˆط±ط©
            var invoice = SalesInvoice.Create(
                request.CustomerId,
                request.WarehouseId,
                (PaymentType)request.PaymentType,
                request.PaidAmount,
                request.Notes,
                userId);

            // 5b. ط¥ط¶ط§ظپط© ط§ظ„ط¹ظ†ط§طµط± (ط§ظ„ط­ط³ط§ط¨ ظٹطھظ… ظپظٹ Domain)
            foreach (var itemRequest in request.Items)
            {
                invoice.AddItem(
                    itemRequest.ProductId,
                    itemRequest.Quantity,
                    itemRequest.UnitPrice,
                    itemRequest.DiscountAmount);
            }

            // 5c. ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ط§ظ„ظ…ط¨ظ„ط؛ ط§ظ„ظ…ط¯ظپظˆط¹
            if (request.PaidAmount > invoice.TotalAmount)
                return Result<SalesInvoiceDto>.Failure(
                    $"ط§ظ„ظ…ط¨ظ„ط؛ ط§ظ„ظ…ط¯ظپظˆط¹ ({request.PaidAmount:N2}) " +
                    $"ط£ظƒط¨ط± ظ…ظ† ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ظپط§طھظˆط±ط© ({invoice.TotalAmount:N2})",
                    ErrorCodes.InvalidAmount);

            // 5d. طھط±ط­ظٹظ„ ط§ظ„ظپط§طھظˆط±ط©
            invoice.Post();

            // 5e. ط­ظپط¸ ط§ظ„ظپط§طھظˆط±ط© ظپظٹ DB
            await _uow.SalesInvoices.AddAsync(invoice, ct);
            await _uow.SaveChangesAsync(ct);

            // 5f. ط®طµظ… ط§ظ„ظ…ط®ط²ظˆظ† ظ„ظƒظ„ ط¹ظ†طµط±
            // âڑ ï¸ڈ ظٹطھظ… ط¨ط¹ط¯ ط­ظپط¸ ط§ظ„ظپط§طھظˆط±ط© ظˆطھظˆظ„ظٹط¯ ID
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

            // 5g. طھط­ط¯ظٹط« ط±طµظٹط¯ ط§ظ„ط¹ظ…ظٹظ„ (ط¥ط°ط§ ظƒط§ظ† ط¯ظٹظ†)
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
            _logger.LogWarning(ex, "ظ…ط®ط²ظˆظ† ط؛ظٹط± ظƒط§ظپظچ ط£ط«ظ†ط§ط، ط§ظ„ط¨ظٹط¹");
            return Result<SalesInvoiceDto>.Failure(
                ex.Message, ErrorCodes.InsufficientStock);
        }
        catch (DomainException ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogWarning(ex, "ط®ط·ط£ ظپظٹ ظ‚ظˆط§ط¹ط¯ ط§ظ„ط¹ظ…ظ„ ط£ط«ظ†ط§ط، ط§ظ„ط¨ظٹط¹");
            return Result<SalesInvoiceDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹ ط£ط«ظ†ط§ط، ط¥ظ†ط´ط§ط، ظپط§طھظˆط±ط© ط§ظ„ط¨ظٹط¹");
            return Result<SalesInvoiceDto>.Failure(
                "ط­ط¯ط« ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹. ظٹط±ط¬ظ‰ ط§ظ„ظ…ط­ط§ظˆظ„ط© ظ…ط±ط© ط£ط®ط±ظ‰.");
        }
    }

    private Result ValidateRequest(CreateSalesInvoiceRequest request)
    {
        if (request.Items == null || !request.Items.Any())
            return Result.Failure("ظٹط¬ط¨ ط¥ط¶ط§ظپط© ظ…ظ†طھط¬ ظˆط§ط­ط¯ ط¹ظ„ظ‰ ط§ظ„ط£ظ‚ظ„");

        if (request.PaidAmount < 0)
            return Result.Failure("ط§ظ„ظ…ط¨ظ„ط؛ ط§ظ„ظ…ط¯ظپظˆط¹ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");

        if (request.PaymentType is < 1 or > 3)
            return Result.Failure("ظ†ظˆط¹ ط§ظ„ط¯ظپط¹ ط؛ظٹط± طµط­ظٹط­");

        foreach (var item in request.Items)
        {
            if (item.ProductId <= 0)
                return Result.Failure("ظ…ط¹ط±ظ‘ظپ ط§ظ„ظ…ظ†طھط¬ ط؛ظٹط± طµط­ظٹط­");
            if (item.Quantity <= 0)
                return Result.Failure("ط§ظ„ظƒظ…ظٹط© ظٹط¬ط¨ ط£ظ† طھظƒظˆظ† ط£ظƒط¨ط± ظ…ظ† طµظپط±");
            if (item.UnitPrice < 0)
                return Result.Failure("ط§ظ„ط³ط¹ط± ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");
            if (item.DiscountAmount < 0)
                return Result.Failure("ط§ظ„ط®طµظ… ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");

            // âڑ ï¸ڈ ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† LineTotal ظ…ظ†ط·ظ‚ظٹط§ظ‹
            var lineTotal = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
            if (lineTotal < 0)
                return Result.Failure(
                    $"ط¥ط¬ظ…ط§ظ„ظٹ ط§ظ„ط³ط·ط± ط³ط§ظ„ط¨ ظ„ظ„ظ…ظ†طھط¬ {item.ProductId}. " +
                    "ط§ظ„ط®طµظ… ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² ط³ط¹ط± ط§ظ„ط¨ظٹط¹.");
        }

        return Result.Success();
    }
}
8.3 InventoryService âڑ ï¸ڈ ط­ط±ط¬ ط¬ط¯ط§ظ‹
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
    /// âڑ ï¸ڈ ظٹظڈط³طھط¯ط¹ظ‰ ط¯ط§ط¦ظ…ط§ظ‹ ط¯ط§ط®ظ„ Transaction ط®ط§ط±ط¬ظٹط©
    /// </summary>
    public async Task DecreaseStockAsync(
        int warehouseId, int productId, decimal quantity,
        MovementType movementType, string referenceType,
        int referenceId, decimal? unitCost, int? userId,
        CancellationToken ct = default)
    {
        // ط¬ظ„ط¨ ط£ظˆ ط¥ظ†ط´ط§ط، ط³ط¬ظ„ ط§ظ„ظ…ط®ط²ظˆظ†
        var stock = await _uow.WarehouseStocks
            .GetByWarehouseAndProductAsync(warehouseId, productId, ct);

        if (stock == null)
            throw new DomainException(
                $"ظ„ط§ ظٹظˆط¬ط¯ ط³ط¬ظ„ ظ…ط®ط²ظˆظ† ظ„ظ„ظ…ظ†طھط¬ {productId} ظپظٹ ط§ظ„ظ…ط®ط²ظ† {warehouseId}");

        var quantityBefore = stock.Quantity;

        // âڑ ï¸ڈ ظ‡ط°ط§ ظٹط±ظ…ظٹ InsufficientStockException ط¥ط°ط§ ظ„ظ… طھظƒظ† ط§ظ„ظƒظ…ظٹط© ظƒط§ظپظٹط©
        stock.Decrease(quantity);

        // طھط³ط¬ظٹظ„ ط§ظ„ط­ط±ظƒط©
        var movement = new InventoryMovement
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            MovementType = movementType,
            QuantityChange = -quantity,    // ط³ط§ظ„ط¨ = ط®ط±ظˆط¬
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

        // ط¥ط°ط§ ظ„ظ… ظٹظƒظ† ظ‡ظ†ط§ظƒ ط³ط¬ظ„طŒ ظ†ظڈظ†ط´ط¦ ظˆط§ط­ط¯ط§ظ‹
        if (stock == null)
        {
            stock = WarehouseStock.Create(warehouseId, productId);
            await _uow.WarehouseStocks.AddAsync(stock, ct);
            await _uow.SaveChangesAsync(ct); // ظ„ظ„ط­طµظˆظ„ ط¹ظ„ظ‰ ID
        }

        var quantityBefore = stock.Quantity;
        stock.Increase(quantity);

        var movement = new InventoryMovement
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            MovementType = movementType,
            QuantityChange = quantity,     // ظ…ظˆط¬ط¨ = ط¯ط®ظˆظ„
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
8.4 DocumentSequenceService â€” طھظˆظ„ظٹط¯ ط£ط±ظ‚ط§ظ… ط§ظ„ظˆط«ط§ط¦ظ‚
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
    /// âڑ ï¸ڈ Thread-safe â€” ظٹظڈط³طھط®ط¯ظ… Lock ظ„ظ…ظ†ط¹ ط§ظ„ط£ط±ظ‚ط§ظ… ط§ظ„ظ…ظƒط±ط±ط©
    /// ط§ظ„ظ…ط®ط±ط¬: INV-2025-000001
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
                    $"Sequence prefix '{prefix}' ط؛ظٹط± ظ…ظˆط¬ظˆط¯");

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
8.5 SalesReturn Service âڑ ï¸ڈ ظ…ظ†ط·ظ‚ ط§ظ„ظ…ط±طھط¬ط¹ ط§ظ„ط­ط±ط¬
csharp

// SalesSystem.Application/Services/SalesReturnService.cs
public async Task<Result<SalesReturnDto>> CreateReturnAsync(
    CreateSalesReturnRequest request, int? userId, CancellationToken ct = default)
{
    // === ط§ظ„طھط­ظ‚ظ‚ ط§ظ„ظ…ط³ط¨ظ‚ ===
    // ط¥ط°ط§ ظƒط§ظ† ظ…ط±طھط¬ط¹ ظ„ظپط§طھظˆط±ط© ظ…ط­ط¯ط¯ط©طŒ طھط­ظ‚ظ‚ ظ…ظ† ط§ظ„ظƒظ…ظٹط§طھ
    if (request.SalesInvoiceId.HasValue)
    {
        var originalInvoice = await _uow.SalesInvoices
            .GetByIdWithItemsAsync(request.SalesInvoiceId.Value, ct);

        if (originalInvoice == null)
            return Result<SalesReturnDto>.Failure("ط§ظ„ظپط§طھظˆط±ط© ط§ظ„ط£طµظ„ظٹط© ط؛ظٹط± ظ…ظˆط¬ظˆط¯ط©");

        if (originalInvoice.Status == InvoiceStatus.Cancelled)
            return Result<SalesReturnDto>.Failure("ظ„ط§ ظٹظ…ظƒظ† ط¥ط±ط¬ط§ط¹ ظپط§طھظˆط±ط© ظ…ظ„ط؛ط§ط©");

        // âڑ ï¸ڈ ط§ظ„طھط­ظ‚ظ‚: ط§ظ„ظƒظ…ظٹط© ط§ظ„ظ…ظڈط±ط¬ط¹ط© ظ„ط§ طھطھط¬ط§ظˆط² ط§ظ„ظƒظ…ظٹط© ط§ظ„ط£طµظ„ظٹط©
        foreach (var returnItem in request.Items)
        {
            var originalItem = originalInvoice.Items
                .FirstOrDefault(i => i.ProductId == returnItem.ProductId);

            if (originalItem == null)
                return Result<SalesReturnDto>.Failure(
                    $"ط§ظ„ظ…ظ†طھط¬ {returnItem.ProductId} ط؛ظٹط± ظ…ظˆط¬ظˆط¯ ظپظٹ ط§ظ„ظپط§طھظˆط±ط© ط§ظ„ط£طµظ„ظٹط©");

            // âڑ ï¸ڈ ط­ط³ط§ط¨ ظ…ط§ طھظ… ط¥ط±ط¬ط§ط¹ظ‡ ظ…ط³ط¨ظ‚ط§ظ‹
            var previouslyReturned = await _uow.SalesReturnItems
                .GetTotalReturnedQuantityAsync(
                    request.SalesInvoiceId.Value, returnItem.ProductId, ct);

            var maxReturnable = originalItem.Quantity - previouslyReturned;

            if (returnItem.Quantity > maxReturnable)
                return Result<SalesReturnDto>.Failure(
                    $"ط§ظ„ظƒظ…ظٹط© ط§ظ„ظ…ط·ظ„ظˆط¨ ط¥ط±ط¬ط§ط¹ظ‡ط§ ({returnItem.Quantity}) " +
                    $"ط£ظƒط¨ط± ظ…ظ† ط§ظ„ظƒظ…ظٹط© ط§ظ„ظ‚ط§ط¨ظ„ط© ظ„ظ„ط¥ط±ط¬ط§ط¹ ({maxReturnable})");
        }
    }

    // === Transaction ===
    await using var transaction = await _uow.BeginTransactionAsync(ct);
    try
    {
        var returnNo = await _sequenceService.GenerateAsync("SR", ct);

        // ط­ط³ط§ط¨ ط§ظ„ط¥ط¬ظ…ط§ظ„ظٹ
        var totalAmount = request.Items
            .Sum(i => i.Quantity * i.UnitPrice);   // âœ… decimal

        // ط¥ظ†ط´ط§ط، ط§ظ„ظ…ط±طھط¬ط¹
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

        // ط¥ط¶ط§ظپط© ط§ظ„ط¹ظ†ط§طµط±
        foreach (var item in request.Items)
        {
            var returnItem = new SalesReturnItem
            {
                SalesReturnId = salesReturn.SalesReturnId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice  // âœ… decimal
            };
            await _uow.SalesReturnItems.AddAsync(returnItem, ct);
        }

        await _uow.SaveChangesAsync(ct);

        // âڑ ï¸ڈ ط¥ط¹ط§ط¯ط© ط§ظ„ظ…ط®ط²ظˆظ† (ظ…ط±طھط¬ط¹ ط§ظ„ط¨ظٹط¹ ظٹط²ظٹط¯ ط§ظ„ظ…ط®ط²ظˆظ†)
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

        // âڑ ï¸ڈ طھط®ظپظٹط¶ ط±طµظٹط¯ ط§ظ„ط¹ظ…ظٹظ„ (ط§ظ„ظ…ط±طھط¬ط¹ ظٹظڈظ‚ظ„ظ„ ط§ظ„ط¯ظٹظ†)
        var customer = await _uow.Customers.GetByIdAsync(request.CustomerId, ct);
        customer!.DecreaseBalance(totalAmount);
        await _uow.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return Result<SalesReturnDto>.Success(MapToDto(salesReturn));
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync(ct);
        _logger.LogError(ex, "ط®ط·ط£ ظپظٹ ط¥ظ†ط´ط§ط، ظ…ط±طھط¬ط¹ ط§ظ„ط¨ظٹط¹");
        return Result<SalesReturnDto>.Failure("ط­ط¯ط« ط®ط·ط£ ط؛ظٹط± ظ…طھظˆظ‚ط¹");
    }
}
9. Exceptions ط§ظ„ظ…ط®طµطµط©
csharp


<!-- END OF SECTION: 6.1 Baseline Application Services -->


<!-- START OF SECTION: 6.2 Advanced Modules Application Services (v4.3) -->

> [!NOTE]
> Services implementing Dynamic UOM costing cascade (UpdateProductPricingService) and transactional command handlers.

âڑ™ï¸ڈ Phase 3: Application Layer â€” Pricing Service
Task 3.1 â€” UpdateProductPricingService
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
    decimal? NewSalesPrice,          // Optional â€” user may override
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
        // â”€â”€â”€ 1. Load the purchased unit and ALL units for this product â”€â”€â”€
        var purchasedUnit = await _context.ProductUnits
            .Include(u => u.Product)
                .ThenInclude(p => p.Units)
            .FirstOrDefaultAsync(u => u.Id == request.ProductUnitId, ct)
            ?? throw new NotFoundException("ProductUnit", request.ProductUnitId);

        var product = purchasedUnit.Product;
        var allUnits = product.Units.Where(u => u.IsActive).ToList();
        var baseUnit = product.GetBaseUnit();

        // â”€â”€â”€ 2. Calculate new BASE UNIT cost â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€ 3. Cascade cost update to ALL units â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€ 4. Update sales price if user provided one â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€â”€ 5. Save history â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
                // Weighted average: [(OldStock أ— OldCost) + (NewQty أ— NewCost)] / TotalQty
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
Task 3.2 â€” Purchase Invoice Command (Updated)
csharp

// File: Application/Commands/CreatePurchaseInvoice/CreatePurchaseInvoiceCommand.cs

public record CreatePurchaseInvoiceCommand : IRequest<int>
{
    public int SupplierId { get; init; }
    public int CashBoxId { get; init; }     // NEW: Which cash box pays
    public int CashierId { get; init; }
    public string? Notes { get; init; }
    public List<PurchaseInvoiceLineRequest> Items { get; init; } = new();
}

public record PurchaseInvoiceLineRequest(
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
        // â”€â”€â”€ 1. Validate CashBox access â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var cashBox = await _unitOfWork.CashBoxes
            .GetByIdAsync(command.CashBoxId, cancellationToken)
            ?? throw new NotFoundException("CashBox", command.CashBoxId);

        cashBox.CanUserAccess(command.CashierId); // Throws if no permission

        // â”€â”€â”€ 2. Create invoice â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

            // Add stock â€” Domain converts to base units internally
            productUnit.Product.Stock.AddStock(
                itemRequest.Quantity,
                productUnit.BaseConversionFactor);

            totalAmount += (itemRequest.Quantity * itemRequest.UnitCost) - itemRequest.Discount;
        }

        // â”€â”€â”€ 3. Deduct from cash box â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        cashBox.Withdraw(
            totalAmount,
            CashTransactionType.PurchaseOut,
            referenceType: "PurchaseInvoice",
            referenceId: invoice.Id,
            createdBy: command.CashierId,
            notes: $"ط¯ظپط¹ ظپط§طھظˆط±ط© ظ…ط´طھط±ظٹط§طھ ط±ظ‚ظ… {invoice.Id}");

        // â”€â”€â”€ 4. Save invoice and stock â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        _unitOfWork.PurchaseInvoices.Add(invoice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // â”€â”€â”€ 5. Update product pricing (AFTER save so invoice.Id exists) â”€â”€
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
Task 3.3 â€” Cash Transfer Command
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
            throw new DomainException("ظ„ط§ ظٹظ…ظƒظ† ط§ظ„طھط­ظˆظٹظ„ ظ…ظ† ط§ظ„طµظ†ط¯ظˆظ‚ ط¥ظ„ظ‰ ظ†ظپط³ظ‡");

        var fromBox = await _unitOfWork.CashBoxes
            .GetByIdAsync(command.FromCashBoxId, cancellationToken)
            ?? throw new NotFoundException("CashBox", command.FromCashBoxId);

        var toBox = await _unitOfWork.CashBoxes
            .GetByIdAsync(command.ToCashBoxId, cancellationToken)
            ?? throw new NotFoundException("CashBox", command.ToCashBoxId);

        fromBox.CanUserAccess(command.TransferredBy);

        // These two domain calls maintain balance integrity
        fromBox.Withdraw(command.Amount, CashTransactionType.TransferOut,
            notes: $"طھط­ظˆظٹظ„ ط¥ظ„ظ‰: {toBox.BoxName} | {command.Notes}",
            createdBy: command.TransferredBy);

        toBox.Deposit(command.Amount, CashTransactionType.TransferIn,
            notes: $"طھط­ظˆظٹظ„ ظ…ظ†: {fromBox.BoxName} | {command.Notes}",
            createdBy: command.TransferredBy);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
âœ… Phase 3 Checklist
 WeightedAverage formula is correct: (OldQtyأ—OldCost + NewQtyأ—NewCost) / TotalQty
 LastPurchasePrice just overwrites â€” no formula
 Cost cascade goes to ALL units (base and derived)
 Derived unit cost = baseUnitCost أ— ConversionFactor
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
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
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
        // طھط·ط¨ظٹظ‚ ظƒظ„ ط§ظ„ظ€ Configurations
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SalesDbContext).Assembly);
    }
}
10.2 WarehouseStock Configuration âڑ ï¸ڈ
csharp

// SalesSystem.Infrastructure/Configurations/WarehouseStockConfiguration.cs
public class WarehouseStockConfiguration 
    : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks");
        builder.HasKey(x => x.WarehouseStockId);

        // âڑ ï¸ڈ decimal(18,3) ظ„ظ„ظƒظ…ظٹط© â€” ط¥ظ„ط²ط§ظ…ظٹ
        builder.Property(x => x.Quantity)
            .HasColumnType("decimal(18,3)")
            .HasDefaultValue(0m);

        // âڑ ï¸ڈ Unique Constraint ط­ط±ط¬
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

        // âڑ ï¸ڈ ظƒظ„ ط§ظ„ط£ظ…ظˆط§ظ„ decimal(18,2)
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
            .OnDelete(DeleteBehavior.Restrict); // âڑ ï¸ڈ v4.6 â€” NO Cascade delete
    }
}
11. API Controllers
11.1 SalesController
csharp


<!-- END OF SECTION: 7.1 Baseline EF Core Infrastructure -->


<!-- START OF SECTION: 7.2 Advanced Infrastructure configurations & Services -->

> [!NOTE]
> DbContext bindings and configs for advanced entities, plus infrastructural services like BarcodeLookupService.

âڑ™ï¸ڈ Phase 2: Infrastructure â€” EF Core & Services
Task 2.1 â€” EF Configurations
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
Task 2.2 â€” Barcode Lookup Service (Updated)
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
Task 2.3 â€” Settings Repository
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
âœ… Phase 2 Checklist
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

ًں–¥ï¸ڈ Phase 4: WPF ViewModels
Task 4.1 â€” Product Unit Builder ViewModel
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
            // New product â€” show onboarding and pre-add base unit row
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
            Placeholder_UnitName = "ظ…ط«ط§ظ„: ط­ط¨ط©طŒ ظ‚ط·ط¹ط©طŒ ط¨ظٹط¶ط©"
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
            Placeholder_UnitName = "ظ…ط«ط§ظ„: ط·ط¨ظ‚طŒ ظƒط±طھظˆظ†"
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
            ValidationSummary = "âڑ ï¸ڈ ظ„ط§ ظٹظ…ظƒظ† ط­ط°ظپ ط§ظ„ظˆط­ط¯ط© ط§ظ„ط£ط³ط§ط³ظٹط© ط¥ط°ط§ ظƒط§ظ†طھ ظ‡ظ†ط§ظƒ ظˆط­ط¯ط§طھ ط£ط®ط±ظ‰ ظ…ط±طھط¨ط·ط© ط¨ظ‡ط§.";
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
            errors.Add("âڑ ï¸ڈ ط£ط¶ظپ ظˆط­ط¯ط© طµط؛ط±ظ‰ ظˆط§ط­ط¯ط© (ظ…ط«ط§ظ„: ط­ط¨ط©) ظˆط§ط¬ط¹ظ„ ظ…ط¹ط§ظ…ظ„ ط§ظ„طھط­ظˆظٹظ„ = 1");

        if (baseUnits.Count > 1)
            errors.Add("âڑ ï¸ڈ ظ„ط§ ظٹظ…ظƒظ† طھط¹ط±ظٹظپ ط£ظƒط«ط± ظ…ظ† ظˆط­ط¯ط© طµط؛ط±ظ‰ ظˆط§ط­ط¯ط©");

        foreach (var unit in Units)
        {
            if (string.IsNullOrWhiteSpace(unit.UnitName))
                errors.Add($"âڑ ï¸ڈ ط§ظ„طµظپ {unit.SortOrder + 1}: ط§ط³ظ… ط§ظ„ظˆط­ط¯ط© ظ…ط·ظ„ظˆط¨");

            if (!unit.IsBaseUnit && unit.BaseConversionFactor <= 1)
                errors.Add(
                    $"âڑ ï¸ڈ '{unit.UnitName}': ظ…ط¹ط§ظ…ظ„ ط§ظ„طھط­ظˆظٹظ„ ظٹط¬ط¨ ط£ظ† ظٹظƒظˆظ† ط£ظƒط¨ط± ظ…ظ† 1 " +
                    $"(ظƒظ… ظˆط­ط¯ط© طµط؛ط±ظ‰ ط¨ط¯ط§ط®ظ„ظ‡ط§طں)");
        }

        ValidationSummary = errors.Any()
            ? string.Join("\n", errors)
            : "âœ… ظˆط­ط¯ط§طھ ط§ظ„ظ…ظ†طھط¬ طµط­ظٹط­ط©";

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
                "ًں’، ظƒظٹظپ طھط¨ظ†ظٹ ظˆط­ط¯ط§طھ ط§ظ„ظ…ظ†طھط¬طں\n\n" +
                "1ï¸ڈâƒ£  ط§ط¨ط¯ط£ ط¯ط§ط¦ظ…ط§ظ‹ ط¨ط¥ط¶ط§ظپط© ط§ظ„ظˆط­ط¯ط© ط§ظ„طµط؛ط±ظ‰\n" +
                "     ط§ظ„طھظٹ ظ„ط§ ظٹظ…ظƒظ† طھط¬ط²ط¦طھظ‡ط§ (ظ…ط«ظ„: ط­ط¨ط©)\n" +
                "     ظˆط§ط¬ط¹ظ„ ظ…ط¹ط§ظ…ظ„ ط§ظ„طھط­ظˆظٹظ„ = 1\n\n" +
                "2ï¸ڈâƒ£  ط«ظ… ط£ط¶ظپ ط§ظ„ظˆط­ط¯ط§طھ ط§ظ„ط£ظƒط¨ط±\n" +
                "     (ظ…ط«ظ„: ط·ط¨ظ‚طŒ ظƒط±طھظˆظ†)\n" +
                "     ظˆط§ظƒطھط¨ ظƒظ… (ط­ط¨ط©) ط¨ط¯ط§ط®ظ„ظ‡ط§.\n\n" +
                "     ظ…ط«ط§ظ„: ط·ط¨ظ‚ ط§ظ„ط¨ظٹط¶ = 30 ط­ط¨ط©\n" +
                "              ظƒط±طھظˆظ† = 12 ط·ط¨ظ‚ = 360 ط­ط¨ط©\n\n" +
                "âœ…  ط§ظ„ظ†ط¸ط§ظ… ط³ظٹط­ط³ط¨ ظƒظ„ ط´ظٹط، طھظ„ظ‚ط§ط¦ظٹط§ظ‹!"
        };
        dialog.ShowDialog();
    }
}
Task 4.2 â€” Purchase Invoice ViewModel (with Price Sync Indicator)
csharp

// File: WPF/ViewModels/Invoice/PurchaseInvoiceLineViewModel.cs

public class PurchaseInvoiceLineViewModel : BaseViewModel
{
    private readonly IMediator _mediator;

    private int _productUnitId;
    private decimal _quantity = 1;
    private decimal _unitCost;
    private decimal _newSalesPrice;
    private decimal _oldCostInDatabase;
    private string _unitName = string.Empty;

    // â”€â”€â”€ Properties â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â­گ KEY: Shows sync warning icon when cost differs from DB
    public bool CostChangedFromDatabase =>
        _oldCostInDatabase > 0 &&
        Math.Abs(UnitCost - _oldCostInDatabase) > 0.0001m;

    public string PriceDifferenceIndicator
    {
        get
        {
            if (!CostChangedFromDatabase) return string.Empty;

            var diff = UnitCost - _oldCostInDatabase;
            var direction = diff > 0 ? "â†‘ ط§ط±طھظپط¹" : "â†“ ط§ظ†ط®ظپط¶";
            return $"ًں”„ {direction} ط¹ظ† ط§ظ„ط³ط¹ط± ط§ظ„ظ‚ط¯ظٹظ… ({_oldCostInDatabase:N2}) " +
                   $"| ط³ظٹطھظ… طھط­ط¯ظٹط« ط§ظ„طھظƒظ„ظپط© ظپظٹ ط¨ط·ط§ظ‚ط© ط§ظ„طµظ†ظپ ط¹ظ†ط¯ ط§ظ„ط­ظپط¸";
        }
    }

    // â”€â”€â”€ Available units for ComboBox â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
âœ… Phase 4 Checklist
 ProductUnitBuilderViewModel.Validate() shows Arabic error messages
 Onboarding dialog shows automatically for new products
 CostChangedFromDatabase triggers when user edits cost field
 PriceDifferenceIndicator shows direction (â†‘/â†“) and old value
 Unit ComboBox in purchase invoice pre-fills cost from DB

<!-- END OF SECTION: 8.2 Advanced WPF ViewModels -->


<!-- START OF SECTION: 8.3 Advanced WPF XAML Controls -->

> [!NOTE]
> XAML layout declarations for Dynamic UOM sliders, inline invoice rows, and settings views.

ًں–¼ï¸ڈ Phase 5: WPF XAML
Task 5.1 â€” Unit Hierarchy Builder XAML
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
                    <Run FontWeight="Bold">ًں’، ظƒظٹظپ طھط¨ظ†ظٹ ظˆط­ط¯ط§طھ ط§ظ„ظ…ظ†طھط¬طں  </Run>
                    <LineBreak/>
                    <Run>1. ط§ط¨ط¯ط£ ط¨ط§ظ„ظˆط­ط¯ط© ط§ظ„طµط؛ط±ظ‰ (ظ…ط«ط§ظ„: ط­ط¨ط©) â€” ظ…ط¹ط§ظ…ظ„ ط§ظ„طھط­ظˆظٹظ„ = 1</Run>
                    <LineBreak/>
                    <Run>2. ط£ط¶ظپ ط§ظ„ظˆط­ط¯ط§طھ ط§ظ„ط£ظƒط¨ط± ظˆط§ظƒطھط¨ ظƒظ… ظˆط­ط¯ط© طµط؛ط±ظ‰ ط¨ط¯ط§ط®ظ„ظ‡ط§</Run>
                </TextBlock>
                <Button Grid.Column="1"
                        Content="طھظپط§طµظٹظ„ ط£ظƒط«ط± طں"
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
                <DataGridTemplateColumn Header="ط§ط³ظ… ط§ظ„ظˆط­ط¯ط©" Width="140">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <TextBox Text="{Binding UnitName, UpdateSourceTrigger=PropertyChanged}"
                                     PlaceholderText="{Binding Placeholder_UnitName}"
                                     BorderThickness="0" Padding="4"/>
                        </DataTemplate>
                    </DataGridTemplateColumn.CellTemplate>
                </DataGridTemplateColumn>

                <!-- Conversion Factor -->
                <DataGridTemplateColumn Header="ظٹط³ط§ظˆظٹ ظƒظ… ظˆط­ط¯ط© طµط؛ط±ظ‰طں" Width="160">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Grid>
                                <!-- Show "1 (ط£ط³ط§ط³ظٹط©)" for base unit -->
                                <TextBlock
                                    Text="1  âœ… (ظˆط­ط¯ط© ط£ط³ط§ط³ظٹط©)"
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
                <DataGridTextColumn Header="ط³ط¹ط± ط§ظ„ط¨ظٹط¹"
                    Binding="{Binding SalesPrice, UpdateSourceTrigger=PropertyChanged}"
                    Width="90"/>

                <!-- Purchase Cost -->
                <DataGridTextColumn Header="طھظƒظ„ظپط© ط§ظ„ط´ط±ط§ط،"
                    Binding="{Binding PurchaseCost, UpdateSourceTrigger=PropertyChanged}"
                    Width="90"/>

                <!-- Supplier Price -->
                <DataGridTextColumn Header="ط³ط¹ط± ط§ظ„ظ…ظˆط±ط¯"
                    Binding="{Binding SupplierPrice, UpdateSourceTrigger=PropertyChanged}"
                    Width="90"/>

                <!-- Last Purchase Price (Read Only) -->
                <DataGridTextColumn Header="ط¢ط®ط± ط³ط¹ط± طھظˆط±ظٹط¯"
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
                <DataGridTextColumn Header="ط¹ط¯ط¯ ط§ظ„ط¨ط§ط±ظƒظˆط¯ط§طھ"
                    Binding="{Binding BarcodesCount}"
                    Width="110"
                    IsReadOnly="True"/>

                <!-- Delete Button -->
                <DataGridTemplateColumn Header="" Width="40">
                    <DataGridTemplateColumn.CellTemplate>
                        <DataTemplate>
                            <Button Content="ًں—‘"
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
        <Button Content="+ ط¥ط¶ط§ظپط© ظˆط­ط¯ط© ط¬ط¯ظٹط¯ط©"
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
Task 5.2 â€” Purchase Invoice Item Row (Price Sync Indicator)
XML

<!-- Inside Purchase Invoice DataGrid -->
<!-- Add these two columns to existing DataGrid -->

<!-- Unit Cost with sync indicator -->
<DataGridTemplateColumn Header="طھظƒظ„ظپط© ط§ظ„ظˆط­ط¯ط©" Width="130">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBox Text="{Binding UnitCost, UpdateSourceTrigger=PropertyChanged}"
                         BorderThickness="0"/>
                <!-- Sync warning â€” only shows when cost differs from DB -->
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
<DataGridTemplateColumn Header="ط³ط¹ط± ط§ظ„ط¨ظٹط¹ ط§ظ„ط¬ط¯ظٹط¯" Width="120">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <TextBox Text="{Binding NewSalesPrice, UpdateSourceTrigger=PropertyChanged}"
                     PlaceholderText="ط§ط®طھظٹط§ط±ظٹ"
                     BorderThickness="0"
                     ToolTip="ط¥ط°ط§ ط£ط¯ط®ظ„طھ ط³ط¹ط±ط§ظ‹ ط¬ط¯ظٹط¯ط§ظ‹طŒ ط³ظٹطھظ… طھط­ط¯ظٹط« ط³ط¹ط± ط¨ظٹط¹ ط§ظ„طµظ†ظپ ظپظˆط± ط­ظپط¸ ط§ظ„ظپط§طھظˆط±ط©"/>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
Task 5.3 â€” Settings Screen (Costing Method Selector)
XML

<!-- File: Views/Settings/CostingMethodSettingView.xaml -->
<StackPanel Margin="16" FlowDirection="RightToLeft">

    <TextBlock Text="ط·ط±ظٹظ‚ط© ط§ط­طھط³ط§ط¨ طھظƒظ„ظپط© ط§ظ„ظ…ط®ط²ظˆظ†"
               FontSize="16" FontWeight="Bold" Margin="0,0,0,12"/>

    <!-- Option 1: Weighted Average -->
    <Border BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="8"
            Padding="16" Margin="0,4">
        <StackPanel>
            <RadioButton Content="ظ…طھظˆط³ط· ط§ظ„طھظƒظ„ظپط© ط§ظ„ظ…ط±ط¬ط­  (Weighted Average)"
                         IsChecked="{Binding IsWeightedAverageSelected}"
                         FontSize="14" FontWeight="Bold"/>
            <TextBlock Margin="24,6,0,0" TextWrapping="Wrap" Foreground="#555"
                       Text="ظٹط¬ظ…ط¹ ط¨ظٹظ† ط³ط¹ط± ط§ظ„ط¨ط¶ط§ط¹ط© ط§ظ„ظ‚ط¯ظٹظ…ط© ظپظٹ ط§ظ„ظ…ط®ط²ظ† ظˆط§ظ„ط¬ط¯ظٹط¯ط© ظ„ظٹط¹ط·ظٹظƒ طھظƒظ„ظپط© ظ…ظˆط­ط¯ط© ظˆظ…طھظˆط§ط²ظ†ط©. âœ… ط§ظ„ط£ظ†ط³ط¨ ظ„ظ„طھظ‚ط§ط±ظٹط± ط§ظ„ط¶ط±ظٹط¨ظٹط© ط§ظ„ط¯ظ‚ظٹظ‚ط© ظˆظ„ظ„ظ…ط­ط§ط³ط¨ط© ط§ظ„ظ‚ظٹط§ط³ظٹط©."/>
        </StackPanel>
    </Border>

    <!-- Option 2: Last Purchase Price -->
    <Border BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="8"
            Padding="16" Margin="0,4">
        <StackPanel>
            <RadioButton Content="ط¢ط®ط± ط³ط¹ط± طھظˆط±ظٹط¯  (Last Purchase Price)"
                         IsChecked="{Binding IsLastPriceSelected}"
                         FontSize="14" FontWeight="Bold"/>
            <TextBlock Margin="24,6,0,0" TextWrapping="Wrap" Foreground="#555"
                       Text="ظٹط³طھط¨ط¯ظ„ طھظƒظ„ظپط© ط§ظ„ظ…ظ†طھط¬ ط¨ط³ط¹ط± ط¢ط®ط± ظپط§طھظˆط±ط© ط´ط±ط§ط، ظ…ط¨ط§ط´ط±ط©ظ‹. âœ… ظ…ظ†ط§ط³ط¨ ظ„ظ„ط£ط³ظˆط§ظ‚ ط§ظ„ظ…طھظ‚ظ„ط¨ط© ط­ظٹط« طھط±ظٹط¯ ط¯ط§ط¦ظ…ط§ظ‹ ط£ظ† ظٹط¹ظƒط³ ط§ظ„ط³ط¹ط± ط§ظ„ظˆط§ظ‚ط¹ ط§ظ„ط­ط§ظ„ظٹ."/>
        </StackPanel>
    </Border>

    <!-- Option 3: Supplier Price -->
    <Border BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="8"
            Padding="16" Margin="0,4">
        <StackPanel>
            <RadioButton Content="ط³ط¹ط± ط§ظ„ظ…ظˆط±ط¯  (Supplier Catalog Price)"
                         IsChecked="{Binding IsSupplierPriceSelected}"
                         FontSize="14" FontWeight="Bold"/>
            <TextBlock Margin="24,6,0,0" TextWrapping="Wrap" Foreground="#555"
                       Text="ظٹط¹طھظ…ط¯ ط¹ظ„ظ‰ ط§ظ„ط³ط¹ط± ط§ظ„ظ…ط¯ط®ظ„ ظپظٹ ط¨ط·ط§ظ‚ط© ط§ظ„طµظ†ظپ ظ…ظ† ظ‚ط§ط¦ظ…ط© ط§ظ„ظ…ظˆط±ط¯ ظˆظ„ط§ ظٹطھط؛ظٹط± طھظ„ظ‚ط§ط¦ظٹط§ظ‹ ط¹ظ†ط¯ ط§ظ„ط´ط±ط§ط،. âœ… ظ…ظ†ط§ط³ط¨ ط¹ظ†ط¯ظ…ط§ طھطھظپط§ظˆط¶ ط¹ظ„ظ‰ ط³ط¹ط± ط«ط§ط¨طھ ظ…ط¹ ط§ظ„ظ…ظˆط±ط¯ ظ„ظپطھط±ط© ط·ظˆظٹظ„ط©."/>
        </StackPanel>
    </Border>

    <Button Content="ًں’¾  ط­ظپط¸ ط§ظ„ط¥ط¹ط¯ط§ط¯"
            Command="{Binding SaveCostingMethodCommand}"
            HorizontalAlignment="Left"
            Margin="0,16,0,0"
            Padding="20,10"
            Background="#1976D2" Foreground="White" BorderThickness="0"/>
</StackPanel>
âœ… Phase 5 Checklist
 Help box always visible (not just on first open)
 Validation error box only shows when HasValidationError = true
 Base unit row shows "âœ… ظˆط­ط¯ط© ط£ط³ط§ط³ظٹط©" and factor is read-only
 Sync warning shows correct direction (â†‘/â†“) with old price
 Each costing method has Arabic explanation text
 All interactive elements minimum 36px height (touch-friendly)

<!-- END OF SECTION: 8.3 Advanced WPF XAML Controls -->


# 9. Clean Architecture & Production Engineering Guidelines

Detailed breakdown of Clean Architecture layers, project dependencies, design systems, and cross-cutting concerns.

<!-- START OF SECTION: 9.1 Core Philosophy & Layered Architecture -->

> [!NOTE]
> Architecture constraints, boundary directories, and clean separation of concerns.

# MASTER-PLAN â€” Sales Management System (v4.6.2 â€” Validation ErrorTemplate & INotifyDataErrorInfo)

## ًں“‹ Core Philosophy

**One source of truth. AGENTS.md is LAW.** Every rule lives in exactly ONE place. Agents cannot break what they cannot bypass.

- **Clean Architecture (Layered)** â€” NOT Vertical Slices, NOT Feature Folders
- **Domain is king** â€” ZERO dependencies, rich entities, business rules enforced at the entity level
- **Desktop â†’ API â†’ SQL Server** â€” Desktop NEVER connects to the database
- **Result<T> over exceptions** â€” Services return results, controllers translate to HTTP
- **Bilingual UI** â€” Arabic labels, English code. All text columns use `nvarchar`
- **AGENTS.md > everything** â€” If code conflicts with AGENTS.md, the code is wrong

---

## ًںڈ—ï¸ڈ Actual Architecture

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”گ
â”‚                        SOLUTION STRUCTURE (11 Projects)                  â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                                         â”‚
â”‚  SalesSystem.slnx                                                       â”‚
â”‚  â”œâ”€â”€ ًں“¦ SalesSystem.Domain/          â†گ Entities + Enums + Exceptions    â”‚
â”‚  â”‚      (net10.0, ZERO NuGet deps)                                      â”‚
â”‚  â”‚                                                                       â”‚
â”‚  â”œâ”€â”€ ًں“¦ SalesSystem.Contracts/       â†گ DTOs + Requests + Result<T>      â”‚
â”‚  â”‚      (net10.0, ZERO NuGet deps)                                      â”‚
â”‚  â”‚                                                                       â”‚
â”‚  â”œâ”€â”€ ًں“¦ SalesSystem.Application/     â†گ Service interfaces + impls       â”‚
â”‚  â”‚      (net10.0)                                                        â”‚
â”‚  â”‚                                                                       â”‚
â”‚  â”œâ”€â”€ ًں“¦ SalesSystem.Infrastructure/  â†گ EF Core + DbContext + Repos      â”‚
â”‚  â”‚      (net10.0-windows)           + Printing + Backup                 â”‚
â”‚  â”‚                                                                       â”‚
â”‚  â”œâ”€â”€ ًں“¦ SalesSystem.Api/             â†گ Controllers + FluentValidation   â”‚
â”‚  â”‚      (net10.0-windows)           + JWT + Serilog + Swagger           â”‚
â”‚  â”‚                                                                       â”‚
â”‚  â”œâ”€â”€ ًں“¦ SalesSystem.DesktopPWF/      â†گ WPF UI + MVVM + EventBus         â”‚
â”‚  â”‚      (net10.0-windows)           + Navigation + Dialogs              â”‚
â”‚  â”‚                                                                       â”‚
â”‚  â””â”€â”€ ًں§ھ Tests/ (5 projects)          â†گ Unit + Integration tests         â”‚
â”‚                                                                         â”‚
â”‚  Legacy/ (NOT in solution)                                              â”‚
â”‚  â””â”€â”€ ًں—‘ï¸ڈ SalesSystem.Desktop/         â†گ Abandoned WinForms (safe delete) â”‚
â”‚                                                                         â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”ک

Data Flow (NEVER break this chain):

  Desktop (WPF)
      â†“ HttpClient
  SalesSystem.Api (Controllers + FluentValidation + JWT)
      â†“ delegates to
  SalesSystem.Application (Service interfaces + implementations)
      â†“ delegates to
  SalesSystem.Infrastructure (EF Core + DbContext + Repositories)
      â†“ connects to
  SQL Server
      â†‘
  SalesSystem.Domain (ZERO dependencies â€” referenced by ALL layers)
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
- **Rich Domain Model** â€” entities have `private set` + factory methods + guard clauses
- **4-layer validation** â€” Domain â†’ Application â†’ API (FluentValidation) â†’ Database (CHECK constraints)

---

## âœ… Implemented Features (Phases 1-7)

| Phase | Status | Key Deliverables |
|-------|--------|-----------------|
| **Phase 1: Foundation** | âœ… Complete | Domain entities (Product, Customer, Supplier, Invoice, etc.), Enums, DomainException, Guard Clauses, Contracts (DTOs, Requests, Result<T>) |
| **Phase 2: Infrastructure** | âœ… Complete | EF Core DbContext, Repositories, IUnitOfWork, Migrations, Fluent API config, CHECK constraints, Seed data |
| **Phase 3: Application** | âœ… Complete | Service interfaces + implementations for all modules (Products, Customers, Suppliers, Sales, Purchases, Returns, Stock, Reports, Settings, Users, CashBoxes, Inventory) |
| **Phase 4: API** | âœ… Complete | REST Controllers for all modules, FluentValidation validators, JWT authentication, Policy-based authorization, Swagger/OpenAPI, Serilog logging, Error middleware |
| **Phase 5: Desktop Shell** | âœ… Complete | WPF application, Navigation system, MVVM infrastructure, ViewModelBase (292 lines), EventBus, Login screen, Session management, Role-based UI |
| **Phase 6: Desktop Modules** | âœ… Complete | All CRUD screens (Products, Customers, Suppliers, Categories, Units, Warehouses), Sales/Purchase invoices, Returns, Stock transfers, Payments, Reports (Excel export), Barcode input |
| **Phase 7: Production** | âœ… Complete | Auto-Update system, DPAPI encryption, Backup/Restore (raw SQL), Windows Service, Admin screens, Inno Setup installer, Styled dialogs (6 types), Toast notifications, Print engine (A4 + Thermal) |

---

## ًں”§ Actual Code Patterns

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
- ALL services return `Result<T>` or `Result` â€” NEVER throw exceptions
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
- Controllers have ZERO business logic â€” delegate to services
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
            throw new DomainException("ط§ط³ظ… ط§ظ„ظ…ظ†طھط¬ ظ…ط·ظ„ظˆط¨");
        if (categoryId <= 0)
            throw new DomainException("ط§ظ„ظپط¦ط© ظ…ط·ظ„ظˆط¨ط©");

        return new Product { Name = name, CategoryId = categoryId };
    }

    // State change via method
    public void UpdatePrice(decimal retailPrice, decimal wholesalePrice)
    {
        if (retailPrice < 0)
            throw new DomainException("ط³ط¹ط± ط§ظ„طھط¬ط²ط¦ط© ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");
        if (wholesalePrice < 0)
            throw new DomainException("ط³ط¹ط± ط§ظ„ط¬ظ…ظ„ط© ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");
        // ... update logic
    }
}
```

**Key rules:**
- `private set` on ALL critical properties
- State changes via methods ONLY â€” never direct property modification
- Guard clauses in constructors and factory methods
- `DomainException` with Arabic messages

### Validation (4 Layers)

| Layer | Where | Example |
|-------|-------|---------|
| **Domain** | Entity methods | `if (price < 0) throw DomainException("ط§ظ„ط³ط¹ط± ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹")` |
| **Application** | Service methods | Stock availability check before transaction |
| **API** | FluentValidation | `RuleFor(x => x.Name).NotEmpty().WithMessage("ط§ظ„ط§ط³ظ… ظ…ط·ظ„ظˆط¨")` |
| **Database** | CHECK constraints | `CHECK (Quantity >= 0)`, `CHECK (PaidAmount <= TotalAmount)` |

---


<!-- END OF SECTION: 9.1 Core Philosophy & Layered Architecture -->


<!-- START OF SECTION: 9.2 WPF Input Validation Architecture (INotifyDataErrorInfo & ErrorTemplate) -->

> [!NOTE]
> Real-time validation engine replacing archaic booleans with centralized async input validation.

## ًں“ٹ Test Coverage

| Test Project | Target | Status |
|-------------|--------|--------|
| **SalesSystem.Domain.Tests** | Domain entities, guard clauses, business rules | âœ… Active |
| **SalesSystem.Application.Tests** | Service logic, Result<T> patterns | âœ… Active |
| **SalesSystem.Infrastructure.Tests** | EF Core mappings, repositories, migrations | âœ… Active |
| **SalesSystem.Api.Tests** | Controller endpoints, validation, auth | âœ… Active |
| **SalesSystem.Integration.Tests** | End-to-end flows, API + DB integration | âœ… Active |

---


<!-- END OF SECTION: 9.2 WPF Input Validation Architecture (INotifyDataErrorInfo & ErrorTemplate) -->


<!-- START OF SECTION: 9.3 Project Dependencies & Design System -->

> [!NOTE]
> Nuget package manifests and WPF resource styles used to implement modern Dark Mode / Glassmorphism layouts.

## ًں“¦ Project Dependencies

```
SalesSystem.Domain
  â””â”€â”€ (ZERO dependencies â€” pure C#)

SalesSystem.Contracts
  â””â”€â”€ SalesSystem.Domain

SalesSystem.Application
  â”œâ”€â”€ SalesSystem.Domain
  â””â”€â”€ SalesSystem.Contracts
  â””â”€â”€ Microsoft.Extensions.Logging.Abstractions
  â””â”€â”€ MediatR (installed, minimally used)

SalesSystem.Infrastructure
  â”œâ”€â”€ SalesSystem.Application
  â”œâ”€â”€ SalesSystem.Contracts
  â”œâ”€â”€ SalesSystem.Domain
  â””â”€â”€ Microsoft.EntityFrameworkCore.SqlServer 10.x
  â””â”€â”€ BCrypt.Net-Next 4.x
  â””â”€â”€ QuestPDF 2024.3.x
  â””â”€â”€ SixLabors.ImageSharp 3.1.x
  â””â”€â”€ System.Drawing.Common 10.x
  â””â”€â”€ Microsoft.Extensions.Hosting.WindowsServices 10.x
  â””â”€â”€ Microsoft.AspNetCore.DataProtection 10.x

SalesSystem.Api
  â”œâ”€â”€ SalesSystem.Application
  â”œâ”€â”€ SalesSystem.Contracts
  â”œâ”€â”€ SalesSystem.Infrastructure
  â”œâ”€â”€ SalesSystem.Domain
  â””â”€â”€ FluentValidation.AspNetCore 11.x
  â””â”€â”€ Serilog.AspNetCore 8.x
  â””â”€â”€ Microsoft.AspNetCore.Authentication.JwtBearer 10.x
  â””â”€â”€ Swashbuckle.AspNetCore 6.x
  â””â”€â”€ Serilog.Sinks.EventLog 8.x

SalesSystem.DesktopPWF
  â”œâ”€â”€ SalesSystem.Contracts
  â”œâ”€â”€ SalesSystem.Domain
  â””â”€â”€ Microsoft.Extensions.Http 10.x
  â””â”€â”€ System.Text.Json 10.x
  â””â”€â”€ ClosedXML 0.102.x
```

---

## ًںژ¨ Design System (Actual)

**Location:** `SalesSystem.DesktopPWF/Resources/Styles.xaml` (782 lines)

**NOT** `DesignTokens.cs` â€” that file was NEVER created. All styles are centralized in a single XAML ResourceDictionary.

### What's in Styles.xaml:

- **Color Brushes** â€” Primary, Success, Warning, Error, Info, Neutral palette
- **Typography** â€” TextBlock styles for Display, Header, SubHeader, Body, Caption
- **Button Styles** â€” Primary, Secondary, Danger, Success, Ghost, Icon
- **Card Styles** â€” Card (with shadow), CardFlat (no shadow)
- **Input Styles** â€” TextBox, ComboBox, PasswordBox
- **DataGrid Styles** â€” Standard grid with alternating rows, styled headers
- **Status Badges** â€” Success, Warning, Error badges
- **Validation Styles** â€” Red border for validation errors
- **Dialog Styles** â€” Styled dialogs (Error, Success, Warning, Info, Confirmation, DeleteConfirmation)
- **Navigation Styles** â€” Sidebar, menu items, active state
- **Toast Styles** â€” Notification toasts with auto-dismiss

### Usage Pattern:

```xml
<!-- In any XAML view -->
<Button Style="{StaticResource ButtonPrimary}" Content="ط­ظپط¸"/>
<TextBlock Style="{StaticResource TextHeader}" Text="ط§ظ„ظ…ظ†طھط¬ط§طھ"/>
<TextBox Style="{StaticResource TextBoxStandard}" Text="{Binding Name}"/>
<DataGrid Style="{StaticResource DataGridStandard}" .../>
<Border Style="{StaticResource BadgeSuccess}" .../>
```

**Rule:** NEVER hardcode colors or sizes in XAML views â€” always use `{StaticResource ...}`.

---


<!-- END OF SECTION: 9.3 Project Dependencies & Design System -->


# 10. Core Enterprise & Integration Services

Production-grade hardware integration, machine data encryption, high-performance reporting, and system hosting.

<!-- START OF SECTION: 10.1 Hardware Barcode Scanner Service -->

> [!NOTE]
> Event-driven barcode listener capturing continuous keystrokes for instant product scanning.

## ًں“، Barcode Service (Actual)

**Interface:** `IBarcodeInputService` (NOT `IBarcodeScanner`)
**Implementation:** `BarcodeInputService`
**Location:** `SalesSystem.DesktopPWF/Services/App/Barcode/`

### How it works:

USB barcode scanners act as keyboard emulators â€” they type the barcode characters then send Enter. The service intercepts at the application level using a keyboard buffer with timing detection.

```csharp
public interface IBarcodeInputService
{
    event Action<string> BarcodeScanned;
    void StartListening();
    void StopListening();
}
```

### Key characteristics:
- **Keyboard buffer** â€” accumulates characters typed by scanner
- **100ms timeout** â€” distinguishes scanner (fast) from human typing (slow)
- **Application-level** â€” works across all screens, no per-screen setup
- **USB/HID only** â€” NO camera-based scanning (MAUI was never built)
- **Event-driven** â€” fires `BarcodeScanned` event with barcode string

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

## ًں”گ Security (Actual)

### DPAPI Connection String Encryption

- Connection strings encrypted via `IDataProtector` with `"DPAPI:"` prefix
- `FirstRunSetupService` encrypts plaintext connection string on first run (idempotent)
- `SecureDbContextFactory` decrypts before creating DbContext
- DataProtection keys stored in `%ProgramData%\SalesSystem\DataProtectionKeys`
- `appsettings.json` writes use atomic pattern: `.tmp` â†’ `File.Replace()` â†’ `.bak`

### JWT Authentication

- JWT secret from environment variable â€” throws `InvalidOperationException` in production if missing
- `BCrypt` passwords with work factor = 12
- Policy-based authorization: `AdminOnly`, `ManagerAndAbove`, `AllStaff`
- ALL endpoints require `[Authorize]` (except `/api/auth/login`)

### Security Audit

- `SecurityAudit.cs` runs in DEBUG only â€” checks for unencrypted strings, hardcoded passwords
- NEVER log: passwords, connection strings
- Serilog for all logging â€” NEVER `Console.WriteLine`

---


<!-- END OF SECTION: 10.2 Production-Grade DPAPI Encryption Security -->


<!-- START OF SECTION: 10.3 High-Performance Print Engine (QuestPDF & Thermal Raw Printing) -->

> [!NOTE]
> High-performance Win32 raw buffer thermal printing and QuestPDF invoice report generator.

## ًں–¨ï¸ڈ Print Engine (Actual)

**NOT WPF FixedDocument/PrintDialog** â€” uses QuestPDF + Win32 raw printing.

### A4 Invoices (QuestPDF)

- **Library:** QuestPDF Community (free for < $1M revenue)
- **Document:** `A4InvoiceDocument.cs` â€” RTL layout, logo, tax breakdown
- **Output:** PDF files
- **Preview:** WPF `PdfPreviewWindow` (WebBrowser control)

### Thermal Receipts (Win32 Raw Printing)

- **API:** Win32 `OpenPrinter` / `WritePrinter` via `DllImport`
- **Builder:** Custom `EscPos` static class â€” NOT external NuGet packages
- **Format:** 42-char monospaced columns, Windows-1256 encoding for Arabic
- **Output:** Direct to thermal printer (80mm)

### Architecture:

```
Desktop â†’ IPrintApiService (HTTP) â†’ PrintController (API) â†’ IPrintService â†’ Printer
```

**Desktop NEVER prints directly** â€” always goes through the API.

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

**NEVER throw from printing code** â€” always return `PrintResult`.

### Project Structure:

```
SalesSystem.Application/Printing/
â”œâ”€â”€ Contracts/
â”‚   â”œâ”€â”€ InvoicePrintDto.cs
â”‚   â”œâ”€â”€ InvoiceItemPrintDto.cs
â”‚   â”œâ”€â”€ InvoiceTypePrint.cs
â”‚   â””â”€â”€ PrintResult.cs
â”œâ”€â”€ InvoicePrintDtoBuilder.cs
â””â”€â”€ IPrintService.cs

SalesSystem.Infrastructure/Printing/
â”œâ”€â”€ A4InvoiceDocument.cs
â”œâ”€â”€ ThermalReceiptGenerator.cs
â”œâ”€â”€ EscPos.cs
â”œâ”€â”€ PrintService.cs
â”œâ”€â”€ PrinterException.cs
â””â”€â”€ PrintingBootstrapper.cs
```

---


<!-- END OF SECTION: 10.3 High-Performance Print Engine (QuestPDF & Thermal Raw Printing) -->


<!-- START OF SECTION: 10.4 Automated Enterprise Administration (Auto-Update & Daily Backup) -->

> [!NOTE]
> Silent background updater with SHA256 integrity check and automated database backup routines.

## ًں”„ Auto-Update (Actual)

**Location:** `SalesSystem.DesktopPWF/Services/Update/`

### Key rules:
- **NEVER blocks startup** â€” fire-and-forget with silent failure
- **8-second timeout** â€” user never waits for update check
- **SHA256 checksum** verification before launching installer
- **Skipped version** persisted to `%AppData%\SalesSystem\settings.json`
- **Desktop calls API** for updates â€” NEVER implements its own HTTP download
- **`Environment.Exit(0)` is FORBIDDEN** â€” return `Result<bool>` instead

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

Uses `System.Version` â€” NEVER string comparison.

---

## ًں’¾ Backup System (Actual)

**Location:** `SalesSystem.Infrastructure/Backup/`

### Key rules:
- **Raw SQL `BACKUP DATABASE`** â€” NEVER SMO dependency
- **Restore uses `SINGLE_USER WITH ROLLBACK AFTER 30`** â€” gives active transactions 30s
- **Scheduled backup** runs daily at 2:00 AM as `BackgroundService`
- **Retention** = configurable days (default 30) â€” old backups auto-deleted
- **Restore failure** triggers `TrySetMultiUserAsync` recovery
- **Config parsing** uses `int.TryParse` â€” NEVER `int.Parse`

### ScheduledBackupWorker:

```csharp
public class ScheduledBackupWorker : BackgroundService
{
    // Uses IServiceScopeFactory for scoped service resolution
    // Respects CancellationToken for graceful shutdown
    // Runs backup at 2:00 AM daily â†’ then cleanup old backups
}
```

---


<!-- END OF SECTION: 10.4 Automated Enterprise Administration (Auto-Update & Daily Backup) -->


<!-- START OF SECTION: 10.5 Service Hosting & Database Diagnostics -->

> [!NOTE]
> Configuring the API to run as a native Windows Service and validating connection status on bootstrap.

## ًں–¥ï¸ڈ Windows Service (Actual)

**Location:** `SalesSystem.Api/Program.cs`

### Configuration:
- **Service name:** `SalesSystemService` (Arabic display name)
- **Auto-recovery:** 3 restarts on failure (1min, 5min, 15min delays)
- **Serilog EventLog sink** for Windows Service logging
- **SQL retry on startup:** 3 attempts أ— 5 second delay
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
        // userId ظ…ظ† JWT ظ„ط§ط­ظ‚ط§ظ‹ â€” ط§ظ„ط¢ظ† null
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
            ? Ok(new { message = "طھظ… ط¥ظ„ط؛ط§ط، ط§ظ„ظپط§طھظˆط±ط© ط¨ظ†ط¬ط§ط­" })
            : BadRequest(new { error = result.Error });
    }
}
12. Desktop â€” EventBus
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
                // âڑ ï¸ڈ طھظ†ظپظٹط° ط¹ظ„ظ‰ UI Thread (WPF)
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

// Messages (ID only â€” no data payloads per RULE-034)
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

ًں§ھ Phase 6: Unit Tests
csharp

// File: Tests/Domain/ProductUnitTests.cs

public class ProductUnitTests
{
    [Fact]
    public void CreateBaseUnit_AlwaysHasFactorOne()
    {
        var unit = ProductUnit.CreateBaseUnit(productId: 1, unitName: "ط­ط¨ط©");
        Assert.Equal(1, unit.BaseConversionFactor);
        Assert.True(unit.IsBaseUnit);
    }

    [Fact]
    public void CreateDerivedUnit_WithFactorOne_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            ProductUnit.CreateDerivedUnit(1, "ظƒط±طھظˆظ†", baseConversionFactor: 1));

        Assert.Contains("ط£ظƒط¨ط± ظ…ظ† ظˆط­ط¯ط© طµط؛ط±ظ‰ ظˆط§ط­ط¯ط©", ex.Message);
    }

    [Fact]
    public void ToBaseUnitQuantity_MultipliesCorrectly()
    {
        var box = ProductUnit.CreateDerivedUnit(1, "ظƒط±طھظˆظ†", 12);
        var baseQty = box.ToBaseUnitQuantity(3); // 3 boxes أ— 12 = 36 pieces
        Assert.Equal(36, baseQty);
    }

    [Fact]
    public void CalculateCostFromBaseUnitCost_ScalesCorrectly()
    {
        var box = ProductUnit.CreateDerivedUnit(1, "ظƒط±طھظˆظ†", 12);
        var boxCost = box.CalculateCostFromBaseUnitCost(baseUnitCost: 2m);
        Assert.Equal(24m, boxCost); // 2 SAR/piece أ— 12 pieces = 24 SAR/box
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
        // Expected: (10أ—100 + 10أ—150) / 20 = 125 SAR

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
        var box = CashBox.Create("طµظ†ط¯ظˆظ‚ ط§ظ„ظƒط§ط´ظٹط±");
        box.Deposit(100, CashTransactionType.ManualIn, createdBy: 1);

        var ex = Assert.Throws<DomainException>(() =>
            box.Withdraw(200, CashTransactionType.PurchaseOut, createdBy: 1));

        Assert.Contains("ط±طµظٹط¯ ط§ظ„طµظ†ط¯ظˆظ‚ ط؛ظٹط± ظƒط§ظپظچ", ex.Message);
    }

    [Fact]
    public void Deposit_UpdatesBalanceAndCreatesTransaction()
    {
        var box = CashBox.Create("طµظ†ط¯ظˆظ‚ ط§ظ„ظƒط§ط´ظٹط±");
        box.Deposit(500, CashTransactionType.SaleIn, createdBy: 1);

        Assert.Equal(500, box.CurrentBalance);
        Assert.Single(box.Transactions);
        Assert.Equal(0, box.Transactions.First().BalanceBefore);
        Assert.Equal(500, box.Transactions.First().BalanceAfter);
    }

    [Fact]
    public void CanUserAccess_WrongUser_ThrowsDomainException()
    {
        var box = CashBox.Create("طµظ†ط¯ظˆظ‚ ظƒط§ط´ظٹط± 1", assignedUserId: 5);

        Assert.Throws<DomainException>(() => box.CanUserAccess(userId: 99));
    }

    [Fact]
    public void CanUserAccess_SharedBox_AllowsAnyUser()
    {
        var sharedBox = CashBox.Create("ط§ظ„طµظ†ط¯ظˆظ‚ ط§ظ„ط±ط¦ظٹط³ظٹ"); // No assigned user

        // Should NOT throw for any user
        var exception = Record.Exception(() => sharedBox.CanUserAccess(userId: 99));
        Assert.Null(exception);
    }
}
ًں“¦ Final Summary
text

â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”گ
â”‚         DYNAMIC UOM + COSTING + CASH BOXES â€” IMPLEMENTATION      â”‚
â”œâ”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ Step â”‚ Deliverable                              â”‚ Key Rule       â”‚
â”œâ”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  0   â”‚ 8 SQL migrations                         â”‚ Run in order   â”‚
â”‚  1   â”‚ 6 Domain entities + 2 enums              â”‚ No DB in Domainâ”‚
â”‚  2   â”‚ EF configs + Barcode + Settings repos    â”‚ Register in DI â”‚
â”‚  3   â”‚ Pricing service + Commands               â”‚ Save then priceâ”‚
â”‚  4   â”‚ ViewModels (Builder + Invoice)           â”‚ Arabic errors  â”‚
â”‚  5   â”‚ XAML (Builder + Settings + Invoice)      â”‚ 36px min touch â”‚
â”‚  6   â”‚ 7 Unit tests                             â”‚ Never skip     â”‚
â””â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”ک

CRITICAL RULES â€” NEVER VIOLATE:
â”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پâ”پ
âœ… Stock ALWAYS stored in base units (pieces) â€” never in boxes/trays
âœ… Cost cascade: base unit cost أ— ConversionFactor = derived unit cost
âœ… Pricing update runs AFTER invoice.Id is persisted
âœ… WeightedAverage: zero old stock â†’ return new cost (no division by zero)
âœ… CashBox.Withdraw() uses domain method â€” never subtract balance directly
âœ… Product.ValidateUnits() called in command handler before ANY save
âœ… All user-facing errors in Arabic with actionable guidance
âœ… Price history logged for EVERY cost change (audit trail)


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
> ظ‚ظˆط§ط¹ط¯ ط§ظ„ط¹ظ…ظ„ ظˆط§ظ„طھط­ظ‚ظ‚ ظˆط§ظ„ط­ط³ط§ط¨ط§طھ ط§ظ„ظ…ط§ظ„ظٹط© ط§ظ„ظ…ط¹طھظ…ط¯ظ‡ ظ„ظ„ظ†ط¸ط§ظ… ط¨ط§ظ„طھظپطµظٹظ„.

13. ظ‚ظˆط§ط¹ط¯ ط§ظ„ط¹ظ…ظ„ ط§ظ„ط­ط±ط¬ط© â€” ظ…ط±ط¬ط¹ ط³ط±ظٹط¹
text

â•”â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•—
â•‘          ظ‚ظˆط§ط¹ط¯ ط§ظ„ط¹ظ…ظ„ ط§ظ„ط­ط±ط¬ط© â€” ظٹط¬ط¨ طھط·ط¨ظٹظ‚ظ‡ط§ ظپظٹ ظƒظ„ ظ…ظƒط§ظ†           â•‘
â• â•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•£
â•‘                                                                  â•‘
â•‘  FINANCIAL CALCULATIONS                                          â•‘
â•‘  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€                                          â•‘
â•‘  âœ… LineTotal = (Quantity * UnitPrice) - DiscountAmount          â•‘
â•‘  âœ… SubTotal  = Sum(LineTotal)                                   â•‘
â•‘  âœ… Total     = SubTotal - InvoiceDiscount + TaxAmount           â•‘
â•‘  âœ… Due       = Total - PaidAmount                               â•‘
â•‘  â‌Œ ظ„ط§ طھط­ط³ط¨ ظپظٹ ط§ظ„ظ€ UI â€” ط§ظ„ط­ط³ط§ط¨ ظپظٹ Domain Layer ظپظ‚ط·             â•‘
â•‘  â‌Œ ظ„ط§ float/double ظپظٹ ط£ظٹ ط­ط³ط§ط¨ ظ…ط§ظ„ظٹ                             â•‘
â•‘                                                                  â•‘
â•‘  STOCK RULES                                                     â•‘
â•‘  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€                                                     â•‘
â•‘  âœ… طھط­ظ‚ظ‚ ظ…ظ† ط§ظ„ظ…ط®ط²ظˆظ† ظ‚ط¨ظ„ ظپطھط­ Transaction                         â•‘
â•‘  âœ… ط§ط®طµظ… ط§ظ„ظ…ط®ط²ظˆظ† ط¨ط¹ط¯ ط­ظپط¸ ط§ظ„ظپط§طھظˆط±ط© ظˆطھظˆظ„ظٹط¯ ID                    â•‘
â•‘  âœ… ط³ط¬ظ„ ظپظٹ InventoryMovements ظ…ط¹ ظƒظ„ طھط؛ظٹظٹط±                       â•‘
â•‘  â‌Œ ظ„ط§ ط¨ظٹط¹ ط¨ظƒظ…ظٹط© طھطھط¬ط§ظˆط² ط§ظ„ظ…طھظˆظپط± ظپظٹ ط§ظ„ظ…ط®ط²ظ† ط§ظ„ظ…ط­ط¯ط¯               â•‘
â•‘  â‌Œ ظ„ط§ ظƒظ…ظٹط§طھ ط³ط§ظ„ط¨ط© ظپظٹ WarehouseStocks                           â•‘
â•‘                                                                  â•‘
â•‘  BALANCE RULES                                                   â•‘
â•‘  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€                                                   â•‘
â•‘  Customer Balance ظ…ظˆط¬ط¨ = ط§ظ„ط¹ظ…ظٹظ„ ظ…ط¯ظٹظ† ظ„ظ†ط§                        â•‘
â•‘  Customer Balance ط³ط§ظ„ط¨ = ظ„ط¯ظٹظ†ط§ ط±طµظٹط¯ ظ„ظ„ط¹ظ…ظٹظ„                     â•‘
â•‘  Supplier Balance ظ…ظˆط¬ط¨ = ط¹ظ„ظٹظ†ط§ ظ„ظ„ظ…ظˆط±ط¯                           â•‘
â•‘  Supplier Balance ط³ط§ظ„ط¨ = ط§ظ„ظ…ظˆط±ط¯ ظ…ط¯ظٹظ† ظ„ظ†ط§                        â•‘
â•‘                                                                  â•‘
â•‘  TRANSACTION RULES                                               â•‘
â•‘  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€                                               â•‘
â•‘  âœ… ظƒظ„ ظپط§طھظˆط±ط© (ط¨ظٹط¹/ط´ط±ط§ط،/ظ…ط±طھط¬ط¹/طھط­ظˆظٹظ„) ظپظٹ Transaction ظˆط§ط­ط¯ط©     â•‘
â•‘  âœ… ط¥ط°ط§ ظپط´ظ„ ط£ظٹ ط®ط·ظˆط© â†’ Rollback ظƒط§ظ…ظ„                             â•‘
â•‘  â‌Œ ظ„ط§ Partial Commits                                           â•‘
â•‘                                                                  â•‘
â•‘  INVOICE RULES                                                   â•‘
â•‘  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€                                                   â•‘
â•‘  âœ… ط§ظ„ظپط§طھظˆط±ط© طھط¨ط¯ط£ Draft ط«ظ… طھظڈط±ط­ظژظ‘ظ„ Posted                       â•‘
â•‘  âœ… ط§ظ„ظ…ط®ط²ظˆظ† ظٹطھط£ط«ط± ط¹ظ†ط¯ Posted ظپظ‚ط·                                â•‘
â•‘  âœ… ط§ظ„ط¥ظ„ط؛ط§ط، ظٹط¹ظƒط³ ط£ط«ط± ط§ظ„ظ…ط®ط²ظˆظ† ظˆط§ظ„ط±طµظٹط¯                           â•‘
â•‘  â‌Œ ظ„ط§ ط­ط°ظپ ظ†ظ‡ط§ط¦ظٹ ظ„ظ„ظپظˆط§طھظٹط±                                       â•‘
â•‘  â‌Œ ظ„ط§ طھط¹ط¯ظٹظ„ ط¹ظ„ظ‰ ظپط§طھظˆط±ط© Posted                                  â•‘
â•‘                                                                  â•‘
â•ڑâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•گâ•‌

<!-- END OF SECTION: 13. Critical Business Rules Reference (Arabic) -->


# 14. Implementation History & Evolution Roadmap

Comprehensive milestones history, technological decisions, legacy cleanup, and system roadmap.

<!-- START OF SECTION: 14.1 Baseline Project Implementation Phases (Phases 1-13) -->

> [!NOTE]
> Original implementation plan mapping out baseline modules.

14. ط®ط·ط© ط§ظ„طھظ†ظپظٹط° ط§ظ„ظ…ط±ط­ظ„ظٹط©
text

Phase 1 â€” Foundation (ط§ظ„ط£ط³ط§ط³)
â”œâ”€â”€ ط¥ظ†ط´ط§ط، ط§ظ„ظ€ 6 ظ…ط´ط§ط±ظٹط¹ ظپظٹ Solution
â”œâ”€â”€ Domain Entities ظƒط§ظ…ظ„ط©
â”œâ”€â”€ Contracts (DTOs + Requests + Result<T>)
â”œâ”€â”€ Custom Exceptions
â””â”€â”€ SQL Script ظ„ظ„ط¬ط¯ط§ظˆظ„ ظƒط§ظ…ظ„ط© ظ…ط¹ Seed Data

Phase 2 â€” Infrastructure
â”œâ”€â”€ SalesDbContext + ط¬ظ…ظٹط¹ ط§ظ„ظ€ Configurations
â”œâ”€â”€ EF Core Migrations
â”œâ”€â”€ IUnitOfWork + UnitOfWork
â”œâ”€â”€ Repositories ط§ظ„ط£ط³ط§ط³ظٹط©
â””â”€â”€ ط§ط®طھط¨ط§ط± ط§ظ„ط§طھطµط§ظ„ ط¨ظ‚ط§ط¹ط¯ط© ط§ظ„ط¨ظٹط§ظ†ط§طھ

Phase 3 â€” Application Services
â”œâ”€â”€ ProductService (ط§ظ„ط¨ط¯ط§ظٹط© â€” ط§ظ„ط£ط¨ط³ط·)
â”œâ”€â”€ CustomerService + SupplierService
â”œâ”€â”€ WarehouseService
â”œâ”€â”€ DocumentSequenceService âڑ ï¸ڈ ظ…ظ‡ظ…
â”œâ”€â”€ InventoryService âڑ ï¸ڈ ط­ط±ط¬
â”œâ”€â”€ PurchaseService âڑ ï¸ڈ ط­ط±ط¬
â”œâ”€â”€ SalesService âڑ ï¸ڈ ط§ظ„ط£ط­ط±ط¬
â”œâ”€â”€ SalesReturnService âڑ ï¸ڈ
â””â”€â”€ PurchaseReturnService âڑ ï¸ڈ

Phase 4 â€” API
â”œâ”€â”€ ط¬ظ…ظٹط¹ ط§ظ„ظ€ Controllers
â”œâ”€â”€ Global Exception Handler
â””â”€â”€ Swagger Setup

Phase 5 â€” Desktop Shell
â”œâ”€â”€ EventBus
â”œâ”€â”€ NavigationService
â”œâ”€â”€ MainForm + Sidebar
â”œâ”€â”€ LoginForm
â””â”€â”€ Common Controls

Phase 6 â€” Desktop Modules
â”œâ”€â”€ Products Module (ط§ظ„ظ‚ط§ظ„ط¨)
â”œâ”€â”€ Customers + Suppliers
â”œâ”€â”€ Warehouses
â”œâ”€â”€ Purchases Module
â”œâ”€â”€ Sales Module âڑ ï¸ڈ ط§ظ„ط£ظ‡ظ…
â”œâ”€â”€ Returns Module
â””â”€â”€ Reports

Phase 7 â€” Polish
â”œâ”€â”€ BackupService
â”œâ”€â”€ Settings Screen
â””â”€â”€ Error Handling UI

Phase 8 â€” Dynamic UOM & Costing (v4.3)
â”œâ”€â”€ ProductUnits: ظˆط­ط¯ط§طھ ط¯ظٹظ†ط§ظ…ظٹظƒظٹط© ظ„ظƒظ„ ظ…ظ†طھط¬ ظ…ط¹ ط£ط³ط¹ط§ط± ظ…ظ†ظپطµظ„ط©
â”œâ”€â”€ UnitBarcodes: ط¨ط§ط±ظƒظˆط¯ ظ„ظƒظ„ ظˆط­ط¯ط© ظ…ظ†طھط¬
â”œâ”€â”€ UpdateProductPricingService: 3 ط§ط³طھط±ط§طھظٹط¬ظٹط§طھ طھط³ط¹ظٹط±
â”œâ”€â”€ ProductPriceHistory: ط³ط¬ظ„ طھط؛ظٹظٹط±ط§طھ ط§ظ„ط£ط³ط¹ط§ط± ظˆط§ظ„طھظƒظ„ظپط©
â””â”€â”€ Cost cascade: طھط­ط¯ظٹط« طھظƒظ„ظپط© ط¬ظ…ظٹط¹ ط§ظ„ظˆط­ط¯ط§طھ أ— ط¹ط§ظ…ظ„ ط§ظ„طھط­ظˆظٹظ„

Phase 9 â€” Cash Boxes (v4.3)
â”œâ”€â”€ CashBoxes: طµظ†ط§ط¯ظٹظ‚ ظ†ظ‚ط¯ظٹط© ظ…طھط¹ط¯ط¯ط©
â”œâ”€â”€ CashTransactions: ط­ط±ظƒط§طھ ظ†ظ‚ط¯ظٹط© ط؛ظٹط± ظ‚ط§ط¨ظ„ط© ظ„ظ„طھط¹ط¯ظٹظ„
â”œâ”€â”€ DailyClosure: ط¥ظ‚ظپط§ظ„ ظٹظˆظ…ظٹ
â””â”€â”€ Transfer: طھط­ظˆظٹظ„ ط¨ظٹظ† ط§ظ„طµظ†ط§ط¯ظٹظ‚ (ط­ط±ظƒطھظٹظ†)

Phase 10 â€” Print Engine (v4.3)
â”œâ”€â”€ A4: QuestPDF ظ…ط¹ ط´ط¹ط§ط± ط§ظ„ظ…طھط¬ط±
â”œâ”€â”€ Thermal: ESC/POS ط·ط¨ط§ط¹ط© ط­ط±ط§ط±ظٹط© 80mm
â”œâ”€â”€ API: Desktop ظٹط·ظ„ط¨ ط§ظ„ط·ط¨ط§ط¹ط© ط¹ط¨ط± PrintController
â”œâ”€â”€ Preview: ظ…ط¹ط§ظٹظ†ط© ظ‚ط¨ظ„ ط§ظ„ط·ط¨ط§ط¹ط©
â””â”€â”€ Logo: ظ…ط¹ط§ظ„ط¬ط© ط؛ظٹط§ط¨ ط§ظ„ط´ط¹ط§ط± (null check)

Phase 11 â€” Production (v4.4)
â”œâ”€â”€ Auto-Update: طھط­ط¯ظٹط« طھظ„ظ‚ط§ط¦ظٹ ظ…ط¹ ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† SHA256
â”œâ”€â”€ DPAPI: طھط´ظپظٹط± connection strings
â”œâ”€â”€ Backup: ظ†ط³ط® ط§ط­طھظٹط§ط·ظٹ طھظ„ظ‚ط§ط¦ظٹ ظٹظˆظ…ظٹ
â”œâ”€â”€ Windows Service: طھط´ط؛ظٹظ„ API ظƒط®ط¯ظ…ط© ظˆظٹظ†ط¯ظˆط²
â””â”€â”€ Health Check: ظپط­طµ ظ‚ط§ط¹ط¯ط© ط§ظ„ط¨ظٹط§ظ†ط§طھ ط¹ظ†ط¯ ط¨ط¯ط، ط§ظ„طھط´ط؛ظٹظ„

Phase 12 â€” UI Polish (v4.5)
â”œâ”€â”€ Multi-Window: ظپطھط­ ط§ظ„ظ†ظˆط§ظپط° ط¨ط¯ظˆظ† ط­ط¸ط± (ScreenWindowService)
â”œâ”€â”€ ToolTips: طھظ„ظ…ظٹط­ط§طھ ط¹ط±ط¨ظٹط© ظ„ط¬ظ…ظٹط¹ ط§ظ„ط£ط²ط±ط§ط±
â”œâ”€â”€ Sorting: طھط±طھظٹط¨ ط§ظ„ظ‚ظˆط§ط¦ظ… ظ…ظ† ط§ظ„ط£ط­ط¯ط« ظ„ظ„ط£ظ‚ط¯ظ…
â””â”€â”€ EventBus: ط¥ط¯ط§ط±ط© ط§ظ„ط§ط´طھط±ط§ظƒط§طھ (subscribe/dispose)

Phase 13 â€” Validation & Cleanup (v4.6â€“v4.6.2)
â”œâ”€â”€ ID Strategy: ط¥ط²ط§ظ„ط© Code ظ…ظ† ط§ظ„ظ…ظ†طھط¬ط§طھ ظˆط§ظ„ط¹ظ…ظ„ط§ط، ظˆط§ظ„ظ…ظˆط±ط¯ظٹظ† ظˆط§ظ„ظ…ط®ط§ط²ظ†
â”œâ”€â”€ ErrorTemplate: ط¥ط·ط§ط± ط£ط­ظ…ط± + â‌— ظ…ط¹ ToolTip
â”œâ”€â”€ INotifyDataErrorInfo: طھط­ظ‚ظ‚ ظپظˆط±ظٹ ظپظٹ ط­ظ‚ظˆظ„ ط§ظ„ط¥ط¯ط®ط§ظ„
â”œâ”€â”€ ValidateAllAsync: طھط­ظ‚ظ‚ ظ…ظˆط­ط¯ ظ‚ط¨ظ„ ط§ظ„ط­ظپط¸
â””â”€â”€ SetDialogService: ط±ط¨ط· ط®ط¯ظ…ط© ط§ظ„ط­ظˆط§ط± ظپظٹ ط¬ظ…ظٹط¹ ط§ظ„ظ€ ViewModels

<!-- END OF SECTION: 14.1 Baseline Project Implementation Phases (Phases 1-13) -->


<!-- START OF SECTION: 14.2 Advanced Security & Quality Hardening Phases (Phases 18-20) -->

> [!NOTE]
> Recent phases executing real-time validation templates, architecture alignment, and security rate-limiting.

## âœ… WPF Validation ErrorTemplate & INotifyDataErrorInfo (v4.6.2) â€” Historical

**Goal**: Standardize validation UI with red border + â‌— icon ErrorTemplate, replace `HasXxxError` boolean pattern with `INotifyDataErrorInfo`, and add `ValidateAllAsync()` to ViewModelBase.

### Key Changes
- **New ErrorTemplate**: Red border (#EF4444, 1.5px) + â‌— icon badge with ToolTip â€” applies to TextBox, PasswordBox, ComboBox when `Validation.HasError = true`
- **ViewModelBase.cs**: Added `SetDialogService(IDialogService)`, `ValidateAllAsync()`, `ValidateField()`
- **14 Editor VMs**: All call `SetDialogService()` in constructors
- **ProductEditorViewModel**: Removed 7 `HasXxxError` booleans â€” real-time `AddError`/`ClearErrors`
- **CustomerEditorViewModel**: Removed 3 `HasXxxError` booleans â€” real-time `AddError`/`ClearErrors`

### New Rules (AGENTS.md)
| Rule | Description |
|------|-------------|
| RULE-227 | `SetDialogService()` in every Editor VM constructor |
| RULE-228 | Use `INotifyDataErrorInfo` (`AddError/ClearErrors`) â€” no `HasXxxError` booleans |
| RULE-229 | Pre-save validation: `ClearAllErrors()` â†’ `AddError()` â†’ `await ValidateAllAsync()` |
| RULE-230 | ErrorTemplate: red border + â‌— icon with ToolTip bound to `[0].ErrorContent` |

### File Impact
| Layer | Files |
|-------|-------|
| UI (XAML) | `Resources/Styles.xaml` â€” new ErrorTemplate + HasError triggers |
| ViewModels | `ViewModelBase.cs` â€” SetDialogService, ValidateAllAsync, ValidateField |
| Editor VMs | 14 files â€” SetDialogService() in constructors |
| Refactored VMs | ProductEditor, CustomerEditor â€” removed HasXxxError pattern |
| Documentation | AGENTS.md, README.md, 5 subagent files, CHANGELOG.md, MASTER-PLAN.md, CONSTITUTION.md |

---

## âœ… Architecture Alignment & Code Quality Remediation (v4.6.3) â€” Historical

**Goal**: Align Costing settings with Clean Architecture boundaries (moving to `ISettingsApiService` via HTTP Client), resolve ViewModel compiler shadowing (CS0108 warnings), wrap async void operations in ViewModels with safe try-catches, and correct garbled Arabic text.

### Key Changes
- **Costing Settings Refactor**: Migrated `CostingMethodSettingsViewModel` from repository calling to HTTP setting API client calls. Registered the VM inside `App.xaml.cs`.
- **WPF VM Quality Standard**: Avoided CS0108 by calling base class property setting helpers and calling `SetDialogService()`. Safe try-catch wrappers for async void initialization workflows.
- **RTL Arabic Corrections**: Rectified Mojibake in transfers/payments ViewModels.

---

## âœ… Security Hardening & Code Quality (v4.6.4) â€” Historical

**Goal**: Harden security with rate limiting, protect user integrity, secure connection strings, enhance validation, fix build warnings.

### Key Changes
- **Rate Limiting**: Added `AddRateLimiter` with `LoginPolicy` (5 attempts per 15 min per IP) and global policy (100 req/min). Arabic 429 response with `RATE_LIMIT_EXCEEDED` code.
- **User Hard-Delete Guarded**: `UserService.PermanentDeleteAsync()` returns `Result.Failure("ظ„ط§ ظٹظ…ظƒظ† ط­ط°ظپ ط§ظ„ظ…ط³طھط®ط¯ظ…ظٹظ† ط¨ط´ظƒظ„ ظ†ظ‡ط§ط¦ظٹ")` â€” enforces RULE-038.
- **Connection String Security**: Removed plaintext connection string from `appsettings.Development.json`. Uses `SALESSYSTEM_DB_CONNECTION` env var only.
- **FluentValidator Enhancements**: Enhanced all 7 invoice/payment/transfer validators with date, enum, and max-length rules.
- **FallbackErrorDialog**: Added `FallbackErrorDialog.xaml` for thread-safe unhandled exception display.
- **Build Fixes**: Resolved 10 CS0109 warnings and 4 CS1540 errors across 8 ViewModels.
- **Test Coverage**: 5 new tests + 50 test files compiling.

### Key Rules
- RULE-240 through RULE-248 (Rate Limiting, User Hard-Delete, Connection String Security)

### Verification
- [ ] `dotnet build` â€” 0 errors, 0 warnings
- [ ] Login endpoint rate-limited (5/15min)
- [ ] User permanent delete returns failure
- [ ] No plaintext connection strings
- [ ] Security-Plan.md reflects implementation status

---


<!-- END OF SECTION: 14.2 Advanced Security & Quality Hardening (Historical) -->


<!-- START OF SECTION: 14.3 Technical Debt & CQRS Roadmap -->

> [!NOTE]
> MediatR/CQRS is NOT used. Service Layer pattern is the standard. This section documents the decision and any remaining cleanup.

## âœ… MediatR Removed (RULE-148)

- **Package:** MediatR v12.x **HAS BEEN REMOVED** from `SalesSystem.Application`
- **All references replaced** with direct Service Interface injection
- **No Commands/Queries directories** exist â€” Service Layer pattern is the standard
- **No MediatR pipeline behaviors** â€” never used
- **Status:** Fully removed per AGENTS.md RULE-147/148

## CQRS â€” NOT Adopted

- **AGENTS.md RULE-043** updated: "Service Layer pattern is the standard â€” NOT CQRS/MediatR"
- **Service Layer pattern used**: Services handle both reads and writes
- **Status:** Documented and final â€” no migration planned
- **Why:** Service Layer provides sufficient separation at this scale without ceremony

---

## ًں“‹ Future Plans (NOT Implemented)

These are documented in AGENTS.md or discussed but **have zero code in the codebase**:

| Feature | Status | Notes |
|---------|--------|-------|
| **MAUI Mobile App** | â‌Œ Not started | `Presentation.MAUI` directory never created |
| **SharedKernel project** | â‌Œ Not started | Architecture uses layered, not shared kernel |
| **DesignTokens.cs** | â‌Œ Not created | Styles live in `Resources/Styles.xaml` |
| **Roslyn Analyzer** | â‌Œ Not created | No `HardcodedColorAnalyzer` or similar |
| **ExecuteAsync() wrapper** | âœ… In ViewModelBase | RULE-141: All async commands use `ExecuteAsync()` wrapper |
| **Vertical Slices** | â‌Œ Not adopted | Layered architecture is the standard |
| **Camera-based barcode** | â‌Œ Not started | Only USB/HID keyboard scanner implemented |
| **BarcodeScanViewModel** | â‌Œ Not created | Barcode handled via `IBarcodeInputService` event |
| **BaseViewModel in SharedKernel** | â‌Œ Not created | ViewModelBase lives in DesktopPWF |

---

## ًں—‘ï¸ڈ Legacy Code

### `Legacy/SalesSystem.Desktop/`

- **What it is:** Abandoned WinForms desktop application
- **Status:** NOT in solution file, NOT compiled, NOT referenced
- **Safe to delete:** Yes â€” all functionality has been rebuilt in `DesktopPWF` (WPF)
- **Why abandoned:** WinForms couldn't support the modern MVVM + EventBus + styled dialog architecture
- **Recommendation:** Delete when convenient â€” it's dead weight

---


<!-- END OF SECTION: 14.3 Technical Debt & CQRS Roadmap -->


<!-- START OF SECTION: 14.4 Architectural Design Decisions -->

> [!NOTE]
> Detailed rationale explaining core architectural choices (Service Layer, WPF, Rich Domain, Result Pattern).

## ًں“گ Architecture Decisions

### Why Service Layer over CQRS/MediatR?

- The application has ~20 aggregate roots, not 200+ â€” Service Layer is simpler and sufficient
- CQRS adds ceremony (Command/Query classes, handlers, validators) without proportional benefit at this scale
- Service Layer is easier for junior developers to understand and maintain
- Can migrate to CQRS later if complexity demands it

### Why DesktopPWF (WPF) over WinForms?

- WPF supports MVVM pattern with data binding
- XAML enables centralized styling (`Styles.xaml`)
- Better support for modern UI (animations, templates, resources)
- EventBus integration works naturally with WPF's dispatcher
- WinForms required code-behind logic â€” violated separation of concerns

### Why Layered over Vertical Slices?

- Small team (2-3 developers) â€” layered is easier to navigate
- Clear separation of concerns: Domain â†’ Application â†’ Infrastructure â†’ API â†’ Desktop
- Each layer has a single responsibility and single dependency direction
- Vertical slices work better for large teams with many independent features

### Why NOT MAUI?

- Target users are desktop-only (retail shops with POS terminals)
- Mobile would require entirely different UX (touch-optimized, offline-first)
- API already provides mobile-ready endpoints â€” MAUI can be added later
- Focus on perfecting desktop first

### Why Result<T> over Exceptions?

- Exceptions are for exceptional conditions â€” validation failures are expected
- Result<T> makes error handling explicit and type-safe
- Controllers can cleanly map Result to HTTP status codes
- Avoids try/catch boilerplate in every service method

### Why Rich Domain Model?

- Entities own their business rules â€” can't be bypassed from outside
- `private set` prevents accidental state corruption
- Factory methods enforce invariants at creation time
- Guard clauses catch invalid states early with clear Arabic messages

---


<!-- END OF SECTION: 14.4 Architectural Design Decisions -->


<!-- START OF SECTION: 14.5 Build Quality Hardening (v4.10) -->

> [!NOTE]
> Recent session (2026-06-14) achieving 0 errors, 0 warnings across all 6 production projects with CashBoxService implementation, PDF export for 27 reports, multi-currency in Sales, and infrastructure hardening.

## ✅ Build Quality Hardening â€” 0 Errors, 0 Warnings

All six production projects (Domain, Contracts, Application, Infrastructure, API, DesktopPWF) now build with **zero errors and zero warnings** across all target frameworks.

### CashBoxService â€” Full Implementation
- **800-line `ICashBoxService`** implementation with CRUD, voucher lifecycle, journal entry integration
- Auto-creates Level-4 sub-account under `"1110 â€” Ø§Ù„Ù†Ù‚Ø¯ÙŠØ©"` parent when `AccountId` is null
- Uses `IPaymentVoucherService`/`IReceiptVoucherService` for voucher creation (no duplication)
- All 13 interface methods return `Result<T>` with proper Arabic error messages
- Registered in API `Program.cs` DI container
- No more `AggregateException` on API startup

### Warnings Eliminated (34 total)
| Project | Warnings Fixed |
|---------|---------------|
| **Application** (15) | CS8602 null dereferences in AnnualClosingService, JournalEntryService, PurchaseReportService, SalesReportService, FinancialReportService; CS0105 duplicate using in IPrintDataService; CS0219 unused variable in MinStockAlertWorker; CS8604 null param in StoreSettingsService |
| **Infrastructure** (3) | CS8602 in ReportRepository.cs (ThenInclude null-forgiving + variable reuse) |
| **DesktopPWF** (16) | CS0105 duplicate usings (ProductImportApiService, ProductImportViewModel); CS8601 null assignment (PrintDtoExtensions, WarehouseTransferEditorViewModel); CS0618 obsolete member (ProductUnitRowViewModel â€” #pragma suppression) |

### PDF Export â€” First Choice for All 27 Reports
- **QuestPDF** RTL tables with Arabic column headers, alternating row colors, page numbers
- Date filter header shown on all report PDFs
- Pattern A (`AsyncRelayCommand`) or B (`RelayCommand`) `ExportPdfCommand` + `ExportPdfAsync()` in every report ViewModel
- XAML buttons added to all 27 report Views â€” consistent with Excel export

### Multi-Currency Wired in Sales
- `SalesInvoiceEditorViewModel`: Added `Currencies` ObservableCollection, `SelectedCurrencyId`, `ExchangeRate`, `IsForeignCurrency` properties
- Currencies loaded from API reference data on initialization
- `CurrencyId` passed to `BuildRequest()` / `BuildUpdateRequest()` â€” not `null`
- XAML: Currency ComboBox + ExchangeRate TextBox (visible only when `IsForeignCurrency`)

### CompanySettings Type Consistency
- `DefaultCurrencyId` changed from `int` to `short` across ALL layers:
  - Domain entity (`CompanySettings.cs`)
  - EF config (`.HasColumnType("smallint")`)
  - Migration files (20260613014517_InitialCreate + Designer + Snapshot)
  - DTO (`AllDtos.cs`), Request (`MiscRequests.cs`)
  - API Validator, Desktop ViewModel
- Prevents runtime type mismatch between `short` PK in `Currencies` table and `int` FK reference

### Purchase Order Navigation Crash Fix
- `MainViewModel` now initializes `NavigateToPurchaseOrdersCommand` as a `RelayCommand`
- Shows Arabic info dialog `"Ø´Ø§Ø´Ø© Ø£ÙˆØ§Ù…Ø± Ø§Ù„Ø´Ø±Ø§Ø¡ ØªØ­Øª Ø§Ù„ØªØ·ÙˆÙŠØ±"` instead of crashing on stale nav binding
- Prevents `NullReferenceException` when clicking sidebar Purchase Orders button

### Verification
- [ ] `dotnet build` â€” 0 errors, 0 warnings across all 6 production projects
- [ ] API starts without DI exceptions (`CashBoxService` resolves correctly)
- [ ] All 34 warnings (15 Application + 3 Infrastructure + 16 Desktop) eliminated
- [ ] No regression in existing functionality (build-only changes to warnings)

---


<!-- START OF SECTION: 15. System Version & Evolution History -->

> [!NOTE]
> Consolidated version history listing versions from inception to current production-hardened v4.10 release.

## ًں“‌ Version History

| Version | Date | Description |
|---------|------|-------------|
| **v4.10** | **2026-06-14** | **Build Quality Hardening & Feature Completion** â€” 0 errors / 0 warnings, CashBoxService full impl, PDF export for 27 reports, multi-currency in Sales, CompanySettings type fix, Purchase Order nav crash fix |
| **v4.6.4** | **2026-05-23** | **Security Hardening & Code Quality** â€” Rate limiting, user hard-delete guard, connection string security, FluentValidator enhancements, FallbackErrorDialog, build warning fixes |

| Version | Date | Description |
|---------|------|-------------|
| v4.6.3 | 2026-05-23 | Architecture Alignment & Code Quality â€” Costing settings HTTP refactoring, VM DI registration, CS0108 member hiding resolutions, async void try-catch safety, RTL Arabic corrections |
| v4.6.2 | 2026-05-23 | WPF Validation ErrorTemplate â€” Red border + â‌— icon ErrorTemplate, INotifyDataErrorInfo standardization, ValidateAllAsync() base method, 14 Editor VMs updated |
| v4.6.1 | 2026-05-23 | UI Sorting & Dialog Safety â€” Newest-first sorting, DatabaseErrorDialog self-owner fix, comprehensive audit |
| v4.6 | 2026-05-22 | Audit & Polish â€” LogSystemError centralized, Dialog overlay, ValidationErrorsDialog, auto-focus, hard-delete safety, login/settings fixes |
| v4.5.3 | 2026-05-22 | Identifier Strategy Complete â€” Code removal (Product, Customer, Supplier, Warehouse) â€” all entities use auto-increment Id |
| v4.5.2 | 2026-05-22 | Identifier Strategy â€” Code removal (Product/Customer/Supplier) |
| v4.5.1 | 2026-05-22 | Error & Shutdown improvements â€” Error message overhaul, Arabic-friendly errors, MessageBox elimination |
| v4.5 | 2026-05-21 | Multi-Window Screen Management â€” Non-modal editors, ScreenWindowService |
| v4.4 | 2026-05-21 | Production release â€” Auto-Update, DPAPI, Backup, Windows Service, Installer |
| v4.3 | 2026-05-15 | Print engine (QuestPDF + Win32), Dynamic UOM, Costing strategy, Cash boxes, Price history |
| v4.2 | 2026-05-10 | Delete strategy, Defensive programming, WPF dialogs, Toast notifications, Real-time validation |
| v4.1 | 2026-05-05 | Wholesale/Retail pricing, Unit conversion in Domain |
| v4.0 | 2026-05-01 | Clean Architecture rewrite â€” 6 projects, Service Layer, Result<T> |
| v3.0 | 2026-04-15 | Initial architecture â€” PRD-MVP |


<!-- END OF SECTION: 15. System Version & Evolution History -->


<!-- START OF SECTION: 16. AI Agent Core Guidelines & Rules -->

> [!NOTE]
> Rules that AI agents must strictly follow when writing code or applying database changes to the codebase.

15. طھط¹ظ„ظٹظ…ط§طھ ظ„ظ„ظ€ AI Agent
text

âڑ ï¸ڈ طھط¹ظ„ظٹظ…ط§طھ ط¥ظ„ط²ط§ظ…ظٹط© ظ„ظƒظ„ ط·ظ„ط¨ ط¨ط±ظ…ط¬ظٹ:

1. DECIMAL ONLY
   - ط¬ظ…ظٹط¹ ط§ظ„ط­ط³ط§ط¨ط§طھ ط§ظ„ظ…ط§ظ„ظٹط©: decimal ظˆظ„ظٹط³ float/double
   - ط£ظ…ط«ظ„ط©: decimal total = items.Sum(i => i.LineTotal);

2. TRANSACTIONS ALWAYS
   - ظƒظ„ ط¹ظ…ظ„ظٹط© طھط¤ط«ط± ط¹ظ„ظ‰ ط£ظƒط«ط± ظ…ظ† ط¬ط¯ظˆظ„: ط¯ط§ط®ظ„ BeginTransactionAsync
   - ط¹ظ†ط¯ ط£ظٹ ط§ط³طھط«ظ†ط§ط،: RollbackAsync

3. RESULT PATTERN
   - ظƒظ„ Service Method طھط±ط¬ط¹: Result<T> ط£ظˆ Result
   - ظ„ط§ ط±ظ…ظٹ Exception ظ„ظ„ظ€ Controllers ظ…ط¨ط§ط´ط±ط©

4. DOMAIN VALIDATION
   - ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ط§ظ„ظ…ظ†ط·ظ‚ ظپظٹ Domain Entities
   - ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ط§ظ„ط¨ظٹط§ظ†ط§طھ ظپظٹ Application Services
   - ط§ظ„ظ€ Controller ظٹظڈط¹ظٹط¯ HTTP Response ظپظ‚ط·

5. STOCK INTEGRITY
   - طھط­ظ‚ظ‚ ظ…ظ† ط§ظ„ظ…ط®ط²ظˆظ† ظ‚ط¨ظ„ Transaction
   - ط§ط®طµظ… ط¨ط¹ط¯ ط­ظپط¸ ط§ظ„ظپط§طھظˆط±ط©
   - ط³ط¬ظ„ ط¯ط§ط¦ظ…ط§ظ‹ ظپظٹ InventoryMovements

6. NO DIRECT DB
   - Desktop ظ„ط§ ظٹطµظ„ ظ„ظ€ DB ظ…ط¨ط§ط´ط±ط©
   - ظƒظ„ ط·ظ„ط¨ ط¹ط¨ط± HttpClient â†’ API

7. BALANCE DIRECTION
   - Customer.IncreaseBalance() ط¹ظ†ط¯ ط§ظ„ط¨ظٹط¹ ط§ظ„ط¢ط¬ظ„
   - Customer.DecreaseBalance() ط¹ظ†ط¯ ط§ظ„ط¯ظپط¹ ط£ظˆ ظ…ط±طھط¬ط¹ ط§ظ„ط¨ظٹط¹
   - Supplier.IncreaseBalance() ط¹ظ†ط¯ ط§ظ„ط´ط±ط§ط، ط§ظ„ط¢ط¬ظ„
   - Supplier.DecreaseBalance() ط¹ظ†ط¯ ط¯ظپط¹ظ†ط§ ظ„ظ„ظ…ظˆط±ط¯

<!-- END OF SECTION: 16. AI Agent Core Guidelines & Rules -->
