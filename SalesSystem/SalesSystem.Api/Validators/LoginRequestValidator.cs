using FluentValidation;
using SalesSystem.Contracts.Requests.Auth;

namespace SalesSystem.Api.Validators;

/// <summary>
/// Validator for the LoginRequest DTO.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("اسم المستخدم مطلوب")
            .MaximumLength(50).WithMessage("اسم المستخدم يجب ألا يتجاوز 50 حرفاً");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة")
            .MinimumLength(3).WithMessage("كلمة المرور يجب أن تكون 3 أحرف على الأقل");
    }
}
