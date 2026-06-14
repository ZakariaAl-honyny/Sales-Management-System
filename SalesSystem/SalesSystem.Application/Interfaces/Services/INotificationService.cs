using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;

namespace SalesSystem.Application.Interfaces.Services;

public interface INotificationService
{
    Task<Result<List<NotificationDto>>> GetUserNotificationsAsync(int userId, CancellationToken ct);
    Task<Result<NotificationDto>> MarkAsReadAsync(int id, CancellationToken ct);
    Task<Result<int>> GetUnreadCountAsync(int userId, CancellationToken ct);
}
