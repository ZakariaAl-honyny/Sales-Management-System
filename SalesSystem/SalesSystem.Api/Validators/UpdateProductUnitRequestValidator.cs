using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class UpdateProductUnitRequestValidator : AbstractValidator<UpdateProductUnitRequest>
{
    public UpdateProductUnitRequestValidator()
    {
        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("يجب اختيار وحدة قياس صحيحة");
    }
}
