using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Supplier Payment Application API service implementation
/// </summary>
public class SupplierPaymentApplicationApiService : ApiServiceBase, ISupplierPaymentApplicationApiService
{
    public SupplierPaymentApplicationApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SupplierPaymentApplicationDto>>> GetAllAsync(int? supplierPaymentId = null, int? purchaseInvoiceId = null)
    {
        var queryParams = "pageSize=1000";
        if (supplierPaymentId.HasValue)
            queryParams += $"&supplierPaymentId={supplierPaymentId.Value}";
        if (purchaseInvoiceId.HasValue)
            queryParams += $"&purchaseInvoiceId={purchaseInvoiceId.Value}";

        return await ExecutePagedAsync<SupplierPaymentApplicationDto>(
            () => _httpClient.GetAsync($"api/v1/supplier-payment-applications?{queryParams}"),
            "SupplierPaymentApplicationApiService.GetAllAsync");
    }

    public async Task<Result<SupplierPaymentApplicationDto>> GetByIdAsync(int id)
    {
        return await ExecuteAsync<SupplierPaymentApplicationDto>(
            () => _httpClient.GetAsync($"api/v1/supplier-payment-applications/{id}"),
            "SupplierPaymentApplicationApiService.GetByIdAsync");
    }

    public async Task<Result<SupplierPaymentApplicationDto>> CreateAsync(CreateSupplierPaymentApplicationRequest request)
    {
        return await ExecuteAsync<SupplierPaymentApplicationDto>(
            () => _httpClient.PostAsJsonAsync("api/v1/supplier-payment-applications", request),
            "SupplierPaymentApplicationApiService.CreateAsync");
    }

    public async Task<Result> DeleteAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"api/v1/supplier-payment-applications/{id}"),
            "SupplierPaymentApplicationApiService.DeleteAsync");
    }
}
