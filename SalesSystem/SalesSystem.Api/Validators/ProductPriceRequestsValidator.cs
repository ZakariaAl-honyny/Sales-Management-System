using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

/// <summary>
/// Validator for <see cref="CreateProductPriceRequest"/>.
/// Validates ProductUnitId > 0, CurrencyId > 0, Price > 0, EffectiveFrom != default, EffectiveTo > EffectiveFrom.
/// </summary>
public class CreateProductPriceRequestValidator : AbstractValidator<CreateProductPriceRequest>
{
    public CreateProductPriceRequestValidator()
    {
        RuleFor(x => x.ProductUnitId)
            .GreaterThan(0).WithMessage("معرف وحدة المنتج مطلوب");

        RuleFor(x => x.CurrencyId)
            .GreaterThan((short)0).WithMessage("معرف العملة مطلوب");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("السعر يجب أن يكون أكبر من الصفر")
            .PrecisionScale(18, 2, false).WithMessage("السعر يجب أن يكون برقمين عشريين كحد أقصى");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("تاريخ بدء السعر مطلوب")
            .Must(d => d != default).WithMessage("تاريخ بدء السعر غير صالح");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveTo.HasValue)
            .WithMessage("تاريخ انتهاء السعر يجب أن يكون بعد تاريخ البداية");
    }
}

/// <summary>
/// Validator for <see cref="UpdateProductPriceRequest"/>.
/// Validates Price > 0, EffectiveTo > EffectiveFrom (when both provided).
/// </summary>
public class UpdateProductPriceRequestValidator : AbstractValidator<UpdateProductPriceRequest>
{
    public UpdateProductPriceRequestValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("السعر يجب أن يكون أكبر من الصفر")
            .PrecisionScale(18, 2, false).WithMessage("السعر يجب أن يكون برقمين عشريين كحد أقصى");

        RuleFor(x => x.EffectiveFrom)
            .Must(d => d == null || d.Value != default)
            .WithMessage("تاريخ بدء السعر غير صالح");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom!.Value)
            .When(x => x.EffectiveTo.HasValue && x.EffectiveFrom.HasValue)
            .WithMessage("تاريخ انتهاء السعر يجب أن يكون بعد تاريخ البداية");
    }
}
