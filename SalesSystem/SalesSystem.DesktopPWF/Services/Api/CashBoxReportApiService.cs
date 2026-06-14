using System.Net.Http;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public class CashBoxReportApiService : ApiServiceBase, ICashBoxReportApiService
{
    private const string BasePath = "api/v1/reports/cash-boxes";

    public CashBoxReportApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<CashBoxSummaryDto>>> GetCashBoxSummaryAsync(DateTime? asOfDate = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/summary";
        if (asOfDate.HasValue)
            url += $"?asOfDate={asOfDate.Value:yyyy-MM-dd}";
        return await ExecuteAsync<List<CashBoxSummaryDto>>(
            () => _httpClient.GetAsync(url, ct),
            "CashBoxReportApiService.GetCashBoxSummaryAsync");
    }

    public async Task<Result<List<ReceiptVoucherReportDto>>> GetReceiptVoucherReportAsync(DateTime from, DateTime to, int? cashBoxId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/receipt-vouchers?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (cashBoxId.HasValue)
            url += $"&cashBoxId={cashBoxId.Value}";
        return await ExecuteAsync<List<ReceiptVoucherReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "CashBoxReportApiService.GetReceiptVoucherReportAsync");
    }

    public async Task<Result<List<PaymentVoucherReportDto>>> GetPaymentVoucherReportAsync(DateTime from, DateTime to, int? cashBoxId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/payment-vouchers?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (cashBoxId.HasValue)
            url += $"&cashBoxId={cashBoxId.Value}";
        return await ExecuteAsync<List<PaymentVoucherReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "CashBoxReportApiService.GetPaymentVoucherReportAsync");
    }
}
