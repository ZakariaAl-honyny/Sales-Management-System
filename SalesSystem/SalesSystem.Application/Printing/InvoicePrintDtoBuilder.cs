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
            InvoiceNumber = invoice.Id.ToString(),
            InvoiceDate = invoice.CreatedAt,
            InvoiceType = InvoiceTypePrint.Sales,
            CustomerOrSupplierName = invoice.Customer?.Name ?? "زبون نقدي",
            CustomerPhone = invoice.Customer?.Phone,
            CustomerAddress = invoice.Customer?.Address,
            Items = invoice.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                string.Empty,
                item.Quantity,
                item.UnitPrice,
                item.DiscountAmount,
                item.LineTotal
            )).ToList(),
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            TaxRate = taxRate,
            TaxAmount = invoice.TaxAmount,
            GrandTotal = invoice.TotalAmount,
            IsTaxInclusive = false,
            PaymentMethod = invoice.PaymentType switch
            {
                PaymentType.Cash => "نقدي",
                PaymentType.Credit => "آجل",
                PaymentType.Mixed => "نقدي + آجل",
                _ => "نقدي"
            },
            AmountPaid = invoice.PaidAmount,
            ChangeAmount = Math.Max(0, invoice.PaidAmount - invoice.TotalAmount),
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
            InvoiceNumber = invoice.Id.ToString(),
            InvoiceDate = invoice.CreatedAt,
            InvoiceType = InvoiceTypePrint.Purchase,
            CustomerOrSupplierName = invoice.Supplier?.Name ?? "مورد",
            CustomerPhone = invoice.Supplier?.Phone,
            CustomerAddress = invoice.Supplier?.Address,
            Items = invoice.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                string.Empty,
                item.Quantity,
                item.UnitCost,
                item.DiscountAmount,
                item.LineTotal
            )).ToList(),
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            TaxRate = taxRate,
            TaxAmount = invoice.TaxAmount,
            GrandTotal = invoice.TotalAmount,
            IsTaxInclusive = false,
            PaymentMethod = invoice.PaymentType switch
            {
                PaymentType.Cash => "نقدي",
                PaymentType.Credit => "آجل",
                PaymentType.Mixed => "نقدي + آجل",
                _ => "نقدي"
            },
            AmountPaid = invoice.PaidAmount,
            ChangeAmount = Math.Max(0, invoice.PaidAmount - invoice.TotalAmount),
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
            InvoiceNumber = returnEntity.ReturnNo,
            InvoiceDate = returnEntity.CreatedAt,
            InvoiceType = InvoiceTypePrint.SalesReturn,
            CustomerOrSupplierName = returnEntity.Customer?.Name ?? "عميل",
            CustomerPhone = returnEntity.Customer?.Phone,
            CustomerAddress = returnEntity.Customer?.Address,
            Items = returnEntity.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                string.Empty,
                item.Quantity,
                item.UnitPrice,
                item.DiscountAmount,
                item.LineTotal
            )).ToList(),
            SubTotal = returnEntity.SubTotal,
            DiscountAmount = 0,
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
            InvoiceNumber = returnEntity.ReturnNo,
            InvoiceDate = returnEntity.CreatedAt,
            InvoiceType = InvoiceTypePrint.PurchaseReturn,
            CustomerOrSupplierName = returnEntity.Supplier?.Name ?? "مورد",
            CustomerPhone = returnEntity.Supplier?.Phone,
            CustomerAddress = returnEntity.Supplier?.Address,
            Items = returnEntity.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                string.Empty,
                item.Quantity,
                item.UnitCost,
                item.DiscountAmount,
                item.LineTotal
            )).ToList(),
            SubTotal = returnEntity.SubTotal,
            DiscountAmount = 0,
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
}
