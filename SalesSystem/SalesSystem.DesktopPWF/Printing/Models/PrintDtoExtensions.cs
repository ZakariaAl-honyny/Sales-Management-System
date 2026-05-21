using SalesSystem.Contracts.DTOs;
using SalesSystem.DesktopPWF.Printing.Models;

namespace SalesSystem.DesktopPWF.Printing.Models;

/// <summary>
/// Extension methods to map API DTOs to Print DTOs
/// </summary>
public static class PrintDtoExtensions
{
    public static StoreInfoPrintDto ToPrintDto(this StoreSettingsDto settings)
    {
        return new StoreInfoPrintDto(
            settings.StoreName,
            settings.Address,
            settings.Phone,
            null,
            settings.TaxNumber,
            settings.LogoPath
        );
    }

    public static InvoicePrintDto ToPrintDto(this SalesInvoiceDto invoice, string? taxNumber = null)
    {
        return new InvoicePrintDto(
            invoice.InvoiceNo,
            "فاتورة مبيعات",
            invoice.InvoiceDate,
            invoice.CustomerName ?? "عميل نقدي",
            null,
            invoice.WarehouseName,
            invoice.Items.Select((item, index) => new InvoiceItemPrintDto(
                index + 1,
                item.ProductName,
                item.ProductCode,
                item.Quantity,
                item.Mode == 2 ? "جملة" : "تجزئة",
                item.UnitPrice,
                item.DiscountAmount,
                item.LineTotal
            )).ToList(),
            new InvoiceTotalsPrintDto(
                invoice.SubTotal,
                invoice.DiscountAmount,
                invoice.TaxAmount,
                invoice.TotalAmount,
                invoice.PaidAmount,
                invoice.DueAmount
            ),
            invoice.Notes,
            taxNumber
        );
    }

    public static InvoicePrintDto ToPrintDto(this PurchaseInvoiceDto invoice, string? taxNumber = null)
    {
        return new InvoicePrintDto(
            invoice.InvoiceNo,
            "فاتورة مشتريات",
            invoice.InvoiceDate,
            invoice.SupplierName,
            null,
            invoice.WarehouseName,
            invoice.Items.Select((item, index) => new InvoiceItemPrintDto(
                index + 1,
                item.ProductName,
                item.ProductCode,
                item.Quantity,
                item.Mode == 2 ? "جملة" : "تجزئة",
                item.UnitCost,
                item.DiscountAmount,
                item.LineTotal
            )).ToList(),
            new InvoiceTotalsPrintDto(
                invoice.SubTotal,
                invoice.DiscountAmount,
                invoice.TaxAmount,
                invoice.TotalAmount,
                invoice.PaidAmount,
                invoice.DueAmount
            ),
            invoice.Notes,
            taxNumber
        );
    }
}
