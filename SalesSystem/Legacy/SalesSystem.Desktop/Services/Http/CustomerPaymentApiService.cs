using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class CustomerPaymentApiService : ICustomerPaymentApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/customer-payments";

    public CustomerPaymentApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<CustomerPaymentDto>>> GetAllAsync(int? customerId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (customerId.HasValue) query["customerId"] = customerId.Value.ToString();
        if (from.HasValue) query["dateFrom"] = from.Value.ToString("o");
        if (to.HasValue) query["dateTo"] = to.Value.ToString("o");

        return await _http.GetListAsync<CustomerPaymentDto>($"{BasePath}?{query}", ct);
    }

    public async Task<Result<CustomerPaymentDto>> CreateAsync(CreateCustomerPaymentRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<CustomerPaymentDto>(BasePath, r, ct);
    }
}

