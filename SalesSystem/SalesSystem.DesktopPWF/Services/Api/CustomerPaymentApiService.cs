using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Customer Payments
/// </summary>
public class CustomerPaymentApiService : ApiServiceBase, ICustomerPaymentApiService
{
    private const string BasePath = "api/v1/customer-payments";

    public CustomerPaymentApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<CustomerPaymentDto>>> GetAllAsync(
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
        return await ExecutePagedAsync<CustomerPaymentDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "CustomerPaymentApiService.GetAllAsync");
    }

    public async Task<Result<CustomerPaymentDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<CustomerPaymentDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "CustomerPaymentApiService.GetByIdAsync");
    }

    public async Task<Result<CustomerPaymentDto>> CreateAsync(CreateCustomerPaymentRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<CustomerPaymentDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "CustomerPaymentApiService.CreateAsync");
    }

    public async Task<Result<CustomerPaymentDto>> UpdateAsync(int id, UpdateCustomerPaymentRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<CustomerPaymentDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "CustomerPaymentApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "CustomerPaymentApiService.DeleteAsync");
    }
}
