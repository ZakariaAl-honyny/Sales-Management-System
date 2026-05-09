using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Api.Interfaces;

namespace SalesSystem.Desktop.Services.Api;

public sealed class SalesInvoiceApiService : ISalesInvoiceApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/sales-invoices";

    public SalesInvoiceApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<SalesInvoiceDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, byte? status = null, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrEmpty(search)) query["search"] = search;
        if (from.HasValue) query["from"] = from.Value.ToString("yyyy-MM-dd");
        if (to.HasValue) query["to"] = to.Value.ToString("yyyy-MM-dd");
        if (status.HasValue) query["status"] = status.Value.ToString();

        return await _http.GetListAsync<SalesInvoiceDto>($"{BasePath}?{query}", ct);
    }

    public async Task<Result<SalesInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetAsync<SalesInvoiceDto>($"{BasePath}/{id}", ct);
    }

    public async Task<Result<SalesInvoiceDto>> CreateAsync(CreateSalesInvoiceRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<SalesInvoiceDto>(BasePath, r, ct);
    }

    public async Task<Result<SalesInvoiceDto>> UpdateAsync(int id, CreateSalesInvoiceRequest r, CancellationToken ct = default)
    {
        return await _http.PutAsync<SalesInvoiceDto>($"{BasePath}/{id}", r, ct);
    }

    public async Task<Result> PostAsync(int id, CancellationToken ct = default)
    {
        return await _http.PostAsync<object>($"{BasePath}/{id}/post", new { }, ct);
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct = default)
    {
        return await _http.PostAsync<object>($"{BasePath}/{id}/cancel", new { }, ct);
    }
}
