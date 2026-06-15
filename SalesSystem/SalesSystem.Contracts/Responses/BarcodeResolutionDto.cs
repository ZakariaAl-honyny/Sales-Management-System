namespace SalesSystem.Contracts.Responses;

public record BarcodeResolutionDto(
    int ProductId,
    string ProductName,
    int ProductUnitId,
    int UnitId,
    string? UnitName,
    decimal ConversionFactor
);
