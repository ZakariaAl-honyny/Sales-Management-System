using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المنتج مطلوب")
            .MaximumLength(150).WithMessage("اسم المنتج لا يمكن أن يتجاوز 150 حرف");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("الباركود لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.MinStock)
            .GreaterThanOrEqualTo(0).WithMessage("الحد الأدنى للمخزون لا يمكن أن يكون سالباً");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).When(x => x.CategoryId.HasValue).WithMessage("يجب اختيار تصنيف صحيح");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("الوصف لا يمكن أن يتجاوز 500 حرف");
    }
}
