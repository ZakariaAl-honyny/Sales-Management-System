using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// أمر شراء — مستند طلب شراء بضاعة من المورد
/// </summary>
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

    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }
    public virtual List<PurchaseOrderItem> Items { get; private set; } = new();

    private PurchaseOrder() { }

    public static PurchaseOrder Create(
        int supplierId,
        int warehouseId,
        int orderNo,
        DateTime? orderDate = null,
        DateOnly? expectedDate = null,
        int? currencyId = null,
        decimal? exchangeRate = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (orderNo <= 0)
            throw new DomainException("رقم الأمر يجب أن يكون أكبر من الصفر.");
        if (currencyId.HasValue && (!exchangeRate.HasValue || exchangeRate <= 0))
            throw new DomainException("سعر الصرف مطلوب عند اختيار عملة أجنبية.");
        if (!string.IsNullOrEmpty(notes) && notes.Length > 500)
            throw new DomainException("الملاحظات لا تتجاوز 500 حرف.");

        var order = new PurchaseOrder
        {
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            OrderNo = orderNo,
            CurrencyId = currencyId,
            ExchangeRate = exchangeRate,
            OrderDate = orderDate ?? DateTime.UtcNow,
            ExpectedDate = expectedDate,
            Status = PurchaseOrderStatus.Draft,
            SubTotal = 0,
            DiscountAmount = 0,
            TaxAmount = 0,
            TotalAmount = 0,
            Notes = notes,
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

        Items.Add(item);
        RecalculateTotals();
    }

    public void RemoveItem(PurchaseOrderItem item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException("لا يمكن حذف أصناف من أمر شراء غير مسودة.");

        Items.Remove(item);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        SubTotal = Items.Sum(i => i.LineTotal);
        TotalAmount = SubTotal - DiscountAmount + TaxAmount;
    }

    public void Approve()
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException("يمكن اعتماد أوامر الشراء بحالة مسودة فقط.");
        if (!Items.Any())
            throw new DomainException("لا يمكن اعتماد أمر شراء بدون أصناف.");

        Status = PurchaseOrderStatus.Approved;
    }

    public void MarkReceived()
    {
        if (Status != PurchaseOrderStatus.Approved && Status != PurchaseOrderStatus.PartiallyReceived)
            throw new DomainException("يمكن استلام أوامر الشراء بحالة معتمدة أو مستلمة جزئياً فقط.");
        if (Items.Any(i => i.PendingReceiveQuantity > 0))
            throw new DomainException("لا يمكن تعيين الحالة إلى مستلم بالكامل — يوجد أصناف لم يتم استلامها بالكامل.");

        Status = PurchaseOrderStatus.Received;
    }

    public void Cancel()
    {
        if (Status == PurchaseOrderStatus.Received)
            throw new DomainException("لا يمكن إلغاء أمر شراء تم استلامه.");
        if (Status == PurchaseOrderStatus.Cancelled)
            throw new DomainException("أمر الشراء ملغي بالفعل.");

        Status = PurchaseOrderStatus.Cancelled;
    }

    public void SetOrderNo(int orderNo)
    {
        if (orderNo <= 0)
            throw new DomainException("رقم الأمر يجب أن يكون أكبر من الصفر.");
        if (Status != PurchaseOrderStatus.Draft)
            throw new DomainException("لا يمكن تغيير رقم الأمر بعد اعتماده.");

        OrderNo = orderNo;
        UpdateTimestamp();
    }

    public void UpdateReceivedQuantity(int productId, int productUnitId, decimal qty)
    {
        var item = Items.SingleOrDefault(i => i.ProductId == productId && i.ProductUnitId == productUnitId);
        if (item == null)
            throw new DomainException("الصنف غير موجود في أمر الشراء.");

        item.AddReceivedQuantity(qty);

        if (Items.All(i => i.PendingReceiveQuantity == 0))
            Status = PurchaseOrderStatus.Received;
        else if (Items.Any(i => i.ReceivedQuantity > 0))
            Status = PurchaseOrderStatus.PartiallyReceived;
    }
}
