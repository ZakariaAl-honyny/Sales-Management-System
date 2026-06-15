using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateChequeRequestValidator : AbstractValidator<CreateChequeRequest>
{
    public CreateChequeRequestValidator()
    {
        RuleFor(x => x.ChequeNumber)
            .NotEmpty().WithMessage("رقم الشيك مطلوب");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("قيمة الشيك يجب أن تكون أكبر من صفر");

        RuleFor(x => x.IssueDate)
            .NotEmpty().WithMessage("تاريخ الإصدار مطلوب")
            .When(x => x.IssueDate == default);
    }
}

public class UpdateChequeRequestValidator : AbstractValidator<UpdateChequeRequest>
{
    public UpdateChequeRequestValidator()
    {
        RuleFor(x => x.ChequeNumber)
            .NotEmpty().WithMessage("رقم الشيك مطلوب");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("قيمة الشيك يجب أن تكون أكبر من صفر");

        RuleFor(x => x.IssueDate)
            .NotEmpty().WithMessage("تاريخ الإصدار مطلوب")
            .When(x => x.IssueDate == default);
    }
}
