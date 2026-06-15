using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Branch : ActivatableEntity
{
    /// <summary>
    /// smallint PK — overrides base int Id for small lookup tables.
    /// </summary>
    public new short Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? ManagerName { get; private set; }
    public string? Notes { get; private set; }

    private Branch() { }

    public static Branch Create(string name, string? code = null, string? phone = null, string? address = null, string? managerName = null, string? notes = null, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الفرع مطلوب.");

        var branch = new Branch
        {
            Name = name,
            Code = code?.Trim(),
            Phone = phone?.Trim(),
            Address = address?.Trim(),
            ManagerName = managerName?.Trim(),
            Notes = notes?.Trim()
        };
        branch.SetCreatedBy(createdByUserId);
        return branch;
    }

    public void Update(string name, string? code = null, string? phone = null, string? address = null, string? managerName = null, string? notes = null, int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم الفرع مطلوب.");

        Name = name;
        Code = code?.Trim();
        Phone = phone?.Trim();
        Address = address?.Trim();
        ManagerName = managerName?.Trim();
        Notes = notes?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
