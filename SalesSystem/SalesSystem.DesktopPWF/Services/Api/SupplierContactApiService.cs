using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class SupplierContactApiService : ApiServiceBase, ISupplierContactApiService
{
    public SupplierContactApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SupplierContactDto>>> GetAllAsync(int supplierId)
    {
        return await ExecuteAsync<List<SupplierContactDto>>(
            () => _httpClient.GetAsync($"api/v1/supplier-contacts?supplierId={supplierId}"),
            "SupplierContactApiService.GetAllAsync");
    }

    public async Task<Result<SupplierContactDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<SupplierContactDto>(
            () => _httpClient.GetAsync($"api/v1/supplier-contacts/{id}"),
            "SupplierContactApiService.GetByIdAsync");
    }

    public async Task<Result<SupplierContactDto>> CreateAsync(CreateSupplierContactRequest request)
    {
        return await ExecuteAsync<SupplierContactDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/supplier-contacts", request),
            "SupplierContactApiService.CreateAsync");
    }

    public async Task<Result<SupplierContactDto>> UpdateAsync(int id, UpdateSupplierContactRequest request)
    {
        return await ExecuteAsync<SupplierContactDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/supplier-contacts/{id}", request),
            "SupplierContactApiService.UpdateAsync");
    }

    public async Task<Result> DeactivateAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/supplier-contacts/{id}"),
            "SupplierContactApiService.DeactivateAsync");
    }
}
