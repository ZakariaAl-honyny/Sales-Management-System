using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ط§ط³ظ… ط§ظ„ظ…ط®ط²ظ† ظ…ط·ظ„ظˆط¨")
            .MaximumLength(100).WithMessage("ط§ط³ظ… ط§ظ„ظ…ط®ط²ظ† ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 100 ط­ط±ظپ");

        RuleFor(x => x.Code)
            .MaximumLength(30).WithMessage("ظƒظˆط¯ ط§ظ„ظ…ط®ط²ظ† ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 30 ط­ط±ظپ");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("ط§ظ„ط¹ظ†ظˆط§ظ† ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 200 ط­ط±ظپ");
    }
}

public class UpdateWarehouseRequestValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ط§ط³ظ… ط§ظ„ظ…ط®ط²ظ† ظ…ط·ظ„ظˆط¨")
            .MaximumLength(100).WithMessage("ط§ط³ظ… ط§ظ„ظ…ط®ط²ظ† ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 100 ط­ط±ظپ");

        RuleFor(x => x.Code)
            .MaximumLength(30).WithMessage("ظƒظˆط¯ ط§ظ„ظ…ط®ط²ظ† ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 30 ط­ط±ظپ");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("ط§ظ„ط¹ظ†ظˆط§ظ† ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 200 ط­ط±ظپ");
    }
}

