using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class AddProductUnitRequestValidator : AbstractValidator<AddProductUnitRequest>
{
    public AddProductUnitRequestValidator()
    {
        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("يجب اختيار وحدة قياس صحيحة");

        RuleFor(x => x.ConversionFactor)
            .GreaterThan(0).WithMessage("معامل التحويل يجب أن يكون أكبر من صفر");

        RuleFor(x => x.IsBaseUnit)
            .NotNull().WithMessage("يرجى تحديد ما إذا كانت هذه هي الوحدة الأساسية");
    }
}
