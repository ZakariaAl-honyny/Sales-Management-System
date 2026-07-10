using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateCustomerReceiptRequestValidator : AbstractValidator<CreateCustomerReceiptRequest>
{
    public CreateCustomerReceiptRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).WithMessage("معرف العميل مطلوب");

        RuleFor(x => x.CashBoxId)
            .GreaterThan(0).WithMessage("معرف الخزنة مطلوب");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.PaymentMethod)
            .InclusiveBetween((byte)1, (byte)4).WithMessage("طريقة الدفع يجب أن تكون بين 1 (نقدي) و 4 (بطاقة ائتمان)");
    }
}

public class AddReceiptApplicationRequestValidator : AbstractValidator<AddReceiptApplicationRequest>
{
    public AddReceiptApplicationRequestValidator()
    {
        RuleFor(x => x.CustomerReceiptId)
            .GreaterThan(0).WithMessage("معرف سند القبض مطلوب");

        RuleFor(x => x.SalesInvoiceId)
            .GreaterThan(0).WithMessage("معرف فاتورة البيع مطلوب");

        RuleFor(x => x.AppliedAmount)
            .GreaterThan(0).WithMessage("المبلغ المطبق يجب أن يكون أكبر من صفر");
    }
}
