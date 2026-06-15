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

    public async Task<Result<List<StockBalanceReportDto>>> GetStockBalanceReportAsync(int? warehouseId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/stock-balance";
        if (warehouseId.HasValue) url += $"?warehouseId={warehouseId}";

        return await ExecuteAsync<List<StockBalanceReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetStockBalanceReportAsync");
    }

    public async Task<Result<List<WarehouseMovementReportDto>>> GetWarehouseMovementsAsync(int? warehouseId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/warehouse-movements";
        var queryParams = new List<string>();
        if (warehouseId.HasValue) queryParams.Add($"warehouseId={warehouseId}");
        if (from.HasValue) queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) queryParams.Add($"to={to.Value:yyyy-MM-dd}");
        if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

        return await ExecuteAsync<List<WarehouseMovementReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetWarehouseMovementsAsync");
    }

    public async Task<Result<List<DetailedStockLedgerDto>>> GetDetailedStockLedgerAsync(int? productId = null, int? warehouseId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/detailed-stock-ledger";
        var queryParams = new List<string>();
        if (productId.HasValue) queryParams.Add($"productId={productId}");
        if (warehouseId.HasValue) queryParams.Add($"warehouseId={warehouseId}");
        if (from.HasValue) queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) queryParams.Add($"to={to.Value:yyyy-MM-dd}");
        if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

        return await ExecuteAsync<List<DetailedStockLedgerDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetDetailedStockLedgerAsync");
    }

    public async Task<Result<List<ReturnsReportDto>>> GetReturnsReportAsync(string? returnType = null, DateTime? from = null, DateTime? to = null, int? productId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/returns";
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(returnType)) queryParams.Add($"returnType={returnType}");
        if (from.HasValue) queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) queryParams.Add($"to={to.Value:yyyy-MM-dd}");
        if (productId.HasValue) queryParams.Add($"productId={productId}");
        if (queryParams.Count > 0) url += "?" + string.Join("&", queryParams);

        return await ExecuteAsync<List<ReturnsReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetReturnsReportAsync");
    }

    public async Task<Result<List<AgingReportDto>>> GetAgingReportAsync(string partyType = "Customers", int? partyId = null, CancellationToken ct = default)
    {
        var url = $"{BasePath}/aging?partyType={partyType}";
        if (partyId.HasValue) url += $"&partyId={partyId}";

        return await ExecuteAsync<List<AgingReportDto>>(
            () => _httpClient.GetAsync(url, ct),
            "ReportApiService.GetAgingReportAsync");
    }
}
