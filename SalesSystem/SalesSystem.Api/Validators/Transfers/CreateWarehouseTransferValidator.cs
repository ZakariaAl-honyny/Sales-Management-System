using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Transfers;

public class CreateWarehouseTransferValidator : AbstractValidator<CreateWarehouseTransferRequest>
{
    public CreateWarehouseTransferValidator()
    {
        RuleFor(x => x.SourceWarehouseId)
            .Must(id => id > 0).WithMessage("يجب اختيار المستودع المصدر");

        RuleFor(x => x.DestinationWarehouseId)
            .Must(id => id > 0).WithMessage("يجب اختيار المستودع الوجهة")
            .NotEqual(x => x.SourceWarehouseId).WithMessage("المستودع الوجهة لا يمكن أن يكون نفس المستودع المصدر");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Lines).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductUnitId).GreaterThan(0).WithMessage("يجب اختيار المنتج");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
        });
    }
}
