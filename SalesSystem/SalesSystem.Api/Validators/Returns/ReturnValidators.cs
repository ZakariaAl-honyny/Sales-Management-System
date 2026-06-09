using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Returns;

public class CreateSalesReturnValidator : AbstractValidator<CreateSalesReturnRequest>
{
    public CreateSalesReturnValidator()
    {
        RuleFor(x => x.SalesInvoiceId)
            .GreaterThan(0).When(x => x.SalesInvoiceId.HasValue)
            .WithMessage("رقم فاتورة البيع غير صحيح");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("يجب اختيار المستودع");

        RuleFor(x => x.ReturnDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.ReturnDate.HasValue)
            .WithMessage("تاريخ المرتجع لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");

        RuleFor(x => x.Items).NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("يجب اختيار المنتج");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("السعر لا يمكن أن يكون سالباً");
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
            item.RuleFor(i => i.Notes)
                .MaximumLength(200).When(i => i.Notes != null)
                .WithMessage("ملاحظات الصنف لا يمكن أن تتجاوز 200 حرف");
        });
    }
}

public class CreatePurchaseReturnValidator : AbstractValidator<CreatePurchaseReturnRequest>
{
    public CreatePurchaseReturnValidator()
    {
        RuleFor(x => x.PurchaseInvoiceId)
            .GreaterThan(0).When(x => x.PurchaseInvoiceId.HasValue)
            .WithMessage("رقم فاتورة الشراء غير صحيح");

        RuleFor(x => x.SupplierId)
            .GreaterThan(0).WithMessage("يجب اختيار المورد");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("يجب اختيار المستودع");

        RuleFor(x => x.ReturnDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.ReturnDate.HasValue)
            .WithMessage("تاريخ المرتجع لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

        RuleFor(x => x.DiscountRate)
            .InclusiveBetween(0, 100).When(x => x.DiscountType.HasValue && x.DiscountType == (byte)1)
            .WithMessage("نسبة الخصم يجب أن تكون بين 0 و 100");

        // Currency rules
        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).When(x => x.CurrencyId.HasValue)
            .WithMessage("العملة غير صحيحة");

        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0).When(x => x.ExchangeRate.HasValue)
            .WithMessage("سعر الصرف يجب أن يكون أكبر من صفر");

        RuleFor(x => x.ExchangeRate)
            .NotNull().When(x => x.CurrencyId.HasValue)
            .WithMessage("يجب تحديد سعر الصرف عند اختيار عملة أجنبية");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");

        RuleFor(x => x.Items).NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("يجب اختيار المنتج");
            item.RuleFor(i => i.ProductUnitId).GreaterThan(0).WithMessage("يجب اختيار الوحدة");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0).WithMessage("التكلفة لا يمكن أن تكون سالبة");
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
            item.RuleFor(i => i.Notes)
                .MaximumLength(200).When(i => i.Notes != null)
                .WithMessage("ملاحظات الصنف لا يمكن أن تتجاوز 200 حرف");
        });
    }
}

