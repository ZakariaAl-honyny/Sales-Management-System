using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Supplier API service implementation
/// </summary>
public class SupplierApiService : ApiServiceBase, ISupplierApiService
{
    public SupplierApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SupplierDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<SupplierDto>(
            () => _httpClient.GetAsync($"api/v1/suppliers?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "SupplierApiService.GetAllAsync");
    }

    public async Task<Result<SupplierDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<SupplierDto>(
            () => _httpClient.GetAsync($"api/v1/suppliers/{id}"),
            "SupplierApiService.GetByIdAsync");
    }

    public async Task<Result<SupplierDto>> CreateAsync(CreateSupplierRequest request)
    {
        return await ExecuteAsync<SupplierDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/suppliers", request),
            "SupplierApiService.CreateAsync");
    }

    public async Task<Result<SupplierDto>> UpdateAsync(int id, UpdateSupplierRequest request)
    {
        return await ExecuteAsync<SupplierDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/suppliers/{id}", request),
            "SupplierApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/suppliers/{id}"),
            "SupplierApiService.DeleteAsync");
    }

    public async Task<Result> DeletePermanentlyAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/suppliers/{id}/permanent"),
            "SupplierApiService.DeletePermanentlyAsync");
    }
}

/// <summary>
/// Warehouse API service implementation
/// </summary>
public class WarehouseApiService : ApiServiceBase, IWarehouseApiService
{
    public WarehouseApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<WarehouseDto>>> GetAllAsync(bool includeInactive = false)
    {
        return await ExecutePagedAsync<WarehouseDto>(
            () => _httpClient.GetAsync($"api/v1/warehouses?includeInactive={includeInactive.ToString().ToLower()}&pageSize=1000"),
            "WarehouseApiService.GetAllAsync");
    }

    public async Task<Result<WarehouseDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<WarehouseDto>(
            () => _httpClient.GetAsync($"api/v1/warehouses/{id}"),
            "WarehouseApiService.GetByIdAsync");
    }

    public async Task<Result<WarehouseDto>> CreateAsync(CreateWarehouseRequest request)
    {
        return await ExecuteAsync<WarehouseDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/warehouses", request),
            "WarehouseApiService.CreateAsync");
    }

    public async Task<Result<WarehouseDto>> UpdateAsync(int id, UpdateWarehouseRequest request)
    {
        return await ExecuteAsync<WarehouseDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/warehouses/{id}", request),
            "WarehouseApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/warehouses/{id}"),
            "WarehouseApiService.DeleteAsync");
    }

    public async Task<Result> DeletePermanentlyAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/warehouses/{id}/permanent"),
            "WarehouseApiService.DeletePermanentlyAsync");
    }
}
