using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateAttachmentRequestValidator : AbstractValidator<CreateAttachmentRequest>
{
    public CreateAttachmentRequestValidator()
    {
        RuleFor(x => x.ReferenceType)
            .NotEmpty().WithMessage("نوع المرجع مطلوب")
            .MaximumLength(50).WithMessage("نوع المرجع لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.ReferenceId)
            .GreaterThan(0).WithMessage("معرّف المرجع غير صالح");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("اسم الملف مطلوب")
            .MaximumLength(255).WithMessage("اسم الملف لا يمكن أن يتجاوز 255 حرف");

        RuleFor(x => x.FilePath)
            .NotEmpty().WithMessage("مسار الملف مطلوب")
            .MaximumLength(500).WithMessage("مسار الملف لا يمكن أن يتجاوز 500 حرف");

        RuleFor(x => x.FileSize)
            .GreaterThanOrEqualTo(0).WithMessage("حجم الملف لا يمكن أن يكون سالباً");

        RuleFor(x => x.ContentType)
            .MaximumLength(100).When(x => x.ContentType != null)
            .WithMessage("نوع المحتوى لا يمكن أن يتجاوز 100 حرف");
    }
}

public class UpdateAttachmentRequestValidator : AbstractValidator<UpdateAttachmentRequest>
{
    public UpdateAttachmentRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("اسم الملف مطلوب")
            .MaximumLength(255).WithMessage("اسم الملف لا يمكن أن يتجاوز 255 حرف");

        RuleFor(x => x.FilePath)
            .NotEmpty().WithMessage("مسار الملف مطلوب")
            .MaximumLength(500).WithMessage("مسار الملف لا يمكن أن يتجاوز 500 حرف");

        RuleFor(x => x.FileSize)
            .GreaterThanOrEqualTo(0).WithMessage("حجم الملف لا يمكن أن يكون سالباً");

        RuleFor(x => x.ContentType)
            .MaximumLength(100).When(x => x.ContentType != null)
            .WithMessage("نوع المحتوى لا يمكن أن يتجاوز 100 حرف");
    }
}
