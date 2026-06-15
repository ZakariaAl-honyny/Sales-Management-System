using System.Net.Http;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

public class PurchaseReportApiService : ApiServiceBase, IPurchaseReportApiService
{
    private const string BasePath = "api/v1/reports/purchases";

    public PurchaseReportApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<PurchasesBySupplierDto>>> GetPurchasesBySupplierAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<PurchasesBySupplierDto>>(
            () => _httpClient.GetAsync($"{BasePath}/by-supplier?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "PurchaseReportApiService.GetPurchasesBySupplierAsync");
    }

    public async Task<Result<List<PurchasesByProductDto>>> GetPurchasesByProductAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<PurchasesByProductDto>>(
            () => _httpClient.GetAsync($"{BasePath}/by-product?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct),
            "PurchaseReportApiService.GetPurchasesByProductAsync");
    }

    public async Task<Result<List<PurchaseTrendDto>>> GetPurchaseTrendsAsync(DateTime from, DateTime to, string groupBy = "day", CancellationToken ct = default)
    {
        return await ExecuteAsync<List<PurchaseTrendDto>>(
            () => _httpClient.GetAsync($"{BasePath}/trends?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}&groupBy={groupBy}", ct),
            "PurchaseReportApiService.GetPurchaseTrendsAsync");
    }
}
