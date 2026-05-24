namespace SalesSystem.Contracts.Requests;

public record AddProductUnitRequest(
    string UnitName,
    decimal ConversionFactor,
    decimal RetailPrice,
    decimal WholesalePrice,
    bool IsBaseUnit,
    List<string> Barcodes
);
