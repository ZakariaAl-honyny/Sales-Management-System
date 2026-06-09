using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service for payment allocation management (Phase 29).
/// </summary>
public interface IPaymentAllocationApiService
{
    Task<Result<List<PaymentAllocationDto>>> GetAllocationsForPaymentAsync(int paymentId, byte paymentType, CancellationToken ct = default);
    Task<Result> UpdateAllocationsAsync(int paymentId, byte paymentType, UpdateAllocationsRequest request, CancellationToken ct = default);
}

/// <summary>
/// Implementation of IPaymentAllocationApiService following CustomerPaymentApiService pattern.
/// </summary>
public class PaymentAllocationApiService : ApiServiceBase, IPaymentAllocationApiService
{
    private const string BasePath = "api/v1/payment-allocations";

    public PaymentAllocationApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<PaymentAllocationDto>>> GetAllocationsForPaymentAsync(
        int paymentId,
        byte paymentType,
        CancellationToken ct = default)
    {
        return await ExecuteAsync<List<PaymentAllocationDto>>(
            () => _httpClient.GetAsync($"{BasePath}?paymentId={paymentId}&paymentType={paymentType}", ct),
            "PaymentAllocationApiService.GetAllocationsForPaymentAsync");
    }

    public async Task<Result> UpdateAllocationsAsync(
        int paymentId,
        byte paymentType,
        UpdateAllocationsRequest request,
        CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PutAsJsonAsync($"{BasePath}?paymentId={paymentId}&paymentType={paymentType}", request, ct),
            "PaymentAllocationApiService.UpdateAllocationsAsync");
    }
}
