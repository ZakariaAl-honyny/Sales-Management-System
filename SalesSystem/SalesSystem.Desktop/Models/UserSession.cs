using SalesSystem.Contracts.Enums;

namespace SalesSystem.Desktop.Models;

public sealed class UserSession
{
    public int    UserId   { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public UserRole Role   { get; init; }
    public string Token    { get; init; } = string.Empty;

    public bool IsAdmin   => Role == UserRole.Admin;
    public bool IsManager => Role is UserRole.Admin or UserRole.Manager;
}
