using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Product API service implementation for WPF
/// </summary>
public class ProductApiService : ApiServiceBase, IProductApiService
{
    public ProductApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<ProductDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<ProductDto>(
            () => _httpClient.GetAsync($"api/v1/products?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "ProductApiService.GetAllAsync");
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<ProductDto>(
            () => _httpClient.GetAsync($"api/v1/products/{id}"),
            "ProductApiService.GetByIdAsync");
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request)
    {
        return await ExecuteAsync<ProductDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/products", request),
            "ProductApiService.CreateAsync");
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request)
    {
        return await ExecuteAsync<ProductDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/products/{id}", request),
            "ProductApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/products/{id}"),
            "ProductApiService.DeleteAsync");
    }

    public async Task<Result> DeletePermanentlyAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/products/permanent/{id}"),
            "ProductApiService.DeletePermanentlyAsync");
    }

    public async Task<Result<List<ProductDto>>> SearchAsync(string searchTerm)
    {
        return await ExecutePagedAsync<ProductDto>(
            () => _httpClient.GetAsync($"api/v1/products?search={Uri.EscapeDataString(searchTerm)}&pageSize=100"),
            "ProductApiService.SearchAsync");
    }

    public async Task<Result<ProductDto>> GetByBarcodeAsync(string barcode)
    {
        return await ExecuteAsync<ProductDto>(
            () => _httpClient.GetAsync($"api/v1/products/barcode/{barcode}"),
            "ProductApiService.GetByBarcodeAsync");
    }

    public async Task<Result<List<ProductDto>>> GetExpiringProductsAsync(int thresholdDays = 30)
    {
        return await ExecuteAsync<List<ProductDto>>(
            () => _httpClient.GetAsync($"api/v1/products/expiring?thresholdDays={thresholdDays}"),
            "ProductApiService.GetExpiringProductsAsync");
    }

    public async Task<Result<ProductDto>> UploadImageAsync(int productId, byte[] imageBytes, string fileName)
    {
        try
        {
            AddAuthHeader();
            using var content = new MultipartFormDataContent();
            content.Add(new ByteArrayContent(imageBytes), "image", fileName);

            var response = await _httpClient.PostAsync($"api/v1/products/{productId}/image", content);
            return await HandleResponseAsync<ProductDto>(response);
        }
        catch (Exception ex)
        {
            return HandleConnectionError<ProductDto>(ex, "ProductApiService.UploadImageAsync");
        }
    }
}
