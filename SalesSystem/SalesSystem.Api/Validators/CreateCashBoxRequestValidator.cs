using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateCashBoxRequestValidator : AbstractValidator<CreateCashBoxRequest>
{
    public CreateCashBoxRequestValidator()
    {
        RuleFor(x => x.BoxName)
            .NotEmpty().WithMessage("اسم الخزنة مطلوب")
            .MaximumLength(100).WithMessage("اسم الخزنة لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0).WithMessage("الرصيد الافتتاحي لا يمكن أن يكون سالباً");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}
