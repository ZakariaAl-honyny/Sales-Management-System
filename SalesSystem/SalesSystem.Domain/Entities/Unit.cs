using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

public class Unit : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Symbol { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    protected Unit() { }

    public static Unit Create(string name, string? symbol = null, string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        return new Unit
        {
            Name = name,
            Symbol = symbol,
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string? symbol, string? updatedBy = null)
    {
        Name = name;
        Symbol = symbol;
        UpdatedBy = updatedBy;
        UpdateTimestamp();
    }
}