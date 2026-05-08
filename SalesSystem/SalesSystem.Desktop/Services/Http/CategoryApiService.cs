using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Categories;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class CategoryApiService : ICategoryApiService
{
    private readonly HttpClient _httpClient;

    public CategoryApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<CategoryDto>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("api/v1/categories", ct);
            return Result<IReadOnlyList<CategoryDto>>.Success(response ?? new List<CategoryDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<CategoryDto>>.Failure(ex.Message); }
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<CategoryDto>($"api/v1/categories/{id}", ct);
            return response != null ? Result<CategoryDto>.Success(response) : Result<CategoryDto>.Failure("NotFound");
        }
        catch (Exception ex) { return Result<CategoryDto>.Failure(ex.Message); }
    }

    public async Task<Result<CategoryDto>> CreateAsync(string name, string? description, CancellationToken ct = default)
    {
        try
        {
            var request = new CreateCategoryRequest(name, description);
            var response = await _httpClient.PostAsJsonAsync("api/v1/categories", request, ct);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CategoryDto>(cancellationToken: ct);
                return Result<CategoryDto>.Success(data!);
            }
            return Result<CategoryDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<CategoryDto>.Failure(ex.Message); }
    }

    public async Task<Result> UpdateAsync(int id, string name, string? description, bool isActive, CancellationToken ct = default)
    {
        try
        {
            var request = new UpdateCategoryRequest(id, name, description, isActive);
            var response = await _httpClient.PutAsJsonAsync($"api/v1/categories/{id}", request, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
