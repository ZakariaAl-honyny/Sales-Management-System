using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Tracks user login sessions for token management and activity monitoring.
/// When a user logs in, a new session is created. Sessions can be terminated
/// on logout or admin revocation.
/// </summary>
public class UserSession : AuditableEntity
{
    public int UserId { get; private set; }
    public User? User { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public string? DeviceName { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime LoginAt { get; private set; }
    public DateTime LastActivityAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }

    protected UserSession() { } // EF Core

    public static UserSession Create(int userId, string token,
        DateTime loginAt, int expirationHours = 8,
        string? deviceName = null, string? ipAddress = null, string? userAgent = null)
    {
        return new UserSession
        {
            UserId = userId,
            Token = token,
            DeviceName = deviceName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            LoginAt = loginAt,
            LastActivityAt = loginAt,
            ExpiresAt = loginAt.AddHours(expirationHours),
            IsRevoked = false
        };
    }

    /// <summary>
    /// Updates the last activity timestamp for this session.
    /// Call this on each authenticated request to track session freshness.
    /// </summary>
    public void Touch() => LastActivityAt = DateTime.UtcNow;

    /// <summary>
    /// Terminates this session — sets IsRevoked to true.
    /// Typically called on logout or admin session revocation.
    /// </summary>
    public void Revoke() => IsRevoked = true;

    /// <summary>
    /// Returns true if the session has expired based on its expiry time.
    /// </summary>
    public bool HasExpired() => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Returns true if the session is still valid (not revoked and not expired).
    /// </summary>
    public bool IsValid() => !IsRevoked && !HasExpired();
}
