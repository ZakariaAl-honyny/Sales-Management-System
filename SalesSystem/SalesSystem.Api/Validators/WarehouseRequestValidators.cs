using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المخزن مطلوب")
            .MaximumLength(100).WithMessage("اسم المخزن لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("العنوان لا يمكن أن يتجاوز 200 حرف");
    }
}

public class UpdateWarehouseRequestValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المخزن مطلوب")
            .MaximumLength(100).WithMessage("اسم المخزن لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("العنوان لا يمكن أن يتجاوز 200 حرف");
    }
}

