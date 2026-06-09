using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for cheque management (Phase 29).
/// </summary>
public interface IChequeApiService
{
    Task<Result<List<ChequeDto>>> GetAllAsync(int? paymentId = null, byte? status = null, CancellationToken ct = default);
    Task<Result<ChequeDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<ChequeDto>> CreateAsync(CreateChequeRequest request, CancellationToken ct = default);
    Task<Result<ChequeDto>> UpdateStatusAsync(int id, UpdateChequeStatusRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Implementation of IChequeApiService following CustomerPaymentApiService pattern.
/// </summary>
public class ChequeApiService : ApiServiceBase, IChequeApiService
{
    private const string BasePath = "api/v1/cheques";

    public ChequeApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<ChequeDto>>> GetAllAsync(
        int? paymentId = null,
        byte? status = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>();
        if (paymentId.HasValue)
            queryParams.Add($"paymentId={paymentId.Value}");
        if (status.HasValue)
            queryParams.Add($"status={status.Value}");

        var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        return await ExecuteAsync<List<ChequeDto>>(
            () => _httpClient.GetAsync($"{BasePath}{query}", ct),
            "ChequeApiService.GetAllAsync");
    }

    public async Task<Result<ChequeDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<ChequeDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "ChequeApiService.GetByIdAsync");
    }

    public async Task<Result<ChequeDto>> CreateAsync(CreateChequeRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ChequeDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "ChequeApiService.CreateAsync");
    }

    public async Task<Result<ChequeDto>> UpdateStatusAsync(int id, UpdateChequeStatusRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ChequeDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}/status", request, ct),
            "ChequeApiService.UpdateStatusAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "ChequeApiService.DeleteAsync");
    }
}
