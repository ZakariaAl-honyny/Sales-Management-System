namespace SalesSystem.Application.Updates.Models;

public record UpdateCheckResult
{
    public bool IsSuccess { get; init; }
    public UpdateInfo? UpdateInfo { get; init; }
    public string? ErrorMessage { get; init; }
    public bool UpdateAvailable { get; init; }

    public static UpdateCheckResult NoUpdate()
        => new() { IsSuccess = true, UpdateAvailable = false };

    public static UpdateCheckResult Available(UpdateInfo info)
        => new() { IsSuccess = true, UpdateAvailable = true, UpdateInfo = info };

    public static UpdateCheckResult Failed(string reason)
        => new() { IsSuccess = false, ErrorMessage = reason };
}
