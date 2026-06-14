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

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("معرف العملة مطلوب");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");
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
