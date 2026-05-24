using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class AddProductUnitRequestValidator : AbstractValidator<AddProductUnitRequest>
{
    public AddProductUnitRequestValidator()
    {
        RuleFor(x => x.UnitName)
            .NotEmpty().WithMessage("اسم الوحدة مطلوب")
            .MaximumLength(50).WithMessage("اسم الوحدة لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.ConversionFactor)
            .GreaterThan(0).WithMessage("معامل التحويل يجب أن يكون أكبر من صفر");

        RuleFor(x => x.RetailPrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر التجزئة لا يمكن أن يكون سالباً");

        RuleFor(x => x.WholesalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر الجملة لا يمكن أن يكون سالباً");
    }
}
