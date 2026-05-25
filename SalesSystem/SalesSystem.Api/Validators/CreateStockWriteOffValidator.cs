using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

/// <summary>
/// Validator for CreateStockWriteOffRequest
/// </summary>
public class CreateStockWriteOffValidator : AbstractValidator<CreateStockWriteOffRequest>
{
    public CreateStockWriteOffValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("المنتج مطلوب");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("المستودع مطلوب");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من الصفر");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("السبب مطلوب")
            .MaximumLength(250).WithMessage("السبب لا يمكن أن يتجاوز 250 حرف");
    }
}
