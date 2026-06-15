using System.Net.Http;
using System.Net.Http.Json;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.DesktopPWF.Services.Api;

/// <summary>
/// Notification API service implementation
/// </summary>
public class NotificationApiService : ApiServiceBase, INotificationApiService
{
    public NotificationApiService(HttpClient httpClient, ISessionService session)
        : base(httpClient, session)
    {
    }

    public async Task<Result<List<NotificationDto>>> GetAllAsync(int? userId = null, bool unreadOnly = false, int page = 1, int pageSize = 50)
    {
        var queryParams = $"page={page}&pageSize={pageSize}&unreadOnly={unreadOnly.ToString().ToLower()}";
        if (userId.HasValue)
            queryParams += $"&userId={userId.Value}";

        return await ExecutePagedAsync<NotificationDto>(
            () => _httpClient.GetAsync($"api/v1/notifications?{queryParams}"),
            "NotificationApiService.GetAllAsync");
    }

    public async Task<Result> MarkAsReadAsync(int id)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/notifications/{id}/read", null),
            "NotificationApiService.MarkAsReadAsync");
    }

    public async Task<Result> MarkAllAsReadAsync(int userId)
    {
        return await ExecuteCommandAsync(
            () => _httpClient.PostAsync($"api/v1/notifications/mark-all-read?userId={userId}", null),
            "NotificationApiService.MarkAllAsReadAsync");
    }
}
