# Implementation Plan: Identifier Strategy & Validation (v4.5.3–v4.6.2)

**Branch**: `013-identifier-validation` | **Date**: 2026-06-13 | **Spec**: [spec.md](./spec.md)

---

## Summary

This feature eliminates the legacy `Code` string identifier from all master data entities (Product, Customer, Supplier, Warehouse) across the full stack — Domain, Infrastructure, Application, API, and Desktop. The auto-increment `Id` (int PK) becomes the sole identifier. Concurrently, it removes the deprecated `UnitBarcode`/`ProductBarcode` table and moves the barcode directly to the `Products` table as `varchar(50)`. It modernizes the WPF Desktop validation framework by implementing `INotifyDataErrorInfo` in `ViewModelBase`, applying a standardized red-border ErrorTemplate, and replacing silent validation failures with an interactive aggregated error dialog on save. Finally, it standardizes `InvoiceNo` as an `int` (unique per document type, generated thread-safely via `DocumentSequenceService`) and establishes strict FluentValidation rules with Arabic error messages across all request DTOs.

---

## Technical Context

**Language/Version**: C# 13 / .NET 10 LTS (EF Core, ASP.NET Core, WPF)
**Architecture Scope**: Full Stack — every layer from database to desktop UI
**Storage**: SQL Server 2019+ via EF Core Fluent API
**Testing**: xUnit + Moq + FluentAssertions (existing)
**Constraints**:
- `Code` columns must be dropped via EF Core migrations without data loss on `Id` (no rollback needed)
- The `UnitBarcode` / `ProductBarcode` table must be removed entirely; barcode moves to `Products.Barcode` (varchar(50))
- `DocumentSequenceService` is restricted to financial documents only (SalesInvoice, PurchaseInvoice, SalesReturn, PurchaseReturn, CustomerReceipt, SupplierPayment) — never for master data
- `InvoiceNo` for SalesInvoice and PurchaseInvoice is `int`, UNIQUE per table, never nullable in the entity
- All 14 editor ViewModels must migrate from manual `HasXxxError` boolean + computed string validation blocks to `INotifyDataErrorInfo` with `AddError`/`ClearErrors`
- Action buttons must ALWAYS remain enabled — validate on click with `ClearAllErrors` → `AddError` → `ValidateAllAsync`
- `DuplicateCode` error constant is removed from `ErrorCodes`; replaced by `DuplicateBarcode`
- Arabic error messages for ALL validation failures across FluentValidation and Domain guards

---

## Constitution Check

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Decimal Precision | ✅ N/A | No financial math changes |
| II | Domain Formulas | ✅ N/A | No formula changes |
| III | Transactional Integrity | ✅ N/A | No transaction changes |
| IV | Invoice Lifecycle | ✅ N/A | No document state changes |
| V | Stock Integrity | ✅ N/A | No stock logic changes |
| VI | Result Pattern | ✅ PASS | API continues returning `Result<T>` |
| VII | Architecture Boundaries | ✅ PASS | Domain remains pure; UI validation stays in WPF |
| VIII | Security | ✅ N/A | No security changes |
| IX | Four-Layer Validation | ✅ PASS | Aligns UI validation with Domain + Service + API + DB layers |
| X | Logging | ✅ N/A | No logging changes |
| XI | EF Core Conventions | ✅ PASS | `Code` columns dropped via standard Fluent API migration |
| XII | Audit Trail | ✅ N/A | No audit changes |
| XIII | Delete Strategy | ✅ N/A | Soft delete unchanged |
| XIV | Defensive Programming | ✅ PASS | Domain guards remain; UI pre-validation prevents bad requests |
| XV | WPF Dialogs | ✅ PASS | Mandates `IDialogService.ShowWarningAsync` for aggregated validation errors |
| XVI | Toast Notifications | ✅ N/A | Unchanged |
| XVII | Real-Time UI Validation | ✅ PASS | Directly implements RULE-058 (INotifyDataErrorInfo) and RULE-059 (always-enabled buttons) |

**Gate Result**: ✅ ALL CLEAR — No violations.

---

## Identifier Strategy: No Code Column

### Entities Affected

**Product** — The `Code` property is removed entirely. Identification is by `Id` (int PK) or `Barcode` (varchar(50), unique filtered when not null and IsActive). Barcode moves from the deprecated `ProductBarcode`/`UnitBarcode` table to the `Products` table directly, stored as `varchar(50)` (ASCII-only, not nvarchar). The database maintains a filtered unique index: `WHERE Barcode IS NOT NULL AND IsActive = 1`.

**Customer** — The `Code` column is removed. Customers are identified by `Id` (int PK) or `Name` (nvarchar(200)). The `AccountId` FK (linking to Chart of Accounts) becomes the financial reference — not a customer-level code. Phone numbers are validated via regex `^05\d{8}$`.

**Supplier** — The `Code` column is removed. Suppliers are identified by `Id` or `Name`. Same phone regex validation as customers.

**Warehouse** — The `Code` column is removed. Warehouses are identified by `Id` or `Name`, plus optional `Phone` and `Address`.

### Search and Filter Impact

Every search UI across lists and editors changes to filter by `Id` (int) or `Name` (string) — never by Code. The API endpoints accept `?search=term` which matches against `Name.Contains(term)` or `Id == int.Parse(term)`. The Desktop search TextBox ToolTips explain this: "ابحث بالاسم أو رقم المعرف (Id)".

### DTO and Error Code Cleanup

- `ProductResponse`, `CustomerResponse`, `SupplierResponse`, `WarehouseResponse` DTOs have NO Code field
- `ErrorCodes.DuplicateCode` constant is deleted; all references replaced with `ErrorCodes.DuplicateBarcode`
- `DocumentSequenceService` is no longer called for master data entities (Product, Customer, Supplier, Warehouse) — those never used sequence generation anyway in the current architecture

---

## InvoiceNo Strategy: int, Unique, Thread-Safe

### InvoiceNo on SalesInvoice and PurchaseInvoice

`SalesInvoice.InvoiceNo` and `PurchaseInvoice.InvoiceNo` are `int` properties — never string, never nullable in the entity. They represent the user-facing invoice number and are UNIQUE per table (database-level UNIQUE constraint).

### Generation via DocumentSequenceService

The `DocumentSequenceService.GetNextIntAsync(documentType, ct)` method uses a `SemaphoreSlim` lock to ensure thread-safe incrementing:
1. Lock is acquired
2. Read current `NextNumber` from `DocumentSequences` table for the given document type key (e.g., `"SalesInvoice"`, `"PurchaseInvoice"`)
3. Increment the `NextNumber` and save
4. Return the old `NextNumber`

This guarantees no two invoices get the same number, even under concurrent creation from multiple ScreenWindow instances.

### Request DTOs

`CreateSalesInvoiceRequest` and `CreatePurchaseInvoiceRequest` use `int? InvoiceNo` — when null or `<= 0`, the service auto-generates via `DocumentSequenceService`. If a user provides a specific number, the service validates uniqueness before saving.

### SupplierInvoiceNo Distinction

`PurchaseInvoice.SupplierInvoiceNo` (string?, nullable) is preserved as the supplier's own invoice reference number — this is NOT the system `InvoiceNo`. It is an optional field on the PurchaseInvoice entity for tracking the supplier's paperwork, stored as `nvarchar(100)`.

---

## WPF Validation Framework: INotifyDataErrorInfo

### ViewModelBase Changes

`ViewModelBase` implements `INotifyDataErrorInfo` directly, providing:
- `AddError(string propertyName, string errorMessage)` — registers an error for a property
- `ClearErrors(string propertyName)` — removes errors for a property
- `ClearAllErrors()` — clears all property errors (called before re-validation)
- `ValidateAllAsync()` — checks if `HasErrors` is true; if so, shows the aggregated warning dialog with `IDialogService.ShowWarningAsync()` listing all errors
- `SetDialogService(IDialogService)` — must be called in every editor ViewModel constructor to enable `ValidateAllAsync()`

### ErrorTemplate in Styles.xaml

A new `ControlTemplate` for `Validation.ErrorTemplate` is applied to TextBox, PasswordBox, and ComboBox. The template renders:
1. An `AdornedElementPlaceholder` (the original control)
2. A red border (`BorderBrush=Red`, `BorderThickness=2`) around the control
3. A ❗ icon overlay at the top-right corner, with `ToolTip` bound to `AdornedElementPlaceholder.ValidationErrors[0].ErrorContent`

This replaces the old pattern of separate `HasNameError` boolean + `NameError` string TextBlock below each field. Validation happens in property setters via `AddError`/`ClearErrors`, and red borders appear as the user types (real-time, not just on save).

### Interactive Validation Pattern (Buttons Always Enabled)

All action buttons (Save, Post, Cancel, Delete) use `AsyncRelayCommand` with NO `CanExecute` predicate — they are always clickable. When the user clicks Save, the ViewModel:
1. Calls `ClearAllErrors()` to reset all validation state
2. Runs validation logic: checks each required field, calls `AddError(propertyName, message)` for each violation
3. Calls `await ValidateAllAsync()` — if errors exist, the aggregated warning dialog appears listing all missing/incorrect fields in Arabic
4. If validation passes, proceeds with the save operation

This gives the user immediate, aggregate feedback without ever disabling buttons (which users find confusing and frustrating).

### Unique Field Explanations

For fields with uniqueness constraints (Barcode, UserName), a helper text is shown below the input: "الباركود يجب أن يكون فريداً — لا يمكن تكرار نفس الرمز لمنتجين مختلفين". This is in addition to the red border validation on the field itself, which says "الباركود موجود مسبقاً" when a duplicate is detected.

---

## FluentValidation Rules

### Phone Validation (Customer and Supplier)
- Regex: `^05\d{8}$`
- Arabic error: "رقم الجوال يجب أن يبدأ بـ 05 ويتكون من 10 أرقام"
- Applied in both `CreateCustomerRequestValidator`, `UpdateCustomerRequestValidator`, and their supplier equivalents

### Email Validation
- `RuleFor(x => x.Email).EmailAddress()` with Arabic error: "البريد الإلكتروني غير صحيح"
- Applied on all party contact email fields

### Barcode Validation
- `MaximumLength(50)` with Arabic error: "الباركود لا يتجاوز 50 حرفاً"
- Applied in `CreateProductRequestValidator` and `UpdateProductRequestValidator`

### InvoiceNo Validation
- `GreaterThan(0)` with Arabic error: "رقم الفاتورة يجب أن يكون أكبر من صفر"
- Applied in invoice create/update validators when `InvoiceNo` is provided (non-null)

---

## Four-Layer Validation Enforcement

| Layer | Mechanism | Example |
|-------|-----------|---------|
| Domain | Guard clauses in entity constructors and factory methods | `if (string.IsNullOrWhiteSpace(name)) throw new DomainException("الاسم مطلوب")` |
| Application | Pre-condition checks in service methods | `if (await _repo.ExistsByBarcodeAsync(barcode)) return Result.Failure("الباركود موجود مسبقاً")` |
| API | FluentValidation on request DTOs | `RuleFor(x => x.Phone).Matches("^05\\d{8}$")` with Arabic message |
| Database | CHECK constraints and filtered unique indexes | `UNIQUE(Barcode) WHERE Barcode IS NOT NULL AND IsActive = 1` |

---

## Project Structure

```text
SalesSystem/
├── SalesSystem.Domain/
│   └── Entities/
│       ├── Product.cs                        ← REMOVE Code property; Barcode stays as varchar(50)
│       ├── Customer.cs                       ← REMOVE Code property
│       ├── Supplier.cs                       ← REMOVE Code property
│       ├── Warehouse.cs                      ← REMOVE Code property
│       └── ProductBarcode.cs                 ← DELETE entire file
├── SalesSystem.Infrastructure/
│   ├── Configurations/
│   │   ├── ProductConfiguration.cs           ← UPDATE: remove Code, move barcode config
│   │   ├── CustomerConfiguration.cs          ← UPDATE: remove Code
│   │   ├── SupplierConfiguration.cs          ← UPDATE: remove Code
│   │   ├── WarehouseConfiguration.cs         ← UPDATE: remove Code
│   │   └── ProductBarcodeConfiguration.cs    ← DELETE
│   └── Data/
│       └── SalesDbContext.cs                 ← REMOVE ProductBarcode DbSet
├── SalesSystem.Contracts/
│   ├── Requests/                             ← UPDATE all create/update requests: remove Code
│   └── Responses/                            ← UPDATE all response DTOs: remove Code
├── SalesSystem.Application/
│   └── Services/                             ← REMOVE all Code assignment/sync logic
├── SalesSystem.Api/
│   ├── Controllers/                          ← UPDATE: search by Name or Id only
│   └── Validators/                           ← UPDATE: add phone/barcode/email regex rules
└── SalesSystem.DesktopPWF/
    ├── ViewModels/
    │   ├── Base/ViewModelBase.cs              ← ADD INotifyDataErrorInfo, ValidateAllAsync, SetDialogService
    │   └── Editors/                           ← UPDATE all 14 VMs: remove HasXxxError booleans
    └── Views/
        └── Resources/Styles.xaml              ← ADD Validation.ErrorTemplate (red border + ❗ icon)
```

---

## Verification Checklist

- [ ] Product, Customer, Supplier, Warehouse entities have NO Code property at any layer
- [ ] `ProductBarcode` / `UnitBarcode` tables and entities are completely removed
- [ ] `Products.Barcode` is `varchar(50)` with filtered unique index (`WHERE Barcode IS NOT NULL AND IsActive = 1`)
- [ ] All DTOs, requests, and responses exclude Code fields
- [ ] `DuplicateCode` constant deleted from `ErrorCodes`
- [ ] All search/filter uses `Id` (int) or `Name` (string) — never Code
- [ ] `SalesInvoice.InvoiceNo` and `PurchaseInvoice.InvoiceNo` are `int`, UNIQUE per table
- [ ] `DocumentSequenceService.GetNextIntAsync()` is thread-safe (SemaphoreSlim)
- [ ] `SupplierInvoiceNo` preserved as nullable string (supplier's reference only)
- [ ] All 14 editor VMs use `INotifyDataErrorInfo` with `AddError`/`ClearErrors` (no `HasXxxError` booleans)
- [ ] `ViewModelBase` implements `INotifyDataErrorInfo` with `ValidateAllAsync()` and `SetDialogService()`
- [ ] `ErrorTemplate` renders red border + ❗ icon with error ToolTip
- [ ] All action buttons remain enabled — validate on click with aggregated warning dialog
- [ ] Phone validated via `^05\d{8}$`, email via `.EmailAddress()`, barcode via `MaxLength(50)`
- [ ] All validation error messages are in Arabic
- [ ] Four-layer validation: Domain guards + Service checks + FluentValidation + DB constraints
- [ ] Build: 0 errors, 0 warnings
