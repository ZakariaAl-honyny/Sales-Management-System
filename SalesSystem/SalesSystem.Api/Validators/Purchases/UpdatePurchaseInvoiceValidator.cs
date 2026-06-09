using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Purchases;

/// <summary>
/// مدقق صحة طلب تحديث فاتورة شراء — مع دعم العملات والخصم المتنوع والمرفقات والمصاريف الإضافية.
/// </summary>
public class UpdatePurchaseInvoiceValidator : AbstractValidator<UpdatePurchaseInvoiceRequest>
{
    public UpdatePurchaseInvoiceValidator()
    {
        RuleFor(x => x.SupplierId)
            .GreaterThan(0).WithMessage("يجب اختيار المورد");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("يجب اختيار المستودع");

        RuleFor(x => x.PaidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع لا يمكن أن يكون سالباً");

        RuleFor(x => x.CashBoxId)
            .NotNull().When(x => x.PaidAmount > 0)
            .WithMessage("يجب اختيار الصندوق النقدي عند وجود مبلغ مدفوع");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الضريبة لا يمكن أن تكون سالبة");

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

        // Discount type validation
        RuleFor(x => x.DiscountRate)
            .InclusiveBetween(0, 100).When(x => x.DiscountType.HasValue && x.DiscountType == (byte)1)
            .WithMessage("نسبة الخصم يجب أن تكون بين 0 و 100");

        RuleFor(x => x.DiscountRate)
            .NotNull().When(x => x.DiscountType.HasValue && x.DiscountType == (byte)1)
            .WithMessage("نسبة الخصم مطلوبة عند اختيار خصم نسبة مئوية");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");

        RuleFor(x => x.SupplierInvoiceNo)
            .MaximumLength(100).When(x => x.SupplierInvoiceNo != null)
            .WithMessage("رقم فاتورة المورد لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("يجب اختيار المنتج");

            item.RuleFor(i => i.ProductUnitId)
                .GreaterThan(0).WithMessage("يجب اختيار الوحدة");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");

            item.RuleFor(i => i.UnitCost)
                .GreaterThanOrEqualTo(0).WithMessage("التكلفة لا يمكن أن تكون سالبة");

            item.RuleFor(i => i.DiscountAmount)
                .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

            item.RuleFor(i => i.DiscountRate)
                .InclusiveBetween(0, 100).When(i => i.DiscountType.HasValue && i.DiscountType == (byte)1)
                .WithMessage("نسبة الخصم يجب أن تكون بين 0 و 100");
        });

        // Validate AdditionalFees if provided
        When(x => x.AdditionalFees != null && x.AdditionalFees.Any(), () =>
        {
            RuleForEach(x => x.AdditionalFees).ChildRules(fee =>
            {
                fee.RuleFor(f => f.FeeName)
                    .NotEmpty().WithMessage("اسم الرسم الإضافي مطلوب")
                    .MaximumLength(100).WithMessage("اسم الرسم الإضافي لا يتجاوز 100 حرف");

                fee.RuleFor(f => f.FeeAmount)
                    .GreaterThan(0).WithMessage("قيمة الرسم الإضافي يجب أن تكون أكبر من صفر");

                fee.RuleFor(f => f.DistributionMethod)
                    .InclusiveBetween((byte)0, (byte)1)
                    .WithMessage("طريقة التوزيع غير صحيحة");
            });
        });
    }
}
