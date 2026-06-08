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

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).When(x => x.PhoneNumber != null)
            .WithMessage("رقم الجوال لا يمكن أن يتجاوز 20 رقم");

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50).When(x => x.TaxNumber != null)
            .WithMessage("الرقم الضريبي لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.Address)
            .MaximumLength(500).When(x => x.Address != null)
            .WithMessage("العنوان لا يمكن أن يتجاوز 500 حرف");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}
