using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.DTOs;
using Serilog;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service implementation for product import operations.
/// Uses raw HttpClient (no ApiServiceBase) since import endpoints return
/// plain DTOs rather than Result-wrapped responses.
/// </summary>
public class ProductImportApiService : IProductImportApiService
{
    private readonly HttpClient _http;

    public ProductImportApiService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    public async Task<ProductImportResultDto?> PreviewAsync(List<ProductImportRowDto> rows)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/products/import/preview", rows);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductImportResultDto>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ProductImportApiService.PreviewAsync failed");
            return null;
        }
    }

    public async Task<ProductImportResultDto?> ExecuteAsync(List<ProductImportRowDto> rows)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/products/import/execute", rows);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductImportResultDto>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ProductImportApiService.ExecuteAsync failed");
            return null;
        }
    }

    public async Task<byte[]?> DownloadTemplateAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/v1/products/import/template");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "ProductImportApiService.DownloadTemplateAsync failed");
            return null;
        }
    }
}
