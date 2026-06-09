using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Journal Entry operations.
/// Follows the exact pattern from AccountApiService.cs using ApiServiceBase.
/// </summary>
public class JournalEntryApiService : ApiServiceBase, IJournalEntryApiService
{
    private const string BasePath = "api/v1/journal-entries";

    public JournalEntryApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<JournalEntryListDto>>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        return await ExecuteAsync<List<JournalEntryListDto>>(
            () => _httpClient.GetAsync($"{BasePath}?page={page}&pageSize={pageSize}"),
            "JournalEntryApiService.GetAllAsync");
    }

    public async Task<Result<JournalEntryDetailDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<JournalEntryDetailDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}"),
            "JournalEntryApiService.GetByIdAsync");
    }

    public async Task<Result<AccountBalanceDto>> GetBalanceAsync(int accountId, DateTime? asOfDate = null)
    {
        var url = asOfDate.HasValue
            ? $"{BasePath}/balance/{accountId}?asOfDate={asOfDate.Value:yyyy-MM-dd}"
            : $"{BasePath}/balance/{accountId}";
        return await ExecuteAsync<AccountBalanceDto>(
            () => _httpClient.GetAsync(url),
            "JournalEntryApiService.GetBalanceAsync");
    }

    public async Task<Result<AccountLedgerDto>> GetLedgerAsync(int accountId, DateTime startDate, DateTime endDate)
    {
        return await ExecuteAsync<AccountLedgerDto>(
            () => _httpClient.GetAsync($"{BasePath}/ledger/{accountId}?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}"),
            "JournalEntryApiService.GetLedgerAsync");
    }

    public async Task<Result<int>> CreateAsync(CreateJournalEntryRequest request)
    {
        return await ExecuteAsync<int>(
            () => _httpClient.PostAsJsonAsync(BasePath, request),
            "JournalEntryApiService.CreateAsync");
    }

    public async Task<Result<JournalEntryDetailDto>> PostAsync(int id)
    {
        return await ExecuteAsync<JournalEntryDetailDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}/post", new { }),
            "JournalEntryApiService.PostAsync");
    }

    public async Task<Result<JournalEntryDetailDto>> CancelAsync(int id)
    {
        return await ExecuteAsync<JournalEntryDetailDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}/cancel", new { }),
            "JournalEntryApiService.CancelAsync");
    }
}
