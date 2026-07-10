namespace SalesSystem.Application.Printing.Contracts;

public record InvoiceItemPrintDto(
    string ProductName,
    string UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Total
)
{
    public DateTime? ExpiryDate { get; init; }
    public string? Barcode { get; init; }
}
