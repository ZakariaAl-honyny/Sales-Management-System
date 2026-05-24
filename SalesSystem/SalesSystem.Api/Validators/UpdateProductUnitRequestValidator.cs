using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class UpdateProductUnitRequestValidator : AbstractValidator<UpdateProductUnitRequest>
{
    public UpdateProductUnitRequestValidator()
    {
        RuleFor(x => x.UnitName)
            .NotEmpty().WithMessage("اسم الوحدة مطلوب")
            .MaximumLength(50).WithMessage("اسم الوحدة لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.RetailPrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر التجزئة لا يمكن أن يكون سالباً");

        RuleFor(x => x.WholesalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("سعر الجملة لا يمكن أن يكون سالباً");
    }
}
