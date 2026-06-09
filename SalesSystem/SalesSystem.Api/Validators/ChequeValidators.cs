using FluentValidation;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Api.Validators;

public class CreateChequeRequestValidator : AbstractValidator<CreateChequeRequest>
{
    public CreateChequeRequestValidator()
    {
        RuleFor(x => x.ChequeNumber)
            .NotEmpty().WithMessage("رقم الشيك مطلوب")
            .MaximumLength(50).WithMessage("رقم الشيك لا يمكن أن يتجاوز 50 حرفاً");

        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("اسم البنك مطلوب")
            .MaximumLength(100).WithMessage("اسم البنك لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.IssueDate)
            .NotEmpty().WithMessage("تاريخ الإصدار مطلوب");

        RuleFor(x => x.MaturityDate)
            .NotEmpty().WithMessage("تاريخ الاستحقاق مطلوب")
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("تاريخ الاستحقاق يجب أن يكون بعد أو يساوي تاريخ الإصدار");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("قيمة الشيك يجب أن تكون أكبر من الصفر")
            .PrecisionScale(18, 2, false).WithMessage("قيمة الشيك يجب أن تكون برقمين عشريين كحد أقصى");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}

public class UpdateChequeStatusRequestValidator : AbstractValidator<UpdateChequeStatusRequest>
{
    public UpdateChequeStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("حالة الشيك غير صالحة");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}
