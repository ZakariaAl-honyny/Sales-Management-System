using FluentValidation;
using SalesSystem.Contracts.Requests.Units;

namespace SalesSystem.Api.Validators;

public class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم الوحدة مطلوب")
            .MaximumLength(50).WithMessage("اسم الوحدة لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.Symbol)
            .MaximumLength(10).WithMessage("الرمز لا يمكن أن يتجاوز 10 أحرف");
    }
}

public class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم الوحدة مطلوب")
            .MaximumLength(50).WithMessage("اسم الوحدة لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.Symbol)
            .MaximumLength(10).WithMessage("الرمز لا يمكن أن يتجاوز 10 أحرف");
    }
}
