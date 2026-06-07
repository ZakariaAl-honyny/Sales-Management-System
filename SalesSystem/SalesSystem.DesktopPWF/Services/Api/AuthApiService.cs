using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Requests;
using SalesSystem.Contracts.Responses;
using SalesSystem.DesktopPWF.Models;

namespace SalesSystem.DesktopPWF.Services.Api;

public class AuthApiService : ApiServiceBase, IAuthApiService
{
    public AuthApiService(HttpClient httpClient, ISessionService session) 
        : base(httpClient, session)
    {
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request);
            return await HandleResponseAsync<LoginResponse>(response);
        }
        catch (Exception ex)
        {
            return HandleConnectionError<LoginResponse>(ex, "AuthApiService.LoginAsync");
        }
    }

    public async Task<LoginResult> LoginWithDetailsAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/auth/login", request);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return new LoginResult
                {
                    IsSuccess = true,
                    Response = data
                };
            }

            // Parse error response with support for extended fields (userId for RequiresPasswordSetup)
            try
            {
                if (response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    Serilog.Log.Warning("API login failure: {StatusCode} - {Error} ({ErrorCode})", 
                        response.StatusCode, error?.Error, error?.ErrorCode);

                    return new LoginResult
                    {
                        IsSuccess = false,
                        Error = error?.Error ?? "اسم المستخدم أو كلمة المرور غير صحيحة",
                        ErrorCode = error?.ErrorCode,
                        RequiresPasswordSetupUserId = error?.UserId,
                        PasswordResetToken = error?.Token
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                Serilog.Log.Warning("API login failure (non-JSON): {StatusCode} - {Content}", 
                    response.StatusCode, content);
                return new LoginResult
                {
                    IsSuccess = false,
                    Error = $"خطأ في الخادم: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Unexpected error parsing login error response. StatusCode: {StatusCode}", 
                    response.StatusCode);
                return new LoginResult
                {
                    IsSuccess = false,
                    Error = "حدث خطأ غير متوقع",
                    ErrorCode = "Unknown"
                };
            }
        }
        catch (Exception)
        {
            return new LoginResult
            {
                IsSuccess = false,
                Error = "فشل في الاتصال بالخادم. يرجى التحقق من الشبكة.",
                ErrorCode = "ConnectionError"
            };
        }
    }

    public async Task<Result> SetPasswordAsync(SetPasswordRequest request)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsJsonAsync("api/v1/auth/set-password", request),
            "AuthApiService.SetPasswordAsync");
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsJsonAsync("api/v1/auth/change-password", request),
            "AuthApiService.ChangePasswordAsync");
    }
}
