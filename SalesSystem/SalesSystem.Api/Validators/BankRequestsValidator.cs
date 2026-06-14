using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateBankRequestValidator : AbstractValidator<CreateBankRequest>
{
    public CreateBankRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("معرف الحساب المحاسبي مطلوب");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم البنك مطلوب")
            .MaximumLength(100).WithMessage("اسم البنك لا يمكن أن يتجاوز 100 حرف");
    }
}

public class UpdateBankRequestValidator : AbstractValidator<UpdateBankRequest>
{
    public UpdateBankRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم البنك مطلوب")
            .MaximumLength(100).WithMessage("اسم البنك لا يمكن أن يتجاوز 100 حرف");
    }
}
