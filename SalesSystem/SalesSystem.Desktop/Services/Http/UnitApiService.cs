using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Units;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class UnitApiService : IUnitApiService
{
    private readonly HttpClient _httpClient;
    public UnitApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<UnitDto>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<UnitDto>>("api/v1/units", ct);
            return Result<IReadOnlyList<UnitDto>>.Success(response ?? new List<UnitDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<UnitDto>>.Failure(ex.Message); }
    }

    public async Task<Result<UnitDto>> CreateAsync(string name, string? symbol, CancellationToken ct = default)
    {
        try
        {
            var request = new CreateUnitRequest(name, symbol);
            var response = await _httpClient.PostAsJsonAsync("api/v1/units", request, ct);
            if (response.IsSuccessStatusCode) return Result<UnitDto>.Success((await response.Content.ReadFromJsonAsync<UnitDto>(cancellationToken: ct))!);
            return Result<UnitDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<UnitDto>.Failure(ex.Message); }
    }

    public async Task<Result> UpdateAsync(int id, string name, string? symbol, bool isActive, CancellationToken ct = default)
    {
        try
        {
            var request = new UpdateUnitRequest(id, name, symbol, isActive);
            var response = await _httpClient.PutAsJsonAsync($"api/v1/units/{id}", request, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
