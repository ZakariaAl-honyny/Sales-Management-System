using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Application.Interfaces.Services;

/// <summary>
/// Service for cash box-related reports.
/// </summary>
public interface ICashBoxReportService
{
    /// <summary>
    /// Gets cash box summary showing balance information per cash box.
    /// </summary>
    Task<Result<List<CashBoxSummaryDto>>> GetCashBoxSummaryAsync(DateTime? asOfDate = null, CancellationToken ct = default);

    /// <summary>
    /// Gets receipt vouchers for a specific period.
    /// </summary>
    Task<Result<List<ReceiptVoucherReportDto>>> GetReceiptVoucherReportAsync(DateTime from, DateTime to, int? cashBoxId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets payment vouchers for a specific period.
    /// </summary>
    Task<Result<List<PaymentVoucherReportDto>>> GetPaymentVoucherReportAsync(DateTime from, DateTime to, int? cashBoxId = null, CancellationToken ct = default);
}
