using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Accounting;

/// <summary>
/// Validator for <see cref="CreateFiscalYearRequest"/>.
/// Ensures the fiscal year is within a valid range (2000 to currentYear+10).
/// </summary>
public class CreateFiscalYearRequestValidator : AbstractValidator<CreateFiscalYearRequest>
{
    public CreateFiscalYearRequestValidator()
    {
        var currentYear = DateTime.UtcNow.Year;

        RuleFor(x => x.Year)
            .GreaterThanOrEqualTo(2000)
            .WithMessage("السنة المالية يجب أن تكون 2000 أو أكثر")
            .LessThanOrEqualTo(currentYear + 10)
            .WithMessage($"السنة المالية يجب أن تكون {currentYear + 10} أو أقل");
    }
}
