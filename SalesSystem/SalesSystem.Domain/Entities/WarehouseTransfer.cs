using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a stock transfer between two warehouses.
/// Maps to "WarehouseTransfers" table.
/// Schema: TransferNo (int unique), FromWarehouseId (smallint FK),
/// ToWarehouseId (smallint FK), TransferDate (date), Notes, Status (tinyint), audit.
/// </summary>
public class WarehouseTransfer : DocumentEntity
{
    public int TransferNo { get; private set; }
    public DateTime TransferDate { get; private set; }
    public short FromWarehouseId { get; private set; }
    public short ToWarehouseId { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation properties
    public virtual Warehouse? FromWarehouse { get; private set; }
    public virtual Warehouse? ToWarehouse { get; private set; }
    private readonly List<WarehouseTransferLine> _lines = new();
    public IReadOnlyCollection<WarehouseTransferLine> Lines => _lines.AsReadOnly();

    private WarehouseTransfer() { } // EF Core

    public static WarehouseTransfer Create(
        int transferNo,
        short fromWarehouseId,
        short toWarehouseId,
        DateTime? transferDate = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (transferNo <= 0)
            throw new DomainException("رقم التحويل مطلوب.");
        if (fromWarehouseId <= 0)
            throw new DomainException("المستودع المصدر مطلوب.");
        if (toWarehouseId <= 0)
            throw new DomainException("المستودع الوجهة مطلوب.");
        if (fromWarehouseId == toWarehouseId)
            throw new DomainException("المستودع المصدر والوجهة يجب أن يكونا مختلفين.");

        var t = new WarehouseTransfer
        {
            Status = InvoiceStatus.Draft,
            TransferNo = transferNo,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
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
