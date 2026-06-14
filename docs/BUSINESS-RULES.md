# Business Rules — Sales Management System

> **Version**: v4.10.2+
> **Source**: Accounts.md analysis, AGENTS.md §2, PRD-MVP.md, CONSTITUTION.md
> **Last Updated**: 2026-06-15

---

## 1. Money & Quantity

| Rule | Description |
|------|-------------|
| BR-001 | ALL money fields = `decimal(18,2)` — NEVER `float`/`double`/`int` |
| BR-002 | ALL quantities = `decimal(18,3)` — NEVER `int` |

## 2. Account Auto-Creation (Chart of Accounts)

| Rule | Description |
|------|-------------|
| BR-010 | **CashBox** without `AccountId` → auto-create Level-4 sub-account under parent `"1110 — النقدية"` (Cash & Cash Equivalents). Code auto-increments (1111, 1112, ...). |
| BR-011 | **Bank** without `AccountId` → auto-create Level-4 sub-account under parent `"1120 — البنوك"` (Bank Accounts). Code auto-increments (1121, 1122, ...). |
| BR-012 | **Customer** without `AccountId` → auto-create Level-4 sub-account under parent `"1130 — العملاء"` (Accounts Receivable). Code auto-increments (1131, 1132, ...). |
| BR-013 | **Supplier** without `AccountId` → auto-create Level-4 sub-account under parent `"1320 — الموردون"` (Accounts Payable). Code auto-increments (1321, 1322, ...). |
| BR-014 | **Employee** via `POST /api/v1/employees/{id}/auto-create-account` → auto-create Level-4 sub-account under parent `"1170 — عهد الموظفين"` (Employee Custody). |
| BR-015 | All auto-created accounts are Level 4 (detail), `AccountType.Asset` or `AccountType.Liability`, `AllowTransactions = true`, `IsSystemAccount = false`. |
| BR-016 | Parent accounts for auto-creation are looked up by **hardcoded code string** — NEVER by name. Correct codes: CashBox→1110, Bank→1120, Customer→1130, Supplier→1320, Employee→1170. |

## 3. Flexible Input (Sales & Purchase Lines)

| Rule | Description |
|------|-------------|
| BR-020 | User enters ANY TWO of (Quantity, UnitPrice/UnitCost, LineTotal) — system calculates the third. |
| BR-021 | `LineTotal` column is **editable** (not `IsReadOnly`) in DataGrid. |
| BR-022 | `FlexibleInputCalculator` is ONLY invoked when user explicitly edits `LineTotal` (`_lastModifiedField == CalculationField.Total`). |
| BR-023 | When user edits Quantity or UnitPrice/UnitCost, `_lineTotalInput` = `_quantity * _unitPrice` (direct computation — NO calculator). |
| BR-024 | Never pass Quantity or Price as `lastModifiedField` to `FlexibleInputCalculator.Calculate()` — the auto-computed total is treated as a user-entered anchor, causing incorrect recalculation. |

## 4. Invoice Lifecycle

| Rule | Description |
|------|-------------|
| BR-030 | States: Draft (1) → Posted (2) → Cancelled (3) — terminal at Cancelled. |
| BR-031 | Stock/balance changes ONLY when Status = Posted. |
| BR-032 | No editing a Posted invoice — cancel + create new. |
| BR-033 | Cancellation MUST reverse ALL stock and balance. |

## 5. Purchase Invoice (Landed Cost)

| Rule | Description |
|------|-------------|
| BR-040 | `OtherCharges` field on `PurchaseInvoice` — used for freight, customs, etc. |
| BR-041 | `AllocateAdditionalCharges()` distributes `OtherCharges` proportionally by line total. |
| BR-042 | Inventory stock uses `landedUnitCost` (UnitCost + allocated share of OtherCharges). |

## 6. Sales Price Enforcement

| Rule | Description |
|------|-------------|
| BR-050 | If `PreventBelowRetailPrice` is ON → reject sale if `UnitPrice < registered price` in `ProductPrices`. |
| BR-051 | If `AllowBelowCostSale` is OFF → reject sale if `UnitPrice < AvgCost`. |
| BR-052 | Price override within invoice (`IsPriceOverridden`) does NOT change the catalog price — override is invoice-line local. |

## 7. Purchase Return (Standalone)

| Rule | Description |
|------|-------------|
| BR-060 | Standalone mode allowed — `PurchaseInvoiceId` can be null. |
| BR-061 | Journal entry: Dr Supplier Account / Cr PurchaseReturnAccount. |
| BR-062 | `PostedAt`/`CancelledAt` set in `Post()` and `Cancel()`. |

## 8. Delivery Charges

| Rule | Description |
|------|-------------|
| BR-070 | Sales `OtherCharges` credited to `DeliveryChargesRevenue` account (`SystemAccountKey.DeliveryChargesRevenue = 21`), NOT to SalesRevenue. |

## 9. Accounting Integration

| Rule | Description |
|------|-------------|
| BR-080 | Every money-affecting operation creates balanced double-entry journal entries. |
| BR-081 | Per-entity account routing: use `Customer.AccountId` / `Supplier.AccountId` — NOT fixed AR/AP from SystemAccountMappings. |
| BR-082 | Sales revenue uses single account `1520 — إيرادات المبيعات` — NOT split 1521/1522. |
| BR-083 | Purchase return reversal credits `PurchaseReturnAccountId` — NOT `InventoryAssetAccountId`. |
| BR-084 | COGS uses `AverageCost` (weighted average) — NOT `PurchaseCost`. |

## 10. Chart of Accounts (COA)

| Rule | Description |
|------|-------------|
| BR-090 | 60 seeded accounts: 5 L1 + 8 L2 + 20 L3 + 27 L4. |
| BR-091 | `IsSystemAccount = true` for L1-L2 only (protected from modification/deletion). |
| BR-092 | `AllowTransactions = false` for levels < 4. |
| BR-093 | Color codes: Asset=#2196F3, Liability=#F44336, Equity/Revenue=#4CAF50, Expense=#FF9800. |

## 11. Currencies

| Rule | Description |
|------|-------------|
| BR-100 | `CurrencyCode` validates exactly 3 chars (ISO 4217). |
| BR-101 | `IsBaseCurrency` is IMMUTABLE after creation — locked at system setup. |
| BR-102 | `IsSystem` flag protects system-seeded currencies from deletion. |
| BR-103 | `FractionName` stored on Currency entity (e.g., "فلس" for YER). |

## 12. Customer & Supplier

| Rule | Description |
|------|-------------|
| BR-110 | `AccountId` is MANDATORY (non-nullable `int`) — auto-created by service. |
| BR-111 | NO `CustomerGroup`/`CustomerType`/`SupplierType` in V1. |
| BR-112 | NO `OpeningBalance`/`CurrentBalance`/`CurrencyId` on Customer/Supplier entity — balance lives on linked Account. |
| BR-113 | `CheckCreditLimit()` returns `bool` — NEVER throws. |

## 13. Users & Security

| Rule | Description |
|------|-------------|
| BR-120 | `User.Create()` is passwordless — `PasswordHash = null`, `MustChangePassword = true`. |
| BR-121 | `RecordLoginAttempt()` — lockout at 5 failures (`UserStatus.Locked`). |
| BR-122 | Users are soft-deleted only — `PermanentDeleteAsync()` returns `Result.Failure`. |
| BR-123 | Login endpoint rate-limited: 5 attempts per 15 minutes per IP. |

## 14. Database Constraints

| Rule | Description |
|------|-------------|
| BR-130 | ALL FKs use `DeleteBehavior.Restrict` — no cascade. |
| BR-131 | `WarehouseStocks.Quantity` has `CHECK (Quantity >= 0)`. |
| BR-132 | `JournalEntryLine` has `CHK_DebitOrCredit` and `CHK_NoNegativeValues`. |
| BR-133 | Filtered unique indexes on soft-deletable entities include `AND [IsActive] = 1`. |

## 15. WPF UI

| Rule | Description |
|------|-------------|
| BR-140 | Save/Post/Print buttons ALWAYS enabled — validate on click with warning dialog. |
| BR-141 | ALL interactive controls have Arabic ToolTips explaining action/consequence. |
| BR-142 | Editors open non-modally via `ScreenWindowService.OpenScreen()`. |
| BR-143 | Validation uses `INotifyDataErrorInfo` — NO `HasXxxError` booleans. |
| BR-144 | EventBus: subscribe in OnLoad, unsubscribe in `Dispose()`. |
| BR-145 | LineTotal column editable in DataGrid with Flexible Input support. |

---

## 16. Account.Create() — allowTransactions Validation

| Rule | Description |
|------|-------------|
| BR-160 | ALL `Account.Create()` calls for Level 4+ MUST pass `allowTransactions: true`. The default is `false` and `Account.Create()` throws `DomainException("الحساب التفصيلي يجب أن يسمح بالحركات")` when `level >= 4 && !allowTransactions`. |
| BR-161 | Check ALL callers: BankService, CashBoxService, CustomerService, SupplierService, EmployeeService, PartyService — every auto-creation path MUST set `allowTransactions: true`. |

## 17. Service Implementation Completeness

| Rule | Description |
|------|-------------|
| BR-170 | EVERY service interface registered in DI MUST have a concrete implementation class. Missing implementations cause `InvalidOperationException` at DI resolution. |
| BR-171 | Search `Program.cs` or `DependencyInjection.cs` for all `services.AddScoped<I, T>` and `services.AddTransient<I, T>` — verify `T` exists as a class. |

## 18. Report API/ViewModel URL Alignment

| Rule | Description |
|------|-------------|
| BR-180 | Desktop ViewModel report API calls MUST match actual controller routes. Check `ReportApiService.cs` for every `Get*Async()` URL against the API controllers. |
| BR-181 | Common mismatches: `detailed-stock-ledger`, `reports/returns`, `reports/aging`. Add missing endpoints to `ReportsController` rather than changing Desktop URLs (unless Desktop URL is wrong). |

## 19. Payment Update Reversal

| Rule | Description |
|------|-------------|
| BR-190 | `SupplierPaymentService.UpdateAsync()` MUST reverse original journal entry and create new entry when a posted payment's amount changes. |
| BR-191 | `CustomerReceiptService.UpdateAsync()` MUST exist and support Draft-only updates (posted receipts must be cancelled and recreated). |
| BR-192 | Wrap reversal + re-creation in `ExecuteTransactionAsync()` for atomicity. Use per-entity account routing. |

## 20. Standalone Sales Return Journal Entries

| Rule | Description |
|------|-------------|
| BR-200 | `AccountingIntegrationService` MUST have `CreateSalesReturnEntryAsync()` for standalone (non-invoice-cancellation) returns. |
| BR-201 | Entry: Dr `SalesReturnsAccount` / Cr `CustomerAccount` (per-entity routing) for return amount; Dr `InventoryAccount` / Cr `COGSAccount` for cost side. |
| BR-202 | This is separate from `ReverseSalesPostEntryAsync()` which handles full invoice cancellations. |

## 21. Report Service Quality

| Rule | Description |
|------|-------------|
| BR-210 | Report services MUST NOT return hardcoded failure stubs like `Result.Failure("تحت التطوير")`. Implement actual queries using available entities. |
| BR-211 | ALL financial report ViewModels MUST support Excel export via ClosedXML — PDF-only reports are incomplete. |
| BR-212 | Check `AccountStatementViewModel` was missing Excel export — fix pattern: add `ExportExcelCommand` + `ExportExcelAsync()` matching other financial report VMs. |
