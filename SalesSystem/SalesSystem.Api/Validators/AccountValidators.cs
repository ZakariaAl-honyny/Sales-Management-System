using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.AccountCode)
            .NotEmpty().WithMessage("رمز الحساب مطلوب")
            .Matches(@"^\d{4,10}$").WithMessage("رمز الحساب يجب أن يكون أرقاماً فقط (4-10 خانات)");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم الحساب بالعربية مطلوب")
            .MaximumLength(200).WithMessage("اسم الحساب يجب ألا يتجاوز 200 حرف");

        RuleFor(x => x.NameEn)
            .MaximumLength(200).WithMessage("الاسم بالإنجليزية يجب ألا يتجاوز 200 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.NameEn));

        RuleFor(x => x.Nature)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("نوع الحساب غير صالح — القيم المسموحة: 1-5");
    }
}

public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم الحساب بالعربية مطلوب")
            .MaximumLength(200).WithMessage("اسم الحساب (عربي) يجب ألا يتجاوز 200 حرف");

        RuleFor(x => x.NameEn)
            .MaximumLength(200).WithMessage("اسم الحساب (إنجليزي) يجب ألا يتجاوز 200 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.NameEn));

        RuleFor(x => x.Nature)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("نوع الحساب غير صالح — القيم المسموحة: 1-5");
    }
}
