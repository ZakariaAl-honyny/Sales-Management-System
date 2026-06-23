using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single line in a purchase return — links back to the original invoice line.
/// Schema 7.4: PurchaseReturnLines. Columns: PurchaseReturnId FK, PurchaseInvoiceLineId FK (NOT null),
/// Quantity decimal(18,3), Amount decimal(18,2).
/// </summary>
public class PurchaseReturnLine : Entity
{
    public int PurchaseReturnId { get; private set; }
    public int? PurchaseInvoiceLineId { get; private set; }
    public int ProductId { get; private set; }
    public int ProductUnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Amount { get; private set; }

    // Navigation properties
    public virtual PurchaseReturn? PurchaseReturn { get; private set; }
    public virtual PurchaseInvoiceLine? PurchaseInvoiceLine { get; private set; }
    public virtual Product? Product { get; private set; }
    public virtual ProductUnit? ProductUnit { get; private set; }

    private PurchaseReturnLine() { } // EF Core

    public static PurchaseReturnLine Create(
        int productId,
        int productUnitId,
        decimal quantity,
        decimal amount,
        int? purchaseInvoiceLineId = null)
    {
        if (productId <= 0)
            throw new DomainException("المنتج مطلوب.");
        if (productUnitId <= 0)
            throw new DomainException("الوحدة مطلوبة.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (amount < 0)
            throw new DomainException("المبلغ لا يمكن أن يكون سالباً.");

        return new PurchaseReturnLine
        {
            ProductId = productId,
            ProductUnitId = productUnitId,
            PurchaseInvoiceLineId = purchaseInvoiceLineId,
            Quantity = quantity,
            Amount = amount
        };
    }
}

/// <summary>
/// Represents a purchase return (return of goods to a supplier).
/// Schema 7.3: PurchaseReturns. Status: Draft=1, Posted=2, Cancelled=3.
/// Columns: ReturnNo (int unique), PurchaseInvoiceId FK (nullable — standalone mode), SupplierId FK,
/// WarehouseId FK, CurrencyId FK, TotalAmount, Notes, Status, audit fields.
/// </summary>
public class PurchaseReturn : DocumentEntity
{
    public int ReturnNo { get; private set; }
    public DateOnly ReturnDate { get; private set; }
    public int? PurchaseInvoiceId { get; private set; }
    public int SupplierId { get; private set; }
    public short WarehouseId { get; private set; }
    public short CurrencyId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal ReturnedDiscountAmount { get; private set; }
    public decimal ReturnedTaxAmount { get; private set; }
    public decimal ReturnedChargeAmount { get; private set; }
    public short? TaxId { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation properties
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }
    public virtual Supplier? Supplier { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }

    private readonly List<PurchaseReturnLine> _lines = new();
    public IReadOnlyCollection<PurchaseReturnLine> Lines => _lines.AsReadOnly();

    private PurchaseReturn() { } // EF Core

    public static PurchaseReturn Create(
        int returnNo,
        int supplierId,
        short warehouseId,
        short currencyId,
        DateOnly? returnDate = null,
        int? purchaseInvoiceId = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (returnNo <= 0)
            throw new DomainException("رقم الإرجاع مطلوب.");
        if (supplierId <= 0)
            throw new DomainException("المورد مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (currencyId <= 0)
            throw new DomainException("العملة مطلوبة.");

        var pr = new PurchaseReturn
        {
            ReturnNo = returnNo,
            PurchaseInvoiceId = purchaseInvoiceId,
            SupplierId = supplierId,
            WarehouseId = warehouseId,
            CurrencyId = currencyId,
            ReturnDate = returnDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        pr.SetCreatedBy(createdByUserId);
        return pr;
    }

    public void AddLine(PurchaseReturnLine line)
    {
        if (line == null)
            throw new DomainException("صنف الإرجاع مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لمرتجع غير مسودة.");
        _lines.Add(line);
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void RecalculateTotals()
    {
        TotalAmount = _lines.Sum(l => l.Amount);
    }

    /// <summary>
    /// Sets proportional amounts for discount, tax, and charges based on the original invoice.
    /// Called by the service layer BEFORE Post() — the service computes the ratio
    /// from the original invoice's SubTotal, DiscountAmount, TaxAmount, OtherCharges.
    /// </summary>
    public void SetProportionalAmounts(decimal discountPortion, decimal taxPortion, decimal chargesPortion, short? taxId)
    {
        ReturnedDiscountAmount = discountPortion;
        ReturnedTaxAmount = taxPortion;
        ReturnedChargeAmount = chargesPortion;
        TaxId = taxId;
    }

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط مرتجعات المشتريات المسودة يمكن ترحيلها.");
        if (!_lines.Any())
            throw new DomainException("لا يمكن ترحيل مرتجع بدون أصناف.");
        PostedAt = DateTime.UtcNow;
        Status = InvoiceStatus.Posted;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("مرتجع المشتريات ملغي بالفعل.");
        CancelledAt = DateTime.UtcNow;
        Status = InvoiceStatus.Cancelled;
        UpdateTimestamp();
    }
}
