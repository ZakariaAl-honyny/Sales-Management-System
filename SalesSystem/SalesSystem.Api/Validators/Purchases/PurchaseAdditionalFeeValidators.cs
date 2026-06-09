using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Purchases;

/// <summary>
/// مدقق صحة طلب إنشاء مصروف إضافي لفاتورة الشراء.
/// </summary>
public class CreateAdditionalFeeRequestValidator : AbstractValidator<CreateAdditionalFeeRequest>
{
    public CreateAdditionalFeeRequestValidator()
    {
        RuleFor(x => x.FeeName)
            .NotEmpty().WithMessage("اسم الرسم الإضافي مطلوب")
            .MaximumLength(100).WithMessage("اسم الرسم الإضافي لا يتجاوز 100 حرف");

        RuleFor(x => x.FeeAmount)
            .GreaterThan(0).WithMessage("قيمة الرسم الإضافي يجب أن تكون أكبر من صفر");

        RuleFor(x => x.DistributionMethod)
            .InclusiveBetween((byte)0, (byte)1)
            .WithMessage("طريقة التوزيع غير صحيحة (0 = حسب التكلفة، 1 = حسب الكمية)");
    }
}
