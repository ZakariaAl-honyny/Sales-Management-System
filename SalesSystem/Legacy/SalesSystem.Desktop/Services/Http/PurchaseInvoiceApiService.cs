using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class PurchaseInvoiceApiService : IPurchaseInvoiceApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/purchase-invoices";

    public PurchaseInvoiceApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<PurchaseInvoiceDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, byte? status = null, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrEmpty(search)) query["search"] = search;
        if (from.HasValue) query["from"] = from.Value.ToString("yyyy-MM-dd");
        if (to.HasValue) query["to"] = to.Value.ToString("yyyy-MM-dd");
        if (status.HasValue) query["status"] = status.Value.ToString();

        return await _http.GetListAsync<PurchaseInvoiceDto>($"{BasePath}?{query}", ct);
    }

    public async Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetAsync<PurchaseInvoiceDto>($"{BasePath}/{id}", ct);
    }

    public async Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<PurchaseInvoiceDto>(BasePath, r, ct);
    }

    public async Task<Result<PurchaseInvoiceDto>> UpdateAsync(int id, CreatePurchaseInvoiceRequest r, CancellationToken ct = default)
    {
        return await _http.PutAsync<PurchaseInvoiceDto>($"{BasePath}/{id}", r, ct);
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
