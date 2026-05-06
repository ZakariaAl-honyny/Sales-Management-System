namespace SalesSystem.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; protected set; }
    
    public DateTime CreatedAt { get; protected set; }
    
    public DateTime? UpdatedAt { get; protected set; }
    
    public bool IsActive { get; protected set; } = true;

    protected BaseEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public virtual void MarkAsDeleted()
    {
        IsActive = false;
    }

    public virtual void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}