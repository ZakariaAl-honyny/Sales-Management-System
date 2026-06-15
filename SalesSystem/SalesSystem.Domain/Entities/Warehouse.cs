using SalesSystem.Domain.Accounting.Entities;
using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a warehouse/storage location within a branch.
/// Inherits <see cref="ActivatableEntity"/> for audit and soft-delete support.
/// Maps to "Warehouses" table — smallint PK.
/// </summary>
public class Warehouse : ActivatableEntity
{
    /// <summary>
    /// smallint PK — overrides base int Id for small lookup tables.
    /// </summary>
    public new short Id { get; private set; }

    /// <summary>
    /// Warehouse name in Arabic (e.g. "المستودع الرئيسي").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// FK to the branch this warehouse belongs to (smallint).
    /// </summary>
    public short BranchId { get; private set; }

    /// <summary>
    /// The branch navigation property.
    /// </summary>
    public Branch Branch { get; private set; } = null!;

    /// <summary>
    /// Contact phone number for the warehouse.
    /// </summary>
    public string? Phone { get; private set; }

    /// <summary>
    /// Physical address of the warehouse.
    /// </summary>
    public string? Address { get; private set; }

    /// <summary>
    /// Additional notes about the warehouse.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Private constructor required by EF Core.
    /// </summary>
    protected Warehouse() { }

    /// <summary>
    /// Factory method to create a new warehouse.
    /// </summary>
    public static Warehouse Create(
        short branchId,
        string name,
        string? phone = null,
        string? address = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (branchId <= 0)
            throw new DomainException("يجب اختيار الفرع التابع له المستودع.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المستودع مطلوب.");

        var warehouse = new Warehouse
        {
            BranchId = branchId,
            Name = name.Trim(),
            Phone = phone?.Trim(),
            Address = address?.Trim(),
            Notes = notes?.Trim()
        };
        warehouse.SetCreatedBy(createdByUserId);
        return warehouse;
    }

    /// <summary>
    /// Updates the warehouse properties.
    /// </summary>
    public void Update(
        short branchId,
        string name,
        string? phone = null,
        string? address = null,
        string? notes = null,
        int? updatedByUserId = null)
    {
        if (branchId <= 0)
            throw new DomainException("يجب اختيار الفرع التابع له المستودع.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("اسم المستودع مطلوب.");

        BranchId = branchId;
        Name = name.Trim();
        Phone = phone?.Trim();
        Address = address?.Trim();
        Notes = notes?.Trim();
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }
}
