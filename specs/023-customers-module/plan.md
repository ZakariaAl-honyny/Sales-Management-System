# Phase 23 — Customers Module Implementation Plan

> **Version**: 1.0  
> **Phase**: 23 (after Chart of Accounts, before Accounting Automation)  
> **Depends on**: Phase 22 (Chart of Accounts — 60 accounts seeded, including parent "1210 — العملاء")  
> **Scope**: Parties, Customers, CustomerContacts, CustomerReceipts, CustomerReceiptApplications  
> **Existing code**: All 5 entities exist (Party, Customer, CustomerContact, CustomerReceipt, CustomerReceiptApplication), CustomerService exists, CustomerReceiptService exists, API endpoints exist. This plan covers gap-closure, alignment with new schema, and missing UI/validation/configuration work.

---

## 1. Summary

This phase implements the complete Customers sub-system: the **Party** pattern (shared contact data reused by Suppliers and Employees), **Customer** management with automatic Chart of Accounts integration, **Customer Contacts** (multiple contact persons per customer), **Customer Receipts** (سندات قبض) and their optional allocation across sales invoices via **Customer Receipt Applications**.

The module is already partially built (entities, service, controller exist). This plan identifies what must be **added, fixed, or aligned** to match the new database schema (`database-schema.md`), the AGENTS.md rules, and the analysis decisions from Analysis Part 3.

---

## 2. Key Entities

### 2.1 Party (جدول الأطراف — Table 1.1)

Shared entity holding common contact data used by Customers, Suppliers, and Employees. Uses `ActivatableEntity` base (soft-deletable).

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int PK | Shared PK with Customer/Supplier (1:1) |
| `PartyType` | tinyint | 1=Customer, 2=Supplier, 3=Employee |
| `Name` | nvarchar(200) | Required |
| `NameAr` | nvarchar(200) | Optional Arabic name |
| `Phone` | nvarchar(30) | Optional |
| `Mobile` | nvarchar(30) | Optional |
| `Email` | nvarchar(100) | Optional |
| `Address` | nvarchar(300) | Optional |
| `TaxNumber` | nvarchar(50) | Optional |
| `AccountId` | int FK → Accounts | **Every party = a GL account** |
| `Notes` | nvarchar(500) | Optional |
| IsActive + audit fields | | ActivatableEntity |

**Key decision**: AccountId lives on **Party**, not on Customer or Supplier separately. This means every party type (customer, supplier, employee) can optionally be linked to a GL account. For customers and suppliers, AccountId is **mandatory** and auto-created by the service.

### 2.2 Customer (جدول العملاء — Table 1.2)

Customer-specific fields. Uses **shared primary key** pattern: `Customer.Id = Party.Id` (PK is also FK).

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int PK = FK → Parties | Shared PK with Party (1:1) |
| `CategoryId` | int? FK → AccountCategories | Optional — for customer classification |
| `CreditLimit` | decimal(18,2) | 0 = no limit enforced |
| `CustomerSince` | datetime2? | Optional start date |
| `PriceLevel` | tinyint? | 1=Retail, 2=Wholesale, 3=VIP, 4=Distributor |
| `Notes` | nvarchar(500) | Free-text |
| IsActive + audit | | ActivatableEntity |

**Key decisions**:
- **No CustomerGroup in V1** — deferred to V2. The `CustomerGroup` entity exists but must be removed from V1 scope.
- **No CustomerType in V1** — Cash/Credit decision is per-invoice (`SalesInvoice.PaymentType`), not per-customer.
- **Balance lives on Account** — `OpeningBalance` and `CurrentBalance` are NOT stored on Customer. Balance is read from the linked Account (via journal entries). The `OpeningBalance` field in the UI triggers a journal entry creation, not a Customer field update.
- **CreditLimit** is a **soft warning** — `CheckCreditLimit()` returns `bool` (non-throwing). The caller (service/controller) decides whether to block the transaction.
- **AccountId on Party** — already implemented, auto-created under parent "1210 — العملاء" (Accounts Receivable).

### 2.3 CustomerContact (جهات اتصال العميل — Table 1.3)

Multiple contact persons per customer.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int PK | |
| `CustomerId` | int FK → Customers | |
| `Name` | nvarchar(150) | Required |
| `Phone` | nvarchar(30) | Optional |
| `Email` | nvarchar(100) | Optional |
| `Position` | nvarchar(100) | Job title |
| `Notes` | nvarchar(300) | Optional |
| IsActive + audit | | ActivatableEntity |

**Key decision**: Contacts are strictly informational — no login, no permissions. They exist only for phone/email lookup.

### 2.4 CustomerReceipt (سند قبض — Table 6.5)

Receipt/deposit collected from a customer. Uses `DocumentEntity` base (Draft → Posted → Cancelled lifecycle).

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int PK | |
| `ReceiptNo` | int | Unique, generated via DocumentSequenceService |
| `ReceiptDate` | date | |
| `CustomerId` | int FK → Customers | |
| `CashBoxId` | int FK → CashBoxes | Where the money goes |
| `CurrencyId` | smallint FK → Currencies | |
| `Amount` | decimal(18,2) | |
| `Notes` | nvarchar(500) | Optional |
| `Status` | tinyint | 1=Draft, 2=Posted, 3=Cancelled |
| Audit fields | | DocumentEntity |

**Key decisions**:
- **ReceiptNo** is `int` (not string), generated via `IDocumentSequenceService.GetNextIntAsync("CustomerReceipt", ct)`.
- **Posting** creates a journal entry: Dr CashBox Account, Cr Customer Account (via AccountingIntegrationService in Phase 24).
- **Cancellation** creates a reversal journal entry (Phase 24).
- Only **Posted** receipts affect customer balance and cash box.
- Receipts are **immutable** once posted — cancellation is the only allowed modification (via offsetting entry).

### 2.5 CustomerReceiptApplication (توزيع سند القبض — Table 6.6)

Optional allocation of receipt amount to specific sales invoices.

| Field | Type | Notes |
|-------|------|-------|
| `Id` | int PK | |
| `CustomerReceiptId` | int FK → CustomerReceipts | |
| `SalesInvoiceId` | int FK → SalesInvoices | |
| `AppliedAmount` | decimal(18,2) | |

**Key decision**: Applications are **optional** — the receipt can be created without allocation (unapplied receipt). The allocation is created only when the user explicitly distributes the payment to specific invoices. One receipt can be applied across multiple invoices; one invoice can be settled by multiple receipts.

---

## 3. Business Rules

### 3.1 Party Rules
- **RULE-P1**: `Name` is required (non-empty, trimmed).
- **RULE-P2**: `AccountId` must be > 0 for Customer and Supplier parties (auto-created).
- **RULE-P3**: `PartyType` must be a valid enum value (Customer, Supplier, Employee).
- **RULE-P4**: Soft delete via `IsActive = false`. Permanent delete allowed only if no linked invoices, receipts, or journal entries exist.

### 3.2 Customer Rules
- **RULE-C1**: `Id` must equal the Party.Id (shared PK pattern enforced at creation).
- **RULE-C2**: `CreditLimit` must be >= 0 (0 = no limit).
- **RULE-C3**: `CheckCreditLimit(additionalAmount, currentBalance)` returns `bool` — non-throwing. The caller decides blocking.
- **RULE-C4**: `PriceLevel` (if set) must be 1-4 (Retail, Wholesale, VIP, Distributor).
- **RULE-C5**: `Update()` calls `UpdateTimestamp()` — audit trail.
- **RULE-C6**: Soft delete via `MarkAsDeleted()`. Permanent delete guarded by checking `SalesInvoices`, `CustomerReceipts`, `JournalEntryLines` for references.
- **RULE-C7**: `CategoryId` FK to `AccountCategories` is optional — null means uncategorized.
- **RULE-C8**: No `CustomerGroupId` in V1 — the `CustomerGroup` table/entity is excluded from V1 scope.

### 3.3 CustomerContact Rules
- **RULE-CC1**: `Name` required (non-empty, trimmed).
- **RULE-CC2**: `CustomerId` must reference an active customer.
- **RULE-CC3**: Contacts are soft-deletable (IsActive).

### 3.4 CustomerReceipt Rules
- **RULE-R1**: `ReceiptNo` must be > 0 (generated by service, validated in domain).
- **RULE-R2**: `Amount` must be > 0.
- **RULE-R3**: `Post()` only allowed from `Draft` status.
- **RULE-R4**: `Cancel()` allowed from `Draft` or `Posted` status. Cancelling a posted receipt must reverse the journal entry (Phase 24).
- **RULE-R5**: `AddApplication()` only allowed in `Draft` status.
- **RULE-R6**: Total of all `AppliedAmount` values must not exceed receipt `Amount` (validated in service, not domain — since applications are optional).
- **RULE-R7**: No editing a posted receipt — cancel and create new.

### 3.5 CustomerReceiptApplication Rules
- **RULE-RA1**: `AppliedAmount` must be > 0.
- **RULE-RA2**: `SalesInvoiceId` must reference a valid, posted sales invoice.
- **RULE-RA3**: `AppliedAmount` must not exceed the invoice's `RemainingAmount` (validated in service).

---

## 4. Auto-Account Pattern (كل عميل = حساب)

### Design Philosophy

Every customer IS a GL account. This is the single most important architectural decision in this module:

```
إنشاء عميل = إنشاء حساب محاسبي تلقائي
```

When a user creates a customer, the service automatically creates:

1. A Level-4 detail **Account** under parent `"1210 — العملاء"` (Accounts Receivable)
2. The new account is typed as **Asset** (Nature = 1)
3. Code is auto-generated as the next numeric child of 1210 (e.g., 1211, 1212, 1213...)
4. `AllowTransactions` = true (leaf account that can receive journal entry lines)
5. `IsSystemAccount` = false (user can modify if needed)
6. The `AccountId` is set on the **Party** record

### Current Implementation (Already Done)

The `CustomerService.AutoCreateCustomerAccountAsync()` method already implements this pattern:
- Looks up parent account "1210" by code
- Falls back to `SystemAccountMappings.AccountsReceivable` if 1210 not found
- Generates next numeric child code
- Creates the Account, then creates Party + Customer with the new AccountId

### What Needs Enhancement

| Gap | Fix |
|-----|-----|
| No `AccountId` returned in `CreateCustomerRequest`/`UpdateCustomerRequest` | Already correct — AccountId is auto-generated server-side |
| Account code generation uses in-memory `ToListAsync()` | Acceptable for small numbers of customers. For 10,000+ customers, switch to DB-side `MAX()` query |
| OpeningBalance not linked to journal entry | Add `OpeningBalance` field to `CreateCustomerRequest` — when > 0, create an opening journal entry via `AccountingIntegrationService` (Phase 24 integration point) |
| No `CategoryId` on customer entity | Add `CategoryId` field/nav to Customer entity (nullable FK to AccountCategories) |

### Opening Balance Flow

```
User enters OpeningBalance = 5,000 during customer creation
    ↓
Service creates Account (Level 4, Asset, under 1210)
Service creates Party record
Service creates Customer record (shared PK)
    ↓
If OpeningBalance > 0:
    Service creates journal entry:
        Dr Customer Account (121X)    5,000
        Cr Opening Balance Equity     5,000
    (Integration point with Phase 24 — Accounting Engine)
```

---

## 5. UI Screens

### 5.1 Customers List View (شاشة قائمة العملاء)

| Element | Behavior |
|---------|----------|
| DataGrid columns | Name (from Party), Phone, Email, CreditLimit, CurrentBalance, Status (Active/Inactive) |
| Sorting | Newest-first by `Id` descending |
| Search | By Name or Phone (API-side filtering) |
| Toolbar | Add, Edit, Delete, Restore, Refresh |
| Context menu | Edit, Deactivate/Activate, Delete Permanently (if no references), View Statement |
| Empty state | "لا يوجد عملاء — أضف أول عميل" with Add button |
| Toggle inactive | Checkbox "عرض العملاء غير النشطين" |

### 5.2 Customer Editor View (شاشة إضافة/تعديل عميل)

| Section | Fields |
|---------|--------|
| بيانات العميل | Name* (required), Phone, Email, Address, TaxNumber, Notes |
| الإعدادات المالية | CreditLimit (0 = غير محدد), PriceLevel (dropdown: تجزئة/جملة/VIP/موزع), OpeningBalance (shown only on create) |
| معلومات النظام | CustomerSince (date picker, default = today), AccountName (read-only, shown after save) |
| Contacts tab | Sub-DataGrid of CustomerContacts: Name, Phone, Email, Position |

**Key UI decisions**:
- `AccountId` is NEVER shown or editable in the UI — it is auto-created server-side.
- `CategoryId` dropdown (future) — currently hidden in V1 until AccountCategories module is integrated.
- `CurrentBalance` is shown read-only on the list view, sourced from the linked Account's computed balance.
- OpeningBalance input only appears during **Create**, not Edit (opening balance is a one-time setup).
- ToolTip ⓘ explaining what OpeningBalance means: "الرصيد الافتتاحي — يستخدم عند بدء العمل بالنظام. ينشئ قيداً محاسبياً افتتاحياً."

### 5.3 Customer Contacts Sub-Screen

Inline editable DataGrid within the Customer Editor, or a separate popup dialog:
- Add: Name* (required), Phone, Email, Position, Notes
- Edit: Same fields
- Delete: Soft delete (confirm dialog)

### 5.4 Customer Receipts List View (شاشة سندات القبض)

| Element | Behavior |
|---------|----------|
| DataGrid columns | ReceiptNo, Date, Customer Name, Amount, CashBox, Status, CreatedBy |
| Sorting | Newest-first by `ReceiptNo` descending |
| Filter | By Customer, By Date Range, By Status |
| Toolbar | Add, View, Post, Cancel, Print |
| Status colors | Draft=Grey, Posted=Green, Cancelled=Red |

### 5.5 Customer Receipt Editor View (شاشة إضافة سند قبض)

| Section | Fields |
|---------|--------|
| بيانات السند | Customer* (searchable dropdown), ReceiptDate*, CashBox*, Currency*, Amount*, Notes |
| توزيع على فواتير | Tab "التوزيع" — optional DataGrid of SalesInvoice allocations |
| Invoice allocation | Multi-select invoices with `RemainingAmount`, user enters `AppliedAmount` for each |
| Totals | Receipt Amount, Total Allocated, Unallocated (must be >= 0) |

### 5.6 Customer Statement View (شاشة كشف حساب العميل)

Opened from the Customers List (context menu "كشف حساب" or button):
- From/To date range filter
- Columns: Date, Document Type (فاتورة بيع / سند قبض / مرتجع), Document No, Debit, Credit, Running Balance
- Data sourced from Journal Entries (Phase 24 integration)
- Export to Excel button

---

## 6. Tasks (Ordered)

### Task 1: Align Customer Entity with New Schema
- Add `CategoryId` (int? FK → AccountCategories) to Customer entity
- Remove `CustomerGroupId` reference if present (deferred to V2)
- Ensure `Customer.Create()` accepts `categoryId` parameter (nullable)
- Ensure `Customer.Update()` accepts `categoryId` parameter (nullable)
- Add `Category` navigation property (virtual)
- **Files**: `SalesSystem.Domain/Entities/Customer.cs`
- **Verification**: Build passes, no compile errors

### Task 2: Add CustomerId to Party if Missing
- Party currently lives independently. Ensure `Party.PartyType` is always set for customer parties.
- Ensure `Party` entity has full field alignment with schema (Name, Phone, Mobile, Email, Address, TaxNumber, AccountId, Notes)
- **Files**: `SalesSystem.Domain/Entities/Party.cs`
- **Verification**: `Party.Create()` includes all schema fields

### Task 3: Customer FluentValidation Rules
- Enhance `CreateCustomerRequestValidator`: Name required, Phone regex `^05\d{8}$` with Arabic message, Email `.EmailAddress()`, CreditLimit >= 0
- Enhance `UpdateCustomerRequestValidator`: same rules as Create
- Add Arabic error messages for all validators
- **Files**: `SalesSystem.Api/Validators/CustomerRequestsValidator.cs`
- **Verification**: Unit tests pass for valid/invalid inputs

### Task 4: Customer Service — Opening Balance Integration Point
- Add `OpeningBalance` (decimal) to `CreateCustomerRequest` (optional, default 0)
- In `CustomerService.CreateAsync()`: if `OpeningBalance > 0`, prepare data for Phase 24 integration (store `_pendingOpeningEntry` flag or call an `IOpeningBalanceService`)
- For now, log a warning: "Opening balance {amount} for customer {name} — journal entry not yet created (Phase 24)"
- **Files**: `SalesSystem.Contracts/Requests/CreateCustomerRequest.cs`, `SalesSystem.Application/Services/CustomerService.cs`
- **Verification**: OpeningBalance > 0 does not crash; balance is logged

### Task 5: Customer Service — Permanent Delete Guard Enhancement
- Add checks for `CustomerReceipts` and `JournalEntryLines` (if available) before permanent delete
- Ensure the `PermanentDeleteAsync()` returns clear Arabic messages for each reference type
- Log warning on permanent delete attempt with references (per RULE-245)
- **Files**: `SalesSystem.Application/Services/CustomerService.cs`
- **Verification**: Customer with receipts cannot be permanently deleted

### Task 6: Customer Contact CRUD
- Ensure `ICustomerContactService` exists with: `GetByCustomerIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- Ensure `CustomerContactDto` exists in Contracts
- Ensure `CreateCustomerContactRequest` / `UpdateCustomerContactRequest` with validators
- Ensure `CustomerContactsController` with endpoints: GET by customer, POST, PUT, DELETE
- All methods return `Result<T>`
- **Files**: Create or enhance as needed in Application, Contracts, Api layers
- **Verification**: Full CRUD cycle via API tests

### Task 7: Customer Receipt — Schema Alignment
- Ensure `CustomerReceipt.ReceiptNo` is `int` (not string) — already done
- Ensure `CustomerReceipt.ReceiptDate` is `DateTime` (date component only for storage)
- Ensure `CurrencyId` is `short` (consistent with Currencies PK type)
- Ensure `Status` uses `InvoiceStatus` enum (Draft=1, Posted=2, Cancelled=3)
- **Files**: `SalesSystem.Domain/Entities/CustomerReceipt.cs`
- **Verification**: Build passes

### Task 8: Customer Receipt — DocumentSequence Integration
- In `CustomerReceiptService.CreateAsync()`: call `_documentSequenceService.GetNextIntAsync("CustomerReceipt", ct)` before creating entity
- Ensure `ReceiptNo` is unique (DB unique index on ReceiptNo)
- **Files**: `SalesSystem.Application/Services/CustomerReceiptService.cs`
- **Verification**: Consecutive receipts get incremented ReceiptNo values

### Task 9: Customer Receipt — Posting & Cancellation
- `PostAsync()`: validate receipt is Draft, set Status = Posted, set PostedAt
- `CancelAsync()`: validate receipt is not already Cancelled, set Status = Cancelled
- If receipt was Posted, log warning that journal reversal is pending (Phase 24)
- Ensure only Posted receipts are considered "confirmed"
- **Files**: `SalesSystem.Application/Services/CustomerReceiptService.cs`
- **Verification**: Receipt flow: Draft → Posted → Cancelled

### Task 10: Customer Receipt — Application Validation
- In `AddApplicationAsync()`: validate `AppliedAmount > 0`, validate `SalesInvoice` exists and is Posted
- Validate total applied <= receipt amount
- Validate applied amount <= invoice remaining amount
- Return clear Arabic errors for each violation
- **Files**: `SalesSystem.Application/Services/CustomerReceiptService.cs`
- **Verification**: Cannot over-allocate a receipt or allocate to a draft invoice

### Task 11: Desktop UI — Customers List View
- `CustomersListViewModel` with `INotifyDataErrorInfo`, `IDisposable`, EventBus subscription
- DataGrid with columns: Name, Phone, Email, CreditLimit, CurrentBalance (computed), Status
- Search, sort (newest-first), filter inactive toggle
- Add opens CustomerEditorViewModel via `ScreenWindowService`
- Edit opens same VM in edit mode
- Delete uses `IDialogService.ShowDeleteConfirmationAsync()`
- ToolTips on all buttons (per RULE-185+)
- **Files**: New in `SalesSystem.DesktopPWF/ViewModels/Customers/`, `Views/Customers/`

### Task 12: Desktop UI — Customer Editor View
- `CustomerEditorViewModel` with all fields, `INotifyDataErrorInfo`, `ValidateAllAsync()`
- Name* (required), Phone (with regex validation), Email, Address, TaxNumber
- CreditLimit (numeric), PriceLevel (dropdown)
- OpeningBalance (shown only on create mode, with ⓘ ToolTip)
- CustomerSince (DatePicker, default today)
- AccountName (read-only TextBox, shown after save)
- Contacts sub-DataGrid (inline or via expander)
- Save button ALWAYS enabled (no CanExecute) — ValidateAllAsync() on click
- **Files**: New in DesktopPWF

### Task 13: Desktop UI — Customer Receipts List & Editor
- `CustomerReceiptsListViewModel`: DataGrid, search by customer/date/status, newest-first sort
- `CustomerReceiptEditorViewModel`: Customer dropdown, CashBox dropdown, Currency dropdown, Amount, Notes
- Optional Applications tab with invoice selection DataGrid
- Post/Cancel buttons with confirmation dialogs
- **Files**: New in DesktopPWF

### Task 14: DbSeeder — Default Customer
- Seed default customer "عميل نقدي" (Cash Customer) with auto-created Party and Account
- Party.Name = "عميل نقدي", PartyType = Customer, IsActive = true
- Account under 1210 with code = next available
- Customer.CreditLimit = 0 (no limit), PriceLevel = null
- **Files**: `SalesSystem.Infrastructure/Data/DbSeeder.cs`
- **Verification**: After seeding, "عميل نقدي" appears in customer list with linked account

### Task 15: Fluent API Configurations
- Ensure `CustomerConfiguration`: `PartyId` unique index (1:1), `AccountId` FK Restrict, `CategoryId` FK Restrict, `CreditLimit` precision 18,2
- Ensure `CustomerContactConfiguration`: `CustomerId` FK Restrict, `Name` max 150, `Phone` max 30
- Ensure `CustomerReceiptConfiguration`: `ReceiptNo` unique index, `CustomerId` FK Restrict, `CashBoxId` FK Restrict, `CurrencyId` FK Restrict, `Amount` precision 18,2
- Ensure `CustomerReceiptApplicationConfiguration`: `CustomerReceiptId` FK Restrict, `SalesInvoiceId` FK Restrict, composite unique index on `(CustomerReceiptId, SalesInvoiceId)` to prevent duplicate allocations
- **Files**: Update or create in `SalesSystem.Infrastructure/Data/Configurations/`

### Task 16: Integration Tests
- Customer CRUD: Create with auto-account, Update, Soft Delete, Permanent Delete (with/without references)
- Customer Receipt: Create, Post, Cancel, Add Application, validate allocation limits
- **Files**: `Tests/SalesSystem.Api.Tests/Controllers/CustomersControllerTests.cs`, `Tests/SalesSystem.Application.Tests/Services/CustomerServiceTests.cs`
- **Verification**: All 2,083+ existing tests pass; new tests cover all scenarios

---

## 7. Schema Alignment Checklist

Compare existing entities against `database-schema.md`:

| Entity | Schema 1.1/1.2/1.3/6.5/6.6 | Current Code | Status |
|--------|---------------------------|--------------|--------|
| Party | Name, Phone, Email, Address, TaxNumber, Notes, AccountId, IsActive | ✅ Has all fields + NameAr + Mobile | ✅ Aligned |
| Customer | PartyId FK, AccountId FK, CategoryId FK, CreditLimit | ✅ PartyId=Id (shared PK), AccountId on Party, CreditLimit | ⚠️ Missing CategoryId |
| CustomerContact | CustomerId FK, Name, Phone, Email, Position, Notes | ✅ Has all fields | ✅ Aligned |
| CustomerReceipt | ReceiptNo(int), ReceiptDate, CustomerId, CashBoxId, CurrencyId(smallint), Amount, Status | ✅ Has all fields, CurrencyId is short | ✅ Aligned |
| CustomerReceiptApplication | CustomerReceiptId, SalesInvoiceId, AppliedAmount | ✅ Has all fields | ✅ Aligned |

**Only gap**: `Customer.CategoryId` is not yet on the entity. Add it.

---

## 8. Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Opening balance without journal entry | Customer owes 5,000 but GL shows 0 | Phase 24 must add `AccountingIntegrationService.CreateCustomerOpeningEntryAsync()`. For now, log warning and document the integration point. |
| Account code collision under 1210 | Two customers get same account code | Current `GenerateNextAccountCodeAsync()` uses `ToListAsync()` + max — safe for < 10K customers. For scale, switch to raw SQL `SELECT MAX(CAST(AccountCode AS INT))`. |
| Receipt posted without access to CashBox or Currency | FK violation if CashBox/Currency deleted | All FK = Restrict. CashBoxes and Currencies are soft-deletable (IsActive = false) — receipt references them via FK, preventing hard delete. |
| Over-allocation on invoice | Receipt sum > invoice remaining | Service must validate cumulative `AppliedAmount` against invoice `RemainingAmount` inside a transaction to prevent race conditions. |
| Customer used in Sales before Phase 28 | No FK issue, but invoice flow is incomplete | Customer CRUD is independent of Sales. Customers can be created now, used later in Phase 28. |

---

## 9. Integration Points

| Phase | Integration | Impact |
|-------|-------------|--------|
| Phase 22 (Chart of Accounts) | Account auto-creation under 1210 | Must have parent "1210 — العملاء" seeded in AccountingSeeder |
| Phase 24 (Accounting Engine) | Journal entries for receipt posting/cancellation | `CustomerReceiptService.PostAsync()` will call `AccountingIntegrationService.CreateReceiptEntryAsync()` |
| Phase 24 (Accounting Engine) | Opening balance journal entry | `CustomerService.CreateAsync()` with `OpeningBalance > 0` calls `CreateCustomerOpeningEntryAsync()` |
| Phase 28 (Sales) | Customer used in Sales Invoice | CustomerId FK on SalesInvoice — must exist before invoice creation |
| Phase 29 (Receipts & Payments) | ReceiptApplication references SalesInvoice | SalesInvoice must be Posted before allocation |
| Phase 31 (Reports) | Customer Statement, Balance Report, Aging Report | Reports read balance from linked Account via journal entries |
