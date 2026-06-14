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

        RuleFor(x => x.ParentId)
            .GreaterThan(0).When(x => x.ParentId.HasValue)
            .WithMessage("معرف التصنيف الأب غير صالح");
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
