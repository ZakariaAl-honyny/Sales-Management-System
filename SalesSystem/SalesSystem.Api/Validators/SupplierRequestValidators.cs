using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المورد مطلوب")
            .MaximumLength(150).WithMessage("اسم المورد لا يمكن أن يتجاوز 150 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("رقم الهاتف لا يمكن أن يتجاوز 20 حرف");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("البريد الإلكتروني غير صحيح")
            .MaximumLength(100).WithMessage("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0).WithMessage("الرصيد الافتتاحي لا يمكن أن يكون سالباً");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("حد الائتمان لا يمكن أن يكون سالباً");
    }
}

public class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم المورد مطلوب")
            .MaximumLength(150).WithMessage("اسم المورد لا يمكن أن يتجاوز 150 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("رقم الهاتف لا يمكن أن يتجاوز 20 حرف");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("البريد الإلكتروني غير صحيح")
            .MaximumLength(100).WithMessage("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0).WithMessage("حد الائتمان لا يمكن أن يكون سالباً");
    }
}

