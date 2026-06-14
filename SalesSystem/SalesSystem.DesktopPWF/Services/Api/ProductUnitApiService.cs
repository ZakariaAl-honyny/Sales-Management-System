using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public class ProductUnitApiService : ApiServiceBase, IProductUnitApiService
{
    public ProductUnitApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<ProductUnitDto>>> GetByProductIdAsync(int productId)
    {
        return await ExecutePagedAsync<ProductUnitDto>(
            () => _httpClient.GetAsync($"api/v1/products/{productId}/units"),
            "ProductUnitApiService.GetByProductIdAsync");
    }

    public async Task<Result<ProductUnitDto>> AddUnitAsync(int productId, AddProductUnitRequest request)
    {
        return await ExecuteAsync<ProductUnitDto>(
            () => _httpClient.PostAsJsonAsync($"api/v1/products/{productId}/units", request),
            "ProductUnitApiService.AddUnitAsync");
    }

    public async Task<Result<ProductUnitDto>> UpdateUnitAsync(int productId, int unitId, UpdateProductUnitRequest request)
    {
        return await ExecuteAsync<ProductUnitDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/products/{productId}/units/{unitId}", request),
            "ProductUnitApiService.UpdateUnitAsync");
    }

    public async Task<Result> DeleteUnitAsync(int productId, int unitId, DeleteStrategy strategy)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/products/{productId}/units/{unitId}?strategy={(int)strategy}"),
            "ProductUnitApiService.DeleteUnitAsync");
    }

    public async Task<Result<BarcodeResolutionDto>> ResolveBarCodeAsync(string barcode)
    {
        return await ExecuteAsync<BarcodeResolutionDto>(
            () => _httpClient.GetAsync($"api/v1/barcodes/{Uri.EscapeDataString(barcode)}"),
            "ProductUnitApiService.ResolveBarCodeAsync");
    }
}
