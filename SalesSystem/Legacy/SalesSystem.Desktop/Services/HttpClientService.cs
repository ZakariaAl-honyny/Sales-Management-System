using System.Net.Http.Json;
using System.Text.Json;
using SalesSystem.Contracts.Common;
using Serilog;

namespace SalesSystem.Desktop.Services;

public class HttpClientService
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HttpClientService(HttpClient client)
    {
        _client = client;
    }

    public async Task<Result<IReadOnlyList<T>>> GetListAsync<T>(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetAsync(path, ct);
            if (!response.IsSuccessStatusCode) return await HandleErrorResponse<IReadOnlyList<T>>(response, ct);

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            
            // Handle PagedResult { items: [], ... }
            if (doc.RootElement.TryGetProperty("items", out var itemsProp))
            {
                var items = itemsProp.Deserialize<List<T>>(_jsonOptions);
                return Result<IReadOnlyList<T>>.Success(items ?? new List<T>());
            }
            
            // Handle raw List []
            var result = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
            return result != null ? Result<IReadOnlyList<T>>.Success(result) : Result<IReadOnlyList<T>>.Success(new List<T>());
        }
        catch (Exception ex) { return HandleException<IReadOnlyList<T>>(ex); }
    }

    public async Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetAsync(path, ct);
            if (!response.IsSuccessStatusCode) return await HandleErrorResponse<T>(response, ct);

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
            return result != null ? Result<T>.Success(result) : Result<T>.Failure("Empty response");
        }
        catch (Exception ex) { return HandleException<T>(ex); }
    }

    public async Task<Result<T>> PostAsync<T>(string path, object body, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PostAsJsonAsync(path, body, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
                return result != null ? Result<T>.Success(result) : Result<T>.Failure("Empty response");
            }
            return await HandleErrorResponse<T>(response, ct);
        }
        catch (Exception ex) { return HandleException<T>(ex); }
    }

    public async Task<Result> PostAsync(string path, object? body = null, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PostAsJsonAsync(path, body, _jsonOptions, ct);
            if (response.IsSuccessStatusCode) return Result.Success();
            return await HandleErrorResponse(response, ct);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    public async Task<Result<T>> PutAsync<T>(string path, object? body = null, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PutAsJsonAsync(path, body, _jsonOptions, ct);
            if (response.IsSuccessStatusCode)
            {
                if (response.Content.Headers.ContentLength == 0) return Result<T>.Success(default!);
                var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, ct);
                return result != null ? Result<T>.Success(result) : Result<T>.Failure("Empty response");
            }
            return await HandleErrorResponse<T>(response, ct);
        }
        catch (Exception ex) { return HandleException<T>(ex); }
    }

    public async Task<Result> DeleteAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.DeleteAsync(path, ct);
            if (response.IsSuccessStatusCode) return Result.Success();
            return await HandleErrorResponse(response, ct);
        }
        catch (Exception ex) { return HandleException(ex); }
    }

    private async Task<Result<T>> HandleErrorResponse<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var error = await ParseError(response, ct);
        return Result<T>.Failure(error);
    }

    private async Task<Result> HandleErrorResponse(HttpResponseMessage response, CancellationToken ct)
    {
        var error = await ParseError(response, ct);
        return Result.Failure(error);
    }

    private async Task<string> ParseError(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return "انتهت الجلسة أو غير مصرح لك. يرجى تسجيل الدخول مرة أخرى.";
        
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return "ليس لديك الصلاحية الكافية لهذه العملية.";

        try
        {
            var content = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrEmpty(content)) return $"خطأ في الخادم: {response.StatusCode}";

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("error", out var errorProp))
                return errorProp.GetString() ?? content;
            
            if (doc.RootElement.TryGetProperty("errors", out var errorsProp))
            {
                // Handle FluentValidation errors
                var firstError = errorsProp.EnumerateObject().FirstOrDefault();
                return $"{firstError.Value.EnumerateArray().FirstOrDefault()}";
            }
        }
        catch { }

        return $"خطأ غير متوقع: {response.StatusCode}";
    }

    private Result<T> HandleException<T>(Exception ex)
    {
        Log.Error(ex, "HTTP Client Exception");
        if (ex is HttpRequestException || ex.InnerException is System.Net.Sockets.SocketException)
        {
            return Result<T>.Failure("تعذر الاتصال بالخادم. يرجى التأكد من تشغيل نظام الـ API.");
        }
        return Result<T>.Failure($"خطأ في الاتصال: {ex.Message}");
    }

    private Result HandleException(Exception ex)
    {
        Log.Error(ex, "HTTP Client Exception");
        if (ex is HttpRequestException || ex.InnerException is System.Net.Sockets.SocketException)
        {
            return Result.Failure("تعذر الاتصال بالخادم. يرجى التأكد من تشغيل نظام الـ API.");
        }
        return Result.Failure($"خطأ في الاتصال: {ex.Message}");
    }
}
