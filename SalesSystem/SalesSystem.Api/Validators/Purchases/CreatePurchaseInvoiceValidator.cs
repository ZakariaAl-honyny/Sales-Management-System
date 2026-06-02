using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Purchases;

public class CreatePurchaseInvoiceValidator : AbstractValidator<CreatePurchaseInvoiceRequest>
{
    public CreatePurchaseInvoiceValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("يجب اختيار المورد");
        RuleFor(x => x.InvoiceNo)
            .GreaterThan(0).When(x => x.InvoiceNo.HasValue)
            .WithMessage("رقم الفاتورة يجب أن يكون أكبر من صفر");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("يجب اختيار المستودع");
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0).WithMessage("المبلغ المدفوع لا يمكن أن يكون سالباً");
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("الضريبة لا يمكن أن تكون سالبة");

        RuleFor(x => x.PaymentType)
            .IsInEnum().WithMessage("نوع الدفع غير صحيح");

        RuleFor(x => x.InvoiceDate)
            .LessThanOrEqualTo(DateTime.UtcNow).When(x => x.InvoiceDate.HasValue)
            .WithMessage("تاريخ الفاتورة لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow)).When(x => x.DueDate.HasValue)
            .WithMessage("تاريخ الاستحقاق لا يمكن أن يكون في الماضي");

        RuleFor(x => x.SupplierInvoiceNo)
            .MaximumLength(100).When(x => x.SupplierInvoiceNo != null)
            .WithMessage("رقم فاتورة المورد لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");

        // Business Rule: If paying, CashBoxId is required
        RuleFor(x => x.CashBoxId)
            .NotNull().When(x => x.PaidAmount > 0)
            .WithMessage("يجب اختيار صندوق نقدي عند وجود مبلغ مدفوع");

        RuleFor(x => x.Items).NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("يجب اختيار المنتج");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0).WithMessage("التكلفة لا يمكن أن تكون سالبة");
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("الخصم لا يمكن أن يكون سالباً");
            item.RuleFor(i => i.Notes)
                .MaximumLength(200).When(i => i.Notes != null)
                .WithMessage("ملاحظات الصنف لا يمكن أن تتجاوز 200 حرف");
        });
    }
}

