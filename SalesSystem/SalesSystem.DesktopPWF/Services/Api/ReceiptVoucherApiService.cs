using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Receipt Voucher CRUD operations.
/// Maps to ReceiptVouchersController at api/v1/receipt-vouchers.
/// </summary>
public class ReceiptVoucherApiService : ApiServiceBase, IReceiptVoucherApiService
{
    public ReceiptVoucherApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<PagedResult<ReceiptVoucherDto>>> GetAllAsync(
        string? search = null, DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 100, CancellationToken ct = default)
    {
        var query = $"api/v1/receipt-vouchers?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&search={Uri.EscapeDataString(search)}";
        if (from.HasValue)
            query += $"&from={from.Value:yyyy-MM-dd}";
        if (to.HasValue)
            query += $"&to={to.Value:yyyy-MM-dd}";

        return await ExecuteAsync<PagedResult<ReceiptVoucherDto>>(
            () => _httpClient.GetAsync(query, ct),
            "ReceiptVoucherApiService.GetAllAsync");
    }

    public async Task<Result<ReceiptVoucherDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<ReceiptVoucherDto>(
            () => _httpClient.GetAsync($"api/v1/receipt-vouchers/{id}", ct),
            "ReceiptVoucherApiService.GetByIdAsync");
    }

    public async Task<Result<ReceiptVoucherDto>> CreateAsync(CreateReceiptVoucherRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ReceiptVoucherDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/receipt-vouchers", request, ct),
            "ReceiptVoucherApiService.CreateAsync");
    }

    public async Task<Result<ReceiptVoucherDto>> UpdateAsync(int id, UpdateReceiptVoucherRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ReceiptVoucherDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/receipt-vouchers/{id}", request, ct),
            "ReceiptVoucherApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/receipt-vouchers/{id}", ct),
            "ReceiptVoucherApiService.DeleteAsync");
    }

    public async Task<Result<ReceiptVoucherDto>> PostAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<ReceiptVoucherDto>(
            () => _httpClient.PostAsync($"api/v1/receipt-vouchers/{id}/post", null, ct),
            "ReceiptVoucherApiService.PostAsync");
    }

    public async Task<Result<ReceiptVoucherDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<ReceiptVoucherDto>(
            () => _httpClient.PostAsync($"api/v1/receipt-vouchers/{id}/cancel", null, ct),
            "ReceiptVoucherApiService.CancelAsync");
    }
}
