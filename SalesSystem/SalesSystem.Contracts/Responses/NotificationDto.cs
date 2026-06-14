namespace SalesSystem.Contracts.Responses;

public record NotificationDto(
    int Id,
    int UserId,
    byte Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);
