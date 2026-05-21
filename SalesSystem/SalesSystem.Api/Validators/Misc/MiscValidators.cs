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

public class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator()
    {
        RuleFor(x => x.StoreName)
            .NotEmpty().WithMessage("اسم المحل مطلوب")
            .MaximumLength(200).WithMessage("اسم المحل لا يمكن أن يتجاوز 200 حرف");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("العنوان لا يمكن أن يتجاوز 500 حرف");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("رقم الهاتف لا يمكن أن يتجاوز 20 حرف");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("البريد الإلكتروني غير صحيح")
            .MaximumLength(100).WithMessage("البريد الإلكتروني لا يمكن أن يتجاوز 100 حرف")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("العملة مطلوبة")
            .MaximumLength(10).WithMessage("رمز العملة لا يمكن أن يتجاوز 10 أحرف");

        RuleFor(x => x.DefaultTaxRate)
            .GreaterThanOrEqualTo(0).WithMessage("نسبة الضريبة لا يمكن أن تكون سالبة")
            .LessThanOrEqualTo(100).WithMessage("نسبة الضريبة لا يمكن أن تتجاوز 100%");

        RuleFor(x => x.InvoicePrefix)
            .NotEmpty().WithMessage("بادئة الفاتورة مطلوبة")
            .MaximumLength(10).WithMessage("بادئة الفاتورة لا يمكن أن تتجاوز 10 أحرف");
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
    }
}