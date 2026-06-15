using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Department : ActivatableEntity
{
    /// <summary>
    /// Schema: smallint PK (short).
    /// </summary>
    public new short Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description of the department's purpose or responsibilities.
    /// </summary>
    public string? Description { get; private set; }

    private Department() { }

    public static Department Create(string name, string? description = null, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم القسم مطلوب.");

        var department = new Department
        {
            Name = name,
            Description = description?.Trim()
        };
        department.SetCreatedBy(createdByUserId);
        return department;
    }

    public void Update(string name, string? description = null, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم القسم مطلوب.");

        Name = name;
        Description = description?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
