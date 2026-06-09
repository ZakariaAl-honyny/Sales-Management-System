using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Sales;

/// <summary>
/// مدقق صحة طلب إنشاء عرض سعر.
/// </summary>
public class CreateSalesQuotationRequestValidator : AbstractValidator<CreateSalesQuotationRequest>
{
    public CreateSalesQuotationRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("يجب اختيار المستودع");

        RuleFor(x => x.QuotationDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.QuotationDate.HasValue)
            .WithMessage("تاريخ عرض السعر لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(x => x.QuotationDate ?? DateTime.UtcNow).When(x => x.ExpiryDate.HasValue)
            .WithMessage("تاريخ الانتهاء يجب أن يكون بعد تاريخ عرض السعر");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

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
            .WithMessage("الملاحظات لا تتجاوز 500 حرف");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("يجب اختيار المنتج");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("السعر لا يمكن أن يكون سالباً");

            item.RuleFor(i => i.DiscountAmount)
                .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

            item.RuleFor(i => i.Notes)
                .MaximumLength(250).When(i => i.Notes != null)
                .WithMessage("ملاحظات الصنف لا تتجاوز 250 حرف");
        });
    }
}

/// <summary>
/// مدقق صحة طلب تحديث عرض سعر.
/// </summary>
public class UpdateSalesQuotationRequestValidator : AbstractValidator<UpdateSalesQuotationRequest>
{
    public UpdateSalesQuotationRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("يجب اختيار المستودع");

        RuleFor(x => x.QuotationDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.QuotationDate.HasValue)
            .WithMessage("تاريخ عرض السعر لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

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
            .WithMessage("الملاحظات لا تتجاوز 500 حرف");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("يجب اختيار المنتج");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("السعر لا يمكن أن يكون سالباً");

            item.RuleFor(i => i.DiscountAmount)
                .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
        });
    }
}

/// <summary>
/// مدقق صحة طلب تحويل عرض سعر إلى فاتورة بيع.
/// </summary>
public class ConvertQuotationToInvoiceRequestValidator : AbstractValidator<ConvertQuotationToInvoiceRequest>
{
    public ConvertQuotationToInvoiceRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("يجب اختيار المستودع");

        RuleFor(x => x.PaymentType)
            .InclusiveBetween(1, 3).WithMessage("نوع الدفع غير صحيح (1=نقدي, 2=آجل, 3=مختلط)");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الضريبة لا يمكن أن تكون سالبة");

        RuleFor(x => x.PaidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع لا يمكن أن يكون سالباً");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا تتجاوز 500 حرف");
    }
}
