using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateSystemAccountMappingRequestValidator : AbstractValidator<CreateSystemAccountMappingRequest>
{
    public CreateSystemAccountMappingRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("رقم الحساب المحاسبي مطلوب");

        RuleFor(x => x.DescriptionAr)
            .MaximumLength(200).WithMessage("الوصف بالعربية يجب أن لا يتجاوز 200 حرف")
            .When(x => x.DescriptionAr != null);

        RuleFor(x => x.DescriptionEn)
            .MaximumLength(200).WithMessage("الوصف بالإنجليزية يجب أن لا يتجاوز 200 حرف")
            .When(x => x.DescriptionEn != null);
    }
}

public class UpdateSystemAccountMappingRequestValidator : AbstractValidator<UpdateSystemAccountMappingRequest>
{
    public UpdateSystemAccountMappingRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("رقم الحساب المحاسبي مطلوب");

        RuleFor(x => x.DescriptionAr)
            .MaximumLength(200).WithMessage("الوصف بالعربية يجب أن لا يتجاوز 200 حرف")
            .When(x => x.DescriptionAr != null);

        RuleFor(x => x.DescriptionEn)
            .MaximumLength(200).WithMessage("الوصف بالإنجليزية يجب أن لا يتجاوز 200 حرف")
            .When(x => x.DescriptionEn != null);
    }
}
