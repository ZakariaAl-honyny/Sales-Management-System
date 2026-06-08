using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Reports API service implementation
/// </summary>
public class ReportApiService : ApiServiceBase, IReportApiService
{
    private const string BasePath = "api/v1/reports";

    public ReportApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SalesReportDto>>> GetSalesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var url = $"{BasePath}/sales?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (warehouseId.HasValue) url += $"&warehouseId={warehouseId}";
        
        return await ExecuteAsync<List<SalesReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetSalesReportAsync");
    }

    public async Task<Result<List<PurchaseReportDto>>> GetPurchasesReportAsync(int? warehouseId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var url = $"{BasePath}/purchases?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        if (warehouseId.HasValue) url += $"&warehouseId={warehouseId}";
        
        return await ExecuteAsync<List<PurchaseReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetPurchasesReportAsync");
    }

    public async Task<Result<List<StockReportDto>>> GetStockReportAsync(int? warehouseId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/stock";
        if (warehouseId.HasValue) url += $"?warehouseId={warehouseId}";
        
        return await ExecuteAsync<List<StockReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetStockReportAsync");
    }

    public async Task<Result<List<CustomerFinancialBalanceDto>>> GetCustomerBalancesReportAsync(int? customerId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/customers";
        if (customerId.HasValue) url += $"?customerId={customerId}";
        
        return await ExecuteAsync<List<CustomerFinancialBalanceDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetCustomerBalancesReportAsync");
    }

    public async Task<Result<List<SupplierBalanceReportDto>>> GetSupplierBalancesReportAsync(int? supplierId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/suppliers";
        if (supplierId.HasValue) url += $"?supplierId={supplierId}";
        
        return await ExecuteAsync<List<SupplierBalanceReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetSupplierBalancesReportAsync");
    }

    public async Task<Result<List<ProductMovementReportDto>>> GetProductMovementsReportAsync(int productId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/product-movements?productId={productId}";
        if (from.HasValue) url += $"&from={from.Value:yyyy-MM-dd}";
        if (to.HasValue) url += $"&to={to.Value:yyyy-MM-dd}";
        
        return await ExecuteAsync<List<ProductMovementReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetProductMovementsReportAsync");
    }

    public async Task<Result<List<LowStockReportDto>>> GetLowStockReportAsync(int? warehouseId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/low-stock";
        if (warehouseId.HasValue) url += $"?warehouseId={warehouseId}";
        
        return await ExecuteAsync<List<LowStockReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetLowStockReportAsync");
    }

    public async Task<Result<List<ExpiredProductDto>>> GetExpiredProductsReportAsync(int thresholdDays = 0, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<ExpiredProductDto>>(
            () => _httpClient.GetAsync($"{BasePath}/expired-products?thresholdDays={thresholdDays}", ct),
            "ReportApiService.GetExpiredProductsReportAsync");
    }
}
