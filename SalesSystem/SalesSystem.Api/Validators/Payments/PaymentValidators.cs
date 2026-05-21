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
    }
}