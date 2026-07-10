using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateCashBoxRequestValidator : AbstractValidator<CreateCashBoxRequest>
{
    public CreateCashBoxRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("اسم الخزنة مطلوب")
            .MaximumLength(150).WithMessage("اسم الخزنة لا يمكن أن يتجاوز 150 حرف");

        RuleFor(x => x.Description)
            .MaximumLength(300).When(x => x.Description != null)
            .WithMessage("الوصف لا يمكن أن يتجاوز 300 حرف");
    }
}
