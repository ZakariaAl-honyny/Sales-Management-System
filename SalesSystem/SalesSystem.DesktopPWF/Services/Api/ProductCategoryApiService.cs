using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Product Category API service implementation
/// </summary>
public class ProductCategoryApiService : ApiServiceBase, IProductCategoryApiService
{
    public ProductCategoryApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<ProductCategoryDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecuteAsync<List<ProductCategoryDto>>(
            () => _httpClient.GetAsync($"api/v1/product-categories?includeInactive={includeInactive.ToString().ToLower()}"),
            "ProductCategoryApiService.GetAllAsync");
    }

    public async Task<Result<ProductCategoryDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<ProductCategoryDto>(
            () => _httpClient.GetAsync($"api/v1/product-categories/{id}"),
            "ProductCategoryApiService.GetByIdAsync");
    }

    public async Task<Result<ProductCategoryDto>> CreateAsync(CreateProductCategoryRequest request)
    {
        return await ExecuteAsync<ProductCategoryDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/product-categories", request),
            "ProductCategoryApiService.CreateAsync");
    }

    public async Task<Result<ProductCategoryDto>> UpdateAsync(int id, UpdateProductCategoryRequest request)
    {
        return await ExecuteAsync<ProductCategoryDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/product-categories/{id}", request),
            "ProductCategoryApiService.UpdateAsync");
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/product-categories/{id}"),
            "ProductCategoryApiService.DeactivateAsync");
    }

    public async Task<Result> ReactivateAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/product-categories/{id}/reactivate", null),
            "ProductCategoryApiService.ReactivateAsync");
    }
}
