using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class SalesReturnItem : BaseEntity
{
    public int SalesReturnId { get; private set; }
    public int ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public SaleMode Mode { get; private set; }
    public string? Notes { get; private set; }

    public virtual SalesReturn? SalesReturn { get; private set; }
    public virtual Product? Product { get; private set; }

    private SalesReturnItem() { }

    public static SalesReturnItem Create(int productId, decimal quantity, decimal unitPrice, decimal discountAmount = 0, SaleMode mode = SaleMode.Retail, string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitPrice < 0)
            throw new DomainException("سعر الوحدة لا يمكن أن يكون سالباً.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");

        var item = new SalesReturnItem
        {
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountAmount = discountAmount,
            Mode = mode,
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
    public int? CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public virtual SalesInvoice? SalesInvoice { get; private set; }
    public virtual Customer? Customer { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }
    public virtual List<SalesReturnItem> Items { get; private set; } = new();

    private SalesReturn() { }

    public static SalesReturn Create(
        string returnNo,
        int warehouseId,
        int? customerId,
        int? salesInvoiceId = null,
        DateTime? returnDate = null,
        string? notes = null,
        int? currencyId = null,
        decimal? exchangeRate = null,
        int? userId = null)
    {
        if (string.IsNullOrWhiteSpace(returnNo))
            throw new DomainException("رقم الإرجاع مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (currencyId.HasValue && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");

        var sr = new SalesReturn
        {
            ReturnNo = returnNo,
            WarehouseId = warehouseId,
            CustomerId = customerId,
            SalesInvoiceId = salesInvoiceId,
            ReturnDate = returnDate ?? DateTime.UtcNow,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        sr.SetCreatedBy(userId);
        return sr;
    }

    public void AddItem(int productId, decimal quantity, decimal unitPrice, decimal discountAmount = 0, SaleMode mode = SaleMode.Retail, string? notes = null)
    {
        var item = SalesReturnItem.Create(productId, quantity, unitPrice, discountAmount, mode, notes);
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