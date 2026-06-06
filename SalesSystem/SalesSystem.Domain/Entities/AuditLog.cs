using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Tracks all significant actions in the system for audit trail purposes.
/// Examples: LoginSuccess, CreateUser, PostInvoice, CancelInvoice, etc.
/// Audit logs are immutable — once created, they cannot be modified or deleted.
/// </summary>
public class AuditLog : BaseEntity
{
    public int? UserId { get; private set; }
    public User? User { get; private set; }
    public string Action { get; private set; } = string.Empty;        // "LoginSuccess", "CreateUser", etc.
    public string EntityType { get; private set; } = string.Empty;    // "User", "SalesInvoice", etc.
    public int? EntityId { get; private set; }
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime Timestamp { get; private set; }

    protected AuditLog() { } // EF Core

    public static AuditLog Create(int? userId, string action, string entityType,
        int? entityId = null, string? details = null, string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("نوع الحركة مطلوب.");
        if (string.IsNullOrWhiteSpace(entityType))
            throw new DomainException("نوع الكيان مطلوب.");

        return new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };
    }
}
