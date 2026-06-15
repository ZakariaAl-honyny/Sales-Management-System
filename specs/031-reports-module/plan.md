# Phase 31 — Reports Module Implementation Plan

## 1. Summary

Phase 31 delivers comprehensive reporting across 5 categories (Financial, Inventory, Sales, Purchases, Cash) with Excel/PDF export. The implementation builds on existing infrastructure — 6 report services (ReportService, FinancialReportService, SalesReportService, PurchaseReportService, CashBoxReportService, UserReportService), 7 controllers, 35+ DTOs, 28+ Desktop Views/ViewModels, and a working ClosedXML + QuestPDF export pipeline. This plan focuses on completing remaining gaps, adding aging reports, finalizing export integration, validating all existing implementations, and ensuring Desktop navigation/export works end-to-end.

---

## 2. Report Categories & Data Sources

### 2.1 Financial Reports

| Report | Primary Tables | Logic |
|--------|---------------|-------|
| Trial Balance | JournalEntries + JournalEntryLines + Accounts | Aggregate debit/credit per account within date range; compute opening balance from prior entries + account.OpeningBalance |
| Income Statement | JournalEntries (Posted, Revenue/Expense accounts) | SUM revenue accounts (credit side) minus SUM expense accounts (debit side); hierarchical version groups by account type with subtotals at Revenue, COGS, GrossProfit, OperatingExpenses, NetIncome levels |
| Balance Sheet | JournalEntries (Posted, Asset/Liability/Equity accounts) asOfDate | Asset = Dr - Cr + OpeningBalance; Liability/Equity = Cr - Dr + OpeningBalance; sections: Assets, Liabilities, Equity; IsBalanced flag validates Assets = Liabilities + Equity |
| General Ledger | JournalEntryLines (by AccountId) | Running balance computation: opening balance from prior entries, then period lines ordered by date with cumulative Dr/Cr; returns account header + line details |
| Account Statement | SalesInvoices/CustomerReceipts (Customer) OR PurchaseInvoices/SupplierPayments (Supplier) | Chronological list of invoices and payments with running balance; customer = Dr for invoices, Cr for payments; supplier = Cr for invoices, Dr for payments |
| Cash Flow | ReceiptVouchers + PaymentVouchers | Aggregate income (ReceiptVouchers) and expense (PaymentVouchers) by date range; compute opening balance from prior vouchers; report total inflows, outflows, net cash flow, closing balance |
| VAT Report | SalesInvoices + PurchaseInvoices (TaxAmount > 0) | List taxable invoices with taxable amount (SubTotal - Discount), computed tax rate, and tax amount; sorted by date; covers both sales and purchase VAT |

**Key Decision**: Financial reports query the Journal Entries ledger (source of truth for all account balances) rather than denormalized invoice/payment tables. The only exceptions are Customer/Supplier Account Statements (which need per-invoice/per-payment detail) and VAT Report (which needs tax-specific breakdown per invoice).

### 2.2 Inventory Reports

| Report | Primary Tables | Logic |
|--------|---------------|-------|
| Stock Balance | WarehouseStocks + Products + InventoryBatches | Current quantity per product/warehouse; average cost from InventoryBatches WeightedAverage; total value = quantity × avg cost; BalanceStatus computed property (LowStock vs Normal) |
| Low Stock Alert | WarehouseStocks + Products | Filter WHERE Quantity < ReorderLevel; suggest reorder quantities based on wholesale unit conversion; StockAlertDays configurable in SystemSettings |
| Inventory Movement | InventoryTransactions + InventoryTransactionLines + InventoryBatches | All movement types (Purchase, Sale, Return, Transfer, Adjustment, etc.) within date range with quantities before/after; filterable by warehouse, product, date range, movement type |
| Expiry Report | InventoryBatches + Products | Batches WHERE ExpiryDate <= (Today + thresholdDays); shows days remaining/expired, quantity remaining, product name; thresholdDays configurable (default = 0 = already expired) |
| Product Movement | InventoryTransactionLines (by ProductId) | Full transaction history for a single product: date, warehouse, movement type, reference document, quantity change, quantity after |

**Key Decision**: Stock balances are computed from WarehouseStocks (live quantity) and InventoryBatches (cost). All stock changes are recorded in InventoryTransactions with dual-entry lines, ensuring full audit trail. AverageCost for total valuation computed as weighted average across all active batches per product.

### 2.3 Sales Reports

| Report | Primary Tables | Logic |
|--------|---------------|-------|
| Sales Invoice List | SalesInvoices | All posted invoices with customer, date, amounts, status; filterable by date range, customer, warehouse, payment type |
| Sales by Customer | SalesInvoices (grouped by CustomerId) | Invoice count + TotalAmount + PaidAmount + DueAmount per customer; sorted by total descending |
| Sales by Product | SalesInvoiceLines (grouped by ProductId) | Quantity, LineTotal, CostInBaseCurrency, Profit = LineTotal - CostInBaseCurrency, ProfitMargin% |
| Sales by Category | SalesInvoiceLines → Products → ProductCategories | Category-level aggregation with invoice count and total amount; includes "غير مصنف" bucket for uncategorised products |
| Daily Sales Summary | SalesInvoices (grouped by InvoiceDate) | Daily aggregates: invoice count, total amount, discount, net amount |
| Sales Trends | SalesInvoices (grouped by period) | Monthly/Quarterly/Yearly aggregation with profit margin analysis: TotalSales, TotalCost, TotalProfit, ProfitMargin% |
| Sales by User | SalesInvoices (grouped by CreatedByUserId) | Invoice count + total amount per user who created the invoice |
| **Sales Profit per Invoice** | SalesInvoices + SalesInvoiceLines + InventoryBatches | Per-invoice profit = SUM(LineTotal) - SUM(CostInBaseCurrency) — CostInBaseCurrency populated at posting time from average cost (FIFO weighted) |

**Key Decision**: Profit calculation uses CostInBaseCurrency stored on SalesInvoiceLine at posting time (sourced from InventoryBatches AverageCost at that moment), not computed live. This ensures profit figures are immutable after posting and match the accounting journal entries.

### 2.4 Purchase Reports

| Report | Primary Tables | Logic |
|--------|---------------|-------|
| Purchase Invoice List | PurchaseInvoices | All posted invoices with supplier, date, amounts, status |
| Purchases by Supplier | PurchaseInvoices (grouped by SupplierId) | Invoice count + NetTotal + PaidAmount + RemainingAmount per supplier |
| Purchases by Product | PurchaseInvoiceLines (grouped by ProductId) | Quantity + LineTotal (total cost) per product |
| Purchase Trends | PurchaseInvoices (grouped by period) | Monthly/Quarterly/Yearly aggregation of NetTotal |

### 2.5 Cash Reports

| Report | Primary Tables | Logic |
|--------|---------------|-------|
| Cash Box Summary | CashBoxes + ReceiptVouchers + PaymentVouchers + Expenses | Opening balance computed from vouchers before asOfDate; TotalIncome = ReceiptVouchers sum; TotalExpense = PaymentVouchers + Expenses sum; ClosingBalance = Opening + Income - Expense |
| Receipt Voucher Report | ReceiptVouchers | List all receipt vouchers with cash box, account, amount, status |
| Payment Voucher Report | PaymentVouchers | List all payment vouchers with cash box, account, amount, status |
| Daily Summary | ReceiptVouchers + PaymentVouchers + Expenses (grouped by date) | Daily cash activity: total receipts, total payments, net cash flow |

**Key Decision**: Cash reports use ReceiptVouchers and PaymentVouchers as the data source (not a deprecated CashTransactions table). CashBox balance is computed dynamically by aggregating all vouchers per box — never stored as a denormalized field.

### 2.6 User & Audit Reports

| Report | Primary Tables | Logic |
|--------|---------------|-------|
| User Activity | AuditLogs | All audit log entries with timestamp, user, action, entity type, entity ID, details |
| Login History | AuditLogs (filtered by login actions) | LoginSuccess, LoginFailed, LoginBlocked_Locked entries with success/failure status |
| Audit Trail Summary | AuditLogs (aggregated) | Summarised audit trail with user names resolved |

---

## 3. Key DTOs

All DTOs live in `SalesSystem.Contracts.DTOs` (ReportDtos.cs + AllDtos.cs). New DTOs required:

| DTO | Category | Fields | Notes |
|-----|----------|--------|-------|
| CustomerAgingDto | Financial | CustomerId, CustomerName, Phone, CurrentBalance, AgeBuckets (0-30, 31-60, 61-90, 90+) | Single row per customer with 4 aging columns |
| SupplierAgingDto | Financial | SupplierId, SupplierName, Phone, CurrentBalance, AgeBuckets | Mirrors CustomerAging for suppliers |
| CashFlowDetailedDto | Cash | CashBoxId, CashBoxName, PeriodStart, PeriodEnd, OpeningBalance, IncomeItems[], ExpenseItems[], ClosingBalance | Supports drill-down to individual vouchers |

**Existing DTOs** (already defined, no changes needed): IncomeStatementHierarchyDto, BalanceSheetDto, BalanceSheetSectionDto, BalanceSheetLineDto, TrialBalanceDto, GeneralLedgerDto, GeneralLedgerLineDto, AccountStatementDto, SalesByCustomerDto, SalesByProductDto, SalesByCategoryDto, DailySalesSummaryDto, SalesTrendDto, SalesByUserDto, PurchasesBySupplierDto, PurchasesByProductDto, PurchaseTrendDto, CashBoxSummaryDto, ReceiptVoucherReportDto, PaymentVoucherReportDto, UserActivityReportDto, LoginHistoryDto, AuditTrailSummaryDto, StockBalanceReportDto, WarehouseMovementReportDto, ExpiredProductDto, LowStockReportDto, ReportExportResult, VatReportDto, CustomerFinancialBalanceDto, SupplierBalanceReportDto, ProductMovementReportDto, CustomerAgingReportDto, CustomerBalanceReportDto, DashboardSummaryDto, IncomeStatementDto, CashFlowReportDto, CashFlowItemDto, SalesReportDto, PurchaseReportDto, StockReportDto.

---

## 4. Business Logic Decisions

### 4.1 AsNoTracking
ALL report queries use `AsNoTracking()` for performance. Report data is read-only — no tracking overhead needed.

### 4.2 Date Range Validation
ALL report service methods validate `from <= to` at entry point and return `Result.Failure` with Arabic error message `"تاريخ البداية يجب أن يكون قبل تاريخ النهاية"`. This validation is in the service, not duplicated in controllers.

### 4.3 Profit Calculation
SalesLineItem.CostInBaseCurrency is populated at invoice posting time using the AverageCost from InventoryBatches (weighted average of all active batches). This value is immutable — live cost changes after posting do NOT retroactively affect posted invoice profit calculations. Profit = LineTotal - CostInBaseCurrency. ProfitMargin% = Profit / LineTotal × 100.

### 4.4 Aging Buckets
Customer/Supplier aging computed as: for each unpaid invoice, compute days overdue = (today - invoice date). Categorise into: 0-30 days, 31-60 days, 61-90 days, 90+ days. Invoice is "unpaid" when RemainingAmount > 0 and Status = Posted. Payments are applied to invoices via CustomerReceiptApplications/SupplierPaymentApplications.

### 4.5 Hierarchical Financial Statements
Income Statement hierarchy: Revenue section → COGS section → GrossProfit subtotal → OperatingExpenses section → NetIncome total. Balance Sheet: Assets section → Liabilities section → Equity section → TotalLiabilitiesAndEquity → IsBalanced flag (tolerance = 0.01). Each section can have child lines if account-level detail exists.

### 4.6 Currency Support
All financial reports display amounts in the base currency (determined by Currency.IsBaseCurrency = true). Multi-currency invoices/vouchers store their amounts in both original currency (Amount) and base currency (AmountInBaseCurrency via ExchangeRate). Reports always aggregate in base currency.

### 4.7 Date Range Filtering
Every report endpoint accepts `from` and `to` query parameters (DateTime). Reports default to current month if not specified. Reports with "as of date" (Balance Sheet, Trial Balance) accept a single `asOfDate` parameter instead.

---

## 5. Excel/PDF Export Pipeline

### 5.1 Excel Export (ClosedXML)
`IReportExportService.ExportToExcelAsync<T>()` accepts a `List<T>` of report DTOs and uses `ReportDataTableHelper.ToDataTable()` (reflection-based) to convert DTOs to a DataTable. ClosedXML writes the DataTable with RTL layout, styled headers (blue background, white bold font), alternating row colours, auto-fitted columns. Returns byte[] as `ReportExportResult`.

### 5.2 PDF Export (QuestPDF)
`IReportExportService.ExportToPdfAsync()` accepts a DataTable + title and generates an A4 PDF with table layout. Header row, data rows with borders, footer with export timestamp. Uses QuestPDF `Community` license (already initialised in PrintingBootstrapper).

### 5.3 Export Controller
`ReportExportController.Export()` endpoint accepts `ReportExportRequest` (ReportType, Format, Filters). The actual data population delegates to the specific report service (e.g., FinancialReportService) which returns `List<T>`, then calls `ExportToExcelAsync<T>()` or `ExportToPdfAsync()`. The controller returns `FileContentResult` with the appropriate content type and filename.

### 5.4 Desktop Export
Desktop ViewModels call `IFinancialReportExportService` (desktop-side service) which calls the API's export endpoint and saves the downloaded file. Each report ViewModel has an `ExportToExcelCommand` and `ExportToPdfCommand` that:
1. Collects current filter parameters (date range, warehouse, etc.)
2. Calls the API to get report data
3. Calls export service to generate the file
4. Opens SaveFileDialog with suggested filename
5. Writes bytes to the selected path

---

## 6. Desktop View Implementation

### 6.1 Existing Views (Already Implemented — Validation Only)
28+ Views and ViewModels already exist in `SalesSystem.DesktopPWF/Views/Reports/` and `ViewModels/Reports/`. These need verification that:
- All properties bind correctly to existing DTOs
- Date range filters work end-to-end
- Export buttons trigger export pipeline
- EventBus subscriptions are properly disposed
- `ExecuteAsync()` wrapper is used (no manual try/catch)
- Newest-first sorting is applied where applicable
- Arabic ToolTips are present on all interactive controls

### 6.2 Views Requiring Implementation/Completion

| View | Status | Action |
|------|--------|--------|
| ReportsView.xaml (main navigation) | ✅ Exists | Verify navigation to all sub-reports |
| TrialBalanceView | ✅ Exists | Validate data binding |
| IncomeStatementView | ✅ Exists | Validate hierarchy rendering |
| BalanceSheetView | ✅ Exists | Validate section totals |
| AccountStatementView | ✅ Exists | Validate running balance |
| GeneralLedgerView | ✅ Exists | Validate ledger lines |
| StockBalanceReportView | ✅ Exists | Validate stock data |
| LowStockReportView | 🆕 Missing | Create View + ViewModel |
| ExpiredProductsReportView | ✅ Exists | Validate with InventoryBatches |
| WarehouseMovementReportView | ✅ Exists | Validate movement data |
| DailySalesView | ✅ Exists | Validate daily aggregation |
| SalesByCustomerView | ✅ Exists | Validate grouping |
| SalesByProductView | ✅ Exists | Validate profit calculation |
| PurchasesBySupplierView | ✅ Exists | Validate grouping |
| PurchasesByProductView | ✅ Exists | Validate data |
| CashBoxSummaryView | ✅ Exists | Validate summary |
| CashFlowReportView | ✅ Exists | Verify with ReceiptVouchers |
| DailyClosureReportView | ✅ Exists | Validate daily data |
| VatReportView | ✅ Exists | Validate tax breakdown |
| LoginHistoryView | ✅ Exists | Validate login data |
| UserActivityView | ✅ Exists | Validate audit data |

### 6.3 Views Missing (Requires Full Creation)

| View | Reason | DTO |
|------|--------|-----|
| CustomerAgingView | New report | CustomerAgingDto |
| SupplierAgingView | New report | SupplierAgingDto |
| CashFlowDetailedView | Enhanced from existing stub | CashFlowDetailedDto |

### 6.4 MainWindow Navigation
Add menu items in MainWindow sidebar under "التقارير" section:
- التقارير المالية (Financial Reports) → submenu with Trial Balance, Income Statement, Balance Sheet, General Ledger, Account Statement
- تقارير المخزون (Inventory Reports) → submenu with Stock Balance, Low Stock, Movements, Expiry
- تقارير المبيعات (Sales Reports) → submenu with Summary, By Customer, By Product, Daily, Trends
- تقارير المشتريات (Purchase Reports) → submenu with Summary, By Supplier, By Product
- تقارير الصناديق (Cash Reports) → submenu with Summary, Receipt Vouchers, Payment Vouchers
- تقارير المستخدمين (User Reports) → submenu with Activity, Login History, Audit Trail

---

## 7. Tasks (Ordered by Dependency)

### T1: Financial Report Completion
- Validate FinancialReportService.TrialBalanceAsync() with account hierarchies
- Validate BalanceSheetAsync() with asset/liability/equity sections
- Validate GeneralLedgerAsync() running balance computation
- Validate AccountStatementAsync() for both customers and suppliers
- Add CustomerAgingAsync() and SupplierAgingAsync() to IFinancialReportService + implementation
- Add CustomerAgingDto and SupplierAgingDto records
- Add aging endpoints to FinancialReportsController

### T2: Inventory Report Completion
- Validate StockBalanceReportAsync() average cost calculation
- Validate ExpiredProductsReportAsync() threshold filtering
- Validate WarehouseMovementsAsync() data accuracy
- Validate LowStockReportAsync() reorder suggestions

### T3: Sales Report Completion
- Validate SalesByProductAsync() profit calculation
- Validate SalesByCustomerAsync() aggregation
- Validate SalesTrendsAsync() period grouping
- Validate DailySalesSummaryAsync() daily totals

### T4: Purchase Report Completion
- Validate PurchasesBySupplierAsync() data
- Validate PurchasesByProductAsync() aggregation
- Validate PurchaseTrendsAsync() period grouping

### T5: Cash Report Completion
- Implement CashBoxReportService (interface exists, implementation missing)
- Implement GetCashBoxSummaryAsync: aggregate ReceiptVouchers + PaymentVouchers per cash box
- Implement GetReceiptVoucherReportAsync: list receipt vouchers with filters
- Implement GetPaymentVoucherReportAsync: list payment vouchers with filters
- Validate with new schema (no CashTransactions table)

### T6: Export Pipeline Completion
- Fix ReportExportController.Export() to actually call services and generate files
- Add Accept header support to report endpoints (optional: return Excel/PDF directly)
- Implement desktop FFinancialReportExportService for client-side export
- Wire export commands to all report ViewModels

### T7: Desktop Views Completion
- Create CustomerAgingView + CustomerAgingViewModel
- Create SupplierAgingView + SupplierAgingViewModel
- Create CashFlowDetailedView + CashFlowDetailedViewModel
- Create LowStockReportView + LowStockReportViewModel (if missing)
- Register all new Views/ViewModels in Desktop DI
- Add MainWindow navigation entries
- Validate all 28+ existing views compile and render correctly

### T8: API Validation & Testing
- All controllers return `Result<T>` translated to HTTP (200 OK / 400 BadRequest / 404 NotFound)
- Date range validation in service (not duplicated in controller)
- `[Authorize(Policy = "ManagerAndAbove")]` on all report endpoints (except UserReports = AdminOnly)
- FluentValidation for all report request parameters
- Unit tests for report service edge cases (empty data, date inversion, single-day range)
- Integration tests for each report endpoint

### T9: Desktop Integration Testing
- Each report loads data from API without errors
- Date range filter changes trigger reload
- Export buttons produce valid Excel/PDF files
- Error dialogs shown on API failures (not raw exception dialogs)
- Toast notifications on successful export

---

## 8. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| CashBoxReportService missing implementation | Cash reports return 500 | Implement as T5 priority — data sources (ReceiptVouchers/PaymentVouchers) are well-defined |
| Existing ViewModels reference deprecated DTOs | Desktop compilation errors | Run verification pass first; fix binding errors before adding new views |
| ExportController is a stub | Export broken end-to-end | Rewrite to delegate to report services + ReportExportService |
| Aging reports need new DTOs + endpoints | Customer/Supplier aging unavailable | Add new DTOs and endpoints in T1; Desktop views in T7 |
| Large report queries timeout | Poor UX for large date ranges | Apply AsNoTracking, paginate where possible, add loading indicators in Desktop |
| Hierarchical Income Statement uses JournalEntries | Requires JournalEntries to exist and be posted | Graceful empty-result handling already implemented in FinancialReportService |
| LowStockReport requires wholesale unit data | Reorder suggestions may be incomplete | Fall back to basic quantity comparison (CurrentStock < ReorderLevel) without wholesale conversion if unit data unavailable |
