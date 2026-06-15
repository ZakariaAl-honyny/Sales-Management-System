# Implementation Plan: Desktop Modules

**Branch**: `005-desktop-modules` | **Date**: 2026-06-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/005-desktop-modules/spec.md`

---

## Summary

Replace the shell placeholder screens in `SalesSystem.DesktopPWF` with fully functional WPF modules that communicate with the ASP.NET Core API via typed `HttpClient` services. Each module follows consistent MVVM patterns: a **List View** (ObservableCollection, filtered/sorted DataGrid, newest-first display, EventBus auto-refresh, entity-changed cross-module communication) and an **Editor View** (INotifyDataErrorInfo real-time validation, ValidateAllAsync pre-save dialog, non-modal ScreenWindowService hosting). All modules adhere to the strict Clean Architecture rule that the Desktop NEVER connects to the database directly — all data flows through `HttpClient` to the API. Interactive validation ensures buttons are ALWAYS enabled (no CanExecute predicates); validation happens on click with styled warning dialogs. Every interactive control has an Arabic ToolTip explaining its action.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10-windows
**Primary Dependencies**: WPF (.NET 10-windows), `IHttpClientFactory`, `System.Text.Json`, `ClosedXML` (Excel export)
**Storage**: SQL Server via ASP.NET Core API (Desktop never connects directly)
**Testing**: Manual integration testing against running API; unit tests for ViewModels with mocked API services
**Target Platform**: Windows 10/11 desktop (WPF)
**Project Type**: WPF desktop application (MVVM with View/ViewModel split per module)
**Performance Goals**: List screens load in <2s | Invoice post <2s | Reports <5s | Editor open <500ms
**Constraints**: RTL Arabic UI; JWT auth; role-based screen visibility; no direct DB access from Desktop; INotifyDataErrorInfo everywhere; no CanExecute on buttons; ToolTips on ALL controls
**Scale/Scope**: 8 modules, ~50 ViewModel files, ~50 View files, ~30 ApiService files, ~30 message types

---

## Constitution Check

| Gate | Rule | Status | Notes |
|------|------|--------|-------|
| Decimal-only money | RULE-001 | ✅ PASS | All DTOs use `decimal`. Desktop computes display-only previews. |
| Domain-computed formulas | RULE-002 | ✅ PASS | Desktop sends raw inputs; API/Domain computes totals. |
| Transactional integrity | RULE-003 | ✅ PASS | Desktop is read-only caller; transactions are API-side. |
| Invoice lifecycle | RULE-019-021 | ✅ PASS | UI shows status badges (Draft/Posted/Cancelled). Buttons gated per state. |
| Stock integrity | RULE-010 | ✅ PASS | Desktop calls POST then POST /post; API handles stock validation. |
| Result pattern | RULE-006 | ✅ PASS | All `IXxxApiService` methods return `Result<T>`. |
| Clean architecture | RULE-007 | ✅ PASS | Desktop → HttpClient → API; no direct DB. |
| Security | RULE-038-039 | ✅ PASS | JWT stored in-memory; all calls include Bearer token via DelegatingHandler. |
| Four-layer validation | RULE-059 | ✅ PASS | Desktop uses INotifyDataErrorInfo real-time. API has FluentValidation. Domain has guard clauses. DB has CHECK constraints. |
| Serilog logging | RULE-035-036 | ✅ PASS | `LogSystemError()` for system errors. `HandleFailure()` logs at Warning for user errors. |
| EF Core conventions | RULE-016 | N/A | Desktop has no EF Core. |
| Audit trail | RULE-016 | ✅ PASS | `CreatedByUserId` sent via JWT claim server-side. |
| EventBus | RULE-012-013, 034 | ✅ PASS | Subscribe in constructor, unsubscribe in Dispose/Cleanup. ID-only messages. |
| Interactive validation | RULE-059 | ✅ REQUIRED | Save buttons ALWAYS enabled. Validate on click. Warning dialog lists all errors. ToolTips explain rules. |
| INotifyDataErrorInfo | RULE-228-230 | ✅ REQUIRED | No `HasXxxError` booleans. `AddError`/`ClearErrors` in property setters. ErrorTemplate with red border + ❗ icon. |
| DeleteStrategy | RULE-050-051 | ✅ REQUIRED | `ShowDeleteConfirmationAsync` returns Cancel/Deactivate/Permanent. |
| ToolTips | RULE-185-190 | ✅ REQUIRED | ALL buttons, MenuItems, ListBoxItems have Arabic ToolTips. Action descriptions, not label repeats. |
| Newest-first | RULE-220-222 | ✅ REQUIRED | Lists sorted `OrderByDescending(x => x.Id)` or by date. |
| DialogService | RULE-054-055, 160-161 | ✅ REQUIRED | Editors open non-modally via `ScreenWindowService`. No `MessageBox.Show`. |
| SetDialogService | RULE-227 | ✅ REQUIRED | Every Editor VM constructor calls `SetDialogService()`. |
| ValidateAllAsync | RULE-229 | ✅ REQUIRED | Pre-save `ClearAllErrors()` → `AddError()` per field → `await ValidateAllAsync()`. |
| RTL Arabic | RULE-249-252 | ✅ REQUIRED | All strings valid UTF-8 Arabic. ToolTips, labels, messages in Arabic. |

**No Constitution violations detected.**

---

## Module Architecture

### Module Organization

The MainWindow sidebar navigation organizes the 8 primary modules and supporting screens:

```text
MainWindow Sidebar (Width=200, Arabic RTL, Role-Based Visibility)
├── لوحة التحكم (Dashboard)                          → All roles
├── المبيعات (Sales)                                  → AllStaff
│   ├── فواتير البيع (Sales Invoices)
│   ├── عروض الأسعار (Sales Quotations)
│   └── مردودات المبيعات (Sales Returns)
├── المشتريات (Purchases)                             → ManagerAndAbove
│   ├── فواتير المشتريات (Purchase Invoices)
│   ├── أوامر الشراء (Purchase Orders)
│   └── مردودات المشتريات (Purchase Returns)
├── المستودعات (Warehouses & Inventory)               → ManagerAndAbove
│   ├── المنتجات (Products)
│   ├── الوحدات (Units of Measure)
│   ├── التصنيفات (Categories)
│   ├── الأصناف (Inventory Items)
│   ├── حركة المخزون (Inventory Movements)
│   ├── تدوير المخزون (Stock Transfers)
│   └── انتهاء الصلاحية (Expired Products)
├── العملاء والموردين (Parties)                       → AllStaff
│   ├── العملاء (Customers)
│   └── الموردين (Suppliers)
├── المدفوعات (Payments)                              → AllStaff
│   ├── مقبوضات العملاء (Customer Payments)
│   └── مدفوعات الموردين (Supplier Payments)
├── الحسابات (Accounting)                             → ManagerAndAbove
│   ├── دليل الحسابات (Chart of Accounts)
│   ├── قيود اليومية (Journal Entries)
│   ├── الخزائن (Cash Boxes)
│   ├── العملات (Currencies)
│   └── السنوات المالية (Fiscal Years)
├── التقارير (Reports)                                → ManagerAndAbove
│   ├── قائمة الدخل (Income Statement)
│   ├── الميزانية (Balance Sheet)
│   ├── تقارير المبيعات (Sales Reports)
│   ├── تقارير المشتريات (Purchase Reports)
│   ├── كشف حساب (Account Statement)
│   └── (18+ report types)
├── الإعدادات (Settings)                               → AdminOnly
│   ├── إعدادات النظام (System Settings)
│   ├── الضرائب (Taxes)
│   ├── المستخدمين (Users)
│   ├── الصلاحيات (Permissions)
│   ├── النسخ الاحتياطي (Backup)
│   └── التحديثات (Updates)
└── (Sidebar Footer) اسم المستخدم | تسجيل خروج
```

### Standard Module File Layout

Every module follows a consistent file structure. Example for Products:

```text
ViewModels/Products/
├── ProductListViewModel.cs            ← List screen logic
├── ProductEditorViewModel.cs          ← Add/Edit editor logic
├── ProductSelectionViewModel.cs       ← Lookup selection dialog
├── ProductPricesListViewModel.cs      ← Sub-list for multi-currency pricing
├── ProductPriceEditorViewModel.cs     ← Sub-editor for price records
├── ProductUnitsListViewModel.cs       ← Sub-list for units
├── ProductUnitEditorViewModel.cs      ← Sub-editor for unit records
├── ProductImportViewModel.cs          ← Bulk import
├── ProductImagesViewModel.cs          ← Image management
├── BillOfMaterialsListViewModel.cs    ← BOM list
├── BillOfMaterialEditorViewModel.cs   ← BOM editor
├── AssemblyProductionViewModel.cs     ← Assembly production

Views/Products/
├── ProductsListView.xaml / .cs        ← List DataGrid
├── ProductEditorView.xaml / .cs       ← Editor form
├── ProductSelectionView.xaml / .cs    ← Lookup dialog
├── ProductPricesView.xaml / .cs       ← Multi-currency pricing list
├── ProductPriceEditorView.xaml / .cs  ← Price editor
├── ProductUnitsListView.xaml / .cs    ← Unit list
├── ProductUnitEditorView.xaml / .cs   ← Unit editor
├── ProductImagesView.xaml / .cs       ← Image manager
├── BillOfMaterialsListView.xaml / .cs ← BOM list
├── BillOfMaterialEditorView.xaml / .cs← BOM editor
├── AssemblyProductionView.xaml / .cs  ← Assembly production control
├── ProductImportView.xaml / .cs       ← Import dialog
└── UnitHierarchyBuilderControl.xaml/.cs← Visual unit hierarchy

Services/Api/
├── IProductApiService.cs              ← Interface
├── ProductApiService.cs               ← Implementation (typed HttpClient)
```

---

## Key Design Decisions

### Decision 1: List ViewModel Pattern

Every CRUD list ViewModel follows a consistent structure rooted in the standard module pattern. A representative `ProductListViewModel` illustrates the pattern used across all 14+ list ViewModels:

| Element | Implementation | Rules |
|---------|---------------|-------|
| Collection | `ObservableCollection<ProductDto>` bound to `DataGrid.ItemsSource` via `ICollectionView` | RULE-220 |
| Sorting | `OrderByDescending(x => x.Id)` applied on load — newest items appear first | RULE-221 |
| Search | `SearchText` property with `ICollectionView.Filter` lambda; debounced via `UpdateSourceTrigger=PropertyChanged` | — |
| LoadedCommand | `AsyncRelayCommand` wrapping `ExecuteAsync(LoadDataAsync)` — fires on View load | RULE-141 |
| AddCommand | `RelayCommand` (sync) — opens editor via `ScreenWindowService.OpenScreen()` or `DialogService.ShowDialog()` | RULE-160 |
| EditCommand | `RelayCommand` (sync) — opens editor with selected item data | RULE-160 |
| DeleteCommand | `AsyncRelayCommand` — calls `ShowDeleteConfirmationAsync` → API delete → EventBus publish | RULE-050–051 |
| RestoreCommand | `AsyncRelayCommand` — calls API restore, shows toast on success | RULE-289 |
| RefreshCommand | `AsyncRelayCommand` — reloads data from API | — |
| EventBus subscription | Subscribe in constructor, unsubscribe in `Dispose()` via `Cleanup()` | RULE-012, RULE-289 |
| Error display | `ErrorMessage` property bound to error TextBlock; `HandleFailure()` transforms API errors | RULE-172 |
| Empty state | `IsEmpty` boolean computed from collection count; shows "➕ إضافة أول [entity]" button | RULE-189 |

**Commands that open editors are NEVER AsyncRelayCommand** — they are `RelayCommand` (sync) because the `ScreenWindowService.OpenScreen()` is a fire-and-forget dispatcher call that does not await a result. Only commands that perform HTTP operations (Load, Delete, Restore) use `AsyncRelayCommand`.

### Decision 2: Editor ViewModel Pattern

Every editor ViewModel follows a dual-constructor pattern (parameterless for design-time/App.GetService, DI constructor for tests) and implements `INotifyDataErrorInfo` with `ValidateAllAsync` pre-save validation. A representative `ProductEditorViewModel` illustrates the pattern:

| Element | Implementation | Rules |
|---------|---------------|-------|
| Constructor 1 | Parameterless: `App.GetService<T>()` for each dependency | — |
| Constructor 2 | Full DI: all interfaces injected with `ArgumentNullException` guards | — |
| SetDialogService | Called in every constructor: `SetDialogService(dialogService)` | RULE-227 |
| IsEditMode | `bool` set from constructor parameter — determines "إضافة" vs "تعديل" | — |
| LoadDataAsync | Fetches entity by ID in edit mode; populates dropdowns (products, customers, etc.) | — |
| Property validation | Property setters call `ValidateField()` — never `HasXxxError` booleans | RULE-228 |
| ValidateAsync | `ClearAllErrors()` → `AddError()` for each field → `await ValidateAllAsync()` | RULE-229 |
| SaveAsync | Wrapped in `ExecuteAsync(SaveOperationAsync)` — the operation calls `ValidateAsync()` first | RULE-141 |
| Success feedback | `IToastNotificationService.ShowSuccess("تم الحفظ بنجاح")` + `ISoundService.PlaySuccess()` | RULE-056, RULE-231 |
| Close on success | `RequestClose()` delegates to ScreenWindowService/ DialogService | — |
| EventBus publish | Publishes `EntityChangedMessage` on successful save for cross-module refresh | RULE-034 |
| Cleanup() | Virtual dispose: unsubscribes EventBus + disposes any IDisposable fields | RULE-012 |
| DialogResult | `bool?` property for modal dialog result support | — |

**Property validation example** — no `HasXxxError` booleans:

```csharp
public string Name
{
    get => _name;
    set
    {
        if (SetProperty(ref _name, value))
        {
            ValidateField(() => !string.IsNullOrWhiteSpace(value), nameof(Name), "اسم المنتج مطلوب");
            ValidateField(() => value.Length <= 200, nameof(Name), "اسم المنتج لا يتجاوز 200 حرف");
        }
    }
}
```

**Pre-save validation example** — validate on click, never block button:

```csharp
// CORRECT — button always enabled, validate on click
SaveCommand = new AsyncRelayCommand(SaveAsync);   // NO CanExecute predicate

private async Task SaveAsync()
{
    if (!await ValidateAsync()) return;     // ValidateAllAsync shows warning dialog
    await ExecuteAsync(SaveOperationAsync);  // wrapped in IsBusy + error handling
}
```

### Decision 3: IPC — Desktop Never Connects to Database

All data flows through typed `HttpClient` services registered with `Microsoft.Extensions.Http`. The Desktop has zero references to Entity Framework, `SqlConnection`, or any database driver. The contract is purely REST + JSON.

```text
Desktop ViewModel → IXxxApiService (interface)
                         ↓
                   XxxApiService (typed HttpClient)
                         ↓
                   HTTP Request (JSON, Bearer token)
                         ↓
                   ASP.NET Core API Controller
                         ↓
                   Application Service → Infrastructure → SQL Server
```

**IApiService base pattern** — content-type guard before JSON parse:

```csharp
public abstract class ApiServiceBase
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;
    protected readonly ISessionService _session;

    protected async Task<Result<T>> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
            return result != null
                ? Result<T>.Success(result)
                : Result<T>.Failure("استجابة فارغة من الخادم");
        }

        // Content-Type guard — prevent JsonException on HTML/empty 404 bodies (RULE-184)
        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            var errorJson = await response.Content.ReadAsStringAsync(ct);
            // Parse error response...
        }
        else
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Non-JSON error response ({StatusCode}): {Body}", 
                (int)response.StatusCode, body);
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => Result<T>.Failure("العنصر غير موجود", ErrorCodes.NotFound),
            HttpStatusCode.Unauthorized => Result<T>.Failure("انتهت الجلسة. يرجى تسجيل الدخول مجدداً"),
            HttpStatusCode.Conflict => Result<T>.Failure("العنصر مكرر أو مستخدم في مكان آخر"),
            _ => Result<T>.Failure("حدث خطأ في الخادم")
        };
    }
}
```

### Decision 4: DialogService.ShowDialog for Editor Opening

Editors open via `IDialogService.ShowDialog(viewModel)` or `IScreenWindowService.OpenScreen(viewModel, options)`. The `ShowDialog` method resolves the View by naming convention:

| Strategy | Method | When to Use |
|----------|--------|-------------|
| Modal | `_dialogService.ShowDialog(vm)` | Quick add/edit sub-dialogs (Category manager, Unit manager, Unit price editor) |
| Non-modal | `_screenWindowService.OpenScreen(vm, options)` | Primary editors (Product editor, Invoice editor, Customer editor) |

The `DialogService.ShowDialog()` resolves `XxxEditorViewModel` → `XxxEditorView` by replacing "ViewModel" with "View" in the FullName. It binds `CloseRequested` event → `Window.Close()` and returns `DialogResult` from the ViewModel.

```csharp
// CORRECT — open editor via ScreenWindowService (non-modal)
private void EditProduct()
{
    if (SelectedProduct == null) return;
    var vm = new ProductEditorViewModel(SelectedProduct.Id);
    _screenWindowService.OpenScreen(vm, new ScreenWindowOptions
    {
        Title = "تعديل المنتج",
        OnClosed = (closedVm) =>
        {
            _eventBus.Publish(new ProductChangedMessage(SelectedProduct.Id));
            _ = LoadDataAsync();  // Auto-refresh list
        }
    });
}
```

### Decision 5: DeleteStrategy — Three-Option Deletion

All delete operations use the `DeleteStrategy` enum (RULE-050–051) with the styled `DeleteConfirmationDialog`:

| Strategy | Value | Behavior | API Endpoint |
|----------|-------|----------|--------------|
| `Cancel` | 0 | Abort — do nothing | (none) |
| `Deactivate` | 1 | Soft delete — sets `IsActive = false` | `DELETE /api/v1/{entity}/{id}` |
| `Permanent` | 2 | Hard delete — physical removal | `DELETE /api/v1/{entity}/permanent/{id}` |

```csharp
// CORRECT delete pattern
private async Task DeleteProductAsync()
{
    if (SelectedProduct == null) return;
    
    var strategy = await _dialogService.ShowDeleteConfirmationAsync($"المنتج: {SelectedProduct.Name}");
    if (strategy == DeleteStrategy.Cancel) return;
    
    await ExecuteAsync(async () =>
    {
        Result result;
        if (strategy == DeleteStrategy.Deactivate)
            result = await _productService.DeleteAsync(SelectedProduct.Id);
        else  // Permanent
            result = await _productService.DeletePermanentlyAsync(SelectedProduct.Id);
        
        if (result.IsSuccess)
        {
            _eventBus.Publish(new ProductChangedMessage(SelectedProduct.Id));
            _toastService.ShowSuccess(strategy == DeleteStrategy.Deactivate
                ? "تم تعطيل المنتج بنجاح"
                : "تم حذف المنتج بشكل نهائي");
        }
        else
        {
            await _dialogService.ShowErrorAsync("خطأ في الحذف", result.Error ?? "فشلت عملية الحذف");
        }
    });
}
```

### Decision 6: Interactive Validation — Always-Enabled Buttons

Buttons are NEVER disabled due to validation state (RULE-059). All commands omit `CanExecute` predicates. Validation happens on click, with clear feedback:

| Rule | Implementation |
|------|---------------|
| Button enabled | `SaveCommand = new AsyncRelayCommand(SaveAsync)` — no `CanExecute` parameter |
| Validate on click | `SaveAsync()` calls `await ValidateAsync()` (which shows warning dialog) |
| Warning dialog | `ValidationErrorsDialog` lists ALL missing/incorrect fields with bullet points |
| Field markers | Required fields marked with `*` in XAML label (`اسم المنتج *`) |
| ToolTips | Each input field has `ToolTip` explaining its validation rule |
| Unique field explanation | Barcode, username, email have explicit uniqueness explanation in ToolTip |
| Sound feedback | `ISoundService.PlayError()` on validation failure (RULE-232) |
| Auto-focus | `FocusFirstInvalidFieldRequested` event → View focuses the first error field after dialog dismisses |

**XAML pattern** — always enabled, ToolTips explain rules:

```xml
<TextBlock Text="اسم المنتج *" Style="{StaticResource LabelStyle}"/>
<TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"
         ToolTip="أدخل اسم المنتج — هذا الحقل إلزامي ولا يتجاوز 200 حرف"
         Style="{StaticResource ModernTextBox}"/>

<TextBox Text="{Binding Barcode}"
         ToolTip="الباركود — يجب أن يكون فريداً لكل منتج"
         Style="{StaticResource ModernTextBox}"/>
<TextBlock Text="الباركود يجب أن يكون فريداً — لا يمكن تكرار نفس الرمز لمنتجين مختلفين" 
           Style="{StaticResource HelperTextStyle}"/>

<Button Command="{Binding SaveCommand}" Style="{StaticResource PrimaryButton}"
        ToolTip="حفظ المنتج — سيتم فتح نافذة تأكيد في حال وجود أخطاء"
        Content="حفظ"/>
```

### Decision 7: Arabic ToolTips on ALL Interactive Controls

Following RULE-185–190, every interactive control (Button, MenuItem, ListBoxItem, ComboBox, CheckBox, RadioButton) MUST have an Arabic `ToolTip`. ToolTips are action-oriented and descriptive:

| Control | Example ToolTip | Notes |
|---------|----------------|-------|
| Add button | "فتح شاشة إضافة منتج جديد" | Describes destination (RULE-186) |
| Edit button | "تعديل بيانات المنتج المحدد" | Never just "تعديل" |
| Delete button | "حذف المنتج المحدد — مع إمكانية التعطيل أو الحذف النهائي" | Explains options (RULE-187) |
| Save button | "حفظ التغييرات — سيتم التحقق من صحة البيانات أولاً" | Explains validation flow |
| Post button | "ترحيل الفاتورة نهائياً — سيتم تحديث المخزون والرصيد" | Explains consequences (RULE-187) |
| Cancel button | "إلغاء وإغلاق النافذة — لن يتم حفظ أي تغييرات" | Explains no-save consequence |
| Navigation item | "عرض وإدارة فواتير البيع" | Describes destination (RULE-188) |
| Empty-state button | "إضافة أول منتج في النظام" | Critical first-use guidance (RULE-189) |
| Error dismiss "✕" | "إخفاء رسالة الخطأ" | Standard dismiss guidance (RULE-190) |

### Decision 8: Newest-First Sorting

All list ViewModels sort items newest-first (RULE-220–222). This ensures the most recently created/updated records appear at the top, which matches user expectations for invoice and transaction lists:

| Entity Type | Sort Key | Example | 
|-------------|----------|---------|
| Master data (Products, Customers, etc.) | `OrderByDescending(x => x.Id)` | Products list shows newest product first |
| Invoices | `OrderByDescending(x => x.InvoiceDate)` | Sales invoices by date descending |
| Payments | `OrderByDescending(x => x.Id)` | Payments newest first |
| Transactions | `OrderByDescending(x => x.CreatedAt)` | Cash transactions by timestamp |

Applied in the `LoadDataAsync()` method immediately after API response:

```csharp
private async Task LoadDataAsync()
{
    var result = await ExecuteResultAsync(() => _productService.GetAllAsync(_includeInactive));
    if (result != null)
    {
        Products = new ObservableCollection<ProductDto>(
            result.OrderByDescending(x => x.Id)   // ← Newest-first sort
        );
        ProductsView = CollectionViewSource.GetDefaultView(Products);
        ProductsView.Filter = FilterPredicate;
    }
}
```

---

## Implementation Order

### Phase 5A — ApiService Infrastructure (Blocking)

Must complete before any module work to establish the typed HTTP client pattern.

1. **5A-01**: Establish `IApiService.cs` base interface + common `HandleResponseAsync<T>()` with content-type guard
2. **5A-02**: Create all API service interfaces (`IProductApiService`, `ICustomerApiService`, `ISupplierApiService`, `IWarehouseApiService`, `ISalesInvoiceApiService`, `IPurchaseInvoiceApiService`, `ISalesReturnApiService`, `IPurchaseReturnApiService`, `IStockTransferApiService`, `ICustomerPaymentApiService`, `ISupplierPaymentApiService`, `IReportApiService`, `IDashboardApiService`, `ICategoryApiService`, `IUnitApiService`, `ISettingsApiService`, `ITaxesApiService`, `ICashBoxApiService`, `ICurrencyApiService`, `IAccountApiService`, `IJournalEntryApiService`, `IUserApiService`, `IPermissionApiService`, `IAuditLogApiService`, `IProductUnitApiService`, `IProductPriceApiService`, `IInventoryBatchApiService`, `IPurchaseOrderApiService`, `ISalesQuotationApiService`, `IBackupApiService`, `IUpdateApiService`)
3. **5A-03**: Implement all API service classes (each wrapping typed `HttpClient` with `Result<T>` return)
4. **5A-04**: Create `AppMessages.cs` with all EventBus message types (one `*ChangedMessage` per entity)
5. **5A-05**: Register all typed HttpClients and API services in `App.xaml.cs` DI
6. **5A-06**: Register all ViewModels in DI (transient for editors, singleton for lists)

### Phase 5B — Foundation Modules (P1, Dependency Order)

7. **5B-01**: **Products** — `ProductsListView`, `ProductEditorView`, `ProductSelectionView`, `ProductUnitsListView`, `ProductUnitEditorView`, `ProductPricesView`, `ProductPriceEditorView`, `ProductImagesView`, `BillOfMaterialsListView`, `BillOfMaterialEditorView`, `AssemblyProductionView`, `ProductImportView`, `UnitHierarchyBuilderControl`. Includes multi-currency pricing (ProductPrices table), unit-of-measure management (ProductUnits + UnitBarcodes), BOM, and product images.
8. **5B-02**: **Units** — `UnitsListView`, `UnitEditorView`
9. **5B-03**: **Categories** — `CategoriesListView`, `CategoryEditorView`
10. **5B-04**: **Customers** — `CustomersListView`, `CustomerEditorView`, `CustomerSelectionView`. Customer group lookup, credit limit display.
11. **5B-05**: **Suppliers** — `SuppliersListView`, `SupplierEditorView`, `SupplierSelectionView`
12. **5B-06**: **Warehouses** — `WarehousesListView`, `WarehouseEditorView`, `InventoryBatchesView`
13. **5B-07**: **Taxes** — `TaxesListView`, `TaxEditorView`

### Phase 5C — Invoice Modules (P1)

14. **5C-01**: **Sales Invoices** — `SalesInvoicesListView`, `SalesInvoiceEditorView` (header: customer, warehouse, date, payment type; line items grid with product barcode scanning; totals panel with tax toggle; status badge; save draft/post/cancel actions)
15. **5C-02**: **Sales Quotations** — `SalesQuotationsListView`, `SalesQuotationEditorView`
16. **5C-03**: **Purchase Invoices** — `PurchaseInvoicesListView`, `PurchaseInvoiceEditorView` (header: supplier, warehouse, date; multi-currency; landed cost via AdditionalCharge; line items; totals panel)
17. **5C-04**: **Purchase Orders** — `PurchaseOrdersListView`, `PurchaseOrderEditorView`

### Phase 5D — Returns & Transfers (P2)

18. **5D-01**: **Sales Returns** — `SalesReturnsListView`, `SalesReturnEditorView`
19. **5D-02**: **Purchase Returns** — `PurchaseReturnsListView`, `PurchaseReturnEditorView`
20. **5D-03**: **Stock Transfers** — `StockTransfersListView`, `StockTransferEditorView`
21. **5D-04**: **Inventory Operations** — `InventoryOperationListView`, `InventoryOperationEditorView`

### Phase 5E — Payments (P2)

22. **5E-01**: **Customer Payments** — `CustomerPaymentsListView`, `CustomerPaymentEditorView` (multi-invoice distribution, cheque management)
23. **5E-02**: **Supplier Payments** — `SupplierPaymentsListView`, `SupplierPaymentEditorView`

### Phase 5F — Accounting & System Modules (P2)

24. **5F-01**: **Chart of Accounts** — `AccountsListView` (dual-mode TreeView + DataGrid), `AccountEditorView`
25. **5F-02**: **Journal Entries** — `JournalEntriesListView`, `JournalEntryEditorView`
26. **5F-03**: **Cash Boxes** — `CashBoxesListView`, `CashBoxEditorView`, cash transaction log
27. **5F-04**: **Currencies** — `CurrenciesListView`, `CurrencyEditorView`, `CurrencyRatesView`
28. **5F-05**: **Fiscal Years** — `FiscalYearsListView`, `FiscalYearEditorView`

### Phase 5G — Reports & Dashboard (P3)

29. **5G-01**: **Reports** — `ReportsView.xaml` (central report selector) + 18+ report views: IncomeStatement, BalanceSheet, TrialBalance, GeneralLedger, AccountStatement, SalesByCustomer, SalesByProduct, SalesByCategory, DailySales, PurchasesBySupplier, PurchasesByProduct, StockBalance, WarehouseMovement, ExpiredProducts, VatReport, CashFlow, CashBoxSummary, DailyClosure, LoginHistory, UserActivity. Excel export via ClosedXML.
30. **5G-02**: **Dashboard** — `DashboardView` with summary cards (today's sales, purchases, cash balance, low stock alerts). Auto-refresh via EventBus subscriptions.

### Phase 5H — Settings & Admin (P3)

31. **5H-01**: **System Settings** — `SettingsView`, `SystemSettingsView`, `CostingMethodSettingsView`
32. **5H-02**: **Users** — `UsersListView`, `UserEditorView`, `SetPasswordView`, `PasswordChangeView`
33. **5H-03**: **Permissions** — `PermissionsListView`, `PermissionManagementView`
34. **5H-04**: **Audit** — `AuditLogView`, `LoginHistoryView`
35. **5H-05**: **Backup** — `BackupView` (manual backup/restore)
36. **5H-06**: **Updates** — `UpdateDialog` (auto-update download progress)

---

## Complexity Tracking

| Concern | Status |
|---------|--------|
| Constitution violations | ✅ None |
| Clean Architecture violation | ✅ None — Desktop never connects to DB |
| EventBus memory leaks | ✅ Prevented — `IDisposable` + `Cleanup()` on all list ViewModels |
| CanExecute blocking | ✅ NONE — all buttons always enabled |
| INotifyDataErrorInfo coverage | ✅ All 14+ Editor VMs use `AddError`/`ClearErrors`/`ValidateAllAsync` |
| ToolTip coverage | ✅ ALL interactive controls — verified at code review |
| Arabic encoding | ✅ All strings valid UTF-8 — checked via `file --mime-encoding` at commit |
| Dialog self-ownership | ✅ `GetActiveWindow()` checks `owner != window` |
| ScreenWindowService leak | ✅ WeakReference<Window> — no strong references |
| Newest-first sorting | ✅ All list VMs use `OrderByDescending` |
| DeleteStrategy | ✅ All delete operations use `ShowDeleteConfirmationAsync` with 3 options |
| Toast for minor success | ✅ Delete/restore confirmations use toast, not modal dialogs |
