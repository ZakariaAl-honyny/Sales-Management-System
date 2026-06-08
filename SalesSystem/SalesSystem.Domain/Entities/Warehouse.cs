using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class Warehouse : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public WarehouseType Type { get; private set; } = WarehouseType.Main;
    public string? Location { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? ManagerName { get; private set; }
    public int? AccountId { get; private set; }
    public Account? Account { get; private set; }
    public string? Notes { get; private set; }
    public bool IsDefault { get; private set; }

    protected Warehouse() { }

    public static Warehouse Create(
        string name,
        WarehouseType type = WarehouseType.Main,
        string? location = null,
        string? phone = null,
        string? address = null,
        string? managerName = null,
        bool isDefault = false,
        int? accountId = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المستودع مطلوب.");

        var warehouse = new Warehouse
        {
            Name = name,
            Type = type,
            Location = location,
            Phone = phone,
            Address = address,
            ManagerName = managerName,
            IsDefault = isDefault,
            AccountId = accountId,
            Notes = notes
        };
        warehouse.SetCreatedBy(createdByUserId);
        return warehouse;
    }

    public void Update(
        string name,
        WarehouseType type,
        string? location,
        string? phone = null,
        string? address = null,
        string? managerName = null,
        bool isDefault = false,
        int? accountId = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المستودع مطلوب.");

        Name = name;
        Type = type;
        Location = location;
        Phone = phone;
        Address = address;
        ManagerName = managerName;
        IsDefault = isDefault;
        AccountId = accountId;
        Notes = notes;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    public void SetAsDefault()
    {
        IsDefault = true;
    }
}
