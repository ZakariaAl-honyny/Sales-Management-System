using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Returns;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class PurchaseReturnApiService : IPurchaseReturnApiService
{
    private readonly HttpClient _httpClient;
    public PurchaseReturnApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<PurchaseReturnDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<PurchaseReturnDto>>($"api/v1/returns/purchases?{query}", ct);
            return Result<IReadOnlyList<PurchaseReturnDto>>.Success(response ?? new List<PurchaseReturnDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<PurchaseReturnDto>>.Failure(ex.Message); }
    }

    public async Task<Result<PurchaseReturnDto>> CreateAsync(CreatePurchaseReturnRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/returns/purchases", request, ct);
            if (response.IsSuccessStatusCode) return Result<PurchaseReturnDto>.Success((await response.Content.ReadFromJsonAsync<PurchaseReturnDto>(cancellationToken: ct))!);
            return Result<PurchaseReturnDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<PurchaseReturnDto>.Failure(ex.Message); }
    }
}
