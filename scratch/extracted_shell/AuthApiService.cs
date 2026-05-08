    public class AuthApiService
    {
        private readonly HttpClient _httpClient;

        public AuthApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiResponseDto<object>>();
                throw new InvalidOperationException(error?.Message ?? "«”„ «·„” Œœ„ √Ê ﬂ·„… «·„—Ê— Œ«ÿ∆…");
            }

            var result = await response.Content.ReadFromJsonAsync<ApiResponseDto<AuthResponseDto>>();
            return result?.Data;
        }
    }
}
LoginForm.cs (‘«‘…  ”ÃÌ· «·œŒÊ·)
›Ì SalesSystem.Desktop/Forms:
C#
using SalesSystem.Contracts.Auth;
using SalesSystem.Desktop.Services.Api;

namespace SalesSystem.Desktop.Forms
{
