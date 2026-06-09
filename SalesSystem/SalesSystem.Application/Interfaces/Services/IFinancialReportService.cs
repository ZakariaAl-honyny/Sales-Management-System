using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

public interface IFinancialReportService
{
    /// <summary>
    /// Gets income statement (قائمة الدخل) for a date range.
    /// Returns sales revenue, purchase cost, and net profit/loss.
    /// </summary>
    Task<Result<List<IncomeStatementDto>>> GetIncomeStatementAsync(DateTime from, DateTime to, CancellationToken ct);

    /// <summary>
    /// Gets hierarchical income statement — RULE-422.
    /// Revenue - COGS = GrossProfit - OperatingExpenses = NetIncome with subtotals.
    /// </summary>
    Task<Result<IncomeStatementHierarchyDto>> GetIncomeStatementHierarchyAsync(DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Gets balance sheet — RULE-423.
    /// Assets = Liabilities + Equity with section subtotals.
    /// </summary>
    Task<Result<BalanceSheetDto>> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken ct = default);

    /// <summary>
    /// Gets trial balance — opening, transactions, and closing balances for all accounts.
    /// </summary>
    Task<Result<List<TrialBalanceDto>>> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken ct = default);

    /// <summary>
    /// Gets general ledger for a single account.
    /// </summary>
    Task<Result<GeneralLedgerDto>> GetGeneralLedgerAsync(int accountId, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>
    /// Gets cash flow report (كشف التدفق النقدي) for a date range, optionally filtered by cash box.
    /// </summary>
    Task<Result<CashFlowReportDto>> GetCashFlowReportAsync(DateTime from, DateTime to, int? cashBoxId, CancellationToken ct);

    /// <summary>
    /// Gets VAT report (تقرير الضريبة) for a date range from posted invoices.
    /// </summary>
    Task<Result<List<VatReportDto>>> GetVatReportAsync(DateTime from, DateTime to, CancellationToken ct);

    /// <summary>
    /// Gets customer account statement (كشف حساب عميل) for a date range.
    /// </summary>
    Task<Result<List<AccountStatementDto>>> GetAccountStatementAsync(int customerId, DateTime from, DateTime to, CancellationToken ct);

    /// <summary>
    /// Gets supplier account statement (كشف حساب مورد) for a date range.
    /// </summary>
    Task<Result<List<AccountStatementDto>>> GetSupplierStatementAsync(int supplierId, DateTime from, DateTime to, CancellationToken ct);
}
