using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class CustomerApiService : ICustomerApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/customers";

    public CustomerApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<CustomerDto>>> GetAllAsync(string? search = null, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrEmpty(search)) query["search"] = search;
        query["includeInactive"] = includeInactive.ToString().ToLower();

        return await _http.GetListAsync<CustomerDto>($"{BasePath}?{query}", ct);
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetAsync<CustomerDto>($"{BasePath}/{id}", ct);
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<CustomerDto>(BasePath, r, ct);
    }

    public async Task<Result<CustomerDto>> UpdateAsync(int id, UpdateCustomerRequest r, CancellationToken ct = default)
    {
        return await _http.PutAsync<CustomerDto>($"{BasePath}/{id}", r, ct);
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct = default)
    {
        return await _http.PutAsync<object>($"{BasePath}/{id}/deactivate", null, ct);
    }

    public async Task<Result> ReactivateAsync(int id, CancellationToken ct = default)
    {
        return await _http.PutAsync<object>($"{BasePath}/{id}/reactivate", null, ct);
    }
}

