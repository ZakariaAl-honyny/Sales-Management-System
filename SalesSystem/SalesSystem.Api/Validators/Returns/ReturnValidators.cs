using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Returns;

public class CreateSalesReturnValidator : AbstractValidator<CreateSalesReturnRequest>
{
    public CreateSalesReturnValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("ظٹط¬ط¨ ط§ط®طھظٹط§ط± ط§ظ„ظ…ط³طھظˆط¯ط¹");
        RuleFor(x => x.Items).NotEmpty().WithMessage("ظٹط¬ط¨ ط¥ط¶ط§ظپط© طµظ†ظپ ظˆط§ط­ط¯ ط¹ظ„ظ‰ ط§ظ„ط£ظ‚ظ„");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("ظٹط¬ط¨ ط§ط®طھظٹط§ط± ط§ظ„ظ…ظ†طھط¬");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("ط§ظ„ظƒظ…ظٹط© ظٹط¬ط¨ ط£ظ† طھظƒظˆظ† ط£ظƒط¨ط± ظ…ظ† طµظپط±");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("ط§ظ„ط³ط¹ط± ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");
        });
    }
}

public class CreatePurchaseReturnValidator : AbstractValidator<CreatePurchaseReturnRequest>
{
    public CreatePurchaseReturnValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("ظٹط¬ط¨ ط§ط®طھظٹط§ط± ط§ظ„ظ…ظˆط±ط¯");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("ظٹط¬ط¨ ط§ط®طھظٹط§ط± ط§ظ„ظ…ط³طھظˆط¯ط¹");
        RuleFor(x => x.Items).NotEmpty().WithMessage("ظٹط¬ط¨ ط¥ط¶ط§ظپط© طµظ†ظپ ظˆط§ط­ط¯ ط¹ظ„ظ‰ ط§ظ„ط£ظ‚ظ„");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("ظٹط¬ط¨ ط§ط®طھظٹط§ط± ط§ظ„ظ…ظ†طھط¬");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("ط§ظ„ظƒظ…ظٹط© ظٹط¬ط¨ ط£ظ† طھظƒظˆظ† ط£ظƒط¨ط± ظ…ظ† طµظپط±");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("ط§ظ„ط³ط¹ط± ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");
        });
    }
}

