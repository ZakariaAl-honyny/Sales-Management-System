using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateBranchRequestValidator : AbstractValidator<CreateBranchRequest>
{
    public CreateBranchRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم الفرع مطلوب")
            .MaximumLength(150).WithMessage("اسم الفرع لا يمكن أن يتجاوز 150 حرف");

        RuleFor(x => x.Code)
            .MaximumLength(20).When(x => x.Code != null)
            .WithMessage("رمز الفرع لا يمكن أن يتجاوز 20 حرف");
    }
}

public class UpdateBranchRequestValidator : AbstractValidator<UpdateBranchRequest>
{
    public UpdateBranchRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم الفرع مطلوب")
            .MaximumLength(150).WithMessage("اسم الفرع لا يمكن أن يتجاوز 150 حرف");

        RuleFor(x => x.Code)
            .MaximumLength(20).When(x => x.Code != null)
            .WithMessage("رمز الفرع لا يمكن أن يتجاوز 20 حرف");
    }
}
