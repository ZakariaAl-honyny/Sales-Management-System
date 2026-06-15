using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم القسم مطلوب")
            .MaximumLength(150).WithMessage("اسم القسم لا يمكن أن يتجاوز 150 حرف");
    }
}

public class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم القسم مطلوب")
            .MaximumLength(150).WithMessage("اسم القسم لا يمكن أن يتجاوز 150 حرف");
    }
}
