<h1 align="center">🧾 Sales Management System</h1>

<p align="center">
  <strong>A comprehensive sales management platform for small-to-medium retail businesses</strong><br/>
  <em>Desktop Client + RESTful API Backend — Built with Clean Architecture</em>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10%20LTS-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/WPF-MVVM-0078D4?style=for-the-badge&logo=windows&logoColor=white" alt="WPF"/>
  <img src="https://img.shields.io/badge/SQL%20Server-2019+-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server"/>
  <img src="https://img.shields.io/badge/Architecture-Clean-2ECC71?style=for-the-badge" alt="Clean Architecture"/>
  <img src="https://img.shields.io/badge/API-ASP.NET%20Core%2010-512BD4?style=for-the-badge" alt="ASP.NET Core"/>
<img src="https://img.shields.io/badge/Status-v4.6.9%2B%20Complete-2ECC71?style=for-the-badge" 
alt="Status"/>

</p>

<p align="center">
  <img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License"/>
  <img src="https://img.shields.io/badge/Version-v4.6.9%2B-blue.svg?style=flat-square" alt="Version"/>
  <img src="https://img.shields.io/badge/Language-Arabic%20%2B%20English-orange.svg?style=flat-square" alt="Language"/>
</p>

---

## 📋 Overview

A **comprehensive sales management platform** built with Clean Architecture + Service Layer pattern, designed for small-to-medium retail businesses. The current version delivers a **WPF Desktop client (MVVM)** backed by an **ASP.NET Core Web API**, handling sales, purchases, inventory, returns, and financial tracking with full Arabic/English bilingual support.

The API-first architecture is designed to support **future web and mobile clients** without any backend changes.

### Why This Project?

- 🏪 **Purpose-built for retail** — Sales invoices, purchase orders, returns, and stock transfers
- 🔄 **Dynamic Unit of Measure** — Each product has multiple units (Piece, Box, Carton) with configurable conversion factors
  - Per-unit pricing: separate `RetailPrice` and `WholesalePrice` per `ProductUnit`
  - Barcodes stored in `UnitBarcode` table — one barcode per unit
  - `SmartUnitFormatter` selects best display unit based on quantity
  - Base unit always has `ConversionFactor = 1`
- 🔍 **Multi-barcode support** — Barcodes stored per product-unit combination in `UnitBarcode` table
  - Auto-detect unit from scanned barcode
  - Fallback to legacy `Products.Barcode` column
- 💰 **Costing Strategy** — Three costing methods: WeightedAverage, LastPurchasePrice, SupplierPrice
  - Configurable via `SystemSettings` table (seeded as WeightedAverage)
  - `UpdateProductPricingService` handles all three methods
  - Cost cascade: ALL product units recalculate from base unit cost × conversion factor
  - `ProductPriceHistory` audit trail on EVERY cost change
- 🏦 **Cash Box Management** — Track physical cash across multiple boxes
  - `CashBox` with opening/current balance
  - `CashTransaction` immutable entries (Opening, Income, Expense, Transfer, Refund, Payment)
  - `CashBox.CurrentBalance` NEVER negative — validated before dispensing
  - `DailyClosure` for end-of-day reconciliation
- 🔌 **Hardware Integration** — Built-in barcode scanner support (Keyboard Emulation) with mobile camera scanning planned
- 🔒 **Financial integrity** — All money calculations use `decimal` precision, never floating-point
- 📦 **Multi-warehouse & Low Stock AI** — Track inventory across branches with auto-calculated reorder suggestions
  - `ReorderLevel` on each product
  - Smart low-stock reporting (e.g., "1 box + 3 pieces")
- 📒 **Accounting Foundation (Phases 18-31)**: Chart of Accounts (60 accounts), Journal Entries, Fiscal Years, Annual Closing
- 💱 **Multi-Currency (Phase 20)**: YER/USD/SAR support with exchange rates, FractionName, IsSystem guard
- 👤 **Users & Permissions (Phase 21)**: 4-role model (Admin/Accountant/Cashier/Observer), 33 permission codes, MustChangePassword, lockout
- 📦 **FIFO/FEFO Batch Tracking (Phases 25/27/28)**: PurchaseLot entity with FIFO cost allocation, expiry-based FEFO deduction
- 🧾 **Tax Module (Phase 19)** — Full tax management with `Tax` entity (name, rate, type percentage/fixed, `IsDefault`)
  - `ITaxService` with CRUD operations, `TaxesController` API endpoints
  - WPF Desktop UI: `TaxesListView` + `TaxEditorView` with `INotifyDataErrorInfo` validation
  - `TaxId` FK on `SalesInvoice` and `PurchaseInvoice` with `DeleteBehavior.Restrict`
  - `SetTax()` domain method on invoice entities
  - `IsDefault` flag (soft-delete-safe filtered unique index `AND [IsActive] = 1`)
- ⚙️ **System Settings with Memory Caching** — `IMemoryCache` + `ConcurrentDictionary` key tracker for `SystemSetting` lookups
  - 5-minute sliding expiration with `PostEvictionCallback` for automatic cache invalidation
  - `ISystemSettingsRepository` caching layer — `GetAllCachedAsync()` returns from cache or DB
  - `SetStringAsync()`, `SetIntAsync()`, `SetBoolAsync()`, `SetDecimalAsync()` typed helpers with `category` parameter
- 📋 **Store Settings with Deprecation Strategy** — Company information single-row entity
  - Deprecated fields (`DefaultTaxRate`, `IsTaxEnabled`, `InvoicePrefix`, `CurrencyCode`) marked with comments — Tax entity is source of truth
  - `StoreSettings.Update()` calls `UpdateTimestamp()` per RULE-292
- 🔢 **InvoiceNo Strategy (v4.6.7)**: InvoiceNo = int, UNIQUE per document type, thread-safe auto-generation via DocumentSequenceService.GetNextIntAsync() with SemaphoreSlim lock
- 💱 **Currency Module Stabilization (v4.6.8)**: Removed manual transactions conflicting with SqlServerRetryingExecutionStrategy, ExchangeRate precision (18,2) on payments, fixed JournalEntryLine shadow FK, standardized editor validation pattern
- 📑 **Reports (Phase 31)**: 35+ report DTOs, Hierarchical Income Statement + Balance Sheet, Excel export via ClosedXML
- 👥 **Role-based access** — Admin, Manager, and Cashier with granular permissions
- 📊 **Full audit trail** — Every stock change, price change, and financial transaction is tracked
- 🌐 **API-first design** — RESTful API ready for web/mobile clients in future phases

---

## 🏗️ Architecture

The system follows **Clean Architecture** with a strict 6-project solution structure:

```
SalesSystem/
├── 📦 SalesSystem.Contracts/       ← DTOs, Requests, Responses, Result<T>
├── 🏛️ SalesSystem.Domain/          ← Entities, Business Rules, Exceptions
├── ⚙️ SalesSystem.Application/     ← Services, Interfaces, Use Cases
├── 🗄️ SalesSystem.Infrastructure/  ← EF Core, DbContext, Repositories
├── 🌐 SalesSystem.Api/             ← Controllers, Validation, Middleware
└── 🖥️ SalesSystem.DesktopPWF/      ← WPF UI, MVVM, EventBus
```

### Data Flow

```
Desktop → (HttpClient) → API → Application → Infrastructure → SQL Server
                                     ↓
                              Domain (ZERO dependencies)
```

> **Key Principle:** The Desktop app **never** connects to the database directly. All communication goes through the Web API.

### New Services (v4.4)
- `IUpdaterService` / `UpdaterService` / `GitHubUpdaterService` — Auto-update with SHA256 verification
- `IConnectionStringProtector` / `ConnectionStringProtector` — DPAPI encryption for connection strings
- `FirstRunSetupService` — Auto-encrypts plaintext connection string on first run
- `SecureDbContextFactory` — Decrypts connection string before creating DbContext
- `ScheduledBackupWorker` — BackgroundService for daily 2:00 AM backups
- `SecurityAudit` — DEBUG-only pre-build security checks
- `AdminOnlyViewModel` — Base class enforcing Admin role for admin screens
- `UpdateDialogViewModel` — WPF update dialog with IDisposable (dispose CTS)

### New Services (v4.3)
- `UpdateProductPricingService` — WeightedAverage / LastPurchasePrice / SupplierPrice costing
- `BarcodeLookupService` — Unit-aware barcode scanning
- `SmartUnitFormatter` — Best-display-unit selection based on quantity
- `SystemSettingsRepository` — Application-level settings
- `StoreSettingsService` — Store configuration management
- `BackupService` — Database backup automation
- `ProductPriceQuery` — Price history audit trail
- `DialogService` — Modal dialogs (Error, Success, Warning, Confirm, Delete)
- `ToastNotificationService` — Auto-dismissing notifications
- **`InvoicePrintDtoBuilder`** — Builds print DTOs for Sales, Purchase, SalesReturn, PurchaseReturn invoices
- **`IPrintService` / `PrintService`** — QuestPDF A4 generation + Win32 raw ESC/POS thermal printing
- **`PrintApiService`** (Desktop) — HTTP client wrapper for `PrintController` endpoints
- **`A4InvoiceDocument`** — QuestPDF document template (RTL Arabic, logo, tax breakdown)
- **`ThermalReceiptGenerator`** — ESC/POS receipt builder (42-char columns, Windows-1256)
- **`EscPos`** — Static class for ESC/POS command byte sequences

### New Services (v4.6.9)
- `ITaxService` — Full CRUD for Tax entity (name, rate, type, IsDefault) returning `Result<T>` and `Result`
- `TaxesController` — 6 API endpoints (GET all, GET by id, POST create, PUT update, DELETE soft, DELETE permanent) with `AdminOnly` policy
- `TaxesListViewModel` — WPF list view with newest-first sorting, toggle active, `IDisposable` for EventBus
- `TaxEditorViewModel` — WPF editor with `INotifyDataErrorInfo` validation, `ValidateAsync()` base method, `IToastNotificationService` for success feedback
- `CreateTaxRequestValidator` / `UpdateTaxRequestValidator` — FluentValidation with name required, rate > 0, type in enum
- `SystemSettingsCachingService` — `IMemoryCache` with `ConcurrentDictionary` key tracker, 5-minute sliding expiration, `PostEvictionCallback` auto-cleanup
- `SystemSettingsRepository.SetStringAsync(category, key, value)` — Typed helpers with auto-create and category support
- `TaxSeeder` — Seeds default tax (VAT 15%) and `IsDefault` flag as part of `DbSeeder`
- `SystemSettingsViewModel` — ViewModel with 32 strongly-typed settings across 8 categories (Inventory, Sales, Purchases, Barcode, Print, Notifications, Accounting, General) — added 14 missing properties for Sales/Purchases/Print/Notifications
- `StoreSettingsService.ValidateSystemSettings()` — Validates 19 boolean keys and 6 integer keys with range validation before saving batch updates
- `Tax.SetDefault()` / `Tax.ClearDefault()` — Domain methods for cleaner default-tax management with audit trail (`UpdateTimestamp()`)

### New Services (v4.6.7)
- `IDocumentSequenceService.GetNextIntAsync()` — Thread-safe int sequence generation via SemaphoreSlim for InvoiceNo and other int sequences
- `SystemAccountMappingsService` — Maps 7 system operation types to Chart of Accounts
- `JournalEntryAutoService` — 7 auto-journal entry providers (Sales, Purchases, Returns, Payments, Receipts, Transfers, Annual Closing)
- `FiscalYearService` — Fiscal year management with close validation

---

## ✨ Features

### 💰 Sales & Purchases
- Sales invoices with multi-item support
- Purchase invoices with supplier management
- Mixed payment types (Cash / Credit / Mixed)
- Invoice lifecycle: Draft → Posted → Cancelled

### 📦 Inventory Management
- Multi-warehouse stock tracking
- Stock transfers between warehouses
- Real-time stock availability checking
- Negative stock prevention (DB-level CHECK constraints)
- Complete movement audit trail (`InventoryMovements`)

### 🔄 Returns
- Sales returns with original invoice reference
- Purchase returns with supplier tracking
- Automatic stock reversal on return posting

### 👥 User Management
- Three roles: **Admin**, **Manager**, **Cashier**
- JWT Bearer authentication
- Role-based API policies (`AdminOnly`, `ManagerAndAbove`, `AllStaff`)
- BCrypt password hashing (work factor: 12)

### 📊 Reports
- Daily sales reports
- Stock level reports
- Customer debt tracking
- Supplier payment tracking

### 🖨️ Printing Engine (v4.3)
- **A4 invoice PDF generation** via QuestPDF with RTL Arabic support
  - Store logo, name, phone, address, tax number in header
  - Alternating-row item table with line totals
  - Tax breakdown (VAT rate configurable in SystemSettings)
  - Page numbers and footer
- **80mm thermal receipt printing** via Win32 raw ESC/POS
  - 42-character monospaced column layout
  - Windows-1256 encoding for Arabic text
  - Cutter command, cash drawer kick-out
  - Built-in `EscPos` static class (no external packages)
- **Preview** in WPF `PdfPreviewWindow` (WebBrowser control) before printing
- **API-first architecture**: Desktop → `IPrintApiService` → `PrintController` → `IPrintService`
- **Print settings persisted** in `SystemSetting` table (`Category = "Print"`):
  - `ThermalPrinterName`, `A4PrinterName`, `LogoPath`, `StoreTaxNumber`, `TaxRate`
- **Test page** for printer alignment verification
- **11 API endpoints** covering sales/purchase/return A4 + thermal + preview + save
- **254+ unit tests** across Domain, Application, Infrastructure, and API layers

### 📐 Dynamic Unit of Measure (v4.3)
- Multiple units per product (Piece, Box, Carton, etc.)
- Per-unit pricing (RetailPrice + WholesalePrice per ProductUnit)
- Conversion factors (Base=1, Box=24, Carton=144)
- Unit-specific barcodes via `UnitBarcode` table
- `SmartUnitFormatter` for quantity-based display unit selection
- Enforced: at least one unit per product (Domain rule)

### 💰 Costing Strategy (v4.3)
- Three methods: WeightedAverage, LastPurchasePrice, SupplierPrice
- Configurable via `SystemSettings` table
- Cost cascade: ALL product units update from base × conversion factor
- `ProductPriceHistory` audit on every change
- Seeded as WeightedAverage by default

### 🏦 Cash Box Management (v4.3)
- Multiple cash boxes with opening/current balance
- Immutable cash transactions (Open, Income, Expense, Transfer, Refund, Payment)
- `CashBox.CurrentBalance` never negative
- Cash transfers require TWO transactions (Out + In)
- `DailyClosure` for end-of-day reconciliation

### 📒 Chart of Accounts — Phase 22 (v4.6.9+)
- **60-Account Hierarchy** (4 levels): Group → Main → Sub → Detail
  - 5 Level 1 Groups: Assets, Liabilities, Equity, Income, Expenses
  - 8 Level 2 Main: Current Assets, Fixed Assets, Current Liabilities, Equity, Revenue, COGS, Operating Expenses, Other Expenses
  - 20 Level 3 Sub: 27 Level 4 Detail accounts
  - System accounts (L1-L2) protected by `IsSystem` — never deleted or edited
  - Leaf accounts (L4) allow transactions; parent accounts (L1-L3) block transactions
  - Color-coded per account type (#2196F3 Assets, #F44336 Liabilities, #4CAF50 Equity/Revenue, #FF9800 Expenses)
  - `CHK_Account_Level_Range [Level] >= 1 AND [Level] <= 10` CHECK constraint
  - `SetAsBaseCurrency()`/`UnsetBaseCurrency()` domain methods for clean service code
- **Two-Pass Seeder**: Creates parent accounts first (L1→L2) with SaveChanges, queries IDs, then creates children (L3→L4)
- **SystemAccountMappings Updated**: Maps 13 system account codes (Cash, Bank, AR, AP, VAT, Capital, Sales, COGS, etc.)
- **Dual-Mode Desktop UI**: TreeView (HierarchicalDataTemplate) + DataGrid flat view toggle
- **Full CRUD API**: 7 endpoints with proper policies — reads AllStaff, writes ManagerAndAbove, permanent delete AdminOnly

### 📒 Accounting Foundation (v4.6.7)
- **Chart of Accounts** — 60-account hierarchical structure (Assets, Liabilities, Equity, Income, Expenses)
  - System accounts protected by `IsSystem` guard — never deleted or edited
- **Journal Entries** — Automatic journal entry creation for ALL financial transactions
  - 7 auto-providers: Sales, Purchases, Returns, Payments, Receipts, Transfers, Annual Closing
  - `JournalEntryAutoService` handles all providers centrally
- **Fiscal Years** — Annual fiscal periods with open/close lifecycle
  - `FiscalYearService` manages year creation, validation, and closing
  - Closing entries automatically transfer revenue/expense to retained earnings
- **Multi-Currency** — YER/USD/SAR support with exchange rate tables
  - `FractionName` for sub-currency units (Fils, Cent)
  - `IsSystem` guard on base currency
- **Account Mappings** — `SystemAccountMappingsService` maps 7 operation types to COA accounts
  - Configurable per business for tax/custom rules

### ⚙️ Settings Module (Phase 19)
- **System Settings** — Key-value configuration store (`SystemSetting` entity) for all system-level settings across 8 categories:
  - **Inventory** (4): LowStockThreshold, DefaultWarehouseId, EnableNegativeStock, StockAlertEnabled
  - **Sales** (8): DefaultCustomerId, DefaultPaymentType, DefaultSaleMode, EnableDiscount, MaxDiscountPercent, EnableTax, TaxInclusivePricing, RequireCustomerForSale
  - **Purchases** (3): DefaultSupplierId, AutoGenPO, RequireApproval
  - **Barcode** (3): BarcodeSymbology, BarcodeWidth, BarcodeHeight
  - **Accounting** (1): DefaultCostCenterId
  - **Print** (5): ThermalPrinterName, A4PrinterName, LogoPath, ReceiptHeader, ReceiptFooter
  - **Notifications** (4): LowStockNotification, DailyReportNotification, BackupNotification, ErrorNotification
  - **General** (3): AppLanguage, DateFormat, CurrencyDisplay
- **Memory Caching** — `IMemoryCache` with `ConcurrentDictionary` key tracker for SystemSettings (5-minute sliding expiration)
  - `PostEvictionCallback` auto-removes tracker key on eviction
  - `ISystemSettingsRepository.GetAllCachedAsync()` returns from cache or DB with single DB round-trip
- **Costing Method** — Three costing methods configurable via SystemSettings: WeightedAverage (1), LastPurchasePrice (2), SupplierPrice (3)
  - RadioButton group in Settings UI with Arabic explanations
  - Persisted via API to `SystemSettings` key `"CostingMethod"` of type `int`
- **Store Settings** — Company information single-row entity (name, phone, address, email, tax number, signature path)
  - `StoreSettingsService` for get/update operations
  - `StoreSettings.Update()` calls `UpdateTimestamp()` per RULE-292
  - **Deprecated fields**: `DefaultTaxRate`, `IsTaxEnabled`, `InvoicePrefix`, `CurrencyCode` — marked with `// Deprecated` comments; Tax entity is source of truth
- **Tax Module** — Full tax management with `Tax` entity (name, rate, type: percentage/fixed, `IsDefault`)
  - `ITaxService` with CRUD operations returning `Result<T>` and `Result`
  - `TaxesController` API endpoints with FluentValidation (`CreateTaxRequestValidator`, `UpdateTaxRequestValidator`)
  - WPF Desktop UI: `TaxesListView` (newest-first sorting) + `TaxEditorView` (INotifyDataErrorInfo validation)
  - `IsDefault` flag with `DeleteBehavior.Restrict` FK from invoices; filtered unique index `WHERE [IsActive] = 1`
- **Tax on Invoices** — `TaxId` FK (nullable int) on `SalesInvoice` and `PurchaseInvoice`
  - `DeleteBehavior.Restrict` — cannot delete a tax used by invoices
  - `SetTax(int? taxId)` domain method on both invoice entities
  - `GetTaxAmount(decimal subTotal)` computes tax based on Tax.Rate and Tax.Type (percentage/fixed)
- **Print Settings** — Thermal printer name, A4 printer name, logo path (optional — graceful null handling), receipt header/footer text, ESC/POS code page, auto-print on post flag — stored with `Category = "Print"`
- **Backup Settings** — Backup path, scheduled time, retention days, update server URL — stored as SystemSettings
- **29 System Settings seeded** across 8 categories via `DbSeeder`
- **Admin-only access** — All settings/taxes endpoints restricted to `[Authorize(Policy = "AdminOnly")]`

### 💱 Currencies Module (Phase 20)
- **Currency Entity** — Name, ISO code (3 chars), symbol, exchange rate to base, base currency flag, `FractionName` for sub-units (Fils, Cent), `IsSystem` guard for seeded currencies (YER/USD/SAR)
- **Exchange Rate History** — Dated rate changes with `OldRate`/`NewRate` tracking, composite index on `(CurrencyId, EffectiveDate)` for fast lookups
- **Multi-currency Invoice Support** — `CurrencyId` + `ExchangeRate` on sales/purchase invoices; exchange rate recorded per transaction
- **Currency Conversion Service** — `GetBaseCurrency()` and `GetByCode()` API endpoints; `ConvertToBaseAsync()` for rate-based conversion
- **Desktop CRUD** — Currency Editor + List ViewModels with full CRUD, `IDisposable` for EventBus, toast notifications for minor success, buttons always enabled (no CanExecute)
- **Read Endpoints Public** — `AllStaff` policy for GET endpoints so all logged-in users can view exchange rates
- **Rate precision** — Exchange rates displayed as `N6` in DataGrid (not truncated `N2`)
- **v4.6.8 Fixes** — All 14 Critical/Bug items from Phase 20 code review fixed (isSystem param, MarkAsDeleted guard, filtered indexes, controller 404/400, missing validators, DI registration, includeInactive passthrough, composite index, AllStaff policy, N6 display)

### 🔄 Auto-Update System (v4.4)
- Background update check 3 seconds after startup — NEVER blocks app
- SHA256 checksum verification before launching installer
- Borderless RTL WPF dialog with version comparison, changelog, progress bar
- Skip version / Remind Later / Install Now actions
- Manual update check from Help menu
- GitHub API release check as alternative to custom server
- `IUpdaterService` returns `Result<T>` — never throws exceptions
- 8-second timeout on update check — silent failure on network issues

### 🔒 Security & DPAPI (v4.4)
- Connection strings encrypted via DPAPI (`IDataProtector`) with `"DPAPI:"` prefix
- `FirstRunSetupService` auto-encrypts plaintext connection string on first run
- `SecureDbContextFactory` decrypts connection string before creating DbContext
- DataProtection keys stored in `%ProgramData%\SalesSystem\DataProtectionKeys`
- `SecurityAudit.cs` — DEBUG-only pre-build security checks
- Atomic file writes (`.tmp` → `File.Replace()` → `.bak`) for config files
- JWT secret from environment variable — throws in production if missing

### 💾 Backup System (v4.4)
- `BackupService` — raw SQL `BACKUP DATABASE` / `RESTORE DATABASE` (no SMO)
- Restore uses `SINGLE_USER WITH ROLLBACK AFTER 30` — safe for active transactions
- `TrySetMultiUserAsync` recovery on restore failure — NEVER leaves DB in SINGLE_USER
- `ScheduledBackupWorker` — `BackgroundService` running daily at 2:00 AM
- Configurable retention days (default 30) — old backups auto-deleted
- `int.TryParse` for all config values — no exceptions on bad config

### 🖥️ Windows Service (v4.4)
- API runs as Windows Service via `UseWindowsService()`
- Service name: `SalesSystemService` with Arabic display name
- Auto-recovery: 3 restarts on failure (1min, 5min, 15min delays)
- Serilog EventLog sink for Windows Service logging
- SQL retry on startup: 3 attempts × 5 second delay
- Database migration runs on service startup (auto-migrate)
- `Install-Service.bat` / `Uninstall-Service.bat` scripts

### 👑 Admin Screens (v4.4)
- `AdminOnlyViewModel` base class enforces Admin role
- Non-admin users get `UnauthorizedAccessException` — admin UI hidden
- `UserListViewModel` with Toggle Status (soft delete) and Reset Password
- `UsersListView` DataGrid with Arabic labels and action buttons
- Constructor injection — no service locator anti-pattern

### 📦 Installer (v4.4)
- Inno Setup script (`SalesSystem.iss`) — admin install required
- .NET 10 runtime check before installation
- Windows Service auto-start configured during install
- Creates backup directory and sets permissions
- Arabic UI throughout installer

### 🔒 Defensive Programming (v4.2)
- Guard Clauses on ALL Domain entities
- Arabic error messages for validation failures
- Real-time form validation with red border indicators
- Save buttons disabled until form is valid

### 🗑️ Conditional Delete Strategy (v4.2)
- Three-option delete dialog: Cancel / Deactivate / Permanent Delete
- Soft delete keeps entity history for audit trails
- Hard delete with reference validation (prevents breaking FKs)
- Applied to: Products, Customers, Suppliers, Categories, Units, Warehouses

### 💬 Modern Dialogs (v4.2)
- Styled modal dialogs (Error, Success, Warning, Confirmation)
- RTL Arabic interface with custom themes
- Delete confirmation with 3 options
- Toast notifications for minor actions (auto-dismiss)

### ✨ Real-Time Validation (v4.2)
- INotifyDataErrorInfo implementation
- Dynamic validation feedback as user types
- Visual red borders on invalid fields
- Arabic error messages: "الاسم مطلوب", "الكمية يجب أن تكون أكبر من صفر"

### 🚫 Out of Scope (Future Phases)

The following features are **not included** in the current MVP but are planned for future development:

| Feature | Phase | Notes |
|---------|-------|-------|
| 🌐 Web interface | Future | API is ready — only a frontend client is needed |
| 📱 Mobile application | Future | API supports any HTTP client |
| 🏢 Multi-branch management | Future | Current scope is single-branch with multi-warehouse |
| 🔗 External integrations | Future | E-commerce, payment gateways, tax authority APIs |
| 📧 Email / SMS notifications | Future | Customer alerts and payment reminders |
| 📈 Advanced analytics | Future | BI dashboards and trend analysis |

> **Note:** The Clean Architecture and API-first design make adding these features straightforward without modifying existing business logic.

---

## 🛠️ Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Runtime** | .NET | 10 LTS |
| **API** | ASP.NET Core Web API | 10 |
| **Desktop UI** | WPF (MVVM) + INotifyDataErrorInfo | .NET 10 |
| **Database** | SQL Server | 2019+ |
| **ORM** | Entity Framework Core | 10 |
| **Authentication** | JWT Bearer | — |
| **Password Hashing** | BCrypt.Net-Next | 4.x |
| **Validation** | FluentValidation | 11.x |
| **Logging** | Serilog | 8.x |
| **API Docs** | Swashbuckle (Swagger) | 6.x |
| **PDF Generation** | QuestPDF | 2024.3.x |
| **Image Processing** | SixLabors.ImageSharp | 3.1.x |
| **Thermal Printing** | Win32 raw (OpenPrinter/WritePrinter) | — |
| **Reports (Excel)** | ClosedXML | 0.102.x |
| **Data Protection** | ASP.NET Core DataProtection | 10.x |
| **Windows Service** | Microsoft.Extensions.Hosting.WindowsServices | 10.x |
| **Event Log** | Serilog.Sinks.EventLog | 8.x |
| **Installer** | Inno Setup | 6.x |

---

## 📐 Database Schema

**30+ tables** covering the full retail domain:

| Category | Tables |
|----------|--------|
| **Core** | Users, Units, Categories, Products, Warehouses, WarehouseStocks |
| **Products** | ProductUnits (dynamic UOM), UnitBarcodes (unit-specific barcodes) |
| **Trading Partners** | Customers, Suppliers |
| **Purchases** | PurchaseInvoices, PurchaseInvoiceItems |
| **Sales** | SalesInvoices, SalesInvoiceItems |
| **Returns** | PurchaseReturns, PurchaseReturnItems, SalesReturns, SalesReturnItems |
| **Transfers** | StockTransfers, StockTransferItems |
| **Payments** | CustomerPayments, SupplierPayments |
| **Cash Management** | CashBoxes, CashTransactions |
| **Pricing & Costing** | ProductPriceHistory (price audit trail) |
| **Tax** | Taxes (name, rate, type, IsDefault, soft-deletable) |
| **System** | SystemSettings (29 seeded settings across 8 categories), StoreSettings (company info), DocumentSequences, InventoryMovements, SystemLog |

### Key Constraints
- `decimal(18,2)` for all money fields — **never** `float` or `double`
- `decimal(18,3)` for all quantities
- `nvarchar` for all text (Arabic + English support)
- `CHECK (Quantity >= 0)` on warehouse stock
- Soft delete pattern (`IsActive` flag) — no hard deletes
- DPAPI-encrypted connection strings with `"DPAPI:"` prefix

---

## 🔐 Security

| Feature | Implementation |
|---------|---------------|
| Authentication | JWT Bearer (8-hour expiry) |
| Password Storage | BCrypt hash (work factor: 12) |
| Authorization | Policy-based (`AdminOnly`, `ManagerAndAbove`, `AllStaff`) |
| Token Storage | In-memory only (Desktop) — never persisted to disk |
| Connection Strings | DPAPI-encrypted with `"DPAPI:"` prefix — `FirstRunSetupService` auto-encrypts |
| Data Protection Keys | `%ProgramData%\SalesSystem\DataProtectionKeys` — DPAPI-encrypted |
| JWT Secret | Environment variable only — throws in production if missing |
| Config File Writes | Atomic pattern: `.tmp` → `File.Replace()` → `.bak` |
| Security Audit | `SecurityAudit.cs` — DEBUG-only checks for unencrypted strings, hardcoded passwords |
| User Deletion | Soft delete only — FK integrity preserved |

---

## 📂 Project Documentation

| Document | Purpose |
|----------|---------|
| [`AGENTS.md`](AGENTS.md) | Master rules for AI-assisted development |
| [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md) | Non-negotiable architectural rules |
| [`docs/PRD-MVP.md`](docs/PRD-MVP.md) | Full product requirements document |
| [`docs/database-schema.md`](docs/database-schema.md) | SQL Server schema (30+ tables) |
| [`docs/ui-screens.md`](docs/ui-screens.md) | UI/UX flows and EventBus patterns |

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [SQL Server 2019+](https://www.microsoft.com/en-us/sql-server) (or SQL Server Express)
- Visual Studio 2022+ (recommended)

### Setup

```bash
# 1. Clone the repository
git clone https://github.com/ZakariaAl-honyny/Sales-Management-System.git

# 2. Navigate to the project
cd Sales-Management-System

# 3. Create the database
# Run the SQL script from docs/database-schema.md against your SQL Server

# 4. Configure connection string
# Set environment variable:
set SALESSYSTEM_DB_CONNECTION="Server=.;Database=SalesSystemDb;Trusted_Connection=true;TrustServerCertificate=true;"

# 5. Run the API
cd SalesSystem.Api
dotnet run

# 6. Run the Desktop app
cd SalesSystem.DesktopPWF
dotnet run
```

### Default Login
| Username | Password | Role |
|----------|----------|------|
| `admin` | `admin123` | Admin |

---

## 📈 Implementation Phases

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 0 | Database — Migrations, Seed Data, Constraints | ✅ Completed |
| Phase 1 | Domain — Entities, Business Rules, Guards | ✅ Completed |
| Phase 2 | Infrastructure — DbContext, Repositories, UoW | ✅ Completed |
| Phase 3 | Application — Services (Product → Customer → Sales → Purchases → Returns) | ✅ Completed |
| Phase 4 | API — Controllers, Validation, JWT Auth, Swagger | ✅ Completed |
| Phase 5 | Desktop Shell — MainForm, Navigation, EventBus, Login | ✅ Completed |
| Phase 6 | Desktop Modules — Products, Customers, Sales, Purchases, Returns, Reports | ✅ Completed |
| **Phase 7** | **Printing Engine** — A4 PDF (QuestPDF), 80mm Thermal (ESC/POS Win32), Preview, API Endpoints, WPF Integration, Print Settings Persistence | ✅ **Completed** |
| **Phase 8** | Dynamic UOM + Costing + Cash Boxes | ✅ Completed |
| **Phase 9** | **Production Readiness** — Auto-Update, DPAPI Security, Backup System, Windows Service, Admin Screens, Inno Setup Installer | ✅ **Completed** |
| **Phase 10** | **Code Quality & Refactoring** — ExecuteAsync() wrapper, MediatR removal, Legacy deletion, Test updates, MASTER-PLAN.md rewrite | ✅ **Completed** |
| **Phase 11** | **Multi-Window Screen Management** — Non-modal editors, ScreenWindowService, "فتح نافذة جديدة" menus | ✅ **Completed** |
| **v4.5.1** | **Error Message & Shutdown Improvements** — Error message overhaul, Arabic-friendly errors, shutdown handling, MessageBox elimination, async dialog compliance | ✅ **Complete** |
| **v4.5.1a** | **Logging Separation & API Error Fixes** — Logging policy (Error vs Warning), `HandleResponseAsync` content-type guard, print test log level fix | ✅ **Complete** |
| **Phase 13** | **Interactive Validation** — Remove CanExecute blocking, on-click warning dialogs, field ToolTips, required `*` markers, unique field explanations | ✅ **Completed** |
| **Phase 14** | **Audit & Polish** — LogSystemError centralized, Dialog overlay + hover, ValidationErrorsDialog, auto-focus, hard-delete safety, login/settings fixes | ✅ **Completed** |
| **Phase 16** | **Audit & Service Layer Purity** — Result pattern enforcement, decimal precision fix, FK Restrict, Controller purity, FluentValidators, CostingMethod UI, Price Sync Indicators | ✅ **Completed** |
| **v4.6.1** | **UI Sorting & Dialog Safety** — Newest-first sorting across 14 ViewModels, DatabaseErrorDialog self-owner fix, comprehensive system audit | ✅ **Completed** |
| **v4.6.2** | **WPF Validation ErrorTemplate** — Red border + ❗ icon ErrorTemplate, INotifyDataErrorInfo standardization, ValidateAllAsync() base method, 14 Editor VMs updated | ✅ **Completed** |
| **v4.6.3** | **Architecture Alignment & Code Quality Audit** — Settings ViewModels/Views relocation, DI registration, MessageBox removal, async void refactoring, shadowing resolved | ✅ **Completed** |
| **v4.6.4** | **Security Hardening & Code Quality** — Rate limiting (5/15min), user hard-delete guard, connection string security, FluentValidator enhancements, FallbackErrorDialog, build warning fixes | ✅ **Completed** |
| **v4.6.5** | **Invoice Number Removal & Touch POS Polish** — InvoiceNo removed from entities, services, controllers, ViewModels, DTOs. Touch POS product card layout fixed. Stock validation warning on product add. PlayWarning() added to ISoundService. Garbled Arabic fixes | ✅ **Completed** |
| **v4.6.7** | **InvoiceNo Int Re-addition** — InvoiceNo re-added as `int` (not string) to SalesInvoice/PurchaseInvoice. UNIQUE per document type. Thread-safe via DocumentSequenceService. Accounting Foundation: Chart of Accounts, Journal Entries, Fiscal Years. Build: 0 errors, 0 warnings | ✅ **Completed** |
| **Phase 18** | **Accounting Foundation** — Chart of Accounts (Account entity, 4 levels), Journal Entries (double-entry, manual creation), Journal Entry Lines (debit/credit), System Account Mappings (13 default account bindings), Fiscal Year Closure (revenue/expense zeroing, retained earnings transfer), Annual Closing Service (net income calculation), Account Balances/Ledger queries, FiscalYearClosure tracking | ✅ **Completed** |
| **Phase 19** | **Settings Module** — SystemSetting key-value store (8 categories, 29 seeded settings), Tax Module (Tax entity, CRUD service, API, Desktop UI), Tax on Invoices (TaxId FK, SetTax(), GetTaxAmount()), Memory Caching (IMemoryCache + ConcurrentDictionary, 5-min sliding expiration), Store Settings (company info, deprecation strategy), Costing Method switcher (WeightedAverage/LastPurchasePrice/SupplierPrice), Print Settings (printer name, logo, receipt header/footer), Backup Settings (path, schedule, retention), Admin-only endpoints | ✅ **Completed** |
| **Phase 20** | **Currencies Module** — Currency entity (name, code, symbol, exchange rate, base currency), Exchange Rate History (effective dated, change tracking), Multi-currency support for invoices, Currency conversion service, Currency Editor + List ViewModels with full CRUD, Exchange rate change events, GetByCode + GetBaseCurrency API endpoints, AllStaff policy for read endpoints (non-admin users can view rates) | ✅ **Completed** |
| Phase 21 | **Users & Permissions Module** — 4 roles, 33 permissions, passwordless creation, lockout, AuditLog, Permission management UI | ✅ **Completed** |
| **v4.6.8** | **Currency Module Stabilization & EF Core Transaction Strategy** — Fixed BeginTransactionAsync conflict (3 methods). ExchangeRate precision (18,2). JournalEntry shadow FK. CurrencyEditorViewModel: RULE-229 validation, toast, dual constructor. Deep code review: fixed 14 bugs + 3 enhancements across Domain, API, Infrastructure, Desktop — including isSystem param, MarkAsDeleted guard, GetByCode/GetBaseCurrency endpoints, includeInactive passthrough, controller 404/400, filtered indexes, OldRate validation, UpdateExchangeRateRequestValidator, IDisposable, CanExecute removal, composite index, AllStaff policy, N6 display | ✅ **Completed** |
| **v4.6.6** | **UI Compacting — Mobile-Ready Density** — Global UI resize (63 views) for more content per screen: Styles.xaml compact tokens (button 36→28, font 13→11, DataGrid 34→24), all list/editor/dialog views compacted by ~25-30%, PurchaseInvoiceEditorView size reduction, MainWindow sidebar 220→200, touch control sizes preserved. Future mobile-ready foundation | ✅ **Completed** |

### Printing Engine — Phase 7 Breakdown

| Step | Description | Status |
|------|-------------|--------|
| 0 — Setup | NuGet packages (QuestPDF, ImageSharp, Drawing.Common), QuestPDF license init | ✅ |
| 1 — Contracts | DTOs (`InvoicePrintDto`, `InvoiceItemPrintDto`, `PrintResult`), `IPrintService` interface, `InvoicePrintDtoBuilder` (4 overloads) | ✅ |
| 2 — A4 PDF | `A4InvoiceDocument` — RTL Arabic, logo, alternating rows, tax breakdown, page numbers, footer | ✅ |
| 3 — Thermal | `ThermalReceiptGenerator`, `EscPos` static builder — 42-char columns, Windows-1256, cutter, cash drawer | ✅ |
| 4 — PrintService | Win32 raw printing (`OpenPrinter`/`WritePrinter`), temp-file cleanup, Arabic error messages | ✅ |
| 5 — API + Desktop | `PrintController` (11 endpoints), `IPrintApiService`/`PrintApiService`, `PdfPreviewWindow`, WPF print buttons, DI registrations | ✅ |
| 6 — Production | Print settings in `SystemSetting` table, `IPrintApiService` injection into ViewModels, test print endpoint, 254+ tests | ✅ |

---

## 🆕 What's New in v4.5.3 — Identifier Strategy Complete (All Entities)

| Feature | Description |
|---------|-------------|
| **Removed Code Columns** | `Code` column removed from Product, Customer, and Supplier entities — use auto-increment `Id` (int PK) as sole identifier |
| **Faster Search** | Search/filter by integer `Id` or `Name` only — no string Code comparisons |
| **Simpler Forms** | Code text fields removed from all editor screens (Product, Customer, Supplier) |
| **Cleaner Invoice Items** | `ProductCode` removed from all invoice item DTOs — uses `ProductId` only |
| **Removed Code Reports** | Report DTOs (StockReport, CustomerBalance, SupplierBalance, LowStock) no longer include Code fields |
| **Code Auto-Generation Removed** | `DocumentSequenceService` calls for PRD/CUST/SUP prefixes removed — no manual/auto code needed |
| **Smaller Database** | 3 columns and 3 unique indexes removed from Products, Customers, Suppliers tables |
| **Warehouse Code Removed** | `Code` column removed from Warehouse entity — all layers (Domain, EF, DTOs, Validators, ViewModels, Tests) |

---

## 🆕 What's New in v4.6 — Interactive Validation (Self-Explaining System)

| Feature | Description |
|---------|-------------|
| **Buttons Always Enabled** | Save/Post/Print buttons are NEVER disabled — users see all available actions at all times |
| **On-Click Validation** | When clicking Save with incomplete data, a clear warning dialog lists EVERY missing/incorrect field |
| **Required Field Markers (*)** | All required fields consistently marked with `*` across every editor screen |
| **Field-Level ToolTips** | Every input field has an Arabic ToolTip explaining validation rules (format, uniqueness, constraints) |
| **Unique Field Explanations** | Barcode and Username fields have explicit helper text explaining uniqueness requirements |
| **CanExecute Removed From All Commands** | 13 editor ViewModels had CanExecute predicates removed — Save, Post, Cancel commands always enabled |
| **Interactive Instead of Blocking** | System guides users through completing data rather than blocking them with disabled buttons |
| **13 ViewModels + 7 XAML Files Updated** | Product, Customer, Supplier, Category, Unit, Warehouse, User, Sales/Purchase Invoice, Stock Transfer, Sale/Purchase Return editors |

### Before (v4.5.3) — Blocking Validation
- Save buttons disabled (greyed out) until all required fields filled
- Users didn't know WHY the button was disabled
- No field-level explanations for validation rules
- Barcode/username uniqueness not explained
- Not all required fields had `*` markers

### After (v4.6) — Interactive Validation
- All buttons always visible and clickable
- Clicking Save with missing data shows: "يرجى إكمال البيانات الإلزامية التالية:" + bullet list of all issues
- Every input field has ToolTip explaining its rule
- Unique fields have helper text: "الباركود يجب أن يكون فريداً — لا يمكن تكرار نفس الرمز لمنتجين مختلفين"
- Required fields consistently marked with `*`
- Warning dialog titles are screen-specific (e.g., "بيانات غير مكتملة")

---

### 🆕 What's New in v4.6 — Audit Fixes & System-Wide Polish

| Feature | Description |
|---------|-------------|
| **LogSystemError Centralized** | All `Serilog.Log.Error` calls moved to `ViewModelBase.LogSystemError()` — zero direct calls in any ViewModel |
| **Hard Delete Safety** | All 7 Application services now catch `DbUpdateException` in `PermanentDeleteAsync()` — returns descriptive Arabic error with inner exception logged |
| **Dialog Overlay + Hover Effects** | All 8 dialog windows updated with transparent overlay (`WindowStyle="None"`, `AllowsTransparency="True"`, `#80000000` dimming), deep shadow cards, and button hover effects (IsMouseOver/IsPressed) |
| **ValidationErrorsDialog** | New dedicated dialog with `ItemsControl` for bulleted error list — `ShowValidationErrorsAsync(title, List<string>)` added to `IDialogService` |
| **Auto-Focus on First Invalid Field** | `ValidationFocusBehavior` helper class + `FocusFirstInvalidFieldRequested` event — after validation dialog closes, focus jumps to first error field |
| **Login Icon Background Fix** | Login window icon no longer shows black rectangle — transparent background with PrimaryBrush fill |
| **Settings Layout Fixed** | Test Print button no longer overflows — 4th RowDefinition added, bottom margin corrected |
| **6 Subagent Files Updated** | All agent files updated with v4.6 patterns for automatic enforcement by AI agents |

---

## 🆕 What's New in v4.5.1

| Feature | Description |
|---------|-------------|
| **Error Message Best Practices** | All catch blocks now use `Serilog.Log.Error()` — raw `ex.Message` NEVER shown to users |
| **Arabic-Friendly Error Handling** | `HandleFailure()` transforms timeout/network/not-found errors into clear Arabic messages |
| **Screen-Specific Dialog Titles** | All error dialogs now have context titles like `"خطأ في حفظ الفاتورة"` instead of generic `"خطأ"` |
| **MessageBox Eliminated** | All 16 remaining `MessageBox.Show` calls replaced with `IDialogService` across 6 editor ViewModels |
| **Async Dialog Compliance** | All sync `ShowError`/`ShowInfo`/`ShowWarning` calls migrated to `ShowErrorAsync`/`ShowInfoAsync`/`ShowWarningAsync` |
| **Application Shutdown** | `ShutdownMode="OnExplicitShutdown"` with proper exit handling (LoginWindow close → shutdown, MainWindow close → shutdown, Logout → reopen LoginWindow) |
| **Safe Error Logging** | Raw HTTP response bodies no longer shown to users — logged via Serilog instead |
| **Vague Success Messages Fixed** | `"تم التصدير بنجاح"` → `"تم تصدير التقرير إلى Excel بنجاح"` / `"إلى CSV"` |
| **Logging Separation Policy** | `Log.Error` reserved for system errors only (DB down, API unreachable, parse crashes); `Log.Warning` for user mistakes (validation errors, business rules, "not found") |
| **HandleResponseAsync JSON Fix** | Non-generic `HandleResponseAsync` now checks content type before parsing JSON — prevents crash on empty/HTML 404 responses |
| **Print Test Log Level Fixed** | Print test failures downgraded from `Log.Error` to `Log.Warning` — printer config is a user/configuration issue |
| **Manual Window Creation Eliminated** | `CustomerListViewModel` `AddCustomer()`/`EditCustomer()` replaced manual `new CustomerEditorView { DataContext = vm }` + `ShowDialog()` with `_dialogService.ShowDialog()` |
| **Supplier List Refresh Fixed** | `SupplierListViewModel` `AddSupplier()`/`EditSupplier()` now check `ShowDialog()` return and refresh list — previously ignored return causing stale data |
| **Supplier Code Deduplicated** | `SupplierListViewModel` extracted command initialization into `InitializeCommands()` — eliminated duplicate code in both constructors |
| **Supplier Command Properties Fixed** | Command properties changed from `{ get; }` to `{ get; private set; } = null!;` to support standard InitializeCommands pattern |
| **Product Editor Messages Fixed** | `ProductEditorViewModel` added `IDialogService` dependency; replaced all 4 `MessageBox.Show` calls with proper async dialog calls |
| **Self-Explaining System — ToolTips** | 174 Arabic ToolTips added across ~40 XAML files covering every Button, MenuItem, and interactive control |
| **ToolTip Mapping Standard** | ToolTips describe user actions (e.g., `"فتح شاشة إضافة منتج جديد"`) — never just repeat button text |
| **Consequence-Aware ToolTips** | Action buttons explain consequences: `"ترحيل العملية نهائياً — سيتم تحديث المخزون والرصيد"` |
| **Navigation Menu ToolTips** | All 31 MainWindow MenuItems now have destination descriptions: `"عرض وإدارة فواتير البيع"` |
| **Empty-State ToolTips** | All 11 "add first item" buttons now have ToolTips guiding first-time users |

---

## 🆕 What's New in v4.5

| Feature | Description |
|---------|-------------|
| **ExecuteAsync() Pattern** | Centralized error handling wrapper in ViewModelBase — eliminates manual try/catch in all ViewModels |
| **IsBusy Property** | Replaces IsLoading — automatically managed by ExecuteAsync() |
| **MediatR Removed** | Unused package removed — replaced ProductPriceQuery with IProductPriceService (Service Layer) |
| **Legacy WinForms Deleted** | Abandoned SalesSystem.Desktop/ directory removed — all functionality rebuilt in WPF |
| **MASTER-PLAN.md Rewritten** | Now reflects actual Clean Architecture (Layered) — NOT aspirational Vertical Slices |
| **Test Files Updated** | Application.Tests, Api.Tests, DesktopPWF.Tests, E2ETests — all updated to match current API signatures |
| **E2ETests Fixed** | CS0118 namespace conflict resolved (FlaUI.Core.Application vs System.Windows.Application) |
| **AGENTS.md Updated** | v4.5 with RULE-141 to RULE-170 (ExecuteAsync, MediatR removal, Legacy deletion, DB Health Check, Multi-Window) |
| **DB Health Check** | `GET /api/v1/health/database` endpoint + Desktop startup check before login |
| **DatabaseErrorDialog** | Styled dialog with Retry/Exit on DB connection failure (Arabic) |
| **ExceptionMiddleware** | Detects DB failures → returns `DATABASE_CONNECTION_ERROR` with 503 |
| **🆕 Multi-Window Screen Management** | Open multiple independent windows simultaneously (e.g., 3 sale invoices at once) |
| **🆕 IScreenWindowService** | Service layer for non-modal window lifecycle tracking, cascade positioning, weak reference management |
| **🆕 ScreenWindow** | Generic host window that accepts any View/ViewModel pair via naming convention |
| **🆕 Non-Modal Editors** | All 7 editor types (Sales, Purchases, Returns, Payments, Transfers) now open non-modally |
| **🆕 MainWindow Menu** | "فتح نافذة جديدة" items for Sales, Purchases, Warehouses, Products, Customers, Suppliers |
| **🆕 Arabic Auto-Titles** | Windows auto-title with Arabic names (فاتورة بيع, فاتورة شراء, سداد عميل, etc.) |
| **🆕 Cascade Positioning** | New windows offset 30px from MainWindow — no overlapping |

---

## 🆕 What's New in v4.4

| Feature | Description |
|---------|-------------|
| **Auto-Update System** | Background update check, SHA256 verification, RTL WPF dialog, GitHub API support |
| **DPAPI Security** | Connection string encryption, FirstRunSetupService, SecurityAudit |
| **Backup System** | Raw SQL backup/restore, scheduled daily backups, auto-recovery from SINGLE_USER |
| **Windows Service** | UseWindowsService(), auto-recovery, EventLog logging, install scripts |
| **Admin Screens** | AdminOnlyViewModel, User management (toggle status, reset password) |
| **Inno Setup Installer** | .NET 10 runtime check, service auto-start, Arabic UI |
| **Dialog Service Enhanced** | ShowInfoAsync (blue theme), styled dialogs replace all MessageBox.Show |
| **Atomic File Writes** | .tmp → File.Replace() → .bak pattern for all config file writes |

---

## 🆕 What's New in v4.3

| Feature | Description |
|---------|-------------|
| **Dynamic UOM** | ProductUnits with per-unit pricing, barcodes, conversion factors |
| **Costing Strategy** | WeightedAverage / LastPurchasePrice / SupplierPrice methods |
| **Cash Boxes** | Multi-box cash management with immutable transactions |
| **Price History** | ProductPriceHistory audit on every cost/price change |
| **WPF DesktopPWF** | Full MVVM rewrite — EventBus, DialogService, printing subsystem |
| **Barcode Lookup** | Unit-aware barcode scanning via BarcodeLookupService |
| **Smart Formatter** | Quantity-based display unit selection |
| **System Settings** | Configurable costing method, store info |
| **Backup Service** | Database backup automation via API |
| **New Controllers** | Backup, Settings, Users, Dashboard, Logs, Returns, Payments |
| **Printing Engine** | A4 PDF (QuestPDF) + 80mm Thermal (ESC/POS Win32) printing engine |
| **PrintController** | 11 API endpoints for preview, print, save, and test page |
| **Print Settings** | Persisted in `SystemSetting` table — printer names, logo, tax info |
| **254+ Tests** | Full test coverage across all 5 test projects |

---

## 🆕 What's New in v4.6 — Audit & Service Layer Purity

| Feature | Description |
|---------|-------------|
| **UpdateProductPricingService Returns Result** | Changed from `Task` + throwing exceptions to `Task<Result>` — never throws, returns `Result.Failure` with Arabic messages |
| **decimal(18,4) → decimal(18,2)** | All money fields in ProductUnit, CashBox, CashTransaction configurations changed from `HasPrecision(18,4)` to `HasPrecision(18,2)` |
| **FK DeleteBehavior.Restrict Enforced** | Cascade delete removed from ProductUnit, UnitBarcode, ProductBarcode configurations — ALL FKs use `Restrict` |
| **Controller Purity Enforced** | PrintController moved DbContext queries to PrintDataService; LogsController removed `[AllowAnonymous]`; SettingsController GET restricted to AdminOnly |
| **PrintDataService Returns Result<T>** | Changed from returning `InvoicePrintDto?` (nullable) to `Result<InvoicePrintDto>` — never returns null |
| **6 New FluentValidators** | Created for: UpdateSalesInvoice, UpdatePurchaseInvoice, UpdateStockTransfer, UpdateCustomerPayment, UpdateSupplierPayment, CreateLogRequest |
| **Costing Method in Settings UI** | 3 RadioButtons (Weighted Average / Last Purchase Price / Supplier Price) with Arabic explanations in Settings screen — persisted via API to SystemSettings table |
| **Price Sync Indicators in Purchase Invoice** | New `CostChangedFromDatabase` + `PriceDifferenceIndicator` properties in PurchaseInvoiceLineViewModel — orange warning shows when entered cost differs from DB current cost |
| **API CostingMethod Support** | SettingsController Get/Update now read/write CostingMethod via ISystemSettingsRepository; StoreSettingsDto and UpdateSettingsRequest include CostingMethod field |
| **42/42 Tests Passing** | 3 UpdateProductPricingService tests fixed — WeightedAverage rounding (13.71 not 13.7113), Result assertions instead of exception expectations |

---

## 🆕 What's New in v4.6.2 — Validation ErrorTemplate & INotifyDataErrorInfo

| Feature | Description |
|---------|-------------|
| **Newest-First Sorting** | ALL 14 list ViewModels now sort records with newest first — products, customers, suppliers, warehouses, categories, units, users, returns, transfers, and payments sort by `Id` descending; invoices sort by `InvoiceDate` descending |
| **Files Updated** | `ProductListViewModel`, `CustomerListViewModel`, `SupplierListViewModel`, `WarehouseListViewModel`, `CategoryListViewModel`, `UnitListViewModel`, `UserListViewModel`, `SalesReturnListViewModel`, `PurchaseReturnListViewModel`, `StockTransfersListViewModel`, `CustomerPaymentsListViewModel`, `SupplierPaymentsListViewModel`, `SalesInvoiceListViewModel`, `PurchaseInvoiceListViewModel` |
| **DatabaseErrorDialog Bug Fix** | Fixed "Cannot set Owner property to itself" crash on startup — `PositionOverOwner()` now guards against `MainWindow == this` and falls back to `CenterScreen` |
| **Full System Audit** | Code Reviewer, Database Engineer, and Security Auditor subagents performed comprehensive audits — 79/83 code review pass, 12/17 DB schema pass, 8/10 security pass |
| **Audit Findings Documented** | Key issues found: 16 entity configs missing `HasQueryFilter`, `CashBox` missing `OpeningBalance`, `BaseConversionFactor` precision wrong, User hard delete endpoint exists, hardcoded connection strings |

### Newest-First Sorting — Complete List

| ViewModel | Sort Field | Reason |
|-----------|-----------|--------|
| ProductListViewModel | `Id` descending | Auto-increment PK = newest first |
| CustomerListViewModel | `Id` descending | Auto-increment PK = newest first |
| SupplierListViewModel | `Id` descending | Auto-increment PK = newest first |
| WarehouseListViewModel | `Id` descending | Auto-increment PK = newest first |
| CategoryListViewModel | `Id` descending | Auto-increment PK = newest first |
| UnitListViewModel | `Id` descending | Auto-increment PK = newest first |
| UserListViewModel | `Id` descending | Auto-increment PK = newest first |
| SalesReturnListViewModel | `Id` descending | Auto-increment PK = newest first |
| PurchaseReturnListViewModel | `Id` descending | Auto-increment PK = newest first |
| StockTransfersListViewModel | `Id` descending | Auto-increment PK = newest first |
| CustomerPaymentsListViewModel | `Id` descending | Auto-increment PK = newest first |
| SupplierPaymentsListViewModel | `Id` descending | Auto-increment PK = newest first |
| SalesInvoiceListViewModel | `InvoiceDate` descending | Most recent invoices first |
| PurchaseInvoiceListViewModel | `InvoiceDate` descending | Most recent invoices first |

---

## 🆕 What's New in v4.6.2 — WPF Validation ErrorTemplate & INotifyDataErrorInfo

| Feature | Description |
|---------|-------------|
| **Red Border + ❗ Icon on Invalid Fields** | New `Validation.ErrorTemplate` in `Styles.xaml` wraps invalid TextBox/PasswordBox/ComboBox fields in a red border with a red ❗ badge icon — hover shows the error message via ToolTip bound to `[0].ErrorContent` |
| **INotifyDataErrorInfo Standardized** | All Editor ViewModels now use `AddError/ClearErrors` in property setters for real-time validation — replaces legacy `HasXxxError` boolean + computed string pattern |
| **ValidateAllAsync() Base Method** | `ViewModelBase` now provides a reusable `ValidateAllAsync()` that checks `HasErrors`, shows the validation warning dialog, and calls `RequestFocusFirstInvalidField()` — no more duplicated dialog logic in each Editor VM |
| **14 Editor VMs Updated** | All 14 Editor ViewModels now call `SetDialogService()` in constructors to enable the base validation infrastructure |

### Files Modified

| File | Change |
|------|--------|
| `Resources/Styles.xaml` | New `ErrorTemplate` with red border + ❗ icon + ToolTip; Validation.HasError triggers on TextBox, PasswordBox, ComboBox |
| `ViewModels/ViewModelBase.cs` | Added `SetDialogService()`, `ValidateAllAsync()`, `ValidateField()` |
| `ViewModels/Products/ProductEditorViewModel.cs` | Refactored: removed 7 `HasXxxError` booleans, uses pure INotifyDataErrorInfo |
| `ViewModels/Customers/CustomerEditorViewModel.cs` | Refactored: removed 3 `HasXxxError` booleans, uses pure INotifyDataErrorInfo |
| `ViewModels/Suppliers/SupplierEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/Categories/CategoryEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/Units/UnitEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/WarehouseEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/Users/UserEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/Payments/CustomerPaymentEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/Payments/SupplierPaymentEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/Transfers/StockTransferEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/Returns/SalesReturnEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/Returns/PurchaseReturnEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/Sales/SalesInvoiceEditorViewModel.cs` | Added `SetDialogService()` |
| `ViewModels/Purchases/PurchaseInvoiceEditorViewModel.cs` | Added `SetDialogService()` |

---

## 🛡️ What's New in v4.6.4 — Security Hardening & Code Quality

| Feature | Description |
|---------|-------------|
| **Rate Limiting** | Login endpoint protected with `[EnableRateLimiting("LoginPolicy")]` — 5 attempts per 15 minutes per IP. Global rate limit of 100 req/min with Arabic 429 response (`RATE_LIMIT_EXCEEDED`) |
| **User Hard-Delete Blocked** | `UserService.PermanentDeleteAsync()` now returns `Result.Failure` — users can only be soft-deactivated per RULE-038. Hard-delete attempt logged as warning |
| **Connection String Security** | Removed plaintext connection string from `appsettings.Development.json`. Uses only `SALESSYSTEM_DB_CONNECTION` env var with `_comment` explaining the policy |
| **FluentValidator Enhancements** | Enhanced all 7 invoice/payment/transfer/return validators with additional rules: `PaymentType.IsInEnum()`, `InvoiceDate` not future, `Notes.MaxLength(500)`, `DiscountAmount >= 0` |
| **FallbackErrorDialog** | Added `FallbackErrorDialog.xaml` — thread-safe dialog overlay for unhandled exceptions, replacing raw `MessageBox.Show` |
| **Build Warning Fixes** | Resolved 10 CS0109 warnings (unnecessary `new` keyword) and 4 CS1540 errors (protected member access) across 8 ViewModel files |
| **Test Coverage** | 50 test files re-enabled and compiling. 5 new tests: SetDialogService constructor, ValidateAsync (empty/valid/multiple), Post_AlreadyPostedInvoice |
| **Security-Plan.md** | Comprehensive 7-layer security architecture document with implementation status table tracking what's done vs planned |
| 🔒 **Rate Limiting & Brute-Force Protection** | 5 attempts/15min per IP, 100 req/min global — with Arabic 429 response |
| 🔒 **User Hard-Delete Protection** | Soft-delete only, `PermanentDeleteAsync` returns `Result.Failure` |
| 🔒 **Connection String Security** | Environment variables only, no plaintext in config files |
| 🌐 **Arabic Encoding Integrity** | UTF-8 enforcement, garbled text auto-detection and fixes across multiple ViewModels |

---

## 🆕 What's New in v4.6.5 — Invoice Number Removal & Touch POS Polish

| Feature | Description |
|---------|-------------|
| **InvoiceNo Removed from Entities** | `InvoiceNo` (string) removed from `SalesInvoice` and `PurchaseInvoice` entities — use auto-increment `Id` (int PK) as sole invoice identifier |
| **GetByNumberAsync Removed** | `GetByNumberAsync()` methods removed from SalesInvoiceService, PurchaseInvoiceService, API controllers, and Desktop API clients — search by `Id` instead |
| **Cleaner Invoice Forms** | Invoice number text field and auto-generation removed — invoice displays use formatted `Id` (`#ID`) |
| **Search by Id** | Invoice list view search uses `int.TryParse()` + `Id` comparison — no string `InvoiceNo` filtering |
| **Report DTOs Simplified** | `SalesReportDto` and `PurchaseReportDto` use `int Id` instead of `string InvoiceNo` |
| **Print DTO Updated** | `InvoicePrintDto` uses `int Id` instead of `string InvoiceNo` for PDF/thermal printing |
| **Touch POS Card Layout Fixed** | Removed `VirtualizingPanel.ScrollUnit="Pixel"` from ModernListBox style, increased `MinHeight` on cart view — product cards now render with proper proportions |
| **Stock Validation on Product Add** | Adding a product with insufficient stock now shows a warning dialog with product name, requested qty, and available stock |
| **PlayWarning() Sound** | New `PlayWarning()` method on `ISoundService` — plays `SystemSounds.Asterisk` on stock validation failures and other warnings |
| **Garbled Arabic Fixes** | Multiple files cleaned up with correct UTF-8 Arabic encoding |

---

## 🆕 What's New in v4.6.9 — Settings Module Complete (Phase 19)

| Feature | Description |
|---------|-------------|
| **Tax Module (New)** | Full `Tax` entity (name, rate, type percentage/fixed, `IsDefault`) with `ITaxService` CRUD, `TaxesController` API endpoints, `TaxesListView` + `TaxEditorView` WPF UI with `INotifyDataErrorInfo` validation |
| **Tax on Invoices** | `TaxId` FK (nullable int) on `SalesInvoice` and `PurchaseInvoice` with `DeleteBehavior.Restrict` — `SetTax(int? taxId)` domain method, `GetTaxAmount(decimal subTotal)` computes tax based on rate/type |
| **Memory Caching for SystemSettings** | `IMemoryCache` with `ConcurrentDictionary` key tracker — 5-minute sliding expiration, `PostEvictionCallback` auto-removes tracker, `GetAllCachedAsync()` returns from cache or DB with single DB round-trip |
| **29 System Settings Seeded** | 29 settings across 8 categories (Inventory 4, Sales 8, Purchases 3, Barcode 3, Accounting 1, Print 5, Notifications 4, General 3) via `DbSeeder` |
| **Typed SystemSetting Helpers** | `SetStringAsync(category, key, value)`, `SetIntAsync()`, `SetBoolAsync()`, `SetDecimalAsync()` — auto-creates setting with correct `DataType` if not found |
| **SystemSetting Guard Clauses** | `Create()` validates `Category` not empty and `DataType` is one of (string, int, bool, decimal) — per RULE-293 |
| **Filtered Unique Index Fix** | All filtered unique indexes on soft-deletable entities include `AND [IsActive] = 1` — per RULE-294 |
| **StoreSettings Deprecation Strategy** | `DefaultTaxRate`, `IsTaxEnabled`, `InvoicePrefix`, `CurrencyCode` deprecated with comments — Tax entity is source of truth. `StoreSettings.Update()` calls `UpdateTimestamp()` per RULE-292 |
| **SetBatchSystemSettingsAsync Fix** | No longer calls `SaveChangesAsync()` directly — uses `_uow.SaveChangesAsync()` per RULE-291 |
| **SystemSetting Create Validates Category & DataType** | Guard clauses reject empty categories and invalid data types per RULE-293 |
| **SetStringAsync Accepts category Param** | No longer hardcodes `category: "Print"` — defaults to `"General"` per RULE-295 |
| **StoreSettings Seed** | `defaultTaxRate: 0m` (not 15m) — Tax entity is the source of truth per RULE-297 |
| **Taxes List View** | `TaxesListView` with newest-first sorting (`Id` descending), toggle active/inactive, edit/delete actions |
| **Tax Editor View** | `TaxEditorView` with name, rate, type (percentage/fixed), IsDefault checkbox — `INotifyDataErrorInfo` validation, `ValidateAsync()` calls `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()` |
| **Tax FluentValidators** | `CreateTaxRequestValidator` and `UpdateTaxRequestValidator` — name required, rate > 0, type in enum |
| **IsDefault Filtered Unique Index** | `WHERE [IsActive] = 1` on `IX_Taxes_IsDefault` — soft-deleted default tax doesn't block setting a new default |
| **Admin-Only Tax Endpoints** | All tax CRUD endpoints restricted to `[Authorize(Policy = "AdminOnly")]` |
| **Build & Test** | 0 errors, 0 warnings across all 9 projects |
| **BUG-008 Fix** | CurrencyCode domain validation now enforces exactly 3 characters (ISO 4217 standard) — `Currency.Create()` changed from `code.Length > 10` to `code.Trim().Length != 3` |
| **SystemSettingsViewModel Enhancement** | Added 14 missing properties: `HideTaxInSales`, `ShowExpiryInInvoices` (Sales), `HideTaxInPurchases` (Purchases), `ShowLogo`, `FooterNote`, `ThermalPrinterName`, `A4PrinterName`, `LogoPath`, `StoreTaxNumber` (Print), `LowStockAlert`, `ExpiryAlert`, `ExpiryAlertDays`, `CreditLimitAlert` (Notifications) — all mapped via `MapFromDictionary`/`BuildDictionary` |
| **Service-Level Validation** | `StoreSettingsService.UpdateSystemSettingsAsync()` validates 19 boolean keys via `bool.TryParse` and 6 integer keys via `int.TryParse` with range checks (CostingMethod 1-3, DecimalPlaces 0-6, StockAlertDays 1-365, ExpiryAlertDays 0+) |
| **Tax Domain Methods** | Added `Tax.SetDefault()` and `Tax.ClearDefault()` — both call `UpdateTimestamp()` for audit trail, replaces service-level direct property manipulation |
| **Phase 18 Review Doc Updated** | All 12 bugs (5 critical, 7 standard) in `docs/phase18_accounting_review.md` marked as `[FIXED]` with post-review fix status table |
| **CashBox OpeningBalance Fix** | `CashBox.Create()` now sets `OpeningBalance = initialBalance` — was always `0` regardless of initial balance (ENH-005) |
| **Currency Domain Methods** | Added `SetAsBaseCurrency()` and `UnsetBaseCurrency()` on `Currency` entity — both call `UpdateTimestamp()` for audit trail (ENH-007) |
| **Removed Unnecessary async** | `CurrencyEditorViewModel.LoadRateHistoryAsync()` removed unnecessary `async` from `InvokeOnUIThreadAsync` lambda — no `await` inside (ENH-012) |
| **Phase 21 Users & Permissions** | Full Users & Permissions module — 16 tasks, 22 gaps closed. User entity rebuilt (UserStatus, passwordless creation, lockout 5 attempts, Phone/Email/Avatar/DefaultCashBoxId), Permission+RolePermission DB-backed system (33 permissions, 4 roles), AuditLog (long Id, indexed, paginated browser), UserSession, AuthService (SetPassword/ChangePassword/audit trail), API endpoints (10 new), Desktop UI (PasswordChangeScreen, AuditLog browser, PermissionManagement, enhanced UserEditor, StatusBar avatar/role badge/logout) |

### Key Rules Added to AGENTS.md
- RULE-291 to RULE-301 covering Phase 19 settings module remediations
- RULE-302 to RULE-304 covering Phase 20 currency enhancement remediations (CashBox.OpeningBalance, SetAsBaseCurrency/UnsetBaseCurrency, InvokeOnUIThreadAsync pattern)
- RULE-305 to RULE-320 covering Phase 21 Users & Permissions module remediations (User entity rebuild, UserStatus, Permission/RolePermission entities, AuditLog, UserSession, AuthService, API endpoints, Desktop UI)

---

## 🆕 What's New in v4.6.8 — Currency Module Stabilization & EF Core Transaction Strategy

| Feature | Description |
|---------|-------------|
| **Fixed: BeginTransactionAsync conflict** | Removed manual `BeginTransactionAsync` from `CurrencyService.CreateAsync`/`UpdateAsync`/`UpdateExchangeRateAsync` — conflicts with `SqlServerRetryingExecutionStrategy` which throws `InvalidOperationException` on user-initiated transactions. Single `SaveChangesAsync` is now used (EF Core implicit transaction). |
| **ExchangeRate precision (18,2)** | Added `.HasPrecision(18, 2)` on `CustomerPayment.ExchangeRate` and `SupplierPayment.ExchangeRate` — prevents silent decimal truncation |
| **Fixed: JournalEntryId1 shadow FK** | Changed `JournalEntryConfiguration` bare `.WithOne()` to `.WithOne(x => x.JournalEntry)` — eliminates the shadow FK `JournalEntryId1` |
| **Fixed: CurrencyEditorViewModel validation** | `ValidateAsync()` now follows RULE-229: calls `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()` instead of custom dialog bypassing INotifyDataErrorInfo |
| **Fixed: Log level violation** | Removed `LogSystemError` from `SaveOperationAsync` else block — API validation errors (duplicate name/code) are user mistakes, not system errors. `HandleFailure` already logs at Warning |
| **Added: IToastNotificationService** | CurrencyEditorViewModel now shows `"تم إضافة العملة بنجاح"` / `"تم تعديل العملة بنجاح"` success toast before closing |
| **Dual constructor pattern** | CurrencyEditorViewModel now follows CashBoxEditorViewModel pattern: parameterless → parameterized, enabling unit testability |
| **Added: isSystem param** | `Currency.Create()` now accepts `bool isSystem = false` — system currencies (YER/USD/SAR) can be protected from deletion |
| **Added: IsSystem guard in MarkAsDeleted** | `Currency.MarkAsDeleted()` throws `DomainException` if `IsSystem == true` — prevents deleting system currencies |
| **Added: GetByCode + GetBaseCurrency endpoints** | New API endpoints `GET /api/v1/currencies/by-code/{code}` and `GET /api/v1/currencies/base` |
| **Fixed: includeInactive passthrough** | `CurrencyApiService.GetAllAsync()` now passes `?includeInactive=` to API; controller and service accept the parameter |
| **Fixed: Controller 404 vs 400** | Delete, PermanentDelete, UpdateExchangeRate endpoints now return 404 NotFound when entity not found (was always 400) |
| **Fixed: Filtered unique indexes** | Currency Name/Code indexes add `.HasFilter("[IsActive] = 1")`; IsBaseCurrency filter adds `AND [IsActive] = 1` |
| **Fixed: OldRate validation** | `ExchangeRateHistory.Create()` now rejects `oldRate <= 0` (was allowing zero) |
| **Fixed: UpdateExchangeRateRequestValidator** | Added missing FluentValidation validator for `NewRate > 0` and `UserId > 0` |
| **Fixed: Removed IsActive from UpdateCurrencyRequest** | Removed unused `IsActive` parameter from update request — was confusing (sent but ignored by backend) |
| **Fixed: IDisposable on list VM** | `CurrenciesListViewModel` now implements `IDisposable` — EventBus subscriptions cleaned up in `Dispose()` |
| **Fixed: CanExecute predicates** | Removed CanExecute from `EditCommand` and `DeleteCommand` per RULE-059 — buttons always enabled |
| **Fixed: Restore uses toast** | `RestoreCurrencyAsync` uses `_toastService.ShowSuccess()` instead of modal dialog |
| **Fixed: LogSystemError discipline** | Hard-delete error path uses `HandleFailure()` (Warning) instead of `LogSystemError()` (Error) |
| **Fixed: Missing composite index** | Added composite index on `ExchangeRateHistory(CurrencyId, EffectiveDate)` for fast lookups |
| **Fixed: ExchangeRate display precision** | CurrencyEditorView XAML: OldRate/NewRate bindings use `N6` instead of `N2` |
| **Fixed: AllStaff policy** | Read endpoints now use `[Authorize(Policy = "AllStaff")]` instead of restrictive `AdminOnly` |

### Key Rules Added to AGENTS.md
- RULE-275 to RULE-280 covering transaction strategy, precision, relationship config, editor pattern, and log level discipline

---

## 🆕 What's New in v4.6.7 — InvoiceNo Int Re-addition & Accounting Foundation

| Feature | Description |
|---------|-------------|
| **InvoiceNo Back as int** | `SalesInvoice` and `PurchaseInvoice` now have `int InvoiceNo` — user-facing invoice number, separate from auto-increment `Id` PK |
| **UNIQUE per Document Type** | InvoiceNo is UNIQUE within each table — no duplicates allowed for search/return/report integrity |
| **Thread-Safe Auto-generation** | Service uses `IDocumentSequenceService.GetNextIntAsync()` with `SemaphoreSlim` lock — NOT `lastId + 1` (not thread-safe) |
| **Request DTO** | `int? InvoiceNo` in `CreateSalesInvoiceRequest` / `CreatePurchaseInvoiceRequest` — null/0 = auto-generate via DocumentSequenceService |
| **Desktop Display** | Editor ViewModels show `int InvoiceNo` field; list ViewModels display and filter by InvoiceNo |
| **Migration Added** | `20260602050426_AddInvoiceNoColumn` — adds `InvoiceNo int NOT NULL DEFAULT 0` to both tables + UNIQUE index |
| **Accounting Foundation** | Chart of Accounts (60 accounts), 7 auto-journal entry providers, Fiscal Year management, Multi-Currency (YER/USD/SAR) |
| **Build Verification** | **0 errors, 0 warnings** — all 9 projects compile clean |
| **Tests** | 411 passed, 28 failed (all 28 failures are pre-existing garbled Arabic assertion mismatches — not InvoiceNo related) |

### Key Design Decisions
- `InvoiceNo` is `int` (NOT string) — simpler, faster, no formatting overhead
- **UNIQUE** per document type — prevents confusion in search, returns, reports, and customer service
- Auto-generated via `DocumentSequenceService.GetNextIntAsync()` with `SemaphoreSlim` lock — thread-safe
- `SupplierInvoiceNo` (string?) stays on PurchaseInvoice as supplier's reference — NOT the system InvoiceNo
- `InvoicePrintDto.InvoiceNumber` (string) still exists — formatted via `InvoiceNo.ToString()` in the builder

---

## 🆕 What's New in v4.6.6 — UI Compacting (Mobile-Ready Density)

| Feature | Description |
|---------|-------------|
| **Global Styles Compacted** | Styles.xaml global tokens reduced: button heights 36→28, TextBox/ComboBox 36→28, font sizes 13→11, DataGrid row height 34→24, header fonts 20→16 |
| **Dashboard Compacted** | Spacing between KPI cards reduced 32→12px, icon padding 12→6, description font size reduced |
| **15 List Views Compacted** | All toolbar spacing reduced, search box widths narrowed from 220→160, all `Height="36"` hardcoded overrides removed from buttons, empty-state button margins 20→12px and widths 160→140px |
| **14 Editor Views Compacted** | Header/footer padding reduced by ~40%, section field spacing reduced from 12→6/8px, title fonts 18→14, footer bars reduced 24,16→12,8 |
| **PurchaseInvoiceEditorView Fixed** | **Major miss caught** — completely untouched view compacted: header 16,8→12,6, title 18→14, all field margins 12→6/8, footer 20,12→12,8, print button Padding=16,0 removed |
| **15 Reports/Settings/Inventory Views** | Filter bar compacted (ComboBox heights removed), report result padding 16,12→10,6, section margins 20→12px, all `Height=34/36` removed |
| **19 Dialogs/Shell Views** | Dialog titles 20→16, icon borders 50×50→44×44, button widths reduced (MinWidth 80-100), dialog containers shrunk ~15% |
| **MainWindow Sidebar** | Width reduced 220→200, menu item padding 5→3, brand area padding 16,20→12,12 |
| **ScreenWindow** | MinWidth/MinHeight reduced 600/400→500/350, default size 900×650→850×600 |
| **NumericKeypadControl** | Touch keys reduced MinHeight 30→28, MinWidth 40→36, FontSize 16→14 |
| **Touch-optimized views preserved** | TouchPosCartView, TouchQty buttons kept at touch-friendly sizes (32px) |
| **Future Mobile Ready** | Compact components scale better to smaller screens — all spacing/fonts now follow consistent compact token system |
| **Build Verification** | **0 errors, 0 warnings** across DesktopPWF after compacting all 63 views |

### Before (v4.6.5) — Spacious/Large
- Buttons at 36px height with 16px+ padding
- Dialog titles at 20px font size
- Form field spacing at 12-16px margins
- Toolbar/search bars with 36px buttons and 20px margins
- Empty-state buttons at 160px+ width with 20px top margin
- Sidebar at 220px width

### After (v4.6.6) — Compact/Dense
- Buttons at 28px default (style-driven) — all inline overrides removed
- Dialog titles at 16px, section headers at 14px
- Form field spacing at 6-8px margins
- Toolbar buttons at style-default 28px with 4-8px margins
- Empty-state buttons at 140px width with 12px top margin
- Sidebar at 200px width
- ~25-30% more content visible on screen at once

### Files Modified (63 total)

| Category | Files |
|----------|-------|
| **Styles** | `Resources/Styles.xaml` — global token compaction |
| **Dashboard** | `Views/Dashboard/DashboardView.xaml` |
| **List Views** | 15 files: SalesInvoices, PurchaseInvoices, Customers, Suppliers, Categories, Units, Users, Warehouses, Transfers, SalesReturns, PurchaseReturns, CustomerPayments, SupplierPayments, CashBoxes, ProductUnits |
| **Editor Views** | 14 files: Customer, Supplier, Category, Unit, Warehouse, User, ProductUnit, SalesReturn, PurchaseReturn, StockTransfer, CustomerPayment, SupplierPayment, CashBox, CashTransfer |
| **Reports/Settings** | 15 files: Reports, AccountStatement, IncomeStatement, VatReport, CashFlow, ExpiredProducts, Settings, CostingMethod, Backup, CashBoxTransactions, DailyClosure, Inventory, LowStock, Login, ProductUnitEditor |
| **Dialogs** | 9 files: Error, Warning, Success, Info, Confirmation, DeleteConfirmation, ValidationErrors, DatabaseError, FallbackError |
| **Shell** | 5 files: MainWindow, ScreenWindow, PdfPreviewWindow, SalesInvoicesView, UpdateDialog |
| **Selection** | 5 files: ProductSelection, CustomerSelection, SupplierSelection, SalesInvoiceSelection, PurchaseInvoiceSelection |
| **Controls** | 1 file: NumericKeypadControl |
| **Major Miss Fixed** | `Views/Purchases/PurchaseInvoiceEditorView.xaml` — fully compacted (18 edits) |

---

### 🆕 What's New in v4.6.3 — Architecture Alignment & Code Quality Audit

| Feature | Description |
|---------|-------------|
| **Settings Relocation** | Relocated `CostingMethodSettingsViewModel` and `CostingMethodSettingsView` to their proper architectural namespaces under ViewModels/Settings and Views/Settings |
| **API Purity for Settings** | Refactored `CostingMethodSettingsViewModel` to utilize `ISettingsApiService` via HTTP client instead of direct `ISystemSettingsRepository` (Domain/Infrastructure) reference |
| **DI Registration** | Registered `CostingMethodSettingsViewModel` as a transient service in `App.ConfigureServices` to enable dynamic dependency injection resolution |
| **MessageBox Removal** | Replaced raw unhandled exception `MessageBox.Show` call in `App.xaml.cs` with a secure fallback or proper DialogService overlay |
| **Swallowed Exceptions Fixed** | Resolved empty `catch { }` blocks in `DialogService.cs` to ensure all runtime errors are correctly logged via Serilog |
| **CS0108 Hiding Warnings Fixed** | Eliminated compiler warnings by removing shadowed `DialogService` properties in list ViewModels and using `SetDialogService()` base method |
| **Arabic Encoding Restored** | Corrected garbled Arabic string literals in `StockTransfersListViewModel.cs` and `SupplierPaymentsListViewModel.cs` |
| **Async Void Refactored** | Standardized asynchronous execution flows by wrapping critical `async void` operations in ViewModels with safe try-catch patterns |

---

## 📜 Version History

### v4.6.9 — Phase 21 Users & Permissions Complete (Current)
- **Tax Module**: Full `Tax` entity (name, rate, type percentage/fixed, `IsDefault`) with CRUD service, API controller, WPF Desktop UI (list + editor views)
- **Tax on Invoices**: `TaxId` FK on `SalesInvoice`/`PurchaseInvoice` with `DeleteBehavior.Restrict`, `SetTax()` domain method, `GetTaxAmount()` computation
- **Memory Caching**: `IMemoryCache` + `ConcurrentDictionary` key tracker for SystemSettings — 5-minute sliding expiration with `PostEvictionCallback` auto-cleanup
- **29 System Settings seeded** across 8 categories via `DbSeeder`
- **Typed helpers**: `SetStringAsync(category, key, value)`, `SetIntAsync()`, `SetBoolAsync()`, `SetDecimalAsync()` with auto-create
- **SystemSetting guard clauses**: `Create()` validates `Category` (not empty) and `DataType` (must be string/int/bool/decimal)
- **Filtered unique index IsActive guard**: All filtered indexes on soft-deletable entities include `AND [IsActive] = 1`
- **StoreSettings deprecation**: `DefaultTaxRate`, `IsTaxEnabled`, `InvoicePrefix`, `CurrencyCode` deprecated — Tax entity is source of truth
- **SystemSettingsViewModel expanded**: Added 14 missing properties — `HideTaxInSales`, `ShowExpiryInInvoices` (Sales), `HideTaxInPurchases` (Purchases), `ShowLogo`, `FooterNote`, `ThermalPrinterName`, `A4PrinterName`, `LogoPath`, `StoreTaxNumber` (Print), `LowStockAlert`, `ExpiryAlert`, `ExpiryAlertDays`, `CreditLimitAlert` (Notifications)
- **Service-level validation**: `StoreSettingsService.ValidateSystemSettings()` validates 19 bool keys + 6 int keys with ranges (CostingMethod 1-3, DecimalPlaces 0-6, StockAlertDays 1-365)
- **Tax domain methods**: Added `SetDefault()` and `ClearDefault()` with `UpdateTimestamp()` audit trail
- **Phase 18 review doc updated**: All 12 bugs marked `[FIXED]` with post-review fix table
- **RULE-291 to RULE-301**: 7 settings + 3 enhancement rules for settings module integrity
- **Build**: 0 errors, 0 warnings across all 9 projects
- **Phase 21 Users & Permissions**: 16 tasks implemented — User entity rebuild (UserStatus, passwordless creation, lockout 5 attempts, Phone/Email/Avatar/DefaultCashBoxId), Permission+RolePermission DB-backed system (33 permissions, 4 roles), AuditLog (long Id, 3 indexes, paginated browser), UserSession, AuthService (SetPassword/ChangePassword/audit trail), 10 API endpoints, 5 Desktop screens, EF Migration Phase21_UsersAndPermissions
- **RULE-305 to RULE-320**: 16 new rules covering Phase 21 Users & Permissions module
- **Tests**: 1,887 passed, 5 pre-existing failures, 70 skipped

### v4.6.8 — Currency Module Stabilization & EF Core Transaction Strategy
- **BeginTransactionAsync removed**: Removed manual transactions from CurrencyService (3 methods)
- **ExchangeRate precision (18,2)**: HasPrecision on CustomerPayment/SupplierPayment
- **JournalEntryId1 shadow FK fixed**: bare `.WithOne()` → `.WithOne(x => x.JournalEntry)`
- **CurrencyEditorViewModel RULE-229**: ValidateAsync pattern fixed, toast added, dual constructor
- **isSystem parameter**: Currency.Create() now accepts `bool isSystem`; MarkAsDeleted() guards against system currency deletion
- **includeInactive passthrough**: API service → controller → service chain now supports includeInactive query param
- **Controller 404 vs 400**: Delete, PermanentDelete, UpdateExchangeRate return 404 for NotFound
- **Filtered unique indexes**: Name/Code/IsBaseCurrency indexes filter `[IsActive] = 1`
- **OldRate validation**: ExchangeRateHistory.Create() rejects `oldRate <= 0`
- **UpdateExchangeRateRequestValidator**: Added FluentValidation for NewRate > 0
- **IsActive removed from UpdateCurrencyRequest**: Confusing unused field removed
- **IDisposable on CurrenciesListViewModel**: Prevents EventBus subscription leaks
- **CanExecute removed**: EditCommand/DeleteCommand no longer have CanExecute predicates
- **Restore uses toast**: `_toastService.ShowSuccess()` instead of modal dialog
- **LogSystemError discipline**: HandleFailure used for API business errors, not LogSystemError
- **Composite index**: ExchangeRateHistory(CurrencyId, EffectiveDate) for fast lookups
- **N6 display**: ExchangeRate bindings in XAML use N6 instead of N2
- **AllStaff policy**: Read endpoints use AllStaff, not AdminOnly
- **GetByCode + GetBaseCurrency endpoints**: New API endpoints
- **Deep code review**: 14 bugs + 3 enhancements fixed across Domain/API/Infrastructure/Desktop
- **Build**: 0 errors, 0 warnings across all 9 projects
- **AGENTS.md**: Added RULE-275 to RULE-280 + 15 new checklist items + 6 new FORBIDDEN items

### v4.6.7 — InvoiceNo Int Re-addition & Accounting Foundation
- **InvoiceNo Back as int**: SalesInvoice and PurchaseInvoice now have `int InvoiceNo` — user-facing invoice number
- **UNIQUE per type**: InvoiceNo is UNIQUE within each table — prevents confusion in search/returns/reports
- **Thread-Safe Auto-generation**: Uses `IDocumentSequenceService.GetNextIntAsync()` with `SemaphoreSlim` lock
- **Request DTOs**: `int? InvoiceNo` — null/0 means auto-generate via DocumentSequenceService
- **Desktop**: Editor VMs have `int InvoiceNo` field, list views display and filter by InvoiceNo
- **Accounting Foundation**: Chart of Accounts (60 accounts), 7 auto-journal entry providers, Fiscal Year management, Multi-Currency (YER/USD/SAR)
- **Migration**: `20260602050426_AddInvoiceNoColumn` adds column + UNIQUE index to both tables
- **Build**: 0 errors, 0 warnings across 9 projects
- **Tests**: 411 passed, 28 failed (pre-existing garbled Arabic, not InvoiceNo-related)

### v4.6.6 — UI Compacting — Mobile-Ready Density
- **Global UI Resize**: 63 views compacted by ~25-30% — more content per screen
- **Styles.xaml Tokens**: Button 36→28, font 13→11, DataGrid row 34→24
- **All List/Editor/Dialog Views**: Height=36 overrides removed, padding reduced, margins shrunk
- **PurchaseInvoiceEditorView Fixed**: Major miss — fully compacted (was completely untouched)
- **MainWindow Sidebar**: Width 220→200
- **Touch Views Preserved**: Touch-optimized controls kept at touch-friendly sizes
- **Build**: DesktopPWF 0 errors, 0 warnings

### v4.6.5 — Invoice Number Removal & Touch POS Polish
- **InvoiceNo (string) Removed**: InvoiceNo string column removed from SalesInvoice and PurchaseInvoice — auto-increment Id used temporarily as display identifier
- **NOTE**: InvoiceNo re-added as `int` in v4.6.7 (see above) — this was a temporary removal
- **GetByNumber Endpoints Removed**: Services, controllers, and API clients cleaned up
- **Touch POS Polish**: Product card layout fixed, stock validation warning, PlayWarning() sound
- **Garbled Arabic**: UTF-8 encoding fixes across multiple files
- **Build**: 12/12 projects pass with 0 errors

### v4.6.4 — Security Hardening & Code Quality
- **Rate Limiting**: Login limited to 5 attempts/15min per IP, global 100 req/min
- **User Hard-Delete Protection**: PermanentDeleteAsync always returns Failure
- **Connection String Security**: No plaintext connection strings in config files
- **Arabic Encoding Fixes**: Detected and fixed garbled Arabic across multiple ViewModels
- **Build Quality**: CS0109/CS1540 warnings eliminated, async void patterns removed
- **FallbackErrorDialog**: Thread-safe dialog for unhandled exceptions
- **FluentValidation Enhancement**: All invoice/payment/transfer requests validated

---

## 📋 What's New in Phase 22 — Chart of Accounts Module (v4.6.9+)

| Feature | Description |
|---------|-------------|
| **60-Account Hierarchy** | 4 levels: Group (L1) → Main (L2) → Sub (L3) → Detail (L4). 5 Groups, 8 Main, 20 Sub, 27 Detail. Numeric codes (4-10 digits), hierarchical by prefix. |
| **Account Entity** | `Account` with 13-param `Create()` factory method. New properties: `Level` (int, 1-10), `Description`, `ColorCode` (hex), `AllowTransactions` (L4+), `OpeningBalance`. 24 core properties total. |
| **DB Constraints** | `CHK_Account_Level_Range [Level] >= 1 AND [Level] <= 10` CHECK constraint. Self-referencing `ParentAccountId` FK with `DeleteBehavior.Restrict`. All enum properties use `.HasConversion<int>()` explicitly. |
| **System Account Protection** | L1-L2 accounts marked `IsSystemAccount = true` — `MarkAsDeleted()` and `Update()` guard against modification. `HasChildren()` prevents deletion of parent accounts with children. |
| **Two-Pass Seeder** | First pass creates L1-L2 groups with `SaveChangesAsync()`, queries back IDs, second pass creates L3-L4 with correct `ParentAccountId`. 60 accounts seeded with Arabic names, color codes, and descriptions. |
| **SystemAccountMappings** | Updated with new account codes — maps 13 system operation types (Cash, Bank, AR, AP, VAT, Capital, Sales, COGS, Inventory, Expense, Revenue, Discount, RetainedEarnings) for journal entry services. |
| **AccountDto + AccountTreeNodeDto** | `AccountDto` has computed `AccountTypeDisplay` and `LevelDisplay` for UI binding. `AccountTreeNodeDto` has recursive `Children` list for TreeView rendering. Service builds tree from flat list (no N+1 queries). |
| **FluentValidation** | `CreateAccountRequestValidator` validates code format (4-10 digits), NameAr required, ColorCode hex, OpeningBalance non-negative. `UpdateAccountRequestValidator` validates NameAr/AccountType/Level. |
| **AccountService** | 8 methods: `GetTreeAsync` (recursive tree), `GetAllAsync`, `GetByIdAsync`, `GetByTypeAsync`, `CreateAsync` (parent/level/code validation), `UpdateAsync` (system account guard), `DeleteAsync` (soft delete, children guard), `PermanentDeleteAsync` (hard delete, `DbUpdateException` catch). |
| **AccountsController** | 7 CRUD endpoints appended to existing balance/ledger controller. Policies: `AllStaff` for reads, `ManagerAndAbove` for writes, `AdminOnly` for permanent delete. Returns 404 vs 400 via `ErrorCodes.NotFound`. |
| **Dual-Mode Desktop UI** | `AccountsListView` with toggleable TreeView (`HierarchicalDataTemplate`) / DataGrid views. Search by name or code. Filter by AccountType. Search/filter works in BOTH TreeView and DataGrid modes. `AccountsListViewModel` implements `IDisposable` for EventBus, with Edit/Delete toolbar commands. |
| **AccountEditorView** | Full form: Code, NameAr, NameEn, Type (combo), Level (read-only), ColorCode, OpeningBalance, AllowTransactions, **Explanation** (new field). `INotifyDataErrorInfo` validation. Level auto-set from parent. `ValidateAsync()` uses `ClearAllErrors()` → `AddError()` → `await ValidateAllAsync()`. **Edit mode** supported — loads existing account, `AccountCode` read-only during edit. |
| **Desktop API Services** | `IAccountApiService` / `AccountApiService` — typed HTTP client with content-type guard, try-catch, Serilog logging. `AccountChangedMessage` EventBus for auto-refresh. |
| **Desktop Navigation** | Sidebar "دليل الحسابات" button + "الحسابات" menu item. `NavigateToChartOfAccountsCommand` in MainViewModel. |
| **Tests** | 18 new Domain tests for Account entity (Create guards, Update, MarkAsDeleted, IsDebitNormal, Level range). 1,906 total tests, 0 failures. Build: 0 errors, 0 warnings. |

### Phase 22 Bug Fixes (v4.6.9+)

| Bug | Fix | Layer |
|-----|-----|-------|
| **Bug-1**: `HasChildren()` domain guard on `MarkAsDeleted()` never executed — `SubAccounts` nav property not loaded by EF Core | Service now uses `AnyAsync(a => a.ParentAccountId == id)` DB query BEFORE calling `MarkAsDeleted()`. Domain guard retained as defense-in-depth. | Domain + Service |
| **Bug-2/3**: `DeleteAsync()` fetched entity twice — first to check children, second to mark deleted | `DeleteAsync()` now loads once, calls `MarkAsDeleted()` on already-loaded entity. `PermanentDeleteAsync()` uses `DeleteRange(new[] { account })` on already-loaded entity. | Service |
| **Bug-4**: `Explanation` field missing from Domain entity, EF config, DTOs, Requests, Validator, and Seeder | Added `string? Explanation` with `nvarchar(500)` config across ALL layers. All 60 seed accounts now have Arabic explanation text. | Cross-layer |
| **Bug-5**: Level-1 account codes had no special length validation | `CreateAccountRequestValidator` now enforces exactly 3 characters for Level-1 account codes. | API |
| **Bug-6**: `UpdateAccountRequestValidator` was missing `NameAr` Arabic message, `NameEn` MaxLength, `ColorCode` hex validation | Update validator now has SAME rules as Create validator. | API |
| **`:byte` route constraint**: `{type:byte}` causes HTTP 500 — ASP.NET Core has no built-in `:byte` constraint | Changed to `{type:int:min(1):max(5)}` — matches AccountType enum range (1-5). | API |
| **Health check leak**: `DatabaseHealthCheck` used raw `IConfiguration.GetConnectionString()` returning `""` (empty per RULE-040), bypassing DPAPI decryption | Rewritten to inject `SecureDbContextFactory` and call `GetDecryptedConnectionString()` — single source of truth. | Infrastructure |

| Enhancement | Description | Layer |
|-------------|-------------|-------|
| **Enh-3**: Account edit mode | AccountEditorView loads existing account data, populates fields, sets `AccountCode` read-only. | Desktop |
| **Enh-4**: Edit/Delete toolbar | AccountsListViewModel has `EditSelectedAccountCommand` + `DeleteSelectedAccountCommand` with toolbar buttons. | Desktop |
| **Enh-5**: TreeView search | Search/filter now works in BOTH TreeView and DataGrid modes — filter matching tree nodes. | Desktop |

### Key Rules (AGENTS.md §2.72-2.74)

Phase 22 added RULE-321 through RULE-340 covering:
- RULE-321: Account Level CHECK constraint (1-10 range)
- RULE-322: Account.Create() with 13 parameters including Level
- RULE-323: MarkAsDeleted guards IsSystemAccount + HasChildren
- RULE-324: Update guards system accounts
- RULE-325: Two-pass seeder approach
- RULE-326: AllowTransactions = false for levels < 4
- RULE-327: IsSystemAccount = true for L1-L2 only
- RULE-328: Color codes per AccountType
- RULE-329: AccountDto computed properties (AccountTypeDisplay, LevelDisplay)
- RULE-330: AccountTreeNodeDto recursive Children
- RULE-331: Controller policies per operation type
- RULE-332: IDisposable for EventBus ViewModels
- RULE-333: Service builds tree from flat list (no N+1)
- RULE-334: CreateAsync validation (parent, level, code uniqueness)
- RULE-335: PermanentDeleteAsync catches DbUpdateException
- RULE-336: Exactly 60 seeded accounts
- RULE-337: SystemAccountMappings updated with new IDs
- RULE-338: ValidateAsync follows ClearAllErrors → AddError → ValidateAllAsync
- RULE-339: AccountTypeDisplay uses AccountType enum (not byte)
- RULE-340: Dual-mode Desktop UI (TreeView + DataGrid toggle)

---

## 👤 What's New in Phase 21 — Users & Permissions Module (v4.6.9)

| Feature | Description |
|---------|-------------|
| **Passwordless User Creation** | Admin creates users WITHOUT a password — user sets password on first login via `MustChangePassword` flow. `User.Create()` factory method never accepts a password. |
| **UserStatus Enum** | `UserStatus` (Active=1, Inactive=2, Locked=3) replaces plain `IsActive` boolean. User overrides `MarkAsDeleted()`/`Restore()` for dual-state integrity. |
| **Account Lockout** | 5 failed login attempts locks the account (`UserStatus.Locked`). Admin-only unlock via `Unlock()` method. `RecordLoginAttempt()` tracks all attempts. |
| **Permission Entity (DB-backed)** | 33 granular permissions across 9 categories (Sales, Purchases, Inventory, Customers, Suppliers, Products, Reports, Accounting, System, Operations, Audit). `IsSystem` flag protects system permissions. |
| **RolePermission Join Entity** | Many-to-many mapping between `UserRole` and `Permission`. `PermissionService.UpdateRolePermissionsAsync()` uses `ExecuteTransactionAsync()` for atomic updates. |
| **4-Role Model** | Admin (1), Accountant (2), Cashier (3), Observer (4) — replaces old Admin/Manager/Cashier. Every role has precisely defined permissions. |
| **AuditLog System** | High-volume audit logging with `long Id` (bigint). Indexed on `(UserId, Timestamp)`, `(EntityType, EntityId)`, `(Timestamp)`. `AuditLogService` supports paginated/filtered queries. |
| **UserSession Tracking** | `UserSession` entity tracks JWT sessions with `TokenHash`, `ExpiresAt`, and `IsActive`. Foundation for future session management. |
| **AuthService Enhancements** | `SetPasswordAsync()` for first-login password set, `ChangePasswordAsync()` with current password verification, login audit trail (Success/Failed/Locked), `MustChangePassword` redirect. |
| **33 Seed Permissions** | `DbSeeder` seeds 33 permissions with 4-role assignments. Default admin seeded passwordless (`PasswordHash = null`, `MustChangePassword = true`). |
| **New API Endpoints** | `POST /auth/set-password`, `POST /auth/change-password`, `GET /users/current`, `POST /users/{id}/reset-password`, `GET /audit-logs`, `GET /audit-logs/user/{id}`, `GET /audit-logs/login-history`, `GET /permissions`, `GET /permissions/roles`, `PUT /permissions/roles/{role}` |
| **Desktop Permission Management** | `PermissionManagementView` with 4 role tabs, category expanders with checkboxes, grouped permission display. Changes saved via API. |
| **Desktop AuditLog Browser** | `AuditLogListView` with paginated DataGrid, filters by action/entity/date range, user history and login history views. |
| **Desktop Password Change** | `PasswordChangeScreen` with 3 fields (current, new, confirm), `INotifyDataErrorInfo` validation, AuthService API integration. |
| **Enhanced User Editor** | `UserEditorView` with avatar display (80×80), Phone/Email fields, role selector, status badge. |
| **StatusBar Enhancements** | MainWindow StatusBar shows user avatar, role badge (Arabic), change password link, logout button. Permission-based navigation visibility. |

### Key Rules (AGENTS.md §2.71)

Phase 21 added RULE-305 through RULE-320 covering:
- RULE-305: Passwordless User.Create() — no password in factory method
- RULE-306: UserStatus enum replaces IsActive with EF query filter
- RULE-307: RecordLoginAttempt() for all login attempts
- RULE-308: SetInitialPassword() guards MustChangePassword
- RULE-309: Permission.IsSystem protects system permissions
- RULE-310: AuditLog uses long Id (bigint)
- RULE-311: AuditLog indexes for performance
- RULE-312: All FK Restrict on new entities
- RULE-313: MustChangePassword checked before password verification
- RULE-314: SetPasswordAsync validates MustChangePassword
- RULE-315: ChangePasswordAsync verifies current password
- RULE-316: Every login creates AuditLog entry
- RULE-317: PermissionService uses ExecuteTransactionAsync
- RULE-318: Desktop uses API-based permission checks
- RULE-319: DbSeeder seeds 33 permissions with 4-role assignments
- RULE-320: Admin user seeded passwordless

---

## 🤝 Contributing

This project uses AI-assisted development with strict architectural rules. Before contributing:

1. Read [`AGENTS.md`](AGENTS.md) — all 352 non-negotiable rules (RULE-001 to RULE-352)
2. Read [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md) — financial and transaction rules
3. Follow the pre-submission checklist in AGENTS.md §9

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Made with ❤️ for small retail shops
</p>