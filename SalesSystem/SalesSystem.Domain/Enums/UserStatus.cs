namespace SalesSystem.Domain.Enums;

/// <summary>
/// Represents the status of a user account in the system.
/// </summary>
public enum UserStatus : byte
{
    /// <summary>Account is active and can log in.</summary>
    Active = 1,

    /// <summary>Account is deactivated (soft-deleted) and cannot log in.</summary>
    Inactive = 2,

    /// <summary>Account is locked due to multiple failed login attempts.</summary>
    Locked = 3
}
