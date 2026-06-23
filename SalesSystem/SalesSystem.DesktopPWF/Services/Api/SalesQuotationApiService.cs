using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Sales Quotations
/// </summary>
public class SalesQuotationApiService : ApiServiceBase, ISalesQuotationApiService
{
    private const string BasePath = "api/v1/sales-quotations";

    public SalesQuotationApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SalesQuotationDto>>> GetAllAsync(
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        byte? status = null,
        int? customerId = null,
        bool includeInactive = false,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}",
            $"includeInactive={includeInactive}"
        };

        if (!string.IsNullOrEmpty(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (from.HasValue)
            queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)
            queryParams.Add($"to={to.Value:yyyy-MM-dd}");
        if (status.HasValue)
            queryParams.Add($"status={status.Value}");
        if (customerId.HasValue)
            queryParams.Add($"customerId={customerId.Value}");

        var query = string.Join("&", queryParams);
        return await ExecutePagedAsync<SalesQuotationDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "SalesQuotationApiService.GetAllAsync");
    }

    public async Task<Result<SalesQuotationDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "SalesQuotationApiService.GetByIdAsync");
    }

    public async Task<Result<SalesQuotationDto>> CreateAsync(CreateSalesQuotationRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "SalesQuotationApiService.CreateAsync");
    }

    public async Task<Result<SalesQuotationDto>> UpdateAsync(int id, UpdateSalesQuotationRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "SalesQuotationApiService.UpdateAsync");
    }

    public async Task<Result<SalesQuotationDto>> SendAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/send", null, ct),
            "SalesQuotationApiService.SendAsync");
    }

    public async Task<Result<SalesQuotationDto>> AcceptAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/accept", null, ct),
            "SalesQuotationApiService.AcceptAsync");
    }

    public async Task<Result<SalesQuotationDto>> RejectAsync(int id, string? reason, CancellationToken ct = default)
    {
        var request = reason != null ? new { reason } : null;
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PostAsJsonAsync($"{BasePath}/{id}/reject", request, ct),
            "SalesQuotationApiService.RejectAsync");
    }

    public async Task<Result<SalesQuotationDto>> ConvertToInvoiceAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/convert", null, ct),
            "SalesQuotationApiService.ConvertToInvoiceAsync");
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"{BasePath}/{id}/cancel", null, ct),
            "SalesQuotationApiService.CancelAsync");
    }
}
