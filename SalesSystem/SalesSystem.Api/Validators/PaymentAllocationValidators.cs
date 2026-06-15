using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators;

public class UpdateAllocationsRequestValidator : AbstractValidator<UpdateAllocationsRequest>
{
    public UpdateAllocationsRequestValidator()
    {
        RuleFor(x => x.Allocations)
            .NotNull().WithMessage("يجب تقديم قائمة التوزيعات")
            .NotEmpty().WithMessage("يجب تقديم توزيع واحد على الأقل");

        RuleForEach(x => x.Allocations)
            .SetValidator(new CreateAllocationRequestValidator());
    }
}

public class CreateAllocationRequestValidator : AbstractValidator<CreateAllocationRequest>
{
    public CreateAllocationRequestValidator()
    {
        RuleFor(x => x.InvoiceId)
            .GreaterThan(0).WithMessage("معرف الفاتورة غير صالح");

        RuleFor(x => x.InvoiceType)
            .Must(t => t == 1 || t == 2)
            .WithMessage("نوع الفاتورة غير صالح — 1 للمبيعات، 2 للمشتريات");

        RuleFor(x => x.AllocatedAmount)
            .GreaterThan(0).WithMessage("المبلغ المخصص يجب أن يكون أكبر من الصفر")
            .PrecisionScale(18, 2, false).WithMessage("المبلغ يجب أن يكون برقمين عشريين كحد أقصى");
    }
}
