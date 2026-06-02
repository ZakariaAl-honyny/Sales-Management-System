using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class CategoryApiService : ApiServiceBase, ICategoryApiService
{
    public CategoryApiService(HttpClient httpClient, ISessionService session) 
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<CategoryDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<CategoryDto>(
            () => _httpClient.GetAsync($"api/v1/categories?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "CategoryApiService.GetAllAsync");
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request)
    {
        return await ExecuteAsync<CategoryDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/categories", request),
            "CategoryApiService.CreateAsync");
    }

    public async Task<Result<CategoryDto>> UpdateAsync(int id, UpdateCategoryRequest request)
    {
        return await ExecuteAsync<CategoryDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/categories/{id}", request),
            "CategoryApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/categories/{id}"),
            "CategoryApiService.DeleteAsync");
    }

    public async Task<Result> DeletePermanentlyAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/categories/permanent/{id}"),
            "CategoryApiService.DeletePermanentlyAsync");
    }
}
