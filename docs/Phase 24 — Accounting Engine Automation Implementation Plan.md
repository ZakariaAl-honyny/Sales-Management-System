# Phase 24 — Accounting Engine Automation

## Design Overview

Currently, NO business operation creates journal entries automatically. This phase integrates double-entry accounting into all financial operations by introducing a dedicated `AccountingIntegrationService` that is called from within existing service transactions.

## 1. Schema Additions

### 1.1 Opening Balance Equity Account (Already Seeded)

The **Opening Balance Equity** account already exists in the seeded Chart of Accounts (Phase 18):

| Field | Value |
|-------|-------|
| `AccountCode` | `32020001` |
| `NameAr` | `رصيد افتتاحي` |
| `NameEn` | `Opening Balance Equity Detail` |
| `AccountType` | `Equity (3)` |
| `Level` | `4` |
| `ParentAccountId` | Under `3202` (الأرصدة الافتتاحية / Opening Balance Equity — a Level 3 account under 32 — الأرباح والخسائر) |
| `AllowTransactions` | `true` (Level 4 leaf) |
| `IsSystemAccount` | `true` (protected) |
| `Description` | `حساب مؤقت يستخدم لترحيل الأرصدة الافتتاحية عند بدء استخدام النظام المحاسبي.` |
| `ColorCode` | `#4CAF50` (Equity green) |

The corresponding `SystemAccountKey.OpeningBalanceEquity` mapping is already seeded in the `SystemAccountMapping` table, pointing to account `32020001`.

### 1.2 SystemAccountMapping (Key-Value Store — Already Implemented)

The `SystemAccountMapping` entity (singular) uses a key-value pattern with `MappingKey` (string, from `SystemAccountKey` enum) and `AccountId` (FK to `Account`). This replaces the old fixed-column `SystemAccountMappings` design. All 21 mappings are seeded in `AccountingSeeder.cs`.

> See `docs/AGENTS.md` for domain entity patterns (private set, Guard Clauses, domain methods) and `docs/AGENTS.md` §2.16 for EF Core Fluent API conventions.

### 1.3 JournalEntryType Enum — No New Values Needed

The `JournalEntryType` enum in `Domain/Accounting/Enums/` already has values sufficient for all Phase 24 operations. No new enum values are needed beyond what Phase 18 seeded.

### 1.4 Summary of Schema Changes

| Change | File | Type |
|--------|------|------|
| Account `32020001` (رصيد افتتاحي) | `AccountingSeeder.cs` | Already seeded |
| `SystemAccountMapping` table with 21 keys | `SystemAccountMapping.cs` + seeder | Already seeded |
| `SystemAccountKey.OpeningBalanceEquity` mapping | `SystemAccountMapping.cs` | Already seeded |
| `IAccountingIntegrationService` interface | `Interfaces/Services/IAccountingIntegrationService.cs` | NEW |
| `AccountingIntegrationService` implementation | `Application/Accounting/Services/` | NEW |

---

## 2. Service Layer: `AccountingIntegrationService`

All accounting entry creation is centralized into a single **dedicated service** to avoid duplicating account-resolution logic across business services.

> See `docs/CONSTITUTION.md` for the Result<T> pattern and `docs/AGENTS.md` for service layer patterns.

The `AccountingIntegrationService` uses `ISystemAccountService.GetMappingAsync(SystemAccountKey key, ...)` to resolve account IDs dynamically at runtime via the `SystemAccountMapping` key-value table. It does NOT use hardcoded properties like `Mappings.DefaultCashAccountId`.

**Key lookup pattern:**
```csharp
// Fetch required accounts in parallel
var dictResult = await GetAccountIdDictionaryAsync(null, requiredKeys, ct);
var m = dictResult.Value!;
// Use: m[SystemAccountKey.DefaultCash], m[SystemAccountKey.SalesRevenue], etc.
```

**Per-entity account routing:** For customer and supplier journal entries, the service uses `Customer.AccountId` / `Supplier.AccountId` (the linked Chart of Accounts account) with fallback to `SystemAccountKey.AccountsReceivable` / `SystemAccountKey.AccountsPayable` system mappings.

**Fiscal year guard requirement (RULE-281):** Before creating ANY journal entry, the service MUST check `_uow.FiscalYearClosures.AnyAsync(fyc => fyc.FiscalYear == transactionDate.Year)`. Return `Result<int>.Failure("السنة المالية {year} مغلقة — لا يمكن إضافة قيود محاسبية")` if closed.

---

## 3. Exact Journal Entry Specifications

For each operation below, the entry is created **inside the same transaction** as the business operation.

### 3.0 Account Resolution — Cash/AR/AP by PaymentType

All invoice and payment entries resolve debit/credit accounts using this logic:

**For Sales (Revenue side):**
| PaymentType | Debit Account | Credit Side |
|------------|---------------|-------------|
| `Cash (1)` | `SystemAccountKey.DefaultCash` (11010001 — الصندوق) | Full amount |
| `Credit (2)` | `Customer.AccountId` (per-entity, fallback `SystemAccountKey.AccountsReceivable` = 11030001) | Full amount |
| `Mixed (3)` | Split: `DefaultCash` for `PaidAmount` + Customer account for `RemainingAmount` | Partial |

**For Purchases (Cost side):**
| PaymentType | Credit Account | Debit Side |
|------------|----------------|-------------|
| `Cash (1)` | `SystemAccountKey.DefaultCash` (11010001 — الصندوق) | Full amount |
| `Credit (2)` | `Supplier.AccountId` (per-entity, fallback `SystemAccountKey.AccountsPayable` = 21010001) | Full amount |
| `Mixed (3)` | Split: `DefaultCash` for `PaidAmount` + Supplier account for `RemainingAmount` | Partial |

### 3.1 Customer OpeningBalance

**Trigger:** Inside `CustomerService.CreateAsync()` when `openingBalance > 0`  
**Timing:** AFTER customer is saved to DB (has an ID), inside `ExecuteTransactionAsync`  
**Condition:** Only if `OpeningBalance > 0` — otherwise no entry  
**Reference:** `ReferenceType = "Customer"`, `ReferenceId = customer.Id`, `ReferenceNumber = customerId.ToString()`

#### Journal Entry

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1 | Customer's `AccountId` | 11030001 | العميل النقدي | `OpeningBalance` | — | `رصيد افتتاحي للعميل: {customerName}` |
| 2 | `SystemAccountKey.OpeningBalanceEquity` | 32020001 | رصيد افتتاحي | — | `OpeningBalance` | `رصيد افتتاحي للعميل: {customerName}` |

- **EntryType:** `JournalEntryType.OpeningBalance (9)`
- **Balance check:** Dr = Cr = OpeningBalance ✓
- **Edge case:** If `openingBalance == 0`, skip entry entirely

#### Order inside CustomerService.CreateAsync()

```
1. Create Customer entity (in memory)
2. SaveChangesAsync → customer gets ID
3. IF openingBalance > 0:
     a. Call AccountingIntegrationService.CreateCustomerOpeningEntryAsync()
        - Resolves OpeningBalanceEquity mapping (32020001)
        - Uses Customer.AccountId (per-entity account routing)
        - Creates and posts journal entry
4. Return success
```

**Transaction change needed:** `CustomerService.CreateAsync()` currently uses bare `SaveChangesAsync()`. When `openingBalance > 0`, the save must be wrapped in `ExecuteTransactionAsync` to atomically save BOTH the customer and the journal entry.

---

### 3.2 Supplier OpeningBalance

**Trigger:** Inside `SupplierService.CreateAsync()` when `openingBalance > 0`  
**Timing:** AFTER supplier is saved to DB (has an ID), in same `ExecuteTransactionAsync`  
**Condition:** Only if `OpeningBalance > 0`  
**Reference:** `ReferenceType = "Supplier"`, `ReferenceId = supplier.Id`, `ReferenceNumber = supplier.Id.ToString()`

#### Journal Entry

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1 | `SystemAccountKey.OpeningBalanceEquity` | 32020001 | رصيد افتتاحي | `OpeningBalance` | — | `رصيد افتتاحي للمورد: {supplierName}` |
| 2 | Supplier's `AccountId` | 21010001 | المورد النقدي | — | `OpeningBalance` | `رصيد افتتاحي للمورد: {supplierName}` |

- **EntryType:** `JournalEntryType.OpeningBalance (9)`

---

### 3.3 Sales Invoice Post

**Trigger:** Inside `SalesService.PostAsync()` AFTER stock is deducted and customer balance updated  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "SalesInvoice"`, `ReferenceId = invoice.Id`, `ReferenceNumber = invoice.InvoiceNo.ToString()`

#### 3.3.1 Revenue & COGS Values

| Value | Source | Formula |
|-------|--------|---------|
| `NetRevenue` | `invoice.SubTotal - invoice.DiscountAmount` | Already computed on invoice (MUST validate: `DiscountAmount <= SubTotal`) |
| `TaxAmount` | `invoice.TaxAmount` | Already computed |
| `TotalAmount` | `invoice.NetTotal` | `NetRevenue + TaxAmount + OtherCharges` |
| `PaidAmount` | `invoice.PaidAmount` | Cash portion |
| `RemainingAmount` | `invoice.RemainingAmount` | Credit portion |
| `OtherCharges` | `invoice.OtherCharges` | Delivery / service fees |
| `COGS` | Passed as `totalCost` parameter | Computed by caller from line items |

**NetRevenue MUST NOT be clamped to zero:** If `DiscountAmount > SubTotal`, return `Result.Failure("لا يمكن أن يكون الخصم أكبر من إجمالي الفاتورة")` — never clamp to zero (RULE-379).

**DeliveryChargesRevenue (41020003)** is a SEPARATE revenue account from SalesRevenue (41010001). OtherCharges are credited to `DeliveryChargesRevenue`, NOT to SalesRevenue (RULE-493).

#### 3.3.2 Account Resolution for Cash/AR (Revenue Side)

Use `GetCustomerAccountId(invoice, dict)` helper which uses `invoice.Customer?.AccountId` with fallback to `SystemAccountKey.AccountsReceivable`.

#### 3.3.3 Journal Entry — Compound Entry

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1a | `DefaultCash` (if `PaidAmount > 0`) | 11010001 | الصندوق | `PaidAmount` | — | `الجزء النقدي من فاتورة البيع` |
| 1b | Customer's `AccountId` (if `RemainingAmount > 0`) | 11030001 | العميل النقدي | `RemainingAmount` | — | `الجزء الآجل من فاتورة البيع` |
| 2 | `SalesRevenue` | 41010001 | إيرادات المبيعات | — | `NetRevenue` | `إيراد المبيعات (صافي بعد الخصم)` |
| 3 | `DeliveryChargesRevenue` (if `OtherCharges > 0`) | 41020003 | إيرادات التوصيل | — | `OtherCharges` | `إيرادات التوصيل ورسوم الخدمة` |
| 4 | `VatOutput` (if `TaxAmount > 0`) | 21020001 | ضريبة المبيعات (خرج) | — | `TaxAmount` | `ضريبة المخرجات` |
| 5 | `CostOfGoodsSold` (if `totalCost > 0`) | 51010001 | تكلفة البضاعة المباعة | `totalCost` | — | `تكلفة البضاعة المباعة` |
| 6 | `Inventory` (if `totalCost > 0`) | 11040001 | بضاعة أول المدة | — | `totalCost` | `تخفيض المخزون` |

- **Sales Revenue is a SINGLE account** (41010001 — إيرادات المبيعات), NOT split into Cash Sales and Credit Sales. The PaymentType distinction is captured on the debit side (Cash Account vs Customer Account).
- **DeliveryChargesRevenue** is a SEPARATE account (41020003 — إيرادات التوصيل) — credited independently from SalesRevenue.
- **EntryType:** `JournalEntryType.Sales (1)`
- **Balance check:**
  - Debits: `PaidAmount + RemainingAmount + totalCost` = `TotalAmount + totalCost`
  - Credits: `NetRevenue + OtherCharges + TaxAmount + totalCost` = `(SubTotal - Discount) + OtherCharges + TaxAmount + totalCost` = `TotalAmount + totalCost` ✓
- **Missing lines:** Lines 1a/1b are exclusive (cash OR credit OR both for mixed). Lines 3/4 skipped if zero. Lines 5/6 skipped if `totalCost` is 0.

#### 3.3.4 Order inside SalesService.PostAsync()

```
Existing operations (in order):
1. invoice.Post()
2. SaveChangesAsync
3. Deduct Stock (foreach item)
4. Update Customer Balance (if RemainingAmount > 0)
5. Record Cash Transaction (if CashBoxId.HasValue)
--- NEW: Accounting Integration ---
6. Calculate COGS = Σ(item retail quantity × unit cost)
7. Call AccountingIntegrationService.CreateSalesPostEntryAsync(invoice, userId, totalCost)
   - Resolves all system account mappings via GetAccountIdDictionaryAsync
   - Uses per-entity Customer.AccountId for AR side
   - Validates discount ≤ subtotal
   - Creates + posts compound journal entry
   - Fails → rollback entire transaction
--- End New ---
8. SaveChangesAsync
9. CommitAsync
```

---

### 3.4 Sales Invoice Cancel (Full Reversal)

**Trigger:** Inside `SalesService.CancelAsync()` AFTER stock and balance have been reversed  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "SalesInvoice"`, `ReferenceId = invoice.Id`, `ReferenceNumber = $"{invoice.InvoiceNo}-REV"`

**Condition:** Only if invoice was `Status == Posted` before this cancel call. If the invoice was still `Draft`, no journal entry was created, so no reversal is needed.

#### 3.4.1 Journal Entry — Full Reversal

Reverses ALL lines from the Post entry (debit ↔ credit swap):

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1 | `SalesRevenue` | 41010001 | إيرادات المبيعات | `NetRevenue` | — | `عكس إيراد المبيعات` |
| 2 | `DeliveryChargesRevenue` (if original `OtherCharges > 0`) | 41020003 | إيرادات التوصيل | `OtherCharges` | — | `عكس إيرادات التوصيل ورسوم الخدمة` |
| 3 | `VatOutput` (if original `TaxAmount > 0`) | 21020001 | ضريبة المبيعات (خرج) | `TaxAmount` | — | `عكس ضريبة المخرجات` |
| 4a | `DefaultCash` (if original cash) | 11010001 | الصندوق | — | `PaidAmount` | `عكس الجزء النقدي من فاتورة البيع` |
| 4b | Customer's `AccountId` (if original credit) | 11030001 | العميل النقدي | — | `RemainingAmount` | `عكس الجزء الآجل من فاتورة البيع` |
| 5 | `Inventory` (COGS reversal — queries original entry) | 11040001 | بضاعة أول المدة | `cogsAmount` | — | `عكس تكلفة البضاعة المباعة — إعادة المخزون` |
| 6 | `CostOfGoodsSold` (COGS reversal) | 51010001 | تكلفة البضاعة المباعة | — | `cogsAmount` | `عكس تكلفة البضاعة المباعة` |

- **COGS reversal queries the original journal entry** to find the exact COGS amount — the `ReverseSalesPostEntryAsync()` method looks up the original entry by `ReferenceType="SalesInvoice"` + `ReferenceId=invoice.Id` + `EntryType=Sales`, then queries `JournalEntryLines` for lines with `AccountId == COGS account && Debit > 0`.
- **Fallback:** If original entry not found, COGS reversal is skipped with a warning log. Revenue-side reversal still applies.
- **EntryType:** `JournalEntryType.SalesReturn (2)`

#### 3.4.2 Order inside SalesService.CancelAsync()

```
Existing operations (in order):
1. IF Status == Posted:
   a. Reverse Stock (foreach item)
   b. Reverse Customer Balance (if RemainingAmount > 0)
   c. Create offsetting cash transaction (if CashBoxId)
--- NEW: Accounting Integration ---
2. IF Status == Posted:
   a. Call AccountingIntegrationService.ReverseSalesPostEntryAsync(invoice, userId)
   b. Queries original entry for COGS lines
   c. Creates reversal entry with Dr↔Cr swapped
   d. Fails → rollback entire transaction
--- End New ---
3. invoice.SetPaidAmount(0)
4. invoice.Cancel()
5. SaveChangesAsync
6. CommitAsync

IF Status == Draft (no journal entry to reverse):
→ Skip step 2 entirely
```

---

### 3.5 Purchase Invoice Post

**Trigger:** Inside `PurchaseService.PostAsync()` AFTER stock increased and supplier balance updated  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "PurchaseInvoice"`, `ReferenceId = invoice.Id`, `ReferenceNumber = invoice.InvoiceNo.ToString()`

#### 3.5.1 Values

| Value | Source | Formula |
|-------|--------|---------|
| `NetInventoryCost` | `invoice.SubTotal - invoice.DiscountAmount + invoice.OtherCharges` | Landed cost (includes OtherCharges) |
| `TaxAmount` | `invoice.TaxAmount` | VAT input |
| `TotalAmount` | `invoice.NetTotal` | `NetInventoryCost + TaxAmount` |
| `PaidAmount` | `invoice.PaidAmount` | Cash paid |
| `RemainingAmount` | `invoice.RemainingAmount` | Amount owed to supplier |

**Note:** `NetInventoryCost` includes `OtherCharges` (landed cost distribution) — this matches the `AllocateAdditionalCharges()` behavior.

#### 3.5.2 Account Resolution

Use `GetSupplierAccountId(invoice, dict)` helper which uses `invoice.Supplier?.AccountId` with fallback to `SystemAccountKey.AccountsPayable`.

#### 3.5.3 Journal Entry

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1 | `Inventory` | 11040001 | بضاعة أول المدة | `NetInventoryCost` | — | `تكلفة المشتريات (صافي بعد الخصم)` |
| 2 | `VatInput` (if `TaxAmount > 0`) | 21020002 | ضريبة المشتريات (دخل) | `TaxAmount` | — | `ضريبة المدخلات` |
| 3a | `DefaultCash` (if `PaidAmount > 0`) | 11010001 | الصندوق | — | `PaidAmount` | `الجزء النقدي من فاتورة الشراء` |
| 3b | Supplier's `AccountId` (if `RemainingAmount > 0`) | 21010001 | المورد النقدي | — | `RemainingAmount` | `الجزء الآجل من فاتورة الشراء` |

- **EntryType:** `JournalEntryType.Purchase (3)`
- **Balance check:**
  - Debits: `NetInventoryCost + TaxAmount` = `TotalAmount`
  - Credits: `PaidAmount + RemainingAmount` = `TotalAmount` ✓

#### 3.5.4 Order inside PurchaseService.PostAsync()

```
Existing operations (in order):
1. invoice.Post()
2. SaveChangesAsync
3. AutoUpdatePrices (if enabled)
4. Increase Stock (foreach item)
5. Update Pricing/Costing (foreach item)
6. Update Supplier Balance (if RemainingAmount > 0)
7. Record Cash Transaction (if CashBoxId.HasValue)
--- NEW: Accounting Integration ---
8. Call AccountingIntegrationService.CreatePurchasePostEntryAsync(invoice, userId)
   - Resolves all system account mappings
   - Uses per-entity Supplier.AccountId for AP side
   - Validates discount ≤ subtotal
   - Includes OtherCharges in NetInventoryCost (landed cost)
   - Creates + posts journal entry
   - Fails → rollback entire transaction
--- End New ---
9. SaveChangesAsync
10. CommitAsync
```

---

### 3.6 Purchase Invoice Cancel

**Trigger:** Inside `PurchaseService.CancelAsync()` AFTER stock and balance have been reversed  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "PurchaseInvoice"`, `ReferenceId = invoice.Id`, `ReferenceNumber = $"{invoice.InvoiceNo}-REV"`

**Condition:** Only if invoice was `Status == Posted`.

#### 3.6.1 Journal Entry — Full Reversal

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1 | `PurchaseReturns` | 51020002 | مردودات مشتريات | — | `NetInventoryCost` | `عكس تكلفة المشتريات - مردودات مشتريات` |
| 2 | `VatInput` (if original `TaxAmount > 0`) | 21020002 | ضريبة المشتريات (دخل) | — | `TaxAmount` | `عكس ضريبة المدخلات` |
| 3a | `DefaultCash` (if original cash) | 11010001 | الصندوق | `PaidAmount` | — | `عكس الجزء النقدي من فاتورة الشراء` |
| 3b | Supplier's `AccountId` (if original credit) | 21010001 | المورد النقدي | `RemainingAmount` | — | `عكس الجزء الآجل من فاتورة الشراء` |

- **Note:** Purchase return reversal credits `PurchaseReturns` account (51020002 — مردودات مشتريات), NOT `Inventory` account directly. The `PurchaseReturnAccountId` from system mappings is used (RULE-458).
- **EntryType:** `JournalEntryType.PurchaseReturn (4)`
- **Balance check:**
  - Debits: `PaidAmount + RemainingAmount` = `TotalAmount`
  - Credits: `NetInventoryCost + TaxAmount` = `TotalAmount` ✓

#### 3.6.2 Order inside PurchaseService.CancelAsync()

```
Existing operations (in order):
1. IF Status == Posted:
   a. Reverse Stock (foreach item)
   b. Reverse Supplier Balance (if RemainingAmount > 0)
   c. Reverse Cash Transaction (if CashBoxId)
--- NEW: Accounting Integration ---
2. IF Status == Posted:
   a. Call AccountingIntegrationService.ReversePurchasePostEntryAsync(invoice, userId)
   b. Creates reversal entry crediting PurchaseReturns (not Inventory directly)
   c. Fails → rollback entire transaction
--- End New ---
3. invoice.SetPaidAmount(0)
4. invoice.Cancel()
5. SaveChangesAsync
6. CommitAsync
```

---

### 3.7 Customer Payment (Receipt)

**Trigger:** Inside `CustomerReceiptService.CreateAsync()` / `PostAsync()` AFTER customer balance is decreased  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "CustomerReceipt"`, `ReferenceId = receipt.Id`, `ReferenceNumber = receipt.Id.ToString()`

#### 3.7.1 Journal Entry

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1 | `DefaultCash` | 11010001 | الصندوق | `receipt.Amount` | — | `سند قبض من العميل: {customerName}` |
| 2 | Customer's `AccountId` (fallback `AccountsReceivable`) | 11030001 | العميل النقدي | — | `receipt.Amount` | `سند قبض من العميل: {customerName}` |

- **EntryType:** `JournalEntryType.CustomerReceipt (10)`
- **Balance check:** Dr = Cr = Amount ✓
- **Per-entity routing:** Uses `receipt.Customer?.AccountId` with fallback to `SystemAccountKey.AccountsReceivable`.

#### 3.7.2 Order inside CustomerReceiptService.CreateAsync()

```
Existing operations (in order):
1. Create CustomerReceipt entity
2. AddAsync(receipt)
3. customer.DecreaseBalance(amount)
--- NEW: Accounting Integration ---
4. Call AccountingIntegrationService.CreateCustomerPaymentEntryAsync(receipt, customerName, userId)
   - Resolves DefaultCash and AccountsReceivable mappings
   - Uses per-entity Customer.AccountId for AR credit
   - Creates + posts journal entry
   - Fails → rollback entire transaction
--- End New ---
5. SaveChangesAsync
6. CommitAsync
```

#### 3.7.3 Reverse (Delete/Cancel)

When a posted customer receipt is deleted or cancelled, call:
```csharp
ReverseCustomerPaymentEntryAsync(receiptId, amount, customerName, customerAccountId, reversedByUserId, ct)
```
Creates reversal: Dr Customer Account / Cr Cash.

---

### 3.8 Supplier Payment

**Trigger:** Inside `SupplierPaymentService.CreateAsync()` / `PostAsync()` AFTER supplier balance is decreased  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "SupplierPayment"`, `ReferenceId = payment.Id`, `ReferenceNumber = payment.Id.ToString()`

#### 3.8.1 Journal Entry

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1 | Supplier's `AccountId` (fallback `AccountsPayable`) | 21010001 | المورد النقدي | `payment.Amount` | — | `سند دفع للمورد: {supplierName}` |
| 2 | `DefaultCash` | 11010001 | الصندوق | — | `payment.Amount` | `سند دفع للمورد: {supplierName}` |

- **EntryType:** `JournalEntryType.SupplierPayment (11)`
- **Balance check:** Dr = Cr = Amount ✓
- **Per-entity routing:** Uses `payment.Supplier?.AccountId` with fallback to `SystemAccountKey.AccountsPayable`.

#### 3.8.2 Order inside SupplierPaymentService.CreateAsync()

```
Existing operations (in order):
1. Create SupplierPayment entity
2. AddAsync(payment)
3. supplier.DecreaseBalance(amount)
--- NEW: Accounting Integration ---
4. Call AccountingIntegrationService.CreateSupplierPaymentEntryAsync(payment, supplierName, userId)
   - Resolves DefaultCash and AccountsPayable mappings
   - Uses per-entity Supplier.AccountId for AP debit
   - Creates + posts journal entry
   - Fails → rollback entire transaction
--- End New ---
5. SaveChangesAsync
6. CommitAsync
```

#### 3.8.3 Reverse (Delete/Cancel)

When a posted supplier payment is deleted or cancelled, call:
```csharp
ReverseSupplierPaymentEntryAsync(paymentId, amount, supplierName, supplierAccountId, reversedByUserId, ct)
```
Creates reversal: Dr Cash / Cr Supplier Account.

---

### 3.9 Sales Return (Standalone — Partial Return)

**Trigger:** Inside `SalesReturnService.PostAsync()`  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "SalesReturn"`, `ReferenceId = salesReturn.Id`, `ReferenceNumber = salesReturn.ReturnNo.ToString()`

#### 3.9.1 Journal Entry

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1 | `SalesReturns` | 51020001 | مردودات مبيعات | `TotalAmount` | — | `مردود مبيعات — إلغاء الإيراد` |
| 2 | Customer's `AccountId` | 11030001 | العميل النقدي | — | `TotalAmount` | `مردود مبيعات — تخفيض ذمّة العميل` |
| 3 | `Inventory` (if `totalCost > 0`) | 11040001 | بضاعة أول المدة | `totalCost` | — | `مردود مبيعات — إعادة المخزون` |
| 4 | `CostOfGoodsSold` (if `totalCost > 0`) | 51010001 | تكلفة البضاعة المباعة | — | `totalCost` | `مردود مبيعات — عكس التكلفة` |

- **EntryType:** `JournalEntryType.SalesReturn (2)`
- **Per-entity routing:** Uses `salesReturn.Customer?.AccountId` with fallback to `SystemAccountKey.AccountsReceivable`.
- **Reverse method:** `ReverseSalesReturnEntryAsync()` swaps Dr↔Cr to restore original state.

---

### 3.10 Purchase Return (Standalone — Partial Return)

**Trigger:** Inside `PurchaseReturnService.PostAsync()`  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "PurchaseReturn"`, `ReferenceId = purchaseReturn.Id`, `ReferenceNumber = purchaseReturn.ReturnNo.ToString()`

#### 3.10.1 Journal Entry

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1 | Supplier's `AccountId` (fallback `AccountsPayable`) | 21010001 | المورد النقدي | `TotalAmount` | — | `مردود مشتريات — تخفيض ذمّة المورد` |
| 2 | `PurchaseReturns` | 51020002 | مردودات مشتريات | — | `TotalAmount` | `مردود مشتريات` |

- **EntryType:** `JournalEntryType.PurchaseReturn (4)`
- **Per-entity routing:** Uses `purchaseReturn.Supplier?.AccountId` with fallback to `SystemAccountKey.AccountsPayable`.
- **Reverse method:** `ReversePurchaseReturnEntryAsync()` swaps Dr↔Cr.

---

### 3.11 Expense Entry

**Trigger:** Inside `ExpenseService.PostAsync()`  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "Expense"`, `ReferenceId = expense.Id`, `ReferenceNumber = expense.ExpenseNo.ToString()`

#### 3.11.1 Journal Entry

| # | Account | Account Code | Name | Debit | Credit | Description |
|---|---------|-------------|------|-------|--------|-------------|
| 1 | `expense.ExpenseAccountId` (per-entity expense account) | 52010001 | مصروفات عمومية | `expense.Amount` | — | `مصروف: {notes}` |
| 2 | `cashBox.AccountId` (from linked CashBox) | 11010001 | الصندوق | — | `expense.Amount` | `خروج نقدي من الصندوق` |

- **EntryType:** `JournalEntryType.Manual (8)` — expenses are treated as manual entries.
- **Note:** The debit account comes from `Expense.ExpenseAccountId` (the specific expense account selected when creating the expense), not from SystemAccountMappings.
- **Credit account** comes from `CashBox.AccountId` (the Account linked to the CashBox used for payment).
- **Reverse method:** `ReverseExpenseEntryAsync()` swaps Dr↔Cr: Dr CashBox Account / Cr Expense Account.

---

### 3.12 Inventory Opening Entry (Product Opening Stock)

**Trigger:** Inside `ProductService.CreateAsync()` when opening stock is provided  
**Timing:** Inside `ExecuteTransactionAsync`, after product + stock creation  
**Condition:** Only if `totalOpeningValue > 0`  
**Reference:** `ReferenceType = "Product"`, `ReferenceId = product.Id`

#### 3.12.1 Journal Entry

| # | Account Key | Account Code | Name | Debit | Credit | Description |
|---|-------------|-------------|------|-------|--------|-------------|
| 1 | `Inventory` | 11040001 | بضاعة أول المدة | `totalOpeningValue` | — | `رصيد افتتاحي للمخزون: {productName}` |
| 2 | `OpeningBalanceEquity` | 32020001 | رصيد افتتاحي | — | `totalOpeningValue` | `رصيد افتتاحي للمخزون: {productName}` |

- **EntryType:** `JournalEntryType.OpeningBalance (9)`

---

## 4. Implementation Plan

### 4.1 New Files to Create

| File | Purpose |
|------|---------|
| `Application/Interfaces/Services/IAccountingIntegrationService.cs` | Interface with 17 methods |
| `Application/Accounting/Services/AccountingIntegrationService.cs` | Implementation class |

### 4.2 Files to Modify

| File | Change |
|------|--------|
| `Application/Services/CustomerService.cs` | Inject `IAccountingIntegrationService`; call `CreateCustomerOpeningEntryAsync` after create if OB > 0 |
| `Application/Services/SupplierService.cs` | Same pattern for supplier opening balance |
| `Application/Services/SalesService.cs` | Inject; call `CreateSalesPostEntryAsync` in PostAsync + `ReverseSalesPostEntryAsync` in CancelAsync |
| `Application/Services/PurchaseService.cs` | Inject; call `CreatePurchasePostEntryAsync` in PostAsync + `ReversePurchasePostEntryAsync` in CancelAsync |
| `Application/Services/CustomerReceiptService.cs` | Inject; call `CreateCustomerPaymentEntryAsync` on create/post + `ReverseCustomerPaymentEntryAsync` on delete/cancel |
| `Application/Services/SupplierPaymentService.cs` | Inject; call `CreateSupplierPaymentEntryAsync` on create/post + `ReverseSupplierPaymentEntryAsync` on delete/cancel |
| `Application/Services/SalesReturnService.cs` | Inject; call `CreateSalesReturnEntryAsync` on Post + `ReverseSalesReturnEntryAsync` on Cancel |
| `Application/Services/PurchaseReturnService.cs` | Inject; call `CreatePurchaseReturnEntryAsync` on Post + `ReversePurchaseReturnEntryAsync` on Cancel |
| `Application/Services/ExpenseService.cs` | Inject; call `CreateExpenseEntryAsync` on Post + `ReverseExpenseEntryAsync` on Cancel |
| `Application/Services/ProductService.cs` | Inject; call `CreateProductOpeningEntryAsync` when opening stock > 0 |
| `Api/Program.cs` | Register `IAccountingIntegrationService` → `AccountingIntegrationService` in DI |

### 4.3 Transaction Safety Requirements (RULE-275/276)

- ALL calls to `AccountingIntegrationService` MUST happen **inside an existing `ExecuteTransactionAsync` delegate**
- The existing services (`SalesService`, `PurchaseService`) already use `BeginTransactionAsync` — these MUST be migrated to `ExecuteTransactionAsync` where journal entries are added
- For `CustomerService.CreateAsync()` and `SupplierService.CreateAsync()` — currently use bare `SaveChangesAsync`; these MUST be wrapped in `ExecuteTransactionAsync` when opening balance > 0
- If the journal entry creation fails (returns `Result.Failure`), the entire transaction MUST roll back

### 4.4 COGS Calculation at Time of Sale

The `AccountingIntegrationService` receives `totalCost` as a parameter from the caller. The caller (`SalesService.PostAsync()`) computes it as:

```
totalCost = Σ(item retail quantity × product unit cost at time of sale)
```

For reversal, the COGS amount is **queried from the original journal entry** (looking up lines with `AccountId == CostOfGoodsSold && Debit > 0`), not recalculated from current product costs. This ensures exact-match reversal.

### 4.5 Fiscal Year Guard

Before creating ANY journal entry, the service MUST check `_uow.FiscalYearClosures.AnyAsync(fyc => fyc.FiscalYear == transactionDate.Year)`. Return `Result<int>.Failure("السنة المالية {year} مغلقة — لا يمكن إضافة قيود محاسبية")` if closed.

This applies to ALL 17 methods.

### 4.6 Number Generation

Each journal entry uses `_journalEntryService.CreateJournalEntryAsync(request, userId, ct)` which internally generates an entry number via `JournalEntryNumberGenerator` in `JE-{yyyyMMdd}-{NNNN}` format. The number is assigned before creating the `JournalEntry` domain entity.

---

## 5. Account Code Reference — SystemAccountMappings (Already Seeded)

| SystemAccountKey | Enum Value | Account Code | Account Name (Ar) |
|-----------------|------------|-------------|-------------------|
| DefaultCash | 1 | 11010001 | الصندوق |
| DefaultBank | 2 | 11020001 | البنك الأهلي |
| AccountsReceivable | 3 | 11030001 | العميل النقدي |
| AccountsPayable | 4 | 21010001 | المورد النقدي |
| Inventory | 5 | 11040001 | بضاعة أول المدة |
| CostOfGoodsSold | 6 | 51010001 | تكلفة البضاعة المباعة |
| SalesRevenue | 7 | 41010001 | إيرادات المبيعات |
| SalesReturns | 8 | 51020001 | مردودات مبيعات |
| PurchaseReturns | 9 | 51020002 | مردودات مشتريات |
| VatOutput | 10 | 21020001 | ضريبة المبيعات (خرج) |
| VatInput | 11 | 21020002 | ضريبة المشتريات (دخل) |
| Capital | 12 | 31010001 | رأس المال |
| OpeningBalanceEquity | 13 | 32020001 | رصيد افتتاحي |
| RetainedEarnings | 14 | 32010001 | أرباح مدورة |
| UndistributedProfits | 15 | 32030001 | أرباح غير موزعة |
| InventoryShortage | 16 | 52020002 | عجز مخزون |
| InventorySurplus | 17 | 52020003 | زيادة مخزون |
| GeneralExpense | 18 | 52010001 | مصروفات عمومية |
| SpoilageLoss | 19 | 52020001 | هالك المخزون |
| EmployeeCustody | 20 | 11070001 | عهدة الموظفين |
| DeliveryChargesRevenue | 21 | 41020003 | إيرادات التوصيل |

---

## 6. Testing Matrix

| Test Case | Expected JE Lines | Validation |
|-----------|------------------|------------|
| **Customer Create (OB > 0)** | Dr AR (Customer.AccountId), Cr OB Equity (32020001) | Balance zero |
| **Customer Create (OB = 0)** | No entry | — |
| **Supplier Create (OB > 0)** | Dr OB Equity (32020001), Cr AP (Supplier.AccountId) | Balance zero |
| **Product Create (OB > 0)** | Dr Inventory (11040001), Cr OB Equity (32020001) | Balance zero |
| **Sales Post (Cash, no tax)** | Dr Cash (11010001), Cr SalesRevenue (41010001), Dr COGS (51010001), Cr Inventory (11040001) | 4 lines balanced |
| **Sales Post (Credit, with tax, delivery)** | Dr AR (Customer.AccountId), Cr SalesRevenue + Cr DeliveryChargesRevenue + Cr VatOutput, Dr COGS, Cr Inventory | 6 lines balanced |
| **Sales Post (Mixed)** | Dr Cash + Dr AR, Cr SalesRevenue + Cr VatOutput + Dr COGS - Cr Inventory | 6 lines balanced |
| **Sales Cancel (Posted)** | Full reversal — queries original COGS lines | Mirrors post |
| **Sales Cancel (Draft)** | No entry | — |
| **Purchase Post (Cash, no tax)** | Dr Inventory (11040001), Cr Cash (11010001) | 2 lines |
| **Purchase Post (Credit, with tax)** | Dr Inventory + Dr VatInput (21020002), Cr AP (Supplier.AccountId) | 3 lines |
| **Purchase Post (with OtherCharges)** | Dr Inventory (incl. landed cost) + Dr VatInput, Cr Cash/AP | Landed cost included |
| **Purchase Cancel (Posted)** | Cr PurchaseReturns (51020002), Cr VatInput, Dr Cash/AP | Mirrors post |
| **Customer Payment** | Dr Cash (11010001), Cr AR (Customer.AccountId) | 2 lines |
| **Supplier Payment** | Dr AP (Supplier.AccountId), Cr Cash (11010001) | 2 lines |
| **Sales Return** | Dr SalesReturns (51020001), Cr AR + Dr Inventory, Cr COGS | 4 lines |
| **Purchase Return** | Dr AP, Cr PurchaseReturns (51020002) | 2 lines |
| **Expense Post** | Dr ExpenseAccount, Cr CashBox.Account | 2 lines |
| **Expense Cancel** | Dr CashBox.Account, Cr ExpenseAccount | 2 lines reversal |
| **Fiscal Year Closed** | All 17 methods return `Result.Failure` with Arabic message | Blocked |
| **SystemAccountMapping not found** | All methods return `Result.Failure` | Graceful error |
| **NetRevenue negative (Discount > SubTotal)** | `Result.Failure` returned — never clamp to zero | RULE-379 |

---

## 7. Migration & Rollback Plan

### 7.1 Database Migration

No new tables or columns are needed. All Chart of Accounts and SystemAccountMappings are already seeded from Phase 18.

### 7.2 Rollback

If the accounting integration causes issues:
1. Remove the `IAccountingIntegrationService` injection from each business service
2. Comment out the integration calls (4-5 lines per method)
3. Remove DI registration from `Program.cs`
4. The system continues to work without journal entries (as it currently does)
5. No data loss — existing data integrity is preserved
6. The `SystemAccountMapping` table and seeded accounts remain but are unused

---

## 8. Summary of All Changes

| # | File | Type | Description |
|---|------|------|-------------|
| 1 | `IAccountingIntegrationService.cs` | NEW | Interface with 17 methods |
| 2 | `AccountingIntegrationService.cs` | NEW | Implementation class |
| 3 | `CustomerService.cs` | Modify | Inject + call opening entry after create |
| 4 | `SupplierService.cs` | Modify | Inject + call opening entry after create |
| 5 | `SalesService.cs` | Modify | Inject + call in Post/Cancel (with COGS) |
| 6 | `PurchaseService.cs` | Modify | Inject + call in Post/Cancel (with landed cost) |
| 7 | `CustomerReceiptService.cs` | Modify | Inject + call in Create/Delete |
| 8 | `SupplierPaymentService.cs` | Modify | Inject + call in Create/Delete |
| 9 | `SalesReturnService.cs` | Modify | Inject + call in Post/Cancel |
| 10 | `PurchaseReturnService.cs` | Modify | Inject + call in Post/Cancel |
| 11 | `ExpenseService.cs` | Modify | Inject + call in Post/Cancel |
| 12 | `ProductService.cs` | Modify | Inject + call for opening stock |
| 13 | `Api/Program.cs` | Modify | Register `IAccountingIntegrationService` DI |
| 14 | `SystemAccountMapping.cs` | Entity | Already exists from Phase 18 |
| 15 | `AccountingSeeder.cs` | Seeder | Already seeded 81 accounts + 21 mappings from Phase 18 |

**Total: 15 files (2 new, 13 modified)**

### IAccountingIntegrationService Interface (17 Methods)

```csharp
public interface IAccountingIntegrationService
{
    // Opening entries
    Task<Result<int>> CreateCustomerOpeningEntryAsync(...);
    Task<Result<int>> CreateSupplierOpeningEntryAsync(...);
    Task<Result<int>> CreateProductOpeningEntryAsync(...);

    // Sales invoice
    Task<Result<int>> CreateSalesPostEntryAsync(SalesInvoice invoice, int userId, decimal totalCost, CancellationToken ct);
    Task<Result<int>> ReverseSalesPostEntryAsync(SalesInvoice invoice, int userId, CancellationToken ct);

    // Purchase invoice
    Task<Result<int>> CreatePurchasePostEntryAsync(PurchaseInvoice invoice, int userId, CancellationToken ct);
    Task<Result<int>> ReversePurchasePostEntryAsync(PurchaseInvoice invoice, int userId, CancellationToken ct);

    // Customer payment (receipt)
    Task<Result<int>> CreateCustomerPaymentEntryAsync(...);
    Task<Result<int>> ReverseCustomerPaymentEntryAsync(...);

    // Supplier payment
    Task<Result<int>> CreateSupplierPaymentEntryAsync(...);
    Task<Result<int>> ReverseSupplierPaymentEntryAsync(...);

    // Sales return
    Task<Result<int>> CreateSalesReturnEntryAsync(SalesReturn salesReturn, decimal totalCost, int userId, CancellationToken ct);
    Task<Result<int>> ReverseSalesReturnEntryAsync(SalesReturn salesReturn, decimal totalCost, int userId, CancellationToken ct);

    // Purchase return
    Task<Result<int>> CreatePurchaseReturnEntryAsync(PurchaseReturn purchaseReturn, int userId, CancellationToken ct);
    Task<Result<int>> ReversePurchaseReturnEntryAsync(PurchaseReturn purchaseReturn, int userId, CancellationToken ct);

    // Expense
    Task<Result<int>> CreateExpenseEntryAsync(Expense expense, int userId, CancellationToken ct);
    Task<Result<int>> ReverseExpenseEntryAsync(Expense expense, int userId, CancellationToken ct);
}
```
