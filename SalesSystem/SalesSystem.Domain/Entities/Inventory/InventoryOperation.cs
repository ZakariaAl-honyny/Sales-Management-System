using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class InventoryOperation : BaseEntity
{
    public string OperationNo { get; private set; } = string.Empty;
    public int WarehouseId { get; private set; }
    public InventoryOperationType OperationType { get; private set; }
    public DateTime OperationDate { get; private set; }
    public string? ReferenceNo { get; private set; }
    public string? Notes { get; private set; }
    public AdjustmentType? AdjustmentType { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation properties
    public virtual Warehouse? Warehouse { get; private set; }
    private readonly List<InventoryOperationItem> _items = new();
    public IReadOnlyCollection<InventoryOperationItem> Items => _items.AsReadOnly();

    private InventoryOperation() { }

    public static InventoryOperation Create(
        string operationNo,
        int warehouseId,
        InventoryOperationType operationType,
        DateTime? operationDate = null,
        string? referenceNo = null,
        string? notes = null,
        AdjustmentType? adjustmentType = null,
        int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(operationNo))
            throw new DomainException("رقم العملية مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");

        var operation = new InventoryOperation
        {
            OperationNo = operationNo,
            WarehouseId = warehouseId,
            OperationType = operationType,
            OperationDate = operationDate ?? DateTime.UtcNow,
            ReferenceNo = referenceNo,
            Notes = notes,
            AdjustmentType = adjustmentType,
            Status = InvoiceStatus.Draft
        };
        operation.SetCreatedBy(createdByUserId);
        return operation;
    }

    public void AddItem(int productId, decimal quantity, decimal? unitCost = null,
        StockIssueReason? stockIssueReason = null, string? notes = null)
    {
        var item = InventoryOperationItem.Create(productId, quantity, unitCost, stockIssueReason, notes);
        AddItem(item);
    }

    public void AddItem(InventoryOperationItem item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لعملية غير مسودة.");
        _items.Add(item);
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط العمليات المسودة يمكن ترحيلها.");
        if (!_items.Any())
            throw new DomainException("لا يمكن ترحيل عملية بدون أصناف.");
        Status = InvoiceStatus.Posted;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("العملية ملغاة بالفعل.");
        Status = InvoiceStatus.Cancelled;
        UpdateTimestamp();
    }

    public void UpdateNotes(string? notes)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن تحديث ملاحظات عملية غير مسودة.");
        Notes = notes;
        UpdateTimestamp();
    }
}
