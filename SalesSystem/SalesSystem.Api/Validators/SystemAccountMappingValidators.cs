using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateSystemAccountMappingRequestValidator : AbstractValidator<CreateSystemAccountMappingRequest>
{
    public CreateSystemAccountMappingRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("رقم الحساب المحاسبي مطلوب");

    }
}

public class UpdateSystemAccountMappingRequestValidator : AbstractValidator<UpdateSystemAccountMappingRequest>
{
    public UpdateSystemAccountMappingRequestValidator()
    {
        RuleFor(x => x.AccountId)
            .GreaterThan(0).WithMessage("رقم الحساب المحاسبي مطلوب");
    }
}
