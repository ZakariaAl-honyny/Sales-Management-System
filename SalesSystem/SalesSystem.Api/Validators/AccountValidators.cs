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

        RuleFor(x => x.AccountCode)
            .Must((request, code) => !(request.Level == 1 && code.Length > 4))
            .WithMessage("رمز الحساب للمستوى الرئيسي يجب ألا يتجاوز 4 أرقام");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم الحساب بالعربية مطلوب")
            .MaximumLength(200).WithMessage("اسم الحساب يجب ألا يتجاوز 200 حرف");

        RuleFor(x => x.NameEn)
            .MaximumLength(200).WithMessage("الاسم بالإنجليزية يجب ألا يتجاوز 200 حرف");

        RuleFor(x => x.AccountType)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("نوع الحساب غير صالح");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 10).WithMessage("مستوى الحساب يجب أن يكون بين 1 و 10");

        RuleFor(x => x.ColorCode)
            .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("رمز اللون يجب أن يكون بصيغة Hex (#RRGGBB)")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorCode));

        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0).WithMessage("الرصيد الافتتاحي لا يمكن أن يكون سالباً")
            .When(x => x.OpeningBalance.HasValue);

        RuleFor(x => x.Explanation)
            .MaximumLength(500).WithMessage("الشرح يجب ألا يتجاوز 500 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.Explanation));
    }
}

public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم الحساب بالعربية مطلوب")
            .MaximumLength(200).WithMessage("اسم الحساب (عربي) يجب ألا يتجاوز 200 حرف");

        When(x => !string.IsNullOrEmpty(x.NameEn), () =>
        {
            RuleFor(x => x.NameEn).MaximumLength(200)
                .WithMessage("اسم الحساب (إنجليزي) يجب ألا يتجاوز 200 حرف");
        });

        When(x => !string.IsNullOrEmpty(x.ColorCode), () =>
        {
            RuleFor(x => x.ColorCode).Matches("^#[0-9A-Fa-f]{6}$")
                .WithMessage("كود اللون يجب أن يكون بصيغة Hex مثل #FF5722");
        });

        RuleFor(x => x.AccountType)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("نوع الحساب غير صالح");

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 10).WithMessage("مستوى الحساب يجب أن يكون بين 1 و 10");

        RuleFor(x => x.Explanation)
            .MaximumLength(500).WithMessage("الشرح يجب ألا يتجاوز 500 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.Explanation));
    }
}
