using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Fiscal Year management.
/// Communicates with the FiscalYearsController at api/v1/fiscal-years.
/// </summary>
public class FiscalYearApiService : ApiServiceBase, IFiscalYearApiService
{
    private const string BasePath = "api/v1/fiscal-years";

    public FiscalYearApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<FiscalYearDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<List<FiscalYearDto>>(
            () => _httpClient.GetAsync(BasePath, ct),
            "FiscalYearApiService.GetAllAsync");
    }

    public async Task<Result<FiscalYearDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<FiscalYearDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "FiscalYearApiService.GetByIdAsync");
    }

    public async Task<Result<FiscalYearDto>> GetByYearAsync(int year, CancellationToken ct = default)
    {
        return await ExecuteAsync<FiscalYearDto>(
            () => _httpClient.GetAsync($"{BasePath}/by-year/{year}", ct),
            "FiscalYearApiService.GetByYearAsync");
    }

    public async Task<Result<FiscalYearDto>> CreateAsync(CreateFiscalYearRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<FiscalYearDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "FiscalYearApiService.CreateAsync");
    }

    public async Task<Result<FiscalYearDto>> OpenAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<FiscalYearDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}/open", new { }, ct),
            "FiscalYearApiService.OpenAsync");
    }

    public async Task<Result<FiscalYearDto>> CloseAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<FiscalYearDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}/close", new { }, ct),
            "FiscalYearApiService.CloseAsync");
    }
}
