using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a stock transfer between two warehouses.
/// Maps to "WarehouseTransfers" table.
/// </summary>
public class WarehouseTransfer : DocumentEntity
{
    /// <summary>
    /// Transfer status (Draft=1, Posted=2, Cancelled=3).
    /// </summary>
    public InvoiceStatus Status { get; private set; }

    public int TransferNo { get; private set; }
    public DateTime TransferDate { get; private set; }
    public short SourceWarehouseId { get; private set; }
    public short DestinationWarehouseId { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual Warehouse? SourceWarehouse { get; private set; }
    public virtual Warehouse? DestinationWarehouse { get; private set; }
    private readonly List<WarehouseTransferLine> _lines = new();
    public IReadOnlyCollection<WarehouseTransferLine> Lines => _lines.AsReadOnly();

    private WarehouseTransfer() { } // EF Core

    public static WarehouseTransfer Create(
        int transferNo,
        short sourceWarehouseId,
        short destinationWarehouseId,
        DateTime? transferDate = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (transferNo <= 0)
            throw new DomainException("رقم التحويل مطلوب.");
        if (sourceWarehouseId <= 0)
            throw new DomainException("المستودع المصدر مطلوب.");
        if (destinationWarehouseId <= 0)
            throw new DomainException("المستودع الوجهة مطلوب.");
        if (sourceWarehouseId == destinationWarehouseId)
            throw new DomainException("المستودع المصدر والوجهة يجب أن يكونا مختلفين.");

        var t = new WarehouseTransfer
        {
            Status = InvoiceStatus.Draft,
            TransferNo = transferNo,
            SourceWarehouseId = sourceWarehouseId,
            DestinationWarehouseId = destinationWarehouseId,
            TransferDate = transferDate ?? DateTime.UtcNow,
            Notes = notes
        };
        t.SetCreatedBy(createdByUserId);
        return t;
    }

    public void AddLine(WarehouseTransferLine line)
    {
        if (line == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لتحويل غير مسودة.");
        _lines.Add(line);
        UpdateTimestamp();
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط التحويلات المسودة يمكن ترحيلها.");
        if (!_lines.Any())
            throw new DomainException("لا يمكن ترحيل تحويل بدون أصناف.");
        Status = InvoiceStatus.Posted;
        PostedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("التحويل ملغي بالفعل.");
        Status = InvoiceStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdateTimestamp();
    }
}
