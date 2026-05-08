using System.Net.Http.Json;
using System.Text.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Requests.Auth;
using SalesSystem.Contracts.Responses;
using SalesSystem.Contracts.Enums;
using SalesSystem.Desktop.Models;
using SalesSystem.Desktop.Services.Interfaces;

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
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request, ct);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
                if (loginResponse == null)
                    return Result<UserSession>.Failure("›‘· «” ·«„ »Ì«‰«  «·Ã·”… „‰ «·Œ«œ„");

                var session = new UserSession
                {
                    UserId = loginResponse.UserId,
                    UserName = loginResponse.UserName,
                    FullName = loginResponse.FullName,
                    Role = (UserRole)loginResponse.Role,
                    Token = loginResponse.Token
                };

                return Result<UserSession>.Success(session);
            }

            // Handle failure
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            string errorMessage = "›‘·  ”ÃÌ· «·œŒÊ·";

            try 
            {
                using var doc = JsonDocument.Parse(errorContent);
                if (doc.RootElement.TryGetProperty("error", out var errorProp))
                {
                    errorMessage = errorProp.GetString() ?? errorMessage;
                }
            }
            catch { /* Ignore parse errors */ }

            return Result<UserSession>.Failure(errorMessage);
        }
        catch (HttpRequestException)
        {
            return Result<UserSession>.Failure("·« Ì„ﬂ‰ «·« ’«· »«·Œ«œ„.  √ﬂœ „‰  ‘€Ì· «·Œœ„….");
        }
        catch (Exception ex)
        {
            return Result<UserSession>.Failure($"ÕœÀ Œÿ√ €Ì— „ Êﬁ⁄: {ex.Message}");
        }
    }
}
