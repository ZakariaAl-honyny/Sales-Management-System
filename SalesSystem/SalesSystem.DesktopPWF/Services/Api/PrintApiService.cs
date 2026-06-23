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

    public async Task<Result> TestPrintAsync(CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync("api/v1/print/test", null, ct),
            "PrintApiService.TestPrintAsync");
    }

    public async Task<Result<string>> GetSalesA4PdfAsync(int invoiceId, CancellationToken ct = default)
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.GetAsync($"api/v1/print/generate-a4/sales/{invoiceId}", ct);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                var tempPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"Invoice_{invoiceId}_{DateTime.Now:HHmmss}.pdf");
                await System.IO.File.WriteAllBytesAsync(tempPath, bytes, ct);
                return Result<string>.Success(tempPath);
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct);
            Serilog.Log.Warning("GetSalesA4PdfAsync API failure: {StatusCode} - {Content}",
                response.StatusCode, errorContent);
            return Result<string>.Failure("فشل في تحميل ملف PDF", response.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            return HandleConnectionError<string>(ex, "PrintApiService.GetSalesA4PdfAsync");
        }
    }

    public async Task<Result<string>> GetPurchaseA4PdfAsync(int invoiceId, CancellationToken ct = default)
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.GetAsync($"api/v1/print/generate-a4/purchase/{invoiceId}", ct);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                var tempPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"Invoice_{invoiceId}_{DateTime.Now:HHmmss}.pdf");
                await System.IO.File.WriteAllBytesAsync(tempPath, bytes, ct);
                return Result<string>.Success(tempPath);
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct);
            Serilog.Log.Warning("GetPurchaseA4PdfAsync API failure: {StatusCode} - {Content}",
                response.StatusCode, errorContent);
            return Result<string>.Failure("فشل في تحميل ملف PDF", response.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            return HandleConnectionError<string>(ex, "PrintApiService.GetPurchaseA4PdfAsync");
        }
    }

    // ─── Sales Return Print Methods ──────────────────────────────────────────

    public async Task<Result> PrintSalesReturnA4Async(int returnId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/print/a4/sales-returns/{returnId}", null, ct),
            "PrintApiService.PrintSalesReturnA4Async");
    }

    public async Task<Result> PrintSalesReturnThermalAsync(int returnId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/print/thermal/sales-returns/{returnId}", null, ct),
            "PrintApiService.PrintSalesReturnThermalAsync");
    }

    public async Task<Result> PrintPurchaseReturnA4Async(int returnId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/print/a4/purchase-returns/{returnId}", null, ct),
            "PrintApiService.PrintPurchaseReturnA4Async");
    }

    public async Task<Result> PrintPurchaseReturnThermalAsync(int returnId, CancellationToken ct = default)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/print/thermal/purchase-returns/{returnId}", null, ct),
            "PrintApiService.PrintPurchaseReturnThermalAsync");
    }

    public async Task<Result<string>> GetSalesReturnA4PdfAsync(int returnId, CancellationToken ct = default)
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.GetAsync($"api/v1/print/generate-a4/sales-returns/{returnId}", ct);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                var tempPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"SalesReturn_{returnId}_{DateTime.Now:HHmmss}.pdf");
                await System.IO.File.WriteAllBytesAsync(tempPath, bytes, ct);
                return Result<string>.Success(tempPath);
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct);
            Serilog.Log.Warning("GetSalesReturnA4PdfAsync API failure: {StatusCode} - {Content}",
                response.StatusCode, errorContent);
            return Result<string>.Failure("فشل في تحميل ملف PDF", response.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            return HandleConnectionError<string>(ex, "PrintApiService.GetSalesReturnA4PdfAsync");
        }
    }

    public async Task<Result<string>> GetPurchaseReturnA4PdfAsync(int returnId, CancellationToken ct = default)
    {
        try
        {
            AddAuthHeader();
            var response = await _httpClient.GetAsync($"api/v1/print/generate-a4/purchase-returns/{returnId}", ct);
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                var tempPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"PurchaseReturn_{returnId}_{DateTime.Now:HHmmss}.pdf");
                await System.IO.File.WriteAllBytesAsync(tempPath, bytes, ct);
                return Result<string>.Success(tempPath);
            }

            var errorContent = await response.Content.ReadAsStringAsync(ct);
            Serilog.Log.Warning("GetPurchaseReturnA4PdfAsync API failure: {StatusCode} - {Content}",
                response.StatusCode, errorContent);
            return Result<string>.Failure("فشل في تحميل ملف PDF", response.StatusCode.ToString());
        }
        catch (Exception ex)
        {
            return HandleConnectionError<string>(ex, "PrintApiService.GetPurchaseReturnA4PdfAsync");
        }
    }
}
