using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    private Category() { }

    public static Category Create(string name, string? description = null, string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        return new Category
        {
            Name = name,
            Description = description,
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string? description, string? updatedBy = null)
    {
        Name = name;
        Description = description;
        UpdatedBy = updatedBy;
        UpdateTimestamp();
    }
}