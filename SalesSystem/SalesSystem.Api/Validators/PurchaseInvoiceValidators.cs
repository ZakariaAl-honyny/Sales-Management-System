using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreatePurchaseInvoiceRequestValidator : AbstractValidator<CreatePurchaseInvoiceRequest>
{
    public CreatePurchaseInvoiceRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("المورد مطلوب");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("المستودع مطلوب");
        RuleFor(x => x.Items).NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("الضريبة لا يمكن أن تكون سالبة");
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع لا يمكن أن يكون سالباً");

        When(x => x.DiscountType.HasValue, () =>
        {
            RuleFor(x => (int)x.DiscountType!.Value).InclusiveBetween(0, 1)
                .WithMessage("نوع الخصم غير صحيح (0 = مبلغ, 1 = نسبة مئوية)");
        });

        When(x => x.DiscountRate.HasValue, () =>
        {
            RuleFor(x => x.DiscountRate!.Value).InclusiveBetween(0, 100)
                .WithMessage("نسبة الخصم يجب أن تكون بين 0 و 100");
        });

        When(x => x.DiscountType == 1, () =>
        {
            RuleFor(x => x.DiscountRate).NotNull()
                .WithMessage("نسبة الخصم مطلوبة عند اختيار خصم النسبة المئوية");
        });

        When(x => x.CurrencyId.HasValue, () =>
        {
            RuleFor(x => x.CurrencyId!.Value).GreaterThan(0)
                .WithMessage("معرف العملة غير صحيح");
            RuleFor(x => x.ExchangeRate).NotNull().GreaterThan(0)
                .WithMessage("سعر الصرف مطلوب ويجب أن يكون أكبر من صفر");
        });

        When(x => !string.IsNullOrEmpty(x.SupplierInvoiceNo), () =>
        {
            RuleFor(x => x.SupplierInvoiceNo).MaximumLength(100)
                .WithMessage("رقم فاتورة المورد لا يمكن أن يتجاوز 100 حرف");
        });

        When(x => !string.IsNullOrEmpty(x.Notes), () =>
        {
            RuleFor(x => x.Notes).MaximumLength(500)
                .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
        });

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("المنتج مطلوب");
            item.RuleFor(i => i.ProductUnitId).GreaterThan(0).WithMessage("الوحدة مطلوبة");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0).WithMessage("التكلفة لا يمكن أن تكون سالبة");
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

            item.When(i => i.DiscountType.HasValue, () =>
            {
                item.RuleFor(i => (int)i.DiscountType!.Value).InclusiveBetween(0, 1)
                    .WithMessage("نوع الخصم غير صحيح");
            });

            item.When(i => i.DiscountRate.HasValue, () =>
            {
                item.RuleFor(i => i.DiscountRate!.Value).InclusiveBetween(0, 100)
                    .WithMessage("نسبة الخصم يجب أن تكون بين 0 و 100");
            });
        });
    }
}

public class UpdatePurchaseInvoiceRequestValidator : AbstractValidator<UpdatePurchaseInvoiceRequest>
{
    public UpdatePurchaseInvoiceRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("المورد مطلوب");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("المستودع مطلوب");
        RuleFor(x => x.Items).NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("الضريبة لا يمكن أن تكون سالبة");
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع لا يمكن أن يكون سالباً");

        When(x => x.DiscountType.HasValue, () =>
        {
            RuleFor(x => (int)x.DiscountType!.Value).InclusiveBetween(0, 1)
                .WithMessage("نوع الخصم غير صحيح (0 = مبلغ, 1 = نسبة مئوية)");
        });

        When(x => x.DiscountRate.HasValue, () =>
        {
            RuleFor(x => x.DiscountRate!.Value).InclusiveBetween(0, 100)
                .WithMessage("نسبة الخصم يجب أن تكون بين 0 و 100");
        });

        When(x => x.CurrencyId.HasValue, () =>
        {
            RuleFor(x => x.CurrencyId!.Value).GreaterThan(0)
                .WithMessage("معرف العملة غير صحيح");
            RuleFor(x => x.ExchangeRate).NotNull().GreaterThan(0)
                .WithMessage("سعر الصرف مطلوب ويجب أن يكون أكبر من صفر");
        });

        When(x => !string.IsNullOrEmpty(x.SupplierInvoiceNo), () =>
        {
            RuleFor(x => x.SupplierInvoiceNo).MaximumLength(100)
                .WithMessage("رقم فاتورة المورد لا يمكن أن يتجاوز 100 حرف");
        });

        When(x => !string.IsNullOrEmpty(x.Notes), () =>
        {
            RuleFor(x => x.Notes).MaximumLength(500)
                .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
        });

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("المنتج مطلوب");
            item.RuleFor(i => i.ProductUnitId).GreaterThan(0).WithMessage("الوحدة مطلوبة");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0).WithMessage("التكلفة لا يمكن أن تكون سالبة");
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

            item.When(i => i.DiscountType.HasValue, () =>
            {
                item.RuleFor(i => (int)i.DiscountType!.Value).InclusiveBetween(0, 1)
                    .WithMessage("نوع الخصم غير صحيح");
            });

            item.When(i => i.DiscountRate.HasValue, () =>
            {
                item.RuleFor(i => i.DiscountRate!.Value).InclusiveBetween(0, 100)
                    .WithMessage("نسبة الخصم يجب أن تكون بين 0 و 100");
            });
        });
    }
}
