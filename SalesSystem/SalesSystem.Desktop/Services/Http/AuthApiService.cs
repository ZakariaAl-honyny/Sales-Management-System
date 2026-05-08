using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests.Auth;
using SalesSystem.Desktop.Models;
using SalesSystem.Desktop.Services.Interfaces;
using System.Net.Http.Json;

namespace SalesSystem.Desktop.Services.Http;

public sealed class AuthApiService : IAuthApiService
{
    private readonly HttpClient _httpClient;

    public AuthApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<UserSession>> LoginAsync(string userName, string password, CancellationToken ct = default)
    {
        try
        {
            var request = new LoginRequest(userName, password);
            var response = await _httpClient.PostAsJsonAsync("/api/v1/auth/login", request, ct);

            if (response.IsSuccessStatusCode)
            {
                var session = await response.Content.ReadFromJsonAsync<UserSession>(cancellationToken: ct);
                return session != null 
                    ? Result<UserSession>.Success(session) 
                    : Result<UserSession>.Failure("\u062E\u0637\u0623 \u0641\u064A \u062A\u062D\u0648\u064A\u0644 \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A");
            }

            var error = await response.Content.ReadAsStringAsync(ct);
            return Result<UserSession>.Failure(string.IsNullOrWhiteSpace(error) ? "\u0627\u0633\u0645 \u0627\u0644\u0645\u0633\u062A\u062E\u062F\u0645 \u0623\u0648 \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631 \u063A\u064A\u0631 \u0635\u062D\u064A\u062D\u0629" : error);
        }
        catch (HttpRequestException)
        {
            return Result<UserSession>.Failure("\u0644\u0627 \u064A\u0645\u0643\u0646 \u0627\u0644\u0627\u062A\u0635\u0627\u0644 \u0628\u0627\u0644\u062E\u0627\u062F\u0645. \u062A\u0623\u0643\u062F \u0645\u0646 \u062A\u0634\u063A\u064A\u0644 \u0627\u0644\u062E\u062F\u0645\u0629.");
        }
        catch (Exception ex)
        {
            return Result<UserSession>.Failure($"\u062D\u062F\u062B \u062E\u0637\u0623 \u063A\u064A\u0631 \u0645\u062A\u0648\u0642\u0639: {ex.Message}");
        }
    }
}
