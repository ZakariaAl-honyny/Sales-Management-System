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

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("التصنيف مطلوب");

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("مستوى إعادة الطلب لا يمكن أن يكون سالباً");

        RuleFor(x => x.TaxId)
            .Must(taxId => taxId > 0).WithMessage("معرف الضريبة يجب أن يكون أكبر من صفر")
            .When(x => x.TaxId.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("الوصف لا يمكن أن يتجاوز 500 حرف");

        RuleFor(x => x.ImagePath)
            .MaximumLength(500).WithMessage("مسار الصورة لا يمكن أن يتجاوز 500 حرف")
            .When(x => !string.IsNullOrEmpty(x.ImagePath));

        // ─── Opening Stock Validation ─────────────────────────────────
        RuleFor(x => x.OpeningQuantity)
            .GreaterThan(0).WithMessage("الكمية الافتتاحية يجب أن تكون أكبر من صفر")
            .When(x => x.OpeningQuantity.HasValue);

        RuleFor(x => x.OpeningUnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("تكلفة الوحدة الافتتاحية لا يمكن أن تكون سالبة")
            .When(x => x.OpeningUnitCost.HasValue);

        RuleFor(x => x.OpeningExpiryDate)
            .NotNull().WithMessage("تاريخ انتهاء الصلاحية مطلوب عند إدخال رصيد افتتاحي لمنتج له صلاحية")
            .When(x => x.TrackExpiry && x.OpeningQuantity.HasValue && x.OpeningQuantity > 0);
    }
}
