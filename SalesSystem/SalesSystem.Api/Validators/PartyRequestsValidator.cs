using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreatePartyRequestValidator : AbstractValidator<CreatePartyRequest>
{
    public CreatePartyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم الطرف مطلوب")
            .MaximumLength(200).WithMessage("اسم الطرف لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(30).When(x => x.Phone != null)
            .WithMessage("رقم الهاتف لا يمكن أن يتجاوز 30 حرف");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("البريد الإلكتروني غير صحيح")
            .MaximumLength(100).When(x => x.Email != null)
            .WithMessage("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Address)
            .MaximumLength(300).When(x => x.Address != null)
            .WithMessage("العنوان لا يمكن أن يتجاوز 300 حرف");

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50).When(x => x.TaxNumber != null)
            .WithMessage("الرقم الضريبي لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}

public class UpdatePartyRequestValidator : AbstractValidator<UpdatePartyRequest>
{
    public UpdatePartyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم الطرف مطلوب")
            .MaximumLength(200).WithMessage("اسم الطرف لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(30).When(x => x.Phone != null)
            .WithMessage("رقم الهاتف لا يمكن أن يتجاوز 30 حرف");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("البريد الإلكتروني غير صحيح")
            .MaximumLength(100).When(x => x.Email != null)
            .WithMessage("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Address)
            .MaximumLength(300).When(x => x.Address != null)
            .WithMessage("العنوان لا يمكن أن يتجاوز 300 حرف");

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50).When(x => x.TaxNumber != null)
            .WithMessage("الرقم الضريبي لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.Notes)
            .MaximumLength(500).When(x => x.Notes != null)
            .WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}
