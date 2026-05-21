using System.Web;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Desktop.Services.Interfaces;

namespace SalesSystem.Desktop.Services.Http;

public sealed class SupplierPaymentApiService : ISupplierPaymentApiService
{
    private readonly HttpClientService _http;
    private const string BasePath = "api/v1/supplier-payments";

    public SupplierPaymentApiService(HttpClientService http) => _http = http;

    public async Task<Result<IReadOnlyList<SupplierPaymentDto>>> GetAllAsync(int? supplierId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        if (supplierId.HasValue) query["supplierId"] = supplierId.Value.ToString();
        if (from.HasValue) query["dateFrom"] = from.Value.ToString("o");
        if (to.HasValue) query["dateTo"] = to.Value.ToString("o");

        return await _http.GetListAsync<SupplierPaymentDto>($"{BasePath}?{query}", ct);
    }

    public async Task<Result<SupplierPaymentDto>> CreateAsync(CreateSupplierPaymentRequest r, CancellationToken ct = default)
    {
        return await _http.PostAsync<SupplierPaymentDto>(BasePath, r, ct);
    }
}

