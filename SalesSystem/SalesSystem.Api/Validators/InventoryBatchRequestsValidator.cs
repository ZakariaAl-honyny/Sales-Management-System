using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateInventoryBatchRequestValidator : AbstractValidator<CreateInventoryBatchRequest>
{
    public CreateInventoryBatchRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("معرف المنتج مطلوب");

        RuleFor(x => x.WarehouseId)
            .Must(id => id > 0).WithMessage("معرف المستودع مطلوب");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من الصفر");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("تكلفة الوحدة لا يمكن أن تكون سالبة");

        RuleFor(x => x.BatchNo)
            .NotEmpty().WithMessage("رقم الدفعة مطلوب")
            .MaximumLength(50).WithMessage("رقم الدفعة لا يمكن أن يتجاوز 50 حرفاً");

        RuleFor(x => x.PurchaseInvoiceId)
            .GreaterThan(0).WithMessage("معرف فاتورة المشتريات يجب أن يكون أكبر من صفر")
            .When(x => x.PurchaseInvoiceId.HasValue);

        RuleFor(x => x.ExpiryDate)
            .Must(d => d!.Value > DateTime.UtcNow)
            .When(x => x.ExpiryDate.HasValue)
            .WithMessage("تاريخ انتهاء الصلاحية يجب أن يكون في المستقبل");
    }
}
