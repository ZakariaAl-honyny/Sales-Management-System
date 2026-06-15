using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateInventoryAdjustmentRequestValidator : AbstractValidator<CreateInventoryAdjustmentRequest>
{
    public CreateInventoryAdjustmentRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("معرف المستودع مطلوب");

        RuleFor(x => x.AdjustmentDate)
            .NotEmpty().WithMessage("تاريخ التسوية مطلوب");

        RuleFor(x => x.AdjustmentType)
            .InclusiveBetween((byte)1, (byte)3).WithMessage("نوع التسوية غير صالح");

        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("معرف الحساب المحاسبي مطلوب");
    }
}

public class AddInventoryAdjustmentLineRequestValidator : AbstractValidator<AddInventoryAdjustmentLineRequest>
{
    public AddInventoryAdjustmentLineRequestValidator()
    {
        RuleFor(x => x.InventoryAdjustmentId)
            .GreaterThan(0).WithMessage("معرف تسوية المخزون مطلوب");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("معرف المنتج مطلوب");

        RuleFor(x => x.ProductUnitId)
            .GreaterThan(0).WithMessage("معرف وحدة المنتج مطلوب");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");

        RuleFor(x => x.UnitCost)
            .GreaterThan(0).WithMessage("تكلفة الوحدة يجب أن تكون أكبر من صفر");
    }
}
