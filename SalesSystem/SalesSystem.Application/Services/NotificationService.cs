using Microsoft.Extensions.Logging;
using SalesSystem.Application.Interfaces;
using SalesSystem.Application.Interfaces.Services;
using SalesSystem.Contracts.Common;
using SalesSystem.Contracts.Responses;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IUnitOfWork uow, ILogger<NotificationService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<Result<List<NotificationDto>>> GetUserNotificationsAsync(int userId, CancellationToken ct)
    {
        try
        {
            var notifications = await _uow.Notifications.ToListAsync(
                n => n.UserId == userId,
                q => q.OrderByDescending(n => n.CreatedAt),
                ct);

            var dtos = notifications.Select(MapToDto).ToList();
            return Result<List<NotificationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            return Result<List<NotificationDto>>.Failure("حدث خطأ أثناء استرجاع الإشعارات");
        }
    }

    public async Task<Result<NotificationDto>> MarkAsReadAsync(int id, CancellationToken ct)
    {
        try
        {
            var notification = await _uow.Notifications.GetByIdAsync(id, ct);
            if (notification == null)
                return Result<NotificationDto>.Failure("الإشعار غير موجود", ErrorCodes.NotFound);

            notification.MarkAsRead();
            await _uow.SaveChangesAsync(ct);

            _logger.LogInformation("Notification {Id} marked as read", id);

            return Result<NotificationDto>.Success(MapToDto(notification));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read", id);
            return Result<NotificationDto>.Failure("حدث خطأ أثناء تحديث حالة الإشعار");
        }
    }

    public async Task<Result<int>> GetUnreadCountAsync(int userId, CancellationToken ct)
    {
        try
        {
            var count = await _uow.Notifications.CountAsync(
                n => n.UserId == userId && !n.IsRead, ct);

            return Result<int>.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread notification count for user {UserId}", userId);
            return Result<int>.Failure("حدث خطأ أثناء استرجاع عدد الإشعارات غير المقروءة");
        }
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.UserId,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.CreatedAt
        );
    }
}
