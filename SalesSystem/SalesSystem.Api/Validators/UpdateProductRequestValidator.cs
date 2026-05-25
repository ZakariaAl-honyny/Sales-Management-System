using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المنتج مطلوب")
            .MaximumLength(200).WithMessage("اسم المنتج لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.Barcode)
            .MaximumLength(50).WithMessage("الباركود لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر البيع لا يمكن أن يكون سالباً");

        RuleFor(x => x.RetailPrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر التجزئة لا يمكن أن يكون سالباً");

        RuleFor(x => x.WholesalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر الجملة لا يمكن أن يكون سالباً");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر الشراء لا يمكن أن يكون سالباً");

        RuleFor(x => x.ConversionFactor)
            .GreaterThan(0).WithMessage("معامل التحويل يجب أن يكون أكبر من صفر");

        RuleFor(x => x.MinStock)
            .GreaterThanOrEqualTo(0).WithMessage("الحد الأدنى للمخزون لا يمكن أن يكون سالباً");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).When(x => x.CategoryId.HasValue).WithMessage("يجب اختيار تصنيف صحيح");

        RuleFor(x => x.RetailUnitId)
            .GreaterThan(0).When(x => x.RetailUnitId.HasValue).WithMessage("يجب اختيار وحدة التجزئة");

        RuleFor(x => x.WholesaleUnitId)
            .GreaterThan(0).When(x => x.WholesaleUnitId.HasValue).WithMessage("يجب اختيار وحدة الجملة");

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("حالة النشاط مطلوبة");

        RuleFor(x => x.ExpirationDate)
            .GreaterThan(DateTime.Today.AddDays(-1))
            .When(x => x.ExpirationDate.HasValue)
            .WithMessage("تاريخ الانتهاء لا يمكن أن يكون في الماضي");
    }
}
