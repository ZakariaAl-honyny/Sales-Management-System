using System.Net.Http.Json;
using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests.Payments;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class SupplierPaymentApiService : ISupplierPaymentApiService
{
    private readonly HttpClient _httpClient;
    public SupplierPaymentApiService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<Result<IReadOnlyList<SupplierPaymentDto>>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        try
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (!string.IsNullOrEmpty(search)) query["search"] = search;
            var response = await _httpClient.GetFromJsonAsync<IReadOnlyList<SupplierPaymentDto>>("api/v1/payments/suppliers", ct);
            return Result<IReadOnlyList<SupplierPaymentDto>>.Success(response ?? new List<SupplierPaymentDto>());
        }
        catch (Exception ex) { return Result<IReadOnlyList<SupplierPaymentDto>>.Failure(ex.Message); }
    }

    public async Task<Result<SupplierPaymentDto>> CreateAsync(CreateSupplierPaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/payments/suppliers", request, ct);
            if (response.IsSuccessStatusCode) return Result<SupplierPaymentDto>.Success((await response.Content.ReadFromJsonAsync<SupplierPaymentDto>(cancellationToken: ct))!);
            return Result<SupplierPaymentDto>.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result<SupplierPaymentDto>.Failure(ex.Message); }
    }
}
