namespace SalesSystem.Application.Updates.Models;

/// <summary>
/// Pure DTO carrying update check data. Success/failure is expressed via Result&lt;T&gt; wrapper.
/// </summary>
public record UpdateCheckResult
{
    public UpdateInfo? UpdateInfo { get; init; }
    public bool UpdateAvailable { get; init; }

    public static UpdateCheckResult NoUpdate()
        => new() { UpdateAvailable = false };

    public static UpdateCheckResult Available(UpdateInfo info)
        => new() { UpdateAvailable = true, UpdateInfo = info };
}
