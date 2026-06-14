using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class PurchaseReturnItem : Entity
{
    public int PurchaseReturnId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }

    public virtual PurchaseReturn? PurchaseReturn { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private PurchaseReturnItem() { }

    public static PurchaseReturnItem Create(int productId, int productUnitId, decimal quantity, decimal unitCost)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");

        var item = new PurchaseReturnItem
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitCost = unitCost,
        };
        item.RecalculateLineTotal();
        return item;
    }

    public void RecalculateLineTotal() => LineTotal = Quantity * UnitCost;
}

public class PurchaseReturn : DocumentEntity
{
    public int ReturnNo { get; private set; }
    public int? PurchaseInvoiceId { get; private set; }
    public int SupplierId { get; private set; }
    public short WarehouseId { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public string? Notes { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal TotalAmount { get; private set; }
    public short? CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }
    public virtual List<PurchaseReturnItem> Items { get; private set; } = new();

    private PurchaseReturn() { }

    public static PurchaseReturn Create(
        int returnNo,
        short warehouseId,
        int supplierId,
        int? purchaseInvoiceId = null,
        DateTime? returnDate = null,
        string? notes = null,
        short? currencyId = null,
        decimal? exchangeRate = null,
        int? userId = null)
    {
        if (returnNo <= 0)
            throw new DomainException("رقم الإرجاع مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (currencyId.HasValue && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");

        var pr = new PurchaseReturn
        {
            ReturnNo = returnNo,
            WarehouseId = warehouseId,
            SupplierId = supplierId,
            PurchaseInvoiceId = purchaseInvoiceId,
            ReturnDate = returnDate ?? DateTime.UtcNow,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        pr.SetCreatedBy(userId);
        return pr;
    }

    public void AddItem(int productId, int productUnitId, decimal quantity, decimal unitCost)
    {
        var item = PurchaseReturnItem.Create(productId, productUnitId, quantity, unitCost);
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
