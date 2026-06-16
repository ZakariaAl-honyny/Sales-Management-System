using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateInventoryTransactionRequestValidator : AbstractValidator<CreateInventoryTransactionRequest>
{
    public CreateInventoryTransactionRequestValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan((short)0).WithMessage("المستودع مطلوب");
        RuleFor(x => x.MovementType).InclusiveBetween((byte)1, (byte)12).WithMessage("نوع الحركة غير صالح");
        RuleFor(x => x.Lines).NotNull().WithMessage("يجب إضافة صنف واحد على الأقل");
        RuleFor(x => x.Lines).Must(l => l != null && l.Count > 0).WithMessage("يجب إضافة صنف واحد على الأقل");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductUnitId).GreaterThan(0).WithMessage("الوحدة مطلوبة");
            line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            line.RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0).WithMessage("التكلفة لا يمكن أن تكون سالبة");
        });
    }
}
