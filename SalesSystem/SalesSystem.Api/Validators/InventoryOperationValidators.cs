using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreateInventoryOperationRequestValidator : AbstractValidator<CreateInventoryOperationRequest>
{
    public CreateInventoryOperationRequestValidator()
    {
        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).WithMessage("المستودع مطلوب");

        RuleFor(x => x.OperationType)
            .InclusiveBetween((byte)1, (byte)3)
            .WithMessage("نوع العملية يجب أن يكون بين 1 و 3");

        RuleFor(x => x.AdjustmentType)
            .InclusiveBetween((byte)1, (byte)2)
            .WithMessage("نوع التسوية يجب أن يكون 1 (فائض) أو 2 (عجز)")
            .When(x => x.OperationType == 3 && x.AdjustmentType.HasValue);

        RuleFor(x => x.OperationDate)
            .Must(d => d == null || d.Value <= DateTime.UtcNow.AddMinutes(1))
            .WithMessage("تاريخ العملية لا يمكن أن يكون في المستقبل");

        RuleFor(x => x.ReferenceNo)
            .MaximumLength(50).WithMessage("الرقم المرجعي لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateInventoryOperationItemRequestValidator());
    }
}

public class CreateInventoryOperationItemRequestValidator : AbstractValidator<CreateInventoryOperationItemRequest>
{
    public CreateInventoryOperationItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("المنتج مطلوب");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من الصفر");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("التكلفة لا يمكن أن تكون سالبة")
            .When(x => x.UnitCost.HasValue);

        RuleFor(x => x.StockIssueReason)
            .InclusiveBetween((byte)1, (byte)4)
            .WithMessage("سبب الصرف يجب أن يكون بين 1 و 4")
            .When(x => x.StockIssueReason.HasValue);

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("الملاحظات لا يمكن أن تتجاوز 500 حرف");
    }
}
