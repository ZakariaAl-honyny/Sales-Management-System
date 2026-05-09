using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("ط§ط³ظ… ط§ظ„ظ…ظ†طھط¬ ظ…ط·ظ„ظˆط¨")
            .MaximumLength(200).WithMessage("ط§ط³ظ… ط§ظ„ظ…ظ†طھط¬ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 200 ط­ط±ظپ");

        RuleFor(x => x.Code)
            .MaximumLength(50).WithMessage("ظƒظˆط¯ ط§ظ„ظ…ظ†طھط¬ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 50 ط­ط±ظپ");

        RuleFor(x => x.Barcode)
            .MaximumLength(50).WithMessage("ط§ظ„ط¨ط§ط±ظƒظˆط¯ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹطھط¬ط§ظˆط² 50 ط­ط±ظپ");

        RuleFor(x => x.SalePrice)
            .GreaterThanOrEqualTo(0).WithMessage("ط³ط¹ط± ط§ظ„ط¨ظٹط¹ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("ط³ط¹ط± ط§ظ„ط´ط±ط§ط، ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");

        RuleFor(x => x.MinStock)
            .GreaterThanOrEqualTo(0).WithMessage("ط§ظ„ط­ط¯ ط§ظ„ط£ط¯ظ†ظ‰ ظ„ظ„ظ…ط®ط²ظˆظ† ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("ظٹط¬ط¨ ط§ط®طھظٹط§ط± طھطµظ†ظٹظپ طµط­ظٹط­");

        RuleFor(x => x.UnitId)
            .GreaterThan(0).WithMessage("ظٹط¬ط¨ ط§ط®طھظٹط§ط± ظˆط­ط¯ط© طµط­ظٹط­ط©");
    }
}

