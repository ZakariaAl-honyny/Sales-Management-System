namespace SalesSystem.Desktop.Models;

public enum NotificationType { Success, Error, Warning }

public sealed class Notification
{
    public string Message { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public int Duration { get; init; } = 3000; // ms, default 3s
}

