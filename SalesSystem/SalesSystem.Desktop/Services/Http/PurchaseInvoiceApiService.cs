using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Purchases;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class PurchaseInvoiceApiService : IPurchaseInvoiceApiService
{
    private readonly HttpClient _httpClient;
    public PurchaseInvoiceApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<PurchaseInvoiceDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<PurchaseInvoiceDto>>($"api/v1/purchases?{query}", ct);
            return Result<IReadOnlyList<PurchaseInvoiceDto>>.Success(response ?? new List<PurchaseInvoiceDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<PurchaseInvoiceDto>>.Failure(ex.Message); }
    }

    public async Task<Result<PurchaseInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<PurchaseInvoiceDto>($"api/v1/purchases/{id}", ct);
            return response != null ? Result<PurchaseInvoiceDto>.Success(response) : Result<PurchaseInvoiceDto>.Failure("NotFound");
        }
        catch (Exception ex) { return Result<PurchaseInvoiceDto>.Failure(ex.Message); }
    }

    public async Task<Result<PurchaseInvoiceDto>> CreateAsync(CreatePurchaseInvoiceRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/purchases", request, ct);
            if (response.IsSuccessStatusCode) return Result<PurchaseInvoiceDto>.Success((await response.Content.ReadFromJsonAsync<PurchaseInvoiceDto>(cancellationToken: ct))!);
            return Result<PurchaseInvoiceDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<PurchaseInvoiceDto>.Failure(ex.Message); }
    }

    public async Task<Result> PostAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/purchases/{id}/post", null, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/purchases/{id}/cancel", null, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
