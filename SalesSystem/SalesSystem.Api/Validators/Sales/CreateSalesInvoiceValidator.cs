using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Sales;

public class CreateSalesInvoiceValidator : AbstractValidator<CreateSalesInvoiceRequest>
{
    public CreateSalesInvoiceValidator()
    {
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("ظٹط¬ط¨ ط§ط®طھظٹط§ط± ط§ظ„ظ…ط³طھظˆط¯ط¹");
        RuleFor(x => x.PaidAmount).GreaterThanOrEqualTo(0).WithMessage("ط§ظ„ظ…ط¨ظ„ط؛ ط§ظ„ظ…ط¯ظپظˆط¹ ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("ط§ظ„ط®طµظ… ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0).WithMessage("ط§ظ„ط¶ط±ظٹط¨ط© ظ„ط§ ظٹظ…ظƒظ† ط£ظ† طھظƒظˆظ† ط³ط§ظ„ط¨ط©");
        RuleFor(x => x.Items).NotEmpty().WithMessage("ظٹط¬ط¨ ط¥ط¶ط§ظپط© طµظ†ظپ ظˆط§ط­ط¯ ط¹ظ„ظ‰ ط§ظ„ط£ظ‚ظ„");
        
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("ظٹط¬ط¨ ط§ط®طھظٹط§ط± ط§ظ„ظ…ظ†طھط¬");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("ط§ظ„ظƒظ…ظٹط© ظٹط¬ط¨ ط£ظ† طھظƒظˆظ† ط£ظƒط¨ط± ظ…ظ† طµظپط±");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("ط§ظ„ط³ط¹ط± ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");
            item.RuleFor(i => i.DiscountAmount).GreaterThanOrEqualTo(0).WithMessage("ط§ظ„ط®طµظ… ظ„ط§ ظٹظ…ظƒظ† ط£ظ† ظٹظƒظˆظ† ط³ط§ظ„ط¨ط§ظ‹");
        });

        // Business Rule: If payment type is Credit, CustomerId is usually required in logic, 
        // but some shops allow "General Customer" credit. 
        // Constitution says: if invoice.DueAmount > 0 load customer. So we should enforce it here if possible.
    }
}

