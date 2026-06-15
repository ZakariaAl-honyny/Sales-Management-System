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
            CustomerOrSupplierName = invoice.Customer?.Party?.Name ?? "زبون نقدي",
            CustomerPhone = invoice.Customer?.Party?.Phone,
            CustomerAddress = invoice.Customer?.Party?.Address,
            Items = invoice.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                item.ProductUnit?.Unit?.Name ?? string.Empty,
                item.Quantity,
                item.UnitPrice,
                item.DiscountAmount,
                item.LineTotal
            )).ToList(),
            SubTotal = invoice.SubTotal,
            DiscountAmount = invoice.DiscountAmount,
            OtherCharges = invoice.OtherCharges,
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
            InvoiceNumber = invoice.InvoiceNo.ToString(),
            InvoiceDate = invoice.CreatedAt,
            InvoiceType = InvoiceTypePrint.Purchase,
            CustomerOrSupplierName = invoice.Supplier?.Party?.Name ?? "مورد",
            CustomerPhone = invoice.Supplier?.Party?.Phone,
            CustomerAddress = invoice.Supplier?.Party?.Address,
            Items = invoice.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                item.ProductUnit?.Unit?.Name ?? string.Empty,
                item.Quantity,
                item.UnitCost,
                0m,
                item.LineTotal
            )).ToList(),
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
            CustomerOrSupplierName = returnEntity.Customer?.Party?.Name ?? "عميل",
            CustomerPhone = returnEntity.Customer?.Party?.Phone,
            CustomerAddress = returnEntity.Customer?.Party?.Address,
            Items = returnEntity.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                // SalesReturnItem has no ProductUnitId — unit not tracked on sales returns
                string.Empty,
                item.Quantity,
                item.UnitPrice,
                item.DiscountAmount,
                item.LineTotal
            )).ToList(),
            SubTotal = returnEntity.SubTotal,
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
            CustomerOrSupplierName = returnEntity.Supplier?.Party?.Name ?? "مورد",
            CustomerPhone = returnEntity.Supplier?.Party?.Phone,
            CustomerAddress = returnEntity.Supplier?.Party?.Address,
            Items = returnEntity.Items.Select(item => new InvoiceItemPrintDto(
                item.Product?.Name ?? $"منتج #{item.ProductId}",
                item.ProductUnit?.Unit?.Name ?? string.Empty,
                item.Quantity,
                item.UnitCost,
                0m,
                item.LineTotal
            )).ToList(),
            SubTotal = returnEntity.SubTotal,
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
}
