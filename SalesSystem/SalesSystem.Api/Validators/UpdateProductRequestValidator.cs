using FluentValidation;
using SalesSystem.Contracts.Requests.Products;

namespace SalesSystem.Api.Validators;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المنتج مطلوب")
            .MaximumLength(200).WithMessage("اسم المنتج لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("كود المنتج لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.Barcode)
            .MaximumLength(50).WithMessage("الباركود لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر البيع لا يمكن أن يكون سالباً");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر الشراء لا يمكن أن يكون سالباً");

        RuleFor(x => x.MinStock)
            .GreaterThanOrEqualTo(0).WithMessage("الحد الأدنى للمخزون لا يمكن أن يكون سالباً");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("يجب اختيار تصنيف صحيح");

        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("يجب اختيار وحدة صحيحة");
            
        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("حالة النشاط مطلوبة");
    }
}
