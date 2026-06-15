using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Financial report API service implementation.
/// Communicates with the API's financial-reports endpoints.
/// </summary>
public class FinancialReportApiService : ApiServiceBase, IFinancialReportApiService
{
    private const string BasePath = "api/v1/financial-reports";

    public FinancialReportApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    /// <inheritdoc />
    public async Task<Result<List<IncomeStatementDto>>> GetIncomeStatementAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<IncomeStatementDto>>(
            () => _httpClient.GetAsync($"{BasePath}/income-statement?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "FinancialReportApiService.GetIncomeStatementAsync");
    }

    /// <inheritdoc />
    public async Task<Result<CashFlowReportDto>> GetCashFlowReportAsync(DateTime from, DateTime to, int? cashBoxId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/cash-flow?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (cashBoxId.HasValue)
            url += $"&cashBoxId={cashBoxId.Value}";

        return await ExecuteAsync<CashFlowReportDto>(
            () => _httpClient.GetAsync(url, ct),
            "FinancialReportApiService.GetCashFlowReportAsync");
    }

    /// <inheritdoc />
    public async Task<Result<List<VatReportDto>>> GetVatReportAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<VatReportDto>>(
            () => _httpClient.GetAsync($"{BasePath}/vat-report?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "FinancialReportApiService.GetVatReportAsync");
    }

    /// <inheritdoc />
    public async Task<Result<List<AccountStatementDto>>> GetCustomerAccountStatementAsync(int customerId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<AccountStatementDto>>(
            () => _httpClient.GetAsync($"{BasePath}/account-statement/customer/{customerId}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "FinancialReportApiService.GetCustomerAccountStatementAsync");
    }

    /// <inheritdoc />
    public async Task<Result<List<AccountStatementDto>>> GetSupplierAccountStatementAsync(int supplierId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<AccountStatementDto>>(
            () => _httpClient.GetAsync($"{BasePath}/account-statement/supplier/{supplierId}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "FinancialReportApiService.GetSupplierAccountStatementAsync");
    }

    /// <inheritdoc />
    public async Task<Result<BalanceSheetDto>> GetBalanceSheetAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        return await ExecuteAsync<BalanceSheetDto>(
            () => _httpClient.GetAsync($"{BasePath}/balance-sheet?asOfDate={asOfDate:yyyy-MM-dd}", ct),
            "FinancialReportApiService.GetBalanceSheetAsync");
    }

    /// <inheritdoc />
    public async Task<Result<List<TrialBalanceDto>>> GetTrialBalanceAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<TrialBalanceDto>>(
            () => _httpClient.GetAsync($"{BasePath}/trial-balance?asOfDate={asOfDate:yyyy-MM-dd}", ct),
            "FinancialReportApiService.GetTrialBalanceAsync");
    }

    /// <inheritdoc />
    public async Task<Result<AccountLedgerDto>> GetGeneralLedgerAsync(int accountId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<AccountLedgerDto>(
            () => _httpClient.GetAsync($"{BasePath}/general-ledger/{accountId}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "FinancialReportApiService.GetGeneralLedgerAsync");
    }

    public async Task<Result<WorkingCapitalSummaryDto>> GetWorkingCapitalAsync(DateTime asOfDate, CancellationToken ct = default)
    {
        return await ExecuteAsync<WorkingCapitalSummaryDto>(
            () => _httpClient.GetAsync($"{BasePath}/working-capital?asOfDate={asOfDate:yyyy-MM-dd}", ct),
            "FinancialReportApiService.GetWorkingCapitalAsync");
    }

    public async Task<Result<List<AccountBalanceReportDto>>> GetAccountBalancesAsync(byte? accountType = null, string? search = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/account-balances";
        var queryParams = new List<string>();
        if (accountType.HasValue) queryParams.Add($"accountType={accountType}");
        if (!string.IsNullOrEmpty(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

        return await ExecuteAsync<List<AccountBalanceReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "FinancialReportApiService.GetAccountBalancesAsync");
    }
}
