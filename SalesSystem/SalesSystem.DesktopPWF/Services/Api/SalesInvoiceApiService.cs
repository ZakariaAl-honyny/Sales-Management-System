using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Sales Invoices
/// </summary>
public class SalesInvoiceApiService : ApiServiceBase, ISalesInvoiceApiService
{
    private const string BasePath = "api/v1/sales-invoices";

    public SalesInvoiceApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SalesInvoiceDto>>> GetAllAsync(
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

        var query = string.Join("&", queryParams);
        return await ExecutePagedAsync<SalesInvoiceDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "SalesInvoiceApiService.GetAllAsync");
    }

    public async Task<Result<SalesInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesInvoiceDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "SalesInvoiceApiService.GetByIdAsync");
    }

    public async Task<Result<SalesInvoiceDto>> GetByNumberAsync(string invoiceNo, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesInvoiceDto>(
            () => _httpClient.GetAsync($"{BasePath}/number/{invoiceNo}", ct),
            "SalesInvoiceApiService.GetByNumberAsync");
    }

    public async Task<Result<SalesInvoiceDto>> CreateAsync(CreateSalesInvoiceRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesInvoiceDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "SalesInvoiceApiService.CreateAsync");
    }

    public async Task<Result<SalesInvoiceDto>> UpdateAsync(int id, CreateSalesInvoiceRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesInvoiceDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "SalesInvoiceApiService.UpdateAsync");
    }

    public async Task<Result<SalesInvoiceDto>> PostAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesInvoiceDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/post", null, ct),
            "SalesInvoiceApiService.PostAsync");
    }

    public async Task<Result<SalesInvoiceDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesInvoiceDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/cancel", null, ct),
            "SalesInvoiceApiService.CancelAsync");
    }
}
