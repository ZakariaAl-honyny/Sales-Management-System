using FluentValidation;
using SalesSystem.Contracts.Enums;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Purchases;

/// <summary>
/// مدقق صحة طلب إنشاء فاتورة شراء.
/// </summary>
public class CreatePurchaseInvoiceValidator : AbstractValidator<CreatePurchaseInvoiceRequest>
{
    public CreatePurchaseInvoiceValidator()
    {
        RuleFor(x => x.SupplierId)
            .GreaterThan(0).WithMessage("يجب اختيار المورد");

        RuleFor(x => x.InvoiceNo)
            .GreaterThan(0).When(x => x.InvoiceNo.HasValue)
            .WithMessage("رقم الفاتورة يجب أن يكون أكبر من صفر");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("يجب اختيار المستودع");

        RuleFor(x => x.PaidAmount)
            .GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع لا يمكن أن يكون سالباً");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("الضريبة لا يمكن أن تكون سالبة");

        RuleFor(x => x.OtherCharges)
            .GreaterThanOrEqualTo(0).WithMessage("مصاريف إضافية لا يمكن أن تكون سالبة");

        RuleFor(x => x.DiscountType)
            .IsInEnum().When(x => x.DiscountType.HasValue)
            .WithMessage("نوع الخصم غير صحيح");

        RuleFor(x => x.DiscountRate)
            .GreaterThan(0).When(x => x.DiscountType.HasValue && x.DiscountType.Value == DiscountType.Percentage)
            .WithMessage("نسبة الخصم يجب أن تكون أكبر من صفر");

        RuleFor(x => x.DiscountRate)
            .LessThanOrEqualTo(100).When(x => x.DiscountType.HasValue && x.DiscountType.Value == DiscountType.Percentage)
            .WithMessage("نسبة الخصم يجب أن تكون أقل من أو تساوي 100");

        RuleFor(x => x.AttachmentPath)
            .MaximumLength(255).When(x => x.AttachmentPath != null)
            .WithMessage("رابط المرفق لا يمكن أن يتجاوز 255 حرف");

        RuleFor(x => x.PaymentType)
            .IsInEnum().WithMessage("نوع الدفع غير صحيح");

        RuleFor(x => x.InvoiceDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.InvoiceDate.HasValue)
            .WithMessage("تاريخ الفاتورة لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");

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

            item.RuleFor(i => i.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("السعر لا يمكن أن يكون سالباً");

            item.RuleFor(i => i.DiscountType)
                .IsInEnum().When(i => i.DiscountType.HasValue)
                .WithMessage("نوع الخصم في الصنف غير صحيح");

            item.RuleFor(i => i.DiscountRate)
                .GreaterThan(0).When(i => i.DiscountType.HasValue && i.DiscountType.Value == DiscountType.Percentage)
                .WithMessage("نسبة الخصم في الصنف يجب أن تكون أكبر من صفر");

            item.RuleFor(i => i.DiscountRate)
                .LessThanOrEqualTo(100).When(i => i.DiscountType.HasValue && i.DiscountType.Value == DiscountType.Percentage)
                .WithMessage("نسبة الخصم في الصنف يجب أن تكون أقل من أو تساوي 100");
        });
    }
}
