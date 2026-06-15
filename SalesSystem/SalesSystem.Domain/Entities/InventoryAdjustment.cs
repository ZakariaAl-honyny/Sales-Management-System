using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents an inventory adjustment operation that corrects stock levels.
/// Maps to "InventoryAdjustments" table.
/// Schema: AdjustmentNo (int unique), WarehouseId (smallint FK),
/// AdjustmentType (tinyint: 1=Opening,2=Increase,3=Shortage,4=Damage),
/// AdjustmentDate (date), Notes, Status (tinyint), audit.
/// </summary>
public class InventoryAdjustment : DocumentEntity
{
    public int AdjustmentNo { get; private set; }
    public DateTime AdjustmentDate { get; private set; }
    public short WarehouseId { get; private set; }
    public InventoryAdjustmentType AdjustmentType { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual Warehouse? Warehouse { get; private set; }

    private readonly List<InventoryAdjustmentLine> _lines = new();
    public IReadOnlyCollection<InventoryAdjustmentLine> Lines => _lines.AsReadOnly();

    private InventoryAdjustment() { }

    public static InventoryAdjustment Create(
        int adjustmentNo,
        short warehouseId,
        InventoryAdjustmentType adjustmentType,
        DateTime? adjustmentDate = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (adjustmentNo <= 0)
            throw new DomainException("رقم التسوية مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");

        var adjustment = new InventoryAdjustment
        {
            AdjustmentNo = adjustmentNo,
            WarehouseId = warehouseId,
            AdjustmentDate = adjustmentDate ?? DateTime.UtcNow,
            AdjustmentType = adjustmentType,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        adjustment.SetCreatedBy(createdByUserId);
        return adjustment;
    }

    public void AddLine(InventoryAdjustmentLine line)
    {
        if (line == null)
            throw new DomainException("صنف التسوية مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لتسوية غير مسودة.");
        _lines.Add(line);
        UpdateTimestamp();
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط التسويات المسودة يمكن ترحيلها.");
        if (!_lines.Any())
            throw new DomainException("لا يمكن ترحيل تسوية بدون أصناف.");
        Status = InvoiceStatus.Posted;
        PostedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("التسوية ملغاة بالفعل.");
        Status = InvoiceStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdateTimestamp();
    }
}
