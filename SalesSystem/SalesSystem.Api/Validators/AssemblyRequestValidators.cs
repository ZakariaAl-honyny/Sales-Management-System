using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateBillOfMaterialRequestValidator : AbstractValidator<CreateBillOfMaterialRequest>
{
    public CreateBillOfMaterialRequestValidator()
    {
        RuleFor(x => x.AssemblyProductId)
            .GreaterThan(0).WithMessage("معرف المنتج المُجمَّع مطلوب");

        RuleFor(x => x.ComponentProductId)
            .GreaterThan(0).WithMessage("معرف المكوّن مطلوب");

        RuleFor(x => x.ComponentUnitId)
            .GreaterThan(0).WithMessage("معرف وحدة المكوّن مطلوب");

        RuleFor(x => x.QuantityRequired)
            .GreaterThan(0).WithMessage("الكمية المطلوبة يجب أن تكون أكبر من الصفر")
            .PrecisionScale(18, 3, false).WithMessage("الكمية المطلوبة يجب ألا تتجاوز 18 رقم و 3 أرقام عشرية");

        RuleFor(x => x.WastePercentage)
            .GreaterThanOrEqualTo(0).WithMessage("نسبة الهالك لا يمكن أن تكون سالبة")
            .LessThanOrEqualTo(100).WithMessage("نسبة الهالك لا يمكن أن تتجاوز 100%")
            .When(x => x.WastePercentage != 0);
    }
}

public class UpdateBillOfMaterialRequestValidator : AbstractValidator<UpdateBillOfMaterialRequest>
{
    public UpdateBillOfMaterialRequestValidator()
    {
        RuleFor(x => x.ComponentUnitId)
            .GreaterThan(0).WithMessage("معرف وحدة المكوّن مطلوب");

        RuleFor(x => x.QuantityRequired)
            .GreaterThan(0).WithMessage("الكمية المطلوبة يجب أن تكون أكبر من الصفر")
            .PrecisionScale(18, 3, false).WithMessage("الكمية المطلوبة يجب ألا تتجاوز 18 رقم و 3 أرقام عشرية");

        RuleFor(x => x.WastePercentage)
            .GreaterThanOrEqualTo(0).WithMessage("نسبة الهالك لا يمكن أن تكون سالبة")
            .LessThanOrEqualTo(100).WithMessage("نسبة الهالك لا يمكن أن تتجاوز 100%")
            .When(x => x.WastePercentage != 0);
    }
}

public class ProduceAssemblyRequestValidator : AbstractValidator<ProduceAssemblyRequest>
{
    public ProduceAssemblyRequestValidator()
    {
        RuleFor(x => x.AssemblyProductId)
            .GreaterThan(0).WithMessage("معرف المنتج المُجمَّع مطلوب");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("معرف المستودع مطلوب");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("الكمية المنتجة يجب أن تكون أكبر من الصفر")
            .PrecisionScale(18, 3, false).WithMessage("الكمية المنتجة يجب ألا تتجاوز 18 رقم و 3 أرقام عشرية");
    }
}
