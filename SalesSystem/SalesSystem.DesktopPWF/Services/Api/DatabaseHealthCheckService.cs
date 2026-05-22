using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Service that checks if the API and database are reachable before showing the login screen.
/// Uses the /api/v1/health/database endpoint to verify database connectivity.
/// </summary>
public class DatabaseHealthCheckService : IDatabaseHealthCheckService
{
    private readonly HttpClient _httpClient;
    private const string HealthEndpoint = "api/v1/health/database";

    public DatabaseHealthCheckService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HealthCheckResult> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(HealthEndpoint, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await TryReadErrorAsync(response, ct);
                Log.Warning("Database health check failed: HTTP {StatusCode} - {Error}", 
                    (int)response.StatusCode, errorBody);
                return new HealthCheckResult
                {
                    IsApiReachable = true,
                    IsDatabaseConnected = false,
                    ErrorMessage = "قاعدة البيانات غير متصلة. يرجى التحقق من الخادم."
                };
            }

            var result = await response.Content.ReadFromJsonAsync<HealthDbResponse>(ct);
            var isConnected = result?.Status == "connected";

            if (!isConnected)
            {
                Log.Warning("Database health check reports: {Status} - {Message}", 
                    result?.Status, result?.Message);
            }

            return new HealthCheckResult
            {
                IsApiReachable = true,
                IsDatabaseConnected = isConnected,
                ErrorMessage = isConnected ? null : "قاعدة البيانات غير متصلة."
            };
        }
        catch (HttpRequestException ex)
        {
            Log.Error(ex, "API server is not reachable during health check");
            return new HealthCheckResult
            {
                IsApiReachable = false,
                IsDatabaseConnected = false,
                ErrorMessage = "خادم التطبيق غير متصل. يرجى التحقق من تشغيل الخدمة."
            };
        }
        catch (TaskCanceledException)
        {
            Log.Warning("Health check timed out");
            return new HealthCheckResult
            {
                IsApiReachable = false,
                IsDatabaseConnected = false,
                ErrorMessage = "انتهت مهلة الاتصال. يرجى التحقق من الشبكة."
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during health check");
            return new HealthCheckResult
            {
                IsApiReachable = false,
                IsDatabaseConnected = false,
                ErrorMessage = $"حدث خطأ غير متوقع: {ex.Message}"
            };
        }
    }

    private static async Task<string> TryReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch
        {
            return $"HTTP {(int)response.StatusCode}";
        }
    }

    private class HealthDbResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
