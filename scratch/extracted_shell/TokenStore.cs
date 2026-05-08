    public static class TokenStore
    {
        public static string? Token { get; set; }
        public static string? CurrentUserName { get; set; }
    }
}
AuthApiService.cs (έν Services/Api)
C#
using System.Net.Http.Json;
using SalesSystem.Contracts.Auth;
using SalesSystem.Contracts.Common;

namespace SalesSystem.Desktop.Services.Api
{
