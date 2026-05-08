using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Warehouses;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class WarehouseApiService : IWarehouseApiService
{
    private readonly HttpClient _httpClient;
    public WarehouseApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<WarehouseDto>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<WarehouseDto>>("api/v1/warehouses", ct);
            return Result<IReadOnlyList<WarehouseDto>>.Success(response ?? new List<WarehouseDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<WarehouseDto>>.Failure(ex.Message); }
    }

    public async Task<Result<WarehouseDto>> CreateAsync(string name, string? code, string? location, bool isDefault, CancellationToken ct = default)
    {
        try
        {
            var request = new CreateWarehouseRequest(code, name, location, isDefault);
            var response = await _httpClient.PostAsJsonAsync("api/v1/warehouses", request, ct);
            if (response.IsSuccessStatusCode) return Result<WarehouseDto>.Success((await response.Content.ReadFromJsonAsync<WarehouseDto>(cancellationToken: ct))!);
            return Result<WarehouseDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<WarehouseDto>.Failure(ex.Message); }
    }

    public async Task<Result> UpdateAsync(int id, string name, string? code, string? location, bool isDefault, bool isActive, CancellationToken ct = default)
    {
        try
        {
            var request = new UpdateWarehouseRequest(id, code, name, location, isDefault, isActive);
            var response = await _httpClient.PutAsJsonAsync($"api/v1/warehouses/{id}", request, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
