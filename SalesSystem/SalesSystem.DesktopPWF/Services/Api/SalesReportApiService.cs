using System.Net.Http;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public class SalesReportApiService : ApiServiceBase, ISalesReportApiService
{
    private const string BasePath = "api/v1/reports/sales";

    public SalesReportApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SalesByCustomerDto>>> GetSalesByCustomerAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<SalesByCustomerDto>>(
            () => _httpClient.GetAsync($"{BasePath}/by-customer?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "SalesReportApiService.GetSalesByCustomerAsync");
    }

    public async Task<Result<List<SalesByProductDto>>> GetSalesByProductAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<SalesByProductDto>>(
            () => _httpClient.GetAsync($"{BasePath}/by-product?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "SalesReportApiService.GetSalesByProductAsync");
    }

    public async Task<Result<List<SalesByCategoryDto>>> GetSalesByCategoryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<SalesByCategoryDto>>(
            () => _httpClient.GetAsync($"{BasePath}/by-category?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "SalesReportApiService.GetSalesByCategoryAsync");
    }

    public async Task<Result<List<DailySalesSummaryDto>>> GetDailySalesSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<DailySalesSummaryDto>>(
            () => _httpClient.GetAsync($"{BasePath}/daily-summary?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "SalesReportApiService.GetDailySalesSummaryAsync");
    }

    public async Task<Result<List<SalesTrendDto>>> GetSalesTrendsAsync(DateTime from, DateTime to, string groupBy = "day", CancellationToken ct = default)
    {
        return await ExecuteAsync<List<SalesTrendDto>>(
            () => _httpClient.GetAsync($"{BasePath}/trends?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&groupBy={groupBy}", ct),
            "SalesReportApiService.GetSalesTrendsAsync");
    }
}
