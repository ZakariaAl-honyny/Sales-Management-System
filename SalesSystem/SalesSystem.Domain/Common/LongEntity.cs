namespace SalesSystem.Domain.Common;

/// <summary>
/// Base class for high-volume entities using a long (bigint) primary key.
/// Minimal fields — used for AuditLogs and SystemLogs which only need Id and CreatedAt.
/// </summary>
public abstract class LongEntity
{
    public long Id { get; protected set; }

    public DateTime CreatedAt { get; protected set; }

    protected LongEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }
}
