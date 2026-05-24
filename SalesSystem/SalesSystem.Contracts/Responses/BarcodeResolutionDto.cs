namespace SalesSystem.Contracts.Responses;

public record BarcodeResolutionDto(
    int ProductId,
    string ProductName,
    int ProductUnitId,
    string UnitName,
    decimal ConversionFactor,
    decimal RetailPrice,
    decimal WholesalePrice
);
