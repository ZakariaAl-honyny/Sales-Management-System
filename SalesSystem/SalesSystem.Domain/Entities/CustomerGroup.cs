using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a customer grouping category for reporting and filtering.
/// Examples: "عام", "موزعين", "تجزئة", "جملة"
/// </summary>
public class CustomerGroup : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private CustomerGroup() { }

    /// <summary>
    /// Creates a new CustomerGroup entity.
    /// </summary>
    public static CustomerGroup Create(
        string name,
        string? description = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المجموعة مطلوب.");

        var group = new CustomerGroup
        {
            Name = name,
            Description = description
        };
        group.SetCreatedBy(createdByUserId);
        return group;
    }

    /// <summary>
    /// Updates the group's name and description.
    /// </summary>
    public void Update(
        string name,
        string? description = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المجموعة مطلوب.");

        Name = name;
        Description = description;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
