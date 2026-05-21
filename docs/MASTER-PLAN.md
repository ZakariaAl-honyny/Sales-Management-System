# MASTER-PLAN — Sales Management System (v4.4 Production)

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

## 📊 Test Coverage

| Test Project | Target | Status |
|-------------|--------|--------|
| **SalesSystem.Domain.Tests** | Domain entities, guard clauses, business rules | ✅ Active |
| **SalesSystem.Application.Tests** | Service logic, Result<T> patterns | ✅ Active |
| **SalesSystem.Infrastructure.Tests** | EF Core mappings, repositories, migrations | ✅ Active |
| **SalesSystem.Api.Tests** | Controller endpoints, validation, auth | ✅ Active |
| **SalesSystem.Integration.Tests** | End-to-end flows, API + DB integration | ✅ Active |

---

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

## 🔗 Cross-Reference Guide

| Topic | File |
|-------|------|
| **All rules (LAW)** | `AGENTS.md` |
| **Financial formulas** | `docs/CONSTITUTION.md` |
| **Full requirements** | `docs/PRD-MVP-v3.0.md` |
| **Database schema** | `docs/database-schema.md` |
| **UI/UX flows** | `docs/ui-screens.md` |
| **Security details** | `.opencode/agent/security-auditor.md` |
| **Print specs** | `specs/006-printing/plan.md` |
| **Code patterns** | `.opencode/agent/implement-agent.md` |
| **Implementation plan** | `docs/MASTER-PLAN.md` (this file) |
| **Costing & UOM specs** | `docs/CONSTITUTION.md` sections 2.24–2.27 |

---

## 📝 Version History

| Version | Date | Description |
|---------|------|-------------|
| v4.4 | 2026-05-21 | Production release — Auto-Update, DPAPI, Backup, Windows Service, Installer |
| v4.3 | 2026-05-15 | Print engine (QuestPDF + Win32), Dynamic UOM, Costing strategy, Cash boxes, Price history |
| v4.2 | 2026-05-10 | Delete strategy, Defensive programming, WPF dialogs, Toast notifications, Real-time validation |
| v4.1 | 2026-05-05 | Wholesale/Retail pricing, Unit conversion in Domain |
| v4.0 | 2026-05-01 | Clean Architecture rewrite — 6 projects, Service Layer, Result<T> |
| v3.0 | 2026-04-15 | Initial architecture — PRD-MVP-v3.0 |
