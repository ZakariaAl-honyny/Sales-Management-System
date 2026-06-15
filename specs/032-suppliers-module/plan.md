# Phase 32 — Suppliers Module Implementation Plan

> **Version**: 1.0
> **Phase**: 32 (after Reports, before Products)
> **Depends on**: Phase 22 (Chart of Accounts — parent "2100 — حسابات الموردين" seeded), Phase 19 (AccountCategories seeded)
> **Scope**: Parties, Suppliers, SupplierContacts
> **Pattern**: Mirrors Phase 23 (Customers Module) — same Party pattern, same Account auto-creation philosophy

---

## 1. Summary

This phase implements the complete Suppliers sub-system: the **Party** pattern (shared contact data reused from Customers), **Supplier** management with automatic Chart of Accounts integration under parent "2100 — حسابات الموردين", and **Supplier Contacts** (multiple contact persons per supplier).

Suppliers follow the same philosophy as Customers: **كل مورد = حساب**. Every supplier IS a GL account. When a supplier is created, the service automatically creates a Level-4 detail account under "2100 — حسابات الموردين" — the Accounts Payable parent node.

The Suppliers table (1.4) is intentionally lean: no `CreditLimit`, no `OpeningBalance`, no `CurrentBalance`, no `SupplierType`, no `PaymentTerms`. These are either deferred to V2 or handled elsewhere:
- **CreditLimit**: Not stored on Supplier entity. If needed, the purchase invoice flow validates per-invoice limits via `Supplier.CheckCreditLimit(decimal additionalAmount)` — a non-throwing `bool` method.
- **Balance**: Lives on the linked **Account** via journal entries (not on Supplier entity).
- **SupplierType**: Deferred to V2 — not in V1 scope.
- **PaymentTerms**: Per-invoice on `PurchaseInvoice`, not on Supplier entity.

---

## 2. Key Entities

### 2.1 Party (جدول الأطراف — Table 1.1)

Shared entity holding common contact data used by Customers (Phase 23) and Suppliers. Uses `ActivatableEntity` base (soft-deletable via `IsActive`).

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int PK | Shared PK with Supplier (1:1) |
| `Name` | nvarchar(200) | Required |
| `Phone` | nvarchar(30) | Optional |
| `Email` | nvarchar(100) | Optional |
| `Address` | nvarchar(300) | Optional |
| `TaxNumber` | nvarchar(50) | Optional |
| `Notes` | nvarchar(500) | Optional |
| `IsActive` + audit fields | | ActivatableEntity |

**Key decisions**:
- Party holds NO `AccountId` — AccountId is on Supplier entity directly (difference from Customers plan v1; database-schema.md table 1.1 has no AccountId on Party).
- Party is shared between Customer and Supplier — both link via `PartyId` FK.

### 2.2 Supplier (جدول الموردين — Table 1.4)

Supplier-specific fields. Uses **shared primary key** pattern: `Supplier.Id = Party.Id` (PK is also FK to Parties).

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int PK = FK → Parties | Shared PK with Party (1:1) |
| `PartyId` | int FK → Parties | Shared party data — redundant if `Id = PartyId` (denormalized for query convenience) |
| `AccountId` | int FK → Accounts | **Every supplier = a GL account** — auto-created |
| `CategoryId` | int? FK → AccountCategories | Optional — supplier classification |
| `IsActive` + audit | | ActivatableEntity |

**Key decisions**:
- **No CreditLimit on Supplier entity** — CreditLimit validation is per-invoice in PurchaseInvoice flow. The `CheckCreditLimit()` domain method can exist on Supplier but returns `bool` (non-throwing, soft warning).
- **No OpeningBalance, No CurrentBalance** — balance lives on the linked Account via journal entries. Supplier entity has no balance fields.
- **No SupplierType in V1** — deferred to V2. All suppliers are treated equally in V1. Cash/Credit decisions are per-invoice (`PurchaseInvoice.PaymentType`).
- **No PaymentTerms** — handled per-invoice on `PurchaseInvoice.DueDate`, not on Supplier entity.
- **AccountId is mandatory** (non-nullable int) — auto-created by service, NEVER user-supplied in requests.

### 2.3 SupplierContact (جهات اتصال المورد — Table 1.5)

Multiple contact persons per supplier. Strictly informational — no login, no permissions.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int PK | |
| `SupplierId` | int FK → Suppliers | Required |
| `Name` | nvarchar(150) | Required |
| `Phone` | nvarchar(30) | Optional |
| `Email` | nvarchar(100) | Optional |
| `Position` | nvarchar(100) | Job title — Optional |
| `Notes` | nvarchar(300) | Optional |
| `IsActive` + audit | | ActivatableEntity |

---

## 3. Business Rules

### 3.1 Party Rules (shared with Customers)

- **RULE-P1**: `Name` required (non-empty, trimmed).
- **RULE-P2**: `Party` is created as part of Supplier creation — not standalone.
- **RULE-P3**: Soft delete via `IsActive = false`. Permanent delete allowed only if no linked PurchaseInvoices, SupplierPayments, or JournalEntryLines exist.

### 3.2 Supplier Rules

- **RULE-S1**: `Id` must equal the `Party.Id` — shared PK pattern enforced at creation (service creates Party first, gets its Id, then creates Supplier with same Id).
- **RULE-S2**: `AccountId` is mandatory (non-nullable int) — auto-created by `SupplierService`, NEVER user-supplied in requests. The `CreateSupplierRequest`/`UpdateSupplierRequest` DTOs MUST NOT have an `AccountId` field.
- **RULE-S3**: `CategoryId` is optional (nullable FK to AccountCategories).
- **RULE-S4**: `CheckCreditLimit(decimal additionalAmount)` returns `bool` — non-throwing. The caller (purchase invoice service) decides whether to block or warn. Implementation: queries the linked Account's current balance via journal entries and compares against the invoice total + existing balance. Returns `false` if exceeding.
- **RULE-S5**: `Update()` calls `UpdateTimestamp()` at the end — audit trail required.
- **RULE-S6**: Soft delete via `MarkAsDeleted()`. Permanent delete guarded by checking `PurchaseInvoices`, `SupplierPayments`, `PurchaseReturns`, `JournalEntryLines` for references. Returns clear Arabic error message when blocking.
- **RULE-S7**: Phone validation regex `^05\d{8}$` (Saudi/Yemeni mobile format) with Arabic error message. Email uses `.EmailAddress()` FluentValidation rule.
- **RULE-S8**: No `SupplierType`, `SupplierGroup`, or `PaymentTerms` on Supplier entity in V1.

### 3.3 SupplierContact Rules

- **RULE-SC1**: `Name` required (non-empty, trimmed).
- **RULE-SC2**: `SupplierId` must reference an active supplier.
- **RULE-SC3**: Contacts are soft-deletable (`IsActive`).
- **RULE-SC4**: Contacts are strictly informational — no login, no permissions.

---

## 4. Auto-Account Pattern (كل مورد = حساب)

### Design Philosophy

Every supplier IS a GL account. This mirrors the Customer philosophy exactly:

```
إنشاء مورد = إنشاء حساب محاسبي تلقائي تحت "2100 — حسابات الموردين"
```

### Account Auto-Creation Sequence

When `SupplierService.CreateAsync()` is called:

1. Create a **Level-4 detail Account** under parent `"2100 — حسابات الموردين"` (Accounts Payable)
2. `Account.Nature` = `Liability` (2) — suppliers are liabilities (we owe them money)
3. `Account.AccountCode` = next numeric child of 2100 (e.g., 2101, 2102, 2103...) — generated via `SELECT MAX(CAST(AccountCode AS INT)) + 1` under parent 2100
4. `Account.IsLeaf` = `true` — detail account can receive journal entry lines
5. `Account.IsSystem` = `false` — user can modify if needed (non-system)
6. Create **Party** record with the supplier's Name, Phone, Email, Address, TaxNumber, Notes
7. Create **Supplier** record with `Id = Party.Id`, `AccountId = Account.Id`, `CategoryId` (if provided)

### Account Code Generation Strategy

```sql
-- Pseudo-code: find next available code under parent 2100
SELECT ISNULL(MAX(CAST(a.AccountCode AS INT)), 2100) + 1
FROM Accounts a
WHERE a.ParentId = @parentAccountId
  AND a.IsActive = 1
```

For safety, this query is wrapped in a retry loop (collision guard).

### Integration with Accounting Service

When `OpeningBalance > 0` is provided during Supplier creation (future feature — not in V1 scope):
- The service calls `AccountingIntegrationService.CreateSupplierOpeningEntryAsync(supplierAccountId, openingBalance, currencyId, ct)`
- Creates journal entry: Dr OpeningBalanceEquity / Cr Supplier Account
- This integration point is reserved for Phase 24+ automation

---

## 5. UI Screens

### 5.1 Suppliers List View (شاشة قائمة الموردين)

| Element | Behavior |
|---------|----------|
| DataGrid columns | Name (from Party), Phone, Email, TaxNumber, AccountName (read-only, from linked Account), Status (Active/Inactive) |
| Sorting | Newest-first by `Id` descending (per RULE-220) |
| Search | By Name or Phone (API-side filtering) |
| Toolbar | Add, Edit, Delete, Restore, Refresh |
| Context menu | Edit, Deactivate/Activate, Delete Permanently (if no references), View Statement |
| Empty state | "لا يوجد موردون — أضف أول مورد" with Add button |
| Toggle inactive | Checkbox "عرض الموردين غير النشطين" |

**Key UI decision**: No `CurrentBalance` column on the list view — balance is computed from linked Account. A "View Statement" button opens the supplier's account statement instead.

### 5.2 Supplier Editor View (شاشة إضافة/تعديل مورد)

| Section | Fields |
|---------|--------|
| بيانات المورد | Name* (required), Phone, Email, Address, TaxNumber, Notes |
| التصنيف | CategoryId dropdown (from AccountCategories — optional) |
| معلومات النظام | AccountName (read-only TextBox, shown after save with label "الحساب المحاسبي") |

**Key UI decisions**:
- `AccountId` is **NEVER shown or editable** in the UI — auto-created server-side.
- `CategoryId` is a simple dropdown populated from `AccountCategories` API. If no categories exist, hide the field.
- No OpeningBalance input field (deferred to V2 integration with Accounting service).
- No CreditLimit input field (not stored on Supplier entity — per-invoice check in PurchaseInvoice).
- No SupplierType, PaymentTerms, or SupplierGroup fields (deferred).
- ⓘ ToolTip explaining the auto-account concept: "كل مورد ينشئ حساباً محاسبياً تلقائياً تحت الموردون — 2100"
- Save button ALWAYS enabled (no CanExecute) — `ValidateAllAsync()` on click per RULE-229.

### 5.3 Supplier Contacts Sub-Screen

Inline editable DataGrid within the Supplier Editor (or expander section):

| Column | Type | Notes |
|--------|------|-------|
| Name* | TextBox | Required |
| Phone | TextBox | Optional |
| Email | TextBox | Optional |
| Position | TextBox | Optional |
| Notes | TextBox | Optional |
| Actions | Add/Edit/Delete buttons | Delete via confirm dialog |

Add button appends a new row, Edit makes the row editable in-place, Delete removes with soft-delete confirmation.

### 5.4 Supplier Statement View (شاشة كشف حساب المورد)

Opened from the Suppliers List context menu (button "كشف حساب"): **(Phase 31/24 integration — deferred to when accounting automation is in place)**

- From/To date range filter
- Columns: Date, Document Type (فاتورة شراء / سند صرف / مرتجع مشتريات), Document No, Debit, Credit, Running Balance
- Data sourced from Journal Entries linked to the supplier's AccountId
- Export to Excel button (via ClosedXML)

---

## 6. Seed Data

### Default Supplier "مورد نقدي"

```text
Party.Name = "مورد نقدي"
Party.IsActive = true

Account.AccountCode = next available under 2100 (e.g., 2101)
Account.Name = "مورد نقدي"
Account.Nature = Liability (2)
Account.IsLeaf = true
Account.IsSystem = false

Supplier.PartyId = Party.Id
Supplier.AccountId = Account.Id
Supplier.CategoryId = null
```

This matches the "عميل نقدي" pattern from Phase 23. The default supplier is used when a purchase invoice doesn't specify a supplier (cash purchases).

**Where**: `SalesSystem.Infrastructure/Data/DbSeeder.cs` — in the same seed method that creates the default customer.

---

## 7. Tasks (Ordered Implementation)

### Task 1: Ensure Party Entity is Complete & Shared

- Verify `Party` entity has all fields from schema 1.1: `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `Notes`, `IsActive` + audit fields.
- Ensure `PartyConfiguration` is properly mapped (indices on `Name`, `Phone`).
- Ensure Party is used by both Customer and Supplier — no duplication of contact fields.
- **Files**: `SalesSystem.Domain/Entities/Party.cs`, `SalesSystem.Infrastructure/Data/Configurations/PartyConfiguration.cs`
- **Verification**: Build passes, Party is referenced by Customer and Supplier entities.

### Task 2: Supplier Entity Alignment

- Ensure `Supplier` entity matches schema 1.4 exactly: `Id`, `PartyId`, `AccountId`, `CategoryId`, `IsActive` + audit.
- Ensure `Supplier.Create()` accepts: `id` (int, shared PK from Party), `partyId` (int), `accountId` (int, required), `categoryId` (int?, optional), `createdByUserId` (int).
- Ensure `Supplier.Update()` accepts `accountId` + `categoryId`.
- Add `CheckCreditLimit()` domain method returning `bool` (non-throwing, default implementation returns `true`).
- Ensure no `CreditLimit`, `OpeningBalance`, `CurrentBalance`, `SupplierType`, `PaymentTerms` fields exist.
- **Files**: `SalesSystem.Domain/Entities/Supplier.cs`
- **Verification**: Entity matches schema, no extra fields.

### Task 3: SupplierContact Entity

- Ensure `SupplierContact` entity matches schema 1.5: `Id`, `SupplierId`, `Name`, `Phone`, `Email`, `Position`, `Notes`, `IsActive` + audit.
- Ensure `SupplierContact.Create()` validates `Name` non-empty.
- **Files**: `SalesSystem.Domain/Entities/SupplierContact.cs`
- **Verification**: All fields aligned.

### Task 4: Fluent API Configurations

- **SupplierConfiguration**: `PartyId` FK Restrict + unique index (1:1 with Parties), `AccountId` FK Restrict + unique index (1:1 with Accounts), `CategoryId` FK Restrict (nullable), global `HasQueryFilter(x => x.IsActive)`.
- **SupplierContactConfiguration**: `SupplierId` FK Restrict, `Name` max 150, `Phone` max 30, `Email` max 100, `Position` max 100, `Notes` max 300.
- **Files**: `SalesSystem.Infrastructure/Data/Configurations/SupplierConfiguration.cs`, `SupplierContactConfiguration.cs`
- **Verification**: EF Core migration generates correct schema.

### Task 5: Contracts — DTOs and Requests

- `SupplierDto`: `Id`, `PartyId`, `AccountId`, `AccountName` (string? — from Account.Name), `CategoryId`, `CategoryName` (string?), `Name`, `Phone`, `Email`, `Address`, `TaxNumber`, `Notes`, `IsActive`, `CreatedAt`.
- `SupplierContactDto`: `Id`, `SupplierId`, `Name`, `Phone`, `Email`, `Position`, `Notes`.
- `CreateSupplierRequest`: `Name` (required), `Phone`, `Email`, `Address`, `TaxNumber`, `Notes`, `CategoryId` (int? optional). **NO AccountId**.
- `UpdateSupplierRequest`: Same as Create. **NO AccountId**.
- `CreateSupplierContactRequest` / `UpdateSupplierContactRequest`.
- **Files**: `SalesSystem.Contracts/DTOs/Suppliers/`, `SalesSystem.Contracts/Requests/Suppliers/`
- **Verification**: All DTOs exclude AccountId from request.

### Task 6: FluentValidation Rules

- `CreateSupplierRequestValidator`: `Name` not empty (Arabic message), `Phone` regex `^05\d{8}$` (Arabic error), `Email` `.EmailAddress()`, `MaxLength` on all string fields.
- `UpdateSupplierRequestValidator`: Same rules as Create — NEVER less strict.
- `CreateSupplierContactRequestValidator`: `Name` required, `Phone` regex, `Email` validation.
- **Files**: `SalesSystem.Api/Validators/SupplierRequestsValidator.cs`
- **Verification**: Validation tests pass for valid/invalid inputs.

### Task 7: Supplier Service — CRUD with Auto-Account

- `ISupplierService` interface: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` (soft), `PermanentDeleteAsync`, `RestoreAsync`.
- `SupplierService.CreateAsync()`:
  1. Look up parent account `"2100"` by account code — fallback to `SystemAccountMappings.AccountsPayableAccountId`.
  2. Generate next account code (`MAX + 1` under parent 2100).
  3. Create Account (Level 4, Liability, leaf=true, IsSystem=false).
  4. Create Party record.
  5. Create Supplier with `AccountId` and `PartyId`.
  6. Return `Result<SupplierDto>`.
- `SupplierService.PermanentDeleteAsync()`: Check `PurchaseInvoices`, `SupplierPayments`, `PurchaseReturns`, `JournalEntryLines` for references. If any found, return `Result.Failure("لا يمكن حذف المورد لوجود عمليات مرتبطة به")`. Catch `DbUpdateException` and return Result.Failure.
- All methods return `Result<T>` or `Result` — never throw.
- **Files**: `SalesSystem.Application/Services/SupplierService.cs`, `SalesSystem.Application/Interfaces/ISupplierService.cs`
- **Verification**: Full CRUD with auto-account creation verified via tests.

### Task 8: Supplier Contact Service

- `ISupplierContactService`: `GetBySupplierIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`.
- All methods return `Result<T>`.
- **Files**: `SalesSystem.Application/Services/SupplierContactService.cs`, `SalesSystem.Application/Interfaces/ISupplierContactService.cs`
- **Verification**: Full CRUD.

### Task 9: Suppliers API Controller

- `SuppliersController`: `GET /api/v1/suppliers`, `GET /api/v1/suppliers/{id}`, `POST /api/v1/suppliers`, `PUT /api/v1/suppliers/{id}`, `DELETE /api/v1/suppliers/{id}` (soft), `DELETE /api/v1/suppliers/permanent/{id}`, `PATCH /api/v1/suppliers/{id}/restore`.
- Auth: `[Authorize]` on controller, `AllStaff` on reads, `ManagerAndAbove` on writes (per Permissions Matrix: Suppliers CRUD = Admin/Accountant = ManagerAndAbove policy).
- 404 vs 400 differentiation: `NotFound` when entity not found, `BadRequest` for business validation errors.
- Controller delegates to `ISupplierService` — NO business logic, NO direct DbContext.
- `SupplierContactsController`: `GET /api/v1/suppliers/{supplierId}/contacts`, `POST`, `PUT`, `DELETE`.
- **Files**: `SalesSystem.Api/Controllers/SuppliersController.cs`, `SalesSystem.Api/Controllers/SupplierContactsController.cs`
- **Verification**: All endpoints return correct status codes.

### Task 10: Desktop — Suppliers List ViewModel & View

- `SuppliersListViewModel`: `INotifyDataErrorInfo`, `IDisposable`, EventBus subscription (`SupplierChangedMessage`).
- DataGrid: Name, Phone, Email, Status. Newest-first sort.
- Search by Name/Phone.
- "Toggle inactive" checkbox.
- Add opens `SupplierEditorViewModel` via `ScreenWindowService.OpenScreen()`.
- Edit opens same VM in edit mode.
- Delete: `ShowDeleteConfirmationAsync()` → soft delete (default) or permanent (if no references).
- ToolTips on all buttons (per RULE-185).
- **Files**: `SalesSystem.DesktopPWF/ViewModels/Suppliers/SuppliersListViewModel.cs`, `Views/Suppliers/SuppliersListView.xaml`
- **Verification**: List loads, search works, add/edit opens editor non-modally.

### Task 11: Desktop — Supplier Editor ViewModel & View

- `SupplierEditorViewModel`: `INotifyDataErrorInfo`, `ValidateAllAsync()`, `IDisposable`.
- Fields: Name* (required), Phone, Email, Address, TaxNumber, Notes, CategoryId (dropdown).
- AccountName: read-only TextBox — shown after save with label "الحساب المحاسبي".
- CategoryId dropdown: populated from AccountCategories API — if no categories, hide the field.
- Contacts sub-DataGrid (expander section): Name, Phone, Email, Position, Notes with Add/Edit/Delete.
- Save button ALWAYS enabled (no CanExecute) — `ValidateAllAsync()` on click.
- Dual constructor: parameterless (for designer), parameterized (DI).
- `SetDialogService()` called in constructor.
- **Files**: `SalesSystem.DesktopPWF/ViewModels/Suppliers/SupplierEditorViewModel.cs`, `Views/Suppliers/SupplierEditorView.xaml`
- **Verification**: Create/Edit flows work end-to-end.

### Task 12: DbSeeder — Default Supplier

- In `DbSeeder.SeedSuppliersAsync()`: create default supplier "مورد نقدي" with auto-created Party and Account.
- Account under 2100 with next available code.
- **Files**: `SalesSystem.Infrastructure/Data/DbSeeder.cs`
- **Verification**: After seeding, "مورد نقدي" appears in supplier list with linked account 2101.

### Task 13: Desktop DI Registration

- Register in `SalesSystem.DesktopPWF/Program.cs` or `DependencyInjection.cs`:
  - `ISupplierApiService` + `HttpClient`
  - `SuppliersListViewModel`
  - `SupplierEditorViewModel`
- Register `SupplierChangedMessage` in EventBus.
- Add MainWindow navigation: Sidebar "الموردون" menu item linking to SuppliersListView.
- **Files**: Desktop DI container, `MainWindow.xaml` (navigation).
- **Verification**: Navigation works, suppliers screen accessible from sidebar.

### Task 14: Integration Tests

- Supplier CRUD: Create with auto-account, update, soft delete, restore, permanent delete with/without references.
- Supplier Contact CRUD: Add, update, delete contacts for a supplier.
- Validator tests: Phone regex, Email, required fields.
- API controller tests: 200, 404, 400 responses.
- **Files**: `Tests/SalesSystem.Api.Tests/Controllers/SuppliersControllerTests.cs`, `Tests/SalesSystem.Application.Tests/Services/SupplierServiceTests.cs`
- **Verification**: All tests pass, coverage > 80% for service methods.

### Task 15: Cross-Module Integration Points Verification

- Ensure `PurchaseInvoice` references `SupplierId` FK correctly (schema 7.1).
- Ensure `SupplierPayment` references `SupplierId` FK correctly (schema 7.5).
- Ensure `PurchaseReturn` references `SupplierId` FK correctly (schema 7.3).
- Verify all FK = DeleteBehavior.Restrict — NO cascade delete on Supplier from any referencing table.
- **Files**: Verify configurations in Infrastructure layer.
- **Verification**: All FK constraints are Restrict. No cascade paths exist.

---

## 8. Schema Alignment Checklist

| Entity | Schema Reference | Supplier Entity | Status |
|--------|-----------------|-----------------|--------|
| Party | Table 1.1: Name, Phone, Email, Address, TaxNumber, Notes, IsActive | ✅ Same fields — shared entity | ✅ Aligned |
| Supplier | Table 1.4: PartyId, AccountId, CategoryId, IsActive | ⚠️ Must ensure no CreditLimit/OpeningBalance fields | ✅ Clean |
| SupplierContact | Table 1.5: SupplierId, Name, Phone, Email, Position, Notes | ✅ All fields match | ✅ Aligned |
| PurchaseInvoice | Table 7.1: SupplierId FK | ✅ Already references Suppliers | ✅ |
| SupplierPayment | Table 7.5: SupplierId FK | ✅ Already references Suppliers | ✅ |
| PurchaseReturn | Table 7.3: SupplierId FK | ✅ Already references Suppliers | ✅ |

**No gaps** — Suppliers schema is clean and minimal. Party is shared. Account auto-creation is the main new feature.

---

## 9. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Account code collision under 2100 | Two suppliers get same AccountCode | Use `MAX(CAST(... AS INT)) + 1` query guarded by unique index on `AccountCode` (filtered `[IsActive]=1`). In rare race conditions, retry on unique constraint violation. |
| Supplier used in PurchaseInvoice before Phase 27 | No FK issue — invoice references SupplierId | Supplier CRUD is independent of Purchases. Suppliers can be created now, used later in Phase 27. |
| Party.Id reuse across Customer and Supplier | Same Party shared by both | Party is separate — Customer and Supplier have separate Party records. No sharing of Party records between entity types. |
| Permanent delete with no reference check | FK violation crashes API | `PermanentDeleteAsync()` MUST query `PurchaseInvoices`, `SupplierPayments`, `PurchaseReturns`, `JournalEntryLines` AND catch `DbUpdateException` before blocking. |
| Null CategoryId when AccountCategories not seeded | FK violation if categories table is empty | CategoryId is nullable — no FK issue when null. If AccountCategories table is empty, the CategoryId dropdown shows empty. |
| `ModernTextBox` style applied to `ComboBox` | XamlParseException at runtime | Use `ModernComboBox` style on ComboBox elements (not `ModernTextBox` — per RULE-440). |
| `DisplayMemberPath` and `ItemTemplate` both set on ComboBox | InvalidOperationException | Use one or the other exclusively (per RULE-441). |

---

## 10. Integration Points

| Phase | Integration | Impact |
|-------|-------------|--------|
| Phase 22 (Chart of Accounts) | Account auto-creation under 2100 | Must have parent "2100 — حسابات الموردين" seeded in AccountingSeeder |
| Phase 19 (Settings) | AccountCategories seeded | CategoryId dropdown needs AccountCategories table populated |
| Phase 24 (Accounting Engine) | Auto journal entries for supplier payments and opening balance | `SupplierPaymentService.PostAsync()` calls `AccountingIntegrationService`; opening balance creates `CreateSupplierOpeningEntryAsync()` |
| Phase 27 (Purchases) | SupplierId FK on PurchaseInvoice | PurchaseInvoice references Supplier — supplier must exist before purchase |
| Phase 29 (Supplier Payments) | SupplierId FK on SupplierPayment | SupplierPayment references Supplier |
| Phase 31 (Reports) | Supplier Statement, Balance Report, Aging Report | Reports read supplier balance from linked Account via journal entries |
| Phase 23 (Customers) | Party pattern sharing | Party entity is shared — any changes to Party affect both Customers and Suppliers |
