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

    // ─── Cash Box CRUD ───────────────────────────────────

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

    public async Task<Result<CashBoxDto>> UpdateAsync(int id, UpdateCashBoxRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<CashBoxDto>(
            () => _httpClient.PutAsJsonAsync($"api/v1/cash-boxes/{id}", request, ct),
            "CashBoxApiService.UpdateAsync");
    }

    public async Task<Result> DeactivateAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/cash-boxes/{id}", ct),
            "CashBoxApiService.DeactivateAsync");
    }

    // ─── Receipt Vouchers ────────────────────────────────

    public async Task<Result<ReceiptVoucherDto>> CreateReceiptVoucherAsync(int cashBoxId, CreateReceiptVoucherRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<ReceiptVoucherDto>(
            () => _httpClient.PostAsJsonAsync($"api/v1/cash-boxes/{cashBoxId}/receipt-vouchers", request, ct),
            "CashBoxApiService.CreateReceiptVoucherAsync");
    }

    public async Task<Result<ReceiptVoucherDto>> PostReceiptVoucherAsync(int cashBoxId, int voucherId, CancellationToken ct = default)
    {
        return await ExecuteAsync<ReceiptVoucherDto>(
            () => _httpClient.PostAsync($"api/v1/cash-boxes/{cashBoxId}/receipt-vouchers/{voucherId}/post", null, ct),
            "CashBoxApiService.PostReceiptVoucherAsync");
    }

    public async Task<Result<ReceiptVoucherDto>> CancelReceiptVoucherAsync(int cashBoxId, int voucherId, CancellationToken ct = default)
    {
        return await ExecuteAsync<ReceiptVoucherDto>(
            () => _httpClient.PostAsync($"api/v1/cash-boxes/{cashBoxId}/receipt-vouchers/{voucherId}/cancel", null, ct),
            "CashBoxApiService.CancelReceiptVoucherAsync");
    }

    public async Task<Result<List<ReceiptVoucherDto>>> GetReceiptVouchersAsync(int cashBoxId, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<ReceiptVoucherDto>>(
            () => _httpClient.GetAsync($"api/v1/cash-boxes/{cashBoxId}/receipt-vouchers", ct),
            "CashBoxApiService.GetReceiptVouchersAsync");
    }

    // ─── Payment Vouchers ────────────────────────────────

    public async Task<Result<PaymentVoucherDto>> CreatePaymentVoucherAsync(int cashBoxId, CreatePaymentVoucherRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<PaymentVoucherDto>(
            () => _httpClient.PostAsJsonAsync($"api/v1/cash-boxes/{cashBoxId}/payment-vouchers", request, ct),
            "CashBoxApiService.CreatePaymentVoucherAsync");
    }

    public async Task<Result<PaymentVoucherDto>> PostPaymentVoucherAsync(int cashBoxId, int voucherId, CancellationToken ct = default)
    {
        return await ExecuteAsync<PaymentVoucherDto>(
            () => _httpClient.PostAsync($"api/v1/cash-boxes/{cashBoxId}/payment-vouchers/{voucherId}/post", null, ct),
            "CashBoxApiService.PostPaymentVoucherAsync");
    }

    public async Task<Result<PaymentVoucherDto>> CancelPaymentVoucherAsync(int cashBoxId, int voucherId, CancellationToken ct = default)
    {
        return await ExecuteAsync<PaymentVoucherDto>(
            () => _httpClient.PostAsync($"api/v1/cash-boxes/{cashBoxId}/payment-vouchers/{voucherId}/cancel", null, ct),
            "CashBoxApiService.CancelPaymentVoucherAsync");
    }

    public async Task<Result<List<PaymentVoucherDto>>> GetPaymentVouchersAsync(int cashBoxId, CancellationToken ct = default)
    {
        return await ExecuteAsync<List<PaymentVoucherDto>>(
            () => _httpClient.GetAsync($"api/v1/cash-boxes/{cashBoxId}/payment-vouchers", ct),
            "CashBoxApiService.GetPaymentVouchersAsync");
    }

    // ─── Transfer ────────────────────────────────────────

    public async Task<Result> TransferAsync(CashTransferRequest request, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsJsonAsync("api/v1/cash-boxes/transfer", request, ct),
            "CashBoxApiService.TransferAsync");
    }
}
