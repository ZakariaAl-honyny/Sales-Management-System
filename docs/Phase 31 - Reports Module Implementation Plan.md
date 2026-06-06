# Phase 31 — Reports Module: Comprehensive Reports Catalog & Implementation Plan

> **Version**: 1.0 — Full rewrite based on analysis files, existing codebase audit, and user requirements
> **Scope**: Complete Reports Module for V1 with ~20 report types, export (Excel/PDF), preview, filtering, column customization

---

## Table of Contents
1. [Reports Architecture — 7 Categories](#1-reports-architecture--7-categories)
2. [Full Inventory — What Already Exists](#2-full-inventory--what-already-exists)
3. [BLOCKER Resolution — Critical Prerequisites](#3-blocker-resolution--critical-prerequisites)
4. [Design Catalog — Report DTO Specifications](#4-design-catalog--report-dto-specifications)
5. [Gap Analysis](#5-gap-analysis)
6. [Architectural Decisions](#6-architectural-decisions)
7. [Non-V1 Reports (Deferred)](#7-non-v1-reports-deferred)
8. [Implementation Tasks](#8-implementation-tasks)
9. [Compliance Matrix (55+ Rules)](#9-compliance-matrix-55-rules)
10. [Risks & Mitigations](#10-risks--mitigations)
11. [Rollback Plan](#11-rollback-plan)

---

## 1. Reports Architecture — 7 Categories

Reports are divided into **7 main categories** based on functional domain:

| # | Category | Example Reports | Target Audience |
|---|----------|----------------|-----------------|
| 1 | **📊 Financial Reports** | Trial Balance, Income Statement, Balance Sheet, Account Statement, Vat Report | Management / Accounting |
| 2 | **📦 Inventory Reports** | Stock Movement, Remaining Stock, Low Stock Alerts, Expired Products, Category Totals | Warehouse / Purchasing |
| 3 | **💰 Sales Reports** | Sales Invoices, Customer Account, Sales Profit, Unpaid Invoices, Sales by Product | Management / Sales |
| 4 | **📥 Purchase Reports** | Purchase Invoices, Supplier Account, Unpaid Invoices, Purchases by Product | Management / Purchasing |
| 5 | **🏦 Cash Reports** | Cashbox Statement, Daily Closure, Cash Movement | Cashier / Accounting |
| 6 | **📋 Transaction Reports** | Daily Operations, Journal Entries, Account Movement, Payments | Accounting |
| 7 | **📈 Profit & Analysis Reports** | Product Profit, Customer Profit, Period Profit, Cash Flow, Aging | Management |

### Report Engine Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Desktop (WPF MVVM)                        │
│  ┌──────────┐  ┌────────────┐  ┌──────────┐  ┌──────────┐  │
│  │ Report    │  │ Report     │  │ Export   │  │ Preview  │  │
│  │ViewModel  │  │Filtering   │  │ Service  │  │ Window   │  │
│  │ (per rep) │  │ Control    │  │(Excel/PDF│  │(WebView) │  │
│  └─────┬─────┘  └────────────┘  └──────────┘  └──────────┘  │
│        │                                                     │
│  ┌─────▼─────────────────────────────────────────────────┐   │
│  │           ReportApiService (HttpClient)                │   │
│  └──────────────────────────┬────────────────────────────┘   │
└─────────────────────────────┼────────────────────────────────┘
                              │ HTTP
┌─────────────────────────────┼────────────────────────────────┐
│  SalesSystem.Api            │                                │
│  ┌──────────────────────────▼────────────────────────────┐   │
│  │               ReportsController                        │   │
│  │  [Route("api/v1/reports")] [Authorize]                 │   │
│  │  Delegates ALL logic to services                       │   │
│  └──────────────────────────┬────────────────────────────┘   │
│                             │                                │
│  ┌──────────────────────────▼────────────────────────────┐   │
│  │              IReportService / ReportService             │   │
│  │  Orchestrates: IReportRepository + IUnitOfWork          │   │
│  │  Returns Result<T> for ALL methods (RULE-006)          │   │
│  └──────────────────────────┬────────────────────────────┘   │
│                             │                                │
│  ┌──────────────────────────▼────────────────────────────┐   │
│  │              IReportRepository / ReportRepository       │   │
│  │  Raw SQL / EF Core queries — NO business logic         │   │
│  └───────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Export Architecture

```
┌──────────────────────────────────────────────────────────┐
│                    Export Service (Desktop)                │
│  ┌─────────────────────┐  ┌─────────────────────────┐    │
│  │  ExcelExportService  │  │  PdfExportService        │    │
│  │  (ClosedXML)         │  │  (QuestPDF)              │    │
│  │  IExcelExportService  │  │  IPdfExportService       │    │
│  └─────────────────────┘  └─────────────────────────┘    │
└──────────────────────────────────────────────────────────┘
```

**Key design decisions:**
- **No business logic in ReportRepository** — pure data retrieval (QUERY layer only)
- **ReportService orchestrates** — coordinates data + domain logic if any (RULE-022)
- **Desktop calls API only** — NO direct DB access from Desktop (RULE-007)
- **Excel export via ClosedXML** — existing and approved (RULE-approved NuGet)
- **PDF export via QuestPDF** — existing and approved (RULE-approved NuGet)
- **All money in decimal(18,2)** — strict type enforcement (RULE-001)
- **All service methods return Result<T>** — never throw (RULE-006)

---

## 2. Full Inventory — What Already Exists

### 2.1 Report DTOs (Contracts) ✅

| DTO | Location | Fields | Status |
|-----|----------|--------|--------|
| `SalesReportDto` | AllDtos.cs:347 | InvoiceDate, Id, CustomerName, SubTotal, DiscountAmount, TaxAmount, TotalAmount, PaidAmount, DueAmount | ✅ Exist |
| `PurchaseReportDto` | AllDtos.cs:359 | InvoiceDate, Id, SupplierName, SubTotal, DiscountAmount, TaxAmount, TotalAmount, PaidAmount, DueAmount | ✅ Exist |
| `StockReportDto` | AllDtos.cs:371 | ProductId, ProductName, CategoryName, UnitName, WarehouseName, CurrentStock, ReorderLevel, PurchasePrice, TotalValue | ✅ Exist |
| `CustomerBalanceReportDto` | AllDtos.cs:383 | CustomerId, CustomerName, OpeningBalance, TotalSales, TotalReturns, TotalPayments, TotalCredit, CurrentBalance | ✅ Exist |
| `SupplierBalanceReportDto` | AllDtos.cs:394 | SupplierId, SupplierName, OpeningBalance, TotalPurchases, TotalReturns, TotalPayments, TotalDebit, CurrentBalance | ✅ Exist |
| `ProductMovementReportDto` | AllDtos.cs:405 | Date, WarehouseName, MovementType, ReferenceNo, QuantityChange, QuantityAfter | ✅ Exist |
| `LowStockReportDto` | AllDtos.cs:436 | ProductId, ProductName, CategoryName, WarehouseName, CurrentRetailQty, ReorderLevelRetailQty, DeficitRetailQty, SuggestedWholesaleBoxes, SuggestedRetailRemainder, WholesaleUnitName, RetailUnitName, ConversionFactor | ✅ Exist |
| `ExpiredProductDto` | AllDtos.cs:427 | ProductId, ProductName, CategoryName, WarehouseName, CurrentStock, ExpirationDate, DaysExpired | ✅ Exist |
| `IncomeStatementDto` | AllDtos.cs:452 | Category, Description, Amount | ✅ Exist |
| `CashFlowReportDto` | AllDtos.cs:461 | OpeningBalance, TotalIncome, TotalExpense, NetCashFlow, ClosingBalance, IncomeItems, ExpenseItems | ✅ Exist |
| `CashFlowItemDto` | AllDtos.cs:457 | Category, Amount | ✅ Exist |
| `VatReportDto` | AllDtos.cs:470 | InvoiceNumber, InvoiceDate, PartyName, TaxableAmount, TaxRate, TaxAmount | ✅ Exist |
| `AccountBalanceDto` | AllDtos.cs:481 | AccountId, AccountCode, AccountNameAr, AccountType, TotalDebit, TotalCredit, Balance, IsDebitNormal | ✅ Exist |
| `DashboardSummaryDto` | AllDtos.cs:~333 | ActiveCustomers, ActiveSuppliers, TotalProducts, TotalReceivables, TotalPayables, TotalSalesMonth, TotalPurchasesMonth | ✅ Exist |

### 2.2 Report Service Layer ✅

| Component | File | Methods | Status |
|-----------|------|---------|--------|
| `IReportService` | Application/Interfaces/Services/IReportService.cs | GetSalesReportAsync, GetPurchasesReportAsync, GetStockReportAsync, GetCustomerBalancesReportAsync, GetSupplierBalancesReportAsync, GetProductMovementsReportAsync, GetLowStockReportAsync, GetDashboardSummaryAsync, GetExpiredProductsReportAsync | ✅ Exist |
| `ReportService` | Application/Services/ReportService.cs | All 9 methods implemented with Result<T> + IUnitOfWork + logging | ✅ Exist |
| `IReportRepository` | Application/Interfaces/Repositories (presumed) | Data query contracts | ✅ Exist |
| `ReportRepository` | Infrastructure/Repositories | SQL query implementations | ✅ Exist |

### 2.3 API Layer ✅

| Component | Endpoints | Status |
|-----------|-----------|--------|
| `ReportsController` | GET `api/v1/reports/sales` | ✅ Exist |
| | GET `api/v1/reports/purchases` | ✅ Exist |
| | GET `api/v1/reports/stock` | ✅ Exist |
| | GET `api/v1/reports/customers` | ✅ Exist |
| | GET `api/v1/reports/suppliers` | ✅ Exist |
| | GET `api/v1/reports/product-movements` | ✅ Exist |
| | GET `api/v1/reports/low-stock` | ✅ Exist |
| | GET `api/v1/reports/expired-products` | ✅ Exist |
| | All require `[Authorize(Policy = "ManagerAndAbove")]` | ✅ Exist |

### 2.4 Desktop API Service ✅

| Component | Methods | Status |
|-----------|---------|--------|
| `IReportApiService` | All 8 report methods | ✅ Exist |
| `ReportApiService` | All 8 HTTP implementations | ✅ Exist |

### 2.5 Desktop ViewModels & Views (Partial) ✅

| ViewModel | View | Status |
|-----------|------|--------|
| `IncomeStatementViewModel` | IncomeStatementView | ✅ Exist |
| `AccountStatementViewModel` | AccountStatementView | ✅ Exist |
| `CashFlowReportViewModel` | CashFlowReportView | ✅ Exist |
| `VatReportViewModel` | VatReportView | ✅ Exist |
| `ExpiredProductsReportViewModel` | ExpiredProductsReportView | ✅ Exist |
| Reports parent page | ReportsView | ✅ Exist (navigation hub) |

### 2.6 Excel Export ✅

| Component | Status |
|-----------|--------|
| ClosedXML NuGet package installed | ✅ |
| `IExcelExportService` interface | ✅ Exist |
| `ExcelExportService` implementation | ✅ Exist |

### 2.7 PDF Export ✅

| Component | Status |
|-----------|--------|
| QuestPDF NuGet package installed | ✅ |
| `IPdfExportService` / `PrintService` | ✅ Exist |
| `A4InvoiceDocument` | ✅ Exist (invoice-specific) |
| `InvoicePrintDtoBuilder` | ✅ Exist |

### 2.8 Existing Print/Export Infrastructure

| Component | File | Status |
|-----------|------|--------|
| `PrintController` | Api/Controllers/PrintController.cs | ✅ All invoice print endpoints |
| `InvoicePrintDtoBuilder` | Application/Printing | ✅ 4 builder overloads |
| `IPrintService` | Application/Printing/IPrintService.cs | ✅ 4 methods |
| `PrintService` | Infrastructure/Printing | ✅ Win32 + QuestPDF |

### 2.9 Summary: What's Missing

| Area | Missing |
|------|---------|
| **DTOs** | TrialBalanceDto, BalanceSheetDto, CustomerAccountDto, SupplierAccountDto, DailyOperationsDto, JournalEntryDto, CashMovementDto, CategoryTotalsDto, ProductProfitDto, CustomerProfitDto, PeriodProfitDto, AgingReportDto, UnpaidInvoicesDto, RemainingStockDto |
| **Services** | IFinancialReportService, profit analysis services, aging services, cash movement |
| **API Endpoints** | ~20+ new endpoints for missing reports |
| **Desktop ViewModels** | ~15+ new ViewModels for missing reports |
| **Desktop Views** | ~15+ new Views for missing reports |
| **Filtering UI** | Universal date range, entity selector, status filter control |
| **Column Customization** | Show/hide, reorder, resize, grouping subsystem |
| **Export Service** | Generic export base for all reports (not just invoices) |
| **Report Preview** | Generic preview screen (existing is invoice-specific) |

---

## 3. BLOCKER Resolution — Critical Prerequisites

### 3.1 Blocker 1: Auto-Journal Entries Needed for Financial Reports (Phase 30)

**Problem**: Financial reports (Trial Balance, Income Statement, Balance Sheet, Cash Flow) depend on journal entries (`JournalEntry` / `JournalEntryLine`). Without auto-generated journal entries from invoice posting, these reports will return empty or incorrect data.

**Root cause**: The accounting module (Phase 30) auto-creates journal entries when invoices are posted. Financial reports query these entries. If Phase 30 is not completed, financial reports show zero balances.

**Dependency**: **Phase 30 (Accounting Foundation) must be complete before financial reports can be tested.**

**Impact**:
- Trial Balance → needs `JournalEntryLine` aggregated by account
- Income Statement → needs revenue/expense accounts from journal entries
- Balance Sheet → needs asset/liability/equity accounts from journal entries
- Cash Flow → needs cash-related journal entries
- Account Statement → needs journal entries per account

**Mitigation**:
1. Design DTOs and service contracts NOW (Phase 31) — they are forward-compatible
2. Implement data queries against `JournalEntryLine` table — when empty, return zero-balance reports
3. Test with seeded journal entries from Phase 30 migration
4. Document: Financial reports return `Result.Success` with empty/zero data when no entries exist

### 3.2 Blocker 2: Chart of Accounts Needed for Trial Balance (Phase 22)

**Problem**: Trial Balance groups and orders accounts by type (Assets, Liabilities, Equity, Revenue, Expenses). Without a seeded `ChartOfAccounts` table (Phase 22), there is no account hierarchy to query.

**Root cause**: Phase 22 seeds the full Chart of Accounts tree with `parent_id` relationships. Trial Balance requires this hierarchy for proper ordering and subtotals.

**Dependency**: **Phase 22 (Chart of Accounts) must be complete before Trial Balance can display correctly.**

**Mitigation**:
1. Trial Balance DTO is designed to accept flat account data
2. Hierarchy grouping (subtotals by category) is done in the service layer, not DB query
3. Without Chart of Accounts, Trial Balance shows accounts in alphabetical order with flat totals
4. Add `HasQueryFilter` to skip inactive accounts in Trial Balance queries

### 3.3 Blocker 3: Multi-Currency Conversion for Reports (Phase 20)

**Problem**: All monetary values in reports must be displayed in the **base currency** (`StoreSettings.CurrencyCode`, default `SAR`). When transactions occur in foreign currencies, the system must convert to base currency using stored exchange rates.

**Root cause**: Analysis Part 2 (lines 343-395) specifies that all accounting operations must convert to base currency. `ExchangeRateToBase` field is stored per currency. Reports must apply this conversion at query time.

**Dependency**: **Phase 20 (Multi-Currency) must provide the `ExchangeRateToBase` data and `ICurrencyConversionService`.**

**Mitigation**:
1. Base currency is read from `StoreSettings.CurrencyCode` (default `SAR`)
2. `ICurrencyConversionService` converts any amount from foreign currency to base currency
3. Report services call conversion BEFORE aggregating — never sum mixed currencies
4. Reports show both original amount (in transaction currency) and base amount
5. If Phase 20 is incomplete, reports assume single-currency (SAR) with no conversion

---

## 4. Design Catalog — Report DTO Specifications

### 4.1 Financial Reports (6 reports)

#### FR-01: Trial Balance (`TrialBalanceDto`)
```csharp
public record TrialBalanceLineDto(
    int AccountId,
    string AccountCode,
    string AccountNameAr,
    string AccountNameEn,
    byte AccountType,          // 1=Asset, 2=Liability, 3=Equity, 4=Revenue, 5=Expense
    decimal TotalDebit,
    decimal TotalCredit,
    decimal Balance,
    bool IsDebitNormal,
    int? ParentAccountId,
    int Level                 // Hierarchy level for grouping
);

public record TrialBalanceDto(
    DateTime AsOfDate,
    string BaseCurrency,
    decimal TotalDebit,
    decimal TotalCredit,
    bool IsBalanced,
    List<TrialBalanceLineDto> Lines
);
```

#### FR-02: Income Statement (`IncomeStatementReportDto`) — Hierarchical with Sections
```csharp
public record IncomeStatementReportDto(
    string Title,                          // "قائمة الدخل"
    DateTime FromDate,
    DateTime ToDate,
    List<IncomeStatementSectionDto> Sections,
    decimal TotalRevenues,
    decimal TotalExpenses,
    decimal NetProfitOrLoss                // TotalRevenues - TotalExpenses
);

public record IncomeStatementSectionDto(
    string SectionName,                    // "الإيرادات" or "المصروفات"
    string SectionType,                    // "Revenue" or "Expense"
    List<AccountBalanceDto> Accounts,      // See §4.9 for AccountBalanceDto
    decimal SectionTotal
);
```

#### FR-03: Balance Sheet (`BalanceSheetReportDto`) — Hierarchical with Sections
```csharp
public record BalanceSheetReportDto(
    string Title,                          // "قائمة المركز المالي"
    DateTime AsOfDate,
    List<BalanceSheetSectionDto> Sections,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal TotalLiabilitiesAndEquity      // Must equal TotalAssets
);

public record BalanceSheetSectionDto(
    string SectionName,                    // "الأصول المتداولة", "الأصول الثابتة", "الخصوم المتداولة", "حقوق الملكية"
    string SectionType,                    // "CurrentAssets", "FixedAssets", "CurrentLiabilities", "Equity"
    List<AccountBalanceDto> Accounts,      // See §4.9 for AccountBalanceDto
    decimal SectionTotal
);
```

#### FR-04: Account Statement (`AccountStatementDto`)
```csharp
public record AccountStatementLineDto(
    DateTime Date,
    string ReferenceNumber,    // Invoice/transaction number
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance
);

public record AccountStatementDto(
    int AccountId,
    string AccountName,
    DateTime FromDate,
    DateTime ToDate,
    decimal OpeningBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance,
    List<AccountStatementLineDto> Lines
);
```

#### FR-05: VAT Report (`VatReportDto` — already exists ✅)
```csharp
// Already exists at AllDtos.cs:470
public record VatReportDto(
    string InvoiceNumber,
    DateTime InvoiceDate,
    string? PartyName,
    decimal TaxableAmount,
    decimal TaxRate,
    decimal TaxAmount
);
```

#### FR-06: Journal Entries Report (`JournalEntryReportDto`)
```csharp
public record JournalEntryLineReportDto(
    int JournalEntryId,
    DateTime EntryDate,
    string? ReferenceNumber,
    string? Description,
    string AccountName,
    decimal Debit,
    decimal Credit
);

public record JournalEntryReportDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal TotalDebit,
    decimal TotalCredit,
    List<JournalEntryLineReportDto> Lines
);
```

### 4.2 Inventory Reports (5 reports)

#### IR-01: Product Movement (`ProductMovementReportDto` — already exists ✅)
```csharp
// Already exists at AllDtos.cs:405
public record ProductMovementReportDto(
    DateTime Date,
    string WarehouseName,
    string MovementType,
    string ReferenceNo,
    decimal QuantityChange,
    decimal QuantityAfter
);
```

#### IR-02: Remaining Stock (`RemainingStockDto`)
```csharp
public record RemainingStockDto(
    int ProductId,
    string ProductName,
    string? CategoryName,
    string? WarehouseName,
    decimal CurrentStock,
    decimal PurchasePrice,
    decimal TotalValue,
    DateTime? LastMovementDate
);
```

#### IR-03: Low Stock Report (`LowStockReportDto` — already exists ✅)
```csharp
// Already exists at AllDtos.cs:436
public record LowStockReportDto(
    int     ProductId,
    string  ProductName,
    string? CategoryName,
    string  WarehouseName,
    decimal CurrentRetailQty,
    decimal ReorderLevelRetailQty,
    // ...
);
```

#### IR-04: Expired Products (`ExpiredProductDto` — already exists ✅)
```csharp
// Already exists at AllDtos.cs:427
public record ExpiredProductDto(
    int ProductId,
    string ProductName,
    string? CategoryName,
    string? WarehouseName,
    decimal CurrentStock,
    DateTime ExpirationDate,
    int DaysExpired
);
```

#### IR-05: Category Totals (`CategoryTotalsDto`)
```csharp
public record CategoryTotalsLineDto(
    int? CategoryId,
    string CategoryName,
    int ProductCount,
    decimal TotalStock,
    decimal TotalStockValue,
    decimal PercentageOfTotal  // Calculated: (TotalStockValue / GrandTotal) * 100
);

public record CategoryTotalsDto(
    decimal GrandTotalStock,
    decimal GrandTotalValue,
    List<CategoryTotalsLineDto> Categories
);
```

### 4.3 Sales Reports (5 reports)

#### SR-01: Sales Report (`SalesReportDto` — already exists ✅)
```csharp
// Already exists at AllDtos.cs:347
public record SalesReportDto(
    DateTime InvoiceDate,
    int Id,
    string CustomerName,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount
);
```

#### SR-02: Customer Account (`CustomerAccountDto`)
```csharp
public record CustomerAccountLineDto(
    DateTime Date,
    string ReferenceType,      // Invoice, Payment, Return
    int ReferenceId,
    decimal Debit,             // Sales + returns increase debit
    decimal Credit,            // Payments decrease debit
    decimal RunningBalance
);

public record CustomerAccountDto(
    int CustomerId,
    string CustomerName,
    DateTime FromDate,
    DateTime ToDate,
    decimal OpeningBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance,
    decimal CreditLimit,
    List<CustomerAccountLineDto> Lines
);
```

#### SR-03: Sales Profit (`SalesProfitDto`)
```csharp
public record SalesProfitLineDto(
    DateTime InvoiceDate,
    int InvoiceNo,
    string CustomerName,
    decimal SalesTotal,
    decimal CostTotal,
    decimal Profit,
    decimal ProfitPercentage
);

public record SalesProfitDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal TotalSales,
    decimal TotalCost,
    decimal TotalProfit,
    decimal AverageProfitPercentage,
    List<SalesProfitLineDto> Lines
);
```

#### SR-04: Unpaid Sales Invoices (`UnpaidSalesDto`)
```csharp
public record UnpaidSalesLineDto(
    int InvoiceNo,
    DateTime InvoiceDate,
    string CustomerName,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount,
    int DaysOverdue
);

public record UnpaidSalesDto(
    decimal GrandTotalDue,
    int TotalUnpaidInvoices,
    List<UnpaidSalesLineDto> Lines
);
```

#### SR-05: Sales by Product (`SalesByProductDto`)
```csharp
public record SalesByProductLineDto(
    int ProductId,
    string ProductName,
    string? CategoryName,
    decimal TotalQuantity,
    decimal TotalSales,
    decimal TotalCost,
    decimal TotalProfit,
    int TransactionCount
);

public record SalesByProductDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal GrandTotalSales,
    List<SalesByProductLineDto> Lines
);
```

### 4.4 Purchase Reports (4 reports)

#### PR-01: Purchase Report (`PurchaseReportDto` — already exists ✅)
```csharp
// Already exists at AllDtos.cs:359
public record PurchaseReportDto(
    DateTime InvoiceDate,
    int Id,
    string SupplierName,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount
);
```

#### PR-02: Supplier Account (`SupplierAccountDto`)
```csharp
public record SupplierAccountLineDto(
    DateTime Date,
    string ReferenceType,
    int ReferenceId,
    decimal Debit,             // Payments decrease credit
    decimal Credit,            // Purchases increase credit
    decimal RunningBalance
);

public record SupplierAccountDto(
    int SupplierId,
    string SupplierName,
    DateTime FromDate,
    DateTime ToDate,
    decimal OpeningBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance,
    List<SupplierAccountLineDto> Lines
);
```

#### PR-03: Unpaid Purchase Invoices (`UnpaidPurchasesDto`)
```csharp
public record UnpaidPurchasesLineDto(
    int InvoiceNo,
    DateTime InvoiceDate,
    string SupplierName,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount,
    int DaysOverdue
);

public record UnpaidPurchasesDto(
    decimal GrandTotalDue,
    int TotalUnpaidInvoices,
    List<UnpaidPurchasesLineDto> Lines
);
```

#### PR-04: Purchases by Product (`PurchasesByProductDto`)
```csharp
public record PurchasesByProductLineDto(
    int ProductId,
    string ProductName,
    string? CategoryName,
    decimal TotalQuantity,
    decimal TotalCost,
    int TransactionCount
);

public record PurchasesByProductDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal GrandTotalCost,
    List<PurchasesByProductLineDto> Lines
);
```

### 4.5 Cash Reports (3 reports)

#### CR-01: Cashbox Statement (`CashboxStatementDto`)
```csharp
public record CashboxStatementLineDto(
    DateTime Date,
    string TransactionType,
    string? ReferenceNumber,
    string? Description,
    decimal Income,
    decimal Expense,
    decimal RunningBalance
);

public record CashboxStatementDto(
    int CashBoxId,
    string CashBoxName,
    DateTime FromDate,
    DateTime ToDate,
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal ClosingBalance,
    List<CashboxStatementLineDto> Lines
);
```

#### CR-02: Cash Movement (`CashMovementDto`)
```csharp
public record CashMovementLineDto(
    DateTime Date,
    string TransactionType,    // SalesIncome, Expense, TransferOut, etc.
    decimal Amount,
    string? ReferenceNumber,
    string? Description
);

public record CashMovementDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal TotalInflows,
    decimal TotalOutflows,
    decimal NetMovement,
    List<CashMovementLineDto> Lines
);
```

#### CR-03: Daily Closure (`DailyClosureDto`)
```csharp
public record DailyClosureDto(
    DateTime Date,
    int CashBoxId,
    string CashBoxName,
    decimal OpeningBalance,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal ClosingBalance,
    decimal ExpectedCash,
    decimal ActualCash,
    decimal Variance,
    bool IsBalanced
);
```

### 4.6 Transaction Reports (2 reports)

#### TR-01: Daily Operations (`DailyOperationsDto`)
```csharp
public record DailyOperationLineDto(
    DateTime Time,
    string OperationType,      // بيع, مشتريات, مرتجع بيع, مرتجع مشتريات, سند قبض, سند صرف
    string ReferenceNumber,
    string? PartyName,
    decimal Amount,
    string UserName
);

public record DailyOperationsDto(
    DateTime Date,
    decimal TotalOperations,
    int OperationCount,
    List<DailyOperationLineDto> Lines
);
```

#### TR-02: Payments Report (`PaymentsReportDto`)
```csharp
public record PaymentLineDto(
    DateTime Date,
    string PaymentType,        // CustomerPayment, SupplierPayment
    string? ReferenceNumber,
    string? PartyName,
    decimal Amount,
    string? PaymentMethod,
    string UserName
);

public record PaymentsReportDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal TotalPayments,
    List<PaymentLineDto> Lines
);
```

### 4.7 Profit & Analysis Reports (5 reports)

#### AR-01: Product Profit (`ProductProfitDto`)
```csharp
public record ProductProfitLineDto(
    int ProductId,
    string ProductName,
    string? CategoryName,
    decimal TotalSold,
    decimal TotalCost,
    decimal TotalRevenue,
    decimal TotalProfit,
    decimal ProfitMarginPercentage,
    int UnitsSold
);

public record ProductProfitDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal GrandTotalProfit,
    List<ProductProfitLineDto> Lines
);
```

#### AR-02: Customer Profit (`CustomerProfitDto`)
```csharp
public record CustomerProfitLineDto(
    int CustomerId,
    string CustomerName,
    decimal TotalSales,
    decimal TotalCost,
    decimal TotalProfit,
    decimal ProfitMarginPercentage,
    int TransactionCount
);

public record CustomerProfitDto(
    DateTime FromDate,
    DateTime ToDate,
    decimal GrandTotalProfit,
    List<CustomerProfitLineDto> Lines
);
```

#### AR-03: Period Profit (`PeriodProfitDto`)
```csharp
public record PeriodProfitLineDto(
    int Year,
    int Month,
    string MonthName,
    decimal TotalRevenue,
    decimal TotalCost,
    decimal TotalExpenses,
    decimal NetProfit,
    decimal NetProfitPercentage
);

public record PeriodProfitDto(
    int FromYear,
    int ToYear,
    decimal GrandTotalProfit,
    List<PeriodProfitLineDto> Lines
);
```

#### AR-04: Customer Aging (`CustomerAgingDto`)
```csharp
public record CustomerAgingLineDto(
    int CustomerId,
    string CustomerName,
    decimal TotalDue,
    decimal Current,            // 0-30 days
    decimal Days31_60,          // 31-60 days
    decimal Days61_90,          // 61-90 days
    decimal Days91Plus,         // 90+ days
    decimal WeightedScore       // Higher = riskier
);

public record CustomerAgingDto(
    DateTime AsOfDate,
    decimal GrandTotalDue,
    decimal TotalCurrent,
    decimal TotalOverdue,
    List<CustomerAgingLineDto> Lines
);
```

#### AR-05: Supplier Aging (`SupplierAgingDto`)
```csharp
public record SupplierAgingLineDto(
    int SupplierId,
    string SupplierName,
    decimal TotalDue,
    decimal Current,
    decimal Days31_60,
    decimal Days61_90,
    decimal Days91Plus
);

public record SupplierAgingDto(
    DateTime AsOfDate,
    decimal GrandTotalDue,
    List<SupplierAgingLineDto> Lines
);
```

### 4.8 Additional DTOs for Infrastructure

#### Report Filtering
```csharp
public record ReportFilterOptions(
    DateTime? FromDate,
    DateTime? ToDate,
    int? EntityId,
    string? EntityType,       // Customer, Supplier, Product, Warehouse
    int? WarehouseId,
    int? CategoryId,
    bool IncludeInactive,
    RowStatus RowStatus       // All (0), ActiveOnly (1), InactiveOnly (2)
);

public enum RowStatus : byte
{
    All = 0,
    ActiveOnly = 1,
    InactiveOnly = 2
}
```

#### Column Customization (Sub-Reporting System)
```csharp
public record ColumnDefinition(
    string Key,
    string DisplayName,        // Arabic column header
    bool IsVisible,
    int Order,
    string DataType,           // "decimal", "string", "date", "int"
    string Format              // "N2", "dd/MM/yyyy", etc.
);

public record ReportColumnConfig(
    string ReportType,         // Unique key for each report
    List<ColumnDefinition> Columns,
    string? GroupByColumn,     // Column key for grouping
    string? SortByColumn,      // Column key for default sort
    bool SortDescending
);
```

#### Export Base
```csharp
public abstract class BaseReportDto
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = "SAR";
}

// Each report DTO has an implicit ReportTitle property
// mapped via convention: "تقرير {ArabicName}"
```

### 4.9 Shared Hierarchy DTOs — AccountBalanceDto with Children

`AccountBalanceDto` is the **universal hierarchy node** used by both Income Statement and Balance Sheet. Each node carries its own balance plus a `Children` list for drill-down by account tree level.

```csharp
public record AccountBalanceDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    int Level,                           // 1=Heading, 2=Group, 3=Account, 4=Sub-Account
    decimal Debit,
    decimal Credit,
    decimal Balance,                     // Debit - Credit (direction depends on account type)
    List<AccountBalanceDto> Children     // Nested children for expand/collapse in UI
);
```

**Data flow — Report Service builds the hierarchy in C#:**

```
Step 1: Query raw account balances from JournalEntryLine (flat rows)
         SELECT coa.Id, coa.AccountCode, coa.AccountName, coa.Level,
                coa.ParentAccountId, SUM(jel.Debit) AS Debit, SUM(jel.Credit) AS Credit
         FROM JournalEntryLines jel
         JOIN ChartOfAccounts coa ON jel.AccountId = coa.Id
         JOIN JournalEntries je ON jel.JournalEntryId = je.Id
         WHERE je.EntryDate BETWEEN @From AND @To AND je.Status = 2
         GROUP BY coa.Id, coa.AccountCode, coa.AccountName, coa.Level, coa.ParentAccountId

Step 2: In C# service — group accounts by Level 1 heading type:
         - Assets (1), Liabilities (2), Equity (3)     → Balance Sheet
         - Revenue (4), Expenses (5)                   → Income Statement

Step 3: Build hierarchy tree:
         For each Level-1 account → create section
         For each Level-2+ account → attach as child recursively
         Compute subtotals bottom-up (children → parent)

Step 4: Compute report-level totals:
         IncomeStatement: TotalRevenues - TotalExpenses = NetProfitOrLoss
         BalanceSheet:    TotalAssets = TotalLiabilities + TotalEquity

Step 5: Return hierarchical DTO — UI renders with TreeView or
        expandable DataGrid rows (expand/collapse per account level)
```

**UI rendering contract:**
- Level 1 accounts (Headings) → bold, section header color, not expandable
- Level 2 accounts (Groups) → semi-bold, expandable, shows group subtotal
- Level 3+ accounts → normal weight, expandable, leaf nodes have no children
- Section total row → background highlight (`#F5F5F5`) with separator line
- Monetary values formatted as `N2` (RULE-001)
- Empty children list → leaf node (no expand arrow in UI)

---

## 5. Gap Analysis

### 5.1 DTOs

| DTO | Status | Action |
|-----|--------|--------|
| `SalesReportDto` | ✅ Exist | No change |
| `PurchaseReportDto` | ✅ Exist | No change |
| `StockReportDto` | ✅ Exist | No change |
| `CustomerBalanceReportDto` | ✅ Exist | No change |
| `SupplierBalanceReportDto` | ✅ Exist | No change |
| `ProductMovementReportDto` | ✅ Exist | No change |
| `LowStockReportDto` | ✅ Exist | No change |
| `ExpiredProductDto` | ✅ Exist | No change |
| `IncomeStatementDto` | ✅ Exist | Already created |
| `CashFlowReportDto` | ✅ Exist | Already created |
| `VatReportDto` | ✅ Exist | Already created |
| `AccountBalanceDto` | ✅ Exist | Already created |
| **`TrialBalanceDto`** | ❌ Missing | Create new |
| **`BalanceSheetDto`** | ❌ Missing | Create new |
| **`AccountStatementDto`** | ❌ Missing | Create new |
| **`RemainingStockDto`** | ❌ Missing | Create new |
| **`CategoryTotalsDto`** | ❌ Missing | Create new |
| **`CustomerAccountDto`** | ❌ Missing | Create new |
| **`SupplierAccountDto`** | ❌ Missing | Create new |
| **`SalesProfitDto`** | ❌ Missing | Create new |
| **`UnpaidSalesDto`** | ❌ Missing | Create new |
| **`UnpaidPurchasesDto`** | ❌ Missing | Create new |
| **`SalesByProductDto`** | ❌ Missing | Create new |
| **`PurchasesByProductDto`** | ❌ Missing | Create new |
| **`CashboxStatementDto`** | ❌ Missing | Create new |
| **`CashMovementDto`** | ❌ Missing | Create new |
| **`DailyClosureDto`** | ❌ Missing | Create new |
| **`DailyOperationsDto`** | ❌ Missing | Create new |
| **`JournalEntryReportDto`** | ❌ Missing | Create new |
| **`PaymentsReportDto`** | ❌ Missing | Create new |
| **`ProductProfitDto`** | ❌ Missing | Create new |
| **`CustomerProfitDto`** | ❌ Missing | Create new |
| **`PeriodProfitDto`** | ❌ Missing | Create new |
| **`CustomerAgingDto`** | ❌ Missing | Create new |
| **`SupplierAgingDto`** | ❌ Missing | Create new |
| **`ReportFilterOptions`** | ❌ Missing | Create new |
| **`ColumnDefinition`** | ❌ Missing | Create new |
| **`ReportColumnConfig`** | ❌ Missing | Create new |

### 5.2 Service Layer

| Service | Status | Action |
|---------|--------|--------|
| `IReportService` / `ReportService` | ✅ Exist | Extend with new methods |
| `IReportRepository` / `ReportRepository` | ✅ Exist | Extend with new queries |
| **`IFinancialReportService`** | ❌ Missing | Create new for financial reports |
| **`IProfitAnalysisService`** | ❌ Missing | Create new for profit/analysis |
| **`IAgingReportService`** | ❌ Missing | Create new for aging |
| **`ICashReportService`** | ❌ Missing | Create new for cash reports |
| **`IExcelExportService`** | ✅ Partial | Extend for generic data (not just invoices) |
| **`IPdfExportService`** | ✅ Partial | Extend for generic report tables |
| **`IReportFilterService`** | ❌ Missing | Create new for filter persistence |
| **`IColumnCustomizationService`** | ❌ Missing | Create new for column config persistence |

### 5.3 API Endpoints

| Endpoint | Status | Action |
|----------|--------|--------|
| `GET /api/v1/reports/sales` | ✅ Exist | No change |
| `GET /api/v1/reports/purchases` | ✅ Exist | No change |
| `GET /api/v1/reports/stock` | ✅ Exist | No change |
| `GET /api/v1/reports/customers` | ✅ Exist | No change |
| `GET /api/v1/reports/suppliers` | ✅ Exist | No change |
| `GET /api/v1/reports/product-movements` | ✅ Exist | No change |
| `GET /api/v1/reports/low-stock` | ✅ Exist | No change |
| `GET /api/v1/reports/expired-products` | ✅ Exist | No change |
| **~20 new endpoints** | ❌ Missing | Create in ReportsController or new FinancialReportsController |

### 5.4 Desktop ViewModels & Views

| Screen | Status | Action |
|--------|--------|--------|
| Reports hub (ReportsView) | ✅ Exist | Add navigation entries |
| IncomeStatement | ✅ Exist | Enhance filtering |
| AccountStatement | ✅ Exist | Existing = financial account statement |
| CashFlowReport | ✅ Exist | Enhance filtering |
| VatReport | ✅ Exist | Enhance filtering |
| ExpiredProducts | ✅ Exist | Enhance filtering |
| **~15 new screens** | ❌ Missing | Create ViewModel + View pairs |

### 5.5 Export

| Feature | Status | Action |
|---------|--------|--------|
| Excel export (invoice) | ✅ Exist | Extend for all reports |
| Excel export (generic data table) | ❌ Missing | Create generic `IEnumerable` → Excel |
| PDF export (invoice A4) | ✅ Exist | Extend for all reports |
| PDF export (generic report table) | ❌ Missing | Create generic table PDF |
| Report preview screen | ❌ Missing | Create generic preview window |
| Column customization | ❌ Missing | Full sub-system (Phase 20 pattern) |

---

## 6. Architectural Decisions

### 6.1 Report Engine: Service-Layer Pattern (NOT CQRS/MediatR)

All report queries go through **dedicated service interfaces** with `Result<T>` return types.

**Decision**: Keep Service-Layer pattern for all new report services. Do NOT introduce CQRS/MediatR (RULE-147).

**Structure**:
- `IFinancialReportService` / `FinancialReportService` — TrialBalance, IncomeStatement, BalanceSheet, AccountStatement, JournalEntries
- `IInventoryReportService` / `InventoryReportService` — RemainingStock, CategoryTotals (existing movement/low-stock/expired stay on IReportService)
- `ISalesReportService` / `SalesReportService` — CustomerAccount, SalesProfit, UnpaidSales, SalesByProduct (existing sales/purchases stay on IReportService)
- `IPurchaseReportService` / `PurchaseReportService` — SupplierAccount, UnpaidPurchases, PurchasesByProduct
- `ICashReportService` / `CashReportService` — CashboxStatement, CashMovement, DailyClosure
- `ITransactionReportService` / `TransactionReportService` — DailyOperations, PaymentsReport
- `IProfitAnalysisService` / `ProfitAnalysisService` — ProductProfit, CustomerProfit, PeriodProfit
- `IAgingReportService` / `AgingReportService` — CustomerAging, SupplierAging
- `IReportColumnService` / `ReportColumnService` — Column customization persistence
- `IReportExportService` / `ReportExportService` — Excel/PDF generic export

### 6.2 Base Currency Conversion

All monetary values in ALL reports are displayed in the **base currency** (from `StoreSettings.CurrencyCode`, default `"SAR"`).

**Decision** (from Analysis Part 2 + Part 5):
1. Base currency is read once per report generation
2. `ICurrencyConversionService.ConvertToBase(amount, fromCurrency, date)` is called for each foreign-currency transaction
3. If Phase 20 is incomplete, assume single-currency with `ExchangeRateToBase = 1`
4. Reports show a header: `"العملة الأساسية: {CurrencyCode}"`
5. Conversion is done in the **service layer** — not in DB query, not in UI

### 6.3 Result Pattern for ALL Queries

All report service methods return `Result<T>` or `Result<IEnumerable<T>>` (RULE-006).

**Success**: `Result<IEnumerable<Dto>>.Success(data)` — data may be empty list
**Failure**: `Result<IEnumerable<Dto>>.Failure("الرسالة بالعربية", ErrorCode)` — for actual errors
**Never throw**: No exceptions from service layer (RULE-202)

### 6.4 Generic Export Service Architecture

**Decision**: Create a unified `IReportExportService` that handles both Excel and PDF for any report type.

```csharp
public interface IReportExportService
{
    // Generic export: accepts any tabular data with column metadata
    Task<Result> ExportToExcelAsync<T>(
        string reportTitle,
        List<T> data,
        List<ColumnDefinition> columns,
        Stream outputStream,
        CancellationToken ct = default);

    Task<Result> ExportToPdfAsync<T>(
        string reportTitle,
        List<T> data,
        List<ColumnDefinition> columns,
        Stream outputStream,
        CancellationToken ct = default);

    // Preview: returns HTML for WebView
    Task<Result<string>> GetPreviewHtmlAsync<T>(
        string reportTitle,
        List<T> data,
        List<ColumnDefinition> columns,
        CancellationToken ct = default);
}
```

**Why generic**: Avoids creating 20 separate export methods — one generic method handles all report types via reflection + column definitions.

### 6.5 Column Customization (Sub-Reporting System)

**Decision**: Column customization (show/hide, reorder, resize, grouping) is stored per-report-per-user in `SystemSettings` or a new `ReportColumnConfig` table.

**Storage** (JSON in `ReportColumnConfig` table or `SystemSettings` with key `ReportColumnConfig.{ReportType}`):
```json
{
  "columns": [
    {"key": "InvoiceDate", "displayName": "التاريخ", "isVisible": true, "order": 1},
    {"key": "InvoiceNo", "displayName": "رقم الفاتورة", "isVisible": true, "order": 2}
  ],
  "groupByColumn": "CustomerName",
  "sortByColumn": "InvoiceDate",
  "sortDescending": true
}
```

**Desktop Implementation**:
- Context menu on DataGrid column header: "إخفاء العمود", "إظهار الأعمدة"
- Drag to reorder columns (built-in WPF DataGrid support)
- Grouping panel above DataGrid (drag column header to group)
- Resize columns (built-in WPF DataGrid support)
- Persist settings per user via API

### 6.6 Report Filtering Strategy

All reports share a **common filter control** at the top of each report screen:

| Filter | Applies To | Control |
|--------|-----------|---------|
| Date From / To | All time-based reports | DatePicker (2 fields) |
| Entity selection | Account-specific reports | ComboBox (Customer/Supplier/Product/Account) |
| Warehouse | Stock/Sales/Purchase reports | ComboBox (Warehouse) |
| Category | Inventory reports | ComboBox (Category) |
| Status | Invoice-based reports | ComboBox (All/Posted/Draft/Cancelled) |
| Include Inactive | Entity reports | CheckBox |

**Decision**: Filters are built into each report ViewModel, not a shared filter component (avoids complex binding). Each ViewModel has its own `FromDate`, `ToDate`, `SelectedEntityId`, etc. properties.

### 6.7 Report Preview Screen

**Decision**: Create a single `ReportPreviewWindow` (WPF) that displays report data in a DataGrid with export buttons.

```csharp
// ReportPreviewWindow.xaml hosts:
// - DataGrid bound to report results
// - Toolbar: Export to Excel, Export to PDF, Print, Column Settings
// - Filter bar at top (collapsible)
```

Opened via `ScreenWindowService.OpenScreen()` (RULE-160) — non-modal.

### 6.8 Newest-First Default Sort

All report lists default to **newest-first** ordering (RULE-220):
- Invoice-based reports: sort by `InvoiceDate` descending
- Movement reports: sort by `Date` descending
- Balance reports: sort by `CurrentBalance` descending (highest balance first)
- Aging reports: sort by `TotalDue` descending (most overdue first)

### 6.9 Money/Quantity Rules in Reports

- ALL monetary values: `decimal(18,2)` in DTOs, formatted as `N2` in UI (RULE-001)
- ALL quantity values: `decimal(18,3)` in DTOs, formatted as `N3` in UI (RULE-002)
- Percentage values: `decimal(18,2)` with % sign in UI
- Running totals: computed in service layer, not UI

---

## 7. Non-V1 Reports (Deferred)

These reports appeared in analysis files but are **deferred** to future versions:

| Report | Reason |
|--------|--------|
| **Currency Exchange Differences** (فوارق أسعار العملات) | Requires Phase 20 multi-currency + actual cross-rate fluctuations |
| **Working Capital** (رأس المال العامل) | Requires complete Balance Sheet data + current assets/liabilities breakdown |
| **Products by Expiry (detailed countdown)** | Base expired products report exists; enhanced expiry dashboard deferred |
| **Dashboard Widgets** (real-time charts) | Requires SignalR or polling — architectural decision deferred |
| **Scheduled Email Reports** | Requires SMTP config + email template + background job |
| **Custom Report Builder** (drag-drop query builder) | Significant UI investment — requires human approval |
| **Comparative Reports** (period-over-period) | Requires data warehousing pattern — deferred |
| **Budget vs Actual** | Requires Budget entity and tracking — large new feature |

---

## 8. Implementation Tasks

All tasks include:
- Logging via Serilog (RULE-035/036) — `Log.Information`, `Log.Warning`, `LogSystemError()`
- Arabic ToolTips on ALL interactive controls (RULE-185-190)
- UI Compact styles (RULE-262-274) — no hardcoded heights/padding
- Result<T> for ALL service methods (RULE-006)
- Error handling via LogSystemError() (RULE-199)
- Newest-first sorting (RULE-220)

### Task 1 — Report Framework (Base Classes, Filtering, Export Base)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `BaseReportDto`, `ReportFilterOptions`, `RowStatus`, `ColumnDefinition`, `ReportColumnConfig` |
| `Contracts/DTOs/AllDtos.cs` | Add `ReportExportRequest` DTO (reportType, filters, columnConfig, format) |
| `Application/Interfaces/Services/IReportExportService.cs` | New interface: `ExportToExcelAsync<T>`, `ExportToPdfAsync<T>`, `GetPreviewHtmlAsync<T>` |
| `Application/Services/ReportExportService.cs` | Generic export implementation using ClosedXML + QuestPDF |
| `Application/Interfaces/Services/IReportColumnService.cs` | New interface: Get/Save column config per user |
| `Application/Services/ReportColumnService.cs` | Persist column config in `ReportColumnConfig` table |

**Generic export method signature**:
```csharp
public async Task<Result> ExportToExcelAsync<T>(
    string reportTitle,
    List<T> data,
    List<ColumnDefinition> columns,
    Stream outputStream,
    CancellationToken ct)
{
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add(reportTitle);
    // Map ColumnDefinition headers + data via reflection
    // Apply formatting (N2 for money, N3 for quantity, dd/MM/yyyy for dates)
    // Auto-fit columns
    workbook.SaveAs(outputStream);
    return Result.Success();
}
```

**Estimate**: ~4 hours

---

### Task 2 — Stock Reports (Movement, Remaining, Min Stock, Category Totals)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `RemainingStockDto`, `CategoryTotalsDto`, `CategoryTotalsLineDto` |
| `Application/Interfaces/Services/IInventoryReportService.cs` | New interface: `GetRemainingStockAsync`, `GetCategoryTotalsAsync` |
| `Application/Services/InventoryReportService.cs` | Implement with IUnitOfWork + IReportRepository |
| `Application/Interfaces/Repositories/IReportRepository.cs` | Add `GetRemainingStockAsync`, `GetCategoryTotalsAsync` |
| `Infrastructure/Repositories/ReportRepository.cs` | Implement SQL queries |
| `Api/Controllers/ReportsController.cs` | Add endpoints: `GET stock/remaining`, `GET stock/category-totals` |
| `Desktop/Services/Api/IReportApiService.cs` | Add `GetRemainingStockAsync`, `GetCategoryTotalsAsync` |
| `Desktop/Services/Api/ReportApiService.cs` | Implement HTTP calls |
| `Desktop/ViewModels/Reports/RemainingStockViewModel.cs` | New ViewModel |
| `Desktop/Views/Reports/RemainingStockView.xaml` | New View + code-behind |
| `Desktop/ViewModels/Reports/CategoryTotalsViewModel.cs` | New ViewModel |
| `Desktop/Views/Reports/CategoryTotalsView.xaml` | New View + code-behind |
| `Desktop/ViewModels/Reports/LowStockReportViewModel.cs` | Enhance with filtering |
| `Desktop/ViewModels/Reports/ExpiredProductsReportViewModel.cs` | Enhance with filtering |

**RemainingStock query**:
```sql
SELECT 
    p.Id AS ProductId, p.Name AS ProductName,
    c.Name AS CategoryName, w.Name AS WarehouseName,
    ws.Quantity AS CurrentStock,
    p.PurchasePrice,
    (ws.Quantity * p.PurchasePrice) AS TotalValue,
    (SELECT MAX(im.CreatedAt) FROM InventoryMovements im WHERE im.ProductId = p.Id) AS LastMovementDate
FROM WarehouseStocks ws
JOIN Products p ON ws.ProductId = p.Id
LEFT JOIN Categories c ON p.CategoryId = c.Id
JOIN Warehouses w ON ws.WarehouseId = w.Id
WHERE ws.Quantity > 0
ORDER BY p.Name;
```

**CategoryTotals query**:
```sql
SELECT 
    ISNULL(c.Id, 0) AS CategoryId,
    ISNULL(c.Name, 'بدون تصنيف') AS CategoryName,
    COUNT(DISTINCT p.Id) AS ProductCount,
    SUM(ws.Quantity) AS TotalStock,
    SUM(ws.Quantity * p.PurchasePrice) AS TotalStockValue
FROM WarehouseStocks ws
JOIN Products p ON ws.ProductId = p.Id
LEFT JOIN Categories c ON p.CategoryId = c.Id
GROUP BY c.Id, c.Name
ORDER BY TotalStockValue DESC;
```

**Logging** (RULE-035):
- `Log.Information("Generating remaining stock report — {Count} products found", count)`
- `Log.Information("Generating category totals report — {Count} categories found", count)`

**Estimate**: ~6 hours

---

### Task 3 — Sales Reports (Customer Account, Sales Profit, Unpaid Invoices, Sales by Product)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `CustomerAccountDto`, `CustomerAccountLineDto`, `SalesProfitDto`, `SalesProfitLineDto`, `UnpaidSalesDto`, `UnpaidSalesLineDto`, `SalesByProductDto`, `SalesByProductLineDto` |
| `Application/Interfaces/Services/ISalesReportService.cs` | New interface: 4 methods |
| `Application/Services/SalesReportService.cs` | Implement with IUnitOfWork |
| `Application/Interfaces/Repositories/ISalesReportRepository.cs` | New repository interface |
| `Infrastructure/Repositories/SalesReportRepository.cs` | SQL queries |
| `Api/Controllers/SalesReportsController.cs` | New controller: `/api/v1/reports/sales/customer-account`, `/api/v1/reports/sales/profit`, `/api/v1/reports/sales/unpaid`, `/api/v1/reports/sales/by-product` |
| `Desktop/Services/Api/ISalesReportApiService.cs` | New interface |
| `Desktop/Services/Api/SalesReportApiService.cs` | HTTP implementation |
| `Desktop/ViewModels/Reports/CustomerAccountViewModel.cs` | New |
| `Desktop/Views/Reports/CustomerAccountView.xaml` | New |
| `Desktop/ViewModels/Reports/SalesProfitViewModel.cs` | New |
| `Desktop/Views/Reports/SalesProfitView.xaml` | New |
| `Desktop/ViewModels/Reports/UnpaidSalesViewModel.cs` | New |
| `Desktop/Views/Reports/UnpaidSalesView.xaml` | New |
| `Desktop/ViewModels/Reports/SalesByProductViewModel.cs` | New |
| `Desktop/Views/Reports/SalesByProductView.xaml` | New |

**Customer Account query pattern**:
```sql
-- Opening balance: sum of all invoice dues BEFORE fromDate
-- Then: UNION of sales invoices + payments + returns ordered by date
-- Running balance computed in C# service layer
SELECT Date, 'فاتورة بيع' AS ReferenceType, Id AS ReferenceId,
    TotalAmount AS Debit, 0 AS Credit
FROM SalesInvoices
WHERE CustomerId = @CustomerId AND InvoiceDate BETWEEN @From AND @To AND Status = 2

UNION ALL

SELECT Date, 'سند قبض' AS ReferenceType, Id,
    0 AS Debit, Amount AS Credit
FROM CustomerPayments
WHERE CustomerId = @CustomerId AND PaymentDate BETWEEN @From AND @To

ORDER BY Date;
```

**Unpaid Sales query**:
```sql
SELECT 
    si.InvoiceNo,
    si.InvoiceDate,
    c.Name AS CustomerName,
    si.TotalAmount,
    si.PaidAmount,
    si.DueAmount,
    DATEDIFF(DAY, si.InvoiceDate, GETUTCDATE()) AS DaysOverdue
FROM SalesInvoices si
JOIN Customers c ON si.CustomerId = c.Id
WHERE si.DueAmount > 0 AND si.Status = 2
ORDER BY DaysOverdue DESC;
```

**Estimate**: ~10 hours

---

### Task 4 — Purchase Reports (Supplier Account, Unpaid Invoices, Purchases by Product)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `SupplierAccountDto`, `SupplierAccountLineDto`, `UnpaidPurchasesDto`, `UnpaidPurchasesLineDto`, `PurchasesByProductDto`, `PurchasesByProductLineDto` |
| `Application/Interfaces/Services/IPurchaseReportService.cs` | New interface: 3 methods |
| `Application/Services/PurchaseReportService.cs` | Implement |
| `Api/Controllers/PurchaseReportsController.cs` | New controller |
| `Desktop` | 3 new ViewModel + View pairs |

**Estimate**: ~8 hours

---

### Task 5 — Financial Reports (Trial Balance, Income Statement, Balance Sheet)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `TrialBalanceDto`, `TrialBalanceLineDto`, `BalanceSheetReportDto`, `BalanceSheetSectionDto`, `IncomeStatementReportDto`, `IncomeStatementSectionDto`, `AccountBalanceDto` (hierarchical with Children list, replaces flat IS/BS DTOs) |
| `Application/Interfaces/Services/IFinancialReportService.cs` | New interface: `GetTrialBalanceAsync`, `GetIncomeStatementAsync`, `GetBalanceSheetAsync`, `GetAccountStatementAsync` |
| `Application/Services/FinancialReportService.cs` | Implement queries against JournalEntryLine + ChartOfAccounts |
| `Application/Interfaces/Repositories/IFinancialReportRepository.cs` | New repository interface |
| `Infrastructure/Repositories/FinancialReportRepository.cs` | SQL queries: group by account, compute balances |
| `Api/Controllers/FinancialReportsController.cs` | New controller |
| `Desktop/Services/Api/IFinancialReportApiService.cs` | New |
| `Desktop/Services/Api/FinancialReportApiService.cs` | HTTP implementation |
| `Desktop/ViewModels/Reports/TrialBalanceViewModel.cs` | New |
| `Desktop/Views/Reports/TrialBalanceView.xaml` | New |
| `Desktop/ViewModels/Reports/BalanceSheetViewModel.cs` | New |
| `Desktop/Views/Reports/BalanceSheetView.xaml` | New |
| `Desktop/ViewModels/Reports/IncomeStatementViewModel.cs` | Enhance existing |
| `Desktop/ViewModels/Reports/AccountStatementViewModel.cs` | Enhance existing |

**Trial Balance query pattern** (against JournalEntryLine):
```sql
SELECT 
    coa.Id AS AccountId,
    coa.AccountCode,
    coa.AccountNameAr,
    coa.AccountType,
    SUM(jel.Debit) AS TotalDebit,
    SUM(jel.Credit) AS TotalCredit,
    CASE WHEN coa.IsDebitNormal 
        THEN SUM(jel.Debit) - SUM(jel.Credit)
        ELSE SUM(jel.Credit) - SUM(jel.Debit)
    END AS Balance,
    coa.IsDebitNormal,
    coa.ParentAccountId,
    coa.Level
FROM JournalEntryLines jel
JOIN ChartOfAccounts coa ON jel.AccountId = coa.Id
JOIN JournalEntries je ON jel.JournalEntryId = je.Id
WHERE je.EntryDate <= @AsOfDate
    AND je.Status = 2  -- Posted
GROUP BY coa.Id, coa.AccountCode, coa.AccountNameAr, 
         coa.AccountType, coa.IsDebitNormal, 
         coa.ParentAccountId, coa.Level
ORDER BY coa.Level, coa.AccountCode;
```

**Income Statement hierarchy logic** (in `FinancialReportService`):
```csharp
public async Task<Result<IncomeStatementReportDto>> GetIncomeStatementAsync(
    DateTime fromDate, DateTime toDate, CancellationToken ct)
{
    // Step 1: Fetch all account balances from JournalEntryLine
    var flatBalances = await _financialRepo.GetAccountBalancesAsync(
        fromDate, toDate, ct);
    if (!flatBalances.IsSuccess || flatBalances.Value == null)
        return Result<IncomeStatementReportDto>.Failure("فشل في تحميل أرصدة الحسابات");

    // Step 2: Filter revenue accounts (AccountType = 4) and expense accounts (AccountType = 5)
    var revenueAccounts = flatBalances.Value.Where(a => a.AccountType == 4).ToList();
    var expenseAccounts = flatBalances.Value.Where(a => a.AccountType == 5).ToList();

    // Step 3: Build hierarchy tree (Level 1 → groups → leaf accounts)
    var revenueTree = BuildAccountTree(revenueAccounts);
    var expenseTree = BuildAccountTree(expenseAccounts);

    // Step 4: Compute section totals bottom-up
    var revenueSection = new IncomeStatementSectionDto(
        "الإيرادات", "Revenue", revenueTree, revenueTree.Sum(n => ComputeNodeTotal(n)));
    var expenseSection = new IncomeStatementSectionDto(
        "المصروفات", "Expense", expenseTree, expenseTree.Sum(n => ComputeNodeTotal(n)));

    // Step 5: Compute Net Profit/Loss
    var netProfitOrLoss = revenueSection.SectionTotal - expenseSection.SectionTotal;

    return Result<IncomeStatementReportDto>.Success(new IncomeStatementReportDto(
        "قائمة الدخل", fromDate, toDate,
        new List<IncomeStatementSectionDto> { revenueSection, expenseSection },
        revenueSection.SectionTotal, expenseSection.SectionTotal, netProfitOrLoss));
}

// Recursive helper: builds parent–child tree from flat account list
private List<AccountBalanceDto> BuildAccountTree(List<...> flatAccounts)
{
    var lookup = flatAccounts.ToLookup(a => a.ParentAccountId);
    return BuildLevel(lookup, null);  // null parent = Level 1
}

private List<AccountBalanceDto> BuildLevel(ILookup<int?, ...> lookup, int? parentId)
{
    return lookup[parentId].Select(a => new AccountBalanceDto(
        a.Id, a.Code, a.Name, a.Level, a.TotalDebit, a.TotalCredit, a.Balance,
        BuildLevel(lookup, a.Id)  // Recursively attach children
    )).ToList();
}

private decimal ComputeNodeTotal(AccountBalanceDto node)
{
    var childrenTotal = node.Children.Sum(ComputeNodeTotal);
    return node.Balance + childrenTotal;
}
```

**Balance Sheet hierarchy logic** — same pattern but groups by:
- AccountType = 1 (Assets) → sections: CurrentAssets, FixedAssets, OtherAssets
- AccountType = 2 (Liabilities) → sections: CurrentLiabilities, LongTermLiabilities
- AccountType = 3 (Equity) → section: Equity

**Validation**: `TotalAssets` must equal `TotalLiabilitiesAndEquity` — log warning if imbalance detected.

**⚠️ Depends on**: Phase 30 (JournalEntry + JournalEntryLine tables must exist with data).

**Estimate**: ~10 hours

---

### Task 6 — Cash Reports (Cashbox Statement, Closure Reports, Cash Movement)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `CashboxStatementDto`, `CashboxStatementLineDto`, `CashMovementDto`, `CashMovementLineDto`, `DailyClosureDto` |
| `Application/Interfaces/Services/ICashReportService.cs` | New |
| `Application/Services/CashReportService.cs` | Implement |
| `Api/Controllers/CashReportsController.cs` | New |
| `Desktop` | 3 new VM + View pairs |

**Estimate**: ~6 hours

---

### Task 7 — Transaction Reports (Daily Operations, Payments, Account Movement)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `DailyOperationsDto`, `DailyOperationLineDto`, `PaymentsReportDto`, `PaymentLineDto` |
| `Application/Interfaces/Services/ITransactionReportService.cs` | New |
| `Application/Services/TransactionReportService.cs` | Implement |
| `Api/Controllers/TransactionReportsController.cs` | New |
| `Desktop` | 2 new VM + View pairs |

**Estimate**: ~5 hours

---

### Task 8 — Profit Analysis Reports (Per-Product, Per-Customer, Period Profit)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `ProductProfitDto`, `ProductProfitLineDto`, `CustomerProfitDto`, `CustomerProfitLineDto`, `PeriodProfitDto`, `PeriodProfitLineDto` |
| `Application/Interfaces/Services/IProfitAnalysisService.cs` | New |
| `Application/Services/ProfitAnalysisService.cs` | Implement (cost from InvoiceItems + SalesInvoiceItems) |
| `Api/Controllers/ProfitAnalysisController.cs` | New |
| `Desktop` | 3 new VM + View pairs |

**Product Profit formula** (in service):
```csharp
// For each sold item:
var revenue = item.Price * item.Quantity;
var cost = item.UnitCost * item.Quantity;  // From weighted average cost
var profit = revenue - cost;
var margin = revenue > 0 ? (profit / revenue) * 100 : 0;
```

**Estimate**: ~6 hours

---

### Task 9 — Aging Reports (Customer Aging, Supplier Aging)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Add `CustomerAgingDto`, `CustomerAgingLineDto`, `SupplierAgingDto`, `SupplierAgingLineDto` |
| `Application/Interfaces/Services/IAgingReportService.cs` | New |
| `Application/Services/AgingReportService.cs` | Implement aging bucket calculation |
| `Api/Controllers/AgingController.cs` | New |
| `Desktop` | 2 new VM + View pairs |

**Aging bucket computation** (in C# service):
```csharp
var daysOverdue = (DateTime.UtcNow.Date - invoice.DueDate.GetValueOrDefault(invoice.InvoiceDate)).Days;
var agingLine = new CustomerAgingLineDto
{
    CustomerId = customer.Id,
    CustomerName = customer.Name,
    TotalDue = totalDue,
    Current = daysOverdue <= 30 ? totalDue : 0,
    Days31_60 = daysOverdue is > 30 and <= 60 ? totalDue : 0,
    Days61_90 = daysOverdue is > 60 and <= 90 ? totalDue : 0,
    Days91Plus = daysOverdue > 90 ? totalDue : 0,
    WeightedScore = totalDue * (daysOverdue > 90 ? 0.4m : daysOverdue > 60 ? 0.3m : daysOverdue > 30 ? 0.2m : 0.1m)
};
```

**Estimate**: ~4 hours

---

### Task 10 — Cash Flow Report (Enhance Existing)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Enhance `CashFlowReportDto` with operating/investing/financing categories if needed |
| `Application/Services/ReportService.cs` | Enhance `GetCashFlowReportAsync` with cash transaction data |
| `Application/Interfaces/Repositories/IReportRepository.cs` | Add cash flow query method |
| `Infrastructure/Repositories/ReportRepository.cs` | Implement cash flow query |
| `Desktop/ViewModels/Reports/CashFlowReportViewModel.cs` | Enhance with date range, category filtering |
| `Desktop/Views/Reports/CashFlowReportView.xaml` | Enhance filtering |

**Estimate**: ~3 hours

---

### Task 11 — Report Preview Screen (WPF)

**Files**:

| File | Change |
|------|--------|
| `Desktop/Views/Reports/ReportPreviewWindow.xaml` | New generic preview window |
| `Desktop/Views/Reports/ReportPreviewWindow.xaml.cs` | Code-behind: loads data, shows toolbar |
| `Desktop/ViewModels/Reports/ReportPreviewViewModel.cs` | New ViewModel: holds report data, column config, export commands |

**Preview Window layout**:
```
┌─────────────────────────────────────────────────────┐
│ Header: [Report Title]  [Base Currency: SAR]         │
├─────────────────────────────────────────────────────┤
│ Toolbar: [🔍 تصفية] [📥 Excel] [📄 PDF] [🖨️ طباعة]  │
│          [⚙️ أعمدة] [🔃 تحديث]                        │
├─────────────────────────────────────────────────────┤
│ Filter Panel (collapsible):                         │
│  [من تاريخ] [إلى تاريخ] [العميل/المورد] [المستودع]    │
├─────────────────────────────────────────────────────┤
│                                                      │
│  DataGrid — scrollable, sortable, resizable          │
│  Column headers with context menu (إخفاء/إظهار)       │
│                                                      │
├─────────────────────────────────────────────────────┤
│ Footer: [الإجمالي: xxx]  [عدد السجلات: xx]           │
└─────────────────────────────────────────────────────┘
```

**ViewModel pattern** (RULE-141):
```csharp
public class ReportPreviewViewModel : ViewModelBase, IDisposable
{
    public ICommand LoadReportCommand { get; }
    public ICommand ExportToExcelCommand { get; }
    public ICommand ExportToPdfCommand { get; }
    public ICommand PrintCommand { get; }
    public ICommand ConfigureColumnsCommand { get; }

    // All async commands wrapped in ExecuteAsync()
    // NO manual try/catch — ExecuteAsync handles errors + IsBusy
}
```

**ToolTips** (RULE-185-190):
- Excel button: `"تصدير التقرير إلى ملف Excel"`
- PDF button: `"تصدير التقرير إلى ملف PDF"`
- Print button: `"طباعة التقرير"`
- Columns button: `"تخصيص الأعمدة — إظهار، إخفاء، إعادة ترتيب"`
- Filter toggle: `"إظهار/إخفاء لوحة التصفية"`
- Refresh: `"تحديث بيانات التقرير"`

**Estimate**: ~6 hours

---

### Task 12 — Excel Export for All Reports

**Files**:

| File | Change |
|------|--------|
| `Application/Services/ReportExportService.cs` | Implement `ExportToExcelAsync<T>` using ClosedXML |
| `Desktop/ViewModels/Reports/ReportPreviewViewModel.cs` | Wire ExportToExcelCommand to call export service |

**Excel generation pattern**:
```csharp
public async Task<Result> ExportToExcelAsync<T>(
    string reportTitle, List<T> data, List<ColumnDefinition> columns,
    Stream outputStream, CancellationToken ct)
{
    using var workbook = new XLWorkbook();
    var ws = workbook.Worksheets.Add(reportTitle.Length > 31 ? reportTitle[..31] : reportTitle);

    // Row 1: Title (merged, bold, 14pt)
    ws.Cell(1, 1).Value = reportTitle;
    ws.Range(1, 1, 1, columns.Count).Merge().Style.Font.Bold = true.FontSize = 14;

    // Row 2: Headers from ColumnDefinition
    for (int i = 0; i < columns.Count; i++)
    {
        ws.Cell(2, i + 1).Value = columns[i].DisplayName;
        ws.Cell(2, i + 1).Style.Font.Bold = true;
        ws.Cell(2, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
    }

    // Row 3+: Data via reflection
    var props = typeof(T).GetProperties();
    int row = 3;
    foreach (var item in data)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            var prop = props.FirstOrDefault(p => p.Name == columns[i].Key);
            if (prop != null)
            {
                var value = prop.GetValue(item);
                var cell = ws.Cell(row, i + 1);
                
                if (value is decimal dec)
                    cell.Value = dec;
                else if (value is DateTime dt)
                    cell.Value = dt;
                else
                    cell.Value = value?.ToString() ?? "";

                // Apply format
                if (!string.IsNullOrEmpty(columns[i].Format))
                    cell.Style.NumberFormat.Format = columns[i].Format;
            }
        }
        row++;
    }

    // Totals row
    ws.Range(row, 1, row, columns.Count).Style.Font.Bold = true;

    // Auto-fit
    ws.Columns().AdjustToContents();

    workbook.SaveAs(outputStream);
    return Result.Success();
}
```

**Estimate**: ~3 hours

---

### Task 13 — PDF Export for All Reports

**Files**:

| File | Change |
|------|--------|
| `Application/Services/ReportExportService.cs` | Implement `ExportToPdfAsync<T>` using QuestPDF |
| `Application/Interfaces/Services/IReportExportService.cs` | Add preview HTML generation |
| `Desktop/ViewModels/Reports/ReportPreviewViewModel.cs` | Wire ExportToPdfCommand |

**PDF generation pattern** (QuestPDF):
```csharp
Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(20);
        page.Header().Element(c => ComposeHeader(c, reportTitle));
        page.Content().Element(c => ComposeContent(c, data, columns));
        page.Footer().AlignCenter().Text(text =>
        {
            text.Span("الصفحة ");
            text.CurrentPageNumber();
        });
    });
}).GeneratePdf(outputStream);
```

**Estimate**: ~3 hours

---

### Task 14 — Column Customization (Show/Hide, Reorder, Resize, Grouping)

**Files**:

| File | Change |
|------|--------|
| `Contracts/DTOs/AllDtos.cs` | Already has `ColumnDefinition`, `ReportColumnConfig` |
| `Application/Services/ReportColumnService.cs` | Persist/retrieve column config per report + user |
| `Api/Controllers/ColumnConfigController.cs` | New: CRUD for column config |
| `Desktop/ViewModels/Reports/ColumnConfigViewModel.cs` | Dialog ViewModel for column configuration |
| `Desktop/Views/Reports/ColumnConfigView.xaml` | Dialog: checkbox list + drag reorder + grouping selector |
| `Desktop/Services/Api/IColumnConfigApiService.cs` | HTTP client for column config |

**Column Config Dialog**:
```
┌──────────────────────────────────────────────┐
│  تخصيص الأعمدة — تقرير المبيعات               │
├──────────────────────────────────────────────┤
│  ☑ التاريخ       [▲▼]                        │
│  ☑ رقم الفاتورة  [▲▼]                        │
│  ☑ العميل        [▲▼]                        │
│  ☑ الإجمالي      [▲▼]                        │
│  ☐ الخصم         [▲▼]                        │
│  ☐ الضريبة       [▲▼]                        │
│  ☑ المدفوع       [▲▼]                        │
│  ☑ المتبقي       [▲▼]                        │
├──────────────────────────────────────────────┤
│  التجميع حسب: [▼ العميل ]                     │
│  الترتيب: [▼ التاريخ ] [تنازلي ☑]             │
├──────────────────────────────────────────────┤
│  [إعادة تعيين]                  [💾 حفظ]      │
└──────────────────────────────────────────────┘
```

**Estimate**: ~6 hours

---

### Task 15 — Report Filtering UI (Date Range, Entity, Status)

**Files**:

| File | Change |
|------|--------|
| `Desktop/Controls/ReportFilterControl.xaml` | New reusable filter user control |
| `Desktop/Controls/ReportFilterControl.xaml.cs` | Code-behind with dependency properties |
| `Desktop/ViewModels/Reports/ReportFilterViewModel.cs` | New base ViewModel for filter state |

**FilterControl design**:
```xml
<!-- Reusable filter strip for all report screens -->
<StackPanel Orientation="Horizontal" Margin="0,0,0,8">
    <!-- Date From -->
    <TextBlock Text="من تاريخ:" VerticalAlignment="Center" Margin="0,0,4,0"/>
    <DatePicker SelectedDate="{Binding FromDate}" Width="120" Margin="0,0,8,0"
                ToolTip="تاريخ بداية التقرير"/>
    
    <!-- Date To -->
    <TextBlock Text="إلى تاريخ:" VerticalAlignment="Center" Margin="0,0,4,0"/>
    <DatePicker SelectedDate="{Binding ToDate}" Width="120" Margin="0,0,8,0"
                ToolTip="تاريخ نهاية التقرير"/>
    
    <!-- Entity selector -->
    <TextBlock Text="العميل:" VerticalAlignment="Center" Margin="0,0,4,0"/>
    <ComboBox ItemsSource="{Binding Customers}" DisplayMemberPath="Name"
              SelectedValue="{Binding SelectedCustomerId}" SelectedValuePath="Id"
              Width="150" Margin="0,0,8,0"
              ToolTip="اختيار عميل لعرض تقريره"/>
    
    <!-- Generate button -->
    <Button Content="🔄 عرض" Command="{Binding GenerateReportCommand}"
            Style="{StaticResource PrimaryButton}"
            ToolTip="توليد وعرض التقرير حسب الفلاتر المحددة"/>
</StackPanel>
```

**Each report ViewModel** includes its own filter properties:
```csharp
public class SalesProfitViewModel : ViewModelBase
{
    // Filter properties
    public DateTime FromDate { get; set; } = DateTime.Today.AddMonths(-1);
    public DateTime ToDate { get; set; } = DateTime.Today;
    public int? CustomerId { get; set; }
    public int? WarehouseId { get; set; }
    
    // Filter collections
    public ObservableCollection<CustomerDto> Customers { get; set; }
    public ObservableCollection<WarehouseDto> Warehouses { get; set; }
    
    // Generate command
    public ICommand GenerateReportCommand { get; }
}
```

**Estimate**: ~4 hours (shared across all report screens)

---

### Task 16 — Report Navigation in Desktop

**Files**:

| File | Change |
|------|--------|
| `Desktop/Views/Reports/ReportsView.xaml` | Add navigation tree/list for all 7 report categories |
| `Desktop/ViewModels/Reports/ReportsViewModel.cs` | Manage category selection, load report screens |
| `Desktop/App.xaml.cs` | DI registrations for all new ViewModels and Views |

**Navigation structure** (in ReportsView):
```
📊 التقارير المالية
  ├── ميزان المراجعة
  ├── قائمة الدخل
  ├── المركز المالي
  ├── كشف حساب
  └── تقرير ضريبة القيمة المضافة

📦 تقارير المخزون
  ├── حركة الأصناف
  ├── المخزون المتبقي
  ├── المخزون المنخفض (تنبيهات)
  ├── المنتجات المنتهية
  └── إجمالي التصنيفات

💰 تقارير المبيعات
  ├── فواتير البيع
  ├── كشف حساب عميل
  ├── أرباح المبيعات
  ├── فواتير غير مدفوعة
  └── المبيعات حسب الصنف

📥 تقارير المشتريات
  ├── فواتير المشتريات
  ├── كشف حساب مورد
  ├── فواتير غير مدفوعة
  └── المشتريات حسب الصنف

🏦 تقارير الصندوق
  ├── كشف حساب صندوق
  ├── حركة الصندوق
  └── إغلاق يومي

📋 تقارير المعاملات
  ├── العمليات اليومية
  ├── القيود اليومية
  └── المدفوعات

📈 تقارير الأرباح والتحليل
  ├── أرباح الأصناف
  ├── أرباح العملاء
  ├── أرباح الفترة
  ├── تقارير التقادم (عملاء)
  ├── تقارير التقادم (موردين)
  └── التدفق النقدي
```

**Estimate**: ~3 hours

---

### Task 17 — API Endpoints for All Reports

**Files**:

| File | Change |
|------|--------|
| `Api/Controllers/FinancialReportsController.cs` | New: 5 endpoints |
| `Api/Controllers/InventoryReportsController.cs` | New: 2 endpoints |
| `Api/Controllers/SalesReportsController.cs` | New: 4 endpoints |
| `Api/Controllers/PurchaseReportsController.cs` | New: 3 endpoints |
| `Api/Controllers/CashReportsController.cs` | New: 3 endpoints |
| `Api/Controllers/TransactionReportsController.cs` | New: 2 endpoints |
| `Api/Controllers/ProfitAnalysisController.cs` | New: 3 endpoints |
| `Api/Controllers/AgingController.cs` | New: 2 endpoints |
| `Api/Controllers/ReportsController.cs` | Enhance existing: add stock/remaining, stock/category-totals |
| `Api/Controllers/FinancialReportsController.cs` | Also add ColumnConfig endpoints |

**All controllers inject service interfaces only** (RULE-203) — NO `DbContext` or `IUnitOfWork`.

**All endpoints** require `[Authorize(Policy = "ManagerAndAbove")]` or `[Authorize(Policy = "AdminOnly")]` per permissions matrix (RULE-038).

**Endpoint summary** (new + existing):

| # | Method | Endpoint | Policy | Task |
|---|--------|----------|--------|------|
| 1 | GET | `/api/v1/reports/sales` | ManagerAndAbove | ✅ Existing |
| 2 | GET | `/api/v1/reports/purchases` | ManagerAndAbove | ✅ Existing |
| 3 | GET | `/api/v1/reports/stock` | ManagerAndAbove | ✅ Existing |
| 4 | GET | `/api/v1/reports/customers` | ManagerAndAbove | ✅ Existing |
| 5 | GET | `/api/v1/reports/suppliers` | ManagerAndAbove | ✅ Existing |
| 6 | GET | `/api/v1/reports/product-movements` | ManagerAndAbove | ✅ Existing |
| 7 | GET | `/api/v1/reports/low-stock` | ManagerAndAbove | ✅ Existing |
| 8 | GET | `/api/v1/reports/expired-products` | ManagerAndAbove | ✅ Existing |
| 9 | GET | `/api/v1/reports/stock/remaining` | ManagerAndAbove | Task 2 |
| 10 | GET | `/api/v1/reports/stock/category-totals` | ManagerAndAbove | Task 2 |
| 11 | GET | `/api/v1/reports/financial/trial-balance` | ManagerAndAbove | Task 5 |
| 12 | GET | `/api/v1/reports/financial/income-statement` | ManagerAndAbove | Task 5 |
| 13 | GET | `/api/v1/reports/financial/balance-sheet` | ManagerAndAbove | Task 5 |
| 14 | GET | `/api/v1/reports/financial/account-statement/{accountId}` | ManagerAndAbove | Task 5 |
| 15 | GET | `/api/v1/reports/financial/journal-entries` | ManagerAndAbove | Task 5 |
| 16 | GET | `/api/v1/reports/sales/customer-account/{customerId}` | AllStaff | Task 3 |
| 17 | GET | `/api/v1/reports/sales/profit` | ManagerAndAbove | Task 3 |
| 18 | GET | `/api/v1/reports/sales/unpaid` | ManagerAndAbove | Task 3 |
| 19 | GET | `/api/v1/reports/sales/by-product` | ManagerAndAbove | Task 3 |
| 20 | GET | `/api/v1/reports/purchases/supplier-account/{supplierId}` | ManagerAndAbove | Task 4 |
| 21 | GET | `/api/v1/reports/purchases/unpaid` | ManagerAndAbove | Task 4 |
| 22 | GET | `/api/v1/reports/purchases/by-product` | ManagerAndAbove | Task 4 |
| 23 | GET | `/api/v1/reports/cash/statement/{cashBoxId}` | AllStaff | Task 6 |
| 24 | GET | `/api/v1/reports/cash/movement` | ManagerAndAbove | Task 6 |
| 25 | GET | `/api/v1/reports/cash/closure/{cashBoxId}` | ManagerAndAbove | Task 6 |
| 26 | GET | `/api/v1/reports/transactions/daily-operations` | ManagerAndAbove | Task 7 |
| 27 | GET | `/api/v1/reports/transactions/payments` | ManagerAndAbove | Task 7 |
| 28 | GET | `/api/v1/reports/profit/product` | ManagerAndAbove | Task 8 |
| 29 | GET | `/api/v1/reports/profit/customer` | ManagerAndAbove | Task 8 |
| 30 | GET | `/api/v1/reports/profit/period` | AdminOnly | Task 8 |
| 31 | GET | `/api/v1/reports/aging/customers` | ManagerAndAbove | Task 9 |
| 32 | GET | `/api/v1/reports/aging/suppliers` | ManagerAndAbove | Task 9 |
| 33 | GET | `/api/v1/reports/cash-flow` | ManagerAndAbove | Task 10 (enhance) |
| 34 | GET | `/api/v1/reports/export/excel` | ManagerAndAbove | Task 12 |
| 35 | GET | `/api/v1/reports/export/pdf` | ManagerAndAbove | Task 13 |
| 36 | GET | `/api/v1/reports/columns/{reportType}` | AllStaff | Task 14 |
| 37 | PUT | `/api/v1/reports/columns/{reportType}` | AllStaff | Task 14 |

**FluentValidation** (RULE-044) for request DTOs:
- `ReportFilterRequestValidator`: FromDate ≤ ToDate, valid EntityId if provided, valid enum values
- `ColumnConfigValidator`: Column keys match report DTO properties, Order must be ≥ 1

**Estimate**: ~6 hours

---

### Implementation Summary Table

| Task | Description | Est. Hours | Dependencies |
|------|-------------|-----------|--------------|
| 1 | Report Framework (base, export, columns) | 4 | None |
| 2 | Stock Reports (remaining, category totals) | 6 | Task 1 |
| 3 | Sales Reports (customer acct, profit, unpaid, by-product) | 10 | Task 1 |
| 4 | Purchase Reports (supplier acct, unpaid, by-product) | 8 | Task 1 |
| 5 | Financial Reports (TB, IS, BS, acct statement) | 10 | Phase 30, Phase 22, Task 1 |
| 6 | Cash Reports (statement, movement, closure) | 6 | Task 1 |
| 7 | Transaction Reports (daily ops, payments) | 5 | Task 1 |
| 8 | Profit Analysis (product, customer, period) | 6 | Task 1 |
| 9 | Aging Reports (customer, supplier) | 4 | Task 1 |
| 10 | Cash Flow Report (enhance existing) | 3 | Task 1 |
| 11 | Report Preview Screen (WPF) | 6 | Task 1 |
| 12 | Excel Export for all Reports | 3 | Task 1 |
| 13 | PDF Export for all Reports | 3 | Task 1 |
| 14 | Column Customization (show/hide, reorder, grouping) | 6 | Task 11 |
| 15 | Report Filtering UI | 4 | Task 1 |
| 16 | Report Navigation in Desktop | 3 | Tasks 2-10 |
| 17 | API Endpoints for all reports | 6 | Tasks 2-10 |
| | **Total** | **93** | |

---

## 9. Compliance Matrix (55+ Rules)

| Rule | Directive | Where Applied | Verdict |
|------|-----------|---------------|---------|
| **RULE-001** | `decimal(18,2)` for ALL money | All monetary fields in all new DTOs | ✅ |
| **RULE-002** | `decimal(18,3)` for ALL quantities | All quantity fields (RemainingStock.CurrentStock, ProductProfit.UnitsSold) | ✅ |
| **RULE-003** | Multi-table ops in transaction | ReportService queries are read-only — no write transaction needed | ✅ N/A |
| **RULE-006** | ALL services return `Result<T>` | ALL new report services: IFinancialReportService, ISalesReportService, etc. | ✅ |
| **RULE-007** | Desktop calls API via HttpClient | ReportApiService, SalesReportApiService, etc. — NO direct DB | ✅ |
| **RULE-008** | ALL text columns `nvarchar` | All report DTO string fields | ✅ |
| **RULE-016** | BaseEntity audit fields | All report DTOs carry GeneratedAt, GeneratedBy | ✅ |
| **RULE-022** | Controllers delegate to services | All new controllers inject service interfaces only | ✅ |
| **RULE-024** | Services use `IUnitOfWork` | All new report services inject IUnitOfWork | ✅ |
| **RULE-035** | Serilog for logging | All services: Log.Information on report generation | ✅ |
| **RULE-036** | Log critical operations | Report generation, export, column config changes | ✅ |
| **RULE-037** | NEVER log passwords/conn strings | Reports never touch credentials | ✅ |
| **RULE-038** | ALL endpoints `[Authorize]` | All new endpoints require auth; financial reports = ManagerAndAbove | ✅ |
| **RULE-044** | FluentValidation on every Command | ReportFilterRequestValidator, ColumnConfigValidator | ✅ |
| **RULE-050** | DeleteStrategy for all deletes | Column config deletion uses DeleteStrategy | ✅ |
| **RULE-052** | Guard Clauses on all entities | No new entities in this phase (DTOs only) | ✅ N/A |
| **RULE-054** | IDialogService — no MessageBox | All new ViewModels use IDialogService | ✅ |
| **RULE-055** | NEVER raw MessageBox.Show | Verified across all new ViewModels | ✅ |
| **RULE-058** | INotifyDataErrorInfo | All editor ViewModels (ColumnConfigViewModel) | ✅ |
| **RULE-059** | Save always enabled, validate on click | All report editor VMs | ✅ |
| **RULE-141** | ExecuteAsync() wrapper for all VMs | ALL new ViewModels use ExecuteAsync() | ✅ |
| **RULE-147** | NO MediatR / CQRS | Service Layer pattern everywhere | ✅ |
| **RULE-160** | ScreenWindowService for non-modal windows | ReportPreviewWindow opens via OpenScreen() | ✅ |
| **RULE-171** | NO ex.Message in user dialogs | All catch blocks use LogSystemError() | ✅ |
| **RULE-172** | HandleFailure() transforms errors | ViewModelBase pattern in all new VMs | ✅ |
| **RULE-173** | Screen-specific dialog titles | `"خطأ في تحميل التقرير"`, `"خطأ في تصدير Excel"` | ✅ |
| **RULE-174** | NO MessageBox.Show — use IDialogService | All new VMs verified | ✅ |
| **RULE-175** | All dialog calls use Async suffix | `ShowErrorAsync`, `ShowSuccessAsync` | ✅ |
| **RULE-182** | Log.Error for system errors only | DB failures, API unreachable, export crashes | ✅ |
| **RULE-183** | Log.Warning for user mistakes | Invalid date range, no data found, parse fallbacks | ✅ |
| **RULE-184** | HandleResponseAsync checks ContentType | All new API services check ContentType before JSON parse | ✅ |
| **RULE-185** | Arabic ToolTips on ALL interactive controls | All buttons, inputs across all new XAML views | ✅ |
| **RULE-186** | ToolTips describe action (not repeat text) | "تصدير التقرير إلى Excel", NOT "Excel" | ✅ |
| **RULE-187** | Action buttons explain consequences | "🔄 عرض التقرير حسب الفلاتر المحددة" | ✅ |
| **RULE-188** | Navigation MenuItems describe destination | Navigation entries describe report content | ✅ |
| **RULE-189** | Empty-state buttons have ToolTips | "➕ توليد أول تقرير — حدد الفلاتر ثم اضغط عرض" | ✅ |
| **RULE-190** | Error dismiss buttons have ToolTips | "إخفاء رسالة الخطأ" | ✅ |
| **RULE-191** | No Code column on Product/Customer/Supplier | All report DTOs use Id + Name, never Code | ✅ |
| **RULE-199** | LogSystemError() only for system errors | All ViewModels use LogSystemError() — never direct Serilog.Log.Error | ✅ |
| **RULE-200** | Hard-delete catch DbUpdateException | Column config permanent delete catches FK exceptions | ✅ |
| **RULE-201** | All catch blocks use LogSystemError() | All ViewModel catch blocks | ✅ |
| **RULE-202** | ALL Service methods return Result<T> | All report services return Result<T> | ✅ |
| **RULE-203** | Controllers NO DbContext/IUnitOfWork | All controllers inject services only | ✅ |
| **RULE-220** | Newest-first sorting on lists | All report lists: OrderByDescending(InvoiceDate/Id) | ✅ |
| **RULE-227** | SetDialogService() in EVERY Editor VM | ColumnConfigViewModel constructor | ✅ |
| **RULE-228** | INotifyDataErrorInfo (NO HasXxxError booleans) | All editor VMs | ✅ |
| **RULE-229** | ClearAllErrors() + AddError() + ValidateAllAsync() | Pre-save validation in all editor VMs | ✅ |
| **RULE-254** | InvoiceNo as int, NOT string | All report DTOs use `int InvoiceNo` (RULE-254) | ✅ |
| **RULE-255** | InvoiceNo default = lastId + 1 | SalesReportDto already uses int InvoiceNo | ✅ |
| **RULE-262** | No hardcoded Height="36" on buttons/inputs | All new XAML: compact 28px via styles | ✅ |
| **RULE-263** | No hardcoded Padding="16+" on buttons | All new XAML: 10,4 via styles | ✅ |
| **RULE-264** | Header padding 12,6 / Footer 12,8 max | All new report XAML views | ✅ |
| **RULE-265** | Section margins 0,0,0,6 max | Between form fields in filters | ✅ |
| **RULE-266** | Dialog titles FontSize=16 max | All report dialog windows | ✅ |
| **RULE-267** | Section headers FontSize=14 max | All report section headers | ✅ |
| **RULE-268** | Empty-state buttons: Margin=0,12,0,0 Width=140 | All empty-state report views | ✅ |
| **RULE-270** | Dialog icons: 44×44 max | Dialog windows for reports | ✅ |
| **RULE-271** | ScreenWindow MinWidth=500, MinHeight=350 | ReportPreviewWindow | ✅ |
| **RULE-272** | Dialog buttons: MinWidth (80-100), not fixed width | All report dialogs | ✅ |
| **RULE-273** | Remove hardcoded Height/Padding duplicates | All new XAML uses styles only | ✅ |

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Phase 30 (Accounting) not ready** | **HIGH** — Trial Balance, Income Statement, Balance Sheet return empty/zero | Design DTOs and services NOW; they return zero data gracefully; block only affects 3 financial reports |
| **Phase 22 (Chart of Accounts) not ready** | **HIGH** — Trial Balance has no hierarchy | Flat account listing without grouping — still usable |
| **Phase 20 (Multi-Currency) not ready** | Medium — foreign currency amounts not converted | Assume single-currency mode; add conversion later |
| **Report performance with large data** | Medium — slow queries on millions of invoices | Add pagination (skip/take) to query endpoints; set max rows (10000 default) with warning |
| **Excel/PDF export memory overhead** | Medium — large datasets crash export | Stream output; chunking for >5000 rows; add export timeout |
| **Column config conflicts** | Low — two users changing same config | Per-user column config key: `ReportColumnConfig.{ReportType}.{UserId}` |
| **Generic export fails with complex types** | Low — nested objects not supported in generic export | Restrict report DTOs to flat structures (primitives only) |
| **Arabic text alignment in QuestPDF** | Low — RTL layout issues | Use `Direction.RightToLeft` on table elements per existing A4InvoiceDocument pattern |
| **New migration conflicts with existing DB** | Low | Always nullable, additive columns — no breaking changes |
| **Data inconsistency between reports** | Low — different services query different snapshots | All reports read from same IUnitOfWork instance per request |

---

## 11. Rollback Plan

| Scenario | Action |
|----------|--------|
| Report service breaks existing functionality | Revert ReportService.cs changes; keep existing 9 methods unchanged |
| New financial reports cause EF migration conflicts | `DROP TABLE IF EXISTS ReportColumnConfig;` remove migration |
| Excel export causes ClosedXML version conflicts | ClosedXML already installed — no package change needed |
| PDF export causes QuestPDF license issues | QuestPDF Community license — no change needed |
| Column config table not needed | `DELETE FROM ReportColumnConfig; DROP TABLE ReportColumnConfig;` — no data loss |
| Report preview screen has layout issues | Remove navigation entry — reports still accessible via existing ReportsView |
| Specific report has data issues | Disable that report's navigation entry until fixed — other reports unaffected |
| Export performance is unacceptable | Add server-side pagination; limit max rows; add progress dialog |
| All new features need rollback | 1. Revert migration: remove ReportColumnConfig table<br>2. Remove new controller files<br>3. Remove new VM + View files<br>4. Revert ReportService.cs to original<br>5. Remove new service registrations from DI |

---

### Task 18 — Unit Tests

**Scope**: Comprehensive test coverage for all Phase 31 components. Every test category below must be implemented.

#### 18.1 Report DTO Hierarchy Tests

**File**: `Tests/Contracts.Tests/DTOs/ReportDtoTests.cs`

**IncomeStatementReportDto structure tests:**
| Test | Expected |
|------|----------|
| DTO has Title, FromDate, ToDate, Sections list | All properties present |
| DTO has TotalRevenues, TotalExpenses, NetProfitOrLoss | All computed fields present |
| Sections list holds IncomeStatementSectionDto items | Generic type constraint satisfied |
| NetProfitOrLoss = TotalRevenues - TotalExpenses | Computed correctly |

**IncomeStatementSectionDto tests:**
| Test | Expected |
|------|----------|
| SectionName property string | e.g., "الإيرادات" or "المصروفات" |
| SectionType property string | `"Revenue"` or `"Expense"` |
| Accounts property is List<AccountBalanceDto> | Holds nested account list |
| SectionTotal property decimal | Holds computed subtotal |

**BalanceSheetReportDto structure tests:**
| Test | Expected |
|------|----------|
| DTO has Title, AsOfDate, Sections list | All properties |
| DTO has TotalAssets, TotalLiabilities, TotalEquity, TotalLiabilitiesAndEquity | All computed fields |
| TotalLiabilitiesAndEquity == TotalAssets | Balance identity preserved |

**BalanceSheetSectionDto tests:**
| Test | Expected |
|------|----------|
| SectionType values: "CurrentAssets", "FixedAssets", "CurrentLiabilities", "LongTermLiabilities", "Equity" | All 5 section types defined |
| Accounts is List<AccountBalanceDto> | Nested hierarchy |
| SectionTotal holds subtotal | Computed correctly |

**AccountBalanceDto hierarchy tests:**
| Test | Expected |
|------|----------|
| Children property is List<AccountBalanceDto> | Recursive nesting |
| Balance = Debit - Credit (for debit-normal accounts) | Computed correctly |
| Balance = Credit - Debit (for credit-normal accounts) | Computed correctly |
| Level property (1=Heading, 2=Group, 3=Account, 4=Sub-Account) | Valid level range 1-4 |
| Empty Children list = leaf node | Count == 0 |

#### 18.2 Report Data Flow Tests

**File**: `Tests/Application.Tests/Reports/FinancialReportServiceTests.cs`
**File**: `Tests/Application.Tests/Reports/ReportDataFlowTests.cs`

**5-step data flow verification:**
| Step | Test | Expected |
|------|------|----------|
| 1 | Flat SQL query returns rows | Raw AccountBalanceDto list from repository |
| 2 | C# tree built from flat list | BuildAccountTree produces nested hierarchy |
| 3 | Subtotals computed bottom-up | Children totals propagate to parent nodes |
| 4 | Report-level totals computed | TotalRevenues = sum of revenue section accounts |
| 5 | UI-ready DTO returned | Hierarchical structure preserved in final DTO |

**BuildAccountTree recursive helper tests:**
| Test | Expected |
|------|----------|
| Empty flat list → empty tree | Returns empty List<AccountBalanceDto> |
| Single Level-1 account with 2 children | Parent has 2 children in Children list |
| 3-level hierarchy: 1 heading → 2 groups → 4 leaf accounts | Tree depth = 3 |
| Orphan accounts (parentId not found) → attached to root | Placed at Level 1 |
| Circular parent reference → prevented | Stack overflow guard |

**Report totals match sum of section totals:**
| Test | Expected |
|------|----------|
| IncomeStatement: TotalRevenues == RevenueSection.SectionTotal | Matches |
| IncomeStatement: TotalExpenses == ExpenseSection.SectionTotal | Matches |
| BalanceSheet: TotalAssets == sum of Assets sections | Matches |
| BalanceSheet: TotalLiabilities + TotalEquity == TotalLiabilitiesAndEquity | Matches |
| BalanceSheet: TotalAssets == TotalLiabilitiesAndEquity | Balance identity verified |

#### 18.3 Service Tests (using Mock<IUnitOfWork>)

**File**: `Tests/Application.Tests/Reports/`
- `FinancialReportServiceTests.cs`
- `SalesReportServiceTests.cs`
- `PurchaseReportServiceTests.cs`
- `InventoryReportServiceTests.cs`
- `CashReportServiceTests.cs`
- `TransactionReportServiceTests.cs`
- `ProfitAnalysisServiceTests.cs`
- `AgingReportServiceTests.cs`

**Common service tests (each service):**
| Test | Expected |
|------|----------|
| Valid filters → returns Result<T>.Success with data | Data list populated |
| No data matching filters → returns Result<T>.Success with empty list | Empty list, not failure |
| Repository throws exception → Result<T>.Failure | `"فشل في تحميل التقرير"` |
| Invalid date range (From > To) → Result<T>.Failure | `"تاريخ البداية يجب أن يكون قبل تاريخ النهاية"` |

**FinancialReportService specific tests:**
| Test | Expected |
|------|----------|
| GetTrialBalanceAsync with posted entries → balanced result | TotalDebit == TotalCredit |
| GetTrialBalanceAsync with no entries → zero totals | All totals zero |
| GetIncomeStatementAsync with revenue > expenses → positive NetProfit | NetProfitOrLoss > 0 |
| GetIncomeStatementAsync with expenses > revenue → negative NetProfit | NetProfitOrLoss < 0 |
| GetBalanceSheetAsync → TotalAssets == TotalLiabilitiesAndEquity | Balance identity verified |
| GetBalanceSheetAsync with zero entries → all totals zero | Zero report |

**SalesReportService specific tests:**
| Test | Expected |
|------|----------|
| GetCustomerAccountAsync → Debit from sales, Credit from payments | Correct running balance |
| GetSalesProfitAsync → Profit = SalesTotal - CostTotal | Computed correctly |
| GetUnpaidSalesAsync → DaysOverdue computed from InvoiceDate | Positive integer |
| GetSalesByProductAsync → aggregated by ProductId | Grouping correct |

**AgingReportService specific tests:**
| Test | Expected |
|------|----------|
| DaysOverdue <= 30 → Current bucket | Correct bucket assignment |
| DaysOverdue 31-60 → Days31_60 bucket | Correct bucket assignment |
| DaysOverdue 61-90 → Days61_90 bucket | Correct bucket assignment |
| DaysOverdue > 90 → Days91Plus bucket | Correct bucket assignment |
| WeightedScore calculation | Higher overdue = higher score |

#### 18.4 FluentValidation Tests

**File**: `Tests/Api.Tests/Validators/Reports/`
- `ReportFilterRequestValidatorTests.cs`
- `ColumnConfigValidatorTests.cs`

| Test | Expected |
|------|----------|
| Valid report filter request passes | IsValid = true |
| FromDate > ToDate → error | Arabic message |
| Invalid entity ID (0) → error | `"معرف الكيان غير صالح"` |
| Null dates → passes (optional filter) | IsValid = true |
| Valid ColumnConfig passes | IsValid = true |
| Column Order < 1 → error | Arabic message |
| Column key does not match any DTO property → warning | Validation warning |
| Empty ColumnDefinition list → passes | IsValid = true (no columns = show all) |

#### 18.5 Excel Export Tests (ClosedXML)

**File**: `Tests/Desktop.Tests/Services/ExcelExportServiceTests.cs`
**File**: `Tests/Infrastructure.Tests/Printing/ReportExportServiceTests.cs`

| Test | Expected |
|------|----------|
| ExportToExcelAsync with 10 rows → file created | Stream length > 0 |
| ExportToExcelAsync with 0 rows → file created (headers only) | Stream has headers |
| ExportToExcelAsync with null data → Result.Failure | `"لا توجد بيانات للتصدير"` |
| Excel file contains correct column headers | Matches ColumnDefinition.DisplayName |
| Excel file contains correct data types | Money→N2, Quantity→N3, Date→dd/MM/yyyy |
| Column formatting applied | NumberFormat set per column |
| Merged title row at top | Row 1 merged across all columns |
| Totals row at bottom | Last row bolded |
| ExportToExcelAsync with large dataset (5000+ rows) | Streams without OOM |
| Export file deleted after stream disposal | File not locked |
| Arabic column headers rendered correctly | UTF-8 encoded in Excel |

#### 18.6 PDF Export Tests (QuestPDF)

**File**: `Tests/Infrastructure.Tests/Printing/ReportExportServicePdfTests.cs`

| Test | Expected |
|------|----------|
| ExportToPdfAsync with 10 rows → document generated | PDF stream length > 0 |
| ExportToPdfAsync with 0 rows → empty document generated | PDF has headers and "لا توجد بيانات" message |
| ExportToPdfAsync with null data → Result.Failure | `"لا توجد بيانات للتصدير"` |
| PDF contains A4 page size | Page size set correctly |
| PDF header contains report title | Text "تقرير" found in document |
| PDF footer contains page numbers | Page number text present |
| Arabic text rendered correctly (RTL) | Document validates with RTL setting |
| ExportToPdfAsync with large dataset (500 rows) | Generated without OOM |

#### 18.7 Database Configuration Tests

**File**: `Tests/Infrastructure.Tests/Data/Configurations/ReportColumnConfigConfigurationTests.cs`

| Test | Expected |
|------|----------|
| ReportColumnConfig → table "ReportColumnConfig" | ToTable correct |
| ReportType (string) MaxLength(100), IsRequired | HasMaxLength + IsRequired |
| Columns config stored as JSON string | nvarchar(max) column |
| UserId FK → Users with Restrict | OnDelete(DeleteBehavior.Restrict) |
| Unique index on (ReportType, UserId) | Composite unique index |

#### 18.8 Desktop ViewModel Tests

**File**: `Tests/Desktop.Tests/ViewModels/Reports/`
- `ReportPreviewViewModelTests.cs`
- `FinancialReportsViewModelTests.cs` (TrialBalance, IncomeStatement, BalanceSheet)
- `SalesReportsViewModelTests.cs`
- `InventoryReportsViewModelTests.cs`
- `ColumnConfigViewModelTests.cs`

| Test | Expected |
|------|----------|
| LoadReport operation succeeds → Data populated | ObservableCollection has data |
| LoadReport with API failure → ErrorMessage set | `"فشل في تحميل التقرير"` |
| ExportToExcel triggers API call | Verify service.ExportToExcelAsync called |
| ExportToPdf triggers API call | Verify service.ExportToPdfAsync called |
| Filter change triggers auto-reload | LoadReportCommand executes |
| FromDate > ToDate filter → validation warning | `"تاريخ البداية يجب أن يكون قبل تاريخ النهاية"` |
| Column config save success → toast notification | `"تم حفظ إعدادات الأعمدة"` |
| Column config save failure → error dialog | `"فشل في حفظ إعدادات الأعمدة"` |
| Empty state shown when data empty | Empty-state UI visible |
| SetDialogService called in constructor | Verified |
| Dispose unsubscribes all | EventBus subscription disposed |
| Newest-first sorting applied | OrderByDescending called |

**Estimate**: ~12 hours
