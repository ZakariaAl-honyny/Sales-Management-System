using FluentValidation;
using SalesSystem.Contracts.Requests;
using SalesSystem.Domain.Accounting.Enums;

namespace SalesSystem.Api.Validators.Accounting;

/// <summary>
/// Validator for <see cref="CreateJournalEntryRequest"/>.
/// Ensures the journal entry has valid lines, valid entry type, and non-empty description.
/// </summary>
public class CreateJournalEntryRequestValidator : AbstractValidator<CreateJournalEntryRequest>
{
    public CreateJournalEntryRequestValidator()
    {
        RuleFor(x => x.TransactionDate)
            .NotEmpty().WithMessage("تاريخ القيد المحاسبي مطلوب")
            .LessThanOrEqualTo(DateTime.Today.AddDays(1))
            .WithMessage("تاريخ القيد المحاسبي لا يمكن أن يكون في المستقبل البعيد");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("الوصف مطلوب")
            .MaximumLength(500).WithMessage("الوصف لا يمكن أن يتجاوز 500 حرف");

        RuleFor(x => x.EntryType)
            .Must(t => Enum.IsDefined(typeof(JournalEntryType), t))
            .WithMessage("نوع القيد المحاسبي غير صالح");

        RuleFor(x => x.CreatedBy)
            .GreaterThan(0).WithMessage("منشئ القيد المحاسبي مطلوب");

        RuleFor(x => x.ReferenceType)
            .MaximumLength(50).WithMessage("نوع المرجع لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(100).WithMessage("رقم المرجع لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Lines)
            .NotNull().WithMessage("بنود القيد المحاسبي مطلوبة")
            .Must(lines => lines != null && lines.Count >= 2)
            .WithMessage("يجب إضافة بندين على الأقل لقيد محاسبي مزدوج");

        // Validate each line
        RuleForEach(x => x.Lines)
            .SetValidator(new JournalEntryLineRequestValidator());
    }
}

/// <summary>
/// Validator for individual journal entry lines.
/// </summary>
public class JournalEntryLineRequestValidator : AbstractValidator<JournalEntryLineRequest>
{
    public JournalEntryLineRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("رقم الحساب مطلوب");

        RuleFor(x => x.Debit)
            .GreaterThanOrEqualTo(0).WithMessage("قيمة الخصم لا يمكن أن تكون سالبة");

        RuleFor(x => x.Credit)
            .GreaterThanOrEqualTo(0).WithMessage("قيمة الإيداع لا يمكن أن تكون سالبة");

        RuleFor(x => x)
            .Must(x => x.Debit > 0 || x.Credit > 0)
            .WithMessage("يجب أن يكون للبند قيمة خصم أو إيداع")
            .WithName("LineAmount");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("وصف البند لا يمكن أن يتجاوز 500 حرف");
    }
}
