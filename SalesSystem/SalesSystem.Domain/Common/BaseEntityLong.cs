namespace SalesSystem.Domain.Common;

/// <summary>
/// Base class for entities that use a long (bigint) primary key.
/// Used for high-volume tables like AuditLogs where int range may be exceeded.
/// </summary>
public abstract class BaseEntityLong
{
    public long Id { get; protected set; }

    public DateTime CreatedAt { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }

    public bool IsActive { get; protected set; } = true;

    /// <summary>
    /// The ID of the user who created this entity. Null for system-initialized records.
    /// </summary>
    public int? CreatedByUserId { get; protected set; }

    /// <summary>
    /// The ID of the user who last updated this entity.
    /// </summary>
    public int? UpdatedByUserId { get; protected set; }

    protected BaseEntityLong()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public virtual void MarkAsDeleted()
    {
        IsActive = false;
    }

    public virtual void Restore()
    {
        IsActive = true;
    }

    public virtual void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public virtual void SetCreatedBy(int? userId)
    {
        CreatedByUserId = userId;
    }

    public virtual void SetUpdatedBy(int? userId)
    {
        UpdatedByUserId = userId;
    }
}
