using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a physical inventory count session for a specific warehouse.
/// Maps to "InventoryCounts" table.
/// Schema: CountNo (int unique), WarehouseId (smallint FK), CountDate (date),
/// Notes, Status (tinyint), audit.
/// </summary>
public class InventoryCount : DocumentEntity
{
    public int CountNo { get; private set; }
    public DateTime CountDate { get; private set; }
    public short WarehouseId { get; private set; }
    public InventoryCountStatus Status { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual Warehouse? Warehouse { get; private set; }

    private readonly List<InventoryCountLine> _lines = new();
    public IReadOnlyCollection<InventoryCountLine> Lines => _lines.AsReadOnly();

    private InventoryCount() { }

    /// <summary>
    /// Creates a new inventory count session.
    /// </summary>
    public static InventoryCount Create(
        int countNo,
        short warehouseId,
        DateTime? countDate = null,
        int? createdByUserId = null)
    {
        if (countNo <= 0)
            throw new DomainException("رقم الجرد مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");

        var inventoryCount = new InventoryCount
        {
            CountNo = countNo,
            WarehouseId = warehouseId,
            CountDate = countDate ?? DateTime.UtcNow,
            Status = InventoryCountStatus.Draft
        };
        inventoryCount.SetCreatedBy(createdByUserId);
        return inventoryCount;
    }

    /// <summary>
    /// Adds a count line to this inventory count session.
    /// Only allowed when the count is in Draft status.
    /// </summary>
    public void AddLine(InventoryCountLine line)
    {
        if (line == null)
            throw new DomainException("صنف الجرد مطلوب.");
        if (Status != InventoryCountStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لجرد غير مسودة.");
        _lines.Add(line);
        UpdateTimestamp();
    }

    public void Post()
    {
        if (Status != InventoryCountStatus.Draft)
            throw new DomainException("فقط الجرد المسودة يمكن ترحيله.");
        if (!_lines.Any())
            throw new DomainException("لا يمكن ترحيل جرد بدون أصناف.");
        Status = InventoryCountStatus.Posted;
        PostedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == InventoryCountStatus.Cancelled)
            throw new DomainException("الجرد ملغى بالفعل.");
        Status = InventoryCountStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void SetNotes(string? notes)
    {
        if (Status != InventoryCountStatus.Draft)
            throw new DomainException("لا يمكن تحديث ملاحظات جرد غير مسودة.");
        Notes = notes;
        UpdateTimestamp();
    }
}
