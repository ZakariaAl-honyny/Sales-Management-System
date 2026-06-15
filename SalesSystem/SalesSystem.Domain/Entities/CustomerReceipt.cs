using SalesSystem.Domain.Common;
using SalesSystem.Domain.Enums;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Represents a receipt collected from a customer against one or more sales invoices.
/// Schema 6.5: CustomerReceipts. Columns: ReceiptNo (int unique), ReceiptDate, CustomerId FK,
/// CashBoxId FK, CurrencyId FK, Amount, Notes, Status (tinyint), audit fields.
/// </summary>
public class CustomerReceipt : DocumentEntity
{
    public int ReceiptNo { get; private set; }
    public DateTime ReceiptDate { get; private set; }
    public int CustomerId { get; private set; }
    public int CashBoxId { get; private set; }
    public short CurrencyId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Notes { get; private set; }
    public InvoiceStatus Status { get; private set; }

    // Navigation properties
    public virtual Customer? Customer { get; private set; }
    public virtual CashBox? CashBox { get; private set; }
    public virtual Currency? Currency { get; private set; }

    private readonly List<CustomerReceiptApplication> _applications = new();
    public IReadOnlyCollection<CustomerReceiptApplication> Applications => _applications.AsReadOnly();

    private CustomerReceipt() { } // EF Core

    public static CustomerReceipt Create(
        int receiptNo,
        DateTime receiptDate,
        int customerId,
        int cashBoxId,
        short currencyId,
        decimal amount,
        string? notes = null,
        int? createdByUserId = null)
    {
        if (receiptNo <= 0)
            throw new DomainException("رقم السند يجب أن يكون أكبر من الصفر.");
        if (customerId <= 0)
            throw new DomainException("العميل مطلوب.");
        if (cashBoxId <= 0)
            throw new DomainException("الصندوق مطلوب.");
        if (currencyId <= 0)
            throw new DomainException("العملة مطلوبة.");
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");

        var receipt = new CustomerReceipt
        {
            ReceiptNo = receiptNo,
            ReceiptDate = receiptDate.Kind == DateTimeKind.Utc
                ? receiptDate
                : receiptDate.ToUniversalTime(),
            CustomerId = customerId,
            CashBoxId = cashBoxId,
            CurrencyId = currencyId,
            Amount = amount,
            Notes = notes,
            Status = InvoiceStatus.Draft
        };
        receipt.SetCreatedBy(createdByUserId);
        return receipt;
    }

    /// <summary>
    /// Posts the receipt — confirms the collection. Only drafts can be posted.
    /// </summary>
    public void Post()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط السندات المسودة يمكن ترحيلها.");
        PostedAt = DateTime.UtcNow;
        Status = InvoiceStatus.Posted;
        UpdateTimestamp();
    }

    /// <summary>
    /// Cancels the receipt. Only drafts and posted (un-cancelled) receipts can be cancelled.
    /// </summary>
    public void Cancel()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new DomainException("السند ملغي بالفعل.");
        CancelledAt = DateTime.UtcNow;
        Status = InvoiceStatus.Cancelled;
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates the receipt fields. Only allowed while the receipt is in Draft status.
    /// </summary>
    public void Update(
        int cashBoxId,
        short currencyId,
        decimal amount,
        string? notes,
        int? updatedByUserId = null)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("فقط السندات المسودة يمكن تعديلها.");
        if (cashBoxId <= 0)
            throw new DomainException("الصندوق مطلوب.");
        if (currencyId <= 0)
            throw new DomainException("العملة مطلوبة.");
        if (amount <= 0)
            throw new DomainException("المبلغ يجب أن يكون أكبر من الصفر.");

        CashBoxId = cashBoxId;
        CurrencyId = currencyId;
        Amount = amount;
        if (notes != null) Notes = notes;
        SetUpdatedBy(updatedByUserId);
        UpdateTimestamp();
    }

    /// <summary>
    /// Adds an application of this receipt amount to a specific sales invoice.
    /// Only allowed while the receipt is in Draft status.
    /// </summary>
    public void AddApplication(CustomerReceiptApplication application)
    {
        if (application == null)
            throw new DomainException("التخصيص مطلوب.");
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("لا يمكن إضافة تخصيصات لسند غير مسود.");
        _applications.Add(application);
        UpdateTimestamp();
    }
}
