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

        RuleFor(x => x.Type)
            .InclusiveBetween((byte)1, (byte)4)
            .WithMessage("نوع المخزن يجب أن يكون بين 1 و 4");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("الموقع لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("رقم الهاتف لا يمكن أن يتجاوز 20 حرف");

        RuleFor(x => x.Address)
            .MaximumLength(250).WithMessage("العنوان التفصيلي لا يمكن أن يتجاوز 250 حرف");

        RuleFor(x => x.ManagerName)
            .MaximumLength(100).WithMessage("اسم المدير لا يمكن أن يتجاوز 100 حرف");
    }
}

public class UpdateWarehouseRequestValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المخزن مطلوب")
            .MaximumLength(100).WithMessage("اسم المخزن لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Type)
            .InclusiveBetween((byte)1, (byte)4)
            .WithMessage("نوع المخزن يجب أن يكون بين 1 و 4");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("الموقع لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("رقم الهاتف لا يمكن أن يتجاوز 20 حرف");

        RuleFor(x => x.Address)
            .MaximumLength(250).WithMessage("العنوان التفصيلي لا يمكن أن يتجاوز 250 حرف");

        RuleFor(x => x.ManagerName)
            .MaximumLength(100).WithMessage("اسم المدير لا يمكن أن يتجاوز 100 حرف");
    }
}
