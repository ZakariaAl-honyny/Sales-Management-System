using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المخزن مطلوب")
            .MaximumLength(150).WithMessage("اسم المخزن لا يمكن أن يتجاوز 150 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("رقم الهاتف لا يمكن أن يتجاوز 20 حرف");

        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("العنوان لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}

public class UpdateWarehouseRequestValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المخزن مطلوب")
            .MaximumLength(150).WithMessage("اسم المخزن لا يمكن أن يتجاوز 150 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("رقم الهاتف لا يمكن أن يتجاوز 20 حرف");

        RuleFor(x => x.Address)
            .MaximumLength(200).WithMessage("العنوان لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}
