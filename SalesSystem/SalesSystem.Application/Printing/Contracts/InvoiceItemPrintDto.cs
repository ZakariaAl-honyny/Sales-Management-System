namespace SalesSystem.Application.Printing.Contracts;

public record InvoiceItemPrintDto(
    string ProductName,
    string UnitName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Total
);
