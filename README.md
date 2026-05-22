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
  <img src="https://img.shields.io/badge/Status-In%20Development-FFA500?style=for-the-badge" alt="Status"/>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/License-MIT-green.svg?style=flat-square" alt="License"/>
  <img src="https://img.shields.io/badge/Version-v4.6-blue.svg?style=flat-square" alt="Version"/>
  <img src="https://img.shields.io/badge/Language-Arabic%20%2B%20English-orange.svg?style=flat-square" alt="Language"/>
</p>

---

## 📋 Overview

A **comprehensive sales management platform** built with Clean Architecture + CQRS principles, designed for small-to-medium retail businesses. The current version delivers a **WPF Desktop client (MVVM)** backed by an **ASP.NET Core Web API**, handling sales, purchases, inventory, returns, and financial tracking with full Arabic/English bilingual support.

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
| 📒 Full accounting system | Future | Current MVP handles sales/purchase ledgers only |
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
| **System** | SystemSettings (costing method), StoreSettings, DocumentSequences, InventoryMovements, SystemLog |

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
| [`docs/PRD-MVP-v3.0.md`](docs/PRD-MVP-v3.0.md) | Full product requirements document |
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

## 🤝 Contributing

This project uses AI-assisted development with strict architectural rules. Before contributing:

1. Read [`AGENTS.md`](AGENTS.md) — all 218 non-negotiable rules (RULE-001 to RULE-219)
2. Read [`docs/CONSTITUTION.md`](docs/CONSTITUTION.md) — financial and transaction rules
3. Follow the pre-submission checklist in AGENTS.md §9

---

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

<p align="center">
  Made with ❤️ for small retail shops
</p>