using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("اسم المستخدم مطلوب")
            .MaximumLength(100).WithMessage("اسم المستخدم يجب ألا يتجاوز 100 حرفاً")
            .Matches(@"^[a-zA-Z0-9_\.]+$").WithMessage("اسم المستخدم يمكن أن يحتوي فقط على أحرف، أرقام، شرطة سفلية ونقاط");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب")
            .MaximumLength(150).WithMessage("الاسم الكامل يجب ألا يتجاوز 150 حرفاً");

        RuleFor(x => x.Role)
            .InclusiveBetween((byte)1, (byte)3).WithMessage("دور المستخدم غير صالح");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("رقم الهاتف يجب ألا يتجاوز 20 رقماً")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .MaximumLength(100).WithMessage("البريد الإلكتروني يجب ألا يتجاوز 100 حرفاً")
            .EmailAddress().WithMessage("البريد الإلكتروني غير صالح")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.DefaultCashBoxId)
            .GreaterThan(0).WithMessage("معرف الصندوق الافتراضي غير صالح")
            .When(x => x.DefaultCashBoxId.HasValue);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب")
            .MaximumLength(150).WithMessage("الاسم الكامل يجب ألا يتجاوز 150 حرفاً");

        RuleFor(x => x.Role)
            .InclusiveBetween((byte)1, (byte)3).WithMessage("دور المستخدم غير صالح");

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("كلمة المرور يجب أن تكون 8 أحرف على الأقل")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("رقم الهاتف يجب ألا يتجاوز 20 رقماً")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .MaximumLength(100).WithMessage("البريد الإلكتروني يجب ألا يتجاوز 100 حرفاً")
            .EmailAddress().WithMessage("البريد الإلكتروني غير صالح")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.DefaultCashBoxId)
            .GreaterThan(0).WithMessage("معرف الصندوق الافتراضي غير صالح")
            .When(x => x.DefaultCashBoxId.HasValue);
    }
}
