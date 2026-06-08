using System.Net;
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

    public async Task<Result<PurchaseInvoiceDto>> UpdateAsync(int id, CreatePurchaseInvoiceRequest request, CancellationToken ct = default)
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

    public async Task<Result> UploadAttachmentAsync(int id, byte[] fileData, string fileName, CancellationToken ct = default)
    {
        try
        {
            AddAuthHeader();
            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(fileData);
            content.Add(fileContent, "file", fileName);
            var response = await _httpClient.PostAsync($"{BasePath}/{id}/attachment", content, ct);
            return await HandleResponseAsync(response);
        }
        catch (Exception ex)
        {
            return HandleConnectionError(ex, "PurchaseInvoiceApiService.UploadAttachmentAsync");
        }
    }

    public async Task<Result<byte[]>> DownloadAttachmentAsync(int id, CancellationToken ct = default)
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.GetAsync($"{BasePath}/{id}/attachment", ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadAsByteArrayAsync();
                return Result<byte[]>.Success(data);
            }
            try
            {
                if (response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: ct);
                    Serilog.Log.Warning("API failure: {StatusCode} - {Error} ({ErrorCode})", response.StatusCode, error?.Error, error?.ErrorCode);
                    return Result<byte[]>.Failure(error?.Error ?? "حدث خطأ", error?.ErrorCode ?? "Unknown");
                }
                var content = await response.Content.ReadAsStringAsync(ct);
                Serilog.Log.Warning("API failure (non-JSON): {StatusCode} - {Content}", response.StatusCode, content);
                return Result<byte[]>.Failure($"خطأ في الخادم: {response.StatusCode}", response.StatusCode.ToString());
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Unexpected error parsing API error response. StatusCode: {StatusCode}", response.StatusCode);
                return Result<byte[]>.Failure("حدث خطأ غير متوقع", "Unknown");
            }
        }
        catch (Exception ex)
        {
            return HandleConnectionError<byte[]>(ex, "PurchaseInvoiceApiService.DownloadAttachmentAsync");
        }
    }
}
