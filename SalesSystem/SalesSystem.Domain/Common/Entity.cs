namespace SalesSystem.Domain.Common;

/// <summary>
/// Root base entity with only an auto-increment int primary key.
/// No activation (IsActive), no audit fields (CreatedAt, etc.).
/// Use for pure data entities: join tables, line items, stock records.
/// </summary>
public abstract class Entity
{
    public int Id { get; protected set; }

    protected Entity() { }
}
