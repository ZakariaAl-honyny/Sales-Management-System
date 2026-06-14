using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Department : ActivatableEntity
{
    public short BranchId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description of the department's purpose or responsibilities.
    /// </summary>
    public string? Description { get; private set; }

    public virtual Branch Branch { get; private set; } = null!;

    private Department() { }

    public static Department Create(short branchId, string name, string? description = null, int? createdByUserId = null)
    {
        if (branchId <= 0)
            throw new DomainException("معرّف الفرع غير صالح.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم القسم مطلوب.");

        var department = new Department
        {
            BranchId = branchId,
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
