using FluentValidation;
using SalesSystem.Api.Controllers;

namespace SalesSystem.Api.Validators;

public class SavePdfRequestValidator : AbstractValidator<SavePdfRequest>
{
    public SavePdfRequestValidator()
    {
        RuleFor(x => x.FilePath)
            .NotEmpty().WithMessage("مسار حفظ ملف PDF مطلوب")
            .Must(p => p.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                .WithMessage("مسار الملف يجب أن ينتهي بـ .pdf");
    }
}
