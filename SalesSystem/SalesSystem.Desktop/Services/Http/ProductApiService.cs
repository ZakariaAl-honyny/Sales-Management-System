using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Products;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class ProductApiService : IProductApiService
{
    private readonly HttpClient _httpClient;
    public ProductApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<ProductDto>>> GetAllAsync(string? search = null, int? categoryId = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            if (categoryId.HasValue) query["categoryId"] = categoryId.Value.ToString();

            var path = $"api/v1/products?{query}";
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<ProductDto>>(path, ct);
            return Result<IReadOnlyList<ProductDto>>.Success(response ?? new List<ProductDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<ProductDto>>.Failure(ex.Message); }
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ProductDto>($"api/v1/products/{id}", ct);
            return response != null ? Result<ProductDto>.Success(response) : Result<ProductDto>.Failure("NotFound");
        }
        catch (Exception ex) { return Result<ProductDto>.Failure(ex.Message); }
    }

    public async Task<Result<ProductDto>> CreateAsync(ProductDto product, CancellationToken ct = default)
    {
        try
        {
            var request = new CreateProductRequest(
                product.Name, product.Code, product.Barcode, 
                product.CategoryId, product.UnitId, 
                product.PurchasePrice, product.SalePrice, 
                product.MinStock, product.Description);
                
            var response = await _httpClient.PostAsJsonAsync("api/v1/products", request, ct);
            if (response.IsSuccessStatusCode) return Result<ProductDto>.Success((await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct))!);
            return Result<ProductDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<ProductDto>.Failure(ex.Message); }
    }

    public async Task<Result> UpdateAsync(ProductDto product, CancellationToken ct = default)
    {
        try
        {
            var request = new UpdateProductRequest(
                product.Id, product.Name, product.Code, product.Barcode, 
                product.CategoryId, product.UnitId, 
                product.PurchasePrice, product.SalePrice, 
                product.MinStock, product.Description, product.IsActive);

            var response = await _httpClient.PutAsJsonAsync($"api/v1/products/{product.Id}", request, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
