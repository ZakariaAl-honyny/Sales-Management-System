using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateUnitRequestValidator : AbstractValidator<CreateUnitRequest>
{
    public CreateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ط§ط³ظ… ط§ظ„ظˆط­ط¯ط© ظ…ط·ظ„ظˆط¨")
            .MaximumLength(50).WithMessage("ط§ط³ظ… ط§ظ„ظˆط­ط¯ط© ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 50 ط­ط±ظپ");

        RuleFor(x => x.Symbol)
            .MaximumLength(10).WithMessage("ط§ظ„ط±ظ…ط² ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 10 ط£ط­ط±ظپ");
    }
}

public class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ط§ط³ظ… ط§ظ„ظˆط­ط¯ط© ظ…ط·ظ„ظˆط¨")
            .MaximumLength(50).WithMessage("ط§ط³ظ… ط§ظ„ظˆط­ط¯ط© ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 50 ط­ط±ظپ");

        RuleFor(x => x.Symbol)
            .MaximumLength(10).WithMessage("ط§ظ„ط±ظ…ط² ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 10 ط£ط­ط±ظپ");
    }
}

