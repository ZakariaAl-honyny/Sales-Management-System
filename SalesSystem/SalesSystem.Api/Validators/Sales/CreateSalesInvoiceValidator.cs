using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Sales;

public class CreateSalesInvoiceValidator : AbstractValidator<CreateSalesInvoiceRequest>
{
    public CreateSalesInvoiceValidator()
    {
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
            item.RuleFor(i => i.Mode).IsInEnum().WithMessage("نوع البيع غير صحيح");
            item.RuleFor(i => i.Notes)
                .MaximumLength(200).When(i => i.Notes != null)
                .WithMessage("ملاحظات الصنف لا يمكن أن تتجاوز 200 حرف");
        });

        // Business Rule: If paying, CashBoxId is required
        RuleFor(x => x.CashBoxId)
            .NotNull().When(x => x.PaidAmount > 0)
            .WithMessage("يجب اختيار صندوق نقدي عند وجود مبلغ مدفوع");

        // Business Rule: If payment type is Credit, CustomerId is usually required in logic, 
        // but some shops allow "General Customer" credit. 
        // Constitution says: if invoice.DueAmount > 0 load customer. So we should enforce it here if possible.
    }
}

