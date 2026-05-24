using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class PurchaseReturnItem : BaseEntity
{
    public int PurchaseReturnId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public SaleMode Mode { get; private set; }
    public string? Notes { get; private set; }

    public virtual PurchaseReturn? PurchaseReturn { get; private set; }
    public virtual Product? Product { get; private set; }

    private PurchaseReturnItem() { }

    public static PurchaseReturnItem Create(int productId, decimal quantity, decimal unitCost, decimal discountAmount = 0, SaleMode mode = SaleMode.Retail, string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");

        var item = new PurchaseReturnItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitCost = unitCost,
            DiscountAmount = discountAmount,
            Mode = mode,
            Notes = notes
        };
        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal() => LineTotal = (Quantity * UnitCost) - DiscountAmount;
}

public class PurchaseReturn : BaseEntity
{
    public string ReturnNo { get; private set; } = string.Empty;
    public int? PurchaseInvoiceId { get; private set; }
    public int SupplierId { get; private set; }
    public int WarehouseId { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public string? Notes { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal TotalAmount { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual List<PurchaseReturnItem> Items { get; private set; } = new();

    private PurchaseReturn() { }

    public static PurchaseReturn Create(
        string returnNo,
        int warehouseId,
        int supplierId,
        int? purchaseInvoiceId = null,
        DateTime? returnDate = null,
        string? notes = null,
        int? userId = null)
    {
        if (string.IsNullOrWhiteSpace(returnNo))
            throw new DomainException("رقم الإرجاع مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");

        var pr = new PurchaseReturn
        {
            ReturnNo = returnNo,
            WarehouseId = warehouseId,
            SupplierId = supplierId,
            PurchaseInvoiceId = purchaseInvoiceId,
            ReturnDate = returnDate ?? DateTime.UtcNow,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        pr.SetCreatedBy(userId);
        return pr;
    }

    public void AddItem(int productId, decimal quantity, decimal unitCost, decimal discountAmount = 0, SaleMode mode = SaleMode.Retail, string? notes = null)
    {
        var item = PurchaseReturnItem.Create(productId, quantity, unitCost, discountAmount, mode, notes);
        Items.Add(item);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.LineTotal);
        TotalAmount = SubTotal;
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط المرتجعات المسودة يمكن ترحيلها.");
        if (!Items.Any())
            throw new DomainException("لا يمكن ترحيل مرتجع بدون أصناف.");

        Status = InvoiceStatus.Posted;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("المرتجع ملغى بالفعل.");
        Status = InvoiceStatus.Cancelled;
    }
}