using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateInventoryCountRequestValidator : AbstractValidator<CreateInventoryCountRequest>
{
    public CreateInventoryCountRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan((short)0).WithMessage("معرف المستودع مطلوب");

        RuleFor(x => x.Notes)
            .MaximumLength(300).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 300 حرف");
    }
}

public class UpdateInventoryCountRequestValidator : AbstractValidator<UpdateInventoryCountRequest>
{
    public UpdateInventoryCountRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(300).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 300 حرف");
    }
}

public class AddInventoryCountLineRequestValidator : AbstractValidator<AddInventoryCountLineRequest>
{
    public AddInventoryCountLineRequestValidator()
    {
        RuleFor(x => x.InventoryCountId)
            .GreaterThan(0).WithMessage("معرف جرد المخزون مطلوب");

        RuleFor(x => x.ProductUnitId)
            .GreaterThan(0).WithMessage("معرف وحدة المنتج مطلوب");

        RuleFor(x => x.ExpectedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("الكمية المتوقعة لا يمكن أن تكون سالبة");

        RuleFor(x => x.ActualQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("الكمية الفعلية لا يمكن أن تكون سالبة");
    }
}
