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

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة")
            .MinimumLength(8).WithMessage("كلمة المرور يجب أن تكون 8 أحرف على الأقل");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب")
            .MaximumLength(150).WithMessage("الاسم الكامل يجب ألا يتجاوز 150 حرفاً");

        RuleFor(x => x.Role)
            .InclusiveBetween((byte)1, (byte)3).WithMessage("دور المستخدم غير صالح");
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
    }
}
