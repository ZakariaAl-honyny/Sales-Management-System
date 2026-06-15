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

    public async Task<Result<List<ProductProfitabilityDto>>> GetProductProfitabilityAsync(int? productId = null, int? categoryId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/product-profitability";
        var queryParams = new List<string>();
        if (productId.HasValue) queryParams.Add($"productId={productId}");
        if (categoryId.HasValue) queryParams.Add($"categoryId={categoryId}");
        if (from.HasValue) queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) queryParams.Add($"to={to.Value:yyyy-MM-dd}");
        if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

        return await ExecuteAsync<List<ProductProfitabilityDto>>(
            () => _httpClient.GetAsync(url, ct),
            "SalesReportApiService.GetProductProfitabilityAsync");
    }

    public async Task<Result<List<ProfitByCustomerDto>>> GetProfitByCustomerAsync(int? customerId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/profit-by-customer";
        var queryParams = new List<string>();
        if (customerId.HasValue) queryParams.Add($"customerId={customerId}");
        if (from.HasValue) queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) queryParams.Add($"to={to.Value:yyyy-MM-dd}");
        if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

        return await ExecuteAsync<List<ProfitByCustomerDto>>(
            () => _httpClient.GetAsync(url, ct),
            "SalesReportApiService.GetProfitByCustomerAsync");
    }
}
