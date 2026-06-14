using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.PartyId)
            .GreaterThan(0).WithMessage("معرف الطرف مطلوب");

        RuleFor(x => x.EmployeeNo)
            .GreaterThan(0).WithMessage("رقم الموظف يجب أن يكون أكبر من صفر");

        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("تاريخ التعيين مطلوب");

        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0).WithMessage("الراتب لا يمكن أن يكون سالباً");

        RuleFor(x => x.Notes)
            .MaximumLength(300).WithMessage("الملاحظات لا يمكن أن تتجاوز 300 حرف");
    }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.Salary)
            .GreaterThanOrEqualTo(0).WithMessage("الراتب لا يمكن أن يكون سالباً")
            .When(x => x.Salary.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(300).WithMessage("الملاحظات لا يمكن أن تتجاوز 300 حرف");
    }
}
