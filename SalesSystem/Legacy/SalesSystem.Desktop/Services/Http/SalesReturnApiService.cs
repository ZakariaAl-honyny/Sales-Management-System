using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class SalesReturnApiService : ISalesReturnApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/sales-returns";

    public SalesReturnApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<SalesReturnDto>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null, int? customerId = null, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (!string.IsNullOrEmpty(search)) query["search"] = search;
        if (from.HasValue) query["from"] = from.Value.ToString("yyyy-MM-dd");
        if (to.HasValue) query["to"] = to.Value.ToString("yyyy-MM-dd");
        if (customerId.HasValue) query["customerId"] = customerId.Value.ToString();

        return await _http.GetListAsync<SalesReturnDto>($"{BasePath}?{query}", ct);
    }

    public async Task<Result<SalesReturnDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _http.GetAsync<SalesReturnDto>($"{BasePath}/{id}", ct);
    }

    public async Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<SalesReturnDto>(BasePath, r, ct);
    }

    public async Task<Result> PostAsync(int id, CancellationToken ct = default)
    {
        return await _http.PostAsync($"{BasePath}/{id}/post", null, ct);
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct = default)
    {
        return await _http.PostAsync($"{BasePath}/{id}/cancel", null, ct);
    }
}
