namespace SalesSystem.Domain.Common;

/// <summary>
/// Entity with audit fields tracking creation and modification.
/// No activation (IsActive) — use <see cref="ActivatableEntity"/> for soft-deletable entities.
/// No document life cycle — use <see cref="DocumentEntity"/> for entities with Draft/Posted/Cancelled status.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }
    public int? CreatedByUserId { get; protected set; }
    public int? UpdatedByUserId { get; protected set; }

    protected AuditableEntity()
    {
        CreatedAt = DateTime.UtcNow;
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
