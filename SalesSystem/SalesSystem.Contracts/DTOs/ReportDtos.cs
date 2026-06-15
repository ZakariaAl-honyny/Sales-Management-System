using System.Data;

namespace SalesSystem.Contracts.DTOs;

// ═══════════════════════════════════════════════════════
// Phase 31 — Financial Reports
// ═══════════════════════════════════════════════════════

/// <summary>
/// Recursive node in the hierarchical income statement — RULE-422.
/// Title, type (Header/SubTotal/Total), Amount, FormattedAmount, Level, optional Children.
/// </summary>
public record IncomeStatementHierarchyDto(
    string Title,
    string Type,
    decimal Amount,
    string? FormattedAmount,
    int Level,
    List<IncomeStatementHierarchyDto>? Children = null);

/// <summary>
/// Balance sheet line — account name, code, balance, and formatted display amount.
/// </summary>
public record BalanceSheetLineDto(
    string AccountName,
    string AccountCode,
    decimal Balance,
    string FormattedBalance);

/// <summary>
/// Section of the balance sheet (name, total, formatted total, lines).
/// </summary>
public record BalanceSheetSectionDto(
    string Name,
    decimal Total,
    string FormattedTotal,
    List<BalanceSheetLineDto> Lines);

/// <summary>
/// Balance sheet — Assets, Liabilities, Equity with totals and balanced flag.
/// </summary>
public record BalanceSheetDto(
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal TotalLiabilitiesAndEquity,
    bool IsBalanced,
    List<BalanceSheetSectionDto> Sections);

/// <summary>
/// Trial balance — account code, name, opening/transactions/closing debit/credit, type label.
/// </summary>
public record TrialBalanceDto(
    string AccountCode,
    string AccountName,
    decimal OpeningDebit,
    decimal OpeningCredit,
    decimal TransactionDebit,
    decimal TransactionCredit,
    decimal ClosingDebit,
    decimal ClosingCredit,
    string? AccountTypeLabel);

/// <summary>
/// General ledger line — date, entry number, description, debit, credit, running balance.
/// </summary>
public record GeneralLedgerLineDto(
    DateTime Date,
    string EntryNumber,
    string Description,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance);

/// <summary>
/// General ledger for a single account.
/// </summary>
public record GeneralLedgerDto(
    string AccountCode,
    string AccountName,
    DateTime FromDate,
    DateTime ToDate,
    decimal OpeningBalance,
    List<GeneralLedgerLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance);

// ═══════════════════════════════════════════════════════
// Phase 31 — Sales Reports
// ═══════════════════════════════════════════════════════

/// <summary>
/// Sales grouped by customer.
/// </summary>
public record SalesByCustomerDto(
    int CustomerId,
    string CustomerName,
    int InvoiceCount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount);

/// <summary>
/// Sales grouped by product with profit analysis.
/// </summary>
public record SalesByProductDto(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal TotalAmount,
    decimal TotalCost,
    decimal TotalProfit,
    decimal ProfitMargin);

/// <summary>
/// Sales grouped by product category.
/// </summary>
public record SalesByCategoryDto(
    int CategoryId,
    string CategoryName,
    int InvoiceCount,
    decimal TotalAmount);

/// <summary>
/// Daily sales summary.
/// </summary>
public record DailySalesSummaryDto(
    DateTime Date,
    int InvoiceCount,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal NetAmount);

/// <summary>
/// Sales trend across periods.
/// </summary>
public record SalesTrendDto(
    string Period,
    decimal TotalSales,
    decimal TotalCost,
    decimal TotalProfit,
    decimal ProfitMargin);

/// <summary>
/// Sales grouped by user (who created the invoice).
/// </summary>
public record SalesByUserDto(
    int UserId,
    string UserName,
    int InvoiceCount,
    decimal TotalAmount);

// ═══════════════════════════════════════════════════════
// Phase 31 — Purchase Reports
// ═══════════════════════════════════════════════════════

/// <summary>
/// Purchases grouped by supplier.
/// </summary>
public record PurchasesBySupplierDto(
    int SupplierId,
    string SupplierName,
    int InvoiceCount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal DueAmount);

/// <summary>
/// Purchases grouped by product.
/// </summary>
public record PurchasesByProductDto(
    int ProductId,
    string ProductName,
    decimal Quantity,
    decimal TotalCost);

/// <summary>
/// Purchase trend across periods.
/// </summary>
public record PurchaseTrendDto(
    string Period,
    decimal TotalAmount,
    decimal TotalCost);

// ═══════════════════════════════════════════════════════
// Phase 31 — Cash Box Reports
// ═══════════════════════════════════════════════════════

/// <summary>
/// Cash box summary — per-box balance snapshot used by CashBoxSummaryViewModel.
/// </summary>
public record CashBoxSummaryDto(
    int CashBoxId,
    string CashBoxName,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetBalance);

/// <summary>
/// Daily closure report line — used by DailyClosureReportView DataGrid.
/// Mapped from CashBoxSummaryDto at runtime since DailyClosure entity is deferred to V2.
/// </summary>
public record DailyClosureReportDto(
    DateTime Date,
    string CashBoxName,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetBalance,
    bool IsReconciled);

/// <summary>
/// Receipt voucher report line.
/// </summary>
public record ReceiptVoucherReportDto(
    int Id,
    int VoucherNo,
    DateTime VoucherDate,
    string CashBoxName,
    string AccountName,
    decimal TotalAmount,
    string? Notes,
    string StatusDisplay);

/// <summary>
/// Payment voucher report line.
/// </summary>
public record PaymentVoucherReportDto(
    int Id,
    int VoucherNo,
    DateTime VoucherDate,
    string CashBoxName,
    string AccountName,
    decimal TotalAmount,
    string? Notes,
    string StatusDisplay);

// ═══════════════════════════════════════════════════════
// Phase 31 — User Activity & Login Reports
// ═══════════════════════════════════════════════════════

/// <summary>
/// User activity audit trail entry.
/// </summary>
public record UserActivityReportDto(
    int UserId,
    string UserName,
    DateTime Timestamp,
    string Action,
    string EntityType,
    int? EntityId,
    string? Details);

/// <summary>
/// Login history entry.
/// </summary>
public record LoginHistoryDto(
    int UserId,
    string UserName,
    DateTime LoginTime,
    bool IsSuccess,
    string? FailureReason);

/// <summary>
/// Summary of audit trail actions per user.
/// </summary>
public record AuditTrailSummaryDto(
    DateTime Timestamp,
    string UserName,
    string Action,
    string EntityType,
    string? EntityId,
    string? Details);

// ═══════════════════════════════════════════════════════
// Phase 31 — Report Export
// ═══════════════════════════════════════════════════════

/// <summary>
/// Result of a report export operation.
/// </summary>
public record ReportExportResult(
    byte[] FileContent,
    string FileName,
    string ContentType);

// ═══════════════════════════════════════════════════════
// Phase 31 — Report DataTable Helper
// ═══════════════════════════════════════════════════════

public static class ReportDataTableHelper
{
    public static DataTable ToDataTable<T>(
        this List<T> items,
        Dictionary<string, string>? columnHeaders = null)
    {
        var table = new DataTable();
        if (items == null || items.Count == 0)
            return table;

        var props = typeof(T).GetProperties();

        foreach (var prop in props)
        {
            var displayName = columnHeaders?.GetValueOrDefault(prop.Name) ?? prop.Name;
            var type = prop.PropertyType;

            if (Nullable.GetUnderlyingType(type) is { } underlyingType)
                type = underlyingType;

            var colType = type switch
            {
                { } t when t == typeof(decimal) || t == typeof(int) || t == typeof(long) => typeof(decimal),
                { } t when t == typeof(DateTime) => typeof(DateTime),
                { } t when t == typeof(bool) => typeof(bool),
                { } t when t == typeof(byte) => typeof(byte),
                _ => typeof(string)
            };

            table.Columns.Add(displayName, colType);
        }

        foreach (var item in items)
        {
            var row = table.NewRow();
            foreach (var prop in props)
            {
                var displayName = columnHeaders?.GetValueOrDefault(prop.Name) ?? prop.Name;
                var value = prop.GetValue(item);
                row[displayName] = value ?? DBNull.Value;
            }
            table.Rows.Add(row);
        }

        return table;
    }
}
