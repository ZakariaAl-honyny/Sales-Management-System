using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Expense API service implementation
/// Follows ExpensesController.cs API pattern.
/// </summary>
public class ExpenseApiService : ApiServiceBase, IExpenseApiService
{
    private const string BasePath = "api/v1/expenses";

    public ExpenseApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<ExpenseDto>>> GetAllAsync(
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrEmpty(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (from.HasValue)
            queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)
            queryParams.Add($"to={to.Value:yyyy-MM-dd}");

        var query = string.Join("&", queryParams);
        return await ExecutePagedAsync<ExpenseDto>(
            () => _httpClient.GetAsync($"{BasePath}?{query}", ct),
            "ExpenseApiService.GetAllAsync");
    }

    public async Task<Result<ExpenseDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<ExpenseDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "ExpenseApiService.GetByIdAsync");
    }

    public async Task<Result<ExpenseDto>> CreateAsync(CreateExpenseRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ExpenseDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "ExpenseApiService.CreateAsync");
    }

    public async Task<Result<ExpenseDto>> UpdateAsync(int id, UpdateExpenseRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ExpenseDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "ExpenseApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "ExpenseApiService.DeleteAsync");
    }

    public async Task<Result<ExpenseDto>> PostAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<ExpenseDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/post", null, ct),
            "ExpenseApiService.PostAsync");
    }

    public async Task<Result<ExpenseDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<ExpenseDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/cancel", null, ct),
            "ExpenseApiService.CancelAsync");
    }
}
