using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateProductCategoryRequestValidator : AbstractValidator<CreateProductCategoryRequest>
{
    public CreateProductCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم التصنيف مطلوب")
            .MaximumLength(100).WithMessage("اسم التصنيف لا يمكن أن يتجاوز 100 حرف");
    }
}

public class UpdateProductCategoryRequestValidator : AbstractValidator<UpdateProductCategoryRequest>
{
    public UpdateProductCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم التصنيف مطلوب")
            .MaximumLength(100).WithMessage("اسم التصنيف لا يمكن أن يتجاوز 100 حرف");
    }
}
