using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public class ProductPriceApiService : ApiServiceBase, IProductPriceApiService
{
    private const string BasePath = "api/v1/product-prices";

    public ProductPriceApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<ProductPriceDto>>> GetByProductUnitAsync(int productUnitId, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<ProductPriceDto>>(
            () => _httpClient.GetAsync($"{BasePath}/by-unit/{productUnitId}", ct),
            "ProductPriceApiService.GetByProductUnitAsync");
    }

    public async Task<Result<ProductPriceDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<ProductPriceDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "ProductPriceApiService.GetByIdAsync");
    }

    public async Task<Result<ProductPriceDto>> CreateAsync(CreateProductPriceRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ProductPriceDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "ProductPriceApiService.CreateAsync");
    }

    public async Task<Result<ProductPriceDto>> UpdateAsync(int id, UpdateProductPriceRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ProductPriceDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "ProductPriceApiService.UpdateAsync");
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "ProductPriceApiService.DeactivateAsync");
    }
}
