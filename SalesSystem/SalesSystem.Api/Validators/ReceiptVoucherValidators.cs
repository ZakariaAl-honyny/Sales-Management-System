using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateReceiptVoucherRequestValidator : AbstractValidator<CreateReceiptVoucherRequest>
{
    public CreateReceiptVoucherRequestValidator()
    {
        RuleFor(x => x.VoucherDate)
            .NotEmpty().WithMessage("تاريخ سند القبض مطلوب");

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

public class UpdateReceiptVoucherRequestValidator : AbstractValidator<UpdateReceiptVoucherRequest>
{
    public UpdateReceiptVoucherRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("الملاحظات يجب أن لا تتجاوز 500 حرف")
            .When(x => x.Notes != null);
    }
}
