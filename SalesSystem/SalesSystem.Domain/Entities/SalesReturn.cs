using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Domain.Entities;

public class SalesReturnItem
{
    public int SalesReturnItemId { get; private set; }
    public int SalesReturnId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public string? Notes { get; private set; }

    public virtual SalesReturn? SalesReturn { get; private set; }
    public virtual Product? Product { get; private set; }

    private SalesReturnItem() { }

    public static SalesReturnItem Create(int productId, decimal quantity, decimal unitPrice, decimal discountAmount = 0, string? notes = null)
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId is required.", nameof(productId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("UnitPrice cannot be negative.", nameof(unitPrice));

        var item = new SalesReturnItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountAmount = discountAmount,
            Notes = notes
        };
        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal() => LineTotal = (Quantity * UnitPrice) - DiscountAmount;
}

public class SalesReturn : BaseEntity
{
    public string ReturnNo { get; private set; } = string.Empty;
    public int? SalesInvoiceId { get; private set; }
    public int? CustomerId { get; private set; }
    public int WarehouseId { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public string? Notes { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal TotalAmount { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public virtual SalesInvoice? SalesInvoice { get; private set; }
    public virtual Customer? Customer { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual List<SalesReturnItem> Items { get; private set; } = new();

    private SalesReturn() { }

    public static SalesReturn Create(
        string returnNo,
        int warehouseId,
        int? customerId,
        int? salesInvoiceId = null,
        DateTime? returnDate = null,
        string? notes = null,
        int? userId = null)
    {
        if (string.IsNullOrWhiteSpace(returnNo))
            throw new ArgumentException("ReturnNo is required.", nameof(returnNo));
        if (warehouseId <= 0)
            throw new ArgumentException("WarehouseId is required.", nameof(warehouseId));

        var sr = new SalesReturn
        {
            ReturnNo = returnNo,
            WarehouseId = warehouseId,
            CustomerId = customerId,
            SalesInvoiceId = salesInvoiceId,
            ReturnDate = returnDate ?? DateTime.UtcNow,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        sr.SetCreatedBy(userId);
        return sr;
    }

    public void AddItem(int productId, decimal quantity, decimal unitPrice, decimal discountAmount = 0, string? notes = null)
    {
        var item = SalesReturnItem.Create(productId, quantity, unitPrice, discountAmount, notes);
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