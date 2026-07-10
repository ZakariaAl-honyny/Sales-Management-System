using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم العميل مطلوب")
            .MaximumLength(100).WithMessage("اسم العميل لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("رقم الهاتف لا يمكن أن يتجاوز 50 حرف")
            .Matches(@"^05\d{8}$").WithMessage("رقم الهاتف يجب أن يبدأ بـ 05 ويتكون من 10 أرقام")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .MaximumLength(100).WithMessage("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف")
            .EmailAddress().WithMessage("البريد الإلكتروني غير صحيح")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.TaxNumber)
            .MaximumLength(20).WithMessage("الرقم الضريبي لا يمكن أن يتجاوز 20 حرف")
            .When(x => !string.IsNullOrEmpty(x.TaxNumber));

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("حد الائتمان لا يمكن أن يكون سالباً");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("الملاحظات لا يمكن أن تتجاوز 1000 حرف")
            .When(x => !string.IsNullOrEmpty(x.Notes));

    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم العميل مطلوب")
            .MaximumLength(100).WithMessage("اسم العميل لا يمكن أن يتجاوز 100 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("رقم الهاتف لا يمكن أن يتجاوز 50 حرف")
            .Matches(@"^05\d{8}$").WithMessage("رقم الهاتف يجب أن يبدأ بـ 05 ويتكون من 10 أرقام")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .MaximumLength(100).WithMessage("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف")
            .EmailAddress().WithMessage("البريد الإلكتروني غير صحيح")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.TaxNumber)
            .MaximumLength(20).WithMessage("الرقم الضريبي لا يمكن أن يتجاوز 20 حرف")
            .When(x => !string.IsNullOrEmpty(x.TaxNumber));

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("حد الائتمان لا يمكن أن يكون سالباً");

        RuleFor(x => x.IsActive)
            .NotNull().WithMessage("حالة التفعيل مطلوبة");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("الملاحظات لا يمكن أن تتجاوز 1000 حرف")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
