using SalesSystem.Contracts.DTOs;
using SalesSystem.Contracts.Enums;
using SalesSystem.DesktopPWF.Models.Printing;
using SalesSystem.DesktopPWF.Services.App;

namespace SalesSystem.DesktopPWF.Helpers;

public static class PrintDtoExtensions
{
    public static StoreInfoPrintDto ToPrintDto(this StoreSettingsDto settings)
    {
        return new StoreInfoPrintDto
        {
            StoreName = settings.StoreName,
            Address = settings.Address ?? string.Empty,
            Phone = settings.Phone ?? string.Empty,
            TaxNumber = settings.TaxNumber ?? string.Empty,
            LogoPath = settings.LogoPath
        };
    }

    public static InvoicePrintDto ToPrintDto(this SalesInvoiceDto invoice)
    {
        return new InvoicePrintDto
        {
            InvoiceNumber = invoice.Id.ToString(),
            InvoiceDate = invoice.InvoiceDate,
            TypeName = "فاتورة مبيعات",
            CashierName = string.Empty,
            CustomerOrSupplierName = invoice.CustomerName ?? "عميل نقدي",
            PaymentType = (PaymentType)invoice.PaymentType,
            WarehouseName = invoice.WarehouseName,
            Notes = invoice.Notes
        };
    }

    public static List<InvoiceItemPrintDto> ToPrintDtos(this IEnumerable<SalesInvoiceItemDto> items)
    {
        return items.Select(i => new InvoiceItemPrintDto
        {
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Discount = i.DiscountAmount,
            LineTotal = i.LineTotal,
            Mode = i.Mode
        }).ToList();
    }

    public static InvoiceTotalsPrintDto ToTotalsPrintDto(this SalesInvoiceDto invoice)
    {
        return new InvoiceTotalsPrintDto
        {
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            Discount = invoice.DiscountAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            DueAmount = invoice.DueAmount
        };
    }

    public static InvoicePrintDto ToPrintDto(this PurchaseInvoiceDto invoice)
    {
        return new InvoicePrintDto
        {
            InvoiceNumber = invoice.Id.ToString(),
            InvoiceDate = invoice.InvoiceDate,
            TypeName = "فاتورة مشتريات",
            CashierName = string.Empty,
            CustomerOrSupplierName = invoice.SupplierName,
            PaymentType = (PaymentType)invoice.PaymentType,
            WarehouseName = invoice.WarehouseName,
            Notes = invoice.Notes
        };
    }

    public static List<InvoiceItemPrintDto> ToPrintDtos(this IEnumerable<PurchaseInvoiceItemDto> items)
    {
        return items.Select(i => new InvoiceItemPrintDto
        {
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitCost,
            Discount = i.DiscountAmount,
            LineTotal = i.LineTotal,
            Mode = i.Mode
        }).ToList();
    }

    public static InvoiceTotalsPrintDto ToTotalsPrintDto(this PurchaseInvoiceDto invoice)
    {
        return new InvoiceTotalsPrintDto
        {
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            Discount = invoice.DiscountAmount,
            TotalAmount = invoice.TotalAmount,
            PaidAmount = invoice.PaidAmount,
            DueAmount = invoice.DueAmount
        };
    }

    public static InvoicePrintDto ToPrintDto(this SalesReturnDto @return)
    {
        return new InvoicePrintDto
        {
            InvoiceNumber = @return.ReturnNo,
            InvoiceDate = @return.ReturnDate,
            TypeName = "مرتجع مبيعات",
            CashierName = string.Empty,
            CustomerOrSupplierName = @return.CustomerName ?? "عميل",
            PaymentType = PaymentType.Cash, // Returns are usually handled as cash or balance
            Notes = @return.Notes
        };
    }

    public static List<InvoiceItemPrintDto> ToPrintDtos(this IEnumerable<SalesReturnItemDto> items)
    {
        return items.Select(i => new InvoiceItemPrintDto
        {
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Discount = i.DiscountAmount,
            LineTotal = i.LineTotal,
            Mode = i.Mode
        }).ToList();
    }

    public static InvoiceTotalsPrintDto ToTotalsPrintDto(this SalesReturnDto @return)
    {
        return new InvoiceTotalsPrintDto
        {
            SubTotal = @return.TotalAmount,
            TaxAmount = 0,
            Discount = 0,
            TotalAmount = @return.TotalAmount,
            PaidAmount = 0,
            DueAmount = @return.TotalAmount
        };
    }

    public static InvoicePrintDto ToPrintDto(this PurchaseReturnDto @return)
    {
        return new InvoicePrintDto
        {
            InvoiceNumber = @return.ReturnNo,
            InvoiceDate = @return.ReturnDate,
            TypeName = "مرتجع مشتريات",
            CashierName = string.Empty,
            CustomerOrSupplierName = @return.SupplierName,
            PaymentType = PaymentType.Cash,
            Notes = @return.Notes
        };
    }

    public static List<InvoiceItemPrintDto> ToPrintDtos(this IEnumerable<PurchaseReturnItemDto> items)
    {
        return items.Select(i => new InvoiceItemPrintDto
        {
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitCost,
            Discount = i.DiscountAmount,
            LineTotal = i.LineTotal,
            Mode = i.Mode
        }).ToList();
    }

    public static InvoiceTotalsPrintDto ToTotalsPrintDto(this PurchaseReturnDto @return)
    {
        return new InvoiceTotalsPrintDto
        {
            SubTotal = @return.TotalAmount,
            TaxAmount = 0,
            Discount = 0,
            TotalAmount = @return.TotalAmount,
            PaidAmount = 0,
            DueAmount = @return.TotalAmount
        };
    }

    public static PaymentPrintDto ToPrintDto(this CustomerPaymentDto payment)
    {
        return new PaymentPrintDto
        {
            PaymentNumber = payment.PaymentNo,
            Date = payment.PaymentDate,
            Name = payment.CustomerName,
            Amount = payment.Amount,
            AmountWord = PrintHelper.ToWord(payment.Amount),
            Notes = payment.Notes ?? string.Empty,
            PaymentMethod = payment.PaymentTypeDisplay,
            TypeName = "سند قبض عميل"
        };
    }

    public static PaymentPrintDto ToPrintDto(this SupplierPaymentDto payment)
    {
        return new PaymentPrintDto
        {
            PaymentNumber = payment.PaymentNo,
            Date = payment.PaymentDate,
            Name = payment.SupplierName,
            Amount = payment.Amount,
            AmountWord = PrintHelper.ToWord(payment.Amount),
            Notes = payment.Notes ?? string.Empty,
            PaymentMethod = payment.PaymentTypeDisplay,
            TypeName = "سند صرف مورد"
        };
    }

    public static TransferPrintDto ToPrintDto(this StockTransferDto transfer)
    {
        return new TransferPrintDto
        {
            TransferNumber = transfer.TransferNo,
            Date = transfer.TransferDate,
            FromWarehouse = transfer.FromWarehouseName ?? "غير معروف",
            ToWarehouse = transfer.ToWarehouseName ?? "غير معروف",
            Notes = transfer.Notes ?? string.Empty
        };
    }

    public static List<InvoiceItemPrintDto> ToPrintDtos(this IEnumerable<StockTransferItemDto> items)
    {
        return items.Select(i => new InvoiceItemPrintDto
        {
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = 0,
            Discount = 0,
            LineTotal = 0,
            Mode = i.Mode
        }).ToList();
    }

    public static string ToArabicString(this PaymentType type) => type switch
    {
        PaymentType.Cash => "نقدي",
        PaymentType.Credit => "آجل",
        PaymentType.Mixed => "مختلط",
        _ => type.ToString()
    };
}

