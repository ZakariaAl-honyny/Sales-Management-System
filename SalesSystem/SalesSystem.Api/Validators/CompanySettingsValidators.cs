using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class UpdateCompanySettingsRequestValidator : AbstractValidator<UpdateCompanySettingsRequest>
{
    public UpdateCompanySettingsRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("اسم الشركة مطلوب")
            .MaximumLength(200).WithMessage("اسم الشركة لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(30).When(x => x.Phone != null)
            .WithMessage("رقم الهاتف لا يمكن أن يتجاوز 30 حرفاً");

        RuleFor(x => x.Email)
            .MaximumLength(100).When(x => x.Email != null)
            .WithMessage("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Address)
            .MaximumLength(300).When(x => x.Address != null)
            .WithMessage("العنوان لا يمكن أن يتجاوز 300 حرف");

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50).When(x => x.TaxNumber != null)
            .WithMessage("الرقم الضريبي لا يمكن أن يتجاوز 50 حرفاً");
    }
}
