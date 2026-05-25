using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Payments;

public class UpdateCustomerPaymentValidator : AbstractValidator<UpdateCustomerPaymentRequest>
{
    public UpdateCustomerPaymentValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("يجب اختيار العميل");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("طريقة الدفع غير صحيحة");

        RuleFor(x => x.PaymentDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.PaymentDate.HasValue)
            .WithMessage("تاريخ الدفع لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}
