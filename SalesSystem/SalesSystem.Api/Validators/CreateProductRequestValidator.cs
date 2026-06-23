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

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("التصنيف مطلوب");

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("مستوى إعادة الطلب لا يمكن أن يكون سالباً");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("الوصف لا يمكن أن يتجاوز 500 حرف");

        RuleFor(x => x.ImagePath)
            .MaximumLength(500).WithMessage("مسار الصورة لا يمكن أن يتجاوز 500 حرف")
            .When(x => !string.IsNullOrEmpty(x.ImagePath));

        RuleFor(x => x.Barcode)
            .MaximumLength(50).WithMessage("الباركود لا يمكن أن يتجاوز 50 حرف")
            .When(x => !string.IsNullOrEmpty(x.Barcode));
    }
}
