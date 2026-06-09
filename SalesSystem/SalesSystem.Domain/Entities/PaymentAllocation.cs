using SalesSystem.Domain.Common;
using SalesSystem.Domain.Exceptions;

namespace SalesSystem.Domain.Entities;

/// <summary>
/// Tracks how a customer or supplier payment is allocated across multiple invoices.
/// One payment can settle multiple invoices; one invoice can be settled by multiple payments.
/// </summary>
public class PaymentAllocation : BaseEntity
{
    /// <summary>
    /// FK to CustomerPayment. Only set when this allocation belongs to a customer payment.
    /// </summary>
    public int? CustomerPaymentId { get; private set; }

    /// <summary>
    /// FK to SupplierPayment. Only set when this allocation belongs to a supplier payment.
    /// </summary>
    public int? SupplierPaymentId { get; private set; }

    /// <summary>
    /// The ID of the invoice being allocated against.
    /// This can be a SalesInvoiceId or PurchaseInvoiceId depending on InvoiceType.
    /// </summary>
    public int InvoiceId { get; private set; }

    /// <summary>
    /// The type of invoice: 1 = Sales, 2 = Purchase.
    /// </summary>
    public byte InvoiceType { get; private set; }

    /// <summary>
    /// The amount allocated to this invoice from the payment.
    /// </summary>
    public decimal AllocatedAmount { get; private set; }

    // Navigation
    public virtual CustomerPayment? CustomerPayment { get; private set; }
    public virtual SupplierPayment? SupplierPayment { get; private set; }

    private PaymentAllocation() { } // EF Core

    public static PaymentAllocation Create(
        decimal allocatedAmount,
        int invoiceId,
        byte invoiceType,
        int? customerPaymentId = null,
        int? supplierPaymentId = null,
        int? createdByUserId = null)
    {
        if (allocatedAmount <= 0)
            throw new DomainException("المبلغ المخصص يجب أن يكون أكبر من الصفر");

        if (invoiceId <= 0)
            throw new DomainException("معرف الفاتورة غير صالح");

        if (invoiceType != 1 && invoiceType != 2)
            throw new DomainException("نوع الفاتورة غير صالح — 1 للمبيعات، 2 للمشتريات");

        if (!customerPaymentId.HasValue && !supplierPaymentId.HasValue)
            throw new DomainException("يجب تحديد إما سداد عميل أو سداد مورد");

        if (customerPaymentId.HasValue && supplierPaymentId.HasValue)
            throw new DomainException("لا يمكن أن ينتمي التخصيص إلى سداد عميل وسداد مورد معاً");

        var allocation = new PaymentAllocation
        {
            AllocatedAmount = allocatedAmount,
            InvoiceId = invoiceId,
            InvoiceType = invoiceType,
            CustomerPaymentId = customerPaymentId,
            SupplierPaymentId = supplierPaymentId,
            IsActive = true
        };
        allocation.SetCreatedBy(createdByUserId);
        return allocation;
    }
}
