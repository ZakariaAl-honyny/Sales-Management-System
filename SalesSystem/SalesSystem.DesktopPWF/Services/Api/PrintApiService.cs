using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.Services.Api;

public class PrintApiService : ApiServiceBase, IPrintApiService
{
    public PrintApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result> PreviewSalesAsync(int invoiceId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/print/preview/sales/{invoiceId}", null, ct),
            "PrintApiService.PreviewSalesAsync");
    }

    public async Task<Result> PrintSalesA4Async(int invoiceId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/print/a4/sales/{invoiceId}", null, ct),
            "PrintApiService.PrintSalesA4Async");
    }

    public async Task<Result> PrintSalesThermalAsync(int invoiceId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/print/thermal/sales/{invoiceId}", null, ct),
            "PrintApiService.PrintSalesThermalAsync");
    }

    public async Task<Result<PrintPreviewData>> GetSalesPreviewDataAsync(int invoiceId, CancellationToken ct = default)
    {
        return await ExecuteAsync<PrintPreviewData>(
            () => _httpClient.PostAsync($"api/v1/print/preview-data/sales/{invoiceId}", null, ct),
            "PrintApiService.GetSalesPreviewDataAsync");
    }

    public async Task<Result> PreviewPurchaseAsync(int invoiceId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/print/preview/purchase/{invoiceId}", null, ct),
            "PrintApiService.PreviewPurchaseAsync");
    }

    public async Task<Result> PrintPurchaseA4Async(int invoiceId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/print/a4/purchase/{invoiceId}", null, ct),
            "PrintApiService.PrintPurchaseA4Async");
    }

    public async Task<Result> PrintPurchaseThermalAsync(int invoiceId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/print/thermal/purchase/{invoiceId}", null, ct),
            "PrintApiService.PrintPurchaseThermalAsync");
    }

    public async Task<Result<PrintPreviewData>> GetPurchasePreviewDataAsync(int invoiceId, CancellationToken ct = default)
    {
        return await ExecuteAsync<PrintPreviewData>(
            () => _httpClient.PostAsync($"api/v1/print/preview-data/purchase/{invoiceId}", null, ct),
            "PrintApiService.GetPurchasePreviewDataAsync");
    }

    public async Task<Result> TestPrintAsync(CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync("api/v1/print/test", null, ct),
            "PrintApiService.TestPrintAsync");
    }
}
