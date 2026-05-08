using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Customers;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class CustomerApiService : ICustomerApiService
{
    private readonly HttpClient _httpClient;
    public CustomerApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            var path = $"api/v1/customers?{query}";
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<CustomerDto>>(path, ct);
            return Result<IReadOnlyList<CustomerDto>>.Success(response ?? new List<CustomerDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<CustomerDto>>.Failure(ex.Message); }
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CustomerDto>($"api/v1/customers/{id}", ct);
            return response != null ? Result<CustomerDto>.Success(response) : Result<CustomerDto>.Failure("NotFound");
        }
        catch (Exception ex) { return Result<CustomerDto>.Failure(ex.Message); }
    }

    public async Task<Result<CustomerDto>> CreateAsync(string name, string? phone, string? email, string? address, decimal openingBalance, CancellationToken ct = default)
    {
        try
        {
            var request = new CreateCustomerRequest(null, name, phone, email, address, openingBalance);
            var response = await _httpClient.PostAsJsonAsync("api/v1/customers", request, ct);
            if (response.IsSuccessStatusCode) return Result<CustomerDto>.Success((await response.Content.ReadFromJsonAsync<CustomerDto>(cancellationToken: ct))!);
            return Result<CustomerDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<CustomerDto>.Failure(ex.Message); }
    }

    public async Task<Result> UpdateAsync(int id, string name, string? phone, string? email, string? address, bool isActive, CancellationToken ct = default)
    {
        try
        {
            var request = new UpdateCustomerRequest(id, null, name, phone, email, address, isActive);
            var response = await _httpClient.PutAsJsonAsync($"api/v1/customers/{id}", request, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
