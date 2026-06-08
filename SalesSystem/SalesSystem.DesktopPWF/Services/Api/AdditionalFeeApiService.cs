using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

public class AdditionalFeeApiService : ApiServiceBase, IAdditionalFeeApiService
{
    private const string BasePath = "api/v1/additional-fees";

    public AdditionalFeeApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<AdditionalFeeDto>>> GetByInvoiceAsync(int purchaseInvoiceId, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<AdditionalFeeDto>>(
            () => _httpClient.GetAsync($"{BasePath}/by-invoice/{purchaseInvoiceId}", ct),
            "AdditionalFeeApiService.GetByInvoiceAsync");
    }

    public async Task<Result<AdditionalFeeDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<AdditionalFeeDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "AdditionalFeeApiService.GetByIdAsync");
    }

    public async Task<Result<AdditionalFeeDto>> CreateAsync(CreateAdditionalFeeRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<AdditionalFeeDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "AdditionalFeeApiService.CreateAsync");
    }

    public async Task<Result<AdditionalFeeDto>> UpdateAsync(int id, CreateAdditionalFeeRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<AdditionalFeeDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "AdditionalFeeApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "AdditionalFeeApiService.DeleteAsync");
    }
}
