using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

public class PurchaseOrder : BaseEntity
{
    public int OrderNo { get; private set; }
    public int SupplierId { get; private set; }
    public int WarehouseId { get; private set; }
    public int? CurrencyId { get; private set; }
    public decimal? ExchangeRate { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateOnly? ExpectedDate { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }

    // Navigation properties
    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }
    private readonly List<PurchaseOrderItem> _items = new();
    public IReadOnlyCollection<PurchaseOrderItem> Items => _items.AsReadOnly();

    private PurchaseOrder() { }

    public static PurchaseOrder Create(
        int orderNo,
        int supplierId,
        int warehouseId,
        DateTime? orderDate = null,
        DateOnly? expectedDate = null,
        decimal discountAmount = 0,
        string? notes = null,
        int? currencyId = null,
        decimal? exchangeRate = null,
        int? createdByUserId = null)
    {
        if (orderNo <= 0)
            throw new DomainException("رقم أمر الشراء غير صحيح.");
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (currencyId.HasValue && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");

        var order = new PurchaseOrder
        {
            OrderNo = orderNo,
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            OrderDate = orderDate ?? DateTime.UtcNow,
            ExpectedDate = expectedDate,
            DiscountAmount = discountAmount,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            Notes = notes,
            Status = PurchaseOrderStatus.Draft,
            IsActive = true
        };
        order.SetCreatedBy(createdByUserId);
        return order;
    }

    public void AddItem(PurchaseOrderItem item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لأمر شراء غير مسودة.");

        _items.Add(item);
        RecalculateTotals();
    }

    public void RemoveItem(PurchaseOrderItem item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException("لا يمكن حذف أصناف من أمر شراء غير مسودة.");

        _items.Remove(item);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        SubTotal = _items.Sum(i => i.LineTotal);
        TotalAmount = SubTotal - DiscountAmount + TaxAmount;
    }

    public void SetCurrency(int? currencyId, decimal? exchangeRate)
    {
        if (currencyId.HasValue && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");

        CurrencyId = currencyId;
        ExchangeRate = exchangeRate;
        UpdateTimestamp();
    }

    public void Approve()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException("فقط أوامر الشراء المسودة يمكن اعتمادها.");
        if (!_items.Any())
            throw new DomainException("لا يمكن اعتماد أمر شراء بدون أصناف.");

        RecalculateTotals();
        Status = PurchaseOrderStatus.Approved;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cancels the purchase order. Can only cancel Draft or Approved orders.
    /// </summary>
    public void Cancel()
    {
        if (Status == PurchaseOrderStatus.Cancelled)
            throw new DomainException("أمر الشراء ملغى بالفعل.");
        if (Status == PurchaseOrderStatus.Received)
            throw new DomainException("لا يمكن إلغاء أمر شراء تم استلامه بالكامل.");

        Status = PurchaseOrderStatus.Cancelled;
        UpdateTimestamp();
    }

    /// <summary>
    /// Receives a list of items against this purchase order.
    /// Each tuple contains (productId, productUnitId, qtyReceived).
    /// Updates the order status based on receipt progress.
    /// </summary>
    public void ReceiveItems(List<(int productId, int productUnitId, decimal qtyReceived)> receivedItems)
    {
        if (receivedItems == null || !receivedItems.Any())
            throw new DomainException("يجب تحديد الأصناف المستلمة.");
        if (Status != PurchaseOrderStatus.Approved && Status != PurchaseOrderStatus.PartiallyReceived)
            throw new DomainException("لا يمكن استلام أمر شراء غير معتمد.");

        foreach (var (productId, productUnitId, qtyReceived) in receivedItems)
        {
            var item = _items.FirstOrDefault(i =>
                i.ProductId == productId && i.ProductUnitId == productUnitId);
            if (item == null)
                throw new DomainException(
                    $"المنتج (رقم {productId}) غير موجود في أمر الشراء.");

            item.AddReceivedQuantity(qtyReceived);
        }

        // Update status based on receipt progress
        var allFullyReceived = _items.All(i => i.PendingReceiveQuantity == 0);
        var anyReceived = _items.Any(i => i.ReceivedQuantity > 0);

        if (allFullyReceived)
            Status = PurchaseOrderStatus.Received;
        else if (anyReceived)
            Status = PurchaseOrderStatus.PartiallyReceived;

        UpdateTimestamp();
    }

    public void SetDiscount(decimal discountAmount)
    {
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (discountAmount > SubTotal)
            throw new DomainException("الخصم لا يمكن أن يتجاوز مجموع الأصناف.");

        DiscountAmount = discountAmount;
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void SetTaxAmount(decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");

        TaxAmount = taxAmount;
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
        UpdateTimestamp();
    }

    public void UpdateExpectedDate(DateOnly? expectedDate)
    {
        ExpectedDate = expectedDate;
        UpdateTimestamp();
    }
}
