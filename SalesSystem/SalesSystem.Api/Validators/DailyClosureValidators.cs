using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateDailyClosureRequestValidator : AbstractValidator<CreateDailyClosureRequest>
{
    public CreateDailyClosureRequestValidator()
    {
        RuleFor(x => x.CashBoxId)
            .GreaterThan(0).WithMessage("يجب اختيار الصندوق");

        RuleFor(x => x.ClosureDate)
            .NotEmpty().WithMessage("تاريخ الإغلاق مطلوب");
    }
}

public class ReconcileDailyClosureRequestValidator : AbstractValidator<ReconcileDailyClosureRequest>
{
    public ReconcileDailyClosureRequestValidator()
    {
        RuleFor(x => x.ActualCashCount)
            .GreaterThanOrEqualTo(0).WithMessage("العدد الفعلي للنقدية لا يمكن أن يكون سالباً")
            .PrecisionScale(18, 2, false).WithMessage("المبلغ يجب أن يكون برقمين عشريين كحد أقصى");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}
