using FluentValidation;
using SalesSystem.Contracts.DTOs;

namespace SalesSystem.Api.Validators;

public class AuditLogQueryValidator : AbstractValidator<AuditLogQuery>
{
    public AuditLogQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("رقم الصفحة يجب أن يكون 1 على الأقل");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(10, 500).WithMessage("عدد العناصر بالصفحة يجب أن يكون بين 10 و 500");

        When(x => x.From.HasValue && x.To.HasValue, () =>
        {
            RuleFor(x => x.To)
                .Must((query, to) => to >= query.From)
                .WithMessage("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");
        });
    }
}
