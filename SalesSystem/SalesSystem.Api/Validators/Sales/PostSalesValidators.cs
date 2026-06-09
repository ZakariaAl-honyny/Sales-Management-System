using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Sales;

/// <summary>
/// مدقق صحة طلب ترحيل فاتورة بيع.
/// </summary>
public class PostSalesInvoiceRequestValidator : AbstractValidator<PostSalesInvoiceRequest>
{
    public PostSalesInvoiceRequestValidator()
    {
        // PostSalesInvoiceRequest is minimal — all validation is in the service layer
        // This validator ensures basic structural validity
        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا تتجاوز 500 حرف");
    }
}

/// <summary>
/// مدقق صحة طلب ترحيل مرتجع مبيعات.
/// </summary>
public class PostSalesReturnRequestValidator : AbstractValidator<PostSalesReturnRequest>
{
    public PostSalesReturnRequestValidator()
    {
        RuleFor(x => x.RefundAmount)
            .GreaterThanOrEqualTo(0).When(x => x.RefundAmount.HasValue)
            .WithMessage("المبلغ المسترد لا يمكن أن يكون سالباً");

        RuleFor(x => x.CashBoxId)
            .GreaterThan(0).When(x => x.CashBoxId.HasValue)
            .WithMessage("الصندوق النقدي غير صحيح");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا تتجاوز 500 حرف");
    }
}
