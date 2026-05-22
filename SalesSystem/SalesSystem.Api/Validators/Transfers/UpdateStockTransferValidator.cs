using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Transfers;

public class UpdateStockTransferValidator : AbstractValidator<UpdateStockTransferRequest>
{
    public UpdateStockTransferValidator()
    {
        RuleFor(x => x.FromWarehouseId)
            .GreaterThan(0).WithMessage("يجب اختيار المستودع المصدر");

        RuleFor(x => x.ToWarehouseId)
            .GreaterThan(0).WithMessage("يجب اختيار المستودع الوجهة")
            .NotEqual(x => x.FromWarehouseId).WithMessage("المستودع الوجهة لا يمكن أن يكون نفس المستودع المصدر");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("يجب اختيار المنتج");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.Mode).InclusiveBetween((byte)1, (byte)2).WithMessage("نوع البيع غير صحيح");
        });
    }
}
