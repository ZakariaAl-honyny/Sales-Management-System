using SalesSystem.Domain.Common;

namespace SalesSystem.Domain.Entities;

public class Warehouse : BaseEntity
{
    public string? Code { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public bool IsDefault { get; private set; }
    public string? CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    protected Warehouse() { }

    public static Warehouse Create(string name, string? code = null, string? location = null, bool isDefault = false, string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        return new Warehouse
        {
            Name = name,
            Code = code,
            Location = location,
            IsDefault = isDefault,
            CreatedBy = createdBy
        };
    }

    public void Update(string name, string? code, string? location, bool isDefault, string? updatedBy = null)
    {
        Name = name;
        Code = code;
        Location = location;
        IsDefault = isDefault;
        UpdatedBy = updatedBy;
        UpdateTimestamp();
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }
}