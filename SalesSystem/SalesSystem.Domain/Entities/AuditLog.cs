using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Tracks all significant actions in the system for audit trail purposes.
/// Schema 8.1: AuditLogs — bigint PK, high-volume immutable log.
/// Columns: UserId FK (nullable), Action (100), EntityType (100),
/// EntityId (int), OldValues, NewValues, ChangedColumns, IpAddress, CreatedAt.
/// </summary>
public class AuditLog : LongEntity
{
    public int? UserId { get; private set; }
    public virtual User? User { get; private set; }
    public string Action { get; private set; } = string.Empty;         // e.g., "LoginSuccess", "CancelInvoice"
    public string? EntityType { get; private set; }                     // e.g., "SalesInvoice", "Product"
    public int? EntityId { get; private set; }                          // entity PK (int matches all entity PKs)
    public string? OldValues { get; private set; }                      // JSON: values before change
    public string? NewValues { get; private set; }                      // JSON: values after change
    public string? ChangedColumns { get; private set; }                 // comma-separated column names
    public string? IpAddress { get; private set; }

    private AuditLog() { } // EF Core

    public static AuditLog Create(
        int? userId,
        string action,
        string? entityType = null,
        int? entityId = null,
        string? oldValues = null,
        string? newValues = null,
        string? changedColumns = null,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("نوع الحركة مطلوب.");

        return new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            ChangedColumns = changedColumns,
            IpAddress = ipAddress
        };
    }
}
