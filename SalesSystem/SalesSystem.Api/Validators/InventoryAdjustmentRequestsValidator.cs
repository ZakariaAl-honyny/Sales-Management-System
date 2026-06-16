using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateInventoryAdjustmentRequestValidator : AbstractValidator<CreateInventoryAdjustmentRequest>
{
    public CreateInventoryAdjustmentRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan((short)0).WithMessage("معرف المستودع مطلوب");

        RuleFor(x => x.AdjustmentType)
            .InclusiveBetween((byte)1, (byte)3).WithMessage("نوع التسوية غير صالح (1=إضافة, 2=خصم, 3=تصحيح)");
    }
}

public class AddInventoryAdjustmentLineRequestValidator : AbstractValidator<AddInventoryAdjustmentLineRequest>
{
    public AddInventoryAdjustmentLineRequestValidator()
    {
        RuleFor(x => x.InventoryAdjustmentId)
            .GreaterThan(0).WithMessage("معرف تسوية المخزون مطلوب");

        RuleFor(x => x.ProductUnitId)
            .GreaterThan(0).WithMessage("معرف وحدة المنتج مطلوب");

        RuleFor(x => x.ExpectedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("الكمية المتوقعة لا يمكن أن تكون سالبة");

        RuleFor(x => x.ActualQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("الكمية الفعلية لا يمكن أن تكون سالبة");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("تكلفة الوحدة لا يمكن أن تكون سالبة");
    }
}
