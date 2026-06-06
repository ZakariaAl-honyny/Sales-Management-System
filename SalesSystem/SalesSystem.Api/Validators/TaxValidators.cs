using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateTaxRequestValidator : AbstractValidator<CreateTaxRequest>
{
    public CreateTaxRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم الضريبة مطلوب")
            .MaximumLength(100).WithMessage("اسم الضريبة لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Rate)
            .InclusiveBetween(0m, 100m).WithMessage("نسبة الضريبة يجب أن تكون بين 0 و 100");
    }
}

public class UpdateTaxRequestValidator : AbstractValidator<UpdateTaxRequest>
{
    public UpdateTaxRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم الضريبة مطلوب")
            .MaximumLength(100).WithMessage("اسم الضريبة لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Rate)
            .InclusiveBetween(0m, 100m).WithMessage("نسبة الضريبة يجب أن تكون بين 0 و 100");
    }
}
