using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Tracks the allocation of a customer receipt amount to a specific sales invoice.
/// One receipt can be applied across multiple invoices, and one invoice can be
/// settled by multiple receipts.
/// </summary>
public class CustomerReceiptApplication : Entity
{
    public int CustomerReceiptId { get; private set; }
    public int SalesInvoiceId { get; private set; }
    public decimal AppliedAmount { get; private set; }

    // Navigation properties
    public virtual CustomerReceipt? CustomerReceipt { get; private set; }
    public virtual SalesInvoice? SalesInvoice { get; private set; }

    private CustomerReceiptApplication() { } // EF Core

    /// <summary>
    /// Creates a new allocation of a customer receipt to a sales invoice.
    /// </summary>
    public static CustomerReceiptApplication Create(
        int customerReceiptId,
        int salesInvoiceId,
        decimal appliedAmount,
        int? createdByUserId = null)
    {
        if (customerReceiptId <= 0)
            throw new DomainException("معرف سند القبض مطلوب.");
        if (salesInvoiceId <= 0)
            throw new DomainException("فاتورة المبيعات مطلوبة.");
        if (appliedAmount <= 0)
            throw new DomainException("المبلغ المطبق يجب أن يكون أكبر من الصفر.");

        var application = new CustomerReceiptApplication
        {
            CustomerReceiptId = customerReceiptId,
            SalesInvoiceId = salesInvoiceId,
            AppliedAmount = appliedAmount
        };
        return application;
    }
}
