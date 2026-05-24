using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class RestoreBackupRequestValidator : AbstractValidator<RestoreBackupRequest>
{
    public RestoreBackupRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("اسم ملف النسخة الاحتياطية مطلوب")
            .Must(f => !string.IsNullOrEmpty(f) && f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                .WithMessage("اسم الملف يجب أن ينتهي بـ .bak");
    }
}
