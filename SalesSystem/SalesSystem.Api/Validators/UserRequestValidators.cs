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

        RuleFor(x => x.Role)
            .InclusiveBetween((byte)1, (byte)9).WithMessage("دور المستخدم غير صالح — الأرقام من 1 إلى 9");

        RuleFor(x => x.RoleIds)
            .NotEmpty().WithMessage("يجب تحديد دور واحد على الأقل")
            .When(x => x.RoleIds != null && x.RoleIds.Count == 0);

        RuleFor(x => x.DefaultCashBoxId)
            .GreaterThan(0).WithMessage("معرف الصندوق الافتراضي غير صالح")
            .When(x => x.DefaultCashBoxId.HasValue);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Role)
            .InclusiveBetween((byte)1, (byte)9).WithMessage("دور المستخدم غير صالح — الأرقام من 1 إلى 9");

        RuleFor(x => x.Password)
            .MinimumLength(8).WithMessage("كلمة المرور يجب أن تكون 8 أحرف على الأقل")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.DefaultCashBoxId)
            .GreaterThan(0).WithMessage("معرف الصندوق الافتراضي غير صالح")
            .When(x => x.DefaultCashBoxId.HasValue);
    }
}
