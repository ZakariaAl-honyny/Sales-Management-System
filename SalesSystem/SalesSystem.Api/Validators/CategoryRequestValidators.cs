using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ط§ط³ظ… ط§ظ„ظپط¦ط© ظ…ط·ظ„ظˆط¨")
            .MaximumLength(100).WithMessage("ط§ط³ظ… ط§ظ„ظپط¦ط© ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 100 ط­ط±ظپ");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("ط§ظ„ظˆطµظپ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 500 ط­ط±ظپ");
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ط§ط³ظ… ط§ظ„ظپط¦ط© ظ…ط·ظ„ظˆط¨")
            .MaximumLength(100).WithMessage("ط§ط³ظ… ط§ظ„ظپط¦ط© ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 100 ط­ط±ظپ");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("ط§ظ„ظˆطµظپ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 500 ط­ط±ظپ");
    }
}

