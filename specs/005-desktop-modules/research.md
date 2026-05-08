# Research: Desktop Modules (005)

**Branch**: `005-desktop-modules` | **Date**: 2026-05-08

## 1. Excel/CSV Export Libraries

**Decision**: Use `ClosedXML` (MIT license) for Excel export and `System.IO` built-in `StreamWriter` for CSV export.

**Rationale**: ClosedXML is the de-facto standard for .xlsx generation in .NET WinForms apps. It produces proper Office Open XML files, supports cell formatting, and has zero COM dependency. CSV export requires no extra package — just stream-writing comma-delimited lines from the DataGridView/DataTable already in memory.

**Alternatives considered**:
- `EPPlus`: Requires commercial license for non-open-source projects. Rejected.
- `NPOI`: Older API, heavier; no meaningful advantage over ClosedXML here.
- `Microsoft.Office.Interop.Excel`: Requires Office installed on the machine. Rejected for deployment reasons.

**NuGet Package Required**: `ClosedXML` (MIT, free) — requires approval per AGENTS.md §5 since it is not in the approved list. Flagged below.

> ⚠️ **Approval Needed**: `ClosedXML` is not in the approved NuGet list (AGENTS.md §5). It must be added before task implementation begins.

---

## 2. Tax Toggle Approach

**Decision**: The `TaxIncluded` boolean toggle on the invoice form determines whether the entered unit prices are tax-inclusive (price already contains tax) or tax-exclusive (tax is added on top).

**Formula**:
- Tax-Exclusive: `TaxAmount = SubTotal * (TaxRate / 100)`; `TotalAmount = SubTotal + TaxAmount - InvoiceDiscount`
- Tax-Inclusive: `TaxAmount = SubTotal - (SubTotal / (1 + TaxRate / 100))`; `TotalAmount = SubTotal - InvoiceDiscount`

**Where tax rate comes from**: The API will expose a settings endpoint (or a default rate from `AppSettings`). For Phase 5, a configurable default rate (e.g., 15%) is fetched on invoice form load. The Domain entity will store both `TaxRate` and `TaxAmount`.

**Rationale**: Consistent with Constitution Rule II — all tax computation happens in the Domain entity `ComputeTotals()` method. The UI sends `TaxRate` and `IsTaxInclusive` to the API; the domain recomputes.

---

## 3. Barcode Scanner Integration

**Decision**: Standard keyboard-wedge simulation. A barcode scanner acts as a keyboard emitting characters followed by `Enter`. The product search `TextBox` on invoice forms intercepts the `KeyPress` event for `Enter` and triggers an API lookup by barcode.

**Rationale**: No driver or SDK needed. Works with all USB HID barcode scanners out of the box.

---

## 4. Overpayment / Negative Balance

**Decision**: Allow overpayments. The `CustomerPayment` and `SupplierPayment` records are independent of invoice totals — they decrease the entity's balance directly. The balance can go negative (credit).

**Rationale**: Aligns with Constitution Rule XII and AGENTS.md §2.8. The UI shows a confirmation notification when a payment exceeds the current balance, but does not block the transaction.

**API behavior**: The payment endpoint sends a `POST /api/customer-payments` with `Amount` and optional `InvoiceId`. The Application Service applies `customer.DecreaseBalance(amount)` which can result in a negative `CurrentBalance`.

---

## 5. Show Deactivated Toggle

**Decision**: Each list-view API endpoint already supports `?includeInactive=true` query parameter via the existing global `IsActive` query filter pattern. The UI toggle on each list screen simply passes this flag to the API call.

**Rationale**: The EF Core global query filter (`IsActive == true`) is already in place from Phase 2. Bypassing it requires passing `ignoreQueryFilters()` conditionally — the repository `GetAllAsync(bool includeInactive)` overload will handle this.

---

## 6. Categories & Units Sub-Dialogs

**Decision**: Two lightweight modal `Form` dialogs — `CategoryManagerDialog` and `UnitManagerDialog`. Each has a small `DataGridView` (Name column only) with Add/Edit/Delete buttons. They are launched from the Product Editor dialog via small "+" icon buttons next to the Category and Unit dropdowns.

**Rationale**: These are simple single-column CRUD entities. A full `UserControl` + navigation pattern is overkill. A compact modal is faster for the user workflow.

---

## 7. Module Implementation Order

Based on dependencies, the implementation order is:

```
1. API Client Interfaces + HttpService Base     (unblocks all)
2. Products Module + Categories/Units dialogs   (foundational)
3. Customers Module                             (dependency for Sales)
4. Suppliers Module                             (dependency for Purchases)
5. Warehouses Module                            (dependency for all invoices)
6. Sales Invoice Module                         (core revenue flow)
7. Purchase Invoice Module                      (inventory inflow)
8. Sales Returns Module                         (reversal of Sales)
9. Purchase Returns Module                      (reversal of Purchases)
10. Stock Transfer Module                       (multi-warehouse ops)
11. Customer Payments Module                    (close credit cycle)
12. Supplier Payments Module                    (close credit cycle)
13. Reports Module                              (read-only, no dependencies)
14. Dashboard Module                            (aggregation, no dependencies)
```

---

## 8. Common UI Pattern (Lock-in Decision)

Every list-based module follows the **Standard Module Pattern**:

| Component | Type | Notes |
|-----------|------|-------|
| `[Entity]ListControl` | `UserControl` | DataGridView + SearchBar + Toolbar |
| `[Entity]EditorForm` | `Form` (modal) | Add/Edit fields, Save/Cancel buttons |
| `I[Entity]ApiService` | Interface | In `SalesSystem.Desktop/Services/Api/` |
| `[Entity]ApiService` | Class | HTTP calls via `HttpClientService` |
| `[Entity]ChangedMessage` | Record | EventBus message (ID only) |

**Exception**: Invoice modules (Sales, Purchase, Returns, Transfers) use a `[Invoice]Form` with embedded line-items grid instead of a separate editor — the invoice IS the editor.

---

## 9. Approved Package Addition Request

| Package | Purpose | License | Version |
|---------|---------|---------|---------|
| `ClosedXML` | Excel report export | MIT | 0.102.x |

This package needs to be added to AGENTS.md §5 before implementation of the Reports module.
