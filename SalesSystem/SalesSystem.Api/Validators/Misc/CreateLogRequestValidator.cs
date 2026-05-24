using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Misc;

public class CreateLogRequestValidator : AbstractValidator<CreateLogRequest>
{
    private static readonly string[] ValidLogLevels = ["Information", "Warning", "Error", "Debug", "Trace"];

    public CreateLogRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("الرسالة مطلوبة")
            .MaximumLength(4000).WithMessage("الرسالة لا يمكن أن تتجاوز 4000 حرف");

        RuleFor(x => x.LogLevel)
            .NotEmpty().WithMessage("مستوى السجل مطلوب")
            .MaximumLength(50).WithMessage("مستوى السجل لا يمكن أن يتجاوز 50 حرف")
            .Must(BeValidLogLevel).WithMessage("مستوى السجل غير صحيح (مسموح: Information, Warning, Error, Debug, Trace)");

        RuleFor(x => x.Exception)
            .MaximumLength(4000).WithMessage("الاستثناء لا يمكن أن يتجاوز 4000 حرف")
            .When(x => !string.IsNullOrEmpty(x.Exception));

        RuleFor(x => x.StackTrace)
            .MaximumLength(8000).WithMessage("تتبع الاستدعاء لا يمكن أن يتجاوز 8000 حرف")
            .When(x => !string.IsNullOrEmpty(x.StackTrace));

        RuleFor(x => x.Source)
            .MaximumLength(200).WithMessage("المصدر لا يمكن أن يتجاوز 200 حرف")
            .When(x => !string.IsNullOrEmpty(x.Source));

        RuleFor(x => x.Context)
            .MaximumLength(500).WithMessage("السياق لا يمكن أن يتجاوز 500 حرف")
            .When(x => !string.IsNullOrEmpty(x.Context));

        RuleFor(x => x.MachineName)
            .MaximumLength(100).WithMessage("اسم الجهاز لا يمكن أن يتجاوز 100 حرف")
            .When(x => !string.IsNullOrEmpty(x.MachineName));
    }

    private static bool BeValidLogLevel(string? logLevel)
    {
        if (string.IsNullOrEmpty(logLevel))
            return false;

        return ValidLogLevels.Contains(logLevel, StringComparer.OrdinalIgnoreCase);
    }
}
