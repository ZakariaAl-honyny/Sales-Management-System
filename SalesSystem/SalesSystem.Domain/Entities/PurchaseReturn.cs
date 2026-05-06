using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Entities;

public class PurchaseReturnItem
{
    public int PurchaseReturnItemId { get; private set; }
    public int PurchaseReturnId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }

    public virtual PurchaseReturn? PurchaseReturn { get; private set; }
    public virtual Product? Product { get; private set; }

    private PurchaseReturnItem() { }

    public static PurchaseReturnItem Create(int productId, decimal quantity, decimal unitCost)
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId is required.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (unitCost < 0)
            throw new ArgumentException("UnitCost cannot be negative.", nameof(unitCost));

        var item = new PurchaseReturnItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitCost = unitCost
        };
        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal() => LineTotal = Quantity * UnitCost;
}

public class PurchaseReturn : BaseEntity
{
    public string ReturnNo { get; private set; } = string.Empty;
    public int? PurchaseInvoiceId { get; private set; }
    public int SupplierId { get; private set; }
    public int WarehouseId { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public string? Reason { get; private set; }
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
        int supplierId,
        int warehouseId,
        int? purchaseInvoiceId = null,
        string? reason = null,
        DateTime? returnDate = null)
    {
        if (string.IsNullOrWhiteSpace(returnNo))
            throw new ArgumentException("ReturnNo is required.", nameof(returnNo));
        if (supplierId <= 0)
            throw new ArgumentException("SupplierId is required.", nameof(supplierId));
        if (warehouseId <= 0)
            throw new ArgumentException("WarehouseId is required.", nameof(warehouseId));

        return new PurchaseReturn
        {
            ReturnNo = returnNo,
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            PurchaseInvoiceId = purchaseInvoiceId,
            Reason = reason,
            ReturnDate = returnDate ?? DateTime.UtcNow,
            Status = InvoiceStatus.Draft
        };
    }

    public void AddItem(PurchaseReturnItem item)
    {
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
            throw new InvalidOperationException("Only draft returns can be posted.");
        if (!Items.Any())
            throw new InvalidOperationException("Cannot post a return with no items.");
        
        Status = InvoiceStatus.Posted;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Already cancelled.");
        Status = InvoiceStatus.Cancelled;
    }
}