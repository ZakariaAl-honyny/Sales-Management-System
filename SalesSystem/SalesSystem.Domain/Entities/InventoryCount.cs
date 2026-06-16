using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a physical inventory count session for a specific warehouse.
/// Maps to "InventoryCounts" table.
/// Schema: nvarchar(50) CountNo (unique), smallint WarehouseId FK,
/// nvarchar(300) Notes, tinyint Status (Draft=1,Posted=2,Cancelled=3).
/// BaseEntity with CreatedAt only.
/// </summary>
public class InventoryCount : Entity
{
    public string CountNo { get; private set; } = string.Empty;
    public short WarehouseId { get; private set; }
    public InventoryCountStatus Status { get; private set; }
    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public int CreatedByUserId { get; private set; }

    // Navigation properties
    public virtual Warehouse? Warehouse { get; private set; }

    private readonly List<InventoryCountLine> _lines = new();
    public IReadOnlyCollection<InventoryCountLine> Lines => _lines.AsReadOnly();

    private InventoryCount() { }

    /// <summary>
    /// Creates a new inventory count session.
    /// </summary>
    public static InventoryCount Create(
        string countNo,
        short warehouseId,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(countNo))
            throw new DomainException("رقم الجرد مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");

        var inventoryCount = new InventoryCount
        {
            CountNo = countNo.Trim(),
            WarehouseId = warehouseId,
            Notes = notes,
            Status = InventoryCountStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId ?? 0
        };
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
    }

    public void Post()
    {
        if (Status != InventoryCountStatus.Draft)
            throw new DomainException("فقط الجرد المسودة يمكن ترحيله.");
        if (!_lines.Any())
            throw new DomainException("لا يمكن ترحيل جرد بدون أصناف.");
        Status = InventoryCountStatus.Posted;
    }

    public void Cancel()
    {
        if (Status == InventoryCountStatus.Cancelled)
            throw new DomainException("الجرد ملغى بالفعل.");
        Status = InventoryCountStatus.Cancelled;
    }

    public void SetNotes(string? notes)
    {
        if (Status != InventoryCountStatus.Draft)
            throw new DomainException("لا يمكن تحديث ملاحظات جرد غير مسودة.");
        Notes = notes;
    }
}
