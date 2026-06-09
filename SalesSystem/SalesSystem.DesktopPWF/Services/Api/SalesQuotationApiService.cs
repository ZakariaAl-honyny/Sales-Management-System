using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// API service interface for Sales Quotations
/// </summary>
public interface ISalesQuotationApiService
{
    Task<Result<List<SalesQuotationDto>>> GetAllAsync(int? customerId = null, byte? status = null, string? search = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task<Result<SalesQuotationDto>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<SalesQuotationDto>> CreateAsync(CreateSalesQuotationRequest request, CancellationToken ct = default);
    Task<Result<SalesQuotationDto>> UpdateAsync(int id, UpdateSalesQuotationRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    Task<Result<SalesQuotationDto>> ConfirmAsync(int id, CancellationToken ct = default);
    Task<Result<SalesQuotationDto>> ExpireAsync(int id, CancellationToken ct = default);
    Task<Result<SalesQuotationDto>> ConvertToInvoiceAsync(int id, ConvertQuotationToInvoiceRequest request, CancellationToken ct = default);
}

/// <summary>
/// API service implementation for Sales Quotations
/// </summary>
public class SalesQuotationApiService : ApiServiceBase, ISalesQuotationApiService
{
    private const string BasePath = "api/v1/sales-quotations";

    public SalesQuotationApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<SalesQuotationDto>>> GetAllAsync(
        int? customerId = null,
        byte? status = null,
        string? search = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>();

        if (customerId.HasValue)
            queryParams.Add($"customerId={customerId.Value}");
        if (status.HasValue)
            queryParams.Add($"status={status.Value}");
        if (!string.IsNullOrEmpty(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (from.HasValue)
            queryParams.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue)
            queryParams.Add($"to={to.Value:yyyy-MM-dd}");

        var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        return await ExecutePagedAsync<SalesQuotationDto>(
            () => _httpClient.GetAsync($"{BasePath}{query}", ct),
            "SalesQuotationApiService.GetAllAsync");
    }

    public async Task<Result<SalesQuotationDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.GetAsync($"{BasePath}/{id}", ct),
            "SalesQuotationApiService.GetByIdAsync");
    }

    public async Task<Result<SalesQuotationDto>> CreateAsync(CreateSalesQuotationRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PostAsJsonAsync(BasePath, request, ct),
            "SalesQuotationApiService.CreateAsync");
    }

    public async Task<Result<SalesQuotationDto>> UpdateAsync(int id, UpdateSalesQuotationRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PutAsJsonAsync($"{BasePath}/{id}", request, ct),
            "SalesQuotationApiService.UpdateAsync");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.DeleteAsync($"{BasePath}/{id}", ct),
            "SalesQuotationApiService.DeleteAsync");
    }

    public async Task<Result<SalesQuotationDto>> ConfirmAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/confirm", null, ct),
            "SalesQuotationApiService.ConfirmAsync");
    }

    public async Task<Result<SalesQuotationDto>> ExpireAsync(int id, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PostAsync($"{BasePath}/{id}/expire", null, ct),
            "SalesQuotationApiService.ExpireAsync");
    }

    public async Task<Result<SalesQuotationDto>> ConvertToInvoiceAsync(int id, ConvertQuotationToInvoiceRequest request, CancellationToken ct = default)
    {
        return await ExecuteAsync<SalesQuotationDto>(
            () => _httpClient.PostAsJsonAsync($"{BasePath}/{id}/convert-to-invoice", request, ct),
            "SalesQuotationApiService.ConvertToInvoiceAsync");
    }
}
