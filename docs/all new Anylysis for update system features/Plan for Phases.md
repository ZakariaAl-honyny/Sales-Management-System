# 📋 Plan for Phases — Comprehensive Coverage Matrix

> **Version**: 2.3 — Cross-phase gap analysis COMPLETE — 14 Phase plans (18–31) reviewed, 11 BLOCKING issues fixed, 9 HIGH gaps closed, 22 MEDIUM gaps resolved
> **Total Phase plans**: 14 (Phases 18–31), all reviewed and fixed
> **Date**: June 5, 2026
> **Purpose**: Verify every feature from analysis is covered by at least one Phase plan

---

## 1. Module Implementation Order

| Order | Module | Phase File | Status | Gap Fixes Applied |
|-------|--------|------------|--------|-------------------|
| 0 | المحاسبة الأساسية (Accounting Foundation) | `Phase 18 — Accounting Foundation Implementation Plan.md` | ✅ **REVIEWED & FIXED** | decimal(18,4)→(18,2), Cascade→Restrict+Reversal, Annual Closing added, Trial Balance scoped |
| 1 | الإعدادات (Settings) | `Phase 19 — Settings Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | Seed data added (8 entities: warehouse, cashbox, 7 units, 9 doc types, cash customer/supplier, "عام" category), CostingMethod RadioButton |
| 2 | العملات (Currencies) | `Phase 20 - Currencies Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | FractionName + IsSystem fields added, seed data updated, delete guard |
| 3 | المستخدمون والصلاحيات (Users & Permissions) | `Phase 21 — Users & Permissions Module Implementation Plan.md` | ✅ **REVIEWED & FIXED v2.0** | IsActive→Status, EF filter byte cast fix, 33 permission codes aligned §1.2↔§4.9 |
| 4 | دليل الحسابات (Chart of Accounts) | `Phase 22 — Chart of Accounts Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | Level validation relaxed (strict +1→>), 60 accounts (up from 56), IsSystemAccount scoped L1-L2 |
| 5 | العملاء (Customers) | `Phase 23 — Customers Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | Account auto-creation on Customer.Create (Credit type → sub-account under 1210) |
| 6 | الموردون (Suppliers) | `Phase 24 — Suppliers Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | OpeningBalance→journal entry auto-creation, Account auto-creation |
| 7 | الأصناف (Products) | `Phase 25 — Products Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | Opening Stock section on creation (optional Qty/UnitCost/Expiry) |
| 8 | المخازن (Warehouses) | `Phase 26 — Warehouses Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | Physical Count deferred to V2, AdjustmentType + StockIssueReason enums, Notes field |
| 9 | المشتريات (Purchases) | `Phase 27 — Purchases Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | FIFO batch costing, partial PO→Invoice receive, AdditionalCharge.AccountId FK, standalone return, Arabic guards |
| 10 | المبيعات (Sales) | `Phase 28 — Sales Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | Continuous barcode scan (auto-add), CashTransaction RefundOut on return, credit limit, quotation expiry |
| 11 | المقبوضات والمدفوعات (Receipts & Payments) | `Phase 29 - Receipts & Payments Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | CashBox.AccountId FK, ActualCashCount+Difference, negative balance guard, immutability |
| 12 | القيود اليومية (Journal Entries) | `Phase 30 - Journal Entries Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | SystemAccountMappings injection (13 accounts), FiscalYear entity (proper), Cascade→Restrict alignment |
| 13 | التقارير الأساسية (Reports) | `Phase 31 - Reports Module Implementation Plan.md` | ✅ **REVIEWED & FIXED** | Hierarchical Income Statement + Balance Sheet DTOs with subtotals, AccountBalanceDto drill-down |

> **Note**: Phase 21 (Users & Permissions) updated to **v2.0** — now aligns exactly with Analysis Part 5 (lines 3711-5043): 4 roles (Admin/Accountant/Cashier/Observer), 30 exact permission codes with CRUD+Post+Cancel model, passwordless user creation with `MustChangePassword` flow, `UserStatus` enum replacing `IsActive`, `DefaultCashBoxId`, account lockout (5 failed attempts → Locked), `PasswordChangedAt` tracking. All 13 Phase plans (19–31) cover all 22 features with full analysis alignment.

---

## 2. Comprehensive Feature Coverage Matrix

Maps every feature from the 22-item analysis checklist to the Phase plan that covers it.

| # | Feature | Arabic | Covered In | Coverage Depth | Gaps |
|---|---------|--------|------------|----------------|------|
| 1 | ✅ Settings | الإعدادات | **Phase 19** | Full 5-category catalog (Company, System, Print, Tax, Security), 22 key-value pairs, Tax entity, SignaturePath, IMemoryCache, UI screens | None — fully implemented |
| 2 | ✅ Currencies | العملات | **Phase 20** | Full entity (Id, Name, Code, Symbol, ExchangeRate, IsBaseCurrency), ExchangeRateHistory, FK on 8 entities, 15 implementation tasks | None — comprehensive plan |
| 3 | ✅ Chart of Accounts | دليل الحسابات | **Phase 22** | 4-level hierarchy, 56 accounts (vs 18 existing), self-referencing FK, color coding, tree UI, 12 tasks | None — comprehensive plan |
| 4 | ✅ Users & Permissions | المستخدمون والصلاحيات | **Phase 21 v2.0** | Full 3 sub-modules: User Enhancement (passwordless creation, UserStatus, MustChangePassword, DefaultCashBoxId, account lockout), Permissions (30 exact dot-notation codes with 4-role model: Admin/Accountant/Cashier/Observer), Audit Log (2,333 lines, 15 tasks) — ALL analysis gaps filled | None — comprehensive plan v2.0 covers all analysis requirements from Part 5 lines 3711-5043 |
| 5 | ✅ Customers | العملاء | **Phase 23** | AccountId FK, CustomerType, CustomerGroup, CreditLimit validation, 12 tasks | None — comprehensive plan |
| 6 | ✅ Suppliers | الموردون | **Phase 24** | AccountId FK, SupplierType, CreditLimit, 15 tasks, ExecuteAsync() fix | None — comprehensive plan |
| 7 | ✅ CashBoxes | الصناديق | **Phase 29** (Receipts & Payments) | CashBox enhancement, CurrencyId FK, AccountId FK, Daily Closure, Cheque management | Partial — could use stronger visibility as sub-module |
| 8 | ✅ Products | الأصناف | **Phase 25** | 7 sub-modules, FIFO batches, multi-currency pricing, BOM, images, 18 tasks (~40h) | None — comprehensive plan |
| 9 | ✅ Units | الوحدات | **Phase 25** (sub-module) | ProductUnit, ConversionFactor, IsBaseUnit, 7 default units (حبة, كرتون, علبة, كيلو, جرام, لتر, متر) | None |
| 10 | ✅ Barcode | الباركود | **Phase 25** (sub-module) | UnitBarcode entity, multiple barcodes per unit, barcode scanning in Phase 28 | None |
| 11 | ✅ Prices | الأسعار | **Phase 25** (sub-module) | PriceLevel enum (Retail/Wholesale/VIP/Distributor), pricing per (unit + currency), price history | None |
| 12 | ✅ Warehouses | المخازن | **Phase 26** | Enhanced entity (Type, Manager, AccountId), 5 inventory operations, Physical Count, 13 tasks | None — comprehensive plan |
| 13 | ✅ Payments | الدفعات | **Phase 29** (Receipts & Payments) | Multi-invoice distribution, Cheque lifecycle, auto-payments on cash invoices | None |
| 14 | ✅ FIFO / FEFO | FIFO / FEFO | **Phase 25** (sub-module) | PurchaseLot entity, FIFO allocation service, FEFO expiry tracking, batch return | Decision pending (Option A vs B) |
| 15 | ✅ Purchases | المشتريات | **Phase 27** | Multi-currency, additional fees, Purchase Order (NEW), attachments, auto-journal | None — comprehensive plan |
| 16 | ✅ Purchase Returns | مرتجع المشتريات | **Phase 27** (sub-module) | FIFO batch return, supplier discount, currency support | None |
| 17 | ✅ Sales | المبيعات | **Phase 28** | Multi-currency, price override, profit display, Sales Quotation (NEW), barcode POS | None — comprehensive plan |
| 18 | ✅ Sales Returns | مرتجع المبيعات | **Phase 28** (sub-module) | FIFO batch return, discount handling, auto refund, enhanced journal entries | None |
| 19 | ✅ Inventory Operations | العمليات المخزنية | **Phase 26** (sub-module) | Stock Issue/Receipt/Transfer/Adjustment, Physical Count, dedicated entity | None |
| 20 | ✅ Journal Entries | القيود اليومية | **Phase 30** | 3-state lifecycle, multi-currency, attachments, Opening Entry, Annual Closing, auto-entry providers | None — comprehensive plan |
| 21 | ✅ Receipt & Payment Vouchers | سندات القبض والصرف | **Phase 29** (Receipts & Payments) | Cheque management, voucher printing, multiple payment methods | None |
| 22 | ✅ Reports | التقارير | **Phase 31** | 35+ report DTOs across 7 categories, Excel/PDF export, column customization, aging reports | None — comprehensive plan |

---

## 3. Gap Analysis — What's MISSING

### ✅ GAP CLOSED: Phase 21 — Users & Permissions Module

**Status**: ✅ **CREATED** — 1,922 lines, 15 tasks, ~30 hours estimate

**What Phase 21 covers** (from Global Analysis lines 136-145):
- Extends User entity (Phone, Email, AvatarPath, LastLoginAt, LoginAttempts, IsLocked)
- Permission entity + RolePermission join table (22 granular permissions from matrix)
- AuditLog entity with filterable browser UI
- Password change screen (current + new + confirm)
- Account lockout (5 failed attempts)
- Session tracking (UserSession entity)
- "Current user" display in MainWindow status bar
- Permission management admin UI (grid with checkboxes)
- 10 new API endpoints across 3 controllers
- 55+ AGENTS.md rules compliance

### 🟡 MINOR GAP: CashBox enhancement split across phases

**Symptom**: الصناديق (CashBoxes) is partially in Phase 19 (seed data: default cashbox "الصندوق الرئيسي") and partially in Phase 29 (CashBox enhancement with CurrencyId, AccountId, Daily Closure).

**Mitigation**: Phase 29 already covers CashBox comprehensively. Ensure cross-reference in the plan.

### 🟡 MINOR GAP: Multi-language support documented but no enhancement plan

**Symptom**: Global Analysis lines 108-112 confirm multi-language (4 languages) is already implemented. No enhancement needed in V1.

**Mitigation**: Document as existing feature, no Phase needed.

---

## 4. Proposed New Phases

### Phase 18 — Accounting Foundation ✅ CREATED & FIXED

**Order**: Before Settings (Phase 19), after existing v4.x accounting refactoring
**File**: `Phase 18 — Accounting Foundation Implementation Plan.md` (1,519 lines, 13 tasks)
**Status**: ✅ **REVIEWED & FIXED** — decimal(18,4)→(18,2), Cascade→Restrict+Reversal, Annual Closing, Trial Balance scoped

**Key entities**: JournalEntry, JournalEntryLine, Account, FiscalYear (NEW)

### Phase 21 — Users & Permissions Module ✅ CREATED & FIXED

**Order**: After Currencies (Phase 20), before Chart of Accounts (Phase 22)
**File**: `Phase 21 — Users & Permissions Module Implementation Plan.md` (2,333 lines, 15 tasks, ~30 hours)
**Status**: ✅ **REVIEWED & FIXED v2.0**

**Key entities**: User (enhanced), Permission (NEW), RolePermission (NEW), AuditLog (NEW), UserSession (NEW)

### Phase 23 — Customers Module ✅ FIXED

**Status**: ✅ **REVIEWED & FIXED** — Account sub-account auto-creation added

### Phase 24 — Suppliers Module ✅ FIXED

**Status**: ✅ **REVIEWED & FIXED** — OpeningBalance journal entry + Account auto-creation added

### Phase 25 — Products Module ✅ FIXED

**Status**: ✅ **REVIEWED & FIXED** — Opening Stock section, Units seed data expanded

### Phase 26 — Warehouses Module ✅ FIXED

**Status**: ✅ **REVIEWED & FIXED** — Physical Count deferred to V2, AdjustmentType/StockIssueReason enums

### Phase 27 — Purchases Module ✅ FIXED

**Status**: ✅ **REVIEWED & FIXED** — FIFO batches, partial PO receive, AdditionalCharge.AccountId, standalone return

### Phase 28 — Sales Module ✅ FIXED

**Status**: ✅ **REVIEWED & FIXED** — Continuous barcode scan, CashTransaction refund, credit limit, quotation expiry

### Phase 29 — Receipts & Payments Module ✅ FIXED

**Status**: ✅ **REVIEWED & FIXED** — CashBox.AccountId FK, ActualCashCount, balance guard, immutability

### Phase 30 — Journal Entries Module ✅ FIXED

**Status**: ✅ **REVIEWED & FIXED** — SystemAccountMappings integration, FiscalYear entity, Cascade→Restrict alignment

### Phase 31 — Reports Module ✅ FIXED

**Status**: ✅ **REVIEWED & FIXED** — Hierarchical Income Statement + Balance Sheet DTOs with subtotals

---

## 5. Updated Implementation Sequence

```text
Phase 18 — Accounting Foundation              (✅ REVIEWED & FIXED — 1,519 lines, 13 tasks)
     ↓
Phase 19 — Settings Module                    (✅ REVIEWED & FIXED — seed data + CostingMethod)
     ↓
Phase 20 — Currencies Module                  (✅ REVIEWED & FIXED — FractionName + IsSystem)
     ↓
Phase 21 — Users & Permissions Module         (✅ REVIEWED & FIXED v2.0 — 4 roles, 33 permissions)
     ↓
Phase 22 — Chart of Accounts Module           (✅ REVIEWED & FIXED — 60 accounts, relaxed Level)
     ↓
Phase 23 — Customers Module                   (✅ REVIEWED & FIXED — Account auto-creation)
     ↓
Phase 24 — Suppliers Module                   (✅ REVIEWED & FIXED — OpeningBalance→journal)
     ↓
Phase 25 — Products Module                    (✅ REVIEWED & FIXED — Opening Stock section)
     ↓
Phase 26 — Warehouses Module                  (✅ REVIEWED & FIXED — Physical Count deferred to V2)
     ↓
Phase 27 — Purchases Module                   (✅ REVIEWED & FIXED — FIFO, partial PO, Arabic guards)
     ↓
Phase 28 — Sales Module                       (✅ REVIEWED & FIXED — barcode auto-add, refund, credit)
     ↓
Phase 29 — Receipts & Payments Module         (✅ REVIEWED & FIXED — AccountId, balance guard)
     ↓
Phase 30 — Journal Entries Module             (✅ REVIEWED & FIXED — SystemAccountMappings + FiscalYear)
     ↓
Phase 31 — Reports Module                     (✅ REVIEWED & FIXED — hierarchical DTOs)
```

---

## 6. Coverage Summary

| Category | Count | Status |
|----------|-------|--------|
| Features fully covered by Phase plans | 22 of 22 | ✅ |
| Features with dedicated comprehensive Phase | 11 of 22 | ✅ |
| Features needing NEW Phase | **0** — all covered | ✅ |
| Total Phase plans | 14 (Phases 18-31) | 14 created ✅ |
| Phases with cross-phase gap fixes applied | 14 of 14 | ✅ |
| BLOCKING issues found & fixed | 11 | ✅ |
| HIGH gaps found & closed | 9 | ✅ |
| MEDIUM gaps found & resolved | 22 | ✅ |
| Estimated fix effort | ~26 hours | ✅ All applied |

---

## 7. Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Users & Permissions not planned | ~~HIGH~~ ✅ **RESOLVED** | Phase 21 created with 15 tasks |
| CashBox split across Phases 19 + 29 | Low — both plans already reference each other | Cross-reference added in Phase 29 |
| Multi-language UI not in any Phase | Low — already implemented per Global Analysis v1.5 | Documented as existing feature |
| Phase 21 numbering gap | ~~Medium~~ ✅ **RESOLVED** | Phase 21 fills the gap |
