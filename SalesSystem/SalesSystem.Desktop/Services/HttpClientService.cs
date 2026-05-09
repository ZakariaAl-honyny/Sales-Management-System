using System.Net.Http.Json;
using System.Text.Json;
using SalesSystem.Contracts.Common;

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
            var result = await _client.GetFromJsonAsync<List<T>>(path, _jsonOptions, ct);
            return result != null ? Result<IReadOnlyList<T>>.Success(result) : Result<IReadOnlyList<T>>.Failure("Empty response");
        }
        catch (Exception ex) { return Result<IReadOnlyList<T>>.Failure(ex.Message); }
    }

    public async Task<Result<T>> GetAsync<T>(string path, CancellationToken ct = default)
    {
        try
        {
            var result = await _client.GetFromJsonAsync<T>(path, _jsonOptions, ct);
            return result != null ? Result<T>.Success(result) : Result<T>.Failure("Empty response");
        }
        catch (Exception ex) { return Result<T>.Failure(ex.Message); }
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
            var error = await response.Content.ReadAsStringAsync(ct);
            return Result<T>.Failure(string.IsNullOrEmpty(error) ? $"Server error: {response.StatusCode}" : error);
        }
        catch (Exception ex) { return Result<T>.Failure(ex.Message); }
    }

    public async Task<Result> PostAsync(string path, object? body = null, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.PostAsJsonAsync(path, body, _jsonOptions, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
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
            var error = await response.Content.ReadAsStringAsync(ct);
            return Result<T>.Failure(string.IsNullOrEmpty(error) ? $"Server error: {response.StatusCode}" : error);
        }
        catch (Exception ex) { return Result<T>.Failure(ex.Message); }
    }

    public async Task<Result> DeleteAsync(string path, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.DeleteAsync(path, ct);
            return response.IsSuccessStatusCode ? Result.Success() : Result.Failure(await response.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex) { return Result.Failure(ex.Message); }
    }
}
