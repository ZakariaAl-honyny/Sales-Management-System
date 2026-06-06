# PRD-MVP.md — Comprehensive Gap Analysis

**Generated:** 2026-06-06
**PRD Version:** v4.6.7 (header) / v4.6.2 (internal line 1033) / v4.6.4 (version history line 6491)
**Reference:** AGENTS.md v4.6.7, Analysis Parts 3+5, Phase 19 Plan

---

## Gap Classification Key

| Severity | Meaning | Action Required |
|----------|---------|-----------------|
| 🔴 **CRITICAL** | Breaks existing code or causes data loss | Fix IMMEDIATELY |
| 🟠 **HIGH** | Causes build errors, wrong behavior, or security risk | Fix before next build |
| 🟡 **MEDIUM** | Inconsistent with AGENTS.md rules; semantic mismatch | Fix before release |
| 🔵 **LOW** | Documentation/UI inconsistency | Fix when convenient |

---

# GAP ANALYSIS

---

## GAP-001 🔴 CRITICAL — Header version mismatch

| Field | Detail |
|-------|--------|
| **Location** | Line 2 |
| **Current Text** | `# Sales Management System (v4.6.7 — InvoiceNo int Re-addition & Code Polish)` |
| **Problem** | Line 2 says v4.6.7 but line 1033 says `الإصدار: v4.6.2` and line 6491 says `v4.6.4` |
| **Why** | The PRD header was updated to v4.6.7 but internal version references were never synchronized |
| **Fix** | Update line 1033 to `الإصدار: v4.6.7` and line 6491 version history to include v4.6.7 entry |
| **References** | AGENTS.md header says v4.6.7 |

---

## GAP-002 🔴 CRITICAL — CQRS mentioned but Service Layer is the actual pattern

| Field | Detail |
|-------|--------|
| **Location** | Line 408 |
| **Current Text** | `├── SalesSystem.Application/     ← Services + Interfaces + Use Cases + CQRS` |
| **Problem** | AGENTS.md RULE-147 says "Service Layer pattern is the standard — NOT CQRS/MediatR" and RULE-148 says "MediatR package is REMOVED". The PRD still references CQRS in the architecture tree. |
| **Why** | The architecture was migrated from CQRS to Service Layer but the PRD diagram wasn't updated |
| **Fix** | Remove `+ CQRS` from line 408. Change to `├── SalesSystem.Application/     ← Services + Interfaces + Use Cases` |
| **References** | AGENTS.md RULE-147, RULE-148; also line 5038 says "**Service Layer** pattern (NOT CQRS/MediatR)" — this is correct and contradicts line 408 |

---

## GAP-003 🔴 CRITICAL — Invoice number format says PUR/INV prefix (string) but InvoiceNo is now int

| Field | Detail |
|-------|--------|
| **Location** | Lines 96, 108 |
| **Current Text** | Line 96: `Auto-generate invoice number (format: PUR-2026-000001)` |
| | Line 108: `Auto-generate invoice number (format: INV-2026-000001)` |
| **Problem** | AGENTS.md RULE-254 says `InvoiceNo` is `int` (NOT string) with no prefix. Duplicates are allowed. The project removed `InvoicePrefix` from StoreSettings (line 6201). |
| **Why** | String invoice numbers conflicted with int ID in sorting and performance. Migrated to plain int. |
| **Fix** | Replace lines 96 and 108 with: `Auto-generate invoice number (int, auto = lastId + 1, user-overridable)` |
| **References** | AGENTS.md RULE-254 through RULE-261; line 6201 deprecates InvoicePrefix |

---

## GAP-004 🔴 CRITICAL — Out of Scope section says "no accounting" but Phases 14/18 explicitly design it

| Field | Detail |
|-------|--------|
| **Location** | Lines 49-53 |
| **Current Text** | `### Out of Scope (Future Phases) - Full accounting system with general ledger` |
| **Problem** | Lines 6088-6093 (Phase 14) and 6148-6167 (Phase 18) explicitly define the Accounting Foundation: Account entity, JournalEntry, JournalEntryLine, SystemAccountMappings, JournalEntryService, 18 seeded accounts. This directly contradicts "Out of Scope". |
| **Why** | Accounting was originally out of scope but was added later. The Out of Scope section was never updated. |
| **Fix** | Remove "Full accounting system with general ledger" from Out of Scope. Move it to In Scope or add a note: "✅ Phase 14/18 — Core accounting foundation implemented (Chart of Accounts, Journal Entries, 18 accounts)" |
| **References** | Lines 6088-6093, 6148-6167 |

---

## GAP-005 🔴 CRITICAL — SalesInvoice.Create() missing `int invoiceNo` parameter

| Field | Detail |
|-------|--------|
| **Location** | Lines 2464-2490 |
| **Current Text** | `public static SalesInvoice.Create(int customerId, int warehouseId, PaymentType paymentType, decimal paidAmount, string? notes, int? createdByUserId)` — NO `invoiceNo` parameter |
| **Problem** | AGENTS.md RULE-255 says `SalesInvoice.Create()` requires `int invoiceNo` (second param). The PRD entity code still shows the old signature. |
| **Fix** | Add `int invoiceNo` as second parameter to `SalesInvoice.Create()` |
| **References** | AGENTS.md RULE-255 |

---

## GAP-006 🔴 CRITICAL — PurchaseInvoice entity missing entirely from C# entity section

| Field | Detail |
|-------|--------|
| **Location** | Lines 3200-3700 (approximate) — whole Application Services section |
| **Problem** | The C# entity code section (lines 2249-2570) only shows Product, WarehouseStock, SalesInvoice. PurchaseInvoice entity is completely missing from Domain Entity examples. |
| **Why** | PurchaseInvoice was presumably identical in structure but still needs documentation |
| **Fix** | Add PurchaseInvoice entity with `int invoiceNo` parameter (RULE-255), `SupplierInvoiceNo` (string?), and `TaxId` fields |

---

## GAP-007 🔴 CRITICAL — SalesInvoice and PurchaseInvoice entity schemas in SQL missing `InvoiceNo` int column

| Field | Detail |
|-------|--------|
| **Location** | Lines 1208-1227 (PurchaseInvoices table), 1248-1266 (SalesInvoices table), 1714-1735 (SQL CREATE) |
| **Problem** | Both table definitions do NOT include an `InvoiceNo int NOT NULL` column. Lines 1210-1228 show columns: SupplierId, WarehouseId, InvoiceDate, DueDate, PaymentType, SubTotal, DiscountAmount, TaxAmount, TotalAmount, PaidAmount, DueAmount, Notes, Status, audit — but NO `InvoiceNo`. |
| **Why** | The schema was written before InvoiceNo was re-introduced as int. |
| **Fix** | Add `InvoiceNo int NOT NULL` to both SalesInvoices and PurchaseInvoices tables. Also add `SupplierInvoiceNo nvarchar(50) null` to PurchaseInvoices. |
| **References** | AGENTS.md RULE-254, RULE-258 |

---

## GAP-008 🟠 HIGH — CostingMethod enum values WRONG (0,1,2 instead of 1,2,3)

| Field | Detail |
|-------|--------|
| **Location** | Lines 2590-2594 |
| **Current Text** | `WeightedAverage = 0, LastPurchasePrice = 1, SupplierPrice = 2` |
| **Required Value** | `WeightedAverage = 1, LastPurchasePrice = 2, SupplierPrice = 3` |
| **Why** | AGENTS.md §3 Enums explicitly defines these values. The PRD advanced modules section conflicts with both AGENTS.md and line 196-200 of the PRD itself (which correctly shows 1,2,3). |
| **Fix** | Change line 2591 to `WeightedAverage = 1`, line 2592 to `LastPurchasePrice = 2`, line 2593 to `SupplierPrice = 3` |
| **References** | AGENTS.md RULE-207; PRD lines 196-200 (correct) vs 2590-2594 (wrong) |

---

## GAP-009 🟠 HIGH — CashTransactionType enum values WRONG (0-5 instead of 1-8)

| Field | Detail |
|-------|--------|
| **Location** | Lines 2597-2605 |
| **Current Text** | `SaleIn = 0, PurchaseOut = 1, TransferIn = 2, TransferOut = 3, ManualIn = 4, ManualOut = 5` |
| **Required Value** | `OpeningBalance = 1, SalesIncome = 2, Expense = 3, TransferOut = 4, TransferIn = 5, RefundOut = 6, SupplierPayment = 7, CustomerPayment = 8` |
| **Why** | The PRD advanced modules section has a simplified/incorrect enum. Both AGENTS.md §3 and PRD lines 226-236 (correct) define the authoritative 8-value enum. |
| **Fix** | Replace lines 2597-2605 with the correct 8-value enum from AGENTS.md §3 |
| **References** | AGENTS.md RULE-208; PRD lines 226-236 (correct) vs 2597-2605 (wrong) |

---

## GAP-010 🟠 HIGH — SalesInvoiceDto missing `InvoiceNo` (int)

| Field | Detail |
|-------|--------|
| **Location** | Lines 3102-3118 |
| **Current Text** | `SalesInvoiceDto` has: SalesInvoiceId, CustomerId, CustomerName, WarehouseId, WarehouseName, InvoiceDate, PaymentType, SubTotal, DiscountAmount, TaxAmount, TotalAmount, PaidAmount, DueAmount, Status, Items — NO `InvoiceNo` |
| **Fix** | Add `int InvoiceNo` as the second field after `SalesInvoiceId` |
| **References** | AGENTS.md RULE-259 |

---

## GAP-011 🟠 HIGH — SalesInvoiceItemDto missing `SaleMode` and `ProductUnitId`

| Field | Detail |
|-------|--------|
| **Location** | Lines 3121-3128 |
| **Current Text** | `SalesInvoiceItemDto` has: ProductId, ProductName, Quantity, UnitPrice, DiscountAmount, LineTotal — NO `SaleMode`, NO `ProductUnitId` |
| **Why** | v4.3 introduced per-unit pricing and SaleMode (Retail/Wholesale). The DTO must carry these to support different pricing strategies. |
| **Fix** | Add `int? ProductUnitId` and `string SaleMode` (or `int SaleMode`) to `SalesInvoiceItemDto` |
| **References** | AGENTS.md RULE-049, RULE-065 |

---

## GAP-012 🟠 HIGH — CreateSalesInvoiceRequest missing `int? InvoiceNo`

| Field | Detail |
|-------|--------|
| **Location** | Lines 3132-3147 |
| **Current Text** | `CreateSalesInvoiceRequest` has: CustomerId, WarehouseId, PaymentType, PaidAmount, Notes, Items — NO `InvoiceNo` field |
| **Fix** | Add `int? InvoiceNo` as optional field (null/≤0 = auto-generate per RULE-256) |
| **References** | AGENTS.md RULE-256 |

---

## GAP-013 🟠 HIGH — ReturnNo/TransferNo/PaymentNo are `nvarchar(30)` should be `int`

| Field | Detail |
|-------|--------|
| **Location** | Lines 1288, 1319, 1352, 1381, 1400 (schema) and 1832, 1863, 1894, 1921, 1940 (SQL) |
| **Current Text** | `ReturnNo nvarchar(30) not null unique`, `TransferNo nvarchar(30) not null unique`, `PaymentNo nvarchar(30) not null unique` |
| **Problem** | Following InvoiceNo pattern (RULE-254), all document numbers should be `int`. The nvarchar prefix format (SR-2026-000001) is deprecated. |
| **Fix** | Change all to `int NOT NULL` (no unique constraint — duplicates allowed per RULE-261 pattern). Remove the string prefix pattern from all return/transfer/payment entities. |
| **References** | AGENTS.md RULE-254 through RULE-261 (applied consistently to all document numbers) |

---

## GAP-014 🟠 HIGH — DocumentSequences table still exists but should be removed/limited

| Field | Detail |
|-------|--------|
| **Location** | Lines 384, 440, 588, 794, 1955-1962, 6015 |
| **Current Text** | `DocumentSequences → Auto-increment invoice numbers` (line 384) |
| | Full CREATE TABLE for DocumentSequences (line 1955-1962) |
| | `DocumentSequenceService.cs ⚠️ Thread-safe` (line 440) |
| **Problem** | AGENTS.md RULE-195 says "Code auto-generation services (DocumentSequenceService for PRD/CUST/SUP/WH) are REMOVED". Line 794 says "Remove DocumentSequenceService for entity codes (keep only for invoices)". But invoices now use int InvoiceNo (no prefix). DocumentSequences is now only needed for other entity types if any — but the PRD doesn't specify which. |
| **Fix** | Either: (a) Remove DocumentSequences entirely if all document numbers are int (auto-increment Id suffices), OR (b) Document that DocumentSequences is ONLY for entity codes like PRD/CUST/SUP/WH (which were removed per RULE-191). Also remove from service layer diagram (line 440). |
| **References** | AGENTS.md RULE-191, RULE-195 |

---

## GAP-015 🟠 HIGH — Missing `InvoiceNo` on SalesInvoiceDto and PurchaseInvoiceDto in contracts section

| Field | Detail |
|-------|--------|
| **Location** | Lines 3102-3118 |
| **Current Text** | SalesInvoiceDto has NO `InvoiceNumber` or `InvoiceNo` |
| **Fix** | Add `int InvoiceNo` to SalesInvoiceDto and any equivalent PurchaseInvoiceDto |

---

## GAP-016 🟠 HIGH — `InvoicePrintDto.InvoiceNumber` not defined as `string` formatted from int

| Field | Detail |
|-------|--------|
| **Location** | Print section lines 5464-5496 |
| **Problem** | InvoicePrintDto is mentioned but its InvoiceNumber field is not documented as `string InvoiceNumber` formatted from `InvoiceNo.ToString()` per RULE-260 |
| **Fix** | Document `InvoicePrintDto.InvoiceNumber` as `string` derived from `InvoiceNo.ToString()` in the Print DTOs section |

---

## GAP-017 🟠 HIGH — SalesInvoice entity `Create()` method missing `InvoiceNo` parameter in domain code example

| Field | Detail |
|-------|--------|
| **Location** | Lines 2464-2470 |
| **Current Text** | `public static SalesInvoice Create(int customerId, int warehouseId, PaymentType paymentType, decimal paidAmount, string? notes, int? createdByUserId)` |
| **Fix** | Change to: `public static SalesInvoice Create(int invoiceNo, int customerId, int warehouseId, PaymentType paymentType, decimal paidAmount, string? notes, int? createdByUserId)` with guard `if (invoiceNo <= 0) throw new DomainException("رقم الفاتورة غير صالح");` |
| **References** | AGENTS.md RULE-255 |

---

## GAP-018 🟠 HIGH — Entity Summary (Section 5) missing 20+ accounting/foundation entities

| Field | Detail |
|-------|--------|
| **Location** | Lines 360-391 |
| **Current Text** | Lists 32 entities (Users through SystemLog) |
| **Missing Entities** | |
| | 1. `Account` — Chart of Accounts (Phase 14/18, 18 accounts seeded) |
| | 2. `JournalEntry` — Double-entry journal (Phase 14/18) |
| | 3. `JournalEntryLine` — Journal entry lines (Phase 14/18) |
| | 4. `SystemAccountMappings` — Maps 13 system accounts (Phase 18) |
| | 5. `Tax` — Tax rate management (Phase 19, line 6189) |
| | 6. `FiscalYear` — Fiscal year configuration |
| | 7. `InventoryBatch` — Batch tracking for FIFO/FEFO |
| | 8. `Currency` — Multi-currency support |
| | 9. `StockWriteOff` — Expired/damaged stock write-off (Phase 14, line 817) |
| **Fix** | Add all missing entities to the summary list in Section 5 |

---

## GAP-019 🟠 HIGH — Missing fields on existing entities

| Field | Detail |
|-------|--------|
| **Location** | Throughout entity definitions |
| **Missing Fields** | |
| | 1. `Customer.AccountId` (int? FK → Accounts) — Links customer to AR account |
| | 2. `Supplier.AccountId` (int? FK → Accounts) — Links supplier to AP account |
| | 3. `Product.TrackExpiry` (bit) — Whether product tracks expiration dates (Phase 14, line 820) |
| | 4. `Product.TrackBatch` (bit) — Whether product tracks batches for FIFO/FEFO |
| | 5. `Product.ExpirationDate` (DateTime?) — Already in Phase 14 (line 815) |
| | 6. `Product.ImagePath` (string?) — Already in Phase 14 (line 816) |
| | 7. `User.MustChangePassword` (bit) — Password change enforcement |
| | 8. `CashBox.AccountId` (int? FK → Accounts) — Links cash box to GL account |
| | 9. `SalesInvoice.TaxId` (int? FK → Taxes) — Already in Phase 19 (line 6191) |
| | 10. `PurchaseInvoice.TaxId` (int? FK → Taxes) — Already in Phase 19 (line 6191) |
| | 11. `SalesInvoice.CashBoxId` (int? FK → CashBoxes) — Already in SQL (line 2213) but not in entity definition |
| | 12. `PurchaseInvoice.CashBoxId` (int? FK → CashBoxes) — Same issue |
| **Fix** | Add all missing fields to the entity definitions in Sections 3 and 5 |

---

## GAP-020 🔴 CRITICAL — Implementation Phases (Section 7, lines 558+) has only 13 phases — doesn't match 16+ phase plan

| Field | Detail |
|-------|--------|
| **Location** | Lines 558-805 |
| **Current Text** | Lists Phases 1-13 ending at "Identifier Strategy & Validation (v4.5.3–v4.6.2)" |
| **Missing Phases** | |
| | Phase 14 — Product Lifecycle & Media Management (lines 810-842) |
| | Phase 15 — Touch-Optimized Quick POS (lines 859-898) |
| | Phase 16 — Critical Business Rules Reference (lines 911-930) |
| | Phase 17 — Collapsible Tree Sidebar (lines 934-1021) |
| | Phase 18 — Accounting Foundation (lines 1023-1026, also 6148-6167) |
| | Phase 19 — Settings Module (lines 6176-6278) |
| | Phase 20 — Security Hardening (lines 6336-6358) |
| **Why** | The original Phase listing in Section 7 was never updated to include Phases 14-31 even though they appear later in the document |
| **Fix** | Add sections for Phases 14-20+ to the Implementation Phases list (lines 558+) with consistent numbering |

---

## GAP-021 🔴 CRITICAL — Phase numbering discontinuity (Section 7 vs Section 14)

| Field | Detail |
|-------|--------|
| **Location** | Lines 558-805 vs 6088-6358 |
| **Current Text** | Section 7 has Phases 1-13. Then Phases 14-20 appear in Section 14 (Implementation History). Phase 18 appears TWICE: once as Product Lifecycle (line 811?) and once as Accounting Foundation (lines 1023, 6148). |
| **Problem** | Phase numbering is completely inconsistent: |
| | - Phase 14 in Section 7 = Product Lifecycle |
| | - Phase 14 in Section 14 = Accounting Foundation |
| | - Phase 18 in Section 7 = Accounting Foundation |
| | - Phase 18b in Section 14 = Accounting Foundation (duplicate) |
| | - Phase 19 = Settings (appears in Section 14 only) |
| | - Phase 20 = Security Hardening (appears in Section 14 only) |
| **Fix** | Renumber ALL phases into a single authoritative sequence spanning Phases 1-20+ |

---

## GAP-022 🟠 HIGH — SQL script has `DECIMAL(18,4)` in v4.3 migration section

| Field | Detail |
|-------|--------|
| **Location** | Lines 2106-2109, 2152, 2168-2170, 2228-2229 |
| **Current Text** | `DECIMAL(18,4)` used for SalesPrice, PurchaseCost, SupplierPrice, LastPurchasePrice, CurrentBalance, Amount, BalanceBefore, BalanceAfter, OldValue, NewValue |
| **Problem** | AGENTS.md RULE-211 says ALL money fields MUST use `decimal(18,2)` — never `decimal(18,4)`. This applies to ALL SQL in the document. |
| **Fix** | Replace all `DECIMAL(18,4)` with `DECIMAL(18,2)` in the v4.3 migration section |

---

## GAP-023 🟠 HIGH — Product entity missing `ExpirationDate`, `ImagePath`, `TrackExpiry`, `TrackBatch`

| Field | Detail |
|-------|--------|
| **Location** | Lines 1122-1135 (schema), 1643-1660 (SQL), 2277-2368 (C# entity) |
| **Current Text** | Product has: Id, Barcode, Name, CategoryId, SupplierPrice, ReorderLevel, Description, audit fields — NO expiry/expiration fields |
| **Fix** | Add `ExpirationDate datetime2 null`, `ImagePath nvarchar(255) null`, `TrackExpiry bit not null default 0`, `TrackBatch bit not null default 0` to all Product definitions |
| **References** | PRD Phase 14 (lines 814-817) |

---

## GAP-024 🟠 HIGH — `Users` table missing `MustChangePassword` field

| Field | Detail |
|-------|--------|
| **Location** | Lines 1082-1093 (schema), 1586-1601 (SQL) |
| **Current Text** | Users has: Id, UserName, PasswordHash, FullName, Role, CreatedByUserId, UpdatedByUserId, IsActive, CreatedAt, UpdatedAt — NO `MustChangePassword` |
| **Fix** | Add `MustChangePassword bit not null default 0` |
| **Why** | Standard security practice for first-login password change enforcement |

---

## GAP-025 🟠 HIGH — `Customer` and `Supplier` missing `AccountId` FK

| Field | Detail |
|-------|--------|
| **Location** | Lines 1171-1201 (schema), 1680-1711 (SQL) |
| **Current Text** | Customer/Supplier have no link to chart of accounts |
| **Fix** | Add `AccountId int null FK → Accounts(Id)` to both Customer and Supplier entities |
| **Why** | Phase 14/18 accounting requires customer AR and supplier AP links |

---

## GAP-026 🟠 HIGH — `CashBox` missing `AccountId` FK

| Field | Detail |
|-------|--------|
| **Location** | Lines 1489-1498 (schema), 2000-2012 (SQL), 2780-2877 (C#) |
| **Current Text** | CashBox has no link to chart of accounts |
| **Fix** | Add `AccountId int null FK → Accounts(Id)` |
| **Why** | Phase 18 accounting requires cash box → GL account mapping |

---

## GAP-027 🟠 HIGH — `SalesInvoice` and `PurchaseInvoice` missing `TaxId` FK in entity definitions

| Field | Detail |
|-------|--------|
| **Location** | Lines 1208-1227, 1248-1266 (entity schema), 1714-1771 (SQL) |
| **Current Text** | No TaxId field in SalesInvoices or PurchaseInvoices |
| **Problem** | Phase 19 (line 6191) says "Added `int? TaxId` + `Tax` nav property to `SalesInvoice` and `PurchaseInvoice`" — but the entity definitions in Sections 3 and SQL were never updated |
| **Fix** | Add `TaxId int null FK → Taxes(Id)` to both SalesInvoices and PurchaseInvoices entity/SQL definitions |

---

## GAP-028 🟠 HIGH — `CashBoxId` FK missing from entity definitions

| Field | Detail |
|-------|--------|
| **Location** | Lines 1208-1227, 1248-1266 (entity schema) |
| **Current Text** | No CashBoxId in SalesInvoices or PurchaseInvoices entity definitions |
| **Problem** | The SQL script (lines 2213-2219) adds `CashBoxId int null` to both invoice tables, but the entity schema definitions in Section 4 (lines 1208-1266) don't include it |
| **Fix** | Add `CashBoxId int null FK → CashBoxes(Id)` to both SalesInvoice and PurchaseInvoice entity definitions |

---

## GAP-029 🟠 HIGH — Missing `FifoFefo` batch inventory tracking entities and fields

| Field | Detail |
|-------|--------|
| **Location** | Lines 360-391 (entity summary), 6211 (SystemSettings mentions EnableFefo) |
| **Current Text** | Line 6211 mentions `EnableFefo` in SystemSettings but NO: |
| | - `InventoryBatch` entity defined anywhere |
| | - `Product.TrackBatch` field |
| | - `Product.TrackExpiry` field |
| | - `InventoryMovements.BatchId` field |
| | - `SalesInvoiceItem.BatchId` / `PurchaseInvoiceItem.BatchId` fields |
| **Why** | FEFO/FIFO was discussed in Analysis Part 3 (lines 1748-1784) and EnableFefo is seeded (line 6211) but no batch tracking entities were ever defined in the PRD |
| **Fix** | Add InventoryBatch entity with fields: Id, ProductId, WarehouseId, BatchNo, ExpiryDate, Quantity, Cost, CreatedAt. Add BatchId FK to inventory movement and invoice item tables. |
| **References** | Analysis Part 3 lines 1748-1784 |

---

## GAP-030 🟠 HIGH — Missing `FiscalYear` entity

| Field | Detail |
|-------|--------|
| **Location** | Lines 360-391 |
| **Problem** | Accounting foundation requires fiscal year management for journal entries and financial reporting. No FiscalYear entity exists in any table definition. |
| **Fix** | Add FiscalYear entity: Id, Name (nvarchar 20), StartDate, EndDate, IsClosed (bit), ClosedAt |

---

## GAP-031 🟠 HIGH — Missing `Currency` entity

| Field | Detail |
|-------|--------|
| **Location** | Lines 1425 (CurrencyCode in StoreSettings) |
| **Current Text** | CurrencyCode is a simple nvarchar(10) field in StoreSettings |
| **Problem** | No multi-currency support entity. Future accounting may need multiple currencies. |
| **Fix** | Add Currency entity: Id, Code (nvarchar 10), Name (nvarchar 50), Symbol (nvarchar 10), ExchangeRate (decimal 18,6), IsDefault (bit) |

---

## GAP-032 🟠 HIGH — `PurchaseInvoice.Create()` missing `SupplierInvoiceNo` and `invoiceNo` parameters

| Field | Detail |
|-------|--------|
| **Location** | PurchaseInvoice entity section (not shown in C# examples) |
| **Problem** | PurchaseInvoice.C# entity not shown in PRD. Per RULE-255 and RULE-258, it needs: `int invoiceNo` (second param) and `string? supplierInvoiceNo` as optional supplier reference. |
| **Fix** | Add PurchaseInvoice entity with full Create() method showing both parameters |

---

# Architecture & Code Pattern Gaps

---

## GAP-033 🟠 HIGH — MediatR still listed as dependency (line 5252)

| Field | Detail |
|-------|--------|
| **Location** | Line 5252 |
| **Current Text** | `│   └── MediatR (installed, minimally used)` |
| **Fix** | Remove MediatR dependency. AGENTS.md RULE-148 says "MediatR package is REMOVED". |
| **References** | AGENTS.md RULE-148 |

---

## GAP-034 🟠 HIGH — Save buttons disabled pattern wrong

| Field | Detail |
|-------|--------|
| **Location** | Line 5097 |
| **Current Text** | `Save buttons disabled via CanExecute when HasErrors is true` |
| **Problem** | AGENTS.md RULE-059 says "Save buttons ALWAYS enabled (no CanExecute blocking). On click, Validate() shows warning dialog." |
| **Fix** | Change to: `Save buttons always enabled — validate on click with warning dialog listing all missing fields` |
| **References** | AGENTS.md RULE-059 |

---

## GAP-035 🟠 HIGH — `ExecuteAsync()` wrapper NOT implemented

| Field | Detail |
|-------|--------|
| **Location** | Line 6402 |
| **Current Text** | `ExecuteAsync() wrapper — ❌ Not in ViewModelBase — Error handling uses HandleException() / HandleFailure()` |
| **Problem** | AGENTS.md RULE-141 says "ALL ViewModel async commands MUST use ExecuteAsync() wrapper — NEVER manual try/catch/finally". The actual code doesn't have it. |
| **Fix** | Add `ExecuteAsync()` implementation to ViewModelBase and update all ViewModels to use it |
| **References** | AGENTS.md RULE-141 through RULE-146 |

---

## GAP-036 🟠 HIGH — `DocumentSequenceService` still in Application Services list

| Field | Detail |
|-------|--------|
| **Location** | Line 440 |
| **Current Text** | `│   │   └── DocumentSequenceService.cs ⚠️ Thread-safe` |
| **Problem** | AGENTS.md RULE-195 says code auto-generation services are REMOVED. Line 794 says "Remove DocumentSequenceService for entity codes (keep only for invoices)". But if InvoiceNo is now int (no prefix), there's no need for DocumentSequenceService at all. |
| **Fix** | Remove DocumentSequenceService from the architecture diagram or document its limited remaining scope (if any). |

---

## GAP-037 🟠 HIGH — `Product` C# entity missing `AvgCost` field

| Field | Detail |
|-------|--------|
| **Location** | Lines 2277-2368 |
| **Current Text** | Product has: Barcode, Name, CategoryId, SupplierPrice, ReorderLevel, Description — NO `AvgCost` |
| **Problem** | The actual code pattern (line 5165) shows `public decimal AvgCost { get; private set; }` on Product. The PRD entity is missing this field. |
| **Fix** | Add `AvgCost decimal(18,2)` to Product entity definition |

---

## GAP-038 🟠 HIGH — `ProductUnit` C# entity field naming inconsistency

| Field | Detail |
|-------|--------|
| **Location** | Lines 2611-2628 vs 1461-1473 |
| **Problem** | Two different ProductUnit signatures exist: |
| | - Lines 1461-1473 (schema): Uses `UnitName`, `ConversionFactor`, `RetailPrice`, `WholesalePrice`, `Cost`, `IsBaseUnit` |
| | - Lines 2611-2628 (C#): Uses `UnitName`, `BaseConversionFactor`, `SalesPrice`, `PurchaseCost`, `SupplierPrice`, `LastPurchasePrice`, `IsBaseUnit` |
| | The names and fields are completely different between the schema and C# sections. |
| **Fix** | Harmonize ProductUnit fields across all sections. The schema version (lines 1461-1473) is the correct one per AGENTS.md RULE-065: RetailPrice, WholesalePrice, Cost, ConversionFactor |

---

# Settings & Configuration Gaps

---

## GAP-039 🟠 HIGH — Settings section (Section 3.11, lines 154-159) too minimal for Phase 19

| Field | Detail |
|-------|--------|
| **Location** | Lines 154-159 |
| **Current Text** | Only 4 settings items: store name/phone/address, logo upload, default tax rate/toggle, default warehouse |
| **Problem** | Phase 19 (lines 6176-6278) delivers: Company Settings (8 fields), System Settings (23 key-value pairs across 7 categories), Tax CRUD (3 seeded taxes), Print Settings (4 settings), Store Settings (SignaturePath added). The PRD overview section still shows the old minimal settings. |
| **Fix** | Replace Section 3.11 with comprehensive 5-category settings overview matching Phase 19 delivery |

---

## GAP-040 🟠 HIGH — StoreSettings schema missing `SignaturePath` and `Email`

| Field | Detail |
|-------|--------|
| **Location** | Lines 1418-1433 |
| **Current Text** | StoreSettings has: StoreName, Phone, Address, LogoPath, CurrencyCode, DefaultTaxRate, IsTaxEnabled — NO `SignaturePath`, NO `Email` |
| **Fix** | Add `SignaturePath nvarchar(255) null` and `Email nvarchar(100) null` |
| **References** | Phase 19 Plan section 2.1 |

---

## GAP-041 🟠 HIGH — `StoreSettings.DefaultTaxRate` and `IsTaxEnabled` marked as deprecated but still in schema

| Field | Detail |
|-------|--------|
| **Location** | Lines 1426-1427 |
| **Current Text** | These fields exist with no deprecation notice |
| **Fix** | Add deprecation comment: `-- DEPRECATED: Use Tax entity instead. Kept for backwards compatibility.` |
| **References** | Phase 19 Plan lines 54-61 |

---

## GAP-042 🟠 HIGH — Missing `InvoicePrintDto.InvoiceNumber` definition

| Field | Detail |
|-------|--------|
| **Location** | Print section lines 5464-5496 |
| **Problem** | AGENTS.md RULE-260 says `InvoicePrintDto` uses `string InvoiceNumber` formatted from `InvoiceNo.ToString()`. The Print DTOs section doesn't define this properly. |
| **Fix** | Add explicit definition: `public string InvoiceNumber { get; set; }` in InvoicePrintDto, populated as `invoiceNo.ToString()` |

---

# WPF & Desktop Gaps

---

## GAP-043 🟠 HIGH — `ShutdownMode` not documented

| Field | Detail |
|-------|--------|
| **Location** | Nowhere in PRD |
| **Problem** | AGENTS.md RULE-178 says `App.xaml` MUST use `ShutdownMode="OnExplicitShutdown"`. Not mentioned in PRD. |
| **Fix** | Add this requirement to Non-Functional Requirements section |

---

## GAP-044 🟠 HIGH — Arabic encoding requirement (RULE-249-253) not in PRD

| Field | Detail |
|-------|--------|
| **Location** | Nowhere in PRD |
| **Problem** | AGENTS.md RULE-249-253 require all Arabic string literals to be valid UTF-8. PRD doesn't mention this. |
| **Fix** | Add encoding requirement to Non-Functional Requirements section |

---

## GAP-045 🟠 HIGH — `LogSystemError()` centralized logging pattern not documented in PRD

| Field | Detail |
|-------|--------|
| **Location** | Nowhere in PRD |
| **Problem** | AGENTS.md RULE-199 says `LogSystemError()` is the ONLY method for system error logging in ALL ViewModels. PRD doesn't document this. |
| **Fix** | Add to ViewModel pattern section (lines 5063-5090) |

---

## GAP-046 🟠 HIGH — Rate limiting requirement not documented in PRD

| Field | Detail |
|-------|--------|
| **Location** | Nowhere in Security section (lines 331-338) |
| **Problem** | AGENTS.md RULE-240-243 require rate limiting on login (5/15min) and global (100 req/min). Phase 20 (line 6341) implements this. Not in PRD security requirements. |
| **Fix** | Add rate limiting requirements to Section 4.2 Security |

---

## GAP-047 🟠 HIGH — User hard-delete protection not in PRD

| Field | Detail |
|-------|--------|
| **Location** | Nowhere in PRD |
| **Problem** | AGENTS.md RULE-244-246 require `UserService.PermanentDeleteAsync()` to return `Result.Failure`. Not in PRD. |
| **Fix** | Add user deletion rules to Section 3 (Authentication) |

---

## GAP-048 🟠 HIGH — Connection string security not in PRD requirements

| Field | Detail |
|-------|--------|
| **Location** | Lines 331-338 (Security section) |
| **Current Text** | Mentions JWT, BCrypt, FluentValidation — but NO mention of encrypted connection strings or env var policy |
| **Fix** | Add: `- Connection strings MUST use DPAPI encryption or environment variables — NEVER hardcoded` |
| **References** | AGENTS.md RULE-247, RULE-248 |

---

# Cross-Reference & Documentation Gaps

---

## GAP-049 🟡 MEDIUM — Duplicate "Phase 18" numbering (Accounting Foundation)

| Field | Detail |
|-------|--------|
| **Location** | Lines 1023-1026, 6148-6167 |
| **Problem** | "Phase 18 — Accounting Foundation" appears TWICE: once as a heading (line 1023) linking to a separate file, and once as "Phase 18b" (line 6148) with full implementation details. |
| **Fix** | Renumber one of these. Suggest: line 1023 → Phase 14 (Accounting Foundation) to match Section 14 numbering. |

---

## GAP-050 🟡 MEDIUM — Phase 14 Product Lifecycle (line 810) has no Phase number in Section 14

| Field | Detail |
|-------|--------|
| **Location** | Lines 810-842 |
| **Problem** | Product Lifecycle & Media Management is called "Phase 14" in Section 7 but does NOT appear in the Section 14 chronology at all. |
| **Fix** | Add Phase 14 (Product Lifecycle) to Section 14.1 implementation history |

---

## GAP-051 🟡 MEDIUM — Phase 15 (Touch POS) and Phase 17 (Sidebar) not in Section 14

| Field | Detail |
|-------|--------|
| **Location** | Lines 859-898 (Phase 15), 934-1021 (Phase 17) |
| **Problem** | These phases exist as documentation but are not included in the official Implementation History (Section 14.1) |
| **Fix** | Add entries for Phases 15 and 17 in Section 14.1 |

---

## GAP-052 🟡 MEDIUM — `SupplierInvoiceNo` field missing from PurchaseInvoice

| Field | Detail |
|-------|--------|
| **Location** | Lines 1208-1227 |
| **Current Text** | PurchaseInvoice fields don't include `SupplierInvoiceNo nvarchar(50) null` |
| **Fix** | Add `SupplierInvoiceNo nvarchar(50) null` to PurchaseInvoice entity definition |
| **References** | AGENTS.md RULE-258 |

---

## GAP-053 🟡 MEDIUM — `SaleMode` and `ProductUnitId` missing from SalesInvoiceItem

| Field | Detail |
|-------|--------|
| **Location** | Lines 1271-1280 (schema), 1774-1783 (SQL) |
| **Current Text** | SalesInvoiceItem has: SalesInvoiceItemId, SalesInvoiceId, ProductId, Quantity, UnitPrice, DiscountAmount, LineTotal — NO `SaleMode`, NO `ProductUnitId` |
| **Fix** | Add `SaleMode tinyint not null default 1`, `ProductUnitId int null FK → ProductUnits(Id)` |
| **References** | AGENTS.md RULE-049 |

---

## GAP-054 🟡 MEDIUM — `ReturnNo`/`TransferNo`/`PaymentNo` still nvarchar but should be int

| Field | Detail |
|-------|--------|
| **Location** | Lines 1288, 1319, 1352, 1381, 1400 (schema) and 1832, 1863, 1894, 1921, 1940 (SQL) |
| **Current Text** | NVARCHAR(30) with UNIQUE constraint |
| **Why** | Following the InvoiceNo int re-addition, ALL document numbers should be int for consistency |
| **Fix** | Change to `int NOT NULL` (remove UNIQUE, duplicates allowed) |

---

## GAP-055 🟡 MEDIUM — `InvoicePrintDto` not fully defined in Contracts section

| Field | Detail |
|-------|--------|
| **Location** | Lines 5480-5496 |
| **Current Text** | Only file structure listed, no actual DTO properties |
| **Fix** | Add full InvoicePrintDto definition with: InvoiceNumber (string, formatted from int), InvoiceDate, CustomerName, Items, SubTotal, DiscountAmount, TaxAmount, TotalAmount, etc. |

---

## GAP-056 🟡 MEDIUM — Missing `PurchaseInvoiceDto` definition entirely

| Field | Detail |
|-------|--------|
| **Location** | Lines 3102-3128 |
| **Current Text** | Only SalesInvoiceDto defined. No PurchaseInvoiceDto, PurchaseInvoiceItemDto, SalesReturnDto, PurchaseReturnDto in the contracts section. |
| **Fix** | Add all missing DTO definitions |

---

## GAP-057 🟡 MEDIUM — `ErrorCodes` missing `RATE_LIMIT_EXCEEDED`

| Field | Detail |
|-------|--------|
| **Location** | Lines 3054-3065 |
| **Current Text** | ErrorCodes has 8 constants but NOT `RATE_LIMIT_EXCEEDED` |
| **Fix** | Add `public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";` |
| **References** | AGENTS.md RULE-242 |

---

## GAP-058 🟡 MEDIUM — `ErrorCodes` missing `DuplicateCode` still referenced

| Field | Detail |
|-------|--------|
| **Location** | Lines 3054-3065 |
| **Problem** | AGENTS.md RULE-197 says `DuplicateCode` error constant is REMOVED. The ErrorCodes list doesn't have it, so this is OK — but let's verify no remaining references. |
| **Status** | Verify no references in the PRD text. The PRD correctly removed it. ✅ |

---

## GAP-059 🟡 MEDIUM — `IsBusy` vs `IsLoading` inconsistency

| Field | Detail |
|-------|--------|
| **Location** | Line 5088 |
| **Current Text** | `public bool IsBusy { get; protected set; }` |
| **Problem** | AGENTS.md RULE-142 says `IsBusy` replaces `IsLoading`. The PRD shows `IsBusy` which is correct — but some older screen patterns may still use `IsLoading`. |
| **Fix** | Add a note that `IsLoading` is deprecated in favor of `IsBusy` |

---

## GAP-060 🟡 MEDIUM — No mention of UI compacting rules (RULE-262-274)

| Field | Detail |
|-------|--------|
| **Location** | Nowhere in PRD |
| **Problem** | AGENTS.md RULE-262-274 specify mobile-ready density: no hardcoded Height=36/40 on buttons, compact padding, FontSize limits, sidebar width=200, etc. PRD doesn't include these. |
| **Fix** | Add UI compacting requirements to Non-Functional Requirements |

---

## GAP-061 🟡 MEDIUM — No mention of newest-first sorting (RULE-220-223)

| Field | Detail |
|-------|--------|
| **Location** | Nowhere in PRD Functional Requirements |
| **Problem** | AGENTS.md RULE-220-223 require all lists to display newest records first. Not mentioned in PRD. |
| **Fix** | Add sorting requirement to Non-Functional Requirements |

---

## GAP-062 🟡 MEDIUM — No mention of Arabic ToolTips requirement (RULE-185-190)

| Field | Detail |
|-------|--------|
| **Location** | Nowhere in PRD |
| **Problem** | AGENTS.md RULE-185-190 require ALL interactive controls to have Arabic ToolTips. Not in PRD. |
| **Fix** | Add ToolTip requirements to UI section |

---

## GAP-063 🟡 MEDIUM — `SalesInvoiceItem` missing `Notes` field

| Field | Detail |
|-------|--------|
| **Location** | Lines 1271-1280, 1774-1783 |
| **Current Text** | SalesInvoiceItem has NO Notes field |
| **Problem** | PurchaseInvoiceItem has Notes (line 1240), SalesInvoiceItem should have the same |
| **Fix** | Add `Notes nvarchar(250) null` to SalesInvoiceItem |

---

## GAP-064 🟡 MEDIUM — Missing `Cost` field in PurchaseInvoiceItem schema

| Field | Detail |
|-------|--------|
| **Location** | Lines 1237 |
| **Current Text** | `UnitCost decimal(18,2) not null` — OK but missing `Cost` (same as AvgCost) for item cost tracking |
| **Fix** | Add `Cost decimal(18,2) null` to track the actual cost at time of purchase |

---

## GAP-065 🟡 MEDIUM — `PurchaseInvoiceItem` missing `ProductUnitId`

| Field | Detail |
|-------|--------|
| **Location** | Lines 1232-1241 |
| **Current Text** | PurchaseInvoiceItem has: PurchaseInvoiceItemId, PurchaseInvoiceId, ProductId, Quantity, UnitCost, DiscountAmount, LineTotal, Notes — NO `ProductUnitId` |
| **Fix** | Add `ProductUnitId int null FK → ProductUnits(Id)` to link purchase line to specific unit |

---

## GAP-066 🔵 LOW — `SalesInvoiceItem` SQL table uses `SalesInvoiceItemId` instead of `Id`

| Field | Detail |
|-------|--------|
| **Location** | Line 1776 |
| **Current Text** | `SalesInvoiceItemId INT IDENTITY(1,1) NOT NULL` |
| **Problem** | All other entities use `Id` as PK name. This inconsistency breaks BaseEntity pattern. |
| **Fix** | Change to `Id INT IDENTITY(1,1) NOT NULL` |
| **Same issue** | PurchaseInvoiceItem (line 1740), PurchaseReturnItem (line 1851), SalesReturnItem (line 1882), StockTransferItem (line 1910) |

---

## GAP-067 🔵 LOW — `OpeningBalance`/`CurrentBalance` on Suppliers/Customers use `decimal(18,2)` — inconsistent precision

| Field | Detail |
|-------|--------|
| **Location** | Lines 1178-1179, 1195-1196 |
| **Current Text** | Both use `decimal(18,2)` |
| **Status** | This is actually CORRECT per RULE-211. No fix needed. ✅ |

---

## GAP-068 🔵 LOW — Missing `InvoiceTypePrint` enum definition in Enums section

| Field | Detail |
|-------|--------|
| **Location** | Lines 2584-2605 |
| **Current Text** | Only shows CostingMethod and CashTransactionType enums |
| **Problem** | AGENTS.md §3 defines `InvoiceTypePrint` (Sales=1, Purchase=2, SalesReturn=3, PurchaseReturn=4, Test=5) but it's not in the PRD enums section |
| **Fix** | Add InvoiceTypePrint enum to the enums section (around line 2605) |

---

## GAP-069 🔵 LOW — `DeleteBehavior.Restrict` not explicitly documented

| Field | Detail |
|-------|--------|
| **Location** | Nowhere in PRD |
| **Problem** | AGENTS.md RULE-214 says ALL FKs must use `DeleteBehavior.Restrict`. PRD database rules don't mention this. |
| **Fix** | Add to Important Design Rules (lines 1051-1067) |

---

## GAP-070 🔵 LOW — CHECK constraint `PaidAmount <= TotalAmount` not in SQL schema

| Field | Detail |
|-------|--------|
| **Location** | Lines 1714-1771 (SQL) |
| **Current Text** | No CHECK constraint for PaidAmount <= TotalAmount on invoices |
| **Fix** | Add `CONSTRAINT CK_SalesInvoices_PaidAmount CHECK (PaidAmount <= TotalAmount)` and equivalent for PurchaseInvoices |

---

## GAP-071 🔵 LOW — Product.ReorderLevel uses `decimal(18,3)` in baseline but wrong in some places

| Field | Detail |
|-------|--------|
| **Location** | Lines 1129, 1651, 2283 |
| **Current Text** | Most places correctly use `decimal(18,3)`. Some SQL sections might show wrong precision. |
| **Fix** | Verify all ReorderLevel fields use `decimal(18,3)` everywhere |

---

## GAP-072 🔵 LOW — `WarehouseStock` missing `ReorderLevel` field

| Field | Detail |
|-------|--------|
| **Location** | Lines 1153-1168 (schema), 1662-1677 (SQL) |
| **Current Text** | WarehouseStock has: WarehouseId, ProductId, Quantity — NO `ReorderLevel` |
| **Problem** | Line 368 says ReorderLevel is in WarehouseStocks for low-stock alerts but the actual schema puts it on Product |
| **Status** | This is DESIGN-INTENTIONAL: ReorderLevel is per-product (not per-warehouse). Line 368 description is misleading. Fix just the description. |
| **Fix** | Update line 368 to: `(includes: Quantity)` and move ReorderLevel note to Products entity |

---

# PRIORITY ORDER — Recommended Fix Sequence

## 🔴 CRITICAL — Fix IMMEDIATELY (7 items)

| Priority | Gap | Summary |
|----------|-----|---------|
| **P1** | GAP-005 | Add `int invoiceNo` to `SalesInvoice.Create()` |
| **P2** | GAP-007 | Add `InvoiceNo int NOT NULL` column to both invoice SQL tables |
| **P3** | GAP-003 | Replace PUR/INV prefix format with int format |
| **P4** | GAP-020 | Add missing Phases 14-20 to Section 7 |
| **P5** | GAP-021 | Fix Phase numbering discontinuity |
| **P6** | GAP-002 | Remove "+ CQRS" from architecture diagram |
| **P7** | GAP-004 | Update Out of Scope to remove accounting |

## 🟠 HIGH — Fix before next build (25 items)

| Priority | Gap | Summary |
|----------|-----|---------|
| **P8** | GAP-008 | Fix CostingMethod enum values (0→1, 1→2, 2→3) |
| **P9** | GAP-009 | Fix CashTransactionType enum to 8 correct values |
| **P10** | GAP-013 | Change ReturnNo/TransferNo/PaymentNo from nvarchar to int |
| **P11** | GAP-018 | Add 20+ missing entities to entity summary |
| **P12** | GAP-019 | Add missing entity fields (AccountId, TaxId, TrackExpiry, etc.) |
| **P13** | GAP-010 | Add `InvoiceNo` to SalesInvoiceDto |
| **P14** | GAP-011 | Add SaleMode + ProductUnitId to SalesInvoiceItemDto |
| **P15** | GAP-012 | Add `int? InvoiceNo` to CreateSalesInvoiceRequest |
| **P16** | GAP-014 | Remove DocumentSequences table or document limited scope |
| **P17** | GAP-022 | Replace all decimal(18,4) with decimal(18,2) |
| **P18** | GAP-023 | Add ExpirationDate, ImagePath, TrackExpiry, TrackBatch to Product |
| **P19** | GAP-025 | Add AccountId FK to Customer and Supplier |
| **P20** | GAP-026 | Add AccountId FK to CashBox |
| **P21** | GAP-027 | Add TaxId FK to SalesInvoice and PurchaseInvoice |
| **P22** | GAP-028 | Add CashBoxId FK to invoice entity definitions |
| **P23** | GAP-029 | Add InventoryBatch entity and batch tracking fields |
| **P24** | GAP-033 | Remove MediatR from dependency list |
| **P25** | GAP-034 | Fix "Save buttons disabled" to "Save buttons always enabled" |
| **P26** | GAP-037 | Add AvgCost to Product entity |
| **P27** | GAP-038 | Harmonize ProductUnit field names across schema and C# |
| **P28** | GAP-039 | Expand Settings section to 5 categories |
| **P29** | GAP-040 | Add SignaturePath + Email to StoreSettings |
| **P30** | GAP-046 | Add rate limiting to Security requirements |
| **P31** | GAP-048 | Add connection string security to requirements |

## 🟡 MEDIUM — Fix before release (20 items)

| Priority | Gap | Summary |
|----------|-----|---------|
| **P32** | GAP-024 | Add MustChangePassword to Users |
| **P33** | GAP-030 | Add FiscalYear entity |
| **P34** | GAP-031 | Add Currency entity |
| **P35** | GAP-032 | Add PurchaseInvoice entity with full Create() |
| **P36** | GAP-035 | Add ExecuteAsync() wrapper documentation |
| **P37** | GAP-036 | Remove DocumentSequenceService from diagram |
| **P38** | GAP-041 | Mark deprecated StoreSettings fields |
| **P39** | GAP-042 | Define InvoicePrintDto.InvoiceNumber |
| **P40** | GAP-043 | Add ShutdownMode requirement |
| **P41** | GAP-044 | Add Arabic encoding requirement |
| **P42** | GAP-045 | Add LogSystemError() pattern |
| **P43** | GAP-047 | Add user hard-delete protection |
| **P44** | GAP-049 | Fix duplicate Phase 18 numbering |
| **P45** | GAP-050 | Add Phase 14 Product Lifecycle to Section 14 |
| **P46** | GAP-052 | Add SupplierInvoiceNo to PurchaseInvoice |
| **P47** | GAP-053 | Add SaleMode + ProductUnitId to SalesInvoiceItem |
| **P48** | GAP-055 | Define InvoicePrintDto fully |
| **P49** | GAP-056 | Add missing Purchase/SalesReturn DTOs |
| **P50** | GAP-057 | Add RATE_LIMIT_EXCEEDED to ErrorCodes |
| **P51** | GAP-065 | Add ProductUnitId to PurchaseInvoiceItem |

## 🔵 LOW — Fix when convenient (10 items)

| Priority | Gap | Summary |
|----------|-----|---------|
| **P52** | GAP-001 | Fix version header inconsistency |
| **P53** | GAP-006 | Add PurchaseInvoice C# entity |
| **P54** | GAP-066 | Fix SQL PK naming consistency (SalesInvoiceItemId → Id) |
| **P55** | GAP-068 | Add InvoiceTypePrint enum |
| **P56** | GAP-069 | Add DeleteBehavior.Restrict rule |
| **P57** | GAP-070 | Add CHECK constraints for PaidAmount |
| **P58** | GAP-071 | Verify ReorderLevel precision consistency |
| **P59** | GAP-060 | Add UI compacting rules |
| **P60** | GAP-061 | Add newest-first sorting requirement |
| **P61** | GAP-062 | Add Arabic ToolTips requirement |

---

# SUMMARY STATISTICS

| Severity | Count | Key Pattern |
|----------|-------|-------------|
| 🔴 **CRITICAL** | 7 | Missing `InvoiceNo int`, CQRS reference, phase numbering, Out of Scope mismatch |
| 🟠 **HIGH** | 25 | Wrong enum values, missing entities/fields, architecture inconsistencies |
| 🟡 **MEDIUM** | 20 | Incomplete DTOs, missing requirements documentation |
| 🔵 **LOW** | 10 | Naming inconsistencies, missing minor rules |
| **TOTAL** | **62** | |

---

# PHASE-LEVEL GAP DISTRIBUTION

| Phase | Relevant Gaps |
|-------|---------------|
| Foundation (Phase 1) | GAP-007, GAP-018, GAP-019, GAP-023, GAP-024, GAP-025, GAP-026, GAP-029, GAP-030, GAP-031, GAP-037, GAP-038, GAP-066 |
| Infrastructure (Phase 2) | GAP-014, GAP-017, GAP-022, GAP-039, GAP-040, GAP-041 |
| Application (Phase 3) | GAP-008, GAP-009, GAP-010, GAP-011, GAP-012, GAP-032, GAP-053, GAP-065, GAP-068 |
| API (Phase 4) | GAP-002, GAP-033 |
| Desktop Shell (Phase 5) | GAP-043, GAP-035 |
| Desktop Modules (Phase 6) | GAP-034, GAP-045, GAP-060, GAP-061, GAP-062 |
| Production (Phase 7) | GAP-046, GAP-047 |
| Dynamic UOM & Costing (Phases 8-9) | GAP-008, GAP-009, GAP-022, GAP-038 |
| Print Engine (Phase 10) | GAP-042, GAP-055 |
| Production Hardening (Phase 11) | GAP-046, GAP-048 |
| Accounting Foundation (Phases 14/18) | GAP-004, GAP-018, GAP-025, GAP-026, GAP-030, GAP-031 |
| Settings Module (Phase 19) | GAP-039, GAP-040, GAP-041, GAP-057 |
| Security Hardening (Phase 20) | GAP-046, GAP-047, GAP-048 |

---

*End of Gap Analysis — 62 gaps identified (7 CRITICAL, 25 HIGH, 20 MEDIUM, 10 LOW)*
