using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class UpdateDocumentSequenceRequestValidator : AbstractValidator<UpdateDocumentSequenceRequest>
{
    public UpdateDocumentSequenceRequestValidator()
    {
        RuleFor(x => x.NextNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("الرقم التسلسلي يجب أن يكون 1 أو أكثر");
    }
}
