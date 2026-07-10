using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Accounting;

/// <summary>
/// Validator for <see cref="CloseFiscalYearRequest"/>.
/// Ensures the fiscal year is a valid positive year value.
/// </summary>
public class CloseFiscalYearRequestValidator : AbstractValidator<CloseFiscalYearRequest>
{
    public CloseFiscalYearRequestValidator()
    {
        RuleFor(x => x.FiscalYear)
            .GreaterThanOrEqualTo(2000).WithMessage("السنة المالية يجب أن تكون 2000 أو أكثر")
            .LessThanOrEqualTo(9999).WithMessage("السنة المالية غير صالحة")
            .Must(year => year <= DateTime.Today.Year + 1)
            .WithMessage("السنة المالية لا يمكن أن تكون بعد السنة القادمة");
    }
}
