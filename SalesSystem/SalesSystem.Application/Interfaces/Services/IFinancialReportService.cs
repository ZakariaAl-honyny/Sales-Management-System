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
