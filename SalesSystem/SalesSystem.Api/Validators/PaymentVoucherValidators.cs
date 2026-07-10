using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreatePaymentVoucherRequestValidator : AbstractValidator<CreatePaymentVoucherRequest>
{
    public CreatePaymentVoucherRequestValidator()
    {
        RuleFor(x => x.VoucherDate)
            .NotEmpty().WithMessage("تاريخ سند الصرف مطلوب");

        RuleFor(x => x.CashBoxId)
            .GreaterThan(0).WithMessage("الصندوق النقدي مطلوب");

        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("الحساب المحاسبي مطلوب");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من الصفر")
            .PrecisionScale(18, 2, false).WithMessage("المبلغ يجب أن لا يتجاوز 18 رقمًا و 2 منازل عشرية");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("الملاحظات يجب أن لا تتجاوز 500 حرف")
            .When(x => x.Notes != null);
    }
}

public class UpdatePaymentVoucherRequestValidator : AbstractValidator<UpdatePaymentVoucherRequest>
{
    public UpdatePaymentVoucherRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("الملاحظات يجب أن لا تتجاوز 500 حرف")
            .When(x => x.Notes != null);
    }
}
