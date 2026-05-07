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
            .NotEmpty().WithMessage("Username is required")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(3).WithMessage("Password must be at least 3 characters");
    }
}
