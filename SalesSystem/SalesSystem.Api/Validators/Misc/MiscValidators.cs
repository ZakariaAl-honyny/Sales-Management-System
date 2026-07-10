using FluentValidation;
using SalesSystem.Contracts.Requests;

namespace SalesSystem.Api.Validators.Misc;

public class ReportFilterRequestValidator : AbstractValidator<ReportFilterRequest>
{
    public ReportFilterRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0).When(x => x.CustomerId.HasValue).WithMessage("يجب اختيار عميل صحيح");

        RuleFor(x => x.SupplierId)
            .GreaterThan(0).When(x => x.SupplierId.HasValue).WithMessage("يجب اختيار مورد صحيح");

        RuleFor(x => x.WarehouseId)
            .GreaterThan(0).When(x => x.WarehouseId.HasValue).WithMessage("يجب اختيار مستودع صحيح");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).When(x => x.ProductId.HasValue).WithMessage("يجب اختيار منتج صحيح");

        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo).WithMessage("تاريخ البداية يجب أن يكون قبل تاريخ النهاية")
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue);
    }
}

public class UpdatePrintSettingsRequestValidator : AbstractValidator<UpdatePrintSettingsRequest>
{
    public UpdatePrintSettingsRequestValidator()
    {
        RuleFor(x => x.ThermalPrinterName)
            .MaximumLength(200).WithMessage("اسم الطابعة الحرارية لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.A4PrinterName)
            .MaximumLength(200).WithMessage("اسم طابعة A4 لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.StoreTaxNumber)
            .MaximumLength(50).WithMessage("الرقم الضريبي لا يمكن أن يتجاوز 50 حرف");

        RuleFor(x => x.TaxRate)
            .GreaterThanOrEqualTo(0).WithMessage("نسبة الضريبة لا يمكن أن تكون سالبة")
            .LessThanOrEqualTo(100).WithMessage("نسبة الضريبة لا يمكن أن تتجاوز 100%");

        RuleFor(x => x.ReceiptHeader)
            .MaximumLength(200).WithMessage("رأس الإيصال لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.ReceiptFooter)
            .MaximumLength(200).WithMessage("ذيل الإيصال لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.EscPosCodePage)
            .InclusiveBetween(0, 255).WithMessage("رمز صفحة ESC/POS يجب أن يكون بين 0 و 255");
    }
}