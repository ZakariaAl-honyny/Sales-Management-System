using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for Payment Vouchers (سندات صرف)
/// </summary>
public class PaymentVoucherApiService : ApiServiceBase, IPaymentVoucherApiService
{
    public PaymentVoucherApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<PaymentVoucherDto>>> GetAllAsync(
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

        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (from.HasValue)
            queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)
            queryParams.Add($"to={to.Value:yyyy-MM-dd}");

        var query = string.Join("&", queryParams);
        return await ExecutePagedAsync<PaymentVoucherDto>(
            () => _httpClient.GetAsync($"api/v1/payment-vouchers?{query}", ct),
            "PaymentVoucherApiService.GetAllAsync");
    }

    public async Task<Result<PaymentVoucherDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PaymentVoucherDto>(
            () => _httpClient.GetAsync($"api/v1/payment-vouchers/{id}", ct),
            "PaymentVoucherApiService.GetByIdAsync");
    }

    public async Task<Result<PaymentVoucherDto>> CreateAsync(CreatePaymentVoucherRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<PaymentVoucherDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/payment-vouchers", request, ct),
            "PaymentVoucherApiService.CreateAsync");
    }

    public async Task<Result<PaymentVoucherDto>> UpdateAsync(int id, UpdatePaymentVoucherRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<PaymentVoucherDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/payment-vouchers/{id}", request, ct),
            "PaymentVoucherApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/payment-vouchers/{id}", ct),
            "PaymentVoucherApiService.DeleteAsync");
    }

    public async Task<Result<PaymentVoucherDto>> PostAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PaymentVoucherDto>(
            () => _httpClient.PostAsync($"api/v1/payment-vouchers/{id}/post", null, ct),
            "PaymentVoucherApiService.PostAsync");
    }

    public async Task<Result<PaymentVoucherDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<PaymentVoucherDto>(
            () => _httpClient.PostAsync($"api/v1/payment-vouchers/{id}/cancel", null, ct),
            "PaymentVoucherApiService.CancelAsync");
    }
}
