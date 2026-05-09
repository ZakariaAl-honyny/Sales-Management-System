using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ط§ط³ظ… ط§ظ„ط¹ظ…ظٹظ„ ظ…ط·ظ„ظˆط¨")
            .MaximumLength(150).WithMessage("ط§ط³ظ… ط§ظ„ط¹ظ…ظٹظ„ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 150 ط­ط±ظپ");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("ط±ظ‚ظ… ط§ظ„ظ‡ط§طھظپ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 20 ط­ط±ظپ");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ ط؛ظٹط± طµط­ظٹط­")
            .MaximumLength(100).WithMessage("ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 100 ط­ط±ظپ")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Code)
            .MaximumLength(30).WithMessage("ظƒظˆط¯ ط§ظ„ط¹ظ…ظٹظ„ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 30 ط­ط±ظپ");
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ط§ط³ظ… ط§ظ„ط¹ظ…ظٹظ„ ظ…ط·ظ„ظˆط¨")
            .MaximumLength(150).WithMessage("ط§ط³ظ… ط§ظ„ط¹ظ…ظٹظ„ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 150 ط­ط±ظپ");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("ط±ظ‚ظ… ط§ظ„ظ‡ط§طھظپ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 20 ط­ط±ظپ");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ ط؛ظٹط± طµط­ظٹط­")
            .MaximumLength(100).WithMessage("ط§ظ„ط¨ط±ظٹط¯ ط§ظ„ط¥ظ„ظƒطھط±ظˆظ†ظٹ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 100 ط­ط±ظپ")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Code)
            .MaximumLength(30).WithMessage("ظƒظˆط¯ ط§ظ„ط¹ظ…ظٹظ„ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 30 ط­ط±ظپ");
    }
}

