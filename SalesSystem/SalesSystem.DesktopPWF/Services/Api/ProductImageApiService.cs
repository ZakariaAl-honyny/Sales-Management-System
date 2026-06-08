using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public class ProductImageApiService : ApiServiceBase, IProductImageApiService
{
    private const string BasePath = "api/v1/product-images";

    public ProductImageApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<ProductImageDto>>> GetByProductAsync(int productId, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<ProductImageDto>>(
            () => _httpClient.GetAsync($"api/v1/products/{productId}/images", ct),
            "ProductImageApiService.GetByProductAsync");
    }

    public async Task<Result<ProductImageDto>> CreateAsync(CreateProductImageRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ProductImageDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "ProductImageApiService.CreateAsync");
    }

    public async Task<Result> SetPrimaryAsync(int productId, int imageId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PutAsync($"api/v1/products/{productId}/images/{imageId}/primary", null, ct),
            "ProductImageApiService.SetPrimaryAsync");
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "ProductImageApiService.DeactivateAsync");
    }
}
