using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Tracks user login sessions for token management and activity monitoring.
/// When a user logs in, a new session is created. Sessions can be terminated
/// on logout or admin revocation.
/// </summary>
public class UserSession : BaseEntity
{
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime LoginAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    protected UserSession() { } // EF Core

    public static UserSession Create(int userId, string tokenHash,
        DateTime loginAt, int expirationHours = 8)
    {
        return new UserSession
        {
            UserId = userId,
            TokenHash = tokenHash,
            LoginAt = loginAt,
            LastActivityAt = loginAt,
            ExpiresAt = loginAt.AddHours(expirationHours),
            IsActive = true
        };
    }

    /// <summary>
    /// Updates the last activity timestamp for this session.
    /// Call this on each authenticated request to track session freshness.
    /// </summary>
    public void Touch() => LastActivityAt = DateTime.UtcNow;

    /// <summary>
    /// Terminates this session — sets IsActive to false.
    /// Typically called on logout or admin session revocation.
    /// </summary>
    public void Terminate() => IsActive = false;
}
