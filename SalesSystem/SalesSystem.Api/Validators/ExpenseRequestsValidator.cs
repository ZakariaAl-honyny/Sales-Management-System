using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.ExpenseDate)
            .NotEmpty().WithMessage("التاريخ مطلوب");

        RuleFor(x => x.ExpenseAccountId)
            .GreaterThan(0).WithMessage("حساب المصروف مطلوب");

        RuleFor(x => x.CashBoxId)
            .GreaterThan(0).WithMessage("الصندوق مطلوب");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("العملة مطلوبة");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("الملاحظات يجب أن لا تتجاوز 500 حرف");
    }
}

public class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
{
    public UpdateExpenseRequestValidator()
    {
        RuleFor(x => x.ExpenseDate)
            .NotEmpty().WithMessage("التاريخ مطلوب");

        RuleFor(x => x.ExpenseAccountId)
            .GreaterThan(0).WithMessage("حساب المصروف مطلوب");

        RuleFor(x => x.CashBoxId)
            .GreaterThan(0).WithMessage("الصندوق مطلوب");

        RuleFor(x => x.CurrencyId)
            .GreaterThan(0).WithMessage("العملة مطلوبة");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("المبلغ يجب أن يكون أكبر من صفر");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("الملاحظات يجب أن لا تتجاوز 500 حرف");
    }
}
