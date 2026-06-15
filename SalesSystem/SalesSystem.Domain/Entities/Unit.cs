using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a unit of measure (e.g., "حبة", "كجم", "لتر").
/// System-seeded units (e.g., "حبة", "قطعة") are protected from deletion.
/// Maps to "Units" table — smallint PK.
/// </summary>
public class Unit : ActivatableEntity
{
    /// <summary>
    /// smallint PK — overrides base int Id for small lookup tables.
    /// </summary>
    public new short Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional symbol/abbreviation (e.g., "kg", "L", "pc").
    /// </summary>
    public string? Symbol { get; private set; }

    /// <summary>
    /// Indicates whether this is a system-protected unit (cannot be deleted).
    /// System units are seeded by the application and required for correct operation.
    /// </summary>
    public bool IsSystem { get; private set; }

    protected Unit() { }

    /// <summary>
    /// Creates a new unit of measure.
    /// </summary>
    /// <param name="name">Unit name in Arabic (e.g., "حبة", "كجم", "لتر").</param>
    /// <param name="symbol">Optional symbol/abbreviation (e.g., "kg", "L").</param>
    /// <param name="isSystem">Whether this is a system-protected unit. Default false.</param>
    /// <param name="createdByUserId">User who created this record.</param>
    /// <returns>The newly created Unit entity.</returns>
    public static Unit Create(
        string name,
        string? symbol = null,
        bool isSystem = false,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الوحدة مطلوب.");

        var unit = new Unit
        {
            Name = name,
            Symbol = symbol,
            IsSystem = isSystem,
            IsActive = true
        };
        unit.SetCreatedBy(createdByUserId);
        return unit;
    }

    /// <summary>
    /// Updates the unit name and symbol.
    /// </summary>
    public void Update(string name, string? symbol = null, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الوحدة مطلوب.");

        Name = name;
        Symbol = symbol;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Soft-deletes the unit. Throws if it is a system-protected unit.
    /// </summary>
    public override void MarkAsDeleted()
    {
        if (IsSystem)
            throw new DomainException("لا يمكن حذف وحدة النظام — الوحدة محمية.");

        IsActive = false;
        UpdateTimestamp();
    }
}
