using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a Sales Quotation — a non-binding price quote given to a customer.
/// Quotations have NO stock or accounting impact until converted to an invoice.
/// Status lifecycle: Draft(1) → Sent(2) → Accepted(3) → Converted(4)
///                                         → Rejected(5)
///                   Draft(1) → Rejected(5)
/// </summary>
public class SalesQuotation : AuditableEntity
{
    // ─── Fields ────────────────────────────────────────────────────────
    private readonly List<SalesQuotationItem> _items = new();

    // ─── Properties ────────────────────────────────────────────────────
    /// <summary>User-facing quotation number (auto-generated, int, unique).</summary>
    public int QuotationNo { get; private set; }
    /// <summary>Date the quotation was issued.</summary>
    public DateTime QuotationDate { get; private set; }
    /// <summary>Date until which this quotation is valid (optional).</summary>
    public DateTime? ValidUntil { get; private set; }
    public int CustomerId { get; private set; }
    public short WarehouseId { get; private set; }
    public short CurrencyId { get; private set; }
    /// <summary>Exchange rate if CurrencyId is not the base currency.</summary>
    public decimal? ExchangeRate { get; private set; }
    /// <summary>Cash=1, Credit=2 (default Cash).</summary>
    public PaymentType PaymentType { get; private set; }
    /// <summary>Sum of all line totals BEFORE discount and tax.</summary>
    public decimal SubTotal { get; private set; }
    /// <summary>Global discount amount applied to the quotation.</summary>
    public decimal DiscountAmount { get; private set; }
    /// <summary>Tax amount.</summary>
    public decimal TaxAmount { get; private set; }
    /// <summary>Total amount = SubTotal − DiscountAmount + TaxAmount.</summary>
    public decimal TotalAmount { get; private set; }
    /// <summary>General notes (max 500 chars).</summary>
    public string? Notes { get; private set; }
    /// <summary>Terms and conditions (max 2000 chars).</summary>
    public string? TermsAndConditions { get; private set; }
    /// <summary>Current lifecycle status.</summary>
    public QuotationStatus Status { get; private set; }
    /// <summary>Invoice ID when this quotation was converted to an invoice.</summary>
    public int? ConvertedToInvoiceId { get; private set; }
    /// <summary>Reason provided when the quotation was rejected.</summary>
    public string? RejectionReason { get; private set; }

    // ─── Navigation Properties ─────────────────────────────────────────
    public virtual Customer? Customer { get; private set; }
    public virtual Warehouse? Warehouse { get; private set; }
    public virtual Currency? Currency { get; private set; }
    public IReadOnlyCollection<SalesQuotationItem> Items => _items.AsReadOnly();

    // ─── Constructors ──────────────────────────────────────────────────
    private SalesQuotation() { } // EF Core

    public static SalesQuotation Create(
        int quotationNo,
        int customerId,
        short warehouseId,
        short currencyId,
        DateTime? quotationDate = null,
        DateTime? validUntil = null,
        decimal? exchangeRate = null,
        PaymentType paymentType = PaymentType.Cash,
        decimal discountAmount = 0,
        decimal taxAmount = 0,
        string? notes = null,
        string? termsAndConditions = null,
        int? createdByUserId = null)
    {
        if (quotationNo <= 0)
            throw new DomainException("رقم عرض السعر مطلوب.");
        if (customerId <= 0)
            throw new DomainException("العميل مطلوب.");
        if (warehouseId <= 0)
            throw new DomainException("المستودع مطلوب.");
        if (currencyId <= 0)
            throw new DomainException("العملة مطلوبة.");
        if (discountAmount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (taxAmount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");
        if (currencyId > 0 && !exchangeRate.HasValue)
            throw new DomainException("يجب تحديد سعر الصرف عند اختيار العملة.");

        var quotation = new SalesQuotation
        {
            QuotationNo = quotationNo,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            CurrencyId = currencyId,
            QuotationDate = quotationDate ?? DateTime.UtcNow,
            ValidUntil = validUntil,
            ExchangeRate = exchangeRate,
            PaymentType = paymentType,
            DiscountAmount = discountAmount,
            TaxAmount = taxAmount,
            Notes = notes?.Trim(),
            TermsAndConditions = termsAndConditions?.Trim(),
            Status = QuotationStatus.Draft
        };
        quotation.SetCreatedBy(createdByUserId);
        quotation.RecalculateTotals();
        return quotation;
    }

    // ─── Item Management ───────────────────────────────────────────────
    public void AddItem(SalesQuotationItem item)
    {
        if (item == null)
            throw new DomainException("الصنف مطلوب.");
        if (Status != QuotationStatus.Draft)
            throw new DomainException("لا يمكن إضافة أصناف لعرض سعر غير مسودة.");

        _items.Add(item);
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void RemoveItem(int itemId)
    {
        if (Status != QuotationStatus.Draft)
            throw new DomainException("لا يمكن حذف أصناف من عرض سعر غير مسودة.");

        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new DomainException("الصنف غير موجود.");

        _items.Remove(item);
        RecalculateTotals();
        UpdateTimestamp();
    }

    // ─── Totals ────────────────────────────────────────────────────────
    public void RecalculateTotals()
    {
        SubTotal = _items.Sum(i => i.LineTotal);
        TotalAmount = SubTotal - DiscountAmount + TaxAmount;
    }

    // ─── State Transitions ─────────────────────────────────────────────
    public void Send()
    {
        if (Status != QuotationStatus.Draft)
            throw new DomainException("فقط عروض السعر المسودة يمكن إرسالها.");

        if (!_items.Any())
            throw new DomainException("لا يمكن إرسال عرض سعر بدون أصناف.");

        Status = QuotationStatus.Sent;
        UpdateTimestamp();
    }

    public void Accept()
    {
        if (Status != QuotationStatus.Sent)
            throw new DomainException("فقط عروض السعر المرسلة يمكن قبولها.");

        if (ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow)
            throw new DomainException("عرض السعر منتهي الصلاحية ولا يمكن قبوله.");

        Status = QuotationStatus.Accepted;
        UpdateTimestamp();
    }

    public void ConvertToInvoice(int invoiceId)
    {
        if (invoiceId <= 0)
            throw new DomainException("رقم الفاتورة غير صحيح.");

        if (Status != QuotationStatus.Accepted && Status != QuotationStatus.Sent)
            throw new DomainException("فقط عروض السعر المقبولة أو المرسلة يمكن تحويلها إلى فاتورة.");

        if (ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow)
            throw new DomainException("عرض السعر منتهي الصلاحية ولا يمكن تحويله إلى فاتورة.");

        ConvertedToInvoiceId = invoiceId;
        Status = QuotationStatus.Converted;
        UpdateTimestamp();
    }

    public void Reject(string? reason = null)
    {
        if (Status == QuotationStatus.Converted || Status == QuotationStatus.Rejected)
            throw new DomainException("عرض السعر في حالة نهائية ولا يمكن رفضه.");

        RejectionReason = reason?.Trim();
        Status = QuotationStatus.Rejected;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == QuotationStatus.Converted || Status == QuotationStatus.Rejected)
            throw new DomainException("عرض السعر في حالة نهائية ولا يمكن إلغاؤه.");

        RejectionReason = "ملغي";
        Status = QuotationStatus.Rejected;
        UpdateTimestamp();
    }

    public void SetDiscount(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("الخصم لا يمكن أن يكون سالباً.");
        if (Status != QuotationStatus.Draft)
            throw new DomainException("لا يمكن تعديل الخصم على عرض سعر غير مسودة.");

        DiscountAmount = amount;
        RecalculateTotals();
        UpdateTimestamp();
    }

    public void SetTax(decimal amount)
    {
        if (amount < 0)
            throw new DomainException("الضريبة لا يمكن أن تكون سالبة.");
        if (Status != QuotationStatus.Draft)
            throw new DomainException("لا يمكن تعديل الضريبة على عرض سعر غير مسودة.");

        TaxAmount = amount;
        RecalculateTotals();
        UpdateTimestamp();
    }

    // ─── Helpers ───────────────────────────────────────────────────────
    public bool IsExpired()
    {
        return ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow;
    }
}
