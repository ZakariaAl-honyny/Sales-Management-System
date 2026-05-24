using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Payments;

public class CreateCustomerPaymentValidator : AbstractValidator<CreateCustomerPaymentRequest>
{
    public CreateCustomerPaymentValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("يجب اختيار العميل");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("طريقة الدفع غير صحيحة");

        RuleFor(x => x.SalesInvoiceId)
            .GreaterThan(0).When(x => x.SalesInvoiceId.HasValue)
            .WithMessage("رقم فاتورة البيع غير صحيح");

        RuleFor(x => x.PaymentDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.PaymentDate.HasValue)
            .WithMessage("تاريخ الدفع لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}

public class CreateSupplierPaymentValidator : AbstractValidator<CreateSupplierPaymentRequest>
{
    public CreateSupplierPaymentValidator()
    {
        RuleFor(x => x.SupplierId)
            .GreaterThan(0).WithMessage("يجب اختيار المورد");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("طريقة الدفع غير صحيحة");

        RuleFor(x => x.PurchaseInvoiceId)
            .GreaterThan(0).When(x => x.PurchaseInvoiceId.HasValue)
            .WithMessage("رقم فاتورة الشراء غير صحيح");

        RuleFor(x => x.PaymentDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.PaymentDate.HasValue)
            .WithMessage("تاريخ الدفع لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}