using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class PurchaseReturnItem : BaseEntity
{
    public int PurchaseReturnId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal? CostInBaseCurrency { get; private set; }
    public SaleMode Mode { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual PurchaseReturn? PurchaseReturn { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private PurchaseReturnItem() { }

    public static PurchaseReturnItem Create(
        int productId,
        decimal quantity,
        decimal unitCost,
        int productUnitId = 0,
        decimal discountAmount = 0,
        decimal? costInBaseCurrency = null,
        SaleMode mode = SaleMode.Retail,
        string? notes = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("وحدة المنتج مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (unitCost < 0)
            throw new DomainException("تكلفة الوحدة لا يمكن أن تكون سالبة.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (costInBaseCurrency.HasValue && costInBaseCurrency < 0)
            throw new DomainException("التكلفة بعملة الأساس لا يمكن أن تكون سالبة.");

        var item = new PurchaseReturnItem
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            Quantity = quantity,
            UnitCost = unitCost,
            DiscountAmount = discountAmount,
            CostInBaseCurrency = costInBaseCurrency,
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
    public decimal DiscountAmount { get; private set; }
    public DiscountType? DiscountType { get; private set; }
    public decimal? DiscountRate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public int? CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public bool LinkToInvoice { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation properties
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }
    public virtual List<PurchaseReturnItem> Items { get; private set; } = new();

    private PurchaseReturn() { }

    public static PurchaseReturn Create(
        string returnNo,
        int warehouseId,
        int supplierId,
        int? purchaseInvoiceId = null,
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
            LinkToInvoice = purchaseInvoiceId.HasValue,
            DiscountAmount = 0,
            Status = InvoiceStatus.Draft
        };
        pr.SetCreatedBy(userId);
        return pr;
    }

    /// <summary>
    /// Creates a standalone purchase return not linked to any invoice.
    /// </summary>
    public static PurchaseReturn CreateStandalone(
        string returnNo,
        int warehouseId,
        int supplierId,
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
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (currencyId.HasValue && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");

        var pr = new PurchaseReturn
        {
            ReturnNo = returnNo,
            WarehouseId = warehouseId,
            SupplierId = supplierId,
            PurchaseInvoiceId = null,
            ReturnDate = returnDate ?? DateTime.UtcNow,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            Notes = notes,
            LinkToInvoice = false,
            DiscountAmount = 0,
            Status = InvoiceStatus.Draft
        };
        pr.SetCreatedBy(userId);
        return pr;
    }

    public void AddItem(int productId, decimal quantity, decimal unitCost, int productUnitId = 0, decimal discountAmount = 0, decimal? costInBaseCurrency = null, SaleMode mode = SaleMode.Retail, string? notes = null)
    {
        var item = PurchaseReturnItem.Create(productId, quantity, unitCost, productUnitId, discountAmount, costInBaseCurrency, mode, notes);
        Items.Add(item);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.LineTotal);

        // Calculate effective discount (amount or percentage)
        var effectiveDiscount = DiscountAmount;
        if (DiscountType == Enums.DiscountType.Percentage && DiscountRate.HasValue)
        {
            effectiveDiscount = SubTotal * (DiscountRate.Value / 100m);
        }

        TotalAmount = SubTotal - effectiveDiscount;
    }

    /// <summary>
    /// Sets discount with support for both amount and percentage discount types.
    /// </summary>
    public void SetDiscount(decimal discountAmount, DiscountType? discountType = null, decimal? discountRate = null)
    {
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (discountType == Enums.DiscountType.Percentage && (!discountRate.HasValue || discountRate < 0 || discountRate > 100))
            throw new DomainException("نسبة الخصم يجب أن تكون بين 0 و 100.");
        if (discountType != Enums.DiscountType.Percentage && discountRate.HasValue)
            throw new DomainException("نسبة الخصم تستخدم فقط مع خصم النسبة المئوية.");

        DiscountAmount = discountAmount;
        DiscountType = discountType;
        DiscountRate = discountRate;
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط المرتجعات المسودة يمكن ترحيلها.");
        if (!Items.Any())
            throw new DomainException("لا يمكن ترحيل مرتجع بدون أصناف.");

        RecalculateTotals();
        Status = InvoiceStatus.Posted;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("المرتجع ملغى بالفعل.");
        Status = InvoiceStatus.Cancelled;
    }
}
