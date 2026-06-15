using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// A single line in a sales return — links back to the original invoice line.
/// Schema 6.4: SalesReturnLines. Columns: SalesReturnId FK, SalesInvoiceLineId FK (NOT null),
/// Quantity decimal(18,3), Amount decimal(18,2).
/// </summary>
public class SalesReturnLine : Entity
{
    public int SalesReturnId { get; private set; }
    public int SalesInvoiceLineId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Amount { get; private set; }

    // Navigation properties
    public virtual SalesReturn? SalesReturn { get; private set; }
    public virtual SalesInvoiceLine? SalesInvoiceLine { get; private set; }

    private SalesReturnLine() { } // EF Core

    public static SalesReturnLine Create(int salesInvoiceLineId, decimal quantity, decimal amount)
    {
        if (salesInvoiceLineId <= 0)
            throw new DomainException("رقم بند الفاتورة الأصلي مطلوب.");
        if (quantity <= 0)
            throw new DomainException("الكمية يجب أن تكون أكبر من الصفر.");
        if (amount < 0)
            throw new DomainException("المبلغ لا يمكن أن يكون سالباً.");

        return new SalesReturnLine
        {
            SalesInvoiceLineId = salesInvoiceLineId,
            Quantity = quantity,
            Amount = amount
        };
    }
}

/// <summary>
/// Represents a sales return (return of goods from a customer).
/// Schema 6.3: SalesReturns. Status: Draft=1, Posted=2, Cancelled=3.
/// </summary>
public class SalesReturn : DocumentEntity
{
    public int ReturnNo { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public int SalesInvoiceId { get; private set; }
    public int CustomerId { get; private set; }
    public short WarehouseId { get; private set; }
    public short CurrencyId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation properties
    public virtual SalesInvoice? SalesInvoice { get; private set; }
    public virtual Customer? Customer { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }

    private readonly List<SalesReturnLine> _lines = new();
    public IReadOnlyCollection<SalesReturnLine> Lines => _lines.AsReadOnly();

    private SalesReturn() { } // EF Core

    public static SalesReturn Create(
        int returnNo,
        int salesInvoiceId,
        int customerId,
        short warehouseId,
        short currencyId,
        DateTime? returnDate = null,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (returnNo <= 0)
            throw new DomainException("رقم الإرجاع مطلوب.");
        if (salesInvoiceId <= 0)
            throw new DomainException("فاتورة المبيعات الأصلية مطلوبة.");
        if (customerId <= 0)
            throw new DomainException("العميل مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (currencyId <= 0)
            throw new DomainException("العملة مطلوبة.");

        var sr = new SalesReturn
        {
            ReturnNo = returnNo,
            SalesInvoiceId = salesInvoiceId,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            CurrencyId = currencyId,
            ReturnDate = returnDate ?? DateTime.UtcNow,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        sr.SetCreatedBy(createdByUserId);
        return sr;
    }

    public void AddLine(SalesReturnLine line)
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

    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط مرتجعات المبيعات المسودة يمكن ترحيلها.");
        if (!_lines.Any())
            throw new DomainException("لا يمكن ترحيل مرتجع بدون أصناف.");
        PostedAt = DateTime.UtcNow;
        Status = InvoiceStatus.Posted;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("مرتجع المبيعات ملغي بالفعل.");
        CancelledAt = DateTime.UtcNow;
        Status = InvoiceStatus.Cancelled;
        UpdateTimestamp();
    }
}
