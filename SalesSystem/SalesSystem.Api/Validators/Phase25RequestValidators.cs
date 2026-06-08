using FluentValidation;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Api.Validators;

/// <summary>
/// Validator for <see cref="CreateProductPriceRequest"/>.
/// Validates Price > 0, PriceLevel is valid enum, CurrencyId > 0, ProductUnitId > 0, EffectiveFrom != default.
/// </summary>
public class CreateProductPriceRequestValidator : AbstractValidator<CreateProductPriceRequest>
{
    public CreateProductPriceRequestValidator()
    {
        RuleFor(x => x.ProductUnitId)
            .GreaterThan(0).WithMessage("معرف وحدة المنتج مطلوب");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("معرف العملة مطلوب");

        RuleFor(x => x.PriceLevel)
            .IsInEnum().WithMessage("مستوى السعر غير صالح")
            .NotEmpty().WithMessage("مستوى السعر مطلوب");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("السعر يجب أن يكون أكبر من الصفر");

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
/// Same field validations as Create but no ProductUnitId/CurrencyId (immutable after creation).
/// </summary>
public class UpdateProductPriceRequestValidator : AbstractValidator<UpdateProductPriceRequest>
{
    public UpdateProductPriceRequestValidator()
    {
        RuleFor(x => x.PriceLevel)
            .IsInEnum().WithMessage("مستوى السعر غير صالح");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("السعر يجب أن يكون أكبر من الصفر");

        RuleFor(x => x.EffectiveFrom)
            .Must(d => d == null || d != default).WithMessage("تاريخ بدء السعر غير صالح");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom ?? DateTime.MinValue)
            .When(x => x.EffectiveTo.HasValue && x.EffectiveFrom.HasValue)
            .WithMessage("تاريخ انتهاء السعر يجب أن يكون بعد تاريخ البداية");
    }
}

/// <summary>
/// Validator for <see cref="CreateInventoryBatchRequest"/>.
/// Validates Quantity > 0, UnitCost >= 0, ProductId > 0, WarehouseId > 0.
/// </summary>
public class CreateInventoryBatchRequestValidator : AbstractValidator<CreateInventoryBatchRequest>
{
    public CreateInventoryBatchRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("معرف المنتج مطلوب");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("معرف المستودع مطلوب");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من الصفر");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("تكلفة الوحدة لا يمكن أن تكون سالبة");

        RuleFor(x => x.BatchNo)
            .NotEmpty().WithMessage("رقم الدفعة مطلوب")
            .MaximumLength(100).WithMessage("رقم الدفعة لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.ManufactureDate)
            .LessThan(x => x.ExpiryDate)
            .When(x => x.ManufactureDate.HasValue && x.ExpiryDate.HasValue)
            .WithMessage("تاريخ التصنيع يجب أن يكون قبل تاريخ انتهاء الصلاحية");
    }
}

/// <summary>
/// Validator for <see cref="CreateProductImageRequest"/>.
/// Validates ProductId > 0, ImagePath not empty, max length.
/// </summary>
public class CreateProductImageRequestValidator : AbstractValidator<CreateProductImageRequest>
{
    public CreateProductImageRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("معرف المنتج مطلوب");

        RuleFor(x => x.ImagePath)
            .NotEmpty().WithMessage("مسار الصورة مطلوب")
            .MaximumLength(500).WithMessage("مسار الصورة لا يمكن أن يتجاوز 500 حرف");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("ترتيب العرض لا يمكن أن يكون سالباً");
    }
}
