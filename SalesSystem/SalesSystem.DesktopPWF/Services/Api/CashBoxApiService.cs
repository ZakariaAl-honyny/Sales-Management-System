using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

public class CashBoxApiService : ApiServiceBase, ICashBoxApiService
{
    public CashBoxApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<CashBoxDto>>> GetAllAsync(CancellationToken ct = default)
    {
        return await ExecuteAsync<List<CashBoxDto>>(
            () => _httpClient.GetAsync("api/v1/cash-boxes", ct),
            "CashBoxApiService.GetAllAsync");
    }

    public async Task<Result<CashBoxDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<CashBoxDto>(
            () => _httpClient.GetAsync($"api/v1/cash-boxes/{id}", ct),
            "CashBoxApiService.GetByIdAsync");
    }

    public async Task<Result<CashBoxDto>> CreateAsync(CreateCashBoxRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<CashBoxDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/cash-boxes", request, ct),
            "CashBoxApiService.CreateAsync");
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/cash-boxes/{id}", ct),
            "CashBoxApiService.DeactivateAsync");
    }

    public async Task<Result<List<CashTransactionDto>>> GetTransactionsAsync(int cashBoxId, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var url = $"api/v1/cash-boxes/{cashBoxId}/transactions";
        var queryParams = new List<string>();
        if (from.HasValue)
            queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)
            queryParams.Add($"to={to.Value:yyyy-MM-dd}");
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        return await ExecuteAsync<List<CashTransactionDto>>(
            () => _httpClient.GetAsync(url, ct),
            "CashBoxApiService.GetTransactionsAsync");
    }

    public async Task<Result<CashTransactionDto>> RecordExpenseAsync(int cashBoxId, AddCashTransactionRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<CashTransactionDto>(
            () => _httpClient.PostAsJsonAsync($"api/v1/cash-boxes/{cashBoxId}/transactions", request, ct),
            "CashBoxApiService.RecordExpenseAsync");
    }

    public async Task<Result> TransferAsync(CashTransferRequest request, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsJsonAsync("api/v1/cash-boxes/transfer", request, ct),
            "CashBoxApiService.TransferAsync");
    }

    public async Task<Result<DailyClosureDto>> PerformDailyClosureAsync(int cashBoxId, CancellationToken ct = default)
    {
        return await ExecuteAsync<DailyClosureDto>(
            () => _httpClient.PostAsync($"api/v1/cash-boxes/{cashBoxId}/daily-closures", null, ct),
            "CashBoxApiService.PerformDailyClosureAsync");
    }

    public async Task<Result<List<DailyClosureDto>>> GetDailyClosuresAsync(int cashBoxId, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<DailyClosureDto>>(
            () => _httpClient.GetAsync($"api/v1/cash-boxes/{cashBoxId}/daily-closures", ct),
            "CashBoxApiService.GetDailyClosuresAsync");
    }
}
