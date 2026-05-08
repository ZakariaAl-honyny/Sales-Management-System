using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Payments;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class CustomerPaymentApiService : ICustomerPaymentApiService
{
    private readonly HttpClient _httpClient;
    public CustomerPaymentApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<CustomerPaymentDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<CustomerPaymentDto>>("api/v1/payments/customers", ct);
            return Result<IReadOnlyList<CustomerPaymentDto>>.Success(response ?? new List<CustomerPaymentDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<CustomerPaymentDto>>.Failure(ex.Message); }
    }

    public async Task<Result<CustomerPaymentDto>> CreateAsync(CreateCustomerPaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/payments/customers", request, ct);
            if (response.IsSuccessStatusCode) return Result<CustomerPaymentDto>.Success((await response.Content.ReadFromJsonAsync<CustomerPaymentDto>(cancellationToken: ct))!);
            return Result<CustomerPaymentDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<CustomerPaymentDto>.Failure(ex.Message); }
    }
}
