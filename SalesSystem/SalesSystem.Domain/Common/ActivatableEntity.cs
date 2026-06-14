namespace SalesSystem.Domain.Common;

/// <summary>
/// Auditable entity with activation/deactivation (soft-delete) support via IsActive flag.
/// Most business entities (Products, Customers, Suppliers, Currencies, etc.) inherit from this.
/// </summary>
public abstract class ActivatableEntity : AuditableEntity
{
    public bool IsActive { get; protected set; } = true;

    protected ActivatableEntity() : base() { }

    public virtual void MarkAsDeleted()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    public virtual void Restore()
    {
        IsActive = true;
        UpdateTimestamp();
    }
}
