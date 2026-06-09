using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class CreatePurchaseOrderRequestValidator : AbstractValidator<CreatePurchaseOrderRequest>
{
    public CreatePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("المورد مطلوب");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("المستودع مطلوب");
        RuleFor(x => x.Items).NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("المنتج مطلوب");
            item.RuleFor(i => i.ProductUnitId).GreaterThan(0).WithMessage("الوحدة مطلوبة");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0).WithMessage("التكلفة لا يمكن أن تكون سالبة");
        });
    }
}

public class UpdatePurchaseOrderRequestValidator : AbstractValidator<UpdatePurchaseOrderRequest>
{
    public UpdatePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0).WithMessage("المورد مطلوب");
        RuleFor(x => x.WarehouseId).GreaterThan(0).WithMessage("المستودع مطلوب");
        RuleFor(x => x.Items).NotEmpty().WithMessage("يجب إضافة صنف واحد على الأقل");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0).WithMessage("المنتج مطلوب");
            item.RuleFor(i => i.ProductUnitId).GreaterThan(0).WithMessage("الوحدة مطلوبة");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("الكمية يجب أن تكون أكبر من صفر");
            item.RuleFor(i => i.UnitCost).GreaterThanOrEqualTo(0).WithMessage("التكلفة لا يمكن أن تكون سالبة");
        });
    }
}

public class CreateAdditionalFeeRequestValidator : AbstractValidator<CreateAdditionalFeeRequest>
{
    public CreateAdditionalFeeRequestValidator()
    {
        RuleFor(x => x.FeeName).NotEmpty().MaximumLength(100).WithMessage("اسم الرسم مطلوب");
        RuleFor(x => x.FeeAmount).GreaterThan(0).WithMessage("القيمة يجب أن تكون أكبر من صفر");
        RuleFor(x => (int)x.DistributionMethod).InclusiveBetween(0, 1).WithMessage("طريقة التوزيع غير صحيحة");
    }
}

public class UploadAttachmentRequestValidator : AbstractValidator<UploadAttachmentRequest>
{
    public UploadAttachmentRequestValidator()
    {
        RuleFor(x => x.Base64Content).NotEmpty().WithMessage("محتوى المرفق مطلوب");
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255).WithMessage("اسم الملف مطلوب");
    }
}
