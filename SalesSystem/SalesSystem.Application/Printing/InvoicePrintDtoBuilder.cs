using Microsoft.Extensions.Logging;
using SalesSystem.Application.Printing.Contracts;
using SalesSystem.Domain.Entities;
using SalesSystem.Domain.Enums;

namespace SalesSystem.Application.Printing;

/// <summary>
/// Builds InvoicePrintDto from domain entities.
/// This is the ONLY place that maps domain data to print DTOs.
/// </summary>
public class InvoicePrintDtoBuilder
{
    private readonly ILogger<InvoicePrintDtoBuilder> _logger;

    public InvoicePrintDtoBuilder(ILogger<InvoicePrintDtoBuilder> logger)
    {
        _logger = logger;
    }

    public Task<InvoicePrintDto> BuildFromSalesAsync(
        SalesInvoice invoice,
        string storeName,
        string storePhone,
        string storeAddress,
        string storeTaxNumber,
        byte[]? logoBytes,
        decimal taxRate,
        CancellationToken ct = default)
    {
        var dto = new InvoicePrintDto
        {
            StoreName = storeName,
            StorePhone = storePhone,
            StoreAddress = storeAddress,
            StoreTaxNumber = storeTaxNumber,
            LogoBytes = logoBytes,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNo.ToString(),
            InvoiceDate = invoice.CreatedAt,
            InvoiceType = InvoiceTypePrint.Sales,
            CustomerOrSupplierName = invoice.Customer?.Name ?? "زبون نقدي",
            CustomerPhone = invoice.Customer?.Phone,
            CustomerAddress = invoice.Customer?.Address,
            Items = invoice.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                item.ProductUnit?.Unit?.Name ?? string.Empty,
                item.Quantity,
                item.UnitPrice,
                0m,
                item.LineTotal
            )
            {
                Barcode = item.Product?.Barcode
            }).ToList(),
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            OtherCharges = invoice.OtherCharges,
            TaxRate = taxRate,
            TaxAmount = invoice.TaxAmount,
            GrandTotal = invoice.NetTotal,
            IsTaxInclusive = false,
            PaymentMethod = invoice.PaymentType switch
            {
                PaymentType.Cash => "نقدي",
                PaymentType.Credit => "آجل",
                PaymentType.Mixed => "نقدي + آجل",
                _ => "نقدي"
            },
            AmountPaid = invoice.PaidAmount,
            ChangeAmount = Math.Max(0, invoice.PaidAmount - invoice.NetTotal),
            Notes = invoice.Notes
        };
        return Task.FromResult(dto);
    }

    public Task<InvoicePrintDto> BuildFromPurchaseAsync(
        PurchaseInvoice invoice,
        string storeName,
        string storePhone,
        string storeAddress,
        string storeTaxNumber,
        byte[]? logoBytes,
        decimal taxRate,
        CancellationToken ct = default)
    {
        var dto = new InvoicePrintDto
        {
            StoreName = storeName,
            StorePhone = storePhone,
            StoreAddress = storeAddress,
            StoreTaxNumber = storeTaxNumber,
            LogoBytes = logoBytes,
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNo.ToString(),
            InvoiceDate = invoice.CreatedAt,
            InvoiceType = InvoiceTypePrint.Purchase,
            CustomerOrSupplierName = invoice.Supplier?.Name ?? "مورد",
            CustomerPhone = invoice.Supplier?.Phone,
            CustomerAddress = invoice.Supplier?.Address,
            Items = invoice.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                item.ProductUnit?.Unit?.Name ?? string.Empty,
                item.Quantity,
                item.UnitPrice,
                0m,
                item.LineTotal
            )
            {
                Barcode = item.Product?.Barcode
            }).ToList(),
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            OtherCharges = invoice.OtherCharges,
            TaxRate = taxRate,
            TaxAmount = invoice.TaxAmount,
            GrandTotal = invoice.NetTotal,
            IsTaxInclusive = false,
            PaymentMethod = invoice.PaymentType switch
            {
                PaymentType.Cash => "نقدي",
                PaymentType.Credit => "آجل",
                PaymentType.Mixed => "نقدي + آجل",
                _ => "نقدي"
            },
            AmountPaid = invoice.PaidAmount,
            ChangeAmount = Math.Max(0, invoice.PaidAmount - invoice.NetTotal),
            Notes = invoice.Notes
        };
        return Task.FromResult(dto);
    }

    public Task<InvoicePrintDto> BuildFromSalesReturnAsync(
        SalesReturn returnEntity,
        string storeName,
        string storePhone,
        string storeAddress,
        string storeTaxNumber,
        byte[]? logoBytes,
        decimal taxRate,
        CancellationToken ct = default)
    {
        var dto = new InvoicePrintDto
        {
            StoreName = storeName,
            StorePhone = storePhone,
            StoreAddress = storeAddress,
            StoreTaxNumber = storeTaxNumber,
            LogoBytes = logoBytes,
            InvoiceId = returnEntity.Id,
            InvoiceNumber = returnEntity.ReturnNo.ToString(),
            InvoiceDate = returnEntity.CreatedAt,
            InvoiceType = InvoiceTypePrint.SalesReturn,
            CustomerOrSupplierName = returnEntity.Customer?.Name ?? "عميل",
            CustomerPhone = returnEntity.Customer?.Phone,
            CustomerAddress = returnEntity.Customer?.Address,
            Items = returnEntity.Lines.Select(item => new InvoiceItemPrintDto(
                $"بند #{item.SalesInvoiceLineId}",
                // SalesReturnLine has no ProductUnitId — unit not tracked on sales returns
                string.Empty,
                item.Quantity,
                item.Amount,
                0m,
                item.Amount
            )).ToList(),
            SubTotal = returnEntity.TotalAmount,
            DiscountAmount = 0,
            OtherCharges = 0,
            TaxRate = taxRate,
            TaxAmount = 0,
            GrandTotal = returnEntity.TotalAmount,
            IsTaxInclusive = false,
            PaymentMethod = "نقدي",
            AmountPaid = returnEntity.TotalAmount,
            ChangeAmount = 0,
            Notes = returnEntity.Notes
        };
        return Task.FromResult(dto);
    }

    public Task<InvoicePrintDto> BuildFromPurchaseReturnAsync(
        PurchaseReturn returnEntity,
        string storeName,
        string storePhone,
        string storeAddress,
        string storeTaxNumber,
        byte[]? logoBytes,
        decimal taxRate,
        CancellationToken ct = default)
    {
        var dto = new InvoicePrintDto
        {
            StoreName = storeName,
            StorePhone = storePhone,
            StoreAddress = storeAddress,
            StoreTaxNumber = storeTaxNumber,
            LogoBytes = logoBytes,
            InvoiceId = returnEntity.Id,
            InvoiceNumber = returnEntity.ReturnNo.ToString(),
            InvoiceDate = returnEntity.CreatedAt,
            InvoiceType = InvoiceTypePrint.PurchaseReturn,
            CustomerOrSupplierName = returnEntity.Supplier?.Name ?? "مورد",
            CustomerPhone = returnEntity.Supplier?.Phone,
            CustomerAddress = returnEntity.Supplier?.Address,
            Items = returnEntity.Lines.Select(item => new InvoiceItemPrintDto(
                $"بند #{item.PurchaseInvoiceLineId}",
                string.Empty,
                item.Quantity,
                item.Amount,
                0m,
                item.Amount
            )).ToList(),
            SubTotal = returnEntity.TotalAmount,
            DiscountAmount = 0,
            OtherCharges = 0,
            TaxRate = taxRate,
            TaxAmount = 0,
            GrandTotal = returnEntity.TotalAmount,
            IsTaxInclusive = false,
            PaymentMethod = "نقدي",
            AmountPaid = returnEntity.TotalAmount,
            ChangeAmount = 0,
            Notes = returnEntity.Notes
        };
        return Task.FromResult(dto);
    }

    public Task<InvoicePrintDto> BuildFromSalesQuotationAsync(
        SalesQuotation quotation,
        string storeName,
        string storePhone,
        string storeAddress,
        string storeTaxNumber,
        byte[]? logoBytes,
        decimal taxRate,
        CancellationToken ct = default)
    {
        var statusDisplay = quotation.Status switch
        {
            QuotationStatus.Draft => "مسودة",
            QuotationStatus.Sent => "مرسل",
            QuotationStatus.Accepted => "مقبول",
            QuotationStatus.Converted => "محول لفاتورة",
            QuotationStatus.Rejected => "مرفوض",
            _ => "غير معروف"
        };

        var dto = new InvoicePrintDto
        {
            StoreName = storeName,
            StorePhone = storePhone,
            StoreAddress = storeAddress,
            StoreTaxNumber = storeTaxNumber,
            LogoBytes = logoBytes,
            InvoiceId = quotation.Id,
            InvoiceNumber = quotation.QuotationNo.ToString(),
            InvoiceDate = quotation.QuotationDate,
            InvoiceType = InvoiceTypePrint.Sales,
            CustomerOrSupplierName = quotation.Customer?.Name ?? "عميل",
            CustomerPhone = quotation.Customer?.Phone,
            CustomerAddress = quotation.Customer?.Address,
            Items = quotation.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                item.ProductUnit?.Unit?.Name ?? string.Empty,
                item.Quantity,
                item.UnitPrice,
                item.DiscountAmount,
                item.LineTotal
            )).ToList(),
            SubTotal = quotation.SubTotal,
            DiscountAmount = quotation.DiscountAmount,
            OtherCharges = 0,
            TaxRate = taxRate,
            TaxAmount = quotation.TaxAmount,
            GrandTotal = quotation.TotalAmount,
            IsTaxInclusive = false,
            PaymentMethod = "نقدي",
            AmountPaid = 0,
            ChangeAmount = 0,
            Notes = $"عرض سعر #{quotation.QuotationNo} - الحالة: {statusDisplay}\n" +
                    (quotation.TermsAndConditions != null ? $"الشروط: {quotation.TermsAndConditions}\n" : "") +
                    (quotation.ValidUntil.HasValue ? $"صالحة حتى: {quotation.ValidUntil:d}" : "") +
                    (quotation.Notes != null ? $"\n{quotation.Notes}" : "")
        };
        return Task.FromResult(dto);
    }
}
