using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateSupplierPaymentApplicationRequestValidator : AbstractValidator<CreateSupplierPaymentApplicationRequest>
{
    public CreateSupplierPaymentApplicationRequestValidator()
    {
        RuleFor(x => x.SupplierPaymentId)
            .GreaterThan(0).WithMessage("معرف سند الدفع مطلوب");

        RuleFor(x => x.PurchaseInvoiceId)
            .GreaterThan(0).WithMessage("فاتورة المشتريات مطلوبة");

        RuleFor(x => x.AppliedAmount)
            .GreaterThan(0).WithMessage("المبلغ المطبق يجب أن يكون أكبر من الصفر");
    }
}
