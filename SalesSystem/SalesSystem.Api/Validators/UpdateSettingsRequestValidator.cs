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

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("العملة مطلوبة")
            .MaximumLength(10).WithMessage("العملة يجب ألا تتجاوز 10 أحرف");

        RuleFor(x => x.DefaultTaxRate)
            .InclusiveBetween(0m, 100m).WithMessage("نسبة الضريبة يجب أن تكون بين 0 و 100");

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50).WithMessage("الرقم الضريبي يجب ألا يتجاوز 50 حرف")
            .When(x => !string.IsNullOrEmpty(x.TaxNumber));

        RuleFor(x => x.InvoicePrefix)
            .NotEmpty().WithMessage("بادئة الفاتورة مطلوبة")
            .MaximumLength(10).WithMessage("بادئة الفاتورة يجب ألا تتجاوز 10 أحرف");

        RuleFor(x => x.CostingMethod)
            .InclusiveBetween(1, 3).WithMessage("طريقة التكلفة غير صالحة — يجب أن تكون 1 (متوسط مرجح)، 2 (آخر سعر شراء)، أو 3 (سعر المورد)");
    }
}
