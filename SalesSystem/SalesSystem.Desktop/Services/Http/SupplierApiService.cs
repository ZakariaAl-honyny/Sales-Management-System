using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Suppliers;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class SupplierApiService : ISupplierApiService
{
    private readonly HttpClient _httpClient;
    public SupplierApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<SupplierDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            var path = $"api/v1/suppliers?{query}";
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<SupplierDto>>(path, ct);
            return Result<IReadOnlyList<SupplierDto>>.Success(response ?? new List<SupplierDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<SupplierDto>>.Failure(ex.Message); }
    }

    public async Task<Result<SupplierDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<SupplierDto>($"api/v1/suppliers/{id}", ct);
            return response != null ? Result<SupplierDto>.Success(response) : Result<SupplierDto>.Failure("NotFound");
        }
        catch (Exception ex) { return Result<SupplierDto>.Failure(ex.Message); }
    }

    public async Task<Result<SupplierDto>> CreateAsync(string name, string? phone, string? email, string? address, decimal openingBalance, CancellationToken ct = default)
    {
        try
        {
            var request = new CreateSupplierRequest(null, name, phone, email, address, openingBalance);
            var response = await _httpClient.PostAsJsonAsync("api/v1/suppliers", request, ct);
            if (response.IsSuccessStatusCode) return Result<SupplierDto>.Success((await response.Content.ReadFromJsonAsync<SupplierDto>(cancellationToken: ct))!);
            return Result<SupplierDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<SupplierDto>.Failure(ex.Message); }
    }

    public async Task<Result> UpdateAsync(int id, string name, string? phone, string? email, string? address, bool isActive, CancellationToken ct = default)
    {
        try
        {
            var request = new UpdateSupplierRequest(id, null, name, phone, email, address, isActive);
            var response = await _httpClient.PutAsJsonAsync($"api/v1/suppliers/{id}", request, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
