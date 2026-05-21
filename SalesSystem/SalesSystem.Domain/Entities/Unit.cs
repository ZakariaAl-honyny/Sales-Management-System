using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Unit : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Symbol { get; private set; }

    protected Unit() { }

    public static Unit Create(string name, string? symbol = null, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الوحدة مطلوب.");

        var unit = new Unit
        {
            Name = name,
            Symbol = symbol
        };
        unit.SetCreatedBy(createdByUserId);
        return unit;
    }

    public void Update(string name, string? symbol, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الوحدة مطلوب.");

        Name = name;
        Symbol = symbol;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}