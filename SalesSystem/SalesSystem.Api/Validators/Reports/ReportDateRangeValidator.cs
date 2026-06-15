using FluentValidation;

namespace SalesSystem.Api.Validators.Reports;

/// <summary>
/// Validates date range parameters for report endpoints.
/// Ensures start date is before end date and dates are not too far in the future.
/// </summary>
public class ReportDateRangeValidator : AbstractValidator<ReportDateRangeRequest>
{
    public ReportDateRangeValidator()
    {
        RuleFor(x => x.From)
            .NotEmpty().WithMessage("تاريخ البداية مطلوب")
            .LessThanOrEqualTo(x => x.To).WithMessage("تاريخ البداية يجب أن يكون قبل تاريخ النهاية")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1)).WithMessage("لا يمكن أن يكون تاريخ البداية في المستقبل البعيد");

        RuleFor(x => x.To)
            .NotEmpty().WithMessage("تاريخ النهاية مطلوب")
            .GreaterThanOrEqualTo(x => x.From).WithMessage("تاريخ النهاية يجب أن يكون بعد تاريخ البداية")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1)).WithMessage("لا يمكن أن يكون تاريخ النهاية في المستقبل البعيد");
    }
}

/// <summary>
/// Simple request model for date range validation.
/// </summary>
public record ReportDateRangeRequest(DateTime From, DateTime To);
