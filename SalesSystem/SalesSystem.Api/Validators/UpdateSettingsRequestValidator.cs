using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator()
    {
        RuleFor(x => x.StoreName)
            .NotEmpty().WithMessage("اسم المتجر مطلوب")
            .MaximumLength(200).WithMessage("اسم المتجر يجب ألا يتجاوز 200 حرف");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("العنوان يجب ألا يتجاوز 500 حرف")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("رقم الهاتف يجب ألا يتجاوز 20 رقماً")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .MaximumLength(100).WithMessage("البريد الإلكتروني يجب ألا يتجاوز 100 حرف")
            .EmailAddress().WithMessage("البريد الإلكتروني غير صالح")
            .When(x => !string.IsNullOrEmpty(x.Email));

        // DEPRECATED: DefaultTaxRate — use Tax entity instead. Remove in Phase 20.
        RuleFor(x => x.DefaultTaxRate)
            .InclusiveBetween(0m, 100m).WithMessage("نسبة الضريبة يجب أن تكون بين 0 و 100")
            .When(x => x.DefaultTaxRate > 0);

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50).WithMessage("الرقم الضريبي يجب ألا يتجاوز 50 حرف")
            .When(x => !string.IsNullOrEmpty(x.TaxNumber));

        // DEPRECATED: InvoicePrefix — use InvoiceNo (int) instead. Remove in Phase 20.
        RuleFor(x => x.InvoicePrefix)
            .MaximumLength(10).WithMessage("بادئة الفاتورة يجب ألا تتجاوز 10 أحرف")
            .When(x => !string.IsNullOrEmpty(x.InvoicePrefix));

    }
}
