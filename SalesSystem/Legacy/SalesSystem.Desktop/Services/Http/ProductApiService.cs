using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class ProductApiService : IProductApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/products";

    public ProductApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<ProductDto>>> GetAllAsync(string? search = null, int? categoryId = null, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrEmpty(search)) query["search"] = search;
        if (categoryId.HasValue) query["categoryId"] = categoryId.Value.ToString();
        query["includeInactive"] = includeInactive.ToString().ToLower();

        return await _http.GetListAsync<ProductDto>($"{BasePath}?{query}", ct);
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetAsync<ProductDto>($"{BasePath}/{id}", ct);
    }

    public async Task<Result<ProductDto>> GetByBarcodeAsync(string barcode, CancellationToken ct = default)
    {
        return await _http.GetAsync<ProductDto>($"{BasePath}/barcode/{barcode}", ct);
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<ProductDto>(BasePath, r, ct);
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest r, CancellationToken ct = default)
    {
        return await _http.PutAsync<ProductDto>($"{BasePath}/{id}", r, ct);
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

