using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Sales;

/// <summary>
/// مدقق صحة طلب إنشاء عرض سعر
/// </summary>
public class CreateSalesQuotationValidator : AbstractValidator<CreateSalesQuotationRequest>
{
    public CreateSalesQuotationValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("يجب اختيار العميل");
        RuleFor(x => x.WarehouseId).GreaterThan((short)0).WithMessage("يجب اختيار المستودع");

        RuleFor(x => x.QuotationNo)
            .GreaterThan(0).When(x => x.QuotationNo.HasValue)
            .WithMessage("رقم عرض السعر يجب أن يكون أكبر من صفر");

        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("الضريبة لا يمكن أن تكون سالبة");

        RuleFor(x => x.PaymentType)
            .IsInEnum().WithMessage("نوع الدفع غير صحيح");

        RuleFor(x => x.QuotationDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.QuotationDate.HasValue)
            .WithMessage("تاريخ عرض السعر لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.ValidUntil)
            .GreaterThan(x => x.QuotationDate ?? DateTime.UtcNow)
            .When(x => x.ValidUntil.HasValue && x.QuotationDate.HasValue)
            .WithMessage("تاريخ انتهاء الصلاحية يجب أن يكون بعد تاريخ العرض");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");

        RuleFor(x => x.TermsAndConditions)
            .MaximumLength(2000).When(x => x.TermsAndConditions != null)
            .WithMessage("الشروط والأحكام لا يمكن أن تتجاوز 2000 حرف");

        RuleFor(x => x.Items).NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("يجب اختيار المنتج");
            item.RuleFor(i => i.ProductUnitId).GreaterThan(0).WithMessage("يجب اختيار الوحدة");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("السعر لا يمكن أن يكون سالباً");
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
            item.RuleFor(i => i.Notes)
                .MaximumLength(200).When(i => i.Notes != null)
                .WithMessage("ملاحظات الصنف لا يمكن أن تتجاوز 200 حرف");
        });
    }
}

/// <summary>
/// مدقق صحة طلب تحديث عرض سعر
/// </summary>
public class UpdateSalesQuotationValidator : AbstractValidator<UpdateSalesQuotationRequest>
{
    public UpdateSalesQuotationValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0).WithMessage("يجب اختيار العميل");
        RuleFor(x => x.WarehouseId).GreaterThan((short)0).WithMessage("يجب اختيار المستودع");

        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("الضريبة لا يمكن أن تكون سالبة");

        RuleFor(x => x.PaymentType)
            .IsInEnum().WithMessage("نوع الدفع غير صحيح");

        RuleFor(x => x.QuotationDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.QuotationDate.HasValue)
            .WithMessage("تاريخ عرض السعر لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");

        RuleFor(x => x.TermsAndConditions)
            .MaximumLength(2000).When(x => x.TermsAndConditions != null)
            .WithMessage("الشروط والأحكام لا يمكن أن تتجاوز 2000 حرف");

        RuleFor(x => x.Items).NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("يجب اختيار المنتج");
            item.RuleFor(i => i.ProductUnitId).GreaterThan(0).WithMessage("يجب اختيار الوحدة");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("السعر لا يمكن أن يكون سالباً");
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
        });
    }
}
