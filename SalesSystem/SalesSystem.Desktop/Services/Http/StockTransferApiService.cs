using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Inventory;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class StockTransferApiService : IStockTransferApiService
{
    private readonly HttpClient _httpClient;
    public StockTransferApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<StockTransferDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<StockTransferDto>>($"api/v1/inventory/transfers?{query}", ct);
            return Result<IReadOnlyList<StockTransferDto>>.Success(response ?? new List<StockTransferDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<StockTransferDto>>.Failure(ex.Message); }
    }

    public async Task<Result<StockTransferDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<StockTransferDto>($"api/v1/inventory/transfers/{id}", ct);
            return response != null ? Result<StockTransferDto>.Success(response) : Result<StockTransferDto>.Failure("NotFound");
        }
        catch (Exception ex) { return Result<StockTransferDto>.Failure(ex.Message); }
    }

    public async Task<Result<StockTransferDto>> CreateAsync(CreateStockTransferRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/inventory/transfers", request, ct);
            if (response.IsSuccessStatusCode) return Result<StockTransferDto>.Success((await response.Content.ReadFromJsonAsync<StockTransferDto>(cancellationToken: ct))!);
            return Result<StockTransferDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<StockTransferDto>.Failure(ex.Message); }
    }
}
