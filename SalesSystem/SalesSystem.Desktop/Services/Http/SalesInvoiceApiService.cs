using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Sales;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class SalesInvoiceApiService : ISalesInvoiceApiService
{
    private readonly HttpClient _httpClient;
    public SalesInvoiceApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<SalesInvoiceDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<SalesInvoiceDto>>($"api/v1/sales?{query}", ct);
            return Result<IReadOnlyList<SalesInvoiceDto>>.Success(response ?? new List<SalesInvoiceDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<SalesInvoiceDto>>.Failure(ex.Message); }
    }

    public async Task<Result<SalesInvoiceDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<SalesInvoiceDto>($"api/v1/sales/{id}", ct);
            return response != null ? Result<SalesInvoiceDto>.Success(response) : Result<SalesInvoiceDto>.Failure("NotFound");
        }
        catch (Exception ex) { return Result<SalesInvoiceDto>.Failure(ex.Message); }
    }

    public async Task<Result<SalesInvoiceDto>> CreateAsync(CreateSalesInvoiceRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/sales", request, ct);
            if (response.IsSuccessStatusCode) return Result<SalesInvoiceDto>.Success((await response.Content.ReadFromJsonAsync<SalesInvoiceDto>(cancellationToken: ct))!);
            return Result<SalesInvoiceDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<SalesInvoiceDto>.Failure(ex.Message); }
    }

    public async Task<Result> PostAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/sales/{id}/post", null, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }

    public async Task<Result> CancelAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/v1/sales/{id}/cancel", null, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
