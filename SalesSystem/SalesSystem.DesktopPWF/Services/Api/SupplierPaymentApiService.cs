using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Supplier Payments
/// </summary>
public class SupplierPaymentApiService : ApiServiceBase, ISupplierPaymentApiService
{
    private const string BasePath = "api/v1/supplier-payments";

    public SupplierPaymentApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SupplierPaymentDto>>> GetAllAsync(
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrEmpty(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (from.HasValue)
            queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)
            queryParams.Add($"to={to.Value:yyyy-MM-dd}");
        if (includeInactive)
            queryParams.Add("includeInactive=true");

        var query = string.Join("&", queryParams);
        return await ExecutePagedAsync<SupplierPaymentDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "SupplierPaymentApiService.GetAllAsync");
    }

    public async Task<Result<SupplierPaymentDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SupplierPaymentDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "SupplierPaymentApiService.GetByIdAsync");
    }

    public async Task<Result<SupplierPaymentDto>> CreateAsync(CreateSupplierPaymentRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SupplierPaymentDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "SupplierPaymentApiService.CreateAsync");
    }

    public async Task<Result<SupplierPaymentDto>> UpdateAsync(int id, UpdateSupplierPaymentRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SupplierPaymentDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "SupplierPaymentApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "SupplierPaymentApiService.DeleteAsync");
    }
}
