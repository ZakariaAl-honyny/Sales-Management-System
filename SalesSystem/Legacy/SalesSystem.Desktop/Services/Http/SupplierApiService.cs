using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class SupplierApiService : ISupplierApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/suppliers";

    public SupplierApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<SupplierDto>>> GetAllAsync(string? search = null, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrEmpty(search)) query["search"] = search;
        query["includeInactive"] = includeInactive.ToString().ToLower();

        return await _http.GetListAsync<SupplierDto>($"{BasePath}?{query}", ct);
    }

    public async Task<Result<SupplierDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetAsync<SupplierDto>($"{BasePath}/{id}", ct);
    }

    public async Task<Result<SupplierDto>> CreateAsync(CreateSupplierRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<SupplierDto>(BasePath, r, ct);
    }

    public async Task<Result<SupplierDto>> UpdateAsync(int id, UpdateSupplierRequest r, CancellationToken ct = default)
    {
        return await _http.PutAsync<SupplierDto>($"{BasePath}/{id}", r, ct);
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

