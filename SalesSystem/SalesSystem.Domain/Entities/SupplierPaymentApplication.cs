using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Tracks the allocation of a supplier payment amount to a specific purchase invoice.
/// One payment can be applied across multiple invoices, and one invoice can be
/// settled by multiple payments.
/// </summary>
public class SupplierPaymentApplication : Entity
{
    public int SupplierPaymentId { get; private set; }
    public int PurchaseInvoiceId { get; private set; }
    public decimal AppliedAmount { get; private set; }

    // Navigation properties
    public virtual SupplierPayment? SupplierPayment { get; private set; }
    public virtual PurchaseInvoice? PurchaseInvoice { get; private set; }

    private SupplierPaymentApplication() { } // EF Core

    /// <summary>
    /// Creates a new allocation of a supplier payment to a purchase invoice.
    /// </summary>
    public static SupplierPaymentApplication Create(
        int supplierPaymentId,
        int purchaseInvoiceId,
        decimal appliedAmount,
        int? createdByUserId = null)
    {
        if (supplierPaymentId <= 0)
            throw new DomainException("معرف سند الدفع مطلوب.");
        if (purchaseInvoiceId <= 0)
            throw new DomainException("فاتورة المشتريات مطلوبة.");
        if (appliedAmount <= 0)
            throw new DomainException("المبلغ المطبق يجب أن يكون أكبر من الصفر.");

        var application = new SupplierPaymentApplication
        {
            SupplierPaymentId = supplierPaymentId,
            PurchaseInvoiceId = purchaseInvoiceId,
            AppliedAmount = appliedAmount
        };
        return application;
    }
}
