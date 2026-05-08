using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Returns;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class SalesReturnApiService : ISalesReturnApiService
{
    private readonly HttpClient _httpClient;
    public SalesReturnApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<SalesReturnDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<SalesReturnDto>>($"api/v1/returns/sales?{query}", ct);
            return Result<IReadOnlyList<SalesReturnDto>>.Success(response ?? new List<SalesReturnDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<SalesReturnDto>>.Failure(ex.Message); }
    }

    public async Task<Result<SalesReturnDto>> CreateAsync(CreateSalesReturnRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/returns/sales", request, ct);
            if (response.IsSuccessStatusCode) return Result<SalesReturnDto>.Success((await response.Content.ReadFromJsonAsync<SalesReturnDto>(cancellationToken: ct))!);
            return Result<SalesReturnDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<SalesReturnDto>.Failure(ex.Message); }
    }
}
