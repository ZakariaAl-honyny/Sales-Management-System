# Phase 24 — Accounting Engine Automation

## Design Overview

Currently, NO business operation creates journal entries automatically. This phase integrates double-entry accounting into all financial operations by introducing a dedicated `AccountingIntegrationService` that is called from within existing service transactions.

---

## 1. Schema Additions

### 1.1 New Account: Opening Balance Equity

| Field | Value |
|-------|-------|
| `AccountCode` | `1422` |
| `NameAr` | `أرصدة افتتاحية` |
| `NameEn` | `Opening Balance Equity` |
| `AccountType` | `Equity (3)` |
| `Level` | `3` |
| `ParentAccountId` | Under `1420` (الأرباح والخسائر / Profit & Loss) |
| `AllowTransactions` | `true` |
| `IsSystemAccount` | `false` |
| `Description` | `رصيد افتتاحي للعملاء والموردين — يتم إقفاله بعد تسوية الأرصدة الافتتاحية` |
| `Explanation` | `حساب مؤقت يستخدم لتسجيل الأرصدة الافتتاحية للعملاء والموردين عند بدء استخدام النظام. يقفل بعد تسوية جميع الأرصدة الافتتاحية مع رأس المال.` |
| `ColorCode` | `#4CAF50` (Equity green) |

**Seeder note:** This account is NOT a system account. It is user-visible and can be modified. It will be seeded alongside other Level-3 accounts in `AccountingSeeder` (inserted after account `1421`).

### 1.2 New SystemAccountMappings Fields

Add the following field to `SystemAccountMappings`:

| Field | Type | Maps To | Purpose |
|-------|------|---------|---------|
| `OpeningBalanceEquityAccountId` | `int` | `1422` (أرصدة افتتاحية) | Used for customer/supplier opening balance entries |

**To add to the entity (`SystemAccountMappings.cs`):**

```csharp
public int OpeningBalanceEquityAccountId { get; private set; }
public Account? OpeningBalanceEquityAccount { get; private set; }
```

**To add to `Create()` factory:**

```csharp
if (openingBalanceEquityAccountId <= 0)
    throw new DomainException("رقم حساب الأرصدة الافتتاحية مطلوب");
```

**To add to guards and seed data in `AccountingSeeder`:**

After the 13 existing mappings, add:
```csharp
openingBalanceEquityAccountId: allAccounts["1422"].Id,
```

**To add to `SystemAccountMappingsConfiguration`:**

```csharp
builder.Property(x => x.OpeningBalanceEquityAccountId)
    .IsRequired()
    .HasComment("أرصدة افتتاحية (حقوق الملكية)");

builder.HasOne(x => x.OpeningBalanceEquityAccount)
    .WithMany()
    .HasForeignKey(x => x.OpeningBalanceEquityAccountId)
    .OnDelete(DeleteBehavior.Restrict);
```

### 1.3 New JournalEntryType Enum Values

Add the following to `JournalEntryType.cs`:

```csharp
CustomerReceipt = 10,   // تحصيل من عميل
SupplierPayment = 11,   // دفع لمورد
```

These new types enable traceability — payment journal entries are identifiable by type in reports and account ledgers.

### 1.4 Summary of Schema Changes

| Change | File | Type |
|--------|------|------|
| New account `1422` (أرصدة افتتاحية) | `AccountingSeeder.cs` | Seeder data |
| Field `OpeningBalanceEquityAccountId` | `SystemAccountMappings.cs` | Entity + Config |
| Nav property `OpeningBalanceEquityAccount` | `SystemAccountMappings.cs` | Navigation |
| Enum values `CustomerReceipt=10`, `SupplierPayment=11` | `JournalEntryType.cs` | Enum |
| CreateAccountRequest DTO unchanged | — | — |
| UpdateAccountRequest DTO unchanged | — | — |

---

## 2. Service Layer: `AccountingIntegrationService`

All accounting entry creation is centralized into a single **dedicated service** to avoid duplicating account-resolution logic across five business services.

### 2.1 Interface

```csharp
public interface IAccountingIntegrationService
{
    // ─── Opening Balances ───────────────────────────────
    Task<Result<int>> CreateCustomerOpeningEntryAsync(
        Customer customer, int userId, CancellationToken ct);

    Task<Result<int>> CreateSupplierOpeningEntryAsync(
        Supplier supplier, int userId, CancellationToken ct);

    // ─── Sales Invoices ─────────────────────────────────
    Task<Result<int>> CreateSalesInvoiceEntryAsync(
        SalesInvoice invoice, int userId, CancellationToken ct);

    Task<Result<int>> ReverseSalesInvoiceEntryAsync(
        SalesInvoice invoice, int userId, CancellationToken ct);

    // ─── Purchase Invoices ──────────────────────────────
    Task<Result<int>> CreatePurchaseInvoiceEntryAsync(
        PurchaseInvoice invoice, int userId, CancellationToken ct);

    Task<Result<int>> ReversePurchaseInvoiceEntryAsync(
        PurchaseInvoice invoice, int userId, CancellationToken ct);

    // ─── Payments ───────────────────────────────────────
    Task<Result<int>> CreateCustomerPaymentEntryAsync(
        CustomerPayment payment, int userId, CancellationToken ct);

    Task<Result<int>> CreateSupplierPaymentEntryAsync(
        SupplierPayment payment, int userId, CancellationToken ct);
}
```

### 2.2 Dependencies

```csharp
public class AccountingIntegrationService : IAccountingIntegrationService
{
    private readonly IUnitOfWork _uow;
    private readonly IJournalEntryNumberGenerator _numberGenerator;
    private readonly ILogger<AccountingIntegrationService> _logger;

    public AccountingIntegrationService(
        IUnitOfWork uow,
        IJournalEntryNumberGenerator numberGenerator,
        ILogger<AccountingIntegrationService> logger)
    { ... }
}
```

### 2.3 Registration (DI)

```csharp
// In Api/Program.cs or Application/DependencyInjection.cs
builder.Services.AddScoped<IAccountingIntegrationService, AccountingIntegrationService>();
```

### 2.4 Internal Helper Methods

To avoid repetition, the service has two key helper methods:

```csharp
/// <summary>
/// Loads the SystemAccountMappings singleton with all navigation properties eagerly loaded.
/// Throws Result.Failure if not found.
/// </summary>
private async Task<Result<SystemAccountMappings>> GetMappingsAsync(CancellationToken ct);

/// <summary>
/// Loads all accounts referenced in the mappings to get Code + NameAr for journal lines.
/// </summary>
private async Task<Dictionary<int, Account>> GetAccountsMapAsync(
    SystemAccountMappings mappings, CancellationToken ct);

/// <summary>
/// Creates, validates, posts, and persists a journal entry in a single atomic operation.
/// All callers use this method — handles number generation, balance check, fiscal year guard.
/// </summary>
private async Task<Result<int>> CreateAndPostEntryAsync(
    JournalEntry entry, int userId, CancellationToken ct);
```

The `CreateAndPostEntryAsync` will call `entry.ValidateAndPost(userId)` before saving.

**Fiscal year guard requirement (RULE-281):** Before creating ANY journal entry, the service MUST check `_uow.FiscalYearClosures.AnyAsync(fyc => fyc.FiscalYear == transactionDate.Year)`. Return `Result<int>.Failure("السنة المالية {year} مغلقة — لا يمكن إضافة قيود محاسبية")` if closed.

---

## 3. Exact Journal Entry Specifications

For each operation below, the entry is created **inside the same transaction** as the business operation.

### 3.0 Determining Cash/AR Account by PaymentType

All invoice and payment entries use this resolution logic:

| PaymentType | Debit/Credit Account | Condition |
|------------|---------------------|-----------|
| `Cash (1)` | `Mappings.DefaultCashAccountId` (1111) | Full amount |
| `Credit (2)` | `Mappings.AccountsReceivableAccountId` (1131) or `Mappings.AccountsPayableAccountId` (1321) | Full amount |
| `Mixed (3)` | Split: `DefaultCashAccountId` for `PaidAmount`, `AR/AP` for `DueAmount` | Partial |

**For Sales (Revenue side):**
- Cash: Dr `DefaultCashAccountId` for `PaidAmount`
- Credit: Dr `AccountsReceivableAccountId` for `DueAmount`
- Mixed: Dr `DefaultCashAccountId` for `PaidAmount` + Dr `AccountsReceivableAccountId` for `DueAmount`

**For Purchases (Cost side):**
- Cash: Cr `DefaultCashAccountId` for `PaidAmount`
- Credit: Cr `AccountsPayableAccountId` for `DueAmount`
- Mixed: Cr `DefaultCashAccountId` for `PaidAmount` + Cr `AccountsPayableAccountId` for `DueAmount`

---

### 3.1 Customer OpeningBalance

**Trigger:** Inside `CustomerService.CreateAsync()` when `openingBalance > 0`  
**Timing:** AFTER customer is saved to DB (has an ID), inside wrap in `ExecuteTransactionAsync`  
**Condition:** Only if `OpeningBalance > 0` — otherwise no entry  
**Reference:** `ReferenceType = "Customer"`, `ReferenceId = customer.Id`, `ReferenceNumber = customer.Name`

#### Journal Entry

| # | Account | Debit | Credit | Description |
|---|---------|-------|--------|-------------|
| 1 | `Mappings.AccountsReceivableAccountId` (1131 — العميل النقدي) | `OpeningBalance` | — | `رصيد افتتاحي للعميل: {customerName}` |
| 2 | `Mappings.OpeningBalanceEquityAccountId` (1422 — أرصدة افتتاحية) | — | `OpeningBalance` | `رصيد افتتاحي للعميل: {customerName}` |

- **EntryType:** `JournalEntryType.OpeningBalance (9)`
- **Balance check:** Dr = Cr = OpeningBalance ✓
- **Edge case:** If `openingBalance == 0`, skip entry entirely

#### Order inside CustomerService.CreateAsync()

```
1. Create Customer entity (in memory)
2. SaveChangesAsync → customer gets ID
3. IF openingBalance > 0:
     a. Call AccountingIntegrationService.CreateCustomerOpeningEntryAsync()
        - This loads SystemAccountMappings
        - Generates JE number
        - Creates journal entry
        - Saves journal entry + lines
4. Return success
```

**Transaction change needed:** `CustomerService.CreateAsync()` currently uses bare `SaveChangesAsync()`. When `openingBalance > 0`, the save must be wrapped in `ExecuteTransactionAsync` to atomically save BOTH the customer and the journal entry.

---

### 3.2 Supplier OpeningBalance

**Trigger:** Inside `SupplierService.CreateAsync()` when `openingBalance > 0`  
**Timing:** AFTER supplier is saved to DB (has an ID), in same `ExecuteTransactionAsync`  
**Condition:** Only if `OpeningBalance > 0`  
**Reference:** `ReferenceType = "Supplier"`, `ReferenceId = supplier.Id`, `ReferenceNumber = supplier.Name`

#### Journal Entry

| # | Account | Debit | Credit | Description |
|---|---------|-------|--------|-------------|
| 1 | `Mappings.OpeningBalanceEquityAccountId` (1422) | `OpeningBalance` | — | `رصيد افتتاحي للمورد: {supplierName}` |
| 2 | `Mappings.AccountsPayableAccountId` (1321 — المورد النقدي) | — | `OpeningBalance` | `رصيد افتتاحي للمورد: {supplierName}` |

- **EntryType:** `JournalEntryType.OpeningBalance (9)`

---

### 3.3 Sales Invoice Post

**Trigger:** Inside `SalesService.PostAsync()` AFTER stock is deducted and customer balance updated  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "SalesInvoice"`, `ReferenceId = invoice.Id`, `ReferenceNumber = invoice.InvoiceNo.ToString()`

#### 3.3.1 Revenue & COGS Values

| Value | Source | Formula |
|-------|--------|---------|
| `NetRevenue` | `invoice.SubTotal - invoice.DiscountAmount` | Already computed on invoice |
| `TaxAmount` | `invoice.TaxAmount` | Already computed |
| `TotalAmount` | `invoice.TotalAmount` | `NetRevenue + TaxAmount` |
| `PaidAmount` | `invoice.PaidAmount` | Cash portion |
| `DueAmount` | `invoice.DueAmount` | Credit portion |
| `COGS` | **Computed at time of post** | See below |

**COGS = Σ(for each item: `retailQty` × `productUnit.PurchaseCost`)**

Where `retailQty = item.Product.GetRetailQuantityEquivalent(item.Quantity, item.Mode)`  
and `productUnit` = `item.Product.GetBaseUnit()` (base unit holds the current weighted average cost).

**Important:** The `PurchaseCost` on the base `ProductUnit` is the current weighted average cost at the time of sale (updated by `UpdateProductPricingService` during purchase posting).

#### 3.3.2 Account Resolution for Cash/AR (Revenue Side)

Use the `SystemAccountMappings.GetPaymentAccountId()` method extended for Mixed:

```csharp
private (int cashAccountId, int arAccountId) GetSalesPaymentAccounts(
    SystemAccountMappings mappings, SalesInvoice invoice)
{
    var cashId = mappings.DefaultCashAccountId;   // 1111
    var arId = mappings.AccountsReceivableAccountId; // 1131

    return invoice.PaymentType switch
    {
        PaymentType.Cash => (cashId, 0),       // all to cash
        PaymentType.Credit => (0, arId),       // all to AR
        PaymentType.Mixed => (cashId, arId),   // split
        _ => (cashId, 0)
    };
}
```

#### 3.3.3 Journal Entry — Single Compound Entry (5 lines)

| # | Account | Debit | Credit | Description |
|---|---------|-------|--------|-------------|
| 1 | Cash Account (1111) — if `PaidAmount > 0` | `PaidAmount` | — | `سداد نقدي — فاتورة مبيعات رقم {invoiceNo}` |
| 2 | AR Account (1131) — if `DueAmount > 0` | `DueAmount` | — | `رصيد آجل — فاتورة مبيعات رقم {invoiceNo}` |
| 3 | Sales Revenue (1521) | — | `NetRevenue` | `إيراد مبيعات — فاتورة رقم {invoiceNo}` |
| 4 | VAT Output (1331) — if `TaxAmount > 0` | — | `TaxAmount` | `ضريبة مبيعات — فاتورة رقم {invoiceNo}` |
| 5 | COGS (1621) | `COGS` | — | `تكلفة البضاعة المباعة — فاتورة رقم {invoiceNo}` |
| 6 | Inventory (1141) | — | `COGS` | `تخفيض المخزون — فاتورة رقم {invoiceNo}` |

- **EntryType:** `JournalEntryType.Sales (1)`
- **Balance check:**
  - Debits: `PaidAmount + DueAmount + COGS` = `TotalAmount + COGS`
  - Credits: `NetRevenue + TaxAmount + COGS` = `(SubTotal - Discount) + TaxAmount + COGS` = `TotalAmount + COGS` ✓
- **Missing lines:** If `PaidAmount == 0`, skip line 1. If `DueAmount == 0`, skip line 2. If `TaxAmount == 0`, skip line 4.

#### 3.3.4 Order inside SalesService.PostAsync()

```
Existing operations (in order):
1. invoice.Post()
2. SaveChangesAsync
3. Deduct Stock (foreach item)
4. Update Customer Balance (if DueAmount > 0)
5. Record Cash Transaction (if CashBoxId.HasValue)
--- NEW: Accounting Integration ---
6. Calculate COGS from current PurchaseCost per item
7. Call AccountingIntegrationService.CreateSalesInvoiceEntryAsync(invoice, userId)
   - Loads mappings + accounts
   - Generates JE number
   - Creates 5- or 6-line journal entry
   - Saves journal entry + lines
   - Fails → rollback entire transaction
--- End New ---
8. SaveChangesAsync
9. CommitAsync
```

---

### 3.4 Sales Invoice Cancel

**Trigger:** Inside `SalesService.CancelAsync()` AFTER stock and balance have been reversed  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "SalesInvoiceCancel"`, `ReferenceId = invoice.Id`, `ReferenceNumber = invoice.InvoiceNo.ToString()`

**Condition:** Only if invoice was `Status == Posted` before this cancel call. If the invoice was still `Draft`, no journal entry was created, so no reversal is needed.

#### 3.4.1 Journal Entry — Full Reversal

Reverses ALL lines from the Post entry (debit ↔ credit swap):

| # | Account | Debit | Credit | Description |
|---|---------|-------|--------|-------------|
| 1 | Cash Account (1111) — if original `PaidAmount > 0` | — | `PaidAmount` | `عكس سداد نقدي — إلغاء فاتورة رقم {invoiceNo}` |
| 2 | AR Account (1131) — if original `DueAmount > 0` | — | `DueAmount` | `عكس رصيد آجل — إلغاء فاتورة رقم {invoiceNo}` |
| 3 | Sales Returns (1631) | `NetRevenue` | — | `مردود مبيعات — إلغاء فاتورة رقم {invoiceNo}` |
| 4 | VAT Output (1331) — if original `TaxAmount > 0` | `TaxAmount` | — | `عكس ضريبة مبيعات — فاتورة رقم {invoiceNo}` |
| 5 | COGS (1621) | — | `COGS` | `عكس تكلفة البضاعة المباعة — إلغاء فاتورة رقم {invoiceNo}` |
| 6 | Inventory (1141) | `COGS` | — | `إعادة المخزون — إلغاء فاتورة رقم {invoiceNo}` |

- **EntryType:** `JournalEntryType.SalesReturn (2)`
- **Balance check:**
  - Debits: `NetRevenue + TaxAmount + COGS` = `TotalAmount + COGS`
  - Credits: `PaidAmount + DueAmount + COGS` = `TotalAmount + COGS` ✓

#### 3.4.2 Order inside SalesService.CancelAsync()

```
Existing operations (in order):
1. IF Status == Posted:
   a. Reverse Stock (foreach item)
   b. Reverse Customer Balance (if DueAmount > 0)
   c. Create offsetting cash transaction (if CashBoxId)
--- NEW: Accounting Integration ---
2. IF Status == Posted:
   a. Calculate COGS (same as post — reuse the same method)
   b. Call AccountingIntegrationService.ReverseSalesInvoiceEntryAsync(invoice, userId)
   c. Fails → rollback entire transaction
--- End New ---
3. invoice.SetPaidAmount(0)  // Zero out
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
| `NetPurchaseCost` | `invoice.SubTotal - invoice.DiscountAmount` | Net cost of goods |
| `TaxAmount` | `invoice.TaxAmount` | VAT input |
| `TotalAmount` | `invoice.TotalAmount` | `NetPurchaseCost + TaxAmount` |
| `PaidAmount` | `invoice.PaidAmount` | Cash paid |
| `DueAmount` | `invoice.DueAmount` | Amount owed to supplier |

#### 3.5.2 Account Resolution

For purchases, the payment goes to either Cash or AP:

```csharp
private (int cashAccountId, int apAccountId) GetPurchasePaymentAccounts(
    SystemAccountMappings mappings, PurchaseInvoice invoice)
{
    var cashId = mappings.DefaultCashAccountId;    // 1111
    var apId = mappings.AccountsPayableAccountId;   // 1321

    return invoice.PaymentType switch
    {
        PaymentType.Cash => (cashId, 0),
        PaymentType.Credit => (0, apId),
        PaymentType.Mixed => (cashId, apId),
        _ => (cashId, 0)
    };
}
```

#### 3.5.3 Journal Entry

| # | Account | Debit | Credit | Description |
|---|---------|-------|--------|-------------|
| 1 | Inventory (1141) | `NetPurchaseCost` | — | `تكلفة المشتريات — فاتورة رقم {invoiceNo}` |
| 2 | VAT Input (1332) — if `TaxAmount > 0` | `TaxAmount` | — | `ضريبة مشتريات — فاتورة رقم {invoiceNo}` |
| 3 | Cash Account (1111) — if `PaidAmount > 0` | — | `PaidAmount` | `دفع نقدي — فاتورة مشتريات رقم {invoiceNo}` |
| 4 | AP Account (1321) — if `DueAmount > 0` | — | `DueAmount` | `رصيد دائن — فاتورة مشتريات رقم {invoiceNo}` |

- **EntryType:** `JournalEntryType.Purchase (3)`
- **Balance check:**
  - Debits: `NetPurchaseCost + TaxAmount` = `TotalAmount`
  - Credits: `PaidAmount + DueAmount` = `TotalAmount` ✓

#### 3.5.4 Order inside PurchaseService.PostAsync()

```
Existing operations (in order):
1. invoice.Post()
2. SaveChangesAsync
3. AutoUpdatePrices (if enabled)
4. Increase Stock (foreach item)
5. Update Pricing/Costing (foreach item)
6. Update Supplier Balance (if DueAmount > 0)
7. Record Cash Transaction (if CashBoxId.HasValue)
--- NEW: Accounting Integration ---
8. Call AccountingIntegrationService.CreatePurchaseInvoiceEntryAsync(invoice, userId)
   - Loads mappings + accounts
   - Generates JE number
   - Creates 3- or 4-line journal entry
   - Saves journal entry + lines
   - Fails → rollback entire transaction
--- End New ---
9. SaveChangesAsync
10. CommitAsync
```

---

### 3.6 Purchase Invoice Cancel

**Trigger:** Inside `PurchaseService.CancelAsync()` AFTER stock and balance have been reversed  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "PurchaseInvoiceCancel"`, `ReferenceId = invoice.Id`, `ReferenceNumber = invoice.InvoiceNo.ToString()`

**Condition:** Only if invoice was `Status == Posted`.

#### 3.6.1 Journal Entry — Full Reversal

| # | Account | Debit | Credit | Description |
|---|---------|-------|--------|-------------|
| 1 | Inventory (1141) | — | `NetPurchaseCost` | `عكس تكلفة المشتريات — إلغاء فاتورة رقم {invoiceNo}` |
| 2 | VAT Input (1332) — if original `TaxAmount > 0` | — | `TaxAmount` | `عكس ضريبة مشتريات — إلغاء فاتورة رقم {invoiceNo}` |
| 3 | Cash Account (1111) — if original `PaidAmount > 0` | `PaidAmount` | — | `عكس دفع نقدي — إلغاء فاتورة رقم {invoiceNo}` |
| 4 | AP Account (1321) — if original `DueAmount > 0` | `DueAmount` | — | `عكس رصيد دائن — إلغاء فاتورة رقم {invoiceNo}` |

- **EntryType:** `JournalEntryType.PurchaseReturn (4)`
- **Balance check:**
  - Debits: `PaidAmount + DueAmount` = `TotalAmount`
  - Credits: `NetPurchaseCost + TaxAmount` = `TotalAmount` ✓

#### 3.6.2 Order inside PurchaseService.CancelAsync()

```
Existing operations (in order):
1. IF Status == Posted:
   a. Reverse Stock (foreach item)
   b. Reverse Supplier Balance (if DueAmount > 0)
   c. Reverse Cash Transaction (if CashBoxId)
--- NEW: Accounting Integration ---
2. IF Status == Posted:
   a. Call AccountingIntegrationService.ReversePurchaseInvoiceEntryAsync(invoice, userId)
   b. Fails → rollback entire transaction
--- End New ---
3. invoice.SetPaidAmount(0)
4. invoice.Cancel()
5. SaveChangesAsync
6. CommitAsync
```

---

### 3.7 Customer Payment

**Trigger:** Inside `PaymentService.CreateCustomerPaymentAsync()` AFTER customer balance is decreased  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "CustomerPayment"`, `ReferenceId = payment.Id`, `ReferenceNumber = payment.PaymentNo`

#### 3.7.1 Journal Entry

| # | Account | Debit | Credit | Description |
|---|---------|-------|--------|-------------|
| 1 | `Mappings.DefaultCashAccountId` (1111) | `payment.Amount` | — | `تحصيل من العميل {customerName} — سند رقم {paymentNo}` |
| 2 | `Mappings.AccountsReceivableAccountId` (1131) | — | `payment.Amount` | `تحصيل من العميل {customerName} — سند رقم {paymentNo}` |

- **EntryType:** `JournalEntryType.CustomerReceipt (10)`
- **Balance check:** Dr = Cr = Amount ✓

#### 3.7.2 Order inside PaymentService.CreateCustomerPaymentAsync()

```
Existing operations (in order):
1. Create CustomerPayment entity
2. AddAsync(payment)
3. customer.DecreaseBalance(amount)
--- NEW: Accounting Integration ---
4. Call AccountingIntegrationService.CreateCustomerPaymentEntryAsync(payment, userId)
   - Fails → rollback entire transaction
--- End New ---
5. SaveChangesAsync
6. CommitAsync
```

#### 3.7.3 Edge Cases

- **PaymentMethod** (if needed for payment clearing): The current `PaymentMethod` field is a `byte` that can map to `Cash=1`, `Bank=2`, etc. However, the accounting for customer receipt always reduces AR regardless of payment method — the debit to Cash vs Bank depends on how the money was received. For Phase 24, use `DefaultCashAccountId` as the default debit account for all customer payments. A future enhancement can resolve to Bank if `PaymentMethod == 2`.

---

### 3.8 Supplier Payment

**Trigger:** Inside `PaymentService.CreateSupplierPaymentAsync()` AFTER supplier balance is decreased  
**Timing:** Inside the existing transaction, before `CommitAsync()`  
**Reference:** `ReferenceType = "SupplierPayment"`, `ReferenceId = payment.Id`, `ReferenceNumber = payment.PaymentNo`

#### 3.8.1 Journal Entry

| # | Account | Debit | Credit | Description |
|---|---------|-------|--------|-------------|
| 1 | `Mappings.AccountsPayableAccountId` (1321) | `payment.Amount` | — | `دفع للمورد {supplierName} — سند رقم {paymentNo}` |
| 2 | `Mappings.DefaultCashAccountId` (1111) | — | `payment.Amount` | `دفع للمورد {supplierName} — سند رقم {paymentNo}` |

- **EntryType:** `JournalEntryType.SupplierPayment (11)`
- **Balance check:** Dr = Cr = Amount ✓

#### 3.8.2 Order inside PaymentService.CreateSupplierPaymentAsync()

```
Existing operations (in order):
1. Create SupplierPayment entity
2. AddAsync(payment)
3. supplier.DecreaseBalance(amount)
--- NEW: Accounting Integration ---
4. Call AccountingIntegrationService.CreateSupplierPaymentEntryAsync(payment, userId)
   - Fails → rollback entire transaction
--- End New ---
5. SaveChangesAsync
6. CommitAsync
```

---

## 4. Implementation Plan

### 4.1 New Files to Create

| File | Purpose |
|------|---------|
| `Application/Interfaces/Services/IAccountingIntegrationService.cs` | Interface with 8 methods |
| `Application/Accounting/Services/AccountingIntegrationService.cs` | Implementation class |

### 4.2 Files to Modify

| File | Change |
|------|--------|
| `Domain/Accounting/Entities/SystemAccountMappings.cs` | Add `OpeningBalanceEquityAccountId` field + nav property |
| `Domain/Accounting/Enums/JournalEntryType.cs` | Add `CustomerReceipt=10`, `SupplierPayment=11` |
| `Infrastructure/Data/Configurations/SystemAccountMappingsConfiguration.cs` | Add property mapping + FK |
| `Infrastructure/Data/Seeders/AccountingSeeder.cs` | Add account `1422`, update SystemAccountMappings seed |
| `Application/Services/CustomerService.cs` | Inject `IAccountingIntegrationService`; call after create if OB > 0 |
| `Application/Services/SupplierService.cs` | Same pattern |
| `Application/Services/SalesService.cs` | Inject `IAccountingIntegrationService`; call in PostAsync + CancelAsync |
| `Application/Services/PurchaseService.cs` | Same pattern |
| `Application/Services/PaymentService.cs` | Inject `IAccountingIntegrationService`; call in both payment methods |

### 4.3 Transaction Safety Requirements (RULE-275/276)

- ALL calls to `AccountingIntegrationService` MUST happen **inside an existing `ExecuteTransactionAsync` delegate**
- The existing services (`SalesService`, `PurchaseService`, `PaymentService`) already use `BeginTransactionAsync` + `ExecuteAsync` — these need to be migrated to `ExecuteTransactionAsync` where journal entries are added
- For `CustomerService.CreateAsync()` and `SupplierService.CreateAsync()` — currently use bare `SaveChangesAsync`; these MUST be wrapped in `ExecuteTransactionAsync` when opening balance > 0
- If the journal entry creation fails (returns `Result.Failure`), the entire transaction MUST roll back

### 4.4 COGS Calculation at Time of Sale

The `AccountingIntegrationService` needs a private helper to calculate COGS:

```csharp
private async Task<decimal> CalculateCOGSAsync(
    SalesInvoice invoice, CancellationToken ct)
{
    decimal totalCogs = 0;

    foreach (var item in invoice.Items)
    {
        if (item.Product == null) continue;

        var baseUnit = item.Product.GetBaseUnit();
        var retailQty = item.Product.GetRetailQuantityEquivalent(
            item.Quantity, item.Mode);
        var unitCost = baseUnit.PurchaseCost; // Current weighted avg cost

        totalCogs += retailQty * unitCost;
    }

    return Math.Round(totalCogs, 2);
}
```

**Important caveat:** The `item.Product` navigation property must be eagerly loaded for COGS calculation. In `SalesService.PostAsync()`, line 221 shows `"Items.Product"` is already included in the query — so `item.Product` is available.

### 4.5 Fiscal Year Guard

Every method in `AccountingIntegrationService` that creates a journal entry MUST call:

```csharp
var isFiscalYearClosed = await _uow.FiscalYearClosures.AnyAsync(
    fyc => fyc.FiscalYear == transactionDate.Year, ct);
if (isFiscalYearClosed)
    return Result<int>.Failure(
        $"السنة المالية {transactionDate.Year} مغلقة — لا يمكن إضافة قيود محاسبية");
```

This applies to ALL 8 methods.

### 4.6 Number Generation

Each journal entry uses `_numberGenerator.GenerateAsync(ct)` which generates `JE-{yyyyMMdd}-{NNNN}` format. The number is assigned before creating the `JournalEntry` domain entity.

---

## 5. COGS Detail — Why and How

### 5.1 Why COGS Matters

Currently when a sales invoice is posted, stock is deducted but no accounting entry recognizes the cost of goods sold. This means:

- **Inventory asset account** is never credited → inventory value in the balance sheet does not decrease
- **COGS expense account** is never debited → profit is overstated on the income statement
- The business cannot run a proper income statement or balance sheet

**Without COGS entry:** Revenue is recognized but the associated cost is not — gross profit is inflated.

### 5.2 How COGS is Captured

The `ProductUnit.PurchaseCost` field holds the current weighted average cost per base unit (updated by `UpdateProductPricingService` after each purchase). At sale time:

```
LineCOGS = retailQuantity × baseUnit.PurchaseCost
TotalCOGS = Σ(LineCOGS across all items)
```

The retail quantity is obtained via `item.Product.GetRetailQuantityEquivalent(item.Quantity, item.Mode)` — this ensures wholesale quantities are converted to base units before multiplying by the base unit cost.

### 5.3 COGS for Cancel/Reverse

On cancellation, the SAME COGS amount from the original post is used for reversal. Since we don't store COGS on the invoice entity, the `AccountingIntegrationService.ReverseSalesInvoiceEntryAsync()` must recalculate it using the same formula (product cost at reversal time).

**Risk:** If product costs have changed between post and cancel, the COGS reversal amount may differ. This is acceptable because:
1. Cancellations are typically near-term (same day/week)
2. The cost at reversal represents the current inventory valuation
3. Small discrepancies (± a few SAR) are immaterial

---

## 6. Testing Matrix

| Test Case | Expected JE Lines | Validation |
|-----------|------------------|------------|
| **Customer Create (OB > 0)** | Dr AR, Cr OB Equity | Balance zero |
| **Customer Create (OB = 0)** | No entry | — |
| **Supplier Create (OB > 0)** | Dr OB Equity, Cr AP | Balance zero |
| **Sales Post (Cash, no tax)** | Dr Cash, Cr Revenue, Dr COGS, Cr Inventory | 4 lines balanced |
| **Sales Post (Credit, with tax)** | Dr AR, Cr Revenue, Cr VAT, Dr COGS, Cr Inventory | 5 lines balanced |
| **Sales Post (Mixed)** | Dr Cash + Dr AR, Cr Revenue + Cr VAT + Dr COGS - Cr Inventory | 6 lines balanced |
| **Sales Cancel (Posted)** | Full reversal | Mirrors post |
| **Sales Cancel (Draft)** | No entry | — |
| **Purchase Post (Cash)** | Dr Inventory, Cr Cash | 2 lines |
| **Purchase Post (Credit, with tax)** | Dr Inv + Dr VAT Input, Cr AP | 3 lines |
| **Purchase Cancel (Posted)** | Full reversal | Mirrors post |
| **Customer Payment** | Dr Cash, Cr AR | 2 lines |
| **Supplier Payment** | Dr AP, Cr Cash | 2 lines |
| **Fiscal Year Closed** | All 8 methods return `Result.Failure` with Arabic message | Blocked |
| **SystemAccountMappings not found** | All methods return `Result.Failure` | Graceful error |

---

## 7. Migration & Rollback Plan

### 7.1 Database Migration

No new tables are created. Only:
- New seed account `1422` (idempotent — skipped if accounts already seeded)
- New `OpeningBalanceEquityAccountId` column on `SystemAccountMappings` table

**Migration SQL (for reference):**
```sql
-- Add new account
IF NOT EXISTS (SELECT 1 FROM Accounts WHERE AccountCode = '1422')
BEGIN
    DECLARE @parentId INT = (SELECT Id FROM Accounts WHERE AccountCode = '1420');
    INSERT INTO Accounts (AccountCode, NameAr, NameEn, AccountType, Level, ParentAccountId,
        IsSystemAccount, AllowTransactions, ColorCode, Description, Explanation, IsActive, CreatedAt)
    VALUES ('1422', N'أرصدة افتتاحية', 'Opening Balance Equity', 3, 3, @parentId,
        0, 1, '#4CAF50',
        N'رصيد افتتاحي للعملاء والموردين — يتم إقفاله بعد تسوية الأرصدة الافتتاحية',
        N'حساب مؤقت يستخدم لتسجيل الأرصدة الافتتاحية للعملاء والموردين عند بدء استخدام النظام',
        1, GETUTCDATE());
END
GO

-- Add new column to SystemAccountMappings
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SystemAccountMappings') AND name = 'OpeningBalanceEquityAccountId')
BEGIN
    ALTER TABLE SystemAccountMappings
    ADD OpeningBalanceEquityAccountId INT NOT NULL DEFAULT 0;
    
    -- Update existing row with the new account
    UPDATE SystemAccountMappings
    SET OpeningBalanceEquityAccountId = (SELECT Id FROM Accounts WHERE AccountCode = '1422');
    
    -- Add FK constraint
    ALTER TABLE SystemAccountMappings
    ADD CONSTRAINT FK_SystemAccountMappings_OpeningBalanceEquityAccount
    FOREIGN KEY (OpeningBalanceEquityAccountId) REFERENCES Accounts(Id)
    ON DELETE NO ACTION;  -- Restrict
END
GO
```

### 7.2 Rollback

If the accounting integration causes issues:
1. Remove the `IAccountingIntegrationService` injection from each business service
2. Comment out the integration calls (4-5 lines per method)
3. The system continues to work without journal entries (as it currently does)
4. No data loss — existing data integrity is preserved

---

## 8. Summary of All Changes

| # | File | Type | Description |
|---|------|------|-------------|
| 1 | `JournalEntryType.cs` | Enum | Add `CustomerReceipt=10, SupplierPayment=11` |
| 2 | `SystemAccountMappings.cs` | Entity | Add `OpeningBalanceEquityAccountId` field + nav |
| 3 | `SystemAccountMappingsConfiguration.cs` | Config | Map new field + FK with Restrict |
| 4 | `AccountingSeeder.cs` | Seeder | Add account `1422` + update mappings seed |
| 5 | `IAccountingIntegrationService.cs` | NEW | Interface with 8 methods |
| 6 | `AccountingIntegrationService.cs` | NEW | Implementation class |
| 7 | `CustomerService.cs` | Modify | Inject + call after create |
| 8 | `SupplierService.cs` | Modify | Inject + call after create |
| 9 | `SalesService.cs` | Modify | Inject + call in Post/Cancel |
| 10 | `PurchaseService.cs` | Modify | Inject + call in Post/Cancel |
| 11 | `PaymentService.cs` | Modify | Inject + call in both payment creates |

**Total: 11 files affected (2 new, 9 modified)**
