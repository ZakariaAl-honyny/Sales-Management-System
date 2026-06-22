using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        // AccountCode is NOT user-supplied — auto-generated via AccountCodeGeneratorService

        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم الحساب بالعربية مطلوب")
            .MaximumLength(200).WithMessage("اسم الحساب يجب ألا يتجاوز 200 حرف");

        RuleFor(x => x.NameEn)
            .MaximumLength(200).WithMessage("الاسم بالإنجليزية يجب ألا يتجاوز 200 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.NameEn));

        RuleFor(x => x.Nature)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("نوع الحساب غير صالح — القيم المسموحة: 1-5");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("الوصف يجب ألا يتجاوز 500 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Notes)
            .MaximumLength(300).WithMessage("الملاحظات يجب ألا تتجاوز 300 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));

        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0).WithMessage("الرصيد الافتتاحي لا يمكن أن يكون سالباً")
            .When(x => x.OpeningBalance.HasValue);
    }
}

public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithMessage("اسم الحساب بالعربية مطلوب")
            .MaximumLength(200).WithMessage("اسم الحساب (عربي) يجب ألا يتجاوز 200 حرف");

        RuleFor(x => x.NameEn)
            .MaximumLength(200).WithMessage("اسم الحساب (إنجليزي) يجب ألا يتجاوز 200 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.NameEn));

        RuleFor(x => x.Nature)
            .InclusiveBetween((byte)1, (byte)5).WithMessage("نوع الحساب غير صالح — القيم المسموحة: 1-5");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("الوصف يجب ألا يتجاوز 500 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Notes)
            .MaximumLength(300).WithMessage("الملاحظات يجب ألا تتجاوز 300 حرف")
            .When(x => !string.IsNullOrWhiteSpace(x.Notes));
    }
}
