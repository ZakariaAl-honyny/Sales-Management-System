using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CashTransferRequestValidator : AbstractValidator<CashTransferRequest>
{
    public CashTransferRequestValidator()
    {
        RuleFor(x => x.SourceCashBoxId)
            .GreaterThan(0).WithMessage("يجب اختيار الخزنة المصدر");

        RuleFor(x => x.DestinationCashBoxId)
            .GreaterThan(0).WithMessage("يجب اختيار الخزنة الهدف");

        RuleFor(x => x)
            .Must(x => x.SourceCashBoxId != x.DestinationCashBoxId)
            .WithMessage("لا يمكن التحويل لنفس الخزنة");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("مبلغ التحويل يجب أن يكون أكبر من صفر");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}
