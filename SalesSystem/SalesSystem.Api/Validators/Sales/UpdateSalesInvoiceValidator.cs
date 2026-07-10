using FluentValidation;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Sales;

public class UpdateSalesInvoiceValidator : AbstractValidator<UpdateSalesInvoiceRequest>
{
    public UpdateSalesInvoiceValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("يجب اختيار المستودع");

        RuleFor(x => x.PaidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع لا يمكن أن يكون سالباً");

        RuleFor(x => x.CashBoxId)
            .NotNull().When(x => x.PaidAmount > 0)
            .WithMessage("يجب اختيار الصندوق النقدي عند وجود مبلغ مدفوع");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

        RuleFor(x => x.DiscountType)
            .IsInEnum().WithMessage("نوع الخصم غير صحيح");
        RuleFor(x => x.DiscountRate)
            .InclusiveBetween(0, 100).When(x => x.DiscountType == DiscountType.Percentage)
            .WithMessage("نسبة الخصم يجب أن تكون بين 0 و 100");
        RuleFor(x => x.DiscountRate)
            .Null().When(x => x.DiscountType == DiscountType.Amount)
            .WithMessage("نسبة الخصم يجب أن تكون فارغة عند الخصم بمبلغ ثابت");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الضريبة لا يمكن أن تكون سالبة");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("يجب اختيار المنتج");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("السعر لا يمكن أن يكون سالباً");
            item.RuleFor(i => i.DiscountType).IsInEnum().WithMessage("نوع خصم الصنف غير صحيح");
            item.RuleFor(i => i.DiscountRate)
                .InclusiveBetween(0, 100).When(i => i.DiscountType == DiscountType.Percentage)
                .WithMessage("نسبة خصم الصنف يجب أن تكون بين 0 و 100");
        });
    }
}
