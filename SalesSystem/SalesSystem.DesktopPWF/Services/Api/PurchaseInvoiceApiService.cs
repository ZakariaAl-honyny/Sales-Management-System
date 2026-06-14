using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Purchase Invoices
/// </summary>
public class PurchaseInvoiceApiService : ApiServiceBase, IPurchaseInvoiceApiService
{
    private const string BasePath = "api/v1/purchase-invoices";

    public PurchaseInvoiceApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<PurchaseInvoiceDto>>> GetAllAsync(
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        byte? status = null,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 100,
        int? supplierId = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrEmpty(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (from.HasValue)
            queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)
            queryParams.Add($"to={to.Value:yyyy-MM-dd}");
        if (status.HasValue)
            queryParams.Add($"status={status.Value}");
        if (includeInactive)
            queryParams.Add($"includeInactive=true");
        if (supplierId.HasValue)
            queryParams.Add($"supplierId={supplierId.Value}");

        var query = string.Join("&", queryParams);
        return await ExecutePagedAsync<PurchaseInvoiceDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "PurchaseInvoiceApiService.GetAllAsync");
    }

    public async Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseInvoiceDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "PurchaseInvoiceApiService.GetByIdAsync");
    }

    public async Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseInvoiceDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "PurchaseInvoiceApiService.CreateAsync");
    }

    public async Task<Result<PurchaseInvoiceDto>> UpdateAsync(int id, UpdatePurchaseInvoiceRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseInvoiceDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "PurchaseInvoiceApiService.UpdateAsync");
    }

    public async Task<Result<PurchaseInvoiceDto>> PostAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseInvoiceDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/post", null, ct),
            "PurchaseInvoiceApiService.PostAsync");
    }

    public async Task<Result<PurchaseInvoiceDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PurchaseInvoiceDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/cancel", null, ct),
            "PurchaseInvoiceApiService.CancelAsync");
    }

    public async Task<Result<string>> UploadAttachmentAsync(int id, string base64Content, string fileName, CancellationToken ct = default)
    {
        var request = new { base64Content, fileName };
        return await ExecuteAsync<string>(
            () => _httpClient.PostAsJsonAsync($"{BasePath}/{id}/upload-attachment", request, ct),
            "PurchaseInvoiceApiService.UploadAttachmentAsync");
    }

    public async Task<Result> DeleteAttachmentAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}/attachment", ct),
            "PurchaseInvoiceApiService.DeleteAttachmentAsync");
    }
}
