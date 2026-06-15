using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateBankRequestValidator : AbstractValidator<CreateBankRequest>
{
    public CreateBankRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0)
            .When(x => x.AccountId.HasValue)
            .WithMessage("معرف الحساب المحاسبي غير صالح");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم البنك مطلوب")
            .MaximumLength(150).WithMessage("اسم البنك لا يمكن أن يتجاوز 150 حرف");

        RuleFor(x => x.AccountNumber)
            .MaximumLength(100).When(x => x.AccountNumber != null)
            .WithMessage("رقم الحساب لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Iban)
            .MaximumLength(100).When(x => x.Iban != null)
            .WithMessage("الآيبان لا يمكن أن يتجاوز 100 حرف");
    }
}

public class UpdateBankRequestValidator : AbstractValidator<UpdateBankRequest>
{
    public UpdateBankRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم البنك مطلوب")
            .MaximumLength(150).WithMessage("اسم البنك لا يمكن أن يتجاوز 150 حرف");

        RuleFor(x => x.AccountNumber)
            .MaximumLength(100).When(x => x.AccountNumber != null)
            .WithMessage("رقم الحساب لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Iban)
            .MaximumLength(100).When(x => x.Iban != null)
            .WithMessage("الآيبان لا يمكن أن يتجاوز 100 حرف");
    }
}
