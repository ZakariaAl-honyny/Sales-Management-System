using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class StockTransferItem : BaseEntity
{
    public int StockTransferId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Notes { get; private set; }
    public SaleMode Mode { get; private set; }

    public virtual StockTransfer? StockTransfer { get; private set; }
    public virtual Product? Product { get; private set; }

    private StockTransferItem() { }

    public static StockTransferItem Create(int productId, decimal quantity, SaleMode mode = SaleMode.Retail, string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");

        return new StockTransferItem
        {
            ProductId = productId,
            Quantity = quantity,
            Mode = mode,
            Notes = notes
        };
    }
}

public class StockTransfer : BaseEntity
{
    public string TransferNo { get; private set; } = string.Empty;
    public int FromWarehouseId { get; private set; }
    public int ToWarehouseId { get; private set; }
    public DateTime TransferDate { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public virtual Warehouse? FromWarehouse { get; private set; }
    public virtual Warehouse? ToWarehouse { get; private set; }
    public virtual List<StockTransferItem> Items { get; private set; } = new();

    private StockTransfer() { }

    public static StockTransfer Create(
        string transferNo,
        int fromWarehouseId,
        int toWarehouseId,
        string? notes = null,
        DateTime? transferDate = null)
    {
        if (string.IsNullOrWhiteSpace(transferNo))
            throw new DomainException("رقم التحويل مطلوب.");
        if (fromWarehouseId <= 0)
            throw new DomainException("المستودع المصدر مطلوب.");
        if (toWarehouseId <= 0)
            throw new DomainException("المستودع الوجهة مطلوب.");
        if (fromWarehouseId == toWarehouseId)
            throw new DomainException("لا يمكن التحويل إلى نفس المستودع.");

        return new StockTransfer
        {
            TransferNo = transferNo,
            FromWarehouseId = fromWarehouseId,
            ToWarehouseId = toWarehouseId,
            Notes = notes,
            TransferDate = transferDate ?? DateTime.UtcNow,
            Status = InvoiceStatus.Draft
        };
    }

    public void AddItem(int productId, decimal quantity, SaleMode mode = SaleMode.Retail, string? notes = null)
    {
        var item = StockTransferItem.Create(productId, quantity, mode, notes);
        AddItem(item);
    }

    public void AddItem(StockTransferItem item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لتحويل غير مسودة.");
        Items.Add(item);
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط التحويلات المسودة يمكن ترحيلها.");
        if (!Items.Any())
            throw new DomainException("لا يمكن ترحيل تحويل بدون أصناف.");
        Status = InvoiceStatus.Posted;
    }

    public void UpdateNotes(string? notes)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن تحديث ملاحظات تحويل غير مسودة.");
        Notes = notes;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("التحويل ملغى بالفعل.");
        Status = InvoiceStatus.Cancelled;
    }
}