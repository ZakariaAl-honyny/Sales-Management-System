using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Tracks all significant actions in the system for audit trail purposes.
/// Schema 8.1: AuditLogs — bigint PK, high-volume immutable log.
/// Columns: UserId FK (nullable), Action, EntityName, EntityId (string), Details (JSON), IpAddress, CreatedAt.
/// </summary>
public class AuditLog : LongEntity
{
    public int? UserId { get; private set; }
    public virtual User? User { get; private set; }
    public string Action { get; private set; } = string.Empty;         // e.g., "LoginSuccess", "CancelInvoice"
    public string? EntityName { get; private set; }                     // e.g., "SalesInvoice", "Product"
    public string? EntityId { get; private set; }                       // entity identifier as string
    public string? Details { get; private set; }                        // JSON payload
    public string? IpAddress { get; private set; }

    private AuditLog() { } // EF Core

    public static AuditLog Create(
        int? userId,
        string action,
        string? entityName = null,
        string? entityId = null,
        string? details = null,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("نوع الحركة مطلوب.");

        return new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress
        };
    }
}
