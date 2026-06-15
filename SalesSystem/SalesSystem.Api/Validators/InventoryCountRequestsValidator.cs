using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateInventoryCountRequestValidator : AbstractValidator<CreateInventoryCountRequest>
{
    public CreateInventoryCountRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("معرف المستودع مطلوب");

        RuleFor(x => x.CountDate)
            .NotEmpty().WithMessage("تاريخ الجرد مطلوب");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}

public class UpdateInventoryCountRequestValidator : AbstractValidator<UpdateInventoryCountRequest>
{
    public UpdateInventoryCountRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}

public class AddInventoryCountLineRequestValidator : AbstractValidator<AddInventoryCountLineRequest>
{
    public AddInventoryCountLineRequestValidator()
    {
        RuleFor(x => x.InventoryCountId)
            .GreaterThan(0).WithMessage("معرف جرد المخزون مطلوب");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("معرف المنتج مطلوب");

        RuleFor(x => x.ProductUnitId)
            .GreaterThan(0).WithMessage("معرف وحدة المنتج مطلوب");

        RuleFor(x => x.SystemQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("الكمية النظامية لا يمكن أن تكون سالبة");

        RuleFor(x => x.ActualQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("الكمية الفعلية لا يمكن أن تكون سالبة");
    }
}
