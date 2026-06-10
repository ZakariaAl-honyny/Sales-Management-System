using FluentValidation;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Validators;

/// <summary>
/// Validates a single product import row sent from the Desktop client.
/// </summary>
public class ProductImportRowDtoValidator : AbstractValidator<ProductImportRowDto>
{
    public ProductImportRowDtoValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("اسم المنتج مطلوب")
            .MaximumLength(200).WithMessage("اسم المنتج لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.BaseUnitId)
            .GreaterThan(0).WithMessage("الوحدة الأساسية مطلوبة")
            .When(x => x.BaseUnitId.HasValue);

        RuleFor(x => x.Barcode)
            .MaximumLength(50).WithMessage("الباركود لا يمكن أن يتجاوز 50 حرف")
            .When(x => !string.IsNullOrEmpty(x.Barcode));

        RuleFor(x => x.CategoryName)
            .MaximumLength(100).WithMessage("اسم التصنيف لا يمكن أن يتجاوز 100 حرف")
            .When(x => !string.IsNullOrEmpty(x.CategoryName));

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("الحد الأدنى للمخزون لا يمكن أن يكون سالباً")
            .When(x => x.MinStockLevel.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("الوصف لا يمكن أن يتجاوز 500 حرف")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
