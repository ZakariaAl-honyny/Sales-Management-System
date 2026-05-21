using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Category() { }

    public static Category Create(string name, string? description = null, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم التصنيف مطلوب.");

        var category = new Category
        {
            Name = name,
            Description = description
        };
        category.SetCreatedBy(createdByUserId);
        return category;
    }

    public void Update(string name, string? description, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم التصنيف مطلوب.");

        Name = name;
        Description = description;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}