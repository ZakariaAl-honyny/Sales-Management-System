using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateAccountCategoryRequestValidator : AbstractValidator<CreateAccountCategoryRequest>
{
    public CreateAccountCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم التصنيف المحاسبي مطلوب")
            .MaximumLength(100).WithMessage("اسم التصنيف لا يمكن أن يتجاوز 100 حرف");
    }
}

public class UpdateAccountCategoryRequestValidator : AbstractValidator<UpdateAccountCategoryRequest>
{
    public UpdateAccountCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم التصنيف المحاسبي مطلوب")
            .MaximumLength(100).WithMessage("اسم التصنيف لا يمكن أن يتجاوز 100 حرف");
    }
}
